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

        BallController bola = outro.GetComponent<BallController>();
        
        if (bola != null && !bola.jaMarcouGol)
        {
            bola.jaMarcouGol = true;
            Debug.Log($"⚽ GOOOOL DETECTADO! {bola.dadosJogador?.nomeJogador}");

            // Removido o Time.timeScale = 1f daqui para não brigar com o congelamento de fim de jogo do UniversalUI!

            Rigidbody2D rb = outro.GetComponent<Rigidbody2D>();
            if (rb != null) rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

            if (UniversalUI.Instance != null)
            {
                UniversalUI.Instance.RegistrarGol(bola.pertenceAoTimeA, 0);
            }

            if (GoalBannerUI.Instance != null)
            {
                Color cor = bola.visualBola != null ? bola.visualBola.color : Color.white;
                GoalBannerUI.Instance.ExibirBannerDeGol(bola.dadosJogador, cor);
            }

            Destroy(outro.gameObject, 2f);
        }
    }
}