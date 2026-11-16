using MarbleBasket3D.Core;
using UnityEngine;

namespace MarbleBasket3D
{
    public enum MarbleState3D { Held, Contest, Queued, Seated, InFlight, Returning }
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class Marble3D : MonoBehaviour
    {
        public TeamId Team { get; private set; }
        public int Number { get; private set; }
        public MarbleState3D State { get; internal set; }
        public Rigidbody Body { get; private set; }
        public Vector3 Home { get; private set; }
        public float Radius { get; private set; }
        public float StateTime { get; internal set; }
        public float LastMotionTime { get; internal set; }
        public Vector3 LastMotionPosition { get; internal set; }
        public readonly ShotValidation3D Shot = new ShotValidation3D();
        internal MarbleMatch3D Match;
        private float nextContact;
        public void Configure(MarbleMatch3D match, TeamId team, int number, Vector3 home, PhysicsMaterial material)
        {
            Match = match; Team = team; Number = number; Home = home; Radius = match.Settings.marbleRadius;
            Body = GetComponent<Rigidbody>();
            Body.useGravity = true; Body.mass = match.Settings.marbleMass; Body.linearDamping = .015f; Body.angularDamping = .03f;
            Body.solverIterations = 14; Body.solverVelocityIterations = 8;
            Body.interpolation = RigidbodyInterpolation.Interpolate; Body.maxAngularVelocity = 70;
            transform.localScale = Vector3.one * (Radius * 2);
            var collider = GetComponent<SphereCollider>(); collider.radius = .5f; collider.sharedMaterial = material;
            ResetHome();
        }
        public void ResetHome()
        {
            SetKinematic(true); Body.position = Home; Body.rotation = Quaternion.identity;
            Shot.Reset(); State = MarbleState3D.Held; StateTime = LastMotionTime = 0;
            LastMotionPosition = Home; nextContact = 0;
        }
        internal void SetKinematic(bool value)
        {
            if (value)
            {
                if (!Body.isKinematic) { Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero; }
                Body.collisionDetectionMode = CollisionDetectionMode.Discrete;
                Body.isKinematic = true; Body.detectCollisions = false;
            }
            else
            {
                Body.isKinematic = false; Body.detectCollisions = true;
                Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero;
            }
        }
        internal void Release(MarbleState3D state, float now)
        {
            State = state; StateTime = LastMotionTime = now; LastMotionPosition = Body.position;
            SetKinematic(false);
        }
        internal void Seat(Vector3 position, float now)
        {
            SetKinematic(true); Body.position = position; State = MarbleState3D.Seated; StateTime = now;
        }
        internal void Launch(Vector3 position, Vector3 velocity, float now)
        {
            Body.position = position; Shot.Launch(); Release(MarbleState3D.InFlight, now);
            Body.linearVelocity = velocity; Body.WakeUp();
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (Match == null || !Match.Active || Match.SimulationTime < nextContact || collision.relativeVelocity.sqrMagnitude < .7f) return;
            nextContact = Match.SimulationTime + .16f;
            Match.Signals.Contact(this, Mathf.Clamp01(collision.relativeVelocity.magnitude / 8));
        }
    }
}
