using UnityEngine;

public class GoalDetector : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D outro)
    {
        // 1. TRAVA DE SEGURANÇA MÁXIMA: Se a UI não existir ou a partida NÃO estiver rolando (já acabou ou congelou), ignora a bola na hora!
        if (UniversalUI.Instance == null || !UniversalUI.Instance.partidaEmAndamento)
        {
            return;
        }

        BallController bola = outro.GetComponentInParent<BallController>();
        if (bola == null || bola.jaMarcouGol || bola.EmDesintegracao) return;

        UniversalUI ui = UniversalUI.Instance;
        bool modoPokemon = ui.hudPokemon != null;
        if (modoPokemon)
        {
            // O trigger do portal pode chegar antes do Update dos especiais.
            // Consulte o contato/onda antes de aceitar a pontuação deste passo.
            EventosPartidaPokemon bloqueio = ui.hudPokemon.GetComponent<EventosPartidaPokemon>();
            if (bloqueio == null) bloqueio = FindFirstObjectByType<EventosPartidaPokemon>();
            if (bloqueio != null && bloqueio.isActiveAndEnabled && bloqueio.TentarInterceptarBola(bola)) return;
            if (bola.EmDesintegracao) return;
        }

        bola.jaMarcouGol = true;
        JogadorDados jogador = bola.dadosJogador;
        bool pertenceAoTimeA = bola.pertenceAoTimeA;
        Color cor = bola.visualBola != null ? bola.visualBola.color : Color.white;
        if (modoPokemon)
        {
            // Desativa agora: imagem e colisões somem no mesmo instante.
            // Destroy sozinho só removeria o objeto ao fim do frame.
            bola.gameObject.SetActive(false);
            Destroy(bola.gameObject);

            ArenaPokemon arena = ui.hudPokemon.GetComponent<ArenaPokemon>();
            if (arena != null) arena.PiscarPortal(cor);
        }
        else
        {
            Rigidbody2D rb = bola.GetComponent<Rigidbody2D>();
            if (rb != null) rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            Destroy(bola.gameObject, 2f);
        }

        Debug.Log($"PONTO DETECTADO! {jogador?.nomeJogador}");
        ui.RegistrarGol(pertenceAoTimeA, 0);

        if (GoalBannerUI.Instance != null)
        {
            GoalBannerUI.Instance.ExibirBannerDeGol(jogador, cor);
        }
    }
}
