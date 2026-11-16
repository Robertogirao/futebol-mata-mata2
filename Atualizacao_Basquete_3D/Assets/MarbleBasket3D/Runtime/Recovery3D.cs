using UnityEngine;
namespace MarbleBasket3D
{
    [DefaultExecutionOrder(50)]
    public sealed class Recovery3D : MonoBehaviour
    {
        public MarbleMatch3D match;
        public int Recoveries { get; private set; }
        private int matchNumber;
        public void ResetCounters(int epoch) { matchNumber = epoch; Recoveries = 0; }
        private void FixedUpdate()
        {
            if (match == null || !match.Active) return;
            if (matchNumber != match.Rules.MatchNumber) { Recoveries = 0; matchNumber = match.Rules.MatchNumber; }
            float now = match.SimulationTime;
            foreach (var b in match.Marbles)
            {
                if (b == null) continue;
                if (b.State == MarbleState3D.Returning || b.State == MarbleState3D.Held || b.State == MarbleState3D.Seated) continue;
                Vector3 p = b.Body.position;
                bool outside = p.y < -.7f || p.y > 16 || Mathf.Abs(p.x) > 11 || Mathf.Abs(p.z) > 9;
                if (outside) { if (match.Returns.ReturnHome(b, "Fora da arena")) Recoveries++; continue; }
                if (b.State == MarbleState3D.InFlight)
                {
                    if (now - b.StateTime > match.Settings.shotTimeout && match.Returns.ReturnHome(b, "Fim da tentativa")) Recoveries++;
                    continue;
                }
                if ((p - b.LastMotionPosition).sqrMagnitude > .025f * .025f)
                {
                    b.LastMotionPosition = p; b.LastMotionTime = now;
                }
                bool inQueue = b.State == MarbleState3D.Queued && Mathf.Abs(p.x) < .6f && p.z > -2 && p.z < 2.2f;
                if (inQueue && match.Launcher.QueueIsHealthy(now)) { b.LastMotionTime = now; continue; }
                if (now - b.LastMotionTime > match.Settings.stuckSeconds && match.Returns.ReturnHome(b, "Recuperação de travamento")) Recoveries++;
            }
        }
    }
}
