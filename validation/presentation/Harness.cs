using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FutebolDeDiscos;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Executes real presentation sources against the documented API facade.
// This is not a renderer, native layout engine or Unity Editor execution.
public static class PresentationHarness
{
    private static int checks;
    private static string scenario;
    private static void Check(bool ok, string message)
    {
        checks++;
        if (!ok) throw new Exception(scenario + ": " + message + " / " + string.Join(" | ", Debug.Errors.ToArray()));
    }
    private static void Near(float value, float expected, string message, float tolerance = .001f)
    { Check(Math.Abs(value - expected) <= tolerance, message + " (" + value + ")"); }
    private static T Field<T>(object owner, string name) where T : class
    {
        FieldInfo info = owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Check(info != null, "private presentation field exists: " + name);
        return info.GetValue(owner) as T;
    }
    private static void Reset()
    {
        foreach (GameObject go in GameObject.All.Where(g => g.transform.parent == null && !g.destroyed).ToArray())
            UnityEngine.Object.Destroy(go);
        GameObject.All.Clear(); Scheduler.Reset(); Time.captureDeltaTime = 0;
        Debug.Errors.Clear(); Debug.Warnings.Clear(); Debug.Messages.Clear();
    }
    private static float Clock(int rate)
    {
        Scheduler.Reset(); Time.timeScale = 0f; Time.unscaledDeltaTime = .1f;
        Time.captureDeltaTime = rate == 0 ? 0f : 1f / rate;
        return rate == 0 ? .1f : 1f / rate;
    }
    private static int Finish(MonoBehaviour owner, IEnumerator sequence, float duration, int rate)
    {
        float step = Clock(rate);
        Coroutine routine = owner.StartCoroutine(sequence);
        int frames = 0;
        while (!routine.Done && frames < 3000) { Scheduler.Tick(.1f); frames++; }
        Check(routine.Done, "presentation terminates within frame budget");
        Check(Math.Abs(frames * step - duration) <= step * 1.1f + .00001f,
            "capture/unscaled duration " + duration + " at " + rate + " fps, frames=" + frames);
        Check(Time.timeScale == 0f, "presentation does not resume gameplay time itself");
        return frames;
    }
    private static List<TextMeshProUGUI> Texts(Transform root)
    { return root.gameObject.GetComponentsInChildren<TextMeshProUGUI>(true).ToList(); }
    private static void Has(Transform root, string value)
    { Check(Texts(root).Any(t => t.text == value), "visible copy includes " + value); }
    private static void NotLegacy(Transform root)
    {
        foreach (string value in new[] { "UP NEXT", "SUBSCRIBE", "TOURNAMENT BRACKET", "ROAD TO\nTHE TITLE" })
            Check(!Texts(root).Any(t => t.text == value), "old identity absent: " + value);
    }
    private static List<TeamData> Teams(int count, bool withImages = true)
    {
        return Enumerable.Range(1, count).Select(i =>
        {
            TeamData team = ScriptableObject.CreateInstance<TeamData>();
            team.name = "team_" + i; team.nomeEquipe = "TEAM " + i; team.sigla = "T" + i;
            team.escudoOuBandeira = withImages ? new Sprite() : null;
            team.elenco.Add(new JogadorDados { nomeJogador = "CAPTAIN " + i, foto512x512 = withImages ? new Sprite() : null });
            return team;
        }).ToList();
    }
    private static TemaCompeticao Theme()
    {
        TemaCompeticao theme = ScriptableObject.CreateInstance<TemaCompeticao>();
        theme.nomeCompeticao = "CUSTOM CUP";
        theme.fonteTitulo = new TMP_FontAsset(); theme.fonteTexto = new TMP_FontAsset();
        theme.logoCompeticao = new Sprite(); theme.logoCanal = new Sprite();
        theme.fundoAbertura = new Sprite(); theme.fundoChaveamento = new Sprite();
        return theme;
    }
    private static void ClosedCover(object owner)
    {
        Canvas canvas = Field<Canvas>(owner, "canvas");
        RectTransform curtain = Field<RectTransform>(owner, "cortina");
        RectTransform top = Field<RectTransform>(owner, "painelSuperior");
        RectTransform bottom = Field<RectTransform>(owner, "painelInferior");
        Check(canvas != null && canvas.gameObject.activeSelf && curtain.gameObject.activeInHierarchy, "handoff curtain is active");
        Check(curtain.GetComponent<Image>() == null, "iris container has no full-screen image left over after panels open");
        foreach (RectTransform panel in new[] { top, bottom })
        {
            Check(panel != null && panel.parent == curtain && panel.gameObject.activeInHierarchy, "half-panel belongs to iris");
            Image image = panel.GetComponent<Image>();
            Check(image != null && image.enabled && image.color.a >= .999f, "iris half is fully opaque");
            Near(panel.anchorMin.x, 0f, "iris spans full width from left"); Near(panel.anchorMax.x, 1f, "iris spans full width to right");
            Near(panel.anchoredPosition.x, 0f, "iris does not use old horizontal transition");
            Near(panel.anchoredPosition.y, 0f, "iris is closed vertically");
        }
        Near(top.anchorMin.y, .5f, "top half begins at center"); Near(top.anchorMax.y, 1f, "top half reaches top");
        Near(bottom.anchorMin.y, 0f, "bottom half reaches bottom"); Near(bottom.anchorMax.y, .5f, "bottom half meets center");
        Check(top.offsetMin.y <= 0f && bottom.offsetMax.y >= 0f, "overlap leaves no seam at center");
    }
    private static void Reveal(MonoBehaviour owner, Action reveal, Func<bool> showing, Func<bool> completed, int rate)
    {
        float step = Clock(rate);
        reveal(); int frames = 0; bool movedTop = false, movedBottom = false;
        RectTransform top = Field<RectTransform>(owner, "painelSuperior"), bottom = Field<RectTransform>(owner, "painelInferior");
        while (showing() && frames < 200)
        {
            Scheduler.Tick(.1f); frames++;
            movedTop |= top.anchoredPosition.y > 1f;
            movedBottom |= bottom.anchoredPosition.y < -1f;
        }
        Check(frames < 200 && movedTop && movedBottom, "iris opens from center toward opposite vertical edges");
        Check(frames * step >= .59f && frames * step < .75f, "iris reveal has bounded recording duration");
        Check(!Field<Canvas>(owner, "canvas").gameObject.activeSelf && !showing(), "reveal removes complete overlay");
        Check(completed(), "successful opening result survives reveal");
    }
    private static void LeagueOpening()
    {
        foreach (int rate in new[] { 0, 30, 60 }) foreach (float seconds in new[] { 0f, 3f, 6f })
        {
            Reset(); scenario = "league opening / " + rate + "/" + seconds;
            List<TeamData> teams = Teams(2); TemaCompeticao theme = Theme();
            AberturaJogoUI opening = new GameObject("League opening owner").AddComponent<AberturaJogoUI>();
            opening.fonteTitulo = theme.fonteTitulo; opening.fonteNomes = theme.fonteTexto;
            opening.fundo = theme.fundoAbertura; opening.logoCanal = theme.logoCanal; opening.tempoTotal = seconds;
            opening.CobrirTela(); ClosedCover(opening);
            Finish(opening, opening.Exibir(teams[0], teams[1], theme.logoCompeticao, 2, 3, 4), Math.Max(3f, seconds), rate);
            Check(opening.EstaExibindo && opening.UltimaExibicaoConcluida, "completed league opening remains covered for preparation");
            ClosedCover(opening);
            Canvas canvas = Field<Canvas>(opening, "canvas");
            Has(canvas.transform, "ARENA"); Has(canvas.transform, "CHALLENGERS"); Has(canvas.transform, "START"); NotLegacy(canvas.transform);
            Check(Field<TextMeshProUGUI>(opening, "nomeA").font == theme.fonteTexto && Field<TextMeshProUGUI>(opening, "nomeB").font == theme.fonteTexto, "custom team-name font survives redesign");
            Check(Field<Image>(opening, "imagemFundo").sprite == theme.fundoAbertura && Field<Image>(opening, "imagemCanal").sprite == theme.logoCanal && Field<Image>(opening, "imagemLiga").sprite == theme.logoCompeticao, "custom background and logos retained");
            Reveal(opening, opening.RevelarArena, () => opening.EstaExibindo, () => opening.UltimaExibicaoConcluida, rate);
            Check(!theme.fonteTexto.destroyed && !theme.logoCanal.destroyed && !teams[0].escudoOuBandeira.destroyed, "opening never destroys source assets");
        }
    }
    private static void OpeningCopyAndCancellation()
    {
        Reset(); scenario = "custom league words and cancellation";
        var opening = new GameObject("Custom opening").AddComponent<AberturaJogoUI>();
        opening.fonteTitulo = opening.fonteNomes = TMP_Settings.defaultFontAsset;
        var teams = Teams(2, false);
        var script = new RoteiroTransmissaoDiscos { tituloPrevia = "CUSTOM ARENA", tituloDuelo = "CUSTOM DUEL", chamadaJogo = "CUSTOM START", separador = "CUSTOM VS", rodada = "STAGE", partida = "DUEL" };
        opening.ConfigurarRoteiro(script); opening.tempoTotal = 3;
        Finish(opening, opening.Exibir(teams[0], teams[1], null, 2, 3, 4), 3, 30);
        Canvas canvas = Field<Canvas>(opening, "canvas");
        foreach (string value in new[] { "CUSTOM ARENA", "CUSTOM DUEL", "CUSTOM START", "CUSTOM VS" }) Has(canvas.transform, value);
        Check(!Field<Image>(opening, "escudoA").enabled && !Field<Image>(opening, "escudoB").enabled, "missing team images use safe hidden images");
        Clock(60); IEnumerator stale = opening.Exibir(teams[0], teams[1], null, 1, 1, 1); Check(stale.MoveNext(), "stale opening begins");
        opening.Ocultar(); IEnumerator fresh = opening.Exibir(teams[1], teams[0], null, 1, 1, 1); Check(fresh.MoveNext(), "fresh opening reenters");
        Check(!stale.MoveNext(), "old enumerator cannot finish or cover a fresh opening");
        Check(!opening.UltimaExibicaoConcluida, "cancel does not count as a completed opening");
        opening.enabled = false; Check(!opening.EstaExibindo && !canvas.gameObject.activeSelf, "disabled opening removes its overlay");
        opening.enabled = true; opening.CobrirTela(); ClosedCover(opening); opening.RevelarArena(); Scheduler.Tick(.1f); opening.Ocultar();
        for (int i = 0; i < 20; i++) Scheduler.Tick(.1f);
        Check(!canvas.gameObject.activeSelf && !opening.EstaExibindo, "cancelled reveal never resurrects its canvas");
        Check(!opening.Exibir(null, teams[1], null, 1, 1, 1).MoveNext(), "missing team cannot start a league preview");
        foreach (FieldInfo field in typeof(RoteiroTransmissaoDiscos).GetFields()) if (field.FieldType == typeof(string)) field.SetValue(script, "");
        opening.ConfigurarRoteiro(script);
        Finish(opening, opening.Exibir(teams[0], teams[1], null, 1, 1, 1), 3, 30);
        Check(!Texts(canvas.transform).Any(t => new[] { "ARENA", "CHALLENGERS", "START", "SUBSCRIBE" }.Contains(t.text)), "empty words never restore forced slogans");
    }
    private static MataMataModelo.Estado State(List<TeamData> teams, int count)
    { return MataMataModelo.Criar(teams.Select(t => t.name).ToList(), count); }
    private static void Bracket()
    {
        foreach (int rate in new[] { 0, 30, 60 }) foreach (float duration in new[] { 3f, 6f })
        {
            Reset(); scenario = "knockout preview / " + rate + "/" + duration;
            var teams = Teams(4); var theme = Theme(); var state = State(teams, 4);
            string original = JsonUtility.ToJson(state);
            var preview = new GameObject("Bracket owner").AddComponent<ChaveamentoUI>();
            preview.CobrirTela(); ClosedCover(preview);
            Finish(preview, preview.Exibir(state, teams, theme, duration), duration, rate);
            ClosedCover(preview); Check(preview.UltimaExibicaoConcluida, "overview completes before match setup");
            Canvas canvas = Field<Canvas>(preview, "canvas"); Has(canvas.transform, "TOURNAMENT GRID"); NotLegacy(canvas.transform);
            string id = MataMataModelo.Proxima(state).id;
            Finish(preview, preview.Exibir(state, teams, theme, duration, id), duration, rate);
            ClosedCover(preview); Has(canvas.transform, "ARENA"); Has(canvas.transform, "CHALLENGERS"); Has(canvas.transform, "START");
            Check(canvas.gameObject.GetComponentsInChildren<Image>(true).Any(i => i.sprite == theme.fundoAbertura) && canvas.gameObject.GetComponentsInChildren<Image>(true).Any(i => i.sprite == theme.logoCanal), "preview keeps user theme background and channel logo");
            Reveal(preview, preview.RevelarArena, () => preview.EstaExibindo, () => preview.UltimaExibicaoConcluida, rate);
            Check(!Field<Image>(preview, "fundoOpaco").enabled, "reveal releases letterbox coverage as well as iris");
            Check(JsonUtility.ToJson(state) == original, "presentation never mutates bracket state");
            while (MataMataModelo.Proxima(state) != null) { var match = MataMataModelo.Proxima(state); MataMataModelo.Registrar(state, match.id, 1, 0); }
            Finish(preview, preview.Exibir(state, teams, theme, duration, null, true), duration, rate);
            Check(preview.EstaExibindo && preview.UltimaExibicaoConcluida && canvas.gameObject.activeSelf, "champion display remains visible after completion");
            Has(canvas.transform, "ARENA CHAMPION"); Has(canvas.transform, "COMPETITION CONQUERED");
            preview.Ocultar(); Check(!canvas.gameObject.activeSelf, "explicit hide dismisses champion presentation");
        }
    }
    private static void BracketCopySizesCancellation()
    {
        foreach (int count in new[] { 2, 8, 16, 32 })
        {
            Reset(); scenario = "bracket sizes / " + count;
            var teams = Teams(count, false); var state = State(teams, count); var theme = Theme();
            var preview = new GameObject("Size bracket").AddComponent<ChaveamentoUI>();
            var script = new RoteiroTransmissaoDiscos { tituloPrevia = "MY ARENA", tituloDuelo = "MY DUEL", chamadaJogo = "MY START", tituloChaveamento = "MY GRID", separador = "MY VS" };
            preview.ConfigurarRoteiro(script);
            Finish(preview, preview.Exibir(state, teams, theme, 3, MataMataModelo.Proxima(state).id), 3, 60);
            var canvas = Field<Canvas>(preview, "canvas");
            foreach (string value in new[] { "MY ARENA", "MY DUEL", "MY START", "MY VS" }) Has(canvas.transform, value);
            ClosedCover(preview); Clock(30);
            IEnumerator old = preview.Exibir(state, teams, theme, 3); Check(old.MoveNext(), "old bracket starts");
            preview.CobrirTela(); ClosedCover(preview);
            IEnumerator fresh = preview.Exibir(state, teams, theme, 3); Check(fresh.MoveNext(), "bracket reenters after cover");
            Check(!old.MoveNext() && !preview.UltimaExibicaoConcluida, "stale bracket sequence cannot complete new one");
            preview.enabled = false; Check(!preview.EstaExibindo && !canvas.gameObject.activeSelf, "disable removes bracket overlay");
        }
    }
    private static void Standings()
    {
        foreach (int rate in new[] { 0, 30, 60 })
        {
            Reset(); scenario = "standings / " + rate;
            var teams = Teams(32); var table = teams.Select((t, i) => new LigaCampeonato.Linha { id = t.name, TEAM = t.nomeEquipe, POS = i + 1, PTS = 32 - i }).ToList();
            var ui = new GameObject("Standings").AddComponent<ClassificacaoUI>();
            ui.fonte = TMP_Settings.defaultFontAsset; ui.tempoEntradaAbertura = ui.tempoEntradaEncerramento = .4f;
            ui.tempoLeituraAbertura = ui.tempoLeituraEncerramento = .8f; ui.tempoSaida = .3f; ui.manterTabelaFinalNaTela = false;
            Clock(rate); Coroutine routine = ui.StartCoroutine(ui.ExibirTabela(table, null, teams, 3, false));
            var root = Field<GameObject>(ui, "raiz"); Has(root.transform, "LEAGUE STATUS"); Has(root.transform, "NEXT ENCOUNTERS");
            int frames = 0; while (!routine.Done && frames < 1000) { Scheduler.Tick(.1f); frames++; }
            Check(routine.Done && ui.UltimaExibicaoConcluida && !ui.EstaExibindo, "standings complete with capture clock");
            Check(root.GetComponentsInChildren<RectTransform>(true).Any(t => t.gameObject.name == "Hologram Scan"), "standings contain hologram scan geometry");
            Check(root.GetComponentsInChildren<TextMeshProUGUI>(true).Count(t => t.gameObject.name == "Column 1" && t.transform.parent.gameObject.name.StartsWith("Team ")) == 32, "all32 teams retained");
            ui.Ocultar(); Check(!root.activeSelf, "following match releases standings cover");
            var script = new RoteiroTransmissaoDiscos { tituloClassificacao = "MY BOARD", agenda = "MY AGENDA", resultados = "MY RESULTS", rodada = "SECTOR" };
            ui.ConfigurarRoteiro(script); Clock(rate);
            routine = ui.StartCoroutine(ui.ExibirTabela(table, table, teams, 3, true));
            root = Field<GameObject>(ui, "raiz"); Has(root.transform, "MY BOARD"); Has(root.transform, "MY RESULTS"); Has(root.transform, "SECTOR 3  ·  MY RESULTS");
            ui.Ocultar(); for (int i = 0; i < 20; i++) Scheduler.Tick(.1f);
            Check(!root.activeSelf && !ui.UltimaExibicaoConcluida, "cancelled standings neither complete nor resurrect");
        }
    }
    public static void Main()
    {
        string dir = Path.Combine(Path.GetTempPath(), "disc-presentation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir); Application.persistentDataPath = dir;
        try
        {
            LeagueOpening(); OpeningCopyAndCancellation(); Bracket(); BracketCopySizesCancellation(); Standings();
            Console.WriteLine("PASS " + checks + " presentation assertions on actual opening, bracket and standings sources.");
            Console.WriteLine("Clock, iris geometry, coverage, copy, lifecycle and asset references use a Unity API facade; native rendering, text layout and physics were not executed.");
        }
        finally { Reset(); Directory.Delete(dir, true); }
    }
}
