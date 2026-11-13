using UnityEngine;

namespace FutebolDeDiscos
{
    public sealed class GolDiscos : MonoBehaviour
    {
        public bool esquerda;
        private void OnTriggerEnter2D(Collider2D other) { Tentar(other); }
        private void OnTriggerStay2D(Collider2D other) { Tentar(other); }
        private void Tentar(Collider2D other)
        {
            BolaDiscos bola = other.GetComponent<BolaDiscos>();
            if (bola != null && bola.Partida != null) bola.Partida.NotificarGol(esquerda);
        }
    }
}
