using UnityEngine;
namespace MarbleBasket3D
{
    // Powered colliders sweep along the funnel. The direction reverses gradually,
    // so one fixed rotation direction does not continually privilege one entry side.
    [DefaultExecutionOrder(-120)]
    public sealed class RotatingCross3D : MonoBehaviour
    {
        public Rigidbody body;
        private MarbleMatch3D match;
        private Quaternion initialRotation;
        private float angle;
        private bool running;
        public void Configure(MarbleMatch3D owner, Rigidbody rigidbody)
        {
            match = owner; body = rigidbody; initialRotation = body.rotation;
            body.isKinematic = true; body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
        public void ResetMechanism() { running = false; angle = 0; body.rotation = initialRotation; }
        public void Release() { running = true; }
        public void Freeze() { running = false; }
        private void FixedUpdate()
        {
            if (!running || match == null || !match.Active) return;
            float phase = match.SimulationTime * Mathf.PI / match.Settings.crossReversalSeconds;
            angle += match.Settings.crossDegreesPerSecond * Mathf.Sin(phase) * Time.fixedDeltaTime;
            body.MoveRotation(initialRotation * Quaternion.Euler(0, angle, 0));
        }
    }
}
