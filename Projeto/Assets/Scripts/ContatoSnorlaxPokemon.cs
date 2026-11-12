using UnityEngine;

/// <summary>Encaminha os contatos reais do goleiro para o especial da arena.</summary>
[DisallowMultipleComponent]
public sealed class ContatoSnorlaxPokemon : MonoBehaviour
{
    private EventosPartidaPokemon dono;

    public void Vincular(EventosPartidaPokemon eventos) { dono = eventos; }
    public void Desvincular(EventosPartidaPokemon eventos) { if (dono == eventos) dono = null; }

    private void OnCollisionEnter2D(Collision2D colisao) { Processar(colisao.collider); }
    private void OnCollisionStay2D(Collision2D colisao) { Processar(colisao.collider); }
    private void OnTriggerEnter2D(Collider2D outro) { Processar(outro); }
    private void OnTriggerStay2D(Collider2D outro) { Processar(outro); }

    private void Processar(Collider2D outro)
    {
        if (dono == null || outro == null) return;
        BallController bola = outro.GetComponentInParent<BallController>();
        if (bola != null) dono.ProcessarContatoSnorlax(bola);
    }
}
