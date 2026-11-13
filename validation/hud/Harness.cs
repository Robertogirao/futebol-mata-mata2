using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using FutebolDeDiscos;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class Harness
{
    private static int checks;
    private static void Check(bool ok, string message)
    { checks++; if (!ok) throw new Exception("FAIL: " + message); }
    private static void Near(float value, float expected, string message)
    { Check(Math.Abs(value - expected) < .0001f, message + " [" + value + "]"); }
    private static object Field(object target, string name)
    { return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target); }
    private static void Call(object target, string name)
    { target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null); }
    private static RectTransform Rect(Transform parent, string name)
    { var result = parent.Find(name) as RectTransform; Check(result != null, "found " + name); return result; }
    private static TextMeshProUGUI Text(Transform parent, string name)
    { return Rect(parent, name).GetComponent<TextMeshProUGUI>(); }
    private static void ExpectError(Action operation, string message)
    { bool failed = false; try { operation(); } catch (InvalidOperationException) { failed = true; } Check(failed, message); }

    public static void Main()
    {
        Scheduler.Reset();
        var owner = new GameObject("FutebolDiscos_Manager");
        var ui = owner.AddComponent<UniversalUI>();
        var banner = owner.AddComponent<GoalBannerUI>();
        Call(banner, "Awake");
        var hud = owner.AddComponent<HudFutebolDiscos>();
        var config = owner.AddComponent<ProjetoFutebolDiscos>();
        var title = new TMP_FontAsset(); var body = new TMP_FontAsset();
        var cup = new Sprite(); var channel = new Sprite();
        config.tema = new TemaCompeticao { fonteTitulo = title, fonteTexto = body,
            logoCompeticao = cup, logoCanal = channel, corDestaque = new Color(.1f,.9f,.8f) };
        config.formato = FormatoDiscos.MataMata;
        hud.Construir(config, ui, banner);
        var root = (RectTransform)Field(hud, "raiz");
        Check(root.parent == ui.transform, "canvas belongs to active controller");
        Check(root.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay, "screen overlay");
        Check(root.GetComponent<Canvas>().sortingOrder == 100, "live scoreboard sorting order");
        foreach (string filename in new [] { "ClassificacaoUI.cs", "AberturaJogoUI.cs", "ChaveamentoUI.cs" })
        {
            string source = File.ReadAllText("Projeto/Assets/Scripts/" + filename);
            Match assignment = Regex.Match(source, @"canvas\.sortingOrder\s*=\s*(\d+)");
            Check(assignment.Success && root.GetComponent<Canvas>().sortingOrder < int.Parse(assignment.Groups[1].Value), "broadcast below actual " + filename);
        }
        Near(root.GetComponent<CanvasScaler>().referenceResolution.x, 1920, "broadcast width");
        Near(root.GetComponent<CanvasScaler>().referenceResolution.y, 1080, "broadcast height");
        var header = Rect(root, "Scoreboard");
        Check(-header.anchoredPosition.y + header.sizeDelta.y < 180, "header clears pitch top");
        Check(Text(header, "Championship").text == config.nomeCampeonato, "configurable championship name");
        Check(Text(header, "Championship").font == title, "theme heading font");
        Check(Text(root, "Match State").font == body, "theme body font");
        Check(ui.logoCampeonato.sprite == cup, "competition logo binding");
        Check(Rect(header, "Channel Logo").GetComponent<Image>().sprite == channel, "channel logo binding");
        Check(ui.placarTimeA.text == "0" && ui.placarTimeB.text == "0" && ui.textoCronometro.text == "00:00", "live fields initialized");
        Check(Text(root, "Broadcast Format").text.Contains("KNOCKOUT") && Text(root, "Broadcast Format").text.Contains("3 v 3"), "match format label");
        Check(ui.fotosTitularesA.Length == 0 && ui.nomesTitularesB.Length == 0 && ui.numerosTitularesA.Length == 0, "old six-player bindings cleared");
        Check(banner.painelBanner == ui.bannerGol && !banner.painelBanner.activeSelf, "goal panel linked and initially hidden");
        Check(!owner.transform.IsChildOf(banner.painelBanner.transform), "hiding goal panel never disables controller");
        Check(banner.textoDados.richText, "real goal text renders its color tags");
        Check(ui.textoCronometro.richText, "golden-goal clock renders size and color tags");

        var crest = new Sprite();
        var teamA = new TeamData { nomeEquipe = " Brazil ", sigla = "BRA", escudoOuBandeira = crest, corPrincipal = Color.black };
        var teamB = new TeamData { nomeEquipe = "Japan", sigla = "JPN" };
        hud.ConfigurarEquipes(teamA, teamB, Color.black, new Color(1,.1f,.2f,.1f));
        Check(Text(Rect(header, "Home Team"), "Home Name").text == "BRAZIL", "trimmed team name");
        Check(Text(Rect(header, "Away Team"), "Away Name").text == "JAPAN", "away team name");
        Check(ui.escudoTimeA.enabled && ui.escudoTimeA.sprite == crest, "home crest linked");
        Check(!ui.escudoTimeB.enabled, "missing crest hidden");
        var homeAccent = Rect(Rect(header, "Home Team"), "Home Color").GetComponent<Image>();
        var awayAccent = Rect(Rect(header, "Away Team"), "Away Color").GetComponent<Image>();
        Check(homeAccent.color.maxColorComponent > .2f, "black team has visible accent");
        Near(awayAccent.color.a, 1, "transparent team tint made visible");
        hud.MostrarEstado("  GOLDEN GOAL  ");
        Check(Text(root, "Match State").text == "GOLDEN GOAL", "state trim");
        hud.MostrarEstado(null);
        Check(Text(root, "Match State").text == "", "state can clear");
        foreach (var image in root.gameObject.GetComponentsInChildren<Image>(true))
            Check(!image.raycastTarget, "HUD never intercepts pointer / " + image.gameObject.name);
        foreach (var text in root.gameObject.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            Check(!text.raycastTarget, "text never intercepts pointer / " + text.gameObject.name);
            Check(text.enableAutoSizing && text.overflowMode == TextOverflowModes.Ellipsis, "long names fit / " + text.gameObject.name);
        }

        var scorer = new JogadorDados { nomeJogador = "Goal Scorer", foto512x512 = new Sprite() };
        banner.tempoDeExibicao = .1f;
        banner.intervaloEntreBanners = .1f;
        banner.ExibirBannerDeGol(scorer, Color.white);
        banner.ExibirBannerDeGol(null, Color.white);
        Check(owner.activeInHierarchy && banner.painelBanner.activeSelf, "real queued celebration leaves controller alive");
        Check(banner.fotoJogador.sprite == scorer.foto512x512 && banner.textoDados.text.Contains("GOAL SCORER"), "real banner score data");
        Scheduler.Tick(); Scheduler.Tick();
        Check(banner.painelBanner.activeSelf && !banner.fotoJogador.enabled && banner.textoDados.text == "<color=yellow>GOAL!</color>", "queued scorer fallback");
        Scheduler.Tick(); Scheduler.Tick();
        Check(!banner.TemBannersPendentes && !banner.painelBanner.activeSelf, "queued celebrations finish");

        var cards = LayoutJogadorUnicoUI.Criar(root.GetComponent<Canvas>(), body);
        cards.Configurar(teamA, teamB, new List<JogadorDados> { null, scorer }, null);
        var a = Rect(cards.transform, "Time_A"); var b = Rect(cards.transform, "Time_B");
        Near(a.anchorMax.y, .175f, "home card below pitch");
        Near(b.anchorMin.y, .012f, "away card within bottom margin");
        Check(a.anchorMax.y * 1080 < (8.3f - 5.2f) / (8.3f * 2) * 1080, "resting cards never overlap pitch");
        Check(Rect(a,"Foto_Jogador").GetComponent<Image>().sprite == scorer.foto512x512, "first valid representative portrait");
        Check(Text(a,"Nome_Jogador").text == "Goal Scorer", "representative name");
        Check(Text(b,"Sem_Foto").enabled && Text(b,"Sem_Foto").text.Contains("NO PHOTO"), "missing portrait fallback in English");
        Check(Text(b,"Nome_Jogador").text == "PLAYER NOT SET", "missing player fallback in English");
        Check(cards.PrepararApresentacao(), "player introduction prepares");
        Near((a.anchorMin.x + a.anchorMax.x) / 2, .33f, "intro left center");
        Near((b.anchorMin.x + b.anchorMax.x) / 2, .67f, "intro right center");
        Near((a.anchorMin.y + a.anchorMax.y) / 2, .5f, "intro vertical center");
        Near(a.localScale.x, 1.75f, "intro enlarges cards");
        Time.captureDeltaTime = .02f;
        Time.unscaledDeltaTime = .1f;
        var introduction = cards.ExibirApresentacao(2.5f, .4f);
        for (int i = 0; i < 100; i++) Check(introduction.MoveNext(), "capture holds reading frame " + i);
        Near(a.localScale.x, 1.75f, "Recorder time governs reading");
        int frames = 100;
        while (introduction.MoveNext() && frames < 200) frames++;
        Check(frames >= 145 && frames <= 146, "reading plus travel capture duration / " + frames);
        Check(cards.ApresentacaoConcluida, "intro completed");
        Near(a.anchorMax.y,.175f,"intro returns below pitch"); Near(a.localScale.x,1,"intro restores scale");
        cards.PrepararApresentacao(); var stale = cards.ExibirApresentacao(3,1);
        Check(stale.MoveNext(), "cancel sequence starts");
        cards.CancelarApresentacao(); cards.PrepararApresentacao();
        Check(!stale.MoveNext(), "stale sequence cannot reposition new intro");
        cards.gameObject.SetActive(false);
        Near(a.anchorMax.y,.175f,"disable restores resting card");
        Check(!cards.ApresentacaoConcluida, "cancel is not a completed presentation");

        // Build again within the same frame: Unity's deferred Destroy must not leave
        // the retired canvas discoverable by UniversalUI's child-Canvas fallback.
        hud.Construir(config, ui, banner);
        Check(root.gameObject.destroyed && !root.gameObject.activeSelf && root.parent == null, "retired canvas detached before deferred destroy");
        var replacement = (RectTransform)Field(hud, "raiz");
        Check(owner.transform.children.Count == 1 && replacement != root, "single discoverable child canvas after rebuild");
        Check(!cup.destroyed && !channel.destroyed && !title.destroyed && !body.destroyed, "shared assets never destroyed");
        hud.ConfigurarEquipes(null, null, Color.white, Color.white);
        Check(Text(Rect(Rect(replacement,"Scoreboard"),"Home Team"),"Home Name").text == "HOME TEAM", "missing team fallback");
        ExpectError(() => hud.Construir(config,null,banner), "missing score driver rejected");
        Check((RectTransform)Field(hud,"raiz") == replacement, "invalid dependencies preserve valid HUD");
        hud.Limpar(); hud.Limpar();
        Check(Field(hud,"raiz") == null && replacement.gameObject.destroyed, "cleanup is repeatable");
        Check(ui.placarTimeA == null && ui.textoCronometro == null && banner.painelBanner == null, "retired UI bindings removed");
        Check(owner.activeInHierarchy, "controller survives cleanup");
        config.tema = null; config.formato = FormatoDiscos.Liga;
        hud.Construir(config,ui,banner);
        Check(Text(Rect((RectTransform)Field(hud,"raiz"),"Scoreboard"),"Championship").font == TMP_Settings.defaultFontAsset, "default TMP font fallback");
        Check(Text((RectTransform)Field(hud,"raiz"),"Broadcast Format").text.Contains("LEAGUE"), "league label");
        ValidateStandings(((RectTransform)Field(hud,"raiz")).GetComponent<Canvas>().sortingOrder);
        ValidateRecordingTiming(banner);
        ValidateEditableBranding(hud, config, ui, banner);
        Console.WriteLine("PASS scoreboard bindings, fonts, logos, goal queue, pitch clearances, player opening, Recorder time, cancellation and rebuild cleanup");
        Console.WriteLine("PASS " + checks + " assertions using real HUD, LayoutJogadorUnicoUI, GoalBannerUI, ClassificacaoUI, TeamData and TemaCompeticao with Unity UI/TMP contract adapters");
    }

    private static void ValidateEditableBranding(HudFutebolDiscos hud, ProjetoFutebolDiscos config, UniversalUI ui, GoalBannerUI banner)
    {
        config.tituloRodape = "  YOUR SQUAD  ";
        config.subtituloRodape = "  Every disc has a story  ";
        config.formato = FormatoDiscos.MataMata;
        config.discosPorTime = 5;
        hud.Construir(config, ui, banner);
        var root = (RectTransform)Field(hud, "raiz");
        Check(Text(root, "Broadcast Signature").text == "YOUR SQUAD", "custom footer heading replaces fixed branding");
        string description = Text(root, "Broadcast Format").text;
        Check(description.Contains("KNOCKOUT") && description.Contains("5 v 5") && description.EndsWith("Every disc has a story"), "custom footer subtitle retains independent format details");
        Check(!description.Contains("AUTOMATIC DISC FOOTBALL"), "custom footer never retains old automatic-disc branding");
        config.mostrarDetalhesFormato = false;
        hud.Construir(config, ui, banner);
        root = (RectTransform)Field(hud, "raiz");
        Check(Text(root, "Broadcast Format").text == "Every disc has a story", "format details can hide while custom subtitle remains");
        config.mostrarSubtituloRodape = false;
        config.mostrarTituloRodape = false;
        hud.Construir(config, ui, banner);
        root = (RectTransform)Field(hud, "raiz");
        Check(root.Find("Broadcast Signature") == null && root.Find("Broadcast Format") == null, "footer toggles remove both optional text objects");
        config.mostrarDetalhesFormato = true;
        hud.Construir(config, ui, banner);
        root = (RectTransform)Field(hud, "raiz");
        Check(Text(root, "Broadcast Format").text == "KNOCKOUT  ·  5 v 5", "format details stay visible without the subtitle");
        config.mostrarTituloRodape = config.mostrarSubtituloRodape = true;
        config.mostrarDetalhesFormato = false;
        foreach (string empty in new [] { "", "  ", null })
        {
            config.tituloRodape = config.subtituloRodape = empty;
            hud.Construir(config, ui, banner);
            root = (RectTransform)Field(hud, "raiz");
            Check(root.Find("Broadcast Signature") == null && root.Find("Broadcast Format") == null, "empty footer fields hide without restoring fixed defaults");
            foreach (var text in root.gameObject.GetComponentsInChildren<TextMeshProUGUI>(true))
                Check(!text.text.Contains("DISC FOOTBALL"), "cleared branding does not leak elsewhere in HUD / " + text.gameObject.name);
        }
        Check(ui.placarTimeA != null && ui.placarTimeB != null && ui.textoCronometro != null, "branding visibility never removes live match bindings");
        Console.WriteLine("PASS editable footer heading/subtitle, independent format details, toggles and empty-field hiding");
    }

    private static void ValidateRecordingTiming(GoalBannerUI banner)
    {
        foreach (int rate in new [] { 0, 30, 60 })
        {
            Scheduler.Reset(); Time.timeScale = 0f;
            Time.captureDeltaTime = rate == 0 ? 0f : 1f / rate;
            Time.unscaledDeltaTime = .1f;
            float step = rate == 0 ? .1f : 1f / rate;
            banner.tempoDeExibicao = 3f; banner.intervaloEntreBanners = .15f;
            banner.ExibirBannerDeGol(new JogadorDados { nomeJogador = "FIRST" }, Color.white);
            banner.ExibirBannerDeGol(new JogadorDados { nomeJogador = "SECOND" }, Color.white);
            int ticks = 0;
            int firstFrames = (int)Math.Ceiling((3d - .000001d) / step);
            int gapFrames = (int)Math.Ceiling((.15d - .000001d) / step);
            for (; ticks < firstFrames - 1; ticks++) Scheduler.Tick(.1f);
            Check(banner.painelBanner.activeSelf && banner.textoDados.text.Contains("FIRST"), "goal stays visible for configured capture duration / " + rate);
            Scheduler.Tick(.1f); ticks++;
            Check(!banner.painelBanner.activeSelf && banner.TemBannersPendentes, "capture goal interval starts / " + rate);
            for (int i = 0; i < gapFrames; i++) { Scheduler.Tick(.1f); ticks++; }
            Check(banner.painelBanner.activeSelf && banner.textoDados.text.Contains("SECOND"), "capture queue preserves order and interval / " + rate);
            while (banner.TemBannersPendentes && ticks < 1000) { Scheduler.Tick(.1f); ticks++; }
            Check(ticks == (firstFrames + gapFrames) * 2, "capture queue exact duration / " + rate + "/" + ticks);
            Check(!banner.painelBanner.activeSelf, "capture queue hides after two goals / " + rate);
            Check(Time.timeScale == 0f, "banner timing works with paused match / " + rate);

            var standings = new GameObject("Timed standings " + rate).AddComponent<ClassificacaoUI>();
            standings.manterTabelaFinalNaTela = false;
            standings.tempoEntradaAbertura = standings.tempoEntradaEncerramento = .4f;
            standings.tempoLeituraAbertura = standings.tempoLeituraEncerramento = .8f;
            standings.tempoSaida = .3f;
            var table = new List<LigaCampeonato.Linha> { new LigaCampeonato.Linha { POS = 1, TEAM = "FIRST" } };
            foreach (bool closing in new [] { false, true })
            {
                Scheduler.Start(standings.ExibirTabela(table,null,null,1,closing));
                ticks = 0;
                int expected = (int)Math.Round(1.5f / step);
                while (standings.EstaExibindo && ticks < expected + 3) { Scheduler.Tick(.1f); ticks++; }
                Check(ticks == expected, "standings entry, reading and exit use capture step / " + rate + "/" + closing + "/" + ticks);
                Check(standings.UltimaExibicaoConcluida && !standings.EstaExibindo, "timed standings complete / " + rate + "/" + closing);
                standings.Ocultar();
            }
            banner.ExibirBannerDeGol(null,Color.white);
            Scheduler.Tick(.1f); banner.LimparFila();
            for (int i = 0; i < 100; i++) Scheduler.Tick(.1f);
            Check(!banner.TemBannersPendentes && !banner.painelBanner.activeSelf, "capture queue cancellation has no late banner / " + rate);
            Scheduler.Start(standings.ExibirTabela(table,null,null,2,true));
            Scheduler.Tick(.1f); standings.Ocultar();
            for (int i = 0; i < 100; i++) Scheduler.Tick(.1f);
            Check(!standings.UltimaExibicaoConcluida && !standings.EstaExibindo, "capture standings cancellation does not complete / " + rate);
        }
        Scheduler.Reset();
        Console.WriteLine("PASS goal queue and standings timing at 30/60 fps Recorder and unscaled fallback, paused match and cancellation");
    }

    private static void ValidateStandings(int broadcastOrder)
    {
        var standings = new GameObject("Standings owner").AddComponent<ClassificacaoUI>();
        standings.manterTabelaFinalNaTela = false;
        standings.tempoEntradaAbertura = standings.tempoEntradaEncerramento = 0f;
        standings.tempoLeituraAbertura = standings.tempoLeituraEncerramento = 0f;
        standings.tempoSaida = 0f;
        foreach (int count in new [] { 2, 8, 10, 15, 20, 26, 28, 30, 32 })
        {
            var table = new List<LigaCampeonato.Linha>();
            var teams = new List<TeamData>();
            var fixtures = new List<LigaCampeonato.Jogo>();
            for (int i = 0; i < count; i++)
            {
                string id = "team_" + i;
                teams.Add(new TeamData { name = id, nomeEquipe = "TEAM " + i });
                table.Add(new LigaCampeonato.Linha { id = id, POS = i + 1, TEAM = "TEAM " + i, P = 1, PTS = 1 });
            }
            for (int i = 0; i < count / 2; i++)
                fixtures.Add(new LigaCampeonato.Jogo { a = teams[i * 2].name, b = teams[i * 2 + 1].name,
                    nomeA = teams[i * 2].nomeEquipe, nomeB = teams[i * 2 + 1].nomeEquipe, registrado = true, golsA = 1 });
            Scheduler.Start(standings.ExibirTabela(table, null, teams, 1, true, fixtures));
            Check(!standings.EstaExibindo && standings.UltimaExibicaoConcluida, "round closing completes / " + count);
            var canvas = (GameObject)Field(standings, "raiz");
            Check(canvas.GetComponent<Canvas>().sortingOrder > broadcastOrder, "real standings cover broadcast / " + count);
            var panel = Rect(canvas.transform, "Panel");
            for (int i = 0; i < count; i++)
            {
                var row = Rect(panel, "Team " + i);
                Check(-row.anchoredPosition.y + row.sizeDelta.y < 898f, "standings row before footer / " + count + "/" + i);
                Check(Text(row,"Column 1").text == "TEAM " + i, "all standings teams shown / " + count + "/" + i);
            }
            for (int i = 0; i < fixtures.Count; i++)
            {
                var row = Rect(panel, "Match " + (i + 1));
                Check(-row.anchoredPosition.y + row.sizeDelta.y < 898f, "fixture row before footer / " + count + "/" + i);
                var home = Rect(row, "Home Team"); var away = Rect(row, "Away Team");
                Check(-home.anchoredPosition.y + home.sizeDelta.y <= -away.anchoredPosition.y, "fixture names do not overlap / " + count + "/" + i);
                Check(-away.anchoredPosition.y + away.sizeDelta.y <= row.sizeDelta.y, "both fixture names contained / " + count + "/" + i);
                var homeGoals = Rect(row, "Home Goals"); var awayGoals = Rect(row, "Away Goals");
                Check(-homeGoals.anchoredPosition.y + homeGoals.sizeDelta.y <= -awayGoals.anchoredPosition.y, "fixture scores do not overlap / " + count + "/" + i);
                Check(-awayGoals.anchoredPosition.y + awayGoals.sizeDelta.y <= row.sizeDelta.y, "fixture scores contained / " + count + "/" + i);
                var badge = Rect(row, "Away Badge");
                Check(-badge.anchoredPosition.y + badge.sizeDelta.y <= row.sizeDelta.y, "fixture crests contained / " + count + "/" + i);
                Check(Text(row,"Home Team").text == fixtures[i].nomeA && Text(row,"Away Team").text == fixtures[i].nomeB, "fixture names retained / " + count + "/" + i);
            }
            standings.Ocultar();
            Check(!canvas.activeSelf, "next match releases opaque standings background / " + count);
            Scheduler.Start(standings.ExibirTabela(table, table, teams, 2, false, fixtures));
            Check(standings.UltimaExibicaoConcluida && !standings.EstaExibindo, "following round can open / " + count);
            standings.Ocultar();
        }
        Console.WriteLine("PASS actual outside-UI layering and 2/8/10/15/20/26/28/30/32-team standings with next-round continuation");
    }
}
