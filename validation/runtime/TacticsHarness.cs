using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FutebolDeDiscos;
using TMPro;
using UnityEngine;

/// <summary>Actual tactical/game sources; controlled poses and API facade, not native collision simulation.</summary>
public static class TacticsHarness
{
    private static int checks;
    private static string scenario;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(scenario + ": " + message + " Errors: " + string.Join(" | ", Debug.Errors.ToArray()));
    }

    private static void Call(object value, string method)
    {
        MethodInfo found = value.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (found == null) throw new Exception("Missing method " + method);
        try { found.Invoke(value, null); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }

    private static void Clean()
    {
        foreach (GameObject item in GameObject.All.Where(g => !g.destroyed && g.transform.parent == null).ToArray())
            UnityEngine.Object.Destroy(item);
        GameObject.All.Clear(); Scheduler.Reset();
        Time.captureDeltaTime = 0f; Time.timeScale = 1f;
        Debug.Errors.Clear(); Debug.Warnings.Clear(); Debug.Messages.Clear();
    }

    private static TeamData Team(string id)
    {
        var result = ScriptableObject.CreateInstance<TeamData>();
        result.name = result.nomeEquipe = id;
        result.sigla = id;
        for (int index = 0; index < 5; index++)
            result.elenco.Add(new JogadorDados { nomeJogador = id + " player " + index });
        return result;
    }

    private sealed class Match
    {
        public readonly GameObject Root;
        public readonly ProjetoFutebolDiscos Config;
        public readonly PartidaDiscos Engine;
        public readonly UniversalUI Ui;
        public readonly GoalBannerUI Banner;
        public readonly TeamData A, B;

        public Match(int discs = 3)
        {
            Root = new GameObject("Tactical pressing fixture");
            Config = Root.AddComponent<ProjetoFutebolDiscos>();
            Config.tema = ScriptableObject.CreateInstance<TemaCompeticao>();
            Config.tema.fonteTexto = Config.tema.fonteTitulo = TMP_Settings.defaultFontAsset;
            Config.discosPorTime = discs;
            Config.limitarDiscosNaMetade = false;
            Config.pausaAposGol = 0.2f;
            Ui = Root.AddComponent<UniversalUI>();
            Ui.ativarGoldenGoal = false; Ui.duracaoRealEmSegundos = 45f;
            Ui.tempoCongeladoPosJogo = 0f;
            Banner = Root.AddComponent<GoalBannerUI>();
            Banner.tempoDeExibicao = 0.1f; Banner.intervaloEntreBanners = 0f;
            var field = Root.AddComponent<CampoDiscos>();
            var hud = Root.AddComponent<HudFutebolDiscos>();
            Engine = Root.AddComponent<PartidaDiscos>();
            field.Construir(Config); hud.Construir(Config, Ui, Banner);
            Engine.Configurar(Config, field, hud);
            A = Team("Home"); B = Team("Away");
            var a = A.ObterTitularesDaPartida(); var b = B.ObterTitularesDaPartida();
            Engine.PrepararPartida(A, B, a, b, Color.blue, Color.red);
            Ui.ConfigurarPartida(A, B, null, a, b);
            Ui.IniciarCronometro();
            Engine.IniciarPartida(A, B, 45f, a, b, Color.blue, Color.red);
            ConfirmKickoff();
        }

        public DiscoAtleta[] Actors(bool home)
        {
            return Engine.Atletas.Where(a => a.TimeA == home).OrderBy(a => a.Indice).ToArray();
        }

        public void ConfirmKickoff()
        {
            Call(Engine, "FixedUpdate");
            Physics2D.Simulate(Time.fixedDeltaTime);
            Call(Engine, "FixedUpdate");
        }

        public void Ball(bool home, float progress, float towardOpponent = 0f)
        {
            float sign = home ? 1f : -1f;
            Engine.Bola.Repor(new Vector2(sign * progress, 0f), new Vector2(sign * towardOpponent, 0f), true);
        }

        public void Assign()
        {
            TaticaDiscos.Atualizar(Engine.Atletas.ToList(), Engine.Bola, Config);
        }

        public void HomeFormation()
        {
            foreach (DiscoAtleta actor in Engine.Atletas)
                actor.Repor(Engine.Formacao(actor.TimeA, actor.Indice), true);
        }
    }

    private static readonly PosturaTatica[] Postures =
    {
        PosturaTatica.UltraDefensivo, PosturaTatica.Defesa, PosturaTatica.Balanceado, PosturaTatica.Ataque
    };

    private static void EnumAndLimits()
    {
        scenario = "serialized identities and maximum pressing quotas";
        Check((int)PosturaTatica.Ataque == 0 && (int)PosturaTatica.Balanceado == 1 &&
              (int)PosturaTatica.Defesa == 2 && (int)PosturaTatica.UltraDefensivo == 3,
              "new posture preserves all three existing serialized values");
        Check(Enum.GetValues(typeof(PosturaTatica)).Length == 4, "the team sheet offers exactly four postures");
        Check(TaticaDiscos.LimitePressao((PosturaTatica)99, 3) == 0,
              "an invalid serialized posture conservatively releases nobody");
        for (int count = 1; count <= 5; count++)
        {
            for (int index = 0; index < Postures.Length; index++)
            {
                int quota = Math.Min(index, count);
                Check(TaticaDiscos.LimitePressao(Postures[index], count) == quota,
                      "quota is 0/1/2/3, bounded by the number of actual discs");
                foreach (bool home in new[] { true, false })
                {
                    float sign = home ? 1f : -1f;
                    Check(TaticaDiscos.QuantidadeDesejada(Postures[index], count, true, home,
                              new Vector2(sign * 8f, 0f), new Vector2(sign * 3f, 0f)) == 0,
                          "global own-half restriction overrides even an attacking opportunity");
                    Check(TaticaDiscos.QuantidadeDesejada(Postures[index], count, false, home,
                              new Vector2(sign * -4f, 0f), Vector2.zero) == 0,
                          "ball threatening own half withdraws all forward pressure");
                    Check(TaticaDiscos.QuantidadeDesejada(Postures[index], count, false, home,
                              new Vector2(sign * 0.35f, 0f), Vector2.zero) == 0,
                          "a ball on the halfway entry margin is not yet an opportunity to leave home");
                    Check(TaticaDiscos.QuantidadeDesejada(Postures[index], count, false, home,
                              new Vector2(sign * 1f, 0f), Vector2.zero) == Math.Min(1, quota),
                          "a shallow opportunity admits only one player, not the full posture quota");
                    Check(TaticaDiscos.QuantidadeDesejada(Postures[index], count, false, home,
                              new Vector2(sign * 3f, 0f), Vector2.zero) == Math.Min(2, quota),
                          "a developed opportunity can admit a second supporting player");
                    Check(TaticaDiscos.QuantidadeDesejada(Postures[index], count, false, home,
                              new Vector2(sign * 7f, 0f), Vector2.zero) == quota,
                          "a strong forward opportunity can use all allowed pressers");
                    Check(TaticaDiscos.QuantidadeDesejada(Postures[index], count, false, home,
                              new Vector2(sign * 2f, 0f), new Vector2(sign * -6f, 0f)) == 0,
                          "a fast ball returning toward own goal cancels shallow pressure");
                    int deepReturning = TaticaDiscos.QuantidadeDesejada(Postures[index], count, false, home,
                        new Vector2(sign * 7f, 0f), new Vector2(sign * -6f, 0f));
                    Check(deepReturning >= 0 && deepReturning <= quota && deepReturning <= 2,
                          "a deep but rapidly returning ball reduces the pressure commitment");
                }
            }
        }
    }

    private static void ActualTeamsAndMoments()
    {
        scenario = "real team assignments from changing game situations";
        for (int count = 1; count <= 5; count++)
        {
            var match = new Match(count);
            for (int index = 0; index < Postures.Length; index++)
            {
                match.A.posturaTatica = match.B.posturaTatica = Postures[index];
                int quota = Math.Min(index, count);
                foreach (bool home in new[] { true, false })
                {
                    match.HomeFormation();
                    match.Ball(home, 7f);
                    match.Assign();
                    DiscoAtleta[] attacking = match.Actors(home), defending = match.Actors(!home);
                    Check(attacking.Count(a => a.Pressionando) == quota,
                          "strong opportunity assigns the correct actual actor count on either side");
                    Check(defending.All(a => !a.Pressionando), "the threatened team does not attack the other half");
                    Check(attacking.All(a => !(a.Pressionando && a.Recuando)), "an actor never receives conflicting pressure and withdrawal roles");
                    match.Ball(home, -3f);
                    match.Assign();
                    Check(attacking.All(a => !a.Pressionando), "opportunity loss withdraws pressure instead of keeping it permanently active");
                    match.Ball(home, 7f);
                    match.Config.limitarDiscosNaMetade = true;
                    match.Assign();
                    Check(match.Engine.Atletas.All(a => !a.Pressionando), "global restriction suppresses both teams' pressure assignments");
                    match.Config.limitarDiscosNaMetade = false;
                }
            }
            Check(Debug.Errors.Count == 0, "ordinary posture transitions emit no integration errors");
            Clean();
        }
    }

    private static void StableSelectionAndReturningSlots()
    {
        scenario = "stable selection and occupied slots during physical return";
        var match = new Match();
        match.A.posturaTatica = PosturaTatica.Defesa;
        match.B.posturaTatica = PosturaTatica.UltraDefensivo;
        DiscoAtleta[] team = match.Actors(true);
        foreach (DiscoAtleta actor in team) actor.Repor(new Vector2(-3f, 0f), true);
        match.Ball(true, 1f); match.Assign();
        Check(team[0].Pressionando && team.Count(a => a.Pressionando) == 1,
              "equal-distance selection uses a deterministic disc-index tie break");
        team[0].Corpo.position = new Vector2(-2f, 0f);
        team[1].Corpo.position = new Vector2(-1.8f, 0f);
        match.Assign();
        Check(team[0].Pressionando && !team[1].Pressionando,
              "small distance changes do not make pressing ownership oscillate every step");

        team[0].Corpo.position = new Vector2(-0.45f, 4f); // Its edge has crossed the center line.
        team[1].Corpo.position = new Vector2(-0.55f, 0f);
        match.Assign();
        Check(!team[1].Pressionando && team.Count(a => a.Pressionando) <= 1,
              "a partly crossed disc occupies the only quota slot despite a closer inside replacement");
        match.Ball(true, -3f); match.Assign();
        Check(team[0].Recuando && !team[0].Pressionando, "the crossed player is told to return after a defensive threat");
        team[0].Corpo.position = new Vector2(3f, 0f);
        team[0].Corpo.linearVelocity = Vector2.zero;
        Vector2 before = team[0].Corpo.position;
        team[0].Mover(0f, Time.fixedDeltaTime, match.Engine.Atletas.ToList(), match.Engine.Bola, false);
        Check(Vector2.Distance(before, team[0].Corpo.position) < 0.001f,
              "normal tactical withdrawal does not teleport a player back to the halfway line");
        Check(team[0].Corpo.linearVelocity.x < 0f, "the returning player actually accelerates toward its own half");

        team[0].Corpo.position = new Vector2(-0.8f, 0f);
        team[1].Corpo.position = new Vector2(-0.55f, 0f);
        match.Ball(true, -3f); match.Assign();
        Check(!team[0].Recuando, "a player that has fully returned releases its reserved slot");
        match.Ball(true, 1f); match.Assign();
        Check(team[1].Pressionando && !team[0].Pressionando,
              "a replacement can acquire the freed quota slot after the original player gets home");
        Clean();
    }

    private static void AdvanceAndGoalReset()
    {
        scenario = "real movement supports the three-player attack and resets after a goal";
        foreach (bool home in new[] { true, false })
        {
            var match = new Match();
            match.A.posturaTatica = match.B.posturaTatica = PosturaTatica.Ataque;
            DiscoAtleta[] team = match.Actors(home);
            float sign = home ? 1f : -1f;
            foreach (DiscoAtleta actor in team) actor.Repor(new Vector2(sign * -2f, 0f), true);
            match.Ball(home, 7f); match.Assign();
            Check(team.All(a => a.Pressionando) && team.Single(a => a.Capitao).Pressionando,
                  "attack can release all three discs, including the photo captain");
            foreach (DiscoAtleta actor in team)
            {
                actor.Corpo.linearVelocity = Vector2.zero;
                actor.Mover(0f, Time.fixedDeltaTime, match.Engine.Atletas.ToList(), match.Engine.Bola, false);
            }
            Check(team.All(a => sign * a.Corpo.linearVelocity.x > 0f), "all permitted actors accelerate toward the opponent's half");
            Check(team.Any(a => a.Corpo.linearVelocity.y > 0.001f) && team.Any(a => a.Corpo.linearVelocity.y < -0.001f),
                  "supporting pressers use distinct lanes rather than all chasing the same point");

            team[0].Corpo.position = new Vector2(sign * 3f, 0f);
            team[0].Corpo.linearVelocity = Vector2.zero;
            match.Ball(home, -3f); match.Assign();
            Vector2 beforeRetreat = team[0].Corpo.position;
            team[0].Mover(0f, Time.fixedDeltaTime, match.Engine.Atletas.ToList(), match.Engine.Bola, false);
            Check(team[0].Recuando && !team[0].Pressionando &&
                  Vector2.Distance(beforeRetreat, team[0].Corpo.position) < 0.001f &&
                  sign * team[0].Corpo.linearVelocity.x < 0f,
                  "withdrawal on either side accelerates home without a halfway-line teleport");
            match.Ball(home, 7f); match.Assign();

            match.Engine.Bola.RegistrarToque(team[0]);
            match.Engine.Bola.Repor(new Vector2(sign * (CampoDiscos.MeioComprimento + 0.7f), 0f), Vector2.zero, true);
            match.Engine.NotificarGol(!home);
            Check(match.Engine.EmReposicao && (home ? match.Ui.GolsA : match.Ui.GolsB) == 1,
                  "pressing does not interfere with the normal single-goal scoring guard");
            Time.captureDeltaTime = 0.1f;
            for (int frame = 0; frame < 4; frame++) { Call(match.Engine, "Update"); Scheduler.Tick(0.001f); }
            Check(match.Engine.Atletas.All(a => !a.Pressionando && !a.Recuando),
                  "kickoff reposition clears all tactical roles from the previous attack");
            match.ConfirmKickoff();
            Check(!match.Engine.EmReposicao && match.Engine.Atletas.All(a => !a.Pressionando),
                  "a fresh centered kickoff does not inherit an attacking role assignment");

            match.Config.limitarDiscosNaMetade = true;
            match.Ball(home, 7f); match.Assign();
            DiscoAtleta boundary = match.Actors(home)[0];
            boundary.Corpo.position = new Vector2(-sign * match.Config.tamanhoDisco * 0.5f, 0f);
            boundary.Corpo.linearVelocity = new Vector2(sign * 4f, 0f);
            boundary.Mover(0f, Time.fixedDeltaTime, match.Engine.Atletas.ToList(), match.Engine.Bola, false);
            Check(!boundary.Pressionando && sign * boundary.Corpo.linearVelocity.x <= 0f,
                  "global half lock also suppresses outward momentum at either center boundary");
            Clean();
        }
    }

    public static void Main()
    {
        string folder = Path.Combine(Path.GetTempPath(), "disc-tactics-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder); Application.persistentDataPath = folder;
        try
        {
            Clean(); EnumAndLimits(); ActualTeamsAndMoments(); StableSelectionAndReturningSlots(); AdvanceAndGoalReset();
            Console.WriteLine("PASS " + checks + " tactical quota, opportunity, retreat, role hand-off and reset assertions using actual game sources.");
            Console.WriteLine("Controlled actor/ball poses and Unity API facade; native collision responses and rendered movement were not executed.");
        }
        finally { Clean(); Directory.Delete(folder, true); }
    }
}
