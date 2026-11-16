using UnityEngine;
namespace MarbleBasket3D
{
    public enum ZoneKind3D { Queue, Loading, Fall, Collector }
    [RequireComponent(typeof(BoxCollider))]
    public sealed class MarbleZone3D : MonoBehaviour
    {
        public ZoneKind3D kind;
        public MarbleMatch3D match;
        public float collectorDividerX = 5.4f;
        private void OnTriggerEnter(Collider other) { Process(other); }
        private void OnTriggerStay(Collider other) { Process(other); }
        private void Process(Collider other)
        {
            if (match == null || !match.Active) return;
            var ball = other.GetComponent<Marble3D>();
            if (ball == null || ball.Match != match) return;
            if (kind == ZoneKind3D.Loading)
            {
                if (ball.State == MarbleState3D.Contest || ball.State == MarbleState3D.Queued) match.Launcher.Enter(ball);
            }
            else if (kind == ZoneKind3D.Queue && ball.State == MarbleState3D.Contest)
            {
                ball.State = MarbleState3D.Queued; ball.StateTime = match.SimulationTime;
            }
            else if (kind == ZoneKind3D.Fall && (ball.State == MarbleState3D.Contest || ball.State == MarbleState3D.Queued)) match.Returns.ReturnHome(ball, "Queda lateral");
            else if (kind == ZoneKind3D.Collector && ball.State == MarbleState3D.InFlight) match.Returns.Collect(ball, collectorDividerX);
        }
        private void OnTriggerExit(Collider other)
        {
            var b = other.GetComponent<Marble3D>(); if (b == null || match == null) return;
            if (kind == ZoneKind3D.Loading) match.Launcher.Exit(b);
            // Queue state is preserved at the loading cup, but not after an accidental escape.
            if (kind == ZoneKind3D.Queue && b.State == MarbleState3D.Queued && Vector3.Distance(b.Body.position, match.Launcher.seat) > .85f) b.State = MarbleState3D.Contest;
        }
    }
}
