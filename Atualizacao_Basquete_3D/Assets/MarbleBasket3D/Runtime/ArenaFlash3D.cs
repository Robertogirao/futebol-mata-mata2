using UnityEngine;
using System.Collections.Generic;
namespace MarbleBasket3D
{
    public sealed class ArenaFlash3D : MonoBehaviour
    {
        private float age, lifetime;
        private Material material;
        public void Setup(float seconds, Material owned) { lifetime = seconds; material = owned; }
        private void Update()
        {
            age += Time.deltaTime; transform.localScale = Vector3.one * (.15f + age * 1.5f);
            if (age >= lifetime) Destroy(gameObject);
        }
        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
