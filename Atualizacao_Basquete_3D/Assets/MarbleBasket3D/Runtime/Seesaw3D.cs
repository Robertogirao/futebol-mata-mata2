using UnityEngine;
namespace MarbleBasket3D
{
    public sealed class Seesaw3D : MonoBehaviour
    {
        public Rigidbody body;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        public void Configure(Rigidbody rigidbody)
        {
            body = rigidbody; initialPosition = body.position; initialRotation = body.rotation;
            body.solverIterations = 18; body.solverVelocityIterations = 10;
        }
        public void ResetMechanism()
        {
            body.isKinematic = true; body.position = initialPosition; body.rotation = initialRotation;
        }
        public void Release() { body.isKinematic = false; body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; body.WakeUp(); }
        public void Freeze() { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; body.isKinematic = true; }
    }
}
