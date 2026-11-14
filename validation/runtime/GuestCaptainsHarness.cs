using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FutebolDeDiscos;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Actual controller, scheduler, stores, match actors and UI; pose/contact steps
// are injected by the test contract rather than native Unity physics.
public static class GuestCaptainsHarness
{
    private static int checks;
    private static string scenario;
    private static void Check(bool value, string message)
    {
        checks++;
        if (!value) throw new Exception(scenario + ": " + message + " Errors: " + string.Join(" | ", Debug.Errors.ToArray()));
    }
    private static object Call(object target, string name, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null) throw new Exception("Missing method " + name);
        try { return method.Invoke(target, args); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }
    private static void Clean()
    {
        foreach (GameObject go in GameObject.All.Where(g => !g.destroyed && g.transform.parent == null).ToArray()) UnityEngine.Object.Destroy(go);
        GameObject.All.Clear(); Scheduler.Reset(); Time.captureDeltaTime = 0f; Time.timeScale = 1f;
        Debug.Errors.Clear(); Debug.Warnings.Clear(); Debug.Messages.Clear();
    }
    private static List<TeamData> Teams(int count)
    {
        var teams = new List<TeamData>();
        for (int i = 0; i < count; i++)
        {
            var team = ScriptableObject.CreateInstance<TeamData>();
            team.name = "guest_team_" + i; team.nomeEquipe = "Club " + i; team.sigla = "C" + i; team.overall = 61;
            team.escudoOuBandeira = new Sprite { name = "Club crest " + i };
            team.corPrincipal = i % 2 == 0 ? Color.blue : Color.red;
            for (int p = 0; p < 6; p++) team.elenco.Add(new JogadorDados { nomeJogador = "Club " + i + " footballer " + p, foto512x512 = new Sprite { name = "Football portrait " + i + "-" + p } });
            teams.Add(team);
        }
        return teams;
    }
    private static List<CapitaoConvidado> Captains(int count)
    {
        return Enumerable.Range(0, count).Select(i =>
        {
            var captain = ScriptableObject.CreateInstance<CapitaoConvidado>();
            captain.name = "hero_" + i; captain.nomeCapitao = "Hero " + i; captain.overall = 45 + i * 7;
            captain.foto = new Sprite { name = "Hero portrait " + i };
            return captain;
        }).ToList();
    }
    private sealed class Competition
    {
        public readonly GameObject Root;
        public readonly ProjetoFutebolDiscos Config;
        public readonly TournamentManager Manager;
        public readonly LigaCampeonato League;
        public readonly MataMataCampeonato Knockout;
        public readonly UniversalUI Ui;
        public readonly PartidaDiscos Match;
        public readonly GoalBannerUI Banner;
        public Competition(string season, FormatoDiscos mode, List<TeamData> teams, List<CapitaoConvidado> captains,
            bool extra = true, int discs = 3, int seed = 13579, bool guests = true, bool start = true, bool teamStrength = true, float duration = 45f)
        {
            Root = new GameObject("Guest captain fixture"); Root.SetActive(false);
            Config = Root.AddComponent<ProjetoFutebolDiscos>();
            Manager = Root.AddComponent<TournamentManager>(); League = Root.AddComponent<LigaCampeonato>();
            Knockout = Root.AddComponent<MataMataCampeonato>(); Ui = Root.AddComponent<UniversalUI>();
            Root.AddComponent<ClassificacaoUI>(); Root.AddComponent<AberturaJogoUI>();
            Banner = Root.AddComponent<GoalBannerUI>(); Root.AddComponent<ChaveamentoUI>(); Match = Root.AddComponent<PartidaDiscos>();
            Config.competicaoId = "guest_integration"; Config.temporadaId = season; Config.formato = mode;
            Config.participantes = teams; Config.quantidadeTimes = teams.Count; Config.quantidadeRodadas = 3;
            Config.semente = seed; Config.sortearConfrontos = false; Config.usarCapitaesConvidados = guests;
            Config.capitaoExtra = extra; Config.catalogoCapitaes = captains; Config.discosPorTime = discs;
            Config.usarForcaDosTimes = teamStrength; Config.forcaChute = 4f;
            Config.duracaoPartida = duration; Config.tempoAbertura = Config.tempoFotos = Config.tempoTransicaoFotos = Config.tempoTabela = Config.intervaloEntrePartidas = 0f;
            Config.pausaAposGol = .2f;
            Config.tema = ScriptableObject.CreateInstance<TemaCompeticao>(); Config.tema.fonteTexto = Config.tema.fonteTitulo = TMP_Settings.defaultFontAsset;
            Root.SetActive(true); Banner.tempoDeExibicao = .02f; Banner.intervaloEntreBanners = 0f;
            if (start) Start();
        }
        public void Start() { Call(Config, "Start"); Call(Manager, "Start"); }
        public void Frame(bool winGolden = false)
        {
            Time.captureDeltaTime = Time.unscaledDeltaTime = .1f; Time.deltaTime = .1f * Time.timeScale;
            Call(Config, "Update"); Call(Match, "Update"); Call(Match, "FixedUpdate"); Call(Ui, "Update");
            if (winGolden && Ui.EmProrrogacao && Ui.partidaEmAndamento && Match.EmAndamento) Goal(Match.Atletas.First(a => a.TimeA), false);
            Scheduler.Tick(.1f);
        }
        public void Until(Func<bool> condition, int frames = 2000, bool winGolden = false)
        {
            for (int i = 0; i < frames && !condition(); i++) Frame(winGolden);
            Check(condition(), "progress reaches expected gate: " + Config.situacao);
        }
        public void Begin()
        {
            Until(() => Ui.partidaEmAndamento && Match.EmAndamento);
            Call(Match, "FixedUpdate"); Physics2D.Simulate(Time.fixedDeltaTime); Call(Match, "FixedUpdate");
        }
        public void Goal(DiscoAtleta actor, bool left)
        {
            Banner.LimparFila(); Match.Bola.RegistrarToque(actor);
            Vector2 p = new Vector2((left ? -1 : 1) * (CampoDiscos.MeioComprimento + .7f), 0f);
            Match.Bola.transform.position = p; Match.Bola.Corpo.position = p; Match.NotificarGol(left);
        }
        public string Store { get { return Path.Combine(Config.PastaDaTemporada, "capitaes_convidados.json"); } }
        public string Metadata { get { return Path.Combine(Config.PastaDaTemporada, "configuracao_discos.json"); } }
        public string Results { get { return Path.Combine(Application.persistentDataPath, Config.formato == FormatoDiscos.Liga ? "Ligas" : "MataMata", Config.competicaoId, Config.temporadaId, Config.formato == FormatoDiscos.Liga ? "temporada.json" : "torneio.json"); } }
    }
    private static string Mapping(Competition fixture)
    {
        return string.Join("|", fixture.Config.participantes.Select(t => t.name + "=" + fixture.Config.ObterCapitaoConvidado(t).Identificador).ToArray());
    }
    private static void RostersPhotosAndGoals()
    {
        foreach (FormatoDiscos mode in new[] { FormatoDiscos.Liga, FormatoDiscos.MataMata }) foreach (bool extra in new[] { true, false })
        {
            scenario = "guest identity " + mode + " extra=" + extra;
            var teams = Teams(4); var captains = Captains(6); var original = teams.Select(t => t.elenco.ToArray()).ToArray();
            var fixture = new Competition("identity_" + mode + "_" + extra, mode, teams, captains, extra, start: false);
            fixture.Config.capitaesFixos.Add(new VinculoCapitaoConvidado { time = teams[0], capitao = captains[0] }); fixture.Start();
            Check(fixture.Config.Preparado && File.Exists(fixture.Store), "captains are assigned and saved before the first match");
            Check(fixture.Config.ObterCapitaoConvidado(teams[0]) == captains[0], "manual hero/team assignment is honored");
            Check(teams.Select(t => fixture.Config.ObterCapitaoConvidado(t)).Distinct().Count() == teams.Count, "all active teams receive distinct captains");
            fixture.Begin();
            int expectedCount = extra ? 4 : 3;
            foreach (var group in fixture.Match.Atletas.GroupBy(a => a.Equipe))
            {
                var actors = group.OrderBy(a => a.Indice).ToArray(); TeamData team = group.Key; CapitaoConvidado hero = fixture.Config.ObterCapitaoConvidado(team);
                Check(actors.Length == expectedCount && actors.Count(a => a.Capitao) == 1, "one guest captain and the configured number of football slots");
                Check(actors[0].NomeExibicao == hero.nomeCapitao && actors[0].Jogador.foto512x512 == hero.foto, "guest name and portrait are the captain identity");
                Check(actors[0].TemOverallIndividual && actors[0].OverallEfetivo == hero.overall, "only captain receives the individual rating");
                Check(actors.Skip(1).Select(a => a.Jogador).SequenceEqual(team.elenco.Take(expectedCount - 1)), "ordinary discs start with the first footballer rather than excluding the old representative");
                Check(actors.Skip(1).All(a => !a.TemOverallIndividual && a.OverallEfetivo == team.overall), "ordinary players keep their own team's rating");
                Check(fixture.Root.GetComponentsInChildren<Image>(true).Any(i => i.sprite == hero.foto), "the actual broadcast cards show the guest portrait");
                Check(ReferenceEquals(fixture.Config.SelecionarRepresentantes(team)[0], actors[0].Jogador), "opening and actor share the single selected guest object");
                Check(ReferenceEquals(fixture.Match.SelecionarRepresentantes(team)[0], actors[0].Jogador), "match selection forwards the same saved guest identity");
                foreach (var actor in actors)
                    Check(actor.GetComponentsInChildren<TextMeshPro>(true).Any(t => t.text == actor.NomeExibicao), "name tags show each actual captain/footballer");
            }
            Check(fixture.Root.GetComponentsInChildren<TextMeshProUGUI>(true).Any(t => t.gameObject.name == "Broadcast Format" && t.text.Contains(expectedCount + " v " + expectedCount)), "broadcast count includes the guest extra disc");
            var captain = fixture.Match.Atletas.Single(a => a.TimeA && a.Capitao);
            fixture.Goal(captain, false);
            Check(fixture.Ui.GolsA == 1 && fixture.Banner.fotoJogador.sprite == captain.Jogador.foto512x512 && fixture.Banner.textoDados.text.Contains(captain.NomeExibicao.ToUpperInvariant()), "guest goal uses the guest name and portrait");
            fixture.Until(() => !fixture.Match.EmReposicao, 20);
            var ordinary = fixture.Match.Atletas.First(a => a.TimeA && !a.Capitao);
            fixture.Goal(ordinary, false);
            Check(fixture.Ui.GolsA == 2 && fixture.Banner.fotoJogador.sprite == ordinary.Equipe.escudoOuBandeira && fixture.Banner.textoDados.text.Contains(ordinary.NomeExibicao.ToUpperInvariant()), "footballer goal retains his own name and club crest");
            for (int i = 0; i < teams.Count; i++) Check(teams[i].elenco.SequenceEqual(original[i]) && teams[i].overall == 61, "guest assignment and goals do not mutate football assets");
            Clean();
        }
    }
    private static float[] Performance(string season, int heroOverall, bool teamStrength)
    {
        var teams = Teams(2); var heroes = Captains(2); heroes[0].overall = heroOverall;
        var fixture = new Competition(season, FormatoDiscos.Liga, teams, heroes, start: false, teamStrength: teamStrength);
        fixture.Config.capitaesFixos.Add(new VinculoCapitaoConvidado { time = teams[0], capitao = heroes[0] }); fixture.Start(); fixture.Begin();
        var captain = fixture.Match.Atletas.Single(a => a.TimeA && a.Capitao);
        var ordinary = fixture.Match.Atletas.Single(a => a.TimeA && a.Indice == 1);
        var values = new List<float>();
        foreach (var actor in new[] { captain, ordinary })
        {
            foreach (var disc in fixture.Match.Atletas) disc.Repor(fixture.Match.Formacao(disc.TimeA, disc.Indice), true);
            fixture.Match.Bola.Repor(new Vector2(-2f, 1f), Vector2.zero, true);
            actor.Mover(1f, Time.fixedDeltaTime, fixture.Match.Atletas.ToList(), fixture.Match.Bola, false);
            values.Add(actor.Corpo.linearVelocity.magnitude);
        }
        foreach (var actor in new[] { captain, ordinary })
        {
            actor.Repor(fixture.Match.Formacao(actor.TimeA, actor.Indice), true);
            fixture.Match.Bola.Repor(Vector2.zero, Vector2.zero, true);
            fixture.Match.TentarChute(actor); values.Add(fixture.Match.Bola.Corpo.linearVelocity.magnitude);
            Check(fixture.Match.Bola.UltimoToque == actor, "shot is attributed to the actual shooter");
        }
        foreach (var actor in new[] { captain, ordinary })
        {
            float squaredAim = 0f;
            for (int shot = 0; shot < 64; shot++)
            {
                actor.Repor(fixture.Match.Formacao(actor.TimeA, actor.Indice), true);
                fixture.Match.Bola.Repor(Vector2.zero, Vector2.zero, true); fixture.Match.TentarChute(actor);
                Vector2 v = fixture.Match.Bola.Corpo.linearVelocity;
                float atGoal = v.y / v.x * (CampoDiscos.MeioComprimento + .4f);
                squaredAim += atGoal * atGoal;
            }
            values.Add(squaredAim / 64f);
        }
        Check(teams.All(t => t.overall == 61), "individual strength never rewrites either team's rating");
        Clean(); return values.ToArray();
    }
    private static void IndividualStrengthAndSixDiscs()
    {
        foreach (bool useTeamStrength in new[] { false, true })
        {
            scenario = "individual captain performance team-overall=" + useTeamStrength;
            float[] weak = Performance("weak_" + useTeamStrength, 10, useTeamStrength);
            float[] strong = Performance("strong_" + useTeamStrength, 100, useTeamStrength);
            Check(strong[0] > weak[0] + .001f && strong[2] > weak[2] + .001f, "captain rating changes only his movement and shot power even with team strength disabled");
            Check(Math.Abs(strong[1] - weak[1]) < .0001f && Math.Abs(strong[3] - weak[3]) < .0001f, "teammate movement and shot power remain unchanged when captain rating changes");
            Check(strong[4] < weak[4] && Math.Abs(strong[5] - weak[5]) < .0001f, "same-seed shot sample becomes more accurate only for the higher-rated captain");
        }
        scenario = "six discs and guest without portrait";
        var teams = Teams(2); var heroes = Captains(2); heroes[0].foto = heroes[1].foto = null;
        var fixture = new Competition("six_discs", FormatoDiscos.Liga, teams, heroes, discs: 5); fixture.Begin();
        Check(fixture.Match.Atletas.Count == 12 && fixture.Config.TotalDiscosPorTime == 6, "five footballers plus the guest can produce six discs per side");
        foreach (var actor in fixture.Match.Atletas)
        {
            Check(Math.Abs(actor.Corpo.position.x) < CampoDiscos.MeioComprimento && Math.Abs(actor.Corpo.position.y) < CampoDiscos.MeioLargura, "all six formation slots remain within field boundaries");
            if (actor.Capitao) Check(actor.ImagemDoGol == actor.Equipe.escudoOuBandeira, "a guest without portrait falls back to the club crest");
        }
        Clean();
    }
    private static void PersistenceAndGuards()
    {
        foreach (FormatoDiscos mode in new[] { FormatoDiscos.Liga, FormatoDiscos.MataMata })
        {
            scenario = "saved assignments " + mode;
            var teams = Teams(4); var heroes = Captains(6);
            var first = new Competition("resume_" + mode, mode, teams, heroes, seed: 0, duration: .3f);
            string mapping = Mapping(first), contents = File.ReadAllText(first.Store), metadata = File.ReadAllText(first.Metadata);
            Check(first.Config.SementeEfetiva > 0, "automatic captain draw uses the persisted effective season seed");
            first.Begin();
            Check(Mapping(first) == mapping, "preparing a match never rerolls the season's captains");
            BolaDiscos openingBall = first.Match.Bola;
            first.Until(() => mode == FormatoDiscos.Liga
                ? first.Manager.rodadaAtual.Any(c => c.concluido && c.pontosRegistrados)
                : first.Knockout.EstadoAtual.fases.SelectMany(f => f.partidas).Any(p => p.concluida && !p.bye), winGolden: true);
            first.Until(() => first.Match.Bola != openingBall && first.Ui.partidaEmAndamento);
            Check(Mapping(first) == mapping, "starting the next scheduled match preserves every season assignment");
            foreach (var actor in first.Match.Atletas.Where(a => a.Capitao))
                Check(actor.NomeExibicao == first.Config.ObterCapitaoConvidado(actor.Equipe).nomeCapitao, "next match still uses its team's saved guest representative");
            string results = File.ReadAllText(first.Results);
            Clean();
            var resumed = new Competition("resume_" + mode, mode, teams, heroes, seed: 0, duration: .3f);
            Check(resumed.Config.Preparado && Mapping(resumed) == mapping, "a fresh Play restores every captain/team association");
            Check(File.ReadAllText(resumed.Store) == contents && File.ReadAllText(resumed.Metadata) == metadata, "resume preserves exact captain and core metadata bytes");
            Check(File.ReadAllText(resumed.Results) == results, "resume keeps the result already registered by the previous Play");
            if (mode == FormatoDiscos.Liga)
                Check(resumed.Manager.rodadaAtual.Count(c => c.concluido && c.pontosRegistrados) == 1, "pending league round resumes with exactly one saved result rather than repeating it");
            resumed.Begin();
            CapitaoConvidado assigned = resumed.Config.ObterCapitaoConvidado(teams[0]);
            int old = assigned.overall; assigned.overall = old == 100 ? 99 : old + 1;
            Clean();
            var invalid = new Competition("resume_" + mode, mode, teams, heroes, seed: 0, duration: .3f);
            Check(!invalid.Config.Preparado && invalid.Config.ultimoErro.Contains("overall"), "changing an assigned rating cannot silently change a saved season");
            Check(File.ReadAllText(invalid.Store) == contents && File.ReadAllText(invalid.Metadata) == metadata, "rejected stat changes preserve existing files");
            assigned.overall = old; Clean();
            var changedCount = new Competition("resume_" + mode, mode, teams, heroes, extra: false, seed: 0, duration: .3f);
            Check(!changedCount.Config.Preparado && File.ReadAllText(changedCount.Store) == contents, "changing extra/replacement rule cannot rewrite the saved assignment");
            Clean();
            var disabled = new Competition("resume_" + mode, mode, teams, heroes, seed: 0, guests: false, duration: .3f);
            Check(!disabled.Config.Preparado && disabled.Config.ultimoErro.Contains("capitães"), "guest season cannot be resumed as an ordinary football season");
            Clean();
            var next = new Competition("fresh_" + mode, mode, teams, heroes, seed: 0);
            Check(next.Config.Preparado && File.Exists(next.Store) && next.Config.SementeEfetiva > 0, "a new season starts with its own draw and store");
            Check(next.Manager.rodadaAtual.All(c => !c.concluido), "new season inherits no earlier match results");
            Clean();
        }
    }
    private static void OriginalMode()
    {
        scenario = "original football mode remains unchanged";
        var teams = Teams(2); var fixture = new Competition("original", FormatoDiscos.Liga, teams, new List<CapitaoConvidado>(), guests: false);
        Check(fixture.Config.Preparado && !File.Exists(fixture.Store), "ordinary scene needs no external catalogue or companion store");
        fixture.Begin();
        Check(fixture.Match.Atletas.Count == 6 && fixture.Config.TotalDiscosPorTime == 3, "ordinary teams retain the existing disc total");
        Check(fixture.Match.Atletas.All(a => !a.TemOverallIndividual), "ordinary football captains do not gain an individual override");
        foreach (var captain in fixture.Match.Atletas.Where(a => a.Capitao))
            Check(captain.Jogador == captain.Equipe.elenco[0] && fixture.Config.ObterCapitaoConvidado(captain.Equipe) == null, "ordinary scene still chooses a football representative");
        Check(Debug.Errors.Count == 0, "normal football setup logs no new validation error"); Clean();
    }
    public static void Main()
    {
        string folder = Path.Combine(Path.GetTempPath(), "disc-guest-captains-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder); Application.persistentDataPath = folder;
        try
        {
            Clean(); RostersPhotosAndGoals(); IndividualStrengthAndSixDiscs(); PersistenceAndGuards(); OriginalMode();
            Console.WriteLine("PASS " + checks + " guest-captain controller, identity, individual-strength and saved-season integration assertions using actual game sources.");
            Console.WriteLine("Injected lifecycle/poses through Unity adapters; native physics, rendering and audio were not executed.");
        }
        finally { Clean(); Directory.Delete(folder, true); }
    }
}
