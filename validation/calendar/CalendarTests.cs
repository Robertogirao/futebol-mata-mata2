using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using FutebolDeDiscos;

internal static class CalendarTests
{
    private static int checks;
    private static string data;
    private static readonly JsonAdapter serializer = new JsonAdapter();

    private sealed class JsonAdapter : ISerializadorTemporadaDiscos
    {
        public string ParaJson(TemporadaDiscos value)
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(TemporadaDiscos)).WriteObject(stream, value);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
        public TemporadaDiscos DeJson(string text)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
                return (TemporadaDiscos)new DataContractJsonSerializer(typeof(TemporadaDiscos)).ReadObject(stream);
        }
    }

    private sealed class FailingSerializer : ISerializadorTemporadaDiscos
    {
        public bool corruptSeed;
        public string ParaJson(TemporadaDiscos value)
        { if (!corruptSeed) throw new IOException("Injected serializer failure"); return serializer.ParaJson(value); }
        public TemporadaDiscos DeJson(string text)
        { var value = serializer.DeJson(text); value.sementeEfetiva++; return value; }
    }

    public static int Main(string[] args)
    {
        data = args[0];
        if (Directory.Exists(data)) Directory.Delete(data, true);
        Directory.CreateDirectory(data);
        try
        {
            FullTurns();
            ArbitrarySchedulesAndCsv();
            InvalidInputsAndCorruption();
            Persistence();
            ImmutableConfiguration();
            ErrorRecoveryAndHistory();
            Console.WriteLine("PASS: " + checks + " assertions; real CSV parser, scheduling and real-file metadata persistence.");
            return 0;
        }
        catch (Exception error)
        { Console.Error.WriteLine(error); return 1; }
    }

    private static void FullTurns()
    {
        for (int count = 2; count <= 32; count++)
        {
            var ids = Enumerable.Range(1, count).Select(i => "Equipe " + i).ToList();
            int turn = CalendarioDiscos.RodadasPorTurno(count);
            var calendar = CalendarioDiscos.Gerar(ids, 2 * turn, 1367);
            CalendarioDiscos.Validar(calendar);
            Equal(calendar.rodadas.Count, 2 * turn, "Requested full-turn round count");
            Equal(calendar.participantes, ids, "Source roster order preserved");
            var pairCounts = new Dictionary<string, int>();
            var byes = new Dictionary<string, int>();
            for (int r = 0; r < calendar.rodadas.Count; r++)
            {
                var round = calendar.rodadas[r];
                Equal(round.numero, r + 1, "Round numbering");
                var occupied = new HashSet<string>();
                foreach (var match in round.jogos)
                {
                    Check(ids.Contains(match.a) && ids.Contains(match.b) && match.a != match.b, "Real distinct opponents");
                    Check(occupied.Add(match.a) && occupied.Add(match.b), "Each team plays once per round");
                    if (r < turn)
                    {
                        string key = string.CompareOrdinal(match.a, match.b) < 0 ? match.a + ":" + match.b : match.b + ":" + match.a;
                        pairCounts[key] = pairCounts.ContainsKey(key) ? pairCounts[key] + 1 : 1;
                    }
                }
                Equal(round.jogos.Count, count / 2, "Games per round");
                if (count % 2 == 1)
                {
                    Check(ids.Contains(round.folga) && !occupied.Contains(round.folga), "One real bye on odd roster");
                    if (r < turn) byes[round.folga] = byes.ContainsKey(round.folga) ? byes[round.folga] + 1 : 1;
                }
                else Equal(round.folga, "", "No bye on even roster");
                if (r >= turn)
                {
                    var first = calendar.rodadas[r - turn];
                    Equal(round.folga, first.folga, "Return-game bye preserved");
                    for (int g = 0; g < round.jogos.Count; g++)
                    {
                        Equal(round.jogos[g].a, first.jogos[g].b, "Return game swaps home");
                        Equal(round.jogos[g].b, first.jogos[g].a, "Return game swaps away");
                    }
                }
            }
            Equal(pairCounts.Count, count * (count - 1) / 2, "All unique pairings covered");
            Check(pairCounts.Values.All(v => v == 1), "No repeated pairing in a turn");
            if (count % 2 == 1) Check(byes.Count == count && byes.Values.All(v => v == 1), "Every team has one bye per turn");
        }
    }

    private static void ArbitrarySchedulesAndCsv()
    {
        var ids = new List<string> { "São Paulo", "Clube, Azul", "Equipe \"Ouro\"", "A;B", "Atlético-MG" };
        foreach (int rounds in new[] { 1, 3, 7, 19, 100 })
        {
            var a = CalendarioDiscos.Gerar(ids, rounds, -29);
            var b = CalendarioDiscos.Gerar(ids, rounds, -29);
            Equal(CalendarioDiscos.ParaCsv(a), CalendarioDiscos.ParaCsv(b), "Same seed deterministic");
            int warnings = 0;
            var parsed = CalendarioCsvParser.Ler(CalendarioDiscos.ParaCsv(a), (line, warning) => warnings++);
            Equal(warnings, 0, "Generated CSV accepted without warnings");
            Equal(parsed.Count, rounds * (ids.Count / 2), "All generated CSV fixtures read");
            int offset = 0;
            foreach (var round in a.rodadas)
                foreach (var game in round.jogos)
                {
                    Equal(parsed[offset].rodada, round.numero, "CSV round exact");
                    Equal(parsed[offset].timeA, game.a, "CSV Unicode/comma/quote/semicolon asset ID exact");
                    Equal(parsed[offset].timeB, game.b, "CSV opponent ID exact");
                    offset++;
                }
        }
        Equal(ids[0], "São Paulo", "Generating never mutates caller roster");
        Equal(CalendarioDiscos.ParaCsv(CalendarioDiscos.Gerar(ids, 7, 1, false)),
            CalendarioDiscos.ParaCsv(CalendarioDiscos.Gerar(ids, 7, 99, false)), "Unshuffled schedule ignores seed");
        Check(CalendarioDiscos.ParaCsv(CalendarioDiscos.Gerar(ids, 7, 1)) !=
            CalendarioDiscos.ParaCsv(CalendarioDiscos.Gerar(ids, 7, 99)), "Sample different seeds produce different brackets");
        Equal(CalendarioDiscos.Gerar(ids, 100, int.MinValue).rodadas.Count, 100, "Minimum signed seed accepted");
        var hundred = CalendarioDiscos.Gerar(ids, 100, 45);
        for (int r = 10; r < hundred.rodadas.Count; r++)
        {
            Equal(hundred.rodadas[r].folga, hundred.rodadas[r % 10].folga, "Third and later turns repeat cycles");
            for (int g = 0; g < hundred.rodadas[r].jogos.Count; g++)
                Equal(hundred.rodadas[r].jogos[g].a, hundred.rodadas[r % 10].jogos[g].a, "Return cycles repeat exact fixtures");
        }
    }

    private static void InvalidInputsAndCorruption()
    {
        foreach (var ids in new[] { new string[0], new[] { "A" }, new[] { "A", "A" }, new[] { "A", "B\nC" },
            new[] { "A", " B" }, new[] { "A", " " }, new[] { "A", (string)null }, Enumerable.Range(0, 33).Select(i => "T" + i).ToArray() })
            Reject(() => CalendarioDiscos.Gerar(ids, 1, 1), "Invalid roster rejected");
        Reject(() => CalendarioDiscos.Gerar(new[] { "A", "B" }, 0, 1), "Zero rounds rejected");
        Reject(() => CalendarioDiscos.Gerar(new[] { "A", "B" }, 101, 1), "More than 100 rounds rejected");
        Reject(() => CalendarioDiscos.RodadasPorTurno(1), "Invalid turn team count rejected");
        var c = CalendarioDiscos.Gerar(new[] { "A", "B", "C" }, 3, 17);
        c.rodadas[0].jogos[0].b = c.rodadas[0].jogos[0].a;
        Reject(() => CalendarioDiscos.Validar(c), "Corrupt fixture rejected");
        c = CalendarioDiscos.Gerar(new[] { "A", "B", "C" }, 3, 17);
        c.rodadas[0].folga = "Other";
        Reject(() => CalendarioDiscos.Validar(c), "Corrupt bye rejected");
        c = CalendarioDiscos.Gerar(new[] { "A", "B", "C" }, 3, 17);
        c.rodadas.RemoveAt(1);
        Reject(() => CalendarioDiscos.Validar(c), "Missing round rejected");
        foreach (var bad in new Action<DadosConfiguracaoDiscos>[] {
            x => x.competicaoId="../other", x => x.temporadaId="bad/path", x => x.formato=2,
            x => x.discosPorTime=0, x => x.discosPorTime=6, x => x.duracaoPartida=float.NaN,
            x => x.duracaoPartida=601, x => x.velocidadeDiscos=float.PositiveInfinity,
            x => x.velocidadeMaximaBola=0, x => x.forcaChute=-1, x => x.tamanhoDisco=0,
            x => x.tamanhoBola=float.NaN, x => x.pausaAposGol=-1, x => x.distribuicao=2,
            x => x.quantidadeTimes=5, x => x.quantidadeRodadas=101 })
        {
            var config = Config(); bad(config);
            Reject(() => RepositorioTemporadaDiscos.ValidarConfiguracao(config), "Invalid metadata settings rejected");
        }
    }

    private static void Persistence()
    {
        var c = Config();
        var repo = Repo("seed-once");
        var first = repo.CarregarOuCriar(c);
        Check(first.sementeEfetiva != 0, "Automatic seed commits a nonzero effective seed");
        var original = File.ReadAllBytes(repo.CaminhoArquivo);
        var second = repo.CarregarOuCriar(c);
        Equal(first.sementeEfetiva, second.sementeEfetiva, "Automatic seed reused on restart");
        Equal(CalendarioDiscos.ParaCsv(first.calendarioLiga), CalendarioDiscos.ParaCsv(second.calendarioLiga), "Calendar reused on restart");
        Check(original.SequenceEqual(File.ReadAllBytes(repo.CaminhoArquivo)), "Loading does not rewrite metadata");
        c.ids[0] = "Caller changed";
        Equal(first.configuracao.ids[0], "São Paulo", "Saved configuration copies caller roster");
        Equal(first.calendarioLiga.participantes[0], "São Paulo", "Saved calendar copies caller roster");
        foreach (int seed in new[] { int.MinValue, -123, 1, int.MaxValue })
        {
            c = Config(); c.semente = seed;
            var seeded = Repo("fixed-" + seed).CarregarOuCriar(c);
            Equal(seeded.sementeEfetiva, seed, "Fixed seed retained exactly");
        }
        c = Config(); c.formato = 1; c.quantidadeTimes = 8; c.permitirFolgas = true;
        var ko = Repo("knockout-byes");
        var season = ko.CarregarOuCriar(c);
        Equal(season.calendarioLiga.quantidadeRodadas, 0, "Knockout stores no league fixtures");
        Equal(ko.CarregarOuCriar(c).sementeEfetiva, season.sementeEfetiva, "Knockout seed reused");
        season.calendarioLiga = null;
        RepositorioTemporadaDiscos.ValidarTemporada(season, c);
        Check(true, "Null and inline empty knockout calendars both supported");
        c = Config(); c.temporadaId = "temporada_02";
        var next = Repo("next-season"); next.CarregarOuCriar(c);
        Check(File.Exists(repo.CaminhoArquivo) && File.Exists(next.CaminhoArquivo), "A new season retains the old one");
    }

    private static void ImmutableConfiguration()
    {
        var repo = Repo("immutable");
        var config = Config(); repo.CarregarOuCriar(config);
        byte[] original = File.ReadAllBytes(repo.CaminhoArquivo);
        foreach (FieldInfo field in typeof(DadosConfiguracaoDiscos).GetFields())
        {
            var changed = Config();
            if (field.Name == "ids") changed.ids.Reverse();
            else if (field.FieldType == typeof(string)) field.SetValue(changed, (string)field.GetValue(changed) + "_changed");
            else if (field.FieldType == typeof(bool)) field.SetValue(changed, !(bool)field.GetValue(changed));
            else if (field.FieldType == typeof(int)) field.SetValue(changed, (int)field.GetValue(changed) + 1);
            else if (field.FieldType == typeof(float)) field.SetValue(changed, (float)field.GetValue(changed) + .25f);
            else throw new Exception("Unhandled config field " + field.Name);
            Reject(() => repo.CarregarOuCriar(changed), "Changing " + field.Name + " must use a new season");
            Check(original.SequenceEqual(File.ReadAllBytes(repo.CaminhoArquivo)), "Mismatch preserves original bytes: " + field.Name);
        }
        var corrupted = serializer.DeJson(File.ReadAllText(repo.CaminhoArquivo));
        corrupted.calendarioLiga = CalendarioDiscos.Gerar(config.ids, 1, corrupted.sementeEfetiva);
        File.WriteAllText(repo.CaminhoArquivo, serializer.ParaJson(corrupted));
        byte[] corruptBytes = File.ReadAllBytes(repo.CaminhoArquivo);
        Reject(() => repo.CarregarOuCriar(config), "Internally valid but mismatched calendar rejected");
        Check(corruptBytes.SequenceEqual(File.ReadAllBytes(repo.CaminhoArquivo)), "Mismatched calendar never overwritten");
        corrupted = serializer.DeJson(Encoding.UTF8.GetString(original));
        corrupted.sementeEfetiva = 0;
        Reject(() => RepositorioTemporadaDiscos.ValidarTemporada(corrupted, config), "Missing automatic effective seed rejected");
        Check(typeof(DadosConfiguracaoDiscos).GetField("nomeCampeonato") == null &&
            typeof(DadosConfiguracaoDiscos).GetField("tempoAbertura") == null &&
            typeof(DadosConfiguracaoDiscos).GetField("fundoCampo") == null,
            "Presentation excluded from immutable competition rules");
    }

    private static void ErrorRecoveryAndHistory()
    {
        var c = Config();
        var history = Repo("old-results");
        Reject(() => history.CarregarOuCriar(c, true), "Missing identity never regenerates over saved match history");
        Check(!Directory.Exists(history.Pasta), "History rejection creates no folder or metadata");
        var invalid = Config(); invalid.ids[0] = invalid.ids[1];
        var invalidRepo = Repo("invalid-new");
        Reject(() => invalidRepo.CarregarOuCriar(invalid), "Invalid new config rejected before write");
        Check(!Directory.Exists(invalidRepo.Pasta), "Invalid config creates no folder or JSON");
        var repo = Repo("corrupt"); Directory.CreateDirectory(repo.Pasta);
        File.WriteAllText(repo.CaminhoArquivo, "{broken");
        Reject(() => repo.CarregarOuCriar(c), "Corrupt JSON refused");
        Equal(File.ReadAllText(repo.CaminhoArquivo), "{broken", "Corrupt JSON preserved");
        File.WriteAllText(repo.CaminhoArquivo, "{}");
        Reject(() => repo.CarregarOuCriar(c), "Missing serialized fields refused");
        Equal(File.ReadAllText(repo.CaminhoArquivo), "{}", "Incomplete metadata preserved");
        foreach (bool corruptSeed in new[] { false, true })
        {
            var failing = new RepositorioTemporadaDiscos(Path.Combine(data, "serializer-" + corruptSeed), new FailingSerializer { corruptSeed = corruptSeed });
            Reject(() => failing.CarregarOuCriar(c), "Serializer failure cannot commit identity");
            Check(!File.Exists(failing.CaminhoArquivo), "Failed serializer leaves no committed JSON");
            Check(Directory.GetFiles(failing.Pasta, "*.tmp").Length == 0, "Failed serializer leaves no temporary files");
        }
        repo = Repo("write-failure"); Directory.CreateDirectory(repo.CaminhoArquivo);
        Reject(() => repo.CarregarOuCriar(c), "Atomic rename failure propagated");
        Check(Directory.GetFiles(repo.Pasta, "*.tmp").Length == 0, "Failed rename cleans temporary files");
        Directory.Delete(repo.CaminhoArquivo);
        repo.CarregarOuCriar(c);
        Check(File.Exists(repo.CaminhoArquivo), "Failed write can be retried safely");
        byte[] original = File.ReadAllBytes(repo.CaminhoArquivo);
        using (var fileLock = new FileStream(Path.Combine(repo.Pasta, "configuracao_discos.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            Reject(() => repo.CarregarOuCriar(c), "Concurrent writer lock rejects second write");
        Check(original.SequenceEqual(File.ReadAllBytes(repo.CaminhoArquivo)), "Lock contention preserves original JSON");
        repo.CarregarOuCriar(c);
        Check(true, "Reload succeeds after lock is released");
    }

    private static DadosConfiguracaoDiscos Config()
    {
        return new DadosConfiguracaoDiscos { quantidadeTimes = 4, quantidadeRodadas = 3,
            ids = new List<string> { "São Paulo", "Clube, Azul", "Equipe \"Ouro\"", "A;B" } };
    }
    private static RepositorioTemporadaDiscos Repo(string name)
    { return new RepositorioTemporadaDiscos(Path.Combine(data, name), serializer); }
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception("Assertion failed: " + message); }
    private static void Equal<T>(T actual, T expected, string message)
    { Check(EqualityComparer<T>.Default.Equals(actual, expected), message + ": " + actual + " != " + expected); }
    private static void Equal<T>(List<T> actual, List<T> expected, string message)
    { Check(actual.SequenceEqual(expected), message); }
    private static void Reject(Action action, string message)
    {
        bool rejected = false;
        try { action(); } catch { rejected = true; }
        Check(rejected, message);
    }
}
