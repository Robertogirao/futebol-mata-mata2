using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FutebolDeDiscos;
using TMPro;
using UnityEngine;

// Executes the actual match, ball, goal, clock and HUD sources with injected
// positions/contacts. Native collision response and rendering are not modeled.
public static class CornerRestartHarness
{
    private static int checks;
    private static string scenario;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(scenario + ": " + message);
    }

    private static void Call(object target, string name, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null) throw new Exception("Missing method " + name);
        try { method.Invoke(target, args); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }

    private static void Clean()
    {
        foreach (GameObject item in GameObject.All.Where(g => !g.destroyed && g.transform.parent == null).ToArray())
            UnityEngine.Object.Destroy(item);
        GameObject.All.Clear();
        Scheduler.Reset();
        Debug.Errors.Clear(); Debug.Warnings.Clear(); Debug.Messages.Clear();
        Time.captureDeltaTime = 0f; Time.timeScale = 1f;
    }

    private static TeamData Team(string id, Color color)
    {
        var team = ScriptableObject.CreateInstance<TeamData>();
        team.name = team.nomeEquipe = id; team.sigla = id;
        team.corPrincipal = color;
        team.elenco.Add(new JogadorDados { nomeJogador = id + " Captain" });
        return team;
    }

    private sealed class Match
    {
        public readonly GameObject Root;
        public readonly ProjetoFutebolDiscos Config;
        public readonly UniversalUI Ui;
        public readonly PartidaDiscos Engine;
        public readonly GolDiscos Left, Right;
        public readonly TeamData Home, Away;
        public readonly List<JogadorDados> HomePlayers, AwayPlayers;

        public Match(bool knockout = false, bool largeActors = false, bool restrictHalf = true)
        {
            Root = new GameObject("Corner restart fixture");
            Config = Root.AddComponent<ProjetoFutebolDiscos>();
            Config.formato = knockout ? FormatoDiscos.MataMata : FormatoDiscos.Liga;
            Config.tema = ScriptableObject.CreateInstance<TemaCompeticao>();
            Config.tema.fonteTexto = Config.tema.fonteTitulo = TMP_Settings.defaultFontAsset;
            Config.limitarDiscosNaMetade = restrictHalf;
            if (largeActors) { Config.tamanhoBola = .65f; Config.tamanhoDisco = 1.4f; Config.discosPorTime = 5; }
            Ui = Root.AddComponent<UniversalUI>();
            Ui.ativarGoldenGoal = knockout;
            Ui.duracaoRealEmSegundos = 45f; Ui.tempoCongeladoPosJogo = 0f;
            var banner = Root.AddComponent<GoalBannerUI>();
            banner.tempoDeExibicao = .1f; banner.intervaloEntreBanners = 0f;
            var field = Root.AddComponent<CampoDiscos>();
            var hud = Root.AddComponent<HudFutebolDiscos>();
            Engine = Root.AddComponent<PartidaDiscos>();
            field.Construir(Config); hud.Construir(Config, Ui, banner); Engine.Configurar(Config, field, hud);
            Home = Team("Home", Color.blue); Away = Team("Away", Color.red);
            HomePlayers = Home.ObterTitularesDaPartida(); AwayPlayers = Away.ObterTitularesDaPartida();
            Engine.PrepararPartida(Home, Away, HomePlayers, AwayPlayers, Color.blue, Color.red);
            Ui.ConfigurarPartida(Home, Away, null, HomePlayers, AwayPlayers);
            Ui.IniciarCronometro();
            Engine.IniciarPartida(Home, Away, 45f, HomePlayers, AwayPlayers, Color.blue, Color.red);
            Left = Root.GetComponentsInChildren<GolDiscos>().Single(g => g.esquerda);
            Right = Root.GetComponentsInChildren<GolDiscos>().Single(g => !g.esquerda);
            Release();
        }

        public string State { get { return Root.GetComponentsInChildren<TextMeshProUGUI>().Single(t => t.gameObject.name == "Match State").text; } }
        public DiscoAtleta Disc(bool home) { return Engine.Atletas.First(d => d.TimeA == home); }
        public void Touch(bool home) { Engine.Bola.RegistrarToque(Disc(home)); }
        public void Place(Vector2 position, Vector2 velocity)
        {
            Engine.Bola.transform.position = position;
            Engine.Bola.Corpo.position = position;
            Engine.Bola.Corpo.linearVelocity = velocity;
        }
        public void Hold(Vector2 position, float seconds, bool jitter = false, bool fast = false)
        {
            int frames = (int)Math.Ceiling(seconds / Time.fixedDeltaTime);
            for (int i = 0; i < frames && !Engine.EmReposicao; i++)
            {
                Vector2 offset = jitter ? new Vector2((i % 2 == 0 ? 1f : -1f) * .07f, (i % 3 - 1) * .06f) : Vector2.zero;
                Place(position + offset, fast ? new Vector2(i % 2 == 0 ? 12f : -12f, 7f) : Vector2.zero);
                Call(Engine, "FixedUpdate");
            }
        }
        public void Video(float seconds)
        {
            Time.captureDeltaTime = .05f;
            for (int i = 0; i < (int)Math.Ceiling(seconds / .05f); i++)
            {
                Call(Ui, "Update"); Call(Engine, "Update"); Scheduler.Tick(.001f);
            }
        }
        public void Release()
        {
            for (int i = 0; i < 8 && (i < 2 || Engine.EmReposicao); i++)
            {
                Call(Engine, "FixedUpdate");
                if (Engine.EmReposicao) Physics2D.Simulate(Time.fixedDeltaTime);
            }
        }
        public void Sensor(bool left)
        {
            Call(left ? Left : Right, "OnTriggerStay2D", Engine.Bola.GetComponent<CircleCollider2D>());
        }
        public void Goal(bool left)
        {
            Place(new Vector2((left ? -1f : 1f) * (CampoDiscos.MeioComprimento + .65f), 0f), Vector2.zero);
            Sensor(left);
        }
    }

    private static Vector2 Corner(int x, int y)
    {
        return new Vector2(x * (CampoDiscos.MeioComprimento - .28f), y * (CampoDiscos.MeioLargura - .28f));
    }

    private static void FourCornersAndPossession()
    {
        foreach (int x in new[] { -1, 1 }) foreach (int y in new[] { -1, 1 }) foreach (bool defenderTouched in new[] { true, false })
        {
            scenario = "corner " + x + "," + y + " defender touched " + defenderTouched;
            var match = new Match(knockout: y > 0, largeActors: !defenderTouched, restrictHalf: x < 0);
            bool defenderHome = x < 0;
            match.Touch(defenderTouched ? defenderHome : !defenderHome);
            match.Hold(Corner(x, y), 1f);
            Check(!match.Engine.EmReposicao, "a wall impact or short visit does not interrupt play");
            match.Hold(Corner(x, y), 2.3f);
            Check(match.Engine.EmReposicao, "sustained lack of progress produces a set piece");
            Check(match.State.Contains(defenderTouched ? "CORNER" : "GOAL KICK"), "English announcement describes the correct restart");
            Check(match.State.ToUpperInvariant().Contains((defenderTouched ? !defenderHome : defenderHome) ? "HOME" : "AWAY"), "announcement names the awarded team");
            Check(match.Ui.GolsA == 0 && match.Ui.GolsB == 0, "awarding possession never awards a goal");
            Vector2 placement = match.Engine.Bola.Corpo.position;
            Check(Math.Sign(placement.x) == x, "restart stays at the trapped end");
            if (defenderTouched)
                Check(Math.Sign(placement.y) == y && Math.Abs(placement.y) > 3.8f && Math.Abs(placement.x) > 9f, "corner placed by the correct corner flag");
            else
                Check(Math.Abs(placement.y) < 1.5f && Math.Abs(placement.x) < CampoDiscos.MeioComprimento - .8f, "goal kick placed inside the goal area away from the end wall");
            Check(!match.Engine.Bola.Corpo.simulated, "announcement freezes the ball");
            foreach (DiscoAtleta actor in match.Engine.Atletas)
            {
                Check(!actor.Corpo.simulated, "announcement freezes every disc");
                Check(Vector2.Distance(actor.Corpo.position, (Vector2)actor.transform.position) < .001f, "disc physical and visible poses agree");
                Check(Vector2.Distance(actor.Corpo.position, placement) > (match.Config.tamanhoDisco + match.Config.tamanhoBola) * .5f + .01f, "restart position has collision clearance even for maximum actor sizes");
            }
            float clock = match.Ui.TempoAtual;
            match.Video(match.Config.pausaReposicaoBolaPresa + .2f);
            Check(match.Ui.TempoAtual == clock && match.Engine.EmReposicao, "clock and scoring stay paused until physical restart confirmation");
            match.Release();
            Check(!match.Engine.EmReposicao && match.Engine.EmAndamento, "confirmed restart resumes the same match");
            Check(match.Engine.Bola.UltimoToque != null && match.Engine.Bola.UltimoToque.TimeA == (defenderTouched ? !defenderHome : defenderHome), "automatic kick is attributed to the awarded side");
            Vector2 velocity = match.Engine.Bola.Corpo.linearVelocity;
            Check(velocity.x * x < 0f && (!defenderTouched || velocity.y * y < 0f), "kick travels infield instead of into the trapping walls");
            Check(velocity.magnitude > 0f && velocity.magnitude <= match.Config.velocidadeMaximaBola + .001f, "restart has a useful bounded launch speed");
            Check(match.Ui.GolsA == 0 && match.Ui.GolsB == 0, "restart leaves the score unchanged");
            Call(match.Ui, "Update");
            Check(match.Ui.TempoAtual > clock, "clock resumes after release without resetting");
            Clean();
        }
    }

    private static void NoFalseRestarts()
    {
        scenario = "center and single-boundary stagnation";
        foreach (Vector2 point in new[] { Vector2.zero, new Vector2(0f, 4.8f), new Vector2(10.5f, 0f), new Vector2(8f, 3f) })
        {
            var match = new Match(); match.Touch(true); match.Hold(point, 6.5f);
            Check(!match.Engine.EmReposicao, "stationary ball outside the corner region never creates corner/goal kick"); Clean();
        }
        scenario = "no known player contact";
        {
            var match = new Match(); match.Hold(Corner(1, 1), 7f);
            Check(!match.Engine.EmReposicao, "unknown last contact does not invent an awarded team");
            match.Touch(false); match.Hold(Corner(1, 1), 3.3f);
            Check(match.Engine.EmReposicao, "valid contact permits a later correctly assigned restart"); Clean();
        }
        scenario = "feature disabled";
        {
            var match = new Match(); match.Config.reporBolaPresaNoCanto = false; match.Touch(true);
            match.Hold(Corner(-1, -1), 7f); Check(!match.Engine.EmReposicao, "central toggle disables corner restarts"); Clean();
        }
        scenario = "leaving and changing the corner resets consecutive time";
        {
            var match = new Match(); match.Touch(true); match.Hold(Corner(1, 1), 2f);
            match.Hold(new Vector2(0f, 4.8f), .1f); match.Hold(Corner(1, 1), 2f);
            Check(!match.Engine.EmReposicao, "separate visits do not accumulate a stoppage");
            match.Hold(Corner(-1, -1), 2f);
            Check(!match.Engine.EmReposicao, "switching to another corner starts a fresh interval");
            match.Hold(Corner(-1, -1), 1.3f); Check(match.Engine.EmReposicao, "continuous trapping in the new corner eventually restarts"); Clean();
        }
        scenario = "meaningful progress inside the same corner";
        {
            var match = new Match(); match.Touch(true);
            match.Hold(new Vector2(10.6f, 4.8f), 2f); match.Hold(new Vector2(9.9f, 4.8f), 2f);
            Check(!match.Engine.EmReposicao, "moving along the edge by more than the progress tolerance restarts the timer");
            match.Hold(new Vector2(9.9f, 4.8f), 1.3f); Check(match.Engine.EmReposicao, "actual stagnation after that movement is still resolved"); Clean();
        }
    }

    private static void JitterLastContactAndNoCompetingImpulse()
    {
        scenario = "high-speed local jitter and latest contact";
        var match = new Match();
        match.Touch(false); match.Hold(Corner(-1, 1), 1.6f, true, true);
        // Exercise real ball contact tracking independently of the disc's shot cooldown.
        Call(match.Engine.Bola, "OnCollisionStay2D", new Collision2D { collider = match.Disc(true).GetComponent<CircleCollider2D>() });
        Check(match.Engine.Bola.UltimoToque == match.Disc(true), "sustained contact updates last toucher without requiring a new collision Enter");
        match.Hold(Corner(-1, 1), 1.8f, true, true);
        Check(match.Engine.EmReposicao && match.State.Contains("CORNER"), "small oscillations and possession swaps cannot hide a trapped ball");
        Clean();

        foreach (bool golden in new[] { false, true })
        {
            scenario = "no competing random nudge before restart, golden=" + golden;
            match = new Match(golden);
            if (golden)
            {
                Time.captureDeltaTime = 46f; Call(match.Ui, "Update"); Time.captureDeltaTime = 0f;
                Check(match.Ui.EmProrrogacao && match.Ui.partidaEmAndamento, "real clock transition enters golden goal");
            }
            match.Touch(true);
            int frames = (int)Math.Ceiling(2.9f / Time.fixedDeltaTime);
            for (int i = 0; i < frames; i++)
            {
                match.Place(Corner(-1, -1), Vector2.zero); Call(match.Engine, "FixedUpdate");
                Check(match.Engine.Bola.Corpo.linearVelocity.magnitude < .001f, "legacy inactivity impulse does not interfere with a pending corner decision");
            }
            Check(!match.Engine.EmReposicao, "configured three seconds applies in regulation and golden goal");
            match.Hold(Corner(-1, -1), .4f); Check(match.Engine.EmReposicao, "corner is resolved after the configured interval");
            float clock = match.Ui.TempoAtual; match.Video(1.1f); match.Release();
            Check(match.Ui.TempoAtual == clock && match.Ui.EmProrrogacao == golden, "restart preserves elapsed regulation/extra time and match phase");
            match.Goal(false);
            Check(match.Ui.GolsA == 1 && match.Ui.GolsB == 0, "genuine goal after restart still counts");
            if (golden) Check(!match.Ui.partidaEmAndamento && !match.Engine.EmAndamento, "genuine golden goal ends the match normally");
            Clean();
        }
    }

    private static void PhysicalRearmingAndLifecycle()
    {
        scenario = "stale goal callbacks and malformed restart pose";
        var match = new Match(); match.Touch(true); match.Hold(Corner(-1, 1), 3.3f);
        Check(match.Engine.EmReposicao, "fixture reached corner announcement");
        match.Video(1.1f);
        for (int i = 0; i < 16; i++)
        {
            match.Goal(true); match.Sensor(false); Call(match.Engine, "FixedUpdate");
            Check(match.Ui.GolsA == 0 && match.Ui.GolsB == 0 && match.Engine.EmReposicao, "timer and stale sensor callbacks never rearm scoring at the old goal pose");
        }
        Call(match.Engine, "FixedUpdate");
        Check(match.Engine.EmReposicao, "first valid restart sample alone cannot rearm goals");
        match.Release(); Check(!match.Engine.EmReposicao, "consecutive valid poses eventually release restart");
        match.Goal(false); Check(match.Ui.GolsA == 1, "scoring guard accepts a new legitimate crossing");
        for (int i = 0; i < 16; i++) match.Sensor(false);
        Check(match.Ui.GolsA == 1, "duplicate sensor callbacks still award only one goal");
        Clean();

        scenario = "stop cancels a pending corner and fresh kickoff clears detection";
        match = new Match(); match.Touch(false); match.Hold(Corner(1, -1), 3.3f);
        Check(match.Engine.EmReposicao, "fixture reached pending set piece");
        match.Engine.PararPartida(); match.Video(2f); match.Release();
        Check(!match.Engine.EmAndamento && !match.Engine.EmReposicao && !match.Engine.Bola.Corpo.simulated, "stopped match cannot execute its pending set piece");
        match.Engine.IniciarPartida(match.Home, match.Away, 45f, match.HomePlayers, match.AwayPlayers, Color.blue, Color.red);
        match.Release();
        Check(match.Engine.EmAndamento && !match.Engine.EmReposicao && Vector2.Distance(match.Engine.Bola.Corpo.position, Vector2.zero) < .5f, "fresh kickoff uses center rather than a stale restart target");
        match.Touch(false); match.Hold(Corner(1, -1), 2f);
        Check(!match.Engine.EmReposicao, "fresh match does not inherit old corner elapsed time");
        Clean();
    }

    public static void Main()
    {
        string folder = Path.Combine(Path.GetTempPath(), "disc-corner-restart-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder); Application.persistentDataPath = folder;
        try
        {
            Clean(); FourCornersAndPossession(); NoFalseRestarts();
            JitterLastContactAndNoCompetingImpulse(); PhysicalRearmingAndLifecycle();
            Check(Debug.Errors.Count == 0, "no runtime errors logged");
            Console.WriteLine("PASS " + checks + " trapped-corner restart regression assertions using actual game sources.");
            Console.WriteLine("Injected contact/pose/clock lifecycle; native Unity collisions and rendering were not executed.");
        }
        finally { Clean(); Directory.Delete(folder, true); }
    }
}
