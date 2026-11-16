using UnityEngine;
using System.Collections.Generic;
namespace MarbleBasket3D
{
    [RequireComponent(typeof(Camera))]
    public sealed class ArenaCamera3D : MonoBehaviour
    {
        private Camera targetCamera;
        private void LateUpdate()
        {
            if (targetCamera == null) targetCamera = GetComponent<Camera>();
            float window = (float)Screen.width / Mathf.Max(1, Screen.height), desired = 16f / 9;
            targetCamera.rect = window > desired ? new Rect((1 - desired / window) * .5f, 0, desired / window, 1) : new Rect(0, (1 - window / desired) * .5f, 1, window / desired);
        }
    }
}
