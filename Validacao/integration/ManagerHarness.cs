using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PokemonMarbleRace;
using UnityEngine;
using TMPro;

public static class ManagerHarness
{
    private static int assertions;
    private static readonly string RootFolder = "/workspace/work/pokemon-marble-race/validation/integration/runtime-saves";
    private sealed class Context
    {
        public MarbleRaceManager manager;
        public RaceCompetition Competition { get { return Read<RaceCompetition>(manager, "competition"); } }
        public List<RaceMarble> Marbles { get { return Read<List<RaceMarble>>(manager, "marbles"); } }
        public string State { get { return Read<object>(manager, "state").ToString(); } }
        public string SavePath { get { return Path.Combine(manager.PastaDaTemporada, "championship.json"); } }
    }
    private static T Read<T>(object target, string field)
    { return (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(target); }
    private static void Call(object target, string method)
    {
        MethodInfo m = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (m == null) throw new Exception("Missing lifecycle " + method);
        try { m.Invoke(target, null); }
        catch (TargetInvocationException e) { throw e.InnerException; }
    }
    private static void Check(bool value, string message)
    {
        assertions++;
        if (!value) throw new Exception("FAILED: " + message + "\n" + string.Join("\n", Debug.Errors.ToArray()));
    }
    private static void ResetWorld()
    {
        foreach (GameObject go in GameObject.All.ToArray()) if (!go.destroyed) UnityEngine.Object.Destroy(go);
        GameObject.All.Clear(); Scheduler.Reset(); Debug.Errors.Clear(); Debug.Warnings.Clear();
    }
    private static PokemonDados Pokemon(int id)
    {
        PokemonDados p = ScriptableObject.CreateInstance<PokemonDados>();
        p.numeroPokedex = id; p.nomePokemon = "POKÉMON " + id;
        p.tipoPrimario = (TipoPokemon)((id % 18) + 1);
        p.retrato = new Sprite { bounds = new Bounds(Vector3.zero, new Vector3(1.3f, 1.8f, 0)) };
        p.cor = new Color(.3f + (id % 3) * .2f, .7f, .4f);
        return p;
    }
    private static Context Create(string id, RaceFormat format = RaceFormat.MiniGP, int count = 8, int races = 3, bool pause = false, bool clean = true)
    {
        ResetWorld();
        string root = Path.Combine(RootFolder, id);
        if (clean && Directory.Exists(root)) Directory.Delete(root, true);
        Directory.CreateDirectory(root); Application.persistentDataPath = root; Application.isPlaying = true;
        var go = new GameObject("RaceManager");
        var manager = go.AddComponent<MarbleRaceManager>();
        manager.nomeCampeonato = "Test Marble Championship";
        manager.campeonatoId = "race"; manager.temporadaId = "season";
        manager.formato = format; manager.quantidadeParticipantes = count; manager.quantidadeCorridas = races;
        manager.semente = 0; manager.duracaoAlvo = 10f; manager.duracaoMaxima = 12f;
        manager.duracaoAbertura = .1f; manager.duracaoResultado = 1f; manager.intervaloEntreCorridas = .1f;
        manager.umaCorridaPorPlay = pause;
        manager.spriteBola = new Sprite { bounds = new Bounds(Vector3.zero, new Vector3(4.86f, 4.86f, 0)) };
        manager.tema = ScriptableObject.CreateInstance<TemaCompeticao>();
        manager.tema.fonteTitulo = TMP_Settings.defaultFontAsset;
        manager.tema.fonteTexto = TMP_Settings.defaultFontAsset;
        manager.participantes = Enumerable.Range(1, count).Select(Pokemon).ToList();
        return new Context { manager = manager };
    }
    private static void Start(Context c)
    {
        Call(c.manager, "Start");
        Check(c.manager.Preparada && string.IsNullOrEmpty(c.manager.ultimoErro), "manager prepares from real Start");
        Check(c.Competition != null, "actual competition assigned");
    }
    private static void Frame(Context c, bool move = true, float dt = .02f)
    {
        Scheduler.Tick(dt); Time.fixedDeltaTime = dt;
        Call(c.manager, "Update");
        Call(c.manager, "FixedUpdate");
        if (move) Physics2D.Simulate(dt);
    }
    private static void Until(Context c, Func<bool> condition, float timeout, bool move = true)
    {
        int frames = (int)Math.Ceiling(timeout / .02f);
        for (int i = 0; i < frames && !condition(); i++) Frame(c, move);
        Check(condition(), "requested lifecycle state reached within " + timeout + "s; current=" + c.State + ", error=" + c.manager.ultimoErro);
    }
    private static void Stop(Context c) { UnityEngine.Object.Destroy(c.manager.gameObject); }
    private static void LifecycleAndCompletedReload()
    {
        Context c = Create("complete"); Start(c);
        Check(c.State == "Intro" && c.Marbles.Count == 8 && File.Exists(c.SavePath), "start creates frozen grid and durable season");
        int seed = c.Competition.EffectiveSeed;
        Check(c.Competition.CompletedRaces == 0, "no result or points awarded before race");
        Until(c, () => c.State == "Countdown", 1);
        Check(c.Marbles.All(m => !m.GetComponent<Rigidbody2D>().simulated), "countdown retains frozen physical grid");
        Until(c, () => c.State == "Racing", 4);
        Check(c.Marbles.All(m => m.GetComponent<Rigidbody2D>().simulated), "GO starts real marble motors");
        Until(c, () => c.Competition.CompletedRaces == 1, 14);
        Check(c.State == "Results", "a committed race first shows results");
        RaceSeason afterFirst = JsonUtility.FromJson<RaceSeason>(File.ReadAllText(c.SavePath));
        Check(afterFirst.results.Count == 1 && afterFirst.results[0].finishOrder.Count == 8, "result JSON has all finishing positions");
        RaceHud resultHud = Read<RaceHud>(c.manager, "hud");
        Check(Read<TextMeshProUGUI>(resultHud, "resultsTitle").text == "RACE 1 RESULTS", "saved race first shows actual finish-order results");
        var rows = (System.Collections.IList)Read<object>(resultHud, "resultRows");
        for (int i = 0; i < afterFirst.results[0].finishOrder.Count; i++)
        {
            RaceFinish finish = afterFirst.results[0].finishOrder[i];
            PokemonDados pokemon = c.manager.participantes.Find(p => p.IdEstavel == finish.id);
            Check(Read<TextMeshProUGUI>(rows[i], "rank").text == finish.place.ToString(), "result row displays actual finish position");
            Check(Read<TextMeshProUGUI>(rows[i], "name").text == pokemon.nomePokemon, "result row uses the corresponding racer name");
            Check(Read<TextMeshProUGUI>(rows[i], "detail").text.Contains(finish.finished ? "FINISHED" : "TIME LIMIT"), "result row explains finish or time limit");
        }
        Check(c.Competition.Standings().Sum(s => s.points) == 39, "first8 GP scores awarded exactly once");
        Until(c, () => c.manager.CampeonatoConcluido && c.State == "Complete", 40);
        Check(c.Competition.CompletedRaces == 3 && c.Competition.Standings().All(s => s.races == 3), "three-race GP finishes with accumulated standings");
        Check(c.Competition.Standings().Sum(s => s.points) == 117, "all3 races have one points contribution");
        string saved = File.ReadAllText(c.SavePath); Stop(c);
        Context reload = Create("complete", clean: false); Start(reload);
        Check(reload.manager.CampeonatoConcluido && reload.State == "Complete" && reload.Marbles.Count == 0, "completed season displays final standings without new racers");
        Check(reload.Competition.EffectiveSeed == seed, "seed0 is persisted across repeatedPlay");
        for (int i = 0; i < 100; i++) Frame(reload);
        Check(File.ReadAllText(reload.SavePath) == saved && reload.Competition.CompletedRaces == 3, "completed reload never duplicates points or overwrites race JSON");
        Stop(reload);
        Console.WriteLine("PASS actual Start/countdown/three-raceGP/results/autosave/completed reload");
    }
    private static void PartialAndManualContinue()
    {
        Context c = Create("partial", pause: true); Start(c);
        Until(c, () => c.State == "Paused", 18);
        Check(c.Competition.CompletedRaces == 1 && c.Competition.CurrentRace.number == 2, "oneRacePerPlay pauses after a saved result");
        Check(Read<TextMeshProUGUI>(Read<RaceHud>(c.manager, "hud"), "resultsTitle").text == "CHAMPIONSHIP STANDINGS", "pause presents accumulated standings after individual race results");
        string secondId = c.Competition.CurrentRace.id; int secondSeed = c.Competition.CurrentRace.seed;
        c.manager.ContinuarNoPlay(); Check(c.State == "Intro", "Continue begins next prepared race");
        Until(c, () => c.State == "Racing", 4);
        for (int i = 0; i < 60; i++) Frame(c);
        string saved = File.ReadAllText(c.SavePath); Stop(c);
        Context reload = Create("partial", pause: true, clean: false); Start(reload);
        Check(reload.Competition.CompletedRaces == 1 && reload.Competition.CurrentRace.id == secondId && reload.Competition.CurrentRace.seed == secondSeed,
            "midrace stop resumes only pending race with identical track seed");
        Check(Read<float>(reload.manager, "elapsed") == 0 && reload.State == "Intro", "interrupted race restarts from grid");
        Check(File.ReadAllText(reload.SavePath) == saved, "loading an interrupted race preserves completed result bytes");
        Until(reload, () => reload.State == "Paused", 18);
        Check(reload.Competition.CompletedRaces == 2 && reload.Competition.Standings().Sum(s => s.points) == 78, "resumed race awards points once");
        Stop(reload);
        Console.WriteLine("PASS one-race-perPlay, Continue and interrupted-race restart");
    }
    private static void SaveFailureAndRetry()
    {
        Context c = Create("write-failure", RaceFormat.SingleRace); Start(c);
        Until(c, () => c.State == "Racing", 4);
        string original = File.ReadAllText(c.SavePath), held = c.SavePath + ".held";
        File.Move(c.SavePath, held); Directory.CreateDirectory(c.SavePath);
        Until(c, () => c.State == "Error", 14);
        Check(c.Competition.CompletedRaces == 0 && c.Competition.Standings().Sum(s => s.points) == 0, "failed durable write cannot award live points");
        Check(Read<RaceResult>(c.manager, "pendingResult") != null && c.Marbles.All(m => !m.GetComponent<Rigidbody2D>().simulated), "failed save freezes gameplay and retains pending result");
        Check(File.ReadAllText(held) == original, "previous complete file remains recoverable");
        Directory.Delete(c.SavePath); File.Move(held, c.SavePath);
        c.manager.ContinuarNoPlay();
        Check(c.State == "Results" && c.Competition.CompletedRaces == 1 && Read<RaceResult>(c.manager, "pendingResult") == null, "Continue retries durable result without replaying race");
        string committed = File.ReadAllText(c.SavePath);
        c.manager.ContinuarNoPlay();
        Check(c.Competition.CompletedRaces == 1 && File.ReadAllText(c.SavePath) == committed, "repeatContinue cannot duplicate committed result");
        Stop(c);
        Console.WriteLine("PASS failed actual file write preserves pending result and retry commits once");
    }
    private static void TimeoutAndFormats()
    {
        Context timeout = Create("timeout", RaceFormat.SingleRace); Start(timeout);
        Until(timeout, () => timeout.Competition.CompletedRaces == 1, 18, move: false);
        RaceResult result = timeout.Competition.Snapshot().results[0];
        Check(result.finishOrder.All(f => !f.finished && f.time == 12 && f.progress >= 0 && f.progress <= 1), "time limit produces complete bounded classification when nobody reaches finish");
        Check(result.finishOrder.Select(f => f.id).Distinct().Count() == 8 && result.finishOrder.Select(f => f.place).SequenceEqual(Enumerable.Range(1, 8)), "timeout has every racer once and contiguous places");
        Check(result.finishOrder.Any(f => f.rescues > 0), "motionless marbles receive bounded recovery during timeout scenario");
        Stop(timeout);
        Context duel = Create("duel", RaceFormat.BestOfThree, 2); Start(duel);
        Until(duel, () => duel.manager.CampeonatoConcluido && duel.State == "Complete", 55);
        Check(duel.Competition.CompletedRaces >= 2 && duel.Competition.CompletedRaces <= 3 && duel.Competition.Standings()[0].wins >= 2,
            "bestOfThree stops when one racer has two wins"); Stop(duel);
        Context knockout = Create("knockout", RaceFormat.Knockout, 16); Start(knockout);
        Check(knockout.Marbles.Count == 8, "knockout loads eight-racer heat from16 entrants");
        Until(knockout, () => knockout.manager.CampeonatoConcluido && knockout.State == "Complete", 55);
        RaceSeason season = knockout.Competition.Snapshot();
        Check(season.results.Count == 3 && season.plans.Count == 3 && season.plans[2].participantIds.Count == 8, "two heats qualify eight racers for final");
        var expected = season.results.Take(2).SelectMany(r => r.finishOrder.Take(4).Select(f => f.id));
        Check(new HashSet<string>(expected).SetEquals(season.plans[2].participantIds), "actual final roster equals top4 of each saved heat");
        Stop(knockout);
        Console.WriteLine("PASS bounded timeout, bestOfThree and16-entrant knockout flow");
    }
    private static void InvalidConfigAndChangedSeason()
    {
        Action<Context>[] edits = {
            c => c.manager.participantes[1] = c.manager.participantes[0],
            c => c.manager.participantes[0].retrato = null,
            c => c.manager.spriteBola = null,
            c => c.manager.tamanhoMarble = 1.2f,
            c => c.manager.duracaoAlvo = float.NaN,
            c => c.manager.temporadaId = "../unsafe"
        };
        for (int i = 0; i < edits.Length; i++)
        {
            Context invalid = Create("invalid-" + i); edits[i](invalid); Call(invalid.manager, "Start");
            Check(!invalid.manager.Preparada && invalid.State == "Error" && !string.IsNullOrEmpty(invalid.manager.ultimoErro), "invalid config pauses with reason: " + i);
            Check(!Directory.GetFiles(Application.persistentDataPath, "*.json", SearchOption.AllDirectories).Any(), "invalid config creates no season JSON: " + i);
            Stop(invalid);
        }
        Context old = Create("changed"); Start(old); string saved = File.ReadAllText(old.SavePath); Stop(old);
        Context changed = Create("changed", clean: false); changed.manager.participantes[0] = Pokemon(100); Call(changed.manager, "Start");
        Check(!changed.manager.Preparada && changed.State == "Error" && File.ReadAllText(changed.SavePath) == saved, "saved roster mismatch is rejected without overwriting season"); Stop(changed);
        Context corrupt = Create("changed", clean: false); File.WriteAllText(corrupt.SavePath, "{invalid"); Call(corrupt.manager, "Start");
        Check(!corrupt.manager.Preparada && corrupt.State == "Error" && File.ReadAllText(corrupt.SavePath) == "{invalid", "corrupt primary withoutbackup pauses and preserves bytes"); Stop(corrupt);
        Console.WriteLine("PASS invalid config, changed roster and corrupt JSON preserve prior files");
    }
    private static void BoundaryAndDisableEnable()
    {
        Context maximum = Create("maximum16", RaceFormat.SingleRace, 16);
        maximum.manager.tamanhoMarble = .65f;
        Start(maximum);
        Check(maximum.Marbles.Count == 16, "maximum single-race field creates16 racers");
        RaceCourse course = Read<RaceCourse>(maximum.manager, "course");
        foreach (RaceMarble racer in maximum.Marbles)
        {
            Check(racer.Progress < 5f, "maximum allowed size and field stay inside starting block");
            RacePoint center = course.Sample(racer.Progress);
            Check(Vector2.Distance(racer.Position, new Vector2(center.x, center.y)) + .325f < course.width * .5f,
                "maximum allowed size fits rail clearance");
        }
        Until(maximum, () => maximum.State == "Racing", 4);
        string saved = File.ReadAllText(maximum.SavePath); int seed = maximum.Competition.CurrentRace.seed;
        maximum.manager.enabled = false;
        Check(maximum.State == "Paused" && maximum.Marbles.All(m => !m.GetComponent<Rigidbody2D>().simulated), "disabling manager pauses and freezes all physical runners");
        maximum.manager.enabled = true;
        maximum.manager.ContinuarNoPlay();
        Check(maximum.State == "Intro" && maximum.Competition.CurrentRace.seed == seed && File.ReadAllText(maximum.SavePath) == saved,
            "enable andContinue restart pending course without changing saved seed or results");
        Stop(maximum);
        Context knockout32 = Create("knockout32", RaceFormat.Knockout, 32); Start(knockout32);
        Check(knockout32.Competition.TotalRaces == 7 && knockout32.Marbles.Count == 8, "32-entrant knockout schedules7 races and loads only current8-racer heat");
        Stop(knockout32);
        Console.WriteLine("PASS maximum16 size/grid, disable-enable restart and32-entrant heat boundary");
    }
    public static void Main()
    {
        Directory.CreateDirectory(RootFolder);
        LifecycleAndCompletedReload(); PartialAndManualContinue(); SaveFailureAndRetry(); TimeoutAndFormats(); InvalidConfigAndChangedSeason(); BoundaryAndDisableEnable(); RecorderPresentationTiming(); FrozenObstaclePose();
        Console.WriteLine("PASS " + assertions + " assertions on all real runtime scripts with Unity-contract, JSON and velocity-only physics facade");
    }
    private static void RecorderPresentationTiming()
    {
        Context c = Create("recorder-timing");
        c.manager.duracaoAbertura = 4f;
        Start(c);
        Time.captureDeltaTime = .1f;
        Time.unscaledDeltaTime = .7f;
        for (int i = 0; i < 5; i++) Call(c.manager, "Update");
        Check(c.State == "Intro" && Math.Abs(Read<float>(c.manager, "stateTimer") - 3.5f) < .0001f,
            "presentation uses captured video time rather than variable wall-clock frame cost");
        Time.captureDeltaTime = 0f;
        Time.unscaledDeltaTime = .2f;
        Call(c.manager, "Update");
        Check(Math.Abs(Read<float>(c.manager, "stateTimer") - 3.3f) < .0001f,
            "presentation falls back to unscaled frame time when Recorder capture is off");
        Stop(c);
        Console.WriteLine("PASS capture-aware presentation timing and unscaled fallback");
    }
    private static void FrozenObstaclePose()
    {
        Context c = Create("frozen-obstacles");
        c.manager.semente = 2468;
        Start(c);
        RaceTrackView world = Read<RaceTrackView>(c.manager, "world");
        var motion = world.GetComponentsInChildren<RaceObstacleMotion>(true).FirstOrDefault(m => Read<Rigidbody2D>(m, "body") != null);
        Check(motion != null, "fixture has a kinematic course obstacle");
        Rigidbody2D body = Read<Rigidbody2D>(motion, "body");
        RaceObstacle definition = Read<RaceObstacle>(motion, "definition");
        body.position = new Vector2(50f, 50f); body.rotation = 10000f;
        Call(c.manager, "FixedUpdate");
        Check(definition.kind == RaceObstacleKind.Spinner ? Math.Abs(body.rotation) < 1000f : body.position.magnitude < 20f,
            "intro physics ticks reapply the frozen obstacle pose before GO");
        Stop(c);
        Console.WriteLine("PASS obstacle pose stays driven while the race clock is stopped");
    }
}
