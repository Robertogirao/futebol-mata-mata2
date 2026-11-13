using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FutebolDeDiscos;
using UnityEngine;

// The contract deliberately separates frozen physics poses from Transform poses.
// It exercises lifecycle ordering and duplicate scoring, not native Unity contacts.
public static class GoalResetHarness
{
    private static int checks;
    private static string scenario;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(scenario + ": " + message);
    }

    private static void Call(object value, string method, params object[] args)
    {
        MethodInfo found = value.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (found == null) throw new Exception("Missing method " + method);
        try { found.Invoke(value, args); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }

    private static void Clean()
    {
        foreach (GameObject item in GameObject.All.Where(g => !g.destroyed && g.transform.parent == null).ToArray())
            UnityEngine.Object.Destroy(item);
        GameObject.All.Clear();
        Scheduler.Reset();
        Debug.Errors.Clear();
        Debug.Warnings.Clear();
        Debug.Messages.Clear();
        Time.captureDeltaTime = 0f;
        Time.timeScale = 1f;
    }

    private static TeamData Team(string id, Color color)
    {
        var result = ScriptableObject.CreateInstance<TeamData>();
        result.name = id;
        result.nomeEquipe = id;
        result.corPrincipal = color;
        result.elenco.Add(new JogadorDados { nomeJogador = id + " player" });
        return result;
    }

    private sealed class Match
    {
        public readonly GameObject Root;
        public readonly ProjetoFutebolDiscos Config;
        public readonly PartidaDiscos Engine;
        public readonly UniversalUI Ui;
        public readonly GoalBannerUI Banner;
        public readonly GolDiscos Left, Right;

        public Match()
        {
            Root = new GameObject("Goal reset regression fixture");
            Config = Root.AddComponent<ProjetoFutebolDiscos>();
            Config.tema = ScriptableObject.CreateInstance<TemaCompeticao>();
            Config.tema.fonteTexto = Config.tema.fonteTitulo = TMPro.TMP_Settings.defaultFontAsset;
            Config.pausaAposGol = 0.2f;
            Ui = Root.AddComponent<UniversalUI>();
            Ui.ativarGoldenGoal = false;
            Ui.duracaoRealEmSegundos = 45f;
            Ui.tempoCongeladoPosJogo = 0f;
            Banner = Root.AddComponent<GoalBannerUI>();
            Banner.tempoDeExibicao = 0.1f;
            Banner.intervaloEntreBanners = 0f;
            var field = Root.AddComponent<CampoDiscos>();
            var hud = Root.AddComponent<HudFutebolDiscos>();
            Engine = Root.AddComponent<PartidaDiscos>();
            field.Construir(Config);
            hud.Construir(Config, Ui, Banner);
            Engine.Configurar(Config, field, hud);
            TeamData home = Team("Home", Color.blue), away = Team("Away", Color.red);
            List<JogadorDados> a = home.ObterTitularesDaPartida(), b = away.ObterTitularesDaPartida();
            Engine.PrepararPartida(home, away, a, b, Color.blue, Color.red);
            Ui.ConfigurarPartida(home, away, null, a, b);
            Ui.IniciarCronometro();
            Engine.IniciarPartida(home, away, 45f, a, b, Color.blue, Color.red);
            Left = Root.GetComponentsInChildren<GolDiscos>().Single(g => g.esquerda);
            Right = Root.GetComponentsInChildren<GolDiscos>().Single(g => !g.esquerda);
            ConfirmKickoff();
        }

        public void ConfirmKickoff()
        {
            Call(Engine, "FixedUpdate");
            Physics2D.Simulate(Time.fixedDeltaTime);
            Call(Engine, "FixedUpdate");
        }

        public void PlaceAtGoal(bool left)
        {
            Vector2 goal = new Vector2((left ? -1f : 1f) * (CampoDiscos.MeioComprimento + 0.65f), 0f);
            Engine.Bola.transform.position = new Vector3(goal.x, goal.y, 0f);
            Engine.Bola.Corpo.position = goal;
        }

        public void Sensor(bool left, bool enter = false)
        {
            Call(left ? Left : Right, enter ? "OnTriggerEnter2D" : "OnTriggerStay2D", Engine.Bola.GetComponent<CircleCollider2D>());
        }

        public void Goal(bool left)
        {
            PlaceAtGoal(left);
            Sensor(left, true);
        }

        public void VideoFrame(bool left)
        {
            Time.captureDeltaTime = 0.05f;
            Sensor(left);
            Call(Ui, "Update");
            Call(Engine, "Update");
            Scheduler.Tick(0.001f);
        }
    }

    private static void Teleport(BolaDiscos ball, Vector2 position, Vector2 velocity, bool simulate)
    {
        MethodInfo overload = typeof(BolaDiscos).GetMethod("Repor", new[] { typeof(Vector2), typeof(Vector2), typeof(bool) });
        if (overload != null) overload.Invoke(ball, new object[] { position, velocity, simulate });
        else { ball.Repor(position, velocity); ball.Corpo.simulated = simulate; }
    }

    private static void FrozenPoseRegression()
    {
        scenario = "frozen physics pose differs from visible pose";
        var match = new Match();
        BolaDiscos ball = match.Engine.Bola;
        match.PlaceAtGoal(true);
        Vector2 oldGoal = ball.Corpo.position;
        ball.Corpo.simulated = false;
        ball.Corpo.position = Vector2.zero;
        Check(Vector2.Distance(ball.Corpo.position, Vector2.zero) < 0.001f &&
              Vector2.Distance((Vector2)ball.transform.position, oldGoal) < 0.001f,
              "adversarial facade preserves a stale Transform when a frozen body alone is moved");
        ball.Corpo.simulated = true;
        Check(Vector2.Distance(ball.Corpo.position, oldGoal) < 0.001f,
              "original body-only reset order restores the stale goal pose in this lifecycle model");
        ball.Corpo.simulated = false;
        Teleport(ball, Vector2.zero, new Vector2(1.6f, 0f), true);
        Check(Vector2.Distance(ball.Corpo.position, Vector2.zero) < 0.001f &&
              Vector2.Distance((Vector2)ball.transform.position, Vector2.zero) < 0.001f,
              "actual corrected ball reset synchronizes visible and physical poses before reactivation");
        Check(ball.Corpo.simulated && Math.Abs(ball.Corpo.linearVelocity.x - 1.6f) < 0.001f,
              "kickoff velocity survives reactivation");
        foreach (DiscoAtleta disc in match.Engine.Atletas)
        {
            disc.Corpo.position = new Vector2(4f, 4f);
            disc.Congelar(true);
            Vector2 expected = match.Engine.Formacao(disc.TimeA, disc.Indice);
            disc.Repor(expected, true);
            Check(Vector2.Distance(disc.Corpo.position, expected) < 0.001f &&
                  Vector2.Distance((Vector2)disc.transform.position, expected) < 0.001f,
                  "team disc reset also survives frozen-body reactivation");
        }
        Clean();
    }

    private static void RepeatedStayAndPhysicalRearming()
    {
        scenario = "one goal through repeated callbacks and delayed kickoff confirmation";
        var match = new Match();
        match.Goal(true);
        Check(match.Ui.GolsA == 0 && match.Ui.GolsB == 1 && match.Engine.EmReposicao,
              "first complete left crossing awards exactly one away goal");
        float elapsed = match.Ui.TempoAtual;
        for (int frame = 0; frame < 240; frame++)
        {
            match.VideoFrame(true);
            Check(match.Ui.GolsB == 1, "repeated Stay callbacks over multiple pause durations cannot duplicate the goal");
        }
        Check(match.Engine.EmReposicao && match.Ui.TempoAtual == elapsed,
              "ending the video timer alone neither rearms scoring nor resumes the regulation clock");
        Check(Vector2.Distance(match.Engine.Bola.Corpo.position, Vector2.zero) < 0.001f &&
              Vector2.Distance((Vector2)match.Engine.Bola.transform.position, Vector2.zero) < 0.001f,
              "post-goal kickoff teleports the same ball to center in both representations");

        // Deliberately restore the failed goal pose before every verification step.
        // A timer-based gate would score again; this gate must await a valid kickoff.
        for (int frame = 0; frame < 40; frame++)
        {
            match.PlaceAtGoal(true);
            match.Sensor(true);
            Call(match.Engine, "FixedUpdate");
            match.Sensor(true);
            Check(match.Ui.GolsB == 1 && match.Engine.EmReposicao,
                  "a failed reset inside the sensor is repaired without reopening scoring");
        }

        Teleport(match.Engine.Bola, Vector2.zero, new Vector2(1.6f, 0f), true);
        Call(match.Engine, "FixedUpdate");
        Check(match.Engine.EmReposicao, "first valid center sample does not immediately rearm the goal");
        match.PlaceAtGoal(true);
        match.Sensor(true);
        Call(match.Engine, "FixedUpdate");
        Check(match.Ui.GolsB == 1 && match.Engine.EmReposicao,
              "a stale goal pose between confirmation samples cannot create a second goal");

        match.ConfirmKickoff();
        Check(!match.Engine.EmReposicao, "two consecutive valid physical samples release the kickoff");
        match.Goal(false);
        Check(match.Ui.GolsA == 1 && match.Ui.GolsB == 1,
              "a genuine new crossing after a verified kickoff still scores for the other team");
        for (int frame = 0; frame < 20; frame++) match.VideoFrame(false);
        match.ConfirmKickoff();
        match.Goal(true);
        Check(match.Ui.GolsA == 1 && match.Ui.GolsB == 2,
              "the guard permits multiple legitimate goals across successive verified kickoffs");
        match.Engine.PararPartida();
        for (int frame = 0; frame < 20; frame++) match.Sensor(true);
        Check(match.Ui.GolsB == 2, "late callbacks after stopping the match never change its result");
        Clean();
    }

    public static void Main()
    {
        string folder = Path.Combine(Path.GetTempPath(), "disc-goal-reset-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        Application.persistentDataPath = folder;
        try
        {
            Clean();
            FrozenPoseRegression();
            RepeatedStayAndPhysicalRearming();
            Console.WriteLine("PASS " + checks + " goal-reset regression assertions using actual game sources.");
            Console.WriteLine("Adversarial frozen-body/Transform lifecycle and injected callbacks; native Unity physics and rendering were not executed.");
        }
        finally { Clean(); Directory.Delete(folder, true); }
    }
}
