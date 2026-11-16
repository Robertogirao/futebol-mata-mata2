using UnityEngine;
namespace BasqueteArena
{
    public enum EstadoBola { Disputa, Arremesso, Retorno }
    public sealed class BolaArenaBasquete : MonoBehaviour
    {
        public PartidaBasquete partida;
        public Rigidbody2D corpo;
        public CircleCollider2D colisor;
        public SpriteRenderer imagem;
        public JogadorDados jogador;
        public bool timeA, capitao;
        public EstadoBola estado;
        public float espera, idade, parado, alturaAnterior;
        public int valorCesta;
        public bool passouPorCima;
        public Vector2 ultimaPosicao;
        public Color cor;
    }
    public sealed class ZonaBasquete : MonoBehaviour
    {
        public bool elimina;
        private void OnTriggerEnter2D(Collider2D outro) { Contato(outro); }
        private void OnTriggerStay2D(Collider2D outro) { Contato(outro); }
        private void Contato(Collider2D outro)
        {
            var bola = outro.GetComponent<BolaArenaBasquete>();
            if (bola == null || bola.partida == null || bola.estado != EstadoBola.Disputa) return;
            if (elimina) bola.partida.Eliminar(bola); else bola.partida.TentarLancar(bola);
        }
    }
}
