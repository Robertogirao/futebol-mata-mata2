using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FutebolDeDiscos;
using TMPro;
using UnityEngine;

/// <summary>Actual game sources; Unity API adapters do not render or solve collisions.</summary>
public static class CaptainHarness
{
    private static int checks;
    private static string scenario;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(scenario + ": " + message + " Errors: " + string.Join(" | ", Debug.Errors.ToArray()));
    }

    private static object Call(object instance, string name)
    {
        MethodInfo method = instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null) throw new Exception("Missing method: " + name);
        try { return method.Invoke(instance, null); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }

    private static void Clean()
    {
        foreach (GameObject go in GameObject.All.Where(g => g.transform.parent == null && !g.destroyed).ToArray())
            UnityEngine.Object.Destroy(go);
        GameObject.All.Clear();
        Scheduler.Reset();
        Time.captureDeltaTime = 0f;
        Time.timeScale = 1f;
        Debug.Errors.Clear(); Debug.Warnings.Clear(); Debug.Messages.Clear();
    }

    private static TeamData Team(string id, int count)
    {
        TeamData team = ScriptableObject.CreateInstance<TeamData>();
        team.name = id;
        team.nomeEquipe = id + " United";
        team.sigla = id.ToUpperInvariant();
        team.escudoOuBandeira = new Sprite { name = id + " flag" };
        for (int index = 0; index < count; index++)
            team.elenco.Add(new JogadorDados
            {
                nomeJogador = id + " Player " + (index + 1),
                foto512x512 = new Sprite { name = id + " portrait " + (index + 1) }
            });
        return team;
    }

    private sealed class Match
    {
        public readonly GameObject Root;
        public readonly ProjetoFutebolDiscos Config;
        public readonly UniversalUI Ui;
        public readonly GoalBannerUI Banner;
        public readonly PartidaDiscos Engine;
        public readonly TeamData A, B;
        public List<JogadorDados> RepsA, RepsB;

        public Match(bool tags = true, bool captainMarker = true, bool captainPortrait = true, int discs = 3, int players = 3)
        {
            Root = new GameObject("Captain and player identity fixture");
            Config = Root.AddComponent<ProjetoFutebolDiscos>();
            Config.tema = ScriptableObject.CreateInstance<TemaCompeticao>();
            Config.tema.fonteTexto = Config.tema.fonteTitulo = TMP_Settings.defaultFontAsset;
            Config.mostrarTagsJogadores = tags;
            Config.mostrarMarcaCapitao = captainMarker;
            Config.usarFotoNoDiscoCapitao = captainPortrait;
            Config.tamanhoFonteTags = 4.5f;
            Config.larguraTags = 2.6f;
            Config.discosPorTime = discs;
            Config.pausaAposGol = 0.2f;
            Ui = Root.AddComponent<UniversalUI>();
            Ui.ativarGoldenGoal = false;
            Ui.duracaoRealEmSegundos = 45f;
            Ui.tempoCongeladoPosJogo = 0f;
            Banner = Root.AddComponent<GoalBannerUI>();
            Banner.tempoDeExibicao = 0.1f;
            Banner.intervaloEntreBanners = 0f;
            CampoDiscos field = Root.AddComponent<CampoDiscos>();
            HudFutebolDiscos hud = Root.AddComponent<HudFutebolDiscos>();
            Engine = Root.AddComponent<PartidaDiscos>();
            field.Construir(Config);
            hud.Construir(Config, Ui, Banner);
            Engine.Configurar(Config, field, hud);
            A = Team("Home", players); B = Team("Away", players);
            RepsA = new List<JogadorDados> { A.elenco[Math.Min(1, A.elenco.Count - 1)] };
            RepsB = new List<JogadorDados> { B.elenco[Math.Min(1, B.elenco.Count - 1)] };
            PrepareAndStart();
        }

        public void PrepareAndStart()
        {
            Engine.PrepararPartida(A, B, RepsA, RepsB, Color.blue, Color.red);
            Ui.ConfigurarPartida(A, B, null, RepsA, RepsB);
            Ui.IniciarCronometro();
            Engine.IniciarPartida(A, B, 45f, RepsA, RepsB, Color.blue, Color.red);
            ConfirmKickoff();
        }

        public void ConfirmKickoff()
        {
            Call(Engine, "FixedUpdate");
            Physics2D.Simulate(Time.fixedDeltaTime);
            Call(Engine, "FixedUpdate");
        }

        public void Goal(DiscoAtleta actor, bool left)
        {
            Banner.LimparFila();
            if (actor != null) Engine.Bola.RegistrarToque(actor);
            Vector2 position = new Vector2((left ? -1f : 1f) * (CampoDiscos.MeioComprimento + 0.7f), 0f);
            Engine.Bola.transform.position = new Vector3(position.x, position.y, 0f);
            Engine.Bola.Corpo.position = position;
            Engine.NotificarGol(left);
        }

        public void Restart()
        {
            Time.captureDeltaTime = 0.1f;
            for (int index = 0; index < 4; index++) { Call(Engine, "Update"); Scheduler.Tick(0.001f); }
            ConfirmKickoff();
            Time.captureDeltaTime = 0f;
        }
    }

    private static TMP_Text[] WorldTexts(DiscoAtleta actor)
    {
        return actor.GetComponentsInChildren<TextMeshPro>(true).Cast<TMP_Text>().ToArray();
    }

    private static bool HasVisibleText(DiscoAtleta actor, string expected)
    {
        return WorldTexts(actor).Any(t => t.enabled && t.gameObject.activeInHierarchy &&
            string.Equals(t.text, expected, StringComparison.OrdinalIgnoreCase));
    }

    private static void RolesAndIdentity()
    {
        scenario = "captain identity and distinct roster slots";
        var match = new Match();
        foreach (bool home in new[] { true, false })
        {
            TeamData team = home ? match.A : match.B;
            JogadorDados selected = home ? match.RepsA[0] : match.RepsB[0];
            DiscoAtleta[] actors = match.Engine.Atletas.Where(a => a.TimeA == home).OrderBy(a => a.Indice).ToArray();
            Check(actors.Length == 3 && actors.Count(a => a.Capitao) == 1, "each team has exactly one captain among three discs");
            Check(actors[0].Capitao && actors[0].Jogador == selected, "captain is the chosen photo representative, not necessarily first roster entry");
            Check(actors.Select(a => a.Jogador).Distinct().Count() == 3, "ordinary discs use different roster players");
            Check(actors.Skip(1).All(a => a.Jogador != selected && !a.Capitao), "captain is never silently duplicated on ordinary discs");
            Check(actors[0].ImagemDoGol == selected.foto512x512, "captain goal image is the selected portrait");
            Check(actors.Skip(1).All(a => a.ImagemDoGol == team.escudoOuBandeira), "ordinary goal images use flags even with portraits in their player data");
            Check(actors[0].GetComponentsInChildren<SpriteRenderer>(true).Any(s => s.sprite == selected.foto512x512), "captain disc can display its portrait");
            foreach (DiscoAtleta actor in actors)
            {
                Check(actor.NomeExibicao == actor.Jogador.nomeJogador, "display label uses the actual player name");
                Check(HasVisibleText(actor, actor.NomeExibicao), "every player has a visible name tag");
                Check(WorldTexts(actor).Where(t => string.Equals(t.text, actor.NomeExibicao, StringComparison.OrdinalIgnoreCase)).All(t => !t.richText), "player name labels treat names as plain text");
                Check(HasVisibleText(actor, "C") == actor.Capitao, "captain marker appears on exactly the captain disc");
            }
        }
        Check(Debug.Errors.Count == 0, "normal roster builds without runtime integration errors");
        Clean();
    }

    private static void DisplayOptionsAndMissingPlayers()
    {
        scenario = "identity presentation controls and short rosters";
        var hidden = new Match(false, false, false);
        foreach (DiscoAtleta actor in hidden.Engine.Atletas)
        {
            Check(!HasVisibleText(actor, actor.NomeExibicao), "name tags can be hidden");
            Check(!HasVisibleText(actor, "C"), "captain mark can be hidden");
        }
        foreach (DiscoAtleta captain in hidden.Engine.Atletas.Where(a => a.Capitao))
        {
            Check(!captain.GetComponentsInChildren<SpriteRenderer>(true).Any(s => s.sprite == captain.Jogador.foto512x512), "portrait on the disc can be disabled");
            Check(captain.ImagemDoGol == captain.Jogador.foto512x512, "hiding the on-disc portrait does not remove captain goal identity");
        }
        Clean();

        var shortRoster = new Match(true, true, true, 5, 1);
        foreach (bool home in new[] { true, false })
        {
            TeamData team = home ? shortRoster.A : shortRoster.B;
            DiscoAtleta[] actors = shortRoster.Engine.Atletas.Where(a => a.TimeA == home).OrderBy(a => a.Indice).ToArray();
            Check(actors.Count(a => a.Capitao) == 1 && actors.Count(a => a.Jogador != null) == 1, "a one-player roster never fabricates or duplicates the captain in four extra slots");
            foreach (DiscoAtleta actor in actors.Skip(1))
            {
                Check(actor.NomeExibicao == team.sigla + " " + (actor.Indice + 1), "missing roster slots receive explicit team/disc labels");
                Check(actor.ImagemDoGol == team.escudoOuBandeira && HasVisibleText(actor, actor.NomeExibicao), "unnamed ordinary disc still has a flag and readable label");
            }
        }
        shortRoster.RepsA[0].foto512x512 = null;
        shortRoster.PrepareAndStart();
        Check(shortRoster.Engine.Atletas.Single(a => a.TimeA && a.Capitao).ImagemDoGol == shortRoster.A.escudoOuBandeira,
              "captain without a portrait falls back to its team flag");
        Clean();
    }

    private static void ScorerImagesAndNoAssetMutation()
    {
        scenario = "goal attribution, portrait/flag selection and source data preservation";
        var match = new Match();
        JogadorDados[] originalPlayers = match.A.elenco.Concat(match.B.elenco).ToArray();
        string[] originalNames = originalPlayers.Select(p => p.nomeJogador).ToArray();
        Sprite[] originalPhotos = originalPlayers.Select(p => p.foto512x512).ToArray();
        DiscoAtleta captain = match.Engine.Atletas.Single(a => a.TimeA && a.Capitao);
        DiscoAtleta ordinary = match.Engine.Atletas.First(a => a.TimeA && !a.Capitao);
        match.Goal(captain, false);
        Check(match.Ui.GolsA == 1 && match.Banner.textoDados.text.Contains(captain.NomeExibicao.ToUpperInvariant()), "captain goal names the real touching player");
        Check(match.Banner.fotoJogador.sprite == captain.Jogador.foto512x512, "captain goal banner displays the portrait");
        match.Restart();
        match.Goal(ordinary, false);
        Check(match.Ui.GolsA == 2 && match.Banner.textoDados.text.Contains(ordinary.NomeExibicao.ToUpperInvariant()), "ordinary goal names its own player rather than the captain");
        Check(match.Banner.fotoJogador.sprite == match.A.escudoOuBandeira && match.Banner.fotoJogador.sprite != ordinary.Jogador.foto512x512,
              "ordinary goal banner displays the flag rather than that player's available portrait");
        match.Restart();
        match.Goal(ordinary, true);
        Check(match.Ui.GolsB == 1 && match.Banner.textoDados.text.Contains(ordinary.NomeExibicao.ToUpperInvariant() + " (OG)"), "own goal retains actual touching defender and marks OG");
        Check(match.Banner.fotoJogador.sprite == match.A.escudoOuBandeira, "own-goal ordinary image stays with the real player's team");
        match.Restart();
        match.Goal(null, false);
        Check(match.Ui.GolsA == 3 && match.Banner.textoDados.text.Contains(match.A.nomeEquipe.ToUpperInvariant()), "unknown scorer uses the scoring team without inventing a player");
        Check(match.Banner.fotoJogador.sprite == match.A.escudoOuBandeira, "unknown scorer uses the scoring team's flag");
        Check(originalPlayers.Select(p => p.nomeJogador).SequenceEqual(originalNames) && originalPlayers.Select(p => p.foto512x512).SequenceEqual(originalPhotos),
              "all goal paths leave source player names and photographs unchanged");
        Check(match.A.elenco.Concat(match.B.elenco).SequenceEqual(originalPlayers), "roster ordering is not mutated during assignment or goals");
        Clean();
    }

    private static void QueueSnapshots()
    {
        scenario = "captain and teammate goal queue snapshots";
        var match = new Match();
        DiscoAtleta captain = match.Engine.Atletas.Single(a => a.TimeA && a.Capitao);
        DiscoAtleta ordinary = match.Engine.Atletas.First(a => a.TimeA && !a.Capitao);
        string captainName = captain.NomeExibicao, ordinaryName = ordinary.NomeExibicao;
        Sprite captainImage = captain.ImagemDoGol, ordinaryImage = ordinary.ImagemDoGol;
        match.Banner.LimparFila();
        match.Banner.ExibirBannerDeGol(captainName, captainImage, Color.blue);
        match.Banner.ExibirBannerDeGol(ordinaryName, ordinaryImage, Color.blue);
        captain.Jogador.nomeJogador = "Changed after queue";
        ordinary.Jogador.nomeJogador = "Changed teammate after queue";
        captain.Jogador.foto512x512 = new Sprite();
        ordinary.Jogador.foto512x512 = new Sprite();
        Check(match.Banner.textoDados.text.Contains(captainName.ToUpperInvariant()) && match.Banner.fotoJogador.sprite == captainImage,
              "first captain event keeps the copied name and portrait");
        bool seenSecond = false;
        Time.captureDeltaTime = 0.02f;
        for (int frame = 0; frame < 20 && !seenSecond; frame++)
        {
            Scheduler.Tick(0.001f);
            seenSecond = match.Banner.textoDados.text.Contains(ordinaryName.ToUpperInvariant());
        }
        Check(seenSecond && match.Banner.fotoJogador.sprite == ordinaryImage, "queued teammate event retains its copied name and flag after source changes");
        for (int frame = 0; frame < 30; frame++) Scheduler.Tick(0.001f);
        Check(!match.Banner.TemBannersPendentes, "both queued events finish using the recording clock");
        Clean();
    }

    public static void Main()
    {
        string folder = Path.Combine(Path.GetTempPath(), "disc-captain-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        Application.persistentDataPath = folder;
        try
        {
            Clean();
            RolesAndIdentity();
            DisplayOptionsAndMissingPlayers();
            ScorerImagesAndNoAssetMutation();
            QueueSnapshots();
            Console.WriteLine("PASS " + checks + " captain, player-tag, portrait/flag scorer and queued identity assertions using actual game sources.");
            Console.WriteLine("Unity API facade with controlled goal crossings; native contacts, text rendering and appearance were not executed.");
        }
        finally { Clean(); Directory.Delete(folder, true); }
    }
}
