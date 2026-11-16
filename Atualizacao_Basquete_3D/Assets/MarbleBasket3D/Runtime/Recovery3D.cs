using System.Collections.Generic;
using UnityEngine;
namespace MarbleBasket3D
{
    [DefaultExecutionOrder(50)]
    public sealed class Recovery3D : MonoBehaviour
    {
        public MarbleMatch3D match;
        public int Recoveries { get; private set; }
        private int matchNumber;
        private sealed class PassageWatch { public float best, advanced; }
        private readonly Dictionary<Marble3D,PassageWatch> passages=new Dictionary<Marble3D,PassageWatch>();
        public void ResetCounters(int epoch) { matchNumber = epoch; Recoveries = 0; passages.Clear(); }
        private void FixedUpdate()
        {
            if (match == null || !match.Active) return;
            if (matchNumber != match.Rules.MatchNumber) ResetCounters(match.Rules.MatchNumber);
            float now = match.SimulationTime;
            foreach (var b in match.Marbles)
            {
                if (b == null) continue;
                if (b.State == MarbleState3D.Returning || b.State == MarbleState3D.Held || b.State == MarbleState3D.Seated) { passages.Remove(b); continue; }
                Vector3 p = b.Body.position;
                bool outside = p.y < -.7f || p.y > 16 || Mathf.Abs(p.x) > 11 || Mathf.Abs(p.z) > 9;
                if (outside) { if (match.Returns.ReturnHome(b, "Fora da arena")) Recoveries++; continue; }
                if (b.State == MarbleState3D.InFlight)
                {
                    passages.Remove(b);
                    if (now - b.StateTime > match.Settings.shotTimeout && match.Returns.ReturnHome(b, "Fim da tentativa")) Recoveries++;
                    continue;
                }
                // In the open descent, jitter is not forward progress. The central rotor
                // area and the legacy timed queue retain their normal waiting behaviour.
                if (match.Launcher.Continuous && p.y>2.5f && p.y<5 && p.z<.55f && Mathf.Abs(p.x)<4)
                {
                    float progress=-p.z-Mathf.Abs(p.x)*.35f-p.y*.5f;
                    if(!passages.TryGetValue(b,out var watch))passages[b]=watch=new PassageWatch{best=progress,advanced=now};
                    if(progress>watch.best+.12f){watch.best=progress;watch.advanced=now;}
                    if(now-watch.advanced>match.Settings.stuckSeconds && match.Returns.ReturnHome(b,"Recuperação da passagem"))
                    { Recoveries++;passages.Remove(b);continue; }
                }
                else passages.Remove(b);
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
