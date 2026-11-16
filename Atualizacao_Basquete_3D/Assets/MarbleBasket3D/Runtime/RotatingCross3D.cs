using UnityEngine;
namespace MarbleBasket3D
{
    // One driver owns both motors, with mirrored geometry and opposite rotation.
    // The same reset/release/freeze contract remains available to the match.
    [DefaultExecutionOrder(-120)]
    public sealed class RotatingCross3D : MonoBehaviour
    {
        public Rigidbody body;
        public Rigidbody oppositeBody;
        private MarbleMatch3D match;
        private Quaternion initialRotation, oppositeInitialRotation;
        private float angle;
        private bool running;
        public void Configure(MarbleMatch3D owner, Rigidbody rigidbody) => ConfigurePair(owner, rigidbody, null);
        public void ConfigurePair(MarbleMatch3D owner, Rigidbody first, Rigidbody second)
        {
            match = owner; body = first; oppositeBody = second;
            initialRotation = body.rotation;
            if (oppositeBody != null) oppositeInitialRotation = oppositeBody.rotation;
            body.isKinematic = true; body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            if (oppositeBody != null) { oppositeBody.isKinematic = true; oppositeBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; }
        }
        public void ResetMechanism()
        {
            running = false; angle = 0; body.rotation = initialRotation;
            if (oppositeBody != null) oppositeBody.rotation = oppositeInitialRotation;
        }
        public void Release() { running = true; }
        public void Freeze() { running = false; }
        private void FixedUpdate()
        {
            if (!running || match == null || !match.Active) return;
            float phase = match.SimulationTime * Mathf.PI / match.Settings.crossReversalSeconds;
            angle += match.Settings.crossDegreesPerSecond * Mathf.Sin(phase) * Time.fixedDeltaTime;
            body.MoveRotation(initialRotation * Quaternion.Euler(0, angle, 0));
            if (oppositeBody != null) oppositeBody.MoveRotation(oppositeInitialRotation * Quaternion.Euler(0, -angle, 0));
        }
    }
}
