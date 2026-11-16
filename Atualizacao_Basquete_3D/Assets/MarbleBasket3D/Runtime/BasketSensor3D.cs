using UnityEngine;
namespace MarbleBasket3D
{
    [DefaultExecutionOrder(0)]
    public sealed class BasketSensor3D : MonoBehaviour
    {
        public MarbleMatch3D match;
        public float innerRadius = .495f;
        public int ValidBaskets { get; private set; }
        private int matchNumber;
        public void ResetSensor(int epoch) { matchNumber = epoch; ValidBaskets = 0; }
        private void FixedUpdate()
        {
            if (match == null || !match.Active) return;
            if (matchNumber != match.Rules.MatchNumber) { matchNumber = match.Rules.MatchNumber; ValidBaskets = 0; }
            Vector3 rim = transform.position;
            foreach (var ball in match.Marbles)
            {
                if (ball.State != MarbleState3D.InFlight) continue;
                Vector3 p = ball.Body.position;
                int epoch = match.Rules.MatchNumber;
                if (ball.Shot.Sample(p.x, p.y, p.z, rim.x, rim.y, rim.z, innerRadius, ball.Radius) && match.TryAward(ball) && match.Rules.MatchNumber == epoch) ValidBaskets++;
                if (!match.Active) break;
            }
        }
    }
}
