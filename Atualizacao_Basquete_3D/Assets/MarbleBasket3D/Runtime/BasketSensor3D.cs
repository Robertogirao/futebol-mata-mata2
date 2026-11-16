using System.Collections.Generic;
using UnityEngine;
namespace MarbleBasket3D
{
    [DefaultExecutionOrder(0)]
    public sealed class BasketSensor3D : MonoBehaviour
    {
        public MarbleMatch3D match;
        public float innerRadius = .60f;
        public int ValidBaskets { get; private set; }
        private int matchNumber;
        private readonly List<Marble3D> crossed = new List<Marble3D>();
        private readonly List<Marble3D> simultaneous = new List<Marble3D>();
        public void ResetSensor(int epoch) { matchNumber = epoch; ValidBaskets = 0; crossed.Clear(); simultaneous.Clear(); }
        private void FixedUpdate()
        {
            if (match == null || !match.Active) return;
            if (matchNumber != match.Rules.MatchNumber) ResetSensor(match.Rules.MatchNumber);
            Vector3 rim = transform.position; crossed.Clear();
            foreach (var ball in match.Marbles)
            {
                if (ball == null || ball.State != MarbleState3D.InFlight) continue;
                Vector3 p = ball.Body.position;
                if (ball.Shot.Sample(p.x,p.y,p.z,rim.x,rim.y,rim.z,innerRadius,ball.Radius)) crossed.Add(ball);
            }
            crossed.Sort((a,b) => a.Shot.CrossingFraction.CompareTo(b.Shot.CrossingFraction));
            int index=0,epoch=match.Rules.MatchNumber;
            while(index<crossed.Count && match.Active && match.Rules.MatchNumber==epoch)
            {
                simultaneous.Clear(); double fraction=crossed[index].Shot.CrossingFraction;
                do { simultaneous.Add(crossed[index++]); }
                while(index<crossed.Count && System.Math.Abs(crossed[index].Shot.CrossingFraction-fraction)<.000001);
                int awarded=match.TryAwardBatch(simultaneous);
                if(match.Rules.MatchNumber==epoch) ValidBaskets+=awarded;
            }
        }
    }
}
