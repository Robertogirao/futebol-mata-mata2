using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using FutebolDeDiscos;
using UnityEngine;

// The production repository uses the supplied serializer. These tests use XML
// to exercise real filesystem identity/atomicity independently of native Unity.
public static class GuestCaptainStoreHarness
{
    private static int checks;
    private static string root;
    private static readonly XmlCodec codec = new XmlCodec();
    private sealed class XmlCodec : ISerializadorCapitaesConvidados
    {
        private readonly XmlSerializer serializer = new XmlSerializer(typeof(DadosCapitaesConvidados));
        public string ParaJson(DadosCapitaesConvidados dados) { using (var writer = new StringWriter()) { serializer.Serialize(writer, dados); return writer.ToString(); } }
        public DadosCapitaesConvidados DeJson(string json) { using (var reader = new StringReader(json)) return (DadosCapitaesConvidados)serializer.Deserialize(reader); }
    }
    private sealed class ChangedRoundTrip : ISerializadorCapitaesConvidados
    {
        public string ParaJson(DadosCapitaesConvidados dados) { return codec.ParaJson(dados); }
        public DadosCapitaesConvidados DeJson(string json)
        {
            var result = codec.DeJson(json);
            var swap = result.atribuicoes[0]; result.atribuicoes[0] = result.atribuicoes[1]; result.atribuicoes[1] = swap;
            string team = result.atribuicoes[0].timeId;
            result.atribuicoes[0].timeId = result.atribuicoes[1].timeId; result.atribuicoes[1].timeId = team;
            return result;
        }
    }
    private static void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
    private static void Reject(Action action, string message)
    {
        bool rejected = false;
        try { action(); } catch (InvalidDataException) { rejected = true; } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, message);
    }
    private static List<string> Teams(int count) { return Enumerable.Range(1, count).Select(i => "Team " + i.ToString("00")).ToList(); }
    private static List<CapitaoConvidado> Captains(int count)
    {
        return Enumerable.Range(1, count).Select(i => new CapitaoConvidado
        { name = "Captain_" + i.ToString("00"), nomeCapitao = "Character " + i, overall = 20 + i, foto = new Sprite { name = "Portrait " + i } }).ToList();
    }
    private static VinculoCapitaoConvidado Manual(string team, CapitaoConvidado captain)
    { return new VinculoCapitaoConvidado { time = new TeamData { name = team }, capitao = captain }; }
    private static string Signature(DadosCapitaesConvidados value)
    { return string.Join("|", value.atribuicoes.Select(v => v.timeId + ":" + v.capitaoId + ":" + v.overall).ToArray()); }
    private static RepositorioCapitaesConvidados Repository(string folder, ISerializadorCapitaesConvidados serializer = null)
    { return new RepositorioCapitaesConvidados(Path.Combine(root, folder), serializer ?? codec); }
    private static List<RegistroCapitaoConvidado> ArrivalResults(List<string> teams, List<CapitaoConvidado> captains)
    {
        return teams.Select((team, i) => new RegistroCapitaoConvidado
        {
            timeId = team, capitaoId = captains[i].Identificador,
            nomeCapitao = captains[i].nomeCapitao, overall = captains[i].overall
        }).Reverse().ToList();
    }
    private static List<RegistroCapitaoConvidado> CopyResults(IList<RegistroCapitaoConvidado> results)
    {
        return results.Select(v => v == null ? null : new RegistroCapitaoConvidado
        { timeId = v.timeId, capitaoId = v.capitaoId, nomeCapitao = v.nomeCapitao, overall = v.overall }).ToList();
    }

    private static void AssetIdentity()
    {
        var captain = Captains(1)[0]; captain.nomeCapitao = "  Goku  ";
        JogadorDados first = captain.CriarJogador(), second = captain.CriarJogador();
        Check(first.nomeJogador == "Goku" && first.foto512x512 == captain.foto, "guest identity maps to existing portrait/player data");
        Check(!ReferenceEquals(first, second) && captain.nomeCapitao == "  Goku  ", "representatives are independent data without modifying the captain asset");
        Check(captain.Identificador == "Captain_01", "asset file identity is independent from the display name");
        first.nomeJogador = "Changed at runtime";
        Check(second.nomeJogador == "Goku" && captain.overall == 21, "runtime player mutation does not affect another representative or source rating");
    }

    private static void SizesSeedsAndManualAssignments()
    {
        var signatures = new HashSet<string>();
        for (int count = 2; count <= 32; count++)
        {
            var teams = Teams(count); var captains = Captains(count + 3);
            var manual = new List<VinculoCapitaoConvidado> { Manual(teams[0], captains.Last()) };
            var repository = Repository("size_" + count);
            var initial = repository.CarregarOuCriar(teams, captains, manual, 921);
            Check(initial.atribuicoes.Count == count && initial.atribuicoes.Select(v => v.capitaoId).Distinct().Count() == count, "one unique captain per team for size " + count);
            Check(initial.atribuicoes[0].capitaoId == captains.Last().Identificador, "manual captain remains reserved for size " + count);
            Check(initial.times.SequenceEqual(teams) && initial.semente == 921 && initial.capitaoExtra, "season identity and extra-captain rule persist for size " + count);
            foreach (var assigned in initial.atribuicoes)
            {
                var source = captains.Single(c => c.Identificador == assigned.capitaoId);
                Check(assigned.overall == source.overall && assigned.nomeCapitao == source.nomeCapitao, "individual snapshot belongs to assigned asset");
            }
            byte[] before = File.ReadAllBytes(repository.CaminhoArquivo);
            captains.Reverse();
            var resumed = repository.CarregarOuCriar(teams, captains, manual, 921, true);
            Check(Signature(initial) == Signature(resumed) && File.ReadAllBytes(repository.CaminhoArquivo).SequenceEqual(before), "resume preserves exact assignment and file bytes despite catalog order " + count);
            var reorderedInitial = Repository("order_" + count).CarregarOuCriar(teams, captains, manual, 921);
            Check(Signature(initial) == Signature(reorderedInitial), "catalog order does not affect seeded assignment " + count);
            var next = Repository("next_" + count).CarregarOuCriar(teams, captains, manual, 922);
            if (Signature(initial) != Signature(next)) signatures.Add(count.ToString());
            Check(!Directory.GetFiles(repository.Pasta, "*.tmp").Any(), "successful write leaves no temporary file");
        }
        Check(signatures.Count >= 28, "different seeds create broadly different team/character pairings");
        foreach (int seed in new[] { 0, -731, int.MinValue, int.MaxValue })
        {
            var teams = Teams(4); var captains = Captains(6);
            var a = Repository("seed_a_" + seed).CarregarOuCriar(teams, captains, null, seed, false, false);
            var b = Repository("seed_b_" + seed).CarregarOuCriar(teams, captains, null, seed, false, false);
            Check(Signature(a) == Signature(b) && !a.capitaoExtra, "full integer seed domain is deterministic and preserves replacement mode");
        }
        {
            var teams = Teams(4); var captains = Captains(4);
            var fixedAll = teams.Select((id, index) => Manual(id, captains[3 - index])).ToList();
            var repository = Repository("all_manual");
            var assigned = repository.CarregarOuCriar(teams, captains, fixedAll, 83);
            Check(assigned.atribuicoes.All(v => fixedAll.Any(m => m.time.name == v.timeId && m.capitao.Identificador == v.capitaoId)), "all teams can be manually assigned without an unused random pool");
            fixedAll.Reverse();
            Check(Signature(repository.CarregarOuCriar(teams, captains, fixedAll, 83, true)) == Signature(assigned), "manual list order is cosmetic on resume");
        }
    }

    private static void InvalidConfiguration()
    {
        var teams = Teams(2); var captains = Captains(5);
        Reject(() => RepositorioCapitaesConvidados.ValidarConfiguracao(null, captains, null), "missing teams rejected");
        Reject(() => RepositorioCapitaesConvidados.ValidarConfiguracao(new[] { "Only" }, captains, null), "too few teams rejected");
        Reject(() => RepositorioCapitaesConvidados.ValidarConfiguracao(new[] { "Same", "Same" }, captains, null), "duplicate teams rejected");
        Reject(() => RepositorioCapitaesConvidados.ValidarConfiguracao(new[] { "A", " B" }, captains, null), "ambiguous team identity rejected");
        Reject(() => RepositorioCapitaesConvidados.ValidarConfiguracao(teams, null, null), "missing catalog rejected");
        Reject(() => RepositorioCapitaesConvidados.ValidarConfiguracao(teams, captains.Take(1).ToList(), null), "insufficient unique captains rejected");
        Reject(() => RepositorioCapitaesConvidados.ValidarConfiguracao(teams, new[] { captains[0], captains[0] }, null), "duplicate asset identity rejected");
        Reject(() => RepositorioCapitaesConvidados.ValidarConfiguracao(teams, new[] { captains[0], null }, null), "empty catalog slot rejected");
        foreach (int rating in new[] { 0, -1, 101 })
        {
            captains[0].overall = rating;
            Reject(() => RepositorioCapitaesConvidados.ValidarConfiguracao(teams, captains, null), "out-of-range individual overall rejected");
        }
        captains[0].overall = 21; string name = captains[0].nomeCapitao;
        captains[0].nomeCapitao = " ";
        Reject(() => RepositorioCapitaesConvidados.ValidarConfiguracao(teams, captains, null), "empty display identity rejected");
        captains[0].nomeCapitao = name;
        foreach (var invalid in new[] {
            new List<VinculoCapitaoConvidado> { null },
            new List<VinculoCapitaoConvidado> { new VinculoCapitaoConvidado() },
            new List<VinculoCapitaoConvidado> { Manual("Other team",captains[0]) },
            new List<VinculoCapitaoConvidado> { Manual(teams[0],Captains(6).Last()) },
            new List<VinculoCapitaoConvidado> { Manual(teams[0],captains[0]),Manual(teams[0],captains[1]) },
            new List<VinculoCapitaoConvidado> { Manual(teams[0],captains[0]),Manual(teams[1],captains[0]) }
        }) Reject(() => RepositorioCapitaesConvidados.ValidarConfiguracao(teams, captains, invalid), "invalid manual configuration rejected");
    }

    private static void PreservationAndCorruption()
    {
        var teams = Teams(3); var captains = Captains(6);
        var manual = new List<VinculoCapitaoConvidado> { Manual(teams[0], captains[0]) };
        var repository = Repository("preservation");
        var initial = repository.CarregarOuCriar(teams, captains, manual, 13);
        byte[] original = File.ReadAllBytes(repository.CaminhoArquivo);
        string unrelated = Path.Combine(repository.Pasta, "configuracao_discos.json"); File.WriteAllText(unrelated, "original season metadata");
        string results = Path.Combine(repository.Pasta, "results.json"); File.WriteAllText(results, "durable results");
        var assigned = captains.Single(c => c.Identificador == initial.atribuicoes[1].capitaoId);
        int overall = assigned.overall; assigned.overall++;
        Reject(() => repository.CarregarOuCriar(teams, captains, manual, 13, true), "changing assigned overall cannot rewrite a running season");
        assigned.overall = overall;
        var missing = captains.Where(c => !ReferenceEquals(c, assigned)).ToList();
        Reject(() => repository.CarregarOuCriar(teams, missing, manual, 13, true), "missing assigned asset does not reroll captain");
        Reject(() => repository.CarregarOuCriar(teams, captains, null, 13, true), "removing manual constraints cannot rewrite season");
        Reject(() => repository.CarregarOuCriar(teams, captains, manual, 14, true), "different seed cannot rewrite season");
        Reject(() => repository.CarregarOuCriar(teams, captains, manual, 13, true, false), "changing captain extra/replacement rule cannot rewrite season");
        Check(File.ReadAllBytes(repository.CaminhoArquivo).SequenceEqual(original), "all configuration failures preserve assignment bytes");
        assigned.nomeCapitao = "Cosmetic renamed hero"; assigned.foto = new Sprite();
        var cosmetic = repository.CarregarOuCriar(teams, captains, manual, 13, true);
        Check(cosmetic.atribuicoes[1].overall == overall && Signature(cosmetic) == Signature(initial), "cosmetic asset edits do not change assignments or individual rating");
        var extended = new List<CapitaoConvidado>(captains) { new CapitaoConvidado { name = "Unused extra", nomeCapitao = "Unused", overall = 50 } };
        Check(Signature(repository.CarregarOuCriar(teams, extended, manual, 13, true)) == Signature(initial), "adding unused catalog characters does not reroll assignments");

        Action<DadosCapitaesConvidados>[] corruptions = {
            x=>x.versao=9, x=>x.semente=99, x=>x.capitaoExtra=false,
            x=>x.times.Reverse(), x=>x.atribuicoes.RemoveAt(0),
            x=>x.atribuicoes[1].capitaoId=x.atribuicoes[0].capitaoId,
            x=>x.atribuicoes[1].capitaoId="Missing character", x=>x.atribuicoes[1].overall=0,
            x=>x.atribuicoes[1].nomeCapitao="", x=>x.vinculosManuais.Clear(),
            x=>x.vinculosManuais[0].capitaoId=x.atribuicoes[1].capitaoId
        };
        foreach (var corrupt in corruptions)
        {
            var altered = codec.DeJson(System.Text.Encoding.UTF8.GetString(original)); corrupt(altered);
            File.WriteAllText(repository.CaminhoArquivo, codec.ParaJson(altered));
            byte[] before = File.ReadAllBytes(repository.CaminhoArquivo);
            Reject(() => repository.CarregarOuCriar(teams, captains, manual, 13, true), "corrupt stored assignment is rejected");
            Check(File.ReadAllBytes(repository.CaminhoArquivo).SequenceEqual(before), "corrupt stored assignment is preserved for review");
        }
        File.WriteAllText(repository.CaminhoArquivo, "broken serialized content");
        Reject(() => repository.CarregarOuCriar(teams, captains, manual, 13, true), "malformed saved file is rejected");
        Check(File.ReadAllText(repository.CaminhoArquivo) == "broken serialized content", "malformed saved bytes are not replaced");
        Check(File.ReadAllText(unrelated) == "original season metadata" && File.ReadAllText(results) == "durable results", "captain persistence never changes existing season metadata or results");

        var absent = Repository("missing_over_results");
        Reject(() => absent.CarregarOuCriar(teams, captains, null, 13, true), "existing results without companion file fail closed");
        Check(!File.Exists(absent.CaminhoArquivo), "missing companion guard never creates a new draw over old results");
        var alteredSerializer = Repository("changed_serializer", new ChangedRoundTrip());
        Reject(() => alteredSerializer.CarregarOuCriar(teams, captains, null, 13), "round-trip changing valid assignments is rejected before commit");
        Check(!File.Exists(alteredSerializer.CaminhoArquivo), "invalid serializer cannot commit a changed captain mapping");
        var locked = Repository("exclusive_lock"); Directory.CreateDirectory(locked.Pasta);
        using (var stream = new FileStream(Path.Combine(locked.Pasta, "capitaes_convidados.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        {
            bool blocked = false; try { locked.CarregarOuCriar(teams, captains, null, 13); } catch (IOException) { blocked = true; }
            Check(blocked && !File.Exists(locked.CaminhoArquivo), "concurrent writer lock prevents second season identity commit");
        }
    }

    private static void CompletedOnScreenDraw()
    {
        foreach (int count in new[] { 2, 10, 16, 20, 32 })
        {
            var teams = Teams(count); var captains = Captains(count + 3);
            var arrivals = ArrivalResults(teams, captains);
            var manual = new List<VinculoCapitaoConvidado> { Manual(teams[0], captains[0]) };
            string arrivalOrder = string.Join("|", arrivals.Select(v => v.timeId + ":" + v.capitaoId).ToArray());
            var repository = Repository("screen_" + count);
            Check(!File.Exists(repository.CaminhoArquivo), "draw has no saved assignment before it completes");
            var saved = repository.SalvarSorteio(teams, captains, manual, 531, arrivals, false, false);
            Check(saved.atribuicoes.Select(v => v.timeId).SequenceEqual(teams), "arrival records normalize to saved team order for " + count);
            Check(saved.atribuicoes.All(v => arrivals.Any(a => a.timeId == v.timeId && a.capitaoId == v.capitaoId && a.overall == v.overall)), "saved mapping is exactly the displayed draw rather than another seeded assignment");
            Check(saved.atribuicoes[0].capitaoId == captains[0].Identificador && !saved.capitaoExtra, "completed draw retains reservations and replacement mode");
            Check(string.Join("|", arrivals.Select(v => v.timeId + ":" + v.capitaoId).ToArray()) == arrivalOrder,
                "save does not mutate the simulator's arrival order");
            Check(!ReferenceEquals(saved.atribuicoes[0], arrivals.Last()), "saved records do not alias mutable simulator records");
            byte[] before = File.ReadAllBytes(repository.CaminhoArquivo);
            var identical = repository.SalvarSorteio(teams, captains, manual, 531, arrivals.AsEnumerable().Reverse().ToList(), true, false);
            Check(Signature(identical) == Signature(saved) && File.ReadAllBytes(repository.CaminhoArquivo).SequenceEqual(before),
                "repeated completion in another arrival order reuses exact file bytes");
            Check(identical.salvoEmUtc == saved.salvoEmUtc, "idempotent completion preserves its original timestamp");
            var loaded = repository.CarregarOuCriar(teams, captains, manual, 531, true, false);
            Check(Signature(loaded) == Signature(saved), "old loader resumes a physically drawn season without rerolling");
            arrivals[0].nomeCapitao = "Changed after commit";
            Check(saved.atribuicoes.Last().nomeCapitao != arrivals[0].nomeCapitao && File.ReadAllBytes(repository.CaminhoArquivo).SequenceEqual(before),
                "later presentation mutations cannot change saved records or bytes");
            Check(!Directory.GetFiles(repository.Pasta, "*.tmp").Any(), "screen draw commit leaves no temporary file");
        }

        var ids = Teams(4); var catalog = Captains(7); var result = ArrivalResults(ids, catalog);
        Action<List<RegistroCapitaoConvidado>>[] invalidResults = {
            x => x.RemoveAt(0), x => x.Add(x[0]), x => x[0] = null,
            x => x[0].timeId = x[1].timeId, x => x[0].timeId = "Unknown club",
            x => x[0].capitaoId = "Unknown captain", x => x[0].capitaoId = x[1].capitaoId,
            x => x[0].overall++, x => x[0].overall = 0, x => x[0].nomeCapitao = " "
        };
        for (int i = 0; i < invalidResults.Length; i++)
        {
            var repository = Repository("screen_invalid_" + i); var altered = CopyResults(result);
            invalidResults[i](altered);
            Reject(() => repository.SalvarSorteio(ids, catalog, null, 25, altered), "invalid/partial draw result rejected before commit " + i);
            Check(!File.Exists(repository.CaminhoArquivo), "invalid/partial draw never creates assignment file " + i);
        }
        var noResult = Repository("screen_null");
        Reject(() => noResult.SalvarSorteio(ids, catalog, null, 25, null), "missing draw result cannot commit");
        Check(!File.Exists(noResult.CaminhoArquivo), "missing draw result leaves no assignment file");
        var manualMismatch = Repository("screen_manual_conflict");
        Reject(() => manualMismatch.SalvarSorteio(ids, catalog, new[] { Manual(ids[0], catalog[6]) }, 25, result),
            "displayed result cannot ignore a reserved captain");
        Check(!File.Exists(manualMismatch.CaminhoArquivo), "manual conflict leaves no saved assignment");

        var committed = Repository("screen_conflict");
        var initial = committed.SalvarSorteio(ids, catalog, null, 25, result);
        byte[] original = File.ReadAllBytes(committed.CaminhoArquivo);
        var different = CopyResults(result);
        var captainId = different[0].capitaoId; var name = different[0].nomeCapitao; int rating = different[0].overall;
        different[0].capitaoId = different[1].capitaoId; different[0].nomeCapitao = different[1].nomeCapitao; different[0].overall = different[1].overall;
        different[1].capitaoId = captainId; different[1].nomeCapitao = name; different[1].overall = rating;
        Reject(() => committed.SalvarSorteio(ids, catalog, null, 25, different), "a different valid completion cannot replace a saved draw");
        Check(File.ReadAllBytes(committed.CaminhoArquivo).SequenceEqual(original), "conflicting draw leaves original bytes intact");
        Reject(() => committed.SalvarSorteio(ids, catalog, null, 26, result), "completion cannot change a saved seed");
        Reject(() => committed.SalvarSorteio(ids, catalog, null, 25, result, false, false), "completion cannot change extra-captain rule");
        Check(File.ReadAllBytes(committed.CaminhoArquivo).SequenceEqual(original), "seed/rule mismatches preserve the saved result");
        File.WriteAllText(committed.CaminhoArquivo, "broken screen draw");
        Reject(() => committed.SalvarSorteio(ids, catalog, null, 25, result), "completion cannot repair malformed saved draw by replacement");
        Check(File.ReadAllText(committed.CaminhoArquivo) == "broken screen draw", "malformed saved draw bytes remain untouched");
        var broken = codec.DeJson(codec.ParaJson(initial)); broken.atribuicoes[0].overall++;
        File.WriteAllText(committed.CaminhoArquivo, codec.ParaJson(broken));
        byte[] invalidSaved = File.ReadAllBytes(committed.CaminhoArquivo);
        Reject(() => committed.SalvarSorteio(ids, catalog, null, 25, result), "completion refuses invalid existing snapshot");
        Check(File.ReadAllBytes(committed.CaminhoArquivo).SequenceEqual(invalidSaved), "invalid existing snapshot is preserved");

        var oldSaved = Repository("screen_legacy");
        var legacy = oldSaved.CarregarOuCriar(ids, catalog, null, 58);
        byte[] legacyBytes = File.ReadAllBytes(oldSaved.CaminhoArquivo);
        var reused = oldSaved.SalvarSorteio(ids, catalog, null, 58, CopyResults(legacy.atribuicoes).AsEnumerable().Reverse().ToList(), true);
        Check(Signature(reused) == Signature(legacy) && File.ReadAllBytes(oldSaved.CaminhoArquivo).SequenceEqual(legacyBytes),
            "version-one legacy assignment can be safely acknowledged without migration");
        var absent = Repository("screen_missing_over_results");
        Reject(() => absent.SalvarSorteio(ids, catalog, null, 25, result, true), "completed draw cannot assign captains over existing results without companion file");
        Check(!File.Exists(absent.CaminhoArquivo), "history guard does not create a new draw");
        var tampered = Repository("screen_tampered_serializer", new ChangedRoundTrip());
        Reject(() => tampered.SalvarSorteio(ids, catalog, null, 25, result), "serializer cannot alter a completed physical draw");
        Check(!File.Exists(tampered.CaminhoArquivo), "tampered serializer cannot commit");
        var locked = Repository("screen_writer_lock"); Directory.CreateDirectory(locked.Pasta);
        using (var stream = new FileStream(Path.Combine(locked.Pasta, "capitaes_convidados.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        {
            bool blocked = false; try { locked.SalvarSorteio(ids, catalog, null, 25, result); } catch (IOException) { blocked = true; }
            Check(blocked && !File.Exists(locked.CaminhoArquivo), "completed draw respects an active writer lock");
        }
    }

    public static void Main()
    {
        root = Path.Combine(Path.GetTempPath(), "guest-captain-store-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            AssetIdentity(); SizesSeedsAndManualAssignments(); InvalidConfiguration(); PreservationAndCorruption(); CompletedOnScreenDraw();
            Console.WriteLine("PASS " + checks + " guest-captain asset, assignment and real-filesystem persistence assertions.");
            Console.WriteLine("XML serializer injection and minimal asset data adapters; native Unity import/JSON were not executed.");
        }
        finally { Directory.Delete(root, true); }
    }
}
