using System;
using System.Collections.Generic;
using System.Linq;
using FutebolDeDiscos;
using UnityEngine;

public static class Harness
{
    private static int checks;
    private static string scenario;
    private static void Check(bool ok, string message)
    { checks++; if (!ok) throw new Exception(scenario + ": " + message); }
    private static void Reject(Action action, string message)
    {
        bool rejected = false;
        try { action(); } catch (ArgumentException) { rejected = true; } catch (System.IO.InvalidDataException) { rejected = true; }
        Check(rejected, message);
    }
    private static List<TeamData> Teams(int count)
    { return Enumerable.Range(0, count).Select(i => new TeamData { name = "club_" + i.ToString("00") }).ToList(); }
    private static List<CapitaoConvidado> Captains(int count)
    { return Enumerable.Range(0, count).Select(i => new CapitaoConvidado { name = "captain_" + i.ToString("00"), nomeCapitao = "Captain " + i, overall = 75 }).ToList(); }
    private static string Mapping(MotorSorteioCapitaes engine)
    { return string.Join("|", engine.Resultados.OrderBy(r => r.timeId).Select(r => r.timeId + "=" + r.capitaoId).ToArray()); }
    private static string Arrivals(MotorSorteioCapitaes engine)
    { return string.Join("|", engine.Resultados.Select(r => r.capitaoId).ToArray()); }
    private static void Finish(MotorSorteioCapitaes engine, float dt = 1f / 60f)
    {
        int frames = 0;
        while (!engine.Concluido && engine.TempoSimulado < 20f && frames++ < 3000) engine.Avancar(dt);
        Check(engine.Concluido && engine.TempoSimulado < 19f, "all marbles physically cross the finish within a bounded simulation");
    }
    private static void SizesSeedsAndTimeSteps()
    {
        foreach (int n in new[] { 2, 10, 16, 20, 32 }) foreach (int seed in new[] { 0, 7, -173, int.MaxValue, int.MinValue })
        {
            scenario = n + " marbles seed=" + seed;
            var teams = Teams(n); var captains = Captains(n + 3);
            var first = new MotorSorteioCapitaes(teams, captains, null, seed);
            var sixty = new MotorSorteioCapitaes(teams, captains.AsEnumerable().Reverse().ToList(), null, seed);
            Check(first.Marbles.Count == n && first.Marbles.Select(m => m.Capitao).Distinct().Count() == n,
                "selected count equals active club count with no duplicate captain");
            Check(first.Resultados.Count == 0 && first.Marbles.All(m => !m.Concluida && m.TimeAtribuido == null),
                "fresh draw has no preassigned club before the race");
            Check(first.Marbles.All(m => m.Posicao.x >= m.Raio && m.Posicao.x <= MotorSorteioCapitaes.Largura - m.Raio &&
                m.Posicao.y >= m.Raio && m.Posicao.y < 200f), "all selected marbles are visible inside the starting pool");
            Check(first.Obstacles.Select(o => o.Tipo).Distinct().Count() == 3 && first.Obstacles.All(o => o.Ativo),
                "stage contains active pegs, ramps and rotating obstacles");
            Finish(first, 1f / 30f); Finish(sixty, 1f / 60f);
            Check(Mapping(first) == Mapping(sixty) && Arrivals(first) == Arrivals(sixty) && first.TempoSimulado == sixty.TempoSimulado,
                "fixed-step result is identical at 30/60 fps and catalogue order does not bias selection");
            Check(first.Resultados.Count == n && first.Resultados.Select(r => r.timeId).Distinct().Count() == n &&
                first.Resultados.Select(r => r.capitaoId).Distinct().Count() == n, "every club receives exactly one different captain");
            Check(first.Marbles.All(m => m.Concluida && m.TimeAtribuido != null && m.Posicao.y >= MotorSorteioCapitaes.Altura + m.Raio),
                "every revealed assignment follows an actual finish crossing");
            Check(first.Marbles.Sum(m => m.ColisoesObstaculos) > 0, "actual obstacle contacts affect the race");
            foreach (var m in first.Marbles)
            {
                Check(!float.IsNaN(m.Posicao.x) && !float.IsNaN(m.Posicao.y) && !float.IsInfinity(m.Posicao.x) &&
                    m.Posicao.x >= m.Raio && m.Posicao.x <= MotorSorteioCapitaes.Largura - m.Raio,
                    "marble remains finite and contained horizontally");
                Check(first.Resultados.Single(r => r.capitaoId == m.Capitao.Identificador).timeId == m.TimeAtribuido.name,
                    "renderable destination matches the emitted assignment");
            }
            string stable = Mapping(first); float finalTime = first.TempoSimulado;
            first.Avancar(100f);
            Check(Mapping(first) == stable && first.TempoSimulado == finalTime, "completed simulation cannot reveal twice or change results");
        }
    }
    private static void GenuineArrivalAndCollisions()
    {
        scenario = "arrival controls fresh assignment";
        var teams = Teams(10); var catalog = Captains(10);
        var a = new MotorSorteioCapitaes(teams, catalog, null, 31);
        var b = new MotorSorteioCapitaes(teams, catalog, null, 31);
        string firstCaptain = a.Marbles[0].Capitao.Identificador, otherCaptain = b.Marbles[1].Capitao.Identificador;
        a.Marbles[0].Posicao = new Vector2(500f, 650f); b.Marbles[1].Posicao = new Vector2(500f, 650f);
        a.Avancar(MotorSorteioCapitaes.PassoFixo); b.Avancar(MotorSorteioCapitaes.PassoFixo);
        Check(a.Resultados.Count == 1 && b.Resultados.Count == 1 && a.Resultados[0].capitaoId == firstCaptain && b.Resultados[0].capitaoId == otherCaptain,
            "changing which marble crosses first changes the first emitted captain");
        Check(a.Resultados[0].timeId == b.Resultados[0].timeId && a.Resultados[0].capitaoId != b.Resultados[0].capitaoId,
            "first open club is awarded to actual arrival rather than an off-screen preassignment");
        Finish(a); Finish(b); Check(Mapping(a) != Mapping(b), "altering arrivals changes the final pairing");
        scenario = "real peg collision response";
        var engine = new MotorSorteioCapitaes(Teams(2), Captains(2), null, 2);
        var marble = engine.Marbles[0]; var peg = engine.Obstacles.First(o => o.Tipo == MotorSorteioCapitaes.TipoObstaculo.Pino);
        engine.Avancar(.6f); marble.Posicao = peg.Centro + new Vector2(0f, -marble.Raio - peg.Raio + 3f);
        marble.Velocidade = new Vector2(0f, 150f); int oldContacts = marble.ColisoesObstaculos;
        engine.Avancar(MotorSorteioCapitaes.PassoFixo);
        Check(marble.ColisoesObstaculos > oldContacts && marble.Velocidade.y < 0f && Math.Abs(marble.Velocidade.x) > 0f,
            "a peg reflects incoming velocity and deflects centered impacts");
        Check((marble.Posicao - peg.Centro).magnitude >= marble.Raio + peg.Raio - .01f,
            "collision separates the marble from the peg surface");
    }
    private static void ManualReplayAndOverall()
    {
        scenario = "manual reservations and extra catalogue";
        var teams = Teams(10); var catalog = Captains(16);
        var manual = new List<VinculoCapitaoConvidado>
        {
            new VinculoCapitaoConvidado { time = new TeamData { name = teams[0].name }, capitao = catalog[12] },
            new VinculoCapitaoConvidado { time = teams[3], capitao = catalog[15] }
        };
        var engine = new MotorSorteioCapitaes(teams, catalog, manual, 16); Finish(engine);
        Check(engine.Marbles.Count(m => m.Fixa) == 2 && engine.Resultados.Single(r => r.timeId == teams[0].name).capitaoId == catalog[12].Identificador &&
            engine.Resultados.Single(r => r.timeId == teams[3].name).capitaoId == catalog[15].Identificador,
            "manual pairings stay reserved, including equivalent club ID references");
        Check(engine.Resultados.Select(r => r.timeId).Distinct().Count() == teams.Count, "a manual reservation cannot leave its club in the free queue");
        var saved = engine.Resultados.Select(r => new RegistroCapitaoConvidado { timeId = r.timeId, capitaoId = r.capitaoId,
            nomeCapitao = r.nomeCapitao, overall = r.overall }).ToList();
        string savedBytes = string.Join("|", saved.Select(r => r.timeId + r.capitaoId + r.nomeCapitao + r.overall).ToArray());
        var replay = new MotorSorteioCapitaes(teams, catalog, manual, 88888, saved); Finish(replay, .1f);
        Check(replay.Repeticao && Mapping(replay) == Mapping(engine), "replaying stored pairings never rerolls even under a different animation seed");
        Check(savedBytes == string.Join("|", saved.Select(r => r.timeId + r.capitaoId + r.nomeCapitao + r.overall).ToArray()), "replay never mutates stored records");
        Check(replay.Resultados.All(r => !ReferenceEquals(r, saved.First(s => s.capitaoId == r.capitaoId))), "replay emits defensive record copies");
        scenario = "overall does not weight the draw";
        var low = Captains(10); var high = Captains(10); low.ForEach(c => c.overall = 1); high.ForEach(c => c.overall = 100);
        var weaker = new MotorSorteioCapitaes(teams, low, null, 49); var stronger = new MotorSorteioCapitaes(teams, high, null, 49);
        Finish(weaker); Finish(stronger);
        Check(Mapping(weaker) == Mapping(stronger) && Arrivals(weaker) == Arrivals(stronger) && weaker.TempoSimulado == stronger.TempoSimulado,
            "captain gameplay strength never biases marble mass, speed or destination");
        scenario = "new seeds produce new draws";
        var signatures = new HashSet<string>();
        for (int seed = 1; seed <= 12; seed++) { var draw = new MotorSorteioCapitaes(teams, catalog, null, seed); Finish(draw); signatures.Add(Mapping(draw)); }
        Check(signatures.Count == 12, "new seeds vary the actual pairings");
    }
    private static void BoundsAndInvalidData()
    {
        scenario = "endless obstruction cannot hang the ceremony";
        var engine = new MotorSorteioCapitaes(Teams(32), Captains(32), null, 73);
        while (!engine.RetaFinal)
        {
            foreach (var m in engine.Marbles) { m.Posicao = new Vector2(540f + m.Capitao.Identificador.GetHashCode() % 10, 22f); m.Velocidade = Vector2.zero; }
            engine.Avancar(.1f);
        }
        Check(engine.Obstacles.All(o => o.Ativo), "pins remain solid even during final clearance");
        float liberadaEm=engine.TempoSimulado;
        while(!engine.Concluido&&engine.TempoSimulado-liberadaEm<19f)engine.Avancar(1f/60f);
        Check(engine.Concluido,"a previously obstructed pile finishes after release without disabling the obstacles");
        Check(engine.Marbles.All(m => m.Posicao.y >= MotorSorteioCapitaes.Altura + m.Raio), "final clearance still requires each finish crossing");
        scenario = "invalid inputs and stop-safe clock";
        var two = Teams(2); var caps = Captains(2);
        Reject(() => new MotorSorteioCapitaes(null, caps, null, 0), "null team list rejected");
        Reject(() => new MotorSorteioCapitaes(two, Captains(1), null, 0), "insufficient catalogue rejected");
        Reject(() => new MotorSorteioCapitaes(two, caps, new[] { new VinculoCapitaoConvidado { time = two[0], capitao = caps[0] }, new VinculoCapitaoConvidado { time = two[1], capitao = caps[0] } }, 0), "duplicate manual captain rejected");
        Reject(() => new MotorSorteioCapitaes(two, caps, null, 0, new List<RegistroCapitaoConvidado>()), "incomplete replay rejected");
        var clock = new MotorSorteioCapitaes(two, caps, null, 0);
        Reject(() => clock.Avancar(float.NaN), "NaN time rejected"); Reject(() => clock.Avancar(float.PositiveInfinity), "infinite time rejected");
        Reject(() => clock.Avancar(-1f), "negative time rejected"); clock.Avancar(0f);
        Check(clock.TempoSimulado == 0f && clock.Resultados.Count == 0, "zero delta does not move or assign");
    }
    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 d = b - a;
        float length = d.x * d.x + d.y * d.y;
        float t = length < .000001f ? 0f : Math.Max(0f, Math.Min(1f, ((p.x-a.x)*d.x+(p.y-a.y)*d.y)/length));
        return (p-a-d*t).magnitude;
    }
    private static float PinClearance(MotorSorteioCapitaes engine)
    {
        float minimum = float.MaxValue;
        foreach (var m in engine.Marbles.Where(m=>!m.Concluida))
            foreach (var o in engine.Obstacles.Where(o=>o.Ativo))
                minimum=Math.Min(minimum,DistanceToSegment(m.Posicao,o.PontoA,o.PontoB)-m.Raio-o.Raio);
        return minimum;
    }
    private static void SolidObstaclesAndClearPassages()
    {
        scenario="pair separation must not push a marble inside a pin";
        var engine=new MotorSorteioCapitaes(Teams(2),Captains(2),null,2);
        engine.Avancar(.6f);
        var pin=engine.Obstacles.First(o=>o.Tipo==MotorSorteioCapitaes.TipoObstaculo.Pino);
        var lower=engine.Marbles[0];var upper=engine.Marbles[1];
        lower.Posicao=pin.Centro+new Vector2(0f,-lower.Raio-pin.Raio-.02f);
        upper.Posicao=lower.Posicao+new Vector2(0f,-20f);
        lower.Velocidade=upper.Velocidade=Vector2.zero;
        float minimum=float.MaxValue;
        for(int step=0;step<30;step++){engine.Avancar(MotorSorteioCapitaes.PassoFixo);minimum=Math.Min(minimum,PinClearance(engine));}
        Check(minimum>=-.05f,"crowded marble-pair corrections leave every pin and ramp solid after every physical step");

        scenario="fast grazing trajectory intersects a pin between frame endpoints";
        engine=new MotorSorteioCapitaes(Teams(2),Captains(2),null,4);engine.Avancar(.6f);
        pin=engine.Obstacles.First(o=>o.Tipo==MotorSorteioCapitaes.TipoObstaculo.Pino);
        var marble=engine.Marbles[0];engine.Marbles[1].Posicao=new Vector2(1000f,50f);
        marble.Posicao=pin.Centro+new Vector2(-1.8f,-marble.Raio-pin.Raio+.01f);
        marble.Velocidade=new Vector2(430f,0f);int contacts=marble.ColisoesObstaculos;
        engine.Avancar(MotorSorteioCapitaes.PassoFixo);
        Check(marble.ColisoesObstaculos>contacts&&PinClearance(engine)>=-.05f,"continuous/substep contacts catch a grazing path that an endpoint-only check misses");

        scenario="all 32 marbles crowded around one pin";
        engine=new MotorSorteioCapitaes(Teams(32),Captains(32),null,91);engine.Avancar(.6f);
        pin=engine.Obstacles.First(o=>o.Tipo==MotorSorteioCapitaes.TipoObstaculo.Pino);
        for(int i=0;i<engine.Marbles.Count;i++)
        {
            double angle=i*Math.PI*2/engine.Marbles.Count;
            engine.Marbles[i].Posicao=pin.Centro+new Vector2((float)Math.Cos(angle),(float)Math.Sin(angle))*34.02f;
            engine.Marbles[i].Velocidade=Vector2.zero;
        }
        minimum=float.MaxValue;
        for(int step=0;step<120;step++){engine.Avancar(MotorSorteioCapitaes.PassoFixo);minimum=Math.Min(minimum,PinClearance(engine));}
        Check(minimum>=-.05f,"a dense pile cannot force any marble through solid geometry");
        Finish(engine);

        scenario="local balance is freed by motion while pins remain solid";
        engine=new MotorSorteioCapitaes(Teams(2),Captains(2),null,22);engine.Avancar(.6f);
        pin=engine.Obstacles.First(o=>o.Tipo==MotorSorteioCapitaes.TipoObstaculo.Pino);
        marble=engine.Marbles[0];engine.Marbles[1].Posicao=new Vector2(1000f,50f);
        for(int step=0;step<180;step++)
        {
            marble.Posicao=pin.Centro+new Vector2(0f,-marble.Raio-pin.Raio-.02f);
            marble.Velocidade=Vector2.zero;
            engine.Avancar(MotorSorteioCapitaes.PassoFixo);
        }
        Check(marble.Destravamentos>0,"sustained balancing triggers a local sideways/downward velocity release");
        Check(!marble.Concluida&&marble.TimeAtribuido==null&&pin.Ativo,"the release neither teleports to the finish nor assigns a club nor disables the pin");
        Finish(engine);

        scenario="rotor clearances cover the whole rotation rather than the initial angle";
        engine=new MotorSorteioCapitaes(Teams(2),Captains(2),null,21);
        foreach(var rotor in engine.Obstacles.Where(o=>o.Tipo==MotorSorteioCapitaes.TipoObstaculo.Giratorio))
            foreach(var other in engine.Obstacles.Where(o=>!ReferenceEquals(o,rotor)))
            {
                float gap=other.Tipo==MotorSorteioCapitaes.TipoObstaculo.Giratorio
                    ?(rotor.Eixo-other.Eixo).magnitude-rotor.MeiaExtensao-other.MeiaExtensao-rotor.Raio-other.Raio
                    :DistanceToSegment(rotor.Eixo,other.PontoA,other.PontoB)-rotor.MeiaExtensao-rotor.Raio-other.Raio;
                Check(gap>=48f,"a full-width marble has clearance around every rotor throughout its sweep");
            }

        foreach(int count in new[]{10,16,20,32})foreach(int seed in new[]{3,73})
        {
            scenario="solid natural race count="+count+" seed="+seed;
            engine=new MotorSorteioCapitaes(Teams(count),Captains(count),null,seed);
            minimum=float.MaxValue;
            while(!engine.Concluido&&engine.TempoSimulado<19f)
            {
                engine.Avancar(MotorSorteioCapitaes.PassoFixo);
                minimum=Math.Min(minimum,PinClearance(engine));
            }
            Check(minimum>=-.05f,"marbles stay outside every visible obstacle throughout the race");
            Check(engine.Concluido&&engine.TempoSimulado<19f,"all arrivals finish without removing solid pins");
            Check(engine.Obstacles.All(o=>o.Ativo),"the course never disables collisions to force an arrival");
        }
    }
    public static void Main()
    {
        SizesSeedsAndTimeSteps(); GenuineArrivalAndCollisions(); ManualReplayAndOverall(); BoundsAndInvalidData(); SolidObstaclesAndClearPassages();
        Console.WriteLine("PASS " + checks + " actual marble-draw simulation assertions.");
        Console.WriteLine("Deterministic custom 2D collisions and real finish-driven assignments; native Unity rendering was not executed.");
    }
}
