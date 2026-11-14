using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FutebolDeDiscos;
using TMPro;
using UnityEngine;

// Actual controller, draw simulation, Canvas construction and stores. The
// contract drives coroutines/video clocks; native rendering is not executed.
public static class CaptainDrawHarness
{
    private static int checks;
    private static string scenario;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(scenario + ": " + message + " Errors: " + string.Join(" | ", Debug.Errors.ToArray()));
    }
    private static object Call(object value, string method, params object[] args)
    {
        MethodInfo found = value.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (found == null) throw new Exception("Missing method " + method);
        try { return found.Invoke(value, args); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }
    private static T Field<T>(object value, string name)
    {
        return (T)value.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(value);
    }
    private static void Clean()
    {
        foreach (GameObject go in GameObject.All.Where(g => !g.destroyed && g.transform.parent == null).ToArray()) UnityEngine.Object.Destroy(go);
        GameObject.All.Clear(); Scheduler.Reset(); Time.captureDeltaTime = 0f; Time.timeScale = 1f;
        Debug.Errors.Clear(); Debug.Warnings.Clear(); Debug.Messages.Clear();
    }
    private static List<TeamData> Teams(int count)
    {
        return Enumerable.Range(0, count).Select(i =>
        {
            var team = ScriptableObject.CreateInstance<TeamData>();
            team.name = "draw_club_" + i; team.nomeEquipe = "Draft Club " + i; team.sigla = "D" + i;
            team.corPrincipal = i % 2 == 0 ? Color.blue : Color.red;
            team.escudoOuBandeira = new Sprite { name = "Draw club crest " + i };
            for (int p = 0; p < 5; p++) team.elenco.Add(new JogadorDados { nomeJogador = "Football " + i + "-" + p });
            return team;
        }).ToList();
    }
    private static List<CapitaoConvidado> Captains(int count)
    {
        return Enumerable.Range(0, count).Select(i =>
        {
            var captain = ScriptableObject.CreateInstance<CapitaoConvidado>();
            captain.name = "draw_hero_" + i; captain.nomeCapitao = "Draft Hero " + i; captain.overall = 40 + i % 60;
            captain.foto = new Sprite { name = "Draw captain portrait " + i };
            return captain;
        }).ToList();
    }
    private static string Map(IEnumerable<RegistroCapitaoConvidado> records)
    {
        return string.Join("|", records.OrderBy(r => r.timeId, StringComparer.Ordinal).Select(r => r.timeId + "=" + r.capitaoId + ":" + r.overall).ToArray());
    }
    private sealed class Competition
    {
        public readonly GameObject Root;
        public readonly ProjetoFutebolDiscos Config;
        public readonly TournamentManager Manager;
        public readonly UniversalUI Ui;
        public readonly PartidaDiscos Match;
        public readonly List<TeamData> Teams;
        public readonly List<CapitaoConvidado> Captains;
        public Competition(string season, FormatoDiscos mode, List<TeamData> teams = null, List<CapitaoConvidado> captains = null,
            bool show = true, bool replay = false, bool guests = true, bool start = true, int seed = 314159)
        {
            Teams = teams ?? CaptainDrawHarness.Teams(4); Captains = captains ?? CaptainDrawHarness.Captains(Teams.Count + 3);
            Root = new GameObject("Captain draw integration fixture"); Root.SetActive(false);
            Config = Root.AddComponent<ProjetoFutebolDiscos>(); Manager = Root.AddComponent<TournamentManager>();
            Root.AddComponent<LigaCampeonato>(); Root.AddComponent<MataMataCampeonato>();
            Ui = Root.AddComponent<UniversalUI>(); Root.AddComponent<ClassificacaoUI>(); Root.AddComponent<AberturaJogoUI>();
            Root.AddComponent<GoalBannerUI>(); Root.AddComponent<ChaveamentoUI>(); Match = Root.AddComponent<PartidaDiscos>();
            Config.competicaoId = "draw_integration"; Config.temporadaId = season; Config.formato = mode;
            Config.participantes = Teams; Config.quantidadeTimes = Teams.Count; Config.quantidadeRodadas = 3;
            Config.semente = seed; Config.sortearConfrontos = true; Config.usarCapitaesConvidados = guests;
            Config.catalogoCapitaes = Captains; Config.mostrarSorteioCapitaes = show; Config.reexibirSorteioSalvo = replay;
            Config.tempoApresentacaoSorteio = .2f; Config.velocidadeSorteio = 3f;
            Config.intervaloRevelacoesSorteio = .05f; Config.tempoResultadoSorteio = .8f;
            Config.tempoAbertura = Config.tempoFotos = Config.tempoTransicaoFotos = Config.tempoTabela = Config.intervaloEntrePartidas = 0f;
            Config.tema = ScriptableObject.CreateInstance<TemaCompeticao>();
            Config.tema.fonteTexto = Config.tema.fonteTitulo = TMP_Settings.defaultFontAsset;
            Root.SetActive(true);
            if (start) Start();
        }
        public void Start() { Call(Config, "Start"); Call(Manager, "Start"); }
        public SorteioCapitaesUI Draw { get { return Root.GetComponent<SorteioCapitaesUI>(); } }
        public MotorSorteioCapitaes Motor { get { return Field<MotorSorteioCapitaes>(Config, "motorSorteio"); } }
        public void Frame(float step = .1f)
        {
            Time.captureDeltaTime = step; Time.unscaledDeltaTime = step; Time.deltaTime = step * Time.timeScale;
            Call(Config, "Update"); Call(Match, "Update"); Call(Match, "FixedUpdate"); Call(Ui, "Update");
            Scheduler.Tick(step);
        }
        public void Until(Func<bool> condition, int frames = 1500)
        {
            for (int i = 0; i < frames && !condition(); i++) Frame();
            Check(condition(), "expected progress gate reached: " + Config.situacao);
        }
        public int Objects(string prefix)
        {
            return Root.GetComponentsInChildren<Transform>(true).Count(t => t.gameObject.name.StartsWith(prefix, StringComparison.Ordinal));
        }
        public string Store { get { return Path.Combine(Config.PastaDaTemporada, "capitaes_convidados.json"); } }
        public string Metadata { get { return Path.Combine(Config.PastaDaTemporada, "configuracao_discos.json"); } }
        public string Results { get { return Path.Combine(Application.persistentDataPath, Config.formato == FormatoDiscos.Liga ? "Ligas" : "MataMata", Config.competicaoId, Config.temporadaId, Config.formato == FormatoDiscos.Liga ? "temporada.json" : "torneio.json"); } }
        public DadosCapitaesConvidados Read() { return JsonUtility.FromJson<DadosCapitaesConvidados>(File.ReadAllText(Store)); }
    }
    private static void FreshDrawGatesAndPersists()
    {
        foreach (FormatoDiscos mode in new[] { FormatoDiscos.Liga, FormatoDiscos.MataMata })
        {
            scenario = "fresh visible draw " + mode;
            var fixture = new Competition("fresh_" + mode, mode, start: false);
            fixture.Config.capitaesFixos.Add(new VinculoCapitaoConvidado { time = fixture.Teams[0], capitao = fixture.Captains[0] });
            Time.timeScale = 0f;
            fixture.Start();
            fixture.Until(() => fixture.Draw != null && fixture.Draw.Ativo, 10);
            Check(!fixture.Config.Preparado && fixture.Match.Bola == null && fixture.Match.Atletas.Count == 0, "no football actors or prepared competition before marble draw");
            Check(!fixture.Ui.partidaEmAndamento && fixture.Ui.TempoAtual == 0f && !File.Exists(fixture.Results), "draw starts before tournament results and football clock");
            Check(File.Exists(fixture.Metadata) && !File.Exists(fixture.Store), "effective seed persists first; no incomplete assignment file exists");
            Check(fixture.Motor.Resultados.Count == 0 && fixture.Motor.Marbles.Count == fixture.Teams.Count, "all active captain marbles exist without preassigned fresh results");
            Check(fixture.Objects("Captain Marble ") == fixture.Teams.Count && fixture.Objects("Draw Team ") == fixture.Teams.Count, "real Canvas has one marble and one club card for every participant");
            Check(fixture.Root.GetComponentsInChildren<Canvas>(true).Any(c => c.gameObject.name == "CaptainDraft.Canvas" && c.sortingOrder > 2100), "draw covers live HUD, match opening and bracket");
            Vector2 initial = fixture.Motor.Marbles[0].Posicao;
            for (int frame = 0; frame < 10; frame++) fixture.Frame();
            Check(fixture.Motor.TempoSimulado > 0f && Vector2.Distance(initial, fixture.Motor.Marbles[0].Posicao) > 1f, "video time advances the actual obstacle simulation while timeScale is zero");
            Check(!fixture.Ui.partidaEmAndamento && fixture.Ui.TempoAtual == 0f && !File.Exists(fixture.Results), "football remains stopped during moving marbles");
            fixture.Until(() => File.Exists(fixture.Store));
            DadosCapitaesConvidados saved = fixture.Read();
            Check(fixture.Draw.Concluido && fixture.Draw.QuantidadeRevelada == fixture.Teams.Count, "only complete revealed draw is committed");
            Check(fixture.Draw.Ativo && !fixture.Config.Preparado, "result persists while final summary is still shown");
            Check(Map(saved.atribuicoes) == Map(fixture.Motor.Resultados), "saved assignment is exactly the race-produced outcome, not a second random draw");
            Check(saved.atribuicoes.Single(r => r.timeId == fixture.Teams[0].name).capitaoId == fixture.Captains[0].Identificador, "manual captain club reservation is honored in the live draw");
            Check(saved.atribuicoes.Select(r => r.capitaoId).Distinct().Count() == fixture.Teams.Count, "each participant has exactly one unique assigned captain");
            Check(fixture.Motor.Marbles.Sum(m => m.Colisoes) > 0, "marbles actually interact with obstacles");
            fixture.Until(() => fixture.Config.Preparado);
            Check(!fixture.Draw.Ativo, "summary releases the screen before competition presentation");
            Check(fixture.Teams.All(t => fixture.Config.ObterCapitaoConvidado(t).Identificador == saved.atribuicoes.Single(r => r.timeId == t.name).capitaoId), "same revealed assignments become authoritative match representatives");
            fixture.Until(() => fixture.Ui.partidaEmAndamento && fixture.Match.EmAndamento);
            Check(fixture.Match.Atletas.Where(a => a.Capitao).All(a => a.NomeExibicao == fixture.Config.ObterCapitaoConvidado(a.Equipe).nomeCapitao), "first match uses the captain revealed for each actual club");
            Check(Debug.Errors.Count == 0, "fresh draw and first game emit no errors");
            Clean();
        }
    }
    private static void CountsAndCancellation()
    {
        foreach (int count in new[] { 10, 16, 20, 32 })
        {
            scenario = "visible participant count " + count;
            var fixture = new Competition("count_" + count, FormatoDiscos.Liga, Teams(count), Captains(count + 3));
            fixture.Until(() => fixture.Draw != null && fixture.Draw.Ativo, 10);
            Check(fixture.Motor.Marbles.Count == count && fixture.Objects("Captain Marble ") == count, "only chosen active captains are drawn even when catalogue has extras");
            Check(fixture.Objects("Draw Team ") == count && fixture.Motor.Marbles.Select(m => m.Capitao.Identificador).Distinct().Count() == count, "all club cards are present and marbles are unique");
            fixture.Root.SetActive(false);
            Check(!fixture.Draw.Ativo && !fixture.Config.Preparado && !File.Exists(fixture.Store), "disabling during draw hides overlay and keeps assignments uncommitted");
            for (int frame = 0; frame < 160; frame++) Scheduler.Tick(.1f);
            Check(!File.Exists(fixture.Store) && !File.Exists(fixture.Results) && fixture.Match.Bola == null, "cancelled coroutines cannot later commit or start matches");
            Clean();
        }
    }
    private static void InterruptedDrawUsesSavedSeed()
    {
        scenario = "interrupted draw restarts with saved seed";
        List<TeamData> teams = Teams(4); List<CapitaoConvidado> captains = Captains(7);
        var first = new Competition("interrupted", FormatoDiscos.Liga, teams, captains, seed: 0);
        first.Until(() => first.Draw != null && first.Draw.Ativo, 10);
        int effectiveSeed = first.Config.SementeEfetiva;
        string metadata = File.ReadAllText(first.Metadata);
        string initialCaptains = string.Join("|", first.Motor.Marbles.Select(m => m.Capitao.Identificador).ToArray());
        for (int frame = 0; frame < 8; frame++) first.Frame();
        first.Root.SetActive(false);
        Check(!File.Exists(first.Store), "interruption before all arrivals writes no assignment snapshot");
        Clean();
        var next = new Competition("interrupted", FormatoDiscos.Liga, teams, captains, seed: 0);
        next.Until(() => next.Draw != null && next.Draw.Ativo, 10);
        Check(next.Config.SementeEfetiva == effectiveSeed && File.ReadAllText(next.Metadata) == metadata, "Stop/Play retains the exact effective seed and core metadata");
        Check(string.Join("|", next.Motor.Marbles.Select(m => m.Capitao.Identificador).ToArray()) == initialCaptains, "resumed uncommitted draw uses the same selected captains");
        next.Until(() => File.Exists(next.Store));
        Check(Map(next.Read().atribuicoes) == Map(next.Motor.Resultados), "resumed race saves its own full outcome");
        Clean();
    }

    private static void PartialRevealCannotCommit()
    {
        scenario = "partial arrival and reveal stay uncommitted";
        var fixture = new Competition("partial_reveal", FormatoDiscos.Liga, Teams(16), Captains(19));
        fixture.Until(() => fixture.Draw != null && fixture.Draw.QuantidadeRevelada > 0);
        Check(fixture.Draw.QuantidadeRevelada < 16 && fixture.Motor.Resultados.Count > 0, "fixture reaches actual partial finish/reveal state");
        Check(!File.Exists(fixture.Store) && !File.Exists(fixture.Results) && !fixture.Config.Preparado, "one or several revealed pairings cannot commit a partial season or start football");
        fixture.Root.SetActive(false);
        for (int frame = 0; frame < 120; frame++) Scheduler.Tick(.1f);
        Check(!File.Exists(fixture.Store) && !File.Exists(fixture.Results) && !fixture.Draw.Ativo, "cancellation after partial reveal discards the uncommitted draw safely");
        Clean();
    }
    private static void SavedDrawSkipAndReplay()
    {
        foreach (FormatoDiscos mode in new[] { FormatoDiscos.Liga, FormatoDiscos.MataMata })
        {
            scenario = "saved assignment skip and replay " + mode;
            List<TeamData> teams = Teams(4); List<CapitaoConvidado> captains = Captains(7);
            var old = new Competition("replay_" + mode, mode, teams, captains, show: false);
            Check(old.Config.Preparado && File.Exists(old.Store), "display-off keeps legacy immediate saved assignment behavior");
            string original = File.ReadAllText(old.Store); string expected = Map(old.Read().atribuicoes);
            Clean();
            var resumed = new Competition("replay_" + mode, mode, teams, captains);
            Check(resumed.Config.Preparado && (resumed.Draw == null || !resumed.Draw.Ativo), "saved draw is skipped by default instead of rerunning before every round");
            Check(File.ReadAllText(resumed.Store) == original, "skipping leaves snapshot bytes unchanged");
            Clean();
            var replay = new Competition("replay_" + mode, mode, teams, captains, replay: true);
            replay.Until(() => replay.Draw != null && replay.Draw.Ativo, 10);
            Check(!replay.Config.Preparado, "requested saved replay also gates competition start");
            Check(replay.Root.GetComponentsInChildren<TextMeshProUGUI>(true).Any(t => t.text != null && t.text.Contains("SAVED")), "saved playback is clearly labeled rather than claiming a new draw");
            replay.Until(() => replay.Draw.Concluido);
            Check(Map(replay.Motor.Resultados) == expected, "replay reveals exactly the original persisted club-captain map");
            Check(File.ReadAllText(replay.Store) == original, "replaying never rewrites the saved outcome");
            replay.Until(() => replay.Config.Preparado);
            Check(!replay.Draw.Ativo && File.ReadAllText(replay.Store) == original, "summary releases replay with saved associations intact");
            Clean();
        }
    }
    private static void FailureGuardsAndOrdinaryMode()
    {
        scenario = "nested presentation failure aborts safely";
        var brokenUi = new Competition("broken_ui", FormatoDiscos.Liga);
        brokenUi.Until(() => brokenUi.Draw != null && brokenUi.Draw.Ativo, 10);
        typeof(SorteioCapitaesUI).GetField("motor", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(brokenUi.Draw, null);
        brokenUi.Until(() => !string.IsNullOrEmpty(brokenUi.Config.ultimoErro), 10);
        Check(!brokenUi.Config.Preparado && !brokenUi.Draw.Ativo && !File.Exists(brokenUi.Store), "exception inside nested draw iterator uses normal controller failure cleanup");
        Check(!File.Exists(brokenUi.Results) && brokenUi.Match.Bola == null, "nested presentation exception cannot leave a half-started competition");
        Clean();

        scenario = "write conflict aborts before competition";
        var fixture = new Competition("conflict", FormatoDiscos.Liga);
        fixture.Until(() => fixture.Draw != null && fixture.Draw.Ativo, 10);
        const string conflict = "existing corrupted assignment must survive";
        File.WriteAllText(fixture.Store, conflict);
        fixture.Until(() => !string.IsNullOrEmpty(fixture.Config.ultimoErro));
        Check(!fixture.Config.Preparado && !fixture.Manager.enabled && !fixture.Ui.partidaEmAndamento && fixture.Match.Bola == null, "persistence failure cannot start a competition with an uncommitted draw");
        Check(File.ReadAllText(fixture.Store) == conflict && !File.Exists(fixture.Results), "conflicting save bytes and previous results remain untouched");
        Check(!fixture.Draw.Ativo, "failed draw cleans up its overlay");
        for (int frame = 0; frame < 80; frame++) fixture.Frame();
        Check(!fixture.Config.Preparado && !File.Exists(fixture.Results), "failed persistence cannot start a delayed match later");
        Clean();

        scenario = "missing assigned metadata over existing competition";
        List<TeamData> teams = Teams(4); List<CapitaoConvidado> captains = Captains(7);
        fixture = new Competition("missing", FormatoDiscos.Liga, teams, captains, show: false);
        Check(File.Exists(fixture.Results), "fixture has an existing tournament draft/results file");
        string result = File.ReadAllText(fixture.Results);
        File.Delete(fixture.Store); Clean();
        fixture = new Competition("missing", FormatoDiscos.Liga, teams, captains);
        Check(!fixture.Config.Preparado && !string.IsNullOrEmpty(fixture.Config.ultimoErro), "missing old captain snapshot cannot be replaced by a fresh on-screen draw");
        Check(!File.Exists(fixture.Store) && File.ReadAllText(fixture.Results) == result, "missing snapshot guard preserves original competition file");
        Check(fixture.Draw == null || !fixture.Draw.Ativo, "unsafe metadata state never launches the visual draw");
        Clean();

        scenario = "ordinary football scenes do not gain a captain draft";
        fixture = new Competition("ordinary", FormatoDiscos.Liga, guests: false);
        Check(fixture.Config.Preparado && (fixture.Draw == null || !fixture.Draw.Ativo), "old football-only scene ignores the new draw controls");
        Check(!File.Exists(fixture.Store), "ordinary football season does not write guest assignments");
        Clean();
    }

    private static void SummaryCancellationKeepsCompleteResult()
    {
        scenario = "stop during final summary preserves complete outcome";
        List<TeamData> teams = Teams(4); List<CapitaoConvidado> captains = Captains(7);
        var fixture = new Competition("summary_stop", FormatoDiscos.Liga, teams, captains);
        fixture.Until(() => File.Exists(fixture.Store));
        string saved = File.ReadAllText(fixture.Store); string expected = Map(fixture.Read().atribuicoes);
        Check(!fixture.Config.Preparado && fixture.Draw.Ativo && fixture.Draw.Concluido, "fixture stopped at committed summary boundary before competition");
        fixture.Root.SetActive(false);
        for (int i = 0; i < 80; i++) Scheduler.Tick(.1f);
        Check(File.ReadAllText(fixture.Store) == saved && !File.Exists(fixture.Results), "summary cancellation keeps complete assignments without starting results");
        Clean();
        fixture = new Competition("summary_stop", FormatoDiscos.Liga, teams, captains);
        Check(fixture.Config.Preparado && (fixture.Draw == null || !fixture.Draw.Ativo), "next Play recognizes completed draw and skips reroll after summary interruption");
        Check(Map(fixture.Read().atribuicoes) == expected && File.ReadAllText(fixture.Store) == saved, "summary interruption cannot change the already revealed pairing");
        Clean();
    }
    public static void Main()
    {
        string folder = Path.Combine(Path.GetTempPath(), "disc-captain-draw-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder); Application.persistentDataPath = folder;
        try
        {
            Clean(); FreshDrawGatesAndPersists(); CountsAndCancellation(); PartialRevealCannotCommit(); InterruptedDrawUsesSavedSeed();
            SavedDrawSkipAndReplay(); SummaryCancellationKeepsCompleteResult(); FailureGuardsAndOrdinaryMode();
            Console.WriteLine("PASS " + checks + " actual captain-draw controller, Canvas, simulation, cancellation and persistence integration assertions.");
            Console.WriteLine("Custom deterministic draw physics executed; native Unity rendering, input and audio were not executed.");
            string report = "{\n  \"assertions\": " + checks + ",\n  \"actual_game_sources\": true,\n  \"league_and_knockout\": true,\n  \"live_simulation_result_persisted\": true,\n  \"saved_replay_immutable\": true,\n  \"native_unity_executed\": false,\n  \"result\": \"pass\"\n}\n";
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "captain-draw-report.json"), report);
        }
        finally { Clean(); Directory.Delete(folder, true); }
    }
}
