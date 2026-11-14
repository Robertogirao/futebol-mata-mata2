using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FutebolDeDiscos;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Actual bracket, captain store and team sources. The API facade does not render.
public static class BracketCaptainsHarness
{
    private static int checks;
    private static string scenario;
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception(scenario + ": " + message); }
    private static void Clean()
    {
        foreach (GameObject go in GameObject.All.Where(g => !g.destroyed && g.transform.parent == null).ToArray()) UnityEngine.Object.Destroy(go);
        GameObject.All.Clear(); Scheduler.Reset(); Time.captureDeltaTime = 1f / 60f; Time.timeScale = 0f;
        Debug.Errors.Clear(); Debug.Warnings.Clear(); Debug.Messages.Clear();
    }
    private static List<TeamData> Teams(int count)
    {
        return Enumerable.Range(0, count).Select(i =>
        {
            var team = ScriptableObject.CreateInstance<TeamData>(); team.name = "club_" + i;
            team.nomeEquipe = "CLUB " + i; team.sigla = "C" + i; team.overall = 62;
            team.escudoOuBandeira = new Sprite { name = "Club crest " + i };
            team.sortearTitularesAleatorios = true;
            team.elenco.Add(new JogadorDados { nomeJogador = "Footballer " + i, foto512x512 = new Sprite() });
            team.elenco.Add(new JogadorDados { nomeJogador = "Footballer substitute " + i });
            return team;
        }).ToList();
    }
    private static List<CapitaoConvidado> Captains(int count)
    {
        return Enumerable.Range(0, count).Select(i => new CapitaoConvidado
        {
            name = "guest_" + i, nomeCapitao = "Hero " + i, overall = 75,
            foto = new Sprite { name = "Hero portrait " + i }
        }).ToList();
    }
    private sealed class Serializer : ISerializadorCapitaesConvidados
    {
        public string ParaJson(DadosCapitaesConvidados d) { return JsonUtility.ToJson(d, true); }
        public DadosCapitaesConvidados DeJson(string text) { return JsonUtility.FromJson<DadosCapitaesConvidados>(text); }
    }
    private static void Show(ChaveamentoUI ui, MataMataModelo.Estado state, List<TeamData> teams, string focus = null, bool final = false)
    {
        var theme = new TemaCompeticao { fonteTexto = TMP_Settings.defaultFontAsset, fonteTitulo = TMP_Settings.defaultFontAsset };
        IEnumerator sequence = ui.Exibir(state, teams, theme, 3f, focus, final);
        int frames = 0;
        while (sequence.MoveNext()) if (++frames > 1000) throw new Exception("Bracket did not finish");
        Check(ui.UltimaExibicaoConcluida, "bracket completes its recording-safe presentation");
    }
    private static RectTransform[] Rows(ChaveamentoUI ui)
    { return ui.GetComponentsInChildren<RectTransform>(true).Where(t => t.gameObject.name == "EquipeA" || t.gameObject.name == "EquipeB").ToArray(); }
    private static TextMeshProUGUI RowText(RectTransform row, string name)
    { return row.GetComponentsInChildren<TextMeshProUGUI>(true).Single(t => t.gameObject.name == name); }
    private static void CheckRows(ChaveamentoUI ui, List<TeamData> teams, Dictionary<string, CapitaoConvidado> byTeam)
    {
        foreach (RectTransform row in Rows(ui))
        {
            var label = RowText(row, "Nome");
            TeamData team = teams.FirstOrDefault(t => t.nomeEquipe == label.text || t.sigla == label.text);
            var portraits = row.GetComponentsInChildren<Image>(true).Where(i => i.gameObject.name == "FotoCapitao").ToArray();
            if (team == null)
            {
                Check(label.text == "BYE" || label.text == "TBD", "unresolved slots retain their normal label");
                Check(portraits.Length == 0, "unresolved slots never receive a guessed captain photo");
                continue;
            }
            CapitaoConvidado captain = byTeam[team.name];
            if (captain.foto == null) { Check(portraits.Length == 0, "missing captain photo reserves no extra slot"); continue; }
            Check(portraits.Length == 1 && portraits[0].sprite == captain.foto, "club row shows exactly its assigned captain portrait");
            Check(portraits[0].preserveAspect, "captain portrait preserves its proportions");
            var crest = row.GetComponentsInChildren<Image>(true).Single(i => i.gameObject.name == "Escudo");
            Check(crest.sprite == team.escudoOuBandeira, "club crest stays visible beside the portrait");
            RectTransform portraitFrame = (RectTransform)portraits[0].transform.parent;
            RectTransform crestRect = crest.GetComponent<RectTransform>();
            RectTransform score = RowText(row, "Placar").rectTransform;
            Check(crestRect.anchoredPosition.x + crestRect.sizeDelta.x * .5f < portraitFrame.anchoredPosition.x - portraitFrame.sizeDelta.x * .5f,
                "crest and captain portrait do not overlap");
            Check(portraitFrame.anchoredPosition.x + portraitFrame.sizeDelta.x * .5f < label.rectTransform.anchoredPosition.x - label.rectTransform.sizeDelta.x * .5f,
                "portrait leaves room for the club name, including the dense 32-team bracket");
            Check(label.rectTransform.anchoredPosition.x + label.rectTransform.sizeDelta.x * .5f < score.anchoredPosition.x - score.sizeDelta.x * .5f,
                "club name does not overlap its score");
            Check(portraitFrame.sizeDelta.y < row.sizeDelta.y, "portrait frame stays inside the row");
        }
    }
    private static void SavedAssignmentsThroughEveryStage(string folder)
    {
        foreach (int count in new[] { 2, 4, 8, 16, 32 })
        {
            Clean(); scenario = "saved bracket portraits " + count;
            var teams = Teams(count); var captains = Captains(count + 2);
            var store = new RepositorioCapitaesConvidados(Path.Combine(folder, count.ToString()), new Serializer());
            var saved = store.CarregarOuCriar(teams.Select(t => t.name), captains, null, 2971);
            string bytes = File.ReadAllText(store.CaminhoArquivo);
            var byTeam = saved.atribuicoes.ToDictionary(r => r.timeId, r => captains.Single(c => c.Identificador == r.capitaoId));
            var firstPlayer = teams.ToDictionary(t => t.name, t => t.elenco[0]);
            var state = MataMataModelo.Criar(teams.Select(t => t.name).ToList(), count);
            var ui = new GameObject("Saved bracket").AddComponent<ChaveamentoUI>();
            int lookups = 0;
            ui.ConfigurarCapitaes(t => { lookups++; return byTeam[t.name]; });
            UnityEngine.Random.InitState(743); int expectedRandom = UnityEngine.Random.Range(0, 1000000); UnityEngine.Random.InitState(743);
            string originalState = JsonUtility.ToJson(state);
            Show(ui, state, teams);
            Check(lookups == count, "captains resolve once per team rather than being selected again per row/frame");
            Check(UnityEngine.Random.Range(0, 1000000) == expectedRandom, "bracket never rerolls football representatives");
            CheckRows(ui, teams, byTeam);
            Check(JsonUtility.ToJson(state) == originalState, "portrait rendering never changes bracket state");

            var next = MataMataModelo.Proxima(state);
            Show(ui, state, teams, next.id);
            var spotlightPhotos = ui.GetComponentsInChildren<Image>(true).Where(i => i.gameObject.name == "FotoCapitaoDestaque").ToArray();
            Check(spotlightPhotos.Length == 2 && spotlightPhotos.Any(i => i.sprite == byTeam[next.a].foto) && spotlightPhotos.Any(i => i.sprite == byTeam[next.b].foto),
                "match preview keeps both already assigned portraits");
            CheckRows(ui, teams, byTeam);

            MataMataModelo.Registrar(state, next.id, 2, 1);
            Show(ui, state, teams);
            CheckRows(ui, teams, byTeam);
            Check(ui.GetComponentsInChildren<TextMeshProUGUI>(true).Any(t => t.gameObject.name == "Placar" && t.text == "2"), "scores remain in the bracket beside both identities");

            while ((next = MataMataModelo.Proxima(state)) != null) MataMataModelo.Registrar(state, next.id, 1, 0);
            Show(ui, state, teams, null, true);
            CheckRows(ui, teams, byTeam);
            Check(ui.GetComponentsInChildren<Image>(true).Single(i => i.gameObject.name == "FotoCapitaoCampeao").sprite == byTeam[state.campeao].foto,
                "champion presentation shows the champion's fixed captain");
            if (count > 2)
                Check(ui.GetComponentsInChildren<Image>(true).Single(i => i.gameObject.name == "FotoCapitaoCampeaoResumo").sprite == byTeam[state.campeao].foto,
                    "central bracket summary retains the winning club and captain");
            Check(teams.All(t => t.overall == 62 && t.elenco.Count == 2 && ReferenceEquals(t.elenco[0], firstPlayer[t.name])), "team overalls and original rosters remain untouched");
            Check(captains.All(c => c.foto != null && !c.foto.destroyed), "the bracket never destroys external captain photo assets");
            Check(File.ReadAllText(store.CaminhoArquivo) == bytes, "every bracket stage leaves stored associations byte-identical");

            UnityEngine.Object.Destroy(ui.gameObject);
            var loaded = store.CarregarOuCriar(teams.Select(t => t.name), captains, null, 2971);
            var restored = loaded.atribuicoes.ToDictionary(r => r.timeId, r => captains.Single(c => c.Identificador == r.capitaoId));
            ui = new GameObject("Reloaded bracket").AddComponent<ChaveamentoUI>();
            ui.ConfigurarCapitaes(t => restored[t.name]);
            Show(ui, state, teams, null, true);
            Check(ui.GetComponentsInChildren<Image>(true).Single(i => i.gameObject.name == "FotoCapitaoCampeao").sprite == byTeam[state.campeao].foto,
                "reopening the saved tournament retains the same captain portrait");
        }
    }
    private static void MissingPhotosAndByes()
    {
        Clean(); scenario = "missing photos, original mode and byes";
        var teams = Teams(6); var state = MataMataModelo.Criar(teams.Select(t => t.name).ToList(), 8);
        var ui = new GameObject("Fallback bracket").AddComponent<ChaveamentoUI>();
        Show(ui, state, teams);
        var before = Rows(ui).ToDictionary(row => row.parent.gameObject.name + row.gameObject.name,
            row => new[] { RowText(row, "Nome").rectTransform.anchoredPosition.x, RowText(row, "Nome").rectTransform.sizeDelta.x });
        Check(!ui.GetComponentsInChildren<Image>(true).Any(i => i.gameObject.name.StartsWith("FotoCapitao")), "ordinary football mode keeps the original crest-only bracket");
        var noPhoto = Captains(6); noPhoto.ForEach(c => c.foto = null);
        var byTeam = teams.Select((team, index) => new { team.name, captain = noPhoto[index] }).ToDictionary(p => p.name, p => p.captain);
        ui.ConfigurarCapitaes(t => byTeam[t.name]);
        Show(ui, state, teams);
        foreach (RectTransform row in Rows(ui))
        {
            var old = before[row.parent.gameObject.name + row.gameObject.name];
            Check(old[0] == RowText(row, "Nome").rectTransform.anchoredPosition.x && old[1] == RowText(row, "Nome").rectTransform.sizeDelta.x,
                "missing portraits preserve the exact original text space");
        }
        CheckRows(ui, teams, byTeam);
        noPhoto[0].foto = new Sprite { name = "Added later" };
        Show(ui, state, teams);
        Check(ui.GetComponentsInChildren<Image>(true).Any(i => i.gameObject.name == "FotoCapitao" && i.sprite == noPhoto[0].foto),
            "adding a cosmetic portrait updates its fixed captain without another draw");
        ui.ConfigurarCapitaes(null); Show(ui, state, teams);
        Check(!ui.GetComponentsInChildren<Image>(true).Any(i => i.gameObject.name.StartsWith("FotoCapitao")), "removing the guest provider restores original mode");
    }
    public static void Main()
    {
        string folder = Path.Combine(Path.GetTempPath(), "bracket-captain-photos-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            SavedAssignmentsThroughEveryStage(folder); MissingPhotosAndByes();
            Check(Debug.Errors.Count == 0, "no presentation errors logged");
            Console.WriteLine("PASS " + checks + " saved-captain bracket portrait and layout assertions using actual sources.");
            Console.WriteLine("Unity UI/TMP API facade; native rendering was not executed.");
        }
        finally { Clean(); Directory.Delete(folder, true); }
    }
}
