using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using PokemonMarbleRace;

public sealed class JsonAdapter : IRaceSerializer
{
    public bool failWrite;
    public bool emptyWrite;
    public string Serialize(RaceSeason data)
    {
        if (failWrite) throw new IOException("Injected serializer write failure");
        if (emptyWrite) return "{}";
        using (var stream = new MemoryStream())
        {
            new DataContractJsonSerializer(typeof(RaceSeason)).WriteObject(stream, data);
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
    public RaceSeason Deserialize(string json)
    {
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            return (RaceSeason)new DataContractJsonSerializer(typeof(RaceSeason)).ReadObject(stream);
    }
}

public static class Harness
{
    private static int assertions;
    private static readonly JsonAdapter Json = new JsonAdapter();
    private static string temp;

    private static void Check(bool value, string label)
    {
        assertions++;
        if (!value) throw new Exception("FAIL " + label);
    }
    private static void Throws(Action action, string label)
    {
        bool thrown = false;
        try { action(); } catch { thrown = true; }
        Check(thrown, label);
    }
    private static RaceConfig Config(int count = 8, int races = 3, RaceFormat format = RaceFormat.MiniGP, int seed = 123)
    {
        return new RaceConfig { competitionId = "marble_test", seasonId = "season_01", title = "Marble GP",
            participantIds = Enumerable.Range(1, count).Select(n => "pokemon_" + n.ToString("D4")).ToList(),
            format = format, raceCount = races, configuredSeed = seed };
    }
    private static RaceResult Result(RacePlan plan, int rotate = 0)
    {
        var ids = plan.participantIds.Skip(rotate % plan.participantIds.Count)
            .Concat(plan.participantIds.Take(rotate % plan.participantIds.Count)).ToList();
        return new RaceResult { raceId = plan.id, finishOrder = ids.Select((id, index) =>
            new RaceFinish { id = id, place = index + 1, time = 35f + index * .5f, progress = 1f, finished = true }).ToList() };
    }
    private static RaceCompetition Reload(RaceCompetition model)
    {
        return RaceCompetition.Restore(Json.Deserialize(Json.Serialize(model.Snapshot())), model.Config);
    }
    private static void TestSchedules()
    {
        foreach (int count in new[] { 2, 8, 16 })
        foreach (int races in new[] { 1, 3, 30 })
        foreach (int seed in new[] { 1, -1, int.MinValue, int.MaxValue })
        {
            RaceConfig config = Config(count, races, RaceFormat.MiniGP, seed);
            RaceCompetition model = RaceCompetition.Create(config);
            string[] original = config.participantIds.ToArray();
            Check(model.TotalRaces == races && model.CompletedRaces == 0, "GP schedule exact count");
            Check(model.Snapshot().plans.Select(p => p.seed).Distinct().Count() == races, "each race gets distinct saved seed");
            Check(model.Snapshot().plans.All(p => p.participantIds.Count == count &&
                new HashSet<string>(p.participantIds).SetEquals(original)), "GP roster complete every race");
            Check(config.participantIds.SequenceEqual(original), "input roster preserved");
            string firstSchedule = Json.Serialize(model.Snapshot());
            Check(firstSchedule == Json.Serialize(RaceCompetition.Create(config).Snapshot()), "fixed seed schedule deterministic");
            int expectedPoints = 0;
            for (int i = 0; i < races; i++)
            {
                RacePlan plan = model.CurrentRace;
                Check(plan.number == i + 1, "pending race advances consecutively");
                RaceResult result = Result(plan, i % count);
                Check(model.CompleteRace(result), "new result committed");
                string saved = Json.Serialize(model.Snapshot());
                Check(!model.CompleteRace(result) && saved == Json.Serialize(model.Snapshot()), "same result is idempotent");
                expectedPoints += Enumerable.Range(1, count).Sum(RaceCompetition.PointsForPlace);
                Check(model.Standings().Sum(row => row.points) == expectedPoints, "points cannot duplicate on retry");
                model = Reload(model);
                Check(model.CompletedRaces == i + 1, "JSON resume retains completed races");
            }
            Check(model.IsComplete && model.CurrentRace == null, "complete GP cannot replay");
            Check(model.Standings().All(row => row.races == races), "all participants race exact GP count");
            Check(model.ToStandingsCsv().Split('\n').Length == count + 2, "standings CSV contains complete roster");
        }
        RaceCompetition single = RaceCompetition.Create(Config(8, 3, RaceFormat.SingleRace));
        Check(single.TotalRaces == 1, "single race ignores GP race count");
        single.CompleteRace(Result(single.CurrentRace));
        Check(single.IsComplete, "single race concludes");

        RaceCompetition automatic = RaceCompetition.Create(Config(seed: 0));
        int effective = automatic.EffectiveSeed;
        automatic = Reload(automatic);
        Check(automatic.EffectiveSeed == effective && automatic.Config.configuredSeed == 0,
            "automatic seed persists and does not redraw on reload");

        RaceCompetition copies = RaceCompetition.Create(Config());
        copies.CurrentRace.participantIds.Clear();
        copies.Config.participantIds.Clear();
        copies.Snapshot().plans[0].participantIds.Clear();
        Check(copies.CurrentRace.participantIds.Count == 8, "public DTO copies cannot mutate live schedule");
        Console.WriteLine("PASS GP/single schedules, deterministic saved seeds, real JSON resumes and idempotent scoring");
    }

    private static void TestDuel()
    {
        foreach (bool thirdRace in new[] { false, true })
        {
            RaceCompetition duel = RaceCompetition.Create(Config(2, 3, RaceFormat.BestOfThree));
            string winner = duel.Config.participantIds[0];
            RaceResult first = Result(duel.CurrentRace);
            foreach (RaceFinish f in first.finishOrder) f.place = f.id == winner ? 1 : 2;
            duel.CompleteRace(first);
            RaceResult second = Result(duel.CurrentRace);
            foreach (RaceFinish f in second.finishOrder) f.place = (f.id == winner) == !thirdRace ? 1 : 2;
            duel.CompleteRace(second);
            Check(duel.IsComplete == !thirdRace, "best of three ends after second win, not two arbitrary races");
            if (thirdRace)
            {
                RaceResult final = Result(duel.CurrentRace);
                foreach (RaceFinish f in final.finishOrder) f.place = f.id == winner ? 1 : 2;
                duel.CompleteRace(final);
            }
            duel = Reload(duel);
            Check(duel.IsComplete && duel.Standings()[0].id == winner && duel.Standings()[0].points == 2,
                "duel winner persists with two wins");
            Check(duel.CompletedRaces == (thirdRace ? 3 : 2), "unused third race never runs after two straight wins");
        }
        Console.WriteLine("PASS Best of Three early finish, deciding race and restored winner");
    }

    private static void TestKnockout()
    {
        for (int count = 2; count <= 32; count++)
        {
            RaceCompetition model = RaceCompetition.Create(Config(count, 3, RaceFormat.Knockout));
            int total = model.TotalRaces;
            var eliminated = new HashSet<string>();
            string finalWinner = null;
            while (!model.IsComplete)
            {
                RacePlan plan = model.CurrentRace;
                Check(plan.participantIds.Count >= 2 && plan.participantIds.Count <= 8, "balanced knockout heat size 2..8");
                Check(plan.participantIds.All(id => !eliminated.Contains(id)), "eliminated participant never reappears");
                RaceResult result = Result(plan, plan.number % plan.participantIds.Count);
                finalWinner = result.finishOrder[0].id;
                int sameStage = model.Snapshot().plans.Count(p => p.stage == plan.stage);
                if (sameStage > 1)
                    foreach (RaceFinish f in result.finishOrder.Skip(plan.participantIds.Count / 2)) eliminated.Add(f.id);
                model.CompleteRace(result);
                model = Reload(model);
            }
            Check(model.CompletedRaces == total, "knockout fills every scheduled heat");
            Check(model.Standings()[0].id == finalWinner, "final winner leads championship even if qualifier scored more");
            Check(model.Standings().Count == count && model.Standings().Select(r => r.id).Distinct().Count() == count,
                "knockout standings retain all participants once");
        }
        RaceCompetition sixteen = RaceCompetition.Create(Config(16, 3, RaceFormat.Knockout));
        Check(sixteen.TotalRaces == 3, "sixteen participants make two heats and one final");
        RaceCompetition thirtyTwo = RaceCompetition.Create(Config(32, 3, RaceFormat.Knockout));
        Check(thirtyTwo.TotalRaces == 7, "thirty two participants make four heats, two semifinals and final");
        Console.WriteLine("PASS knockout rosters2..32, qualifiers, complete saved bracket and final champion ranking");
    }

    private static void TestValidation()
    {
        Action<RaceConfig>[] invalid = {
            c => c.participantIds[1] = c.participantIds[0], c => c.participantIds[0] = "../escape",
            c => c.competitionId = "../bad", c => c.seasonId = "bad/season", c => c.title = " ",
            c => c.raceCount = 0, c => c.raceCount = 31, c => c.trackVersion = 2,
            c => c.maxSeconds = 4, c => c.targetSeconds = float.NaN, c => c.marbleSize = float.PositiveInfinity,
            c => c.format = (RaceFormat)99, c => c.format = RaceFormat.BestOfThree,
            c => c.participantIds.AddRange(Enumerable.Range(20, 20).Select(n => "p_" + n))
        };
        foreach (var change in invalid)
        {
            RaceConfig config = Config(); change(config);
            Throws(() => RaceCompetition.Create(config), "invalid configuration rejected");
        }
        Throws(() => RaceCompetition.Create(Config(33, 3, RaceFormat.Knockout)), "knockout max32 enforced");
        RaceCompetition model = RaceCompetition.Create(Config());
        string baseline = Json.Serialize(model.Snapshot());
        Action<RaceResult>[] badResults = {
            r => r.raceId = "race_0002", r => r.finishOrder.RemoveAt(0),
            r => r.finishOrder[1].id = r.finishOrder[0].id, r => r.finishOrder[1].place = 1,
            r => r.finishOrder[0].time = float.NaN, r => r.finishOrder[0].progress = 2,
            r => r.finishOrder[0].finished = true, r => r.finishOrder[0].rescues = -1
        };
        for (int i = 0; i < badResults.Length; i++)
        {
            RaceResult result = Result(model.CurrentRace);
            if (i == 6) result.finishOrder[0].progress = .5f;
            badResults[i](result);
            Throws(() => model.CompleteRace(result), "invalid race result rejected");
            Check(Json.Serialize(model.Snapshot()) == baseline, "invalid result cannot mutate season");
        }
        Action<RaceSeason>[] tamper = {
            s => s.version = 2, s => s.effectiveSeed++, s => s.plans[0].seed++,
            s => s.plans[0].participantIds.Reverse(), s => s.plans.RemoveAt(1)
        };
        foreach (var change in tamper)
        {
            RaceSeason altered = model.Snapshot(); change(altered);
            Throws(() => RaceCompetition.Restore(altered, model.Config), "tampered saved schedule rejected");
        }
        Action<RaceConfig>[] changes = {
            c => c.participantIds.Reverse(), c => c.configuredSeed++, c => c.raceCount++,
            c => c.marbleSize = .65f, c => c.targetSeconds = 44, c => c.typePowers = false
        };
        foreach (var change in changes)
        {
            RaceConfig config = model.Config; change(config);
            bool mismatch = false;
            try { RaceCompetition.Restore(model.Snapshot(), config); } catch (RaceConfigurationException) { mismatch = true; }
            Check(mismatch, "changed gameplay configuration has a distinct mismatch error");
        }
        RaceConfig cosmetic = model.Config; cosmetic.title = "New English title";
        Check(RaceCompetition.Restore(model.Snapshot(), cosmetic).Config.title == cosmetic.title,
            "cosmetic title can change while retaining season");
        RaceResult good = Result(model.CurrentRace); model.CompleteRace(good);
        good.finishOrder[0].time++;
        Throws(() => model.CompleteRace(good), "completed result cannot be changed on retry");
        Console.WriteLine("PASS invalid configuration, changed seasons, tampered schedules and invalid results preserve domain state");
    }

    private static void TestStorage()
    {
        string folder = Path.Combine(temp, "atomic");
        var adapter = new JsonAdapter();
        var store = new RaceStorage(folder, adapter);
        RaceConfig config = Config(seed: 0);
        RaceCompetition model = store.LoadOrCreate(config);
        string initial = File.ReadAllText(store.SavePath);
        Check(initial.TrimStart().StartsWith("{"), "actual JSON written to disk");
        Check(store.LoadOrCreate(config).EffectiveSeed == model.EffectiveSeed, "first seed persists across storage reload");
        RaceResult result = Result(model.CurrentRace);
        adapter.failWrite = true;
        Throws(() => store.SaveResult(model, result), "serializer write failure propagated");
        Check(model.CompletedRaces == 0 && File.ReadAllText(store.SavePath) == initial,
            "serializer failure cannot advance live model or replace save");
        adapter.failWrite = false;
        adapter.emptyWrite = true;
        Throws(() => store.SaveResult(model, result), "invalid serializer roundtrip rejected before file mutation");
        Check(model.CompletedRaces == 0 && File.ReadAllText(store.SavePath) == initial,
            "invalid serializer keeps model and save intact");
        adapter.emptyWrite = false;

        Directory.CreateDirectory(store.BackupPath);
        Throws(() => store.SaveResult(model, result), "real filesystem backup destination failure propagated");
        Check(model.CompletedRaces == 0 && File.ReadAllText(store.SavePath) == initial,
            "filesystem failure preserves live state and previous bytes");
        Check(Directory.GetFiles(folder, "*.tmp").Length == 0, "failed write cleans its temporary file");
        Directory.Delete(store.BackupPath);
        Check(store.SaveResult(model, result), "retry saves once after filesystem repaired");
        string after = File.ReadAllText(store.SavePath);
        Check(model.CompletedRaces == 1 && !store.SaveResult(model, result) && File.ReadAllText(store.SavePath) == after,
            "successful retry is idempotent and does not rewrite save");
        Check(File.ReadAllText(store.BackupPath) == initial, "backup holds prior durable state");
        store.ExportStandings(model);
        Check(File.ReadAllText(Path.Combine(folder, "standings.csv")).StartsWith("POS,PARTICIPANT,PTS"),
            "standings CSV exported in English with stable IDs");

        RaceCompetition stale = store.LoadOrCreate(config);
        store.SaveResult(model, Result(model.CurrentRace));
        string latest = File.ReadAllText(store.SavePath);
        Throws(() => store.SaveResult(stale, Result(stale.CurrentRace)), "stale writer rejected rather than overwriting another session");
        Check(File.ReadAllText(store.SavePath) == latest && stale.CompletedRaces == 1,
            "stale writer preserves stored results and its live model");
        using (var fileLock = new FileStream(Path.Combine(folder, "championship.lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Throws(() => store.LoadOrCreate(config), "concurrent exclusive file lock respected");

        string backup = File.ReadAllText(store.BackupPath);
        File.WriteAllText(store.SavePath, "{broken");
        var recovering = new RaceStorage(folder, Json);
        RaceCompetition recovered = recovering.LoadOrCreate(config);
        Check(recovered.CompletedRaces == 1 && recovering.RecoveredFromBackup,
            "corrupt main restores last valid backup and reports possible replay");
        Check(File.ReadAllText(store.BackupPath) == backup && Directory.GetFiles(folder, "championship.corrupt.*.json").Length == 1,
            "recovery preserves backup and exact corrupt file for review");
        Check(File.ReadAllText(Directory.GetFiles(folder, "championship.corrupt.*.json")[0]) == "{broken",
            "corrupt bytes retained verbatim");
        File.Delete(store.SavePath);
        Check(new RaceStorage(folder, Json).LoadOrCreate(config).CompletedRaces == 1,
            "missing main recovers backup without drawing new seed");

        RaceConfig changed = config; changed.raceCount = 4;
        string savedBytes = File.ReadAllText(store.SavePath);
        Throws(() => store.LoadOrCreate(changed), "configuration mismatch cannot silently select older backup");
        Check(File.ReadAllText(store.SavePath) == savedBytes, "configuration mismatch does not rewrite save");
        File.WriteAllText(store.SavePath, "{broken main");
        File.WriteAllText(store.BackupPath, "{broken backup");
        changed.raceCount = 3;
        Throws(() => store.LoadOrCreate(changed), "two corrupt files prevent automatic reset");
        Check(File.ReadAllText(store.SavePath) == "{broken main" && File.ReadAllText(store.BackupPath) == "{broken backup",
            "two corrupt files retained without replacement");

        string invalidFolder = Path.Combine(temp, "invalid");
        RaceConfig invalidConfig = Config(); invalidConfig.participantIds.Clear();
        Throws(() => new RaceStorage(invalidFolder, Json).LoadOrCreate(invalidConfig), "invalid setup fails before any storage mutation");
        Check(!Directory.Exists(invalidFolder), "invalid setup does not create save folder");
        Console.WriteLine("PASS actual JSON disk writes, atomic failures/retries, concurrency, backup recovery and corrupt-file preservation");
    }

    public static void Main()
    {
        temp = Path.Combine(Path.GetTempPath(), "pokemon-race-domain-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            TestSchedules(); TestDuel(); TestKnockout(); TestValidation(); TestStorage();
            Console.WriteLine("PASS " + assertions + " assertions on real RaceCompetition and RaceStorage sources, without Unity stubs");
        }
        finally { Directory.Delete(temp, true); }
    }
}
