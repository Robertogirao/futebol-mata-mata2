using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace PokemonMarbleRace
{
    /// <summary>Owns the racing scene, the automatic race flow and durable championship results.</summary>
    [DisallowMultipleComponent]
    public sealed class MarbleRaceManager : MonoBehaviour
    {
        [Header("Identidade e imagens")]
        public string nomeCampeonato = "Pokémon Marble Race";
        public string campeonatoId = "pokemon_marble_race";
        public string temporadaId = "temporada_01";
        public TemaCompeticao tema;
        public Sprite fundo, spriteBola, spriteSnorlax, spriteRocket;

        [Header("Formato e participantes")]
        public RaceFormat formato = RaceFormat.MiniGP;
        public int quantidadeParticipantes = 8;
        public int quantidadeCorridas = 3;
        public int semente;
        public List<PokemonDados> participantes = new List<PokemonDados>();
        public List<int> geracoesPermitidas = new List<int>();
        public List<TipoPokemon> tiposPermitidos = new List<TipoPokemon>();

        [Header("Ritmo da transmissão")]
        public float duracaoAlvo = 45f;
        public float duracaoMaxima = 60f;
        public float duracaoAbertura = 4f;
        public float duracaoResultado = 7f;
        public float intervaloEntreCorridas = 2f;
        public bool umaCorridaPorPlay;
        public bool poderesPorTipo = true;
        [Range(0.3f, 0.65f)] public float tamanhoMarble = 0.55f;
        [HideInInspector] public string situacao = "Configure o campeonato e salve a cena antes do Play.";
        [HideInInspector] public string ultimoErro = "";

        public bool Preparada { get; private set; }
        public bool CampeonatoConcluido { get { return competition != null && competition.IsComplete; } }
        public string PastaDaTemporada
        {
            get
            {
                Require(ValidId(campeonatoId) && ValidId(temporadaId),
                    "Identificadores aceitam de 1 a 80 letras, números, _ e -.");
                return Path.Combine(Application.persistentDataPath, "MarbleRace", campeonatoId, temporadaId);
            }
        }

        private enum RaceState { Idle, Intro, Countdown, Racing, Results, BetweenRaces, Paused, Complete, Error }
        private RaceState state;
        private RaceCompetition competition;
        private RaceStorage storage;
        private RaceCourse course;
        private RacePlan plan;
        private RaceResult pendingResult;
        private RaceHud hud;
        private RaceTrackView world;
        private GameObject runtimeRoot;
        private Material marbleMaterial;
        private PhysicsMaterial2D marblePhysics;
        private AudioSource audioSource;
        private readonly List<RaceMarble> marbles = new List<RaceMarble>();
        private readonly Dictionary<string, PokemonDados> roster = new Dictionary<string, PokemonDados>(StringComparer.Ordinal);
        private float elapsed, stateTimer, hudTimer;
        private bool started;

        private sealed class UnityRaceSerializer : IRaceSerializer
        {
            public string Serialize(RaceSeason data) { return JsonUtility.ToJson(data, true); }
            public RaceSeason Deserialize(string json) { return JsonUtility.FromJson<RaceSeason>(json); }
        }

        // All scene Awake/OnEnable callbacks have finished before this explicit setup.
        private void Start()
        {
            started = true;
            try
            {
                SetupCamera();
                CreateRuntime();
                ValidateCore();
                roster.Clear();
                foreach (PokemonDados pokemon in participantes) roster.Add(pokemon.IdEstavel, pokemon);
                storage = new RaceStorage(PastaDaTemporada, new UnityRaceSerializer());
                competition = storage.LoadOrCreate(Configuration());
                Preparada = true;
                if (storage.RecoveredFromBackup)
                    Debug.LogWarning(storage.RecoveryMessage, this);
                ExportStandings();
                if (competition.IsComplete) ShowCompletedChampionship();
                else PrepareRace();
            }
            catch (Exception error) { Fail(error); }
        }

        public bool ValidarConfiguracao()
        {
            try { ValidateCore(); ultimoErro = ""; return true; }
            catch (Exception error) { ultimoErro = error.Message; return false; }
        }

        private void ValidateCore()
        {
            Require(participantes != null && participantes.Count == quantidadeParticipantes,
                "Preencha exatamente " + quantidadeParticipantes + " Pokémon ou use o sorteio do catálogo.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (PokemonDados pokemon in participantes)
            {
                Require(pokemon != null, "Preencha todas as vagas de Pokémon.");
                pokemon.ExigirValido();
                Require(ids.Add(pokemon.IdEstavel), "Pokémon repetido: " + pokemon.nomePokemon + ".");
            }
            Require(spriteBola != null, "Atribua o sprite base das marbles.");
            Require(Finite(tamanhoMarble) && tamanhoMarble >= 0.3f && tamanhoMarble <= 0.65f,
                "O tamanho das marbles deve ficar entre 0,30 e 0,65 para caber na pista e no grid.");
            Require(Finite(duracaoAlvo) && duracaoAlvo >= 10f && duracaoAlvo <= 300f,
                "Escolha um tempo alvo entre 10 e 300 segundos.");
            Require(Finite(duracaoAbertura) && duracaoAbertura >= 0f && duracaoAbertura <= 120f &&
                Finite(duracaoResultado) && duracaoResultado >= 1f && duracaoResultado <= 300f &&
                Finite(intervaloEntreCorridas) && intervaloEntreCorridas >= 0f && intervaloEntreCorridas <= 120f,
                "Confira os tempos de abertura, resultado e intervalo.");
            RaceCompetition.ValidateConfig(Configuration());
            TMP_FontAsset font = BodyFont();
            Require(font != null, "Atribua uma fonte TMP ao tema ou importe os recursos essenciais do TextMesh Pro.");
        }

        private RaceConfig Configuration()
        {
            return new RaceConfig
            {
                competitionId = campeonatoId,
                seasonId = temporadaId,
                title = nomeCampeonato,
                format = formato,
                raceCount = quantidadeCorridas,
                configuredSeed = semente,
                trackVersion = 1,
                participantIds = participantes != null ? participantes.Where(p => p != null).Select(p => p.IdEstavel).ToList() : new List<string>(),
                targetSeconds = duracaoAlvo,
                maxSeconds = duracaoMaxima,
                typePowers = poderesPorTipo,
                marbleSize = tamanhoMarble
            };
        }

        private void SetupCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var go = new GameObject("Marble Race Camera");
                go.tag = "MainCamera";
                camera = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(8.3f, 14.4f / Mathf.Max(0.1f, camera.aspect));
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.transform.rotation = Quaternion.identity;
            camera.backgroundColor = new Color(0.015f, 0.025f, 0.065f);
        }

        private void CreateRuntime()
        {
            runtimeRoot = new GameObject("Marble Race Runtime");
            runtimeRoot.transform.SetParent(transform, false);
            var graphics = new GameObject("Broadcast");
            graphics.transform.SetParent(runtimeRoot.transform, false);
            hud = graphics.AddComponent<RaceHud>();
            hud.Build(TitleFont(), BodyFont(), nomeCampeonato);
            var track = new GameObject("Course");
            track.transform.SetParent(runtimeRoot.transform, false);
            world = track.AddComponent<RaceTrackView>();
            marbleMaterial = RaceTrackView.CreateSpriteMaterial();
            marblePhysics = new PhysicsMaterial2D("Race Marbles") { friction = 0.03f, bounciness = 0.65f };
            audioSource = runtimeRoot.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        private void PrepareRace()
        {
            ValidateCore();
            // Check settings against the stored season before using a new race.
            RaceCompetition.Restore(competition.Snapshot(), Configuration());
            ClearMarbles();
            plan = competition.CurrentRace;
            if (plan == null) { ShowCompletedChampionship(); return; }
            course = RaceCourseGenerator.Generate(plan.seed);
            RaceCourseGenerator.Validate(course);
            world.Build(course, fundo, spriteSnorlax, spriteRocket);
            var grid = new List<string>(plan.participantIds);
            var random = new System.Random(plan.seed ^ 0x5f3759df);
            for (int i = grid.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                string swap = grid[i]; grid[i] = grid[j]; grid[j] = swap;
            }
            float speed = (course.Length - 1.5f) / duracaoAlvo;
            for (int i = 0; i < grid.Count; i++)
            {
                var go = new GameObject("Marble " + grid[i]);
                go.transform.SetParent(runtimeRoot.transform, false);
                RaceMarble marble = go.AddComponent<RaceMarble>();
                marble.Initialize(roster[grid[i]], course, plan.seed, i, tamanhoMarble, spriteBola,
                    marbleMaterial, marblePhysics, BodyFont(), poderesPorTipo, speed, world.ObstacleColliders);
                marbles.Add(marble);
            }
            elapsed = hudTimer = 0f;
            pendingResult = null;
            state = RaceState.Intro;
            stateTimer = duracaoAbertura;
            ultimoErro = "";
            situacao = "Prova " + plan.number + "/" + competition.TotalRaces + ": grid pronto; pista salva pela semente " + plan.seed + ".";
            hud.SetRaceInfo(nomeCampeonato, plan.label, plan.number, competition.TotalRaces, duracaoMaxima, "STARTING GRID");
            hud.SetLeaderboard(LiveEntries());
            hud.ShowResults("STARTING GRID", GridEntries(), plan.label + "  •  " + FormatName(formato));
        }

        private void Update()
        {
            if (!Preparada || state == RaceState.Error || state == RaceState.Complete || state == RaceState.Paused) return;
            try
            {
                float dt = Time.captureDeltaTime > 0f && Finite(Time.captureDeltaTime)
                    ? Time.captureDeltaTime : Time.unscaledDeltaTime;
                switch (state)
                {
                    case RaceState.Intro:
                        stateTimer -= dt;
                        if (stateTimer <= 0f)
                        {
                            state = RaceState.Countdown;
                            stateTimer = 3f;
                            hud.ShowMessage("3", "GET READY");
                        }
                        break;
                    case RaceState.Countdown:
                        stateTimer -= dt;
                        if (stateTimer <= 0f) StartRace();
                        else hud.ShowMessage(Mathf.CeilToInt(stateTimer).ToString(CultureInfo.InvariantCulture), "GET READY");
                        break;
                    case RaceState.Racing:
                        elapsed += Time.deltaTime;
                        hudTimer -= dt;
                        if (hudTimer <= 0f) { UpdateLiveHud(); hudTimer = 0.1f; }
                        if (marbles.All(m => m.Finished) || elapsed >= duracaoMaxima) FinishRace();
                        break;
                    case RaceState.Results:
                        stateTimer -= dt;
                        if (stateTimer <= 0f)
                        {
                            if (competition.IsComplete) ShowCompletedChampionship();
                            else if (umaCorridaPorPlay)
                            {
                                state = RaceState.Paused;
                                hud.ShowResults("CHAMPIONSHIP STANDINGS", StandingEntries(), "RESULTS SAVED  •  NEXT RACE READY");
                                situacao = "Resultado salvo. O próximo Play retoma a próxima prova; use Continuar para seguir agora.";
                            }
                            else
                            {
                                hud.ShowResults("CHAMPIONSHIP STANDINGS", StandingEntries(), "RESULTS SAVED  •  NEXT RACE READY");
                                state = RaceState.BetweenRaces;
                                stateTimer = intervaloEntreCorridas;
                            }
                        }
                        break;
                    case RaceState.BetweenRaces:
                        stateTimer -= dt;
                        if (stateTimer <= 0f) PrepareRace();
                        break;
                }
            }
            catch (Exception error) { Fail(error); }
        }

        private void FixedUpdate()
        {
            if (!Preparada || world == null || course == null) return;
            try
            {
                // Hold kinematic gates at the current race pose during introductions,
                // results and pauses instead of leaving their last velocity active.
                world.SetElapsed(elapsed);
                if (state != RaceState.Racing) return;
                for (int i = 0; i < marbles.Count; i++) marbles[i].Step(elapsed, Time.fixedDeltaTime, marbles);
            }
            catch (Exception error) { Fail(error); }
        }

        private void StartRace()
        {
            state = RaceState.Racing;
            elapsed = 0f;
            hud.HideOverlay();
            foreach (RaceMarble marble in marbles) marble.SetRacing(true);
            if (tema != null && tema.somAbertura != null) audioSource.PlayOneShot(tema.somAbertura);
            situacao = "Prova " + plan.number + " em andamento. O resultado será salvo ao terminar.";
            UpdateLiveHud();
        }

        private List<RaceMarble> OrderedMarbles()
        {
            return marbles.OrderByDescending(m => m.Finished)
                .ThenBy(m => m.Finished ? m.FinishTime : 0f)
                .ThenByDescending(m => m.Progress)
                .ThenBy(m => m.Pokemon.IdEstavel, StringComparer.Ordinal).ToList();
        }

        private void FinishRace()
        {
            foreach (RaceMarble marble in marbles) marble.SetRacing(false);
            pendingResult = new RaceResult { raceId = plan.id };
            List<RaceMarble> order = OrderedMarbles();
            for (int i = 0; i < order.Count; i++)
                pendingResult.finishOrder.Add(order[i].Result(i + 1, Mathf.Min(elapsed, duracaoMaxima)));
            SaveAndShowResult();
        }

        private void SaveAndShowResult()
        {
            Require(pendingResult != null, "Não há um resultado pendente para salvar.");
            ValidateCore();
            RaceCompetition.Restore(competition.Snapshot(), Configuration());
            // The durable commit happens before live points or the next race can change.
            storage.SaveResult(competition, pendingResult);
            RaceResult savedResult = pendingResult;
            pendingResult = null;
            ExportStandings();
            state = RaceState.Results;
            stateTimer = duracaoResultado;
            hud.SetRaceInfo(nomeCampeonato, plan.label, plan.number, competition.TotalRaces, 0f, "RESULT SAVED");
            hud.SetAbility("", "", 0f);
            hud.ShowResults("RACE " + plan.number + " RESULTS", ResultEntries(savedResult),
                "RESULT SAVED  •  TOTAL CHAMPIONSHIP POINTS");
            situacao = "Prova " + plan.number + " salva. Classificação atualizada automaticamente.";
            ultimoErro = "";
        }

        private void ShowCompletedChampionship()
        {
            foreach (RaceMarble marble in marbles) marble.SetRacing(false);
            state = RaceState.Complete;
            var standings = StandingEntries();
            string champion = standings.Count > 0 ? standings[0].name.ToUpperInvariant() : "CHAMPION";
            hud.SetRaceInfo(nomeCampeonato, FormatName(formato), competition.CompletedRaces, competition.TotalRaces, 0f, "CHAMPIONSHIP COMPLETE");
            hud.ShowResults("CHAMPION: " + champion, standings, "FINAL STANDINGS  •  CHAMPIONSHIP SAVED");
            situacao = "Campeonato concluído. A classificação final está salva; use Nova temporada para começar outro.";
        }

        private void UpdateLiveHud()
        {
            hud.SetRaceInfo(nomeCampeonato, plan.label, plan.number, competition.TotalRaces,
                Mathf.Max(0f, duracaoMaxima - elapsed), "RACING");
            hud.SetLeaderboard(LiveEntries());
            RaceMarble active = OrderedMarbles().FirstOrDefault(m => m.AbilityRemaining > 0f && !m.Finished);
            hud.SetAbility(active != null ? active.Pokemon.nomePokemon : "",
                active != null ? active.Ability : "", active != null ? active.AbilityRemaining : 0f);
        }

        private List<RaceHudEntry> GridEntries()
        {
            return plan.participantIds.Select((id, i) => Entry(id, i + 1, 0,
                RaceMarble.TypeName(roster[id].tipoPrimario).ToUpperInvariant(), false)).ToList();
        }

        private List<RaceHudEntry> LiveEntries()
        {
            List<RaceMarble> ordered = OrderedMarbles();
            return ordered.Select((marble, i) => Entry(marble.Pokemon.IdEstavel, i + 1, 0,
                marble.Finished ? marble.FinishTime.ToString("0.00", CultureInfo.InvariantCulture) + "s  •  FINISHED"
                    : (marble.Progress / course.Length * 100f).ToString("0", CultureInfo.InvariantCulture) + "%  •  " + course.LabelAt(marble.Progress).ToUpperInvariant(), marble.Finished)).ToList();
        }

        private List<RaceHudEntry> StandingEntries()
        {
            return competition.Standings().Select(row => Entry(row.id, row.position, row.points,
                row.races + " RACES  •  " + row.wins + " WINS", competition.IsComplete)).ToList();
        }

        private List<RaceHudEntry> ResultEntries(RaceResult result)
        {
            Dictionary<string, int> points = competition.Standings().ToDictionary(row => row.id, row => row.points);
            return result.finishOrder.Select(finish => Entry(finish.id, finish.place, points[finish.id],
                finish.finished ? finish.time.ToString("0.00", CultureInfo.InvariantCulture) + "s  •  FINISHED"
                    : (finish.progress * 100f).ToString("0", CultureInfo.InvariantCulture) + "%  •  TIME LIMIT", finish.finished)).ToList();
        }

        private RaceHudEntry Entry(string id, int position, int points, string detail, bool finished)
        {
            PokemonDados pokemon = roster[id];
            return new RaceHudEntry { id = id, name = pokemon.nomePokemon, detail = detail,
                portrait = pokemon.retrato, color = pokemon.cor, position = position, points = points, finished = finished };
        }

        private void ExportStandings()
        {
            try { storage.ExportStandings(competition); }
            catch (Exception error) { Debug.LogWarning("O campeonato foi salvo; não foi possível exportar o CSV: " + error.Message, this); }
        }

        [ContextMenu("Marble Race/Continuar no Play ou tentar salvar")]
        public void ContinuarNoPlay()
        {
            if (!Application.isPlaying || !Preparada || competition == null) return;
            try
            {
                if (pendingResult != null) { SaveAndShowResult(); return; }
                if (competition.IsComplete) { ShowCompletedChampionship(); return; }
                if (state == RaceState.Paused || state == RaceState.Error) PrepareRace();
            }
            catch (Exception error) { Fail(error); }
        }

        private void Fail(Exception error)
        {
            ultimoErro = error.Message;
            situacao = "Corrida pausada: " + ultimoErro;
            state = RaceState.Error;
            foreach (RaceMarble marble in marbles) if (marble != null) marble.SetRacing(false);
            if (hud != null)
                hud.ShowMessage(pendingResult != null ? "RESULT NOT SAVED" : "RACE PAUSED",
                    pendingResult != null ? "Race stopped. Retry saving to continue." : "Your completed race results are preserved.");
            Debug.LogError(ultimoErro, this);
        }

        private TMP_FontAsset BodyFont()
        {
            if (tema != null && tema.fonteTexto != null) return tema.fonteTexto;
            if (tema != null && tema.fonteTitulo != null) return tema.fonteTitulo;
            return TMP_Settings.defaultFontAsset != null ? TMP_Settings.defaultFontAsset : Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        private TMP_FontAsset TitleFont() { return tema != null && tema.fonteTitulo != null ? tema.fonteTitulo : BodyFont(); }
        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static bool ValidId(string value) { return !string.IsNullOrEmpty(value) && Regex.IsMatch(value, @"\A[A-Za-z0-9_-]{1,80}\z"); }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static string FormatName(RaceFormat value)
        {
            switch (value) { case RaceFormat.SingleRace: return "SINGLE RACE"; case RaceFormat.BestOfThree: return "BEST OF THREE"; case RaceFormat.Knockout: return "KNOCKOUT"; default: return "MINI GP"; }
        }

        private void ClearMarbles()
        {
            foreach (RaceMarble marble in marbles)
                if (marble != null) { marble.SetRacing(false); marble.gameObject.SetActive(false); Destroy(marble.gameObject); }
            marbles.Clear();
        }

        private void OnDisable()
        {
            foreach (RaceMarble marble in marbles) if (marble != null) marble.SetRacing(false);
            if (runtimeRoot != null) runtimeRoot.SetActive(false);
            if (started && state != RaceState.Complete && state != RaceState.Error) state = RaceState.Paused;
        }

        private void OnEnable()
        {
            if (runtimeRoot != null) runtimeRoot.SetActive(true);
        }

        private void OnDestroy()
        {
            ClearMarbles();
            if (hud != null) hud.Clear();
            if (world != null) world.Clear();
            if (runtimeRoot != null) Destroy(runtimeRoot);
            if (marbleMaterial != null) Destroy(marbleMaterial);
            if (marblePhysics != null) Destroy(marblePhysics);
        }
    }
}
