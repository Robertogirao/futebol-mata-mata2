using UnityEngine;
namespace MarbleBasket3D
{
    public sealed class LeverContact3D : MonoBehaviour
    {
        public Launcher3D launcher;
        private void OnCollisionEnter(Collision collision)
        {
            if (launcher == null || collision.collider == null) return;
            launcher.RegisterPhysicalHit(collision.collider.GetComponent<Marble3D>());
        }
    }
}
