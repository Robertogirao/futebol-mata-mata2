using System.Collections.Generic;
using UnityEngine;

namespace MarbleBasket3D
{
    [DefaultExecutionOrder(30)]
    public sealed class ReturnSystem3D : MonoBehaviour
    {
        private sealed class Transit
        {
            public Marble3D ball;
            public Vector3[] points;
            public float started, duration;
            public bool secondChance;
        }
        private readonly Dictionary<Marble3D, Transit> transits = new Dictionary<Marble3D, Transit>();
        private readonly List<Marble3D> completed = new List<Marble3D>();
        private MarbleMatch3D match;
        public int ReturnsStarted { get; private set; }
        public int SecondChances { get; private set; }
        public int ReturningCount => transits.Count;
        public static readonly Vector3 CollectorOutlet = new Vector3(5.4f,.18f,-2.3f);
        public static readonly Vector3 CollectorFork = new Vector3(5.4f,.7f,-4.7f);
        public static Vector3 CollectorBranch(Core.TeamId team) => new Vector3(team==Core.TeamId.Red?-8.5f:8.5f,.7f,-4.7f);
        public void Configure(MarbleMatch3D owner) { match = owner; }
        public void ResetSystem() { transits.Clear(); completed.Clear(); ReturnsStarted = SecondChances = 0; }
        public void StopSystem() { }
        public bool ReturnHome(Marble3D ball, string reason)
        {
            if (!CanReturn(ball)) return false;
            float side = ball.Team == Core.TeamId.Red ? -1 : 1;
            var points = new[] { ball.Body.position, new Vector3(side * 8.5f, .7f, -3.7f), new Vector3(side * 8.5f, 8.8f, 3.4f), ball.Home };
            Begin(ball, points, match.Settings.returnSeconds, false); ReturnsStarted++;
            match.Signals.Fall(ball, reason); return true;
        }
        public bool Collect(Marble3D ball, float dividerX)
        {
            if (!CanReturn(ball) || ball.State != MarbleState3D.InFlight) return false;
            float side=ball.Team==Core.TeamId.Red?-1:1;
            var points=new[]{ball.Body.position,CollectorFork,CollectorBranch(ball.Team),new Vector3(side*8.5f,.7f,-3.7f),new Vector3(side*8.5f,8.8f,3.4f),ball.Home};
            Begin(ball,points,match.Settings.returnSeconds,false);ReturnsStarted++;
            match.Signals.Fall(ball,"Retorno ao reservatório");return true;
        }
        private bool CanReturn(Marble3D b) => match != null && match.Active && b != null && b.Match == match && b.State != MarbleState3D.Returning && !transits.ContainsKey(b);
        private void Begin(Marble3D ball, Vector3[] points, float duration, bool secondChance)
        {
            ball.SetKinematic(true); ball.State = MarbleState3D.Returning; ball.StateTime = match.SimulationTime;
            transits.Add(ball, new Transit { ball = ball, points = points, duration = duration, started = match.SimulationTime, secondChance = secondChance });
        }
        private void FixedUpdate()
        {
            if (match == null || !match.Active) return;
            completed.Clear();
            foreach (var pair in transits)
            {
                Transit t = pair.Value; float u = Mathf.Clamp01((match.SimulationTime - t.started) / t.duration);
                // Arc-length parameterization keeps the visible lift/return speed steady along the route.
                t.ball.Body.MovePosition(PointAlong(t.points, u));
                if (u >= 1) completed.Add(t.ball);
            }
            foreach (var b in completed)
            {
                Transit t = transits[b]; transits.Remove(b);
                b.Body.position = t.points[t.points.Length - 1];
                b.Release(t.secondChance ? MarbleState3D.Queued : MarbleState3D.Contest, match.SimulationTime);
            }
        }
        public static Vector3 PointAlong(Vector3[] p, float fraction)
        {
            if (p == null || p.Length == 0) return Vector3.zero;
            if (p.Length == 1) return p[0];
            float length = 0; for (int i = 1; i < p.Length; i++) length += Vector3.Distance(p[i - 1], p[i]);
            float remaining = Mathf.Clamp01(fraction) * length;
            for (int i = 1; i < p.Length; i++)
            {
                float segment = Vector3.Distance(p[i - 1], p[i]);
                if (remaining <= segment && segment > .00001f) return Vector3.Lerp(p[i - 1], p[i], remaining / segment);
                remaining -= segment;
            }
            return p[p.Length - 1];
        }
    }
}
