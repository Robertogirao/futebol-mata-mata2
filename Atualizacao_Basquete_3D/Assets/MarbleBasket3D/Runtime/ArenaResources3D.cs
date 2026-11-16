using UnityEngine;
using System.Collections.Generic;
namespace MarbleBasket3D
{
    public sealed class ArenaResources3D : MonoBehaviour
    {
        private readonly List<Object> owned = new List<Object>();
        public void Own(Object value) { owned.Add(value); }
        private void OnDestroy() { if (Application.isPlaying) foreach (var item in owned) if (item != null) Destroy(item); }
    }
}
