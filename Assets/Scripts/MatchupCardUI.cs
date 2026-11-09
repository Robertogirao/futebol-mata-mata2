using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MatchupCardUI : MonoBehaviour
{
    [Header("Elementos Visuais do Confronto")]
    public Image bandeiraTimeA;
    public TextMeshProUGUI siglaTimeA;
    public TextMeshProUGUI placarA;

    public Image bandeiraTimeB;
    public TextMeshProUGUI siglaTimeB;
    public TextMeshProUGUI placarB;

    [Header("Botão de Ação")]
    [Tooltip("Botão para iniciar ou simular a partida ao clicar no cartão")]
    public Button botaoJogarPartida;

    private Confronto meuConfronto;

    // Função que o TournamentManager chama para preencher este cartão
    public void ConfigurarCartão(Confronto confronto)
    {
        meuConfronto = confronto;

        // 1. Configura Time A
        if (confronto.timeA != null)
        {
            if (siglaTimeA != null) siglaTimeA.text = confronto.timeA.sigla;
            if (bandeiraTimeA != null && confronto.timeA.escudoOuBandeira != null)
                bandeiraTimeA.sprite = confronto.timeA.escudoOuBandeira;
        }
        else
        {
            if (siglaTimeA != null) siglaTimeA.text = "???";
        }

        // 2. Configura Time B
        if (confronto.timeB != null)
        {
            if (siglaTimeB != null) siglaTimeB.text = confronto.timeB.sigla;
            if (bandeiraTimeB != null && confronto.timeB.escudoOuBandeira != null)
                bandeiraTimeB.sprite = confronto.timeB.escudoOuBandeira;
        }
        else
        {
            if (siglaTimeB != null) siglaTimeB.text = "BYE"; // Passou direto
        }

        // 3. Atualiza Placar
        AtualizarPlacarVisual();

        // 4. Conecta o botão para enviar esta partida direto para o UniversalUI rodar!
        if (botaoJogarPartida != null)
        {
            botaoJogarPartida.onClick.RemoveAllListeners();
            botaoJogarPartida.onClick.AddListener(AoClicarEmJogar);
        }
    }

    public void AtualizarPlacarVisual()
    {
        if (meuConfronto == null) return;

        if (placarA != null) placarA.text = meuConfronto.golsA.ToString();
        if (placarB != null) placarB.text = meuConfronto.golsB.ToString();
    }

    private void AoClicarEmJogar()
    {
        if (meuConfronto == null || meuConfronto.concluido) return;

        // Avisa o TournamentManager para começar este jogo!
        if (TournamentManager.Instance != null)
        {
            Debug.Log($"Iniciando partida: {meuConfronto.timeA?.sigla} vs {meuConfronto.timeB?.sigla}");
            TournamentManager.Instance.JogarConfronto(meuConfronto, 60f);
        }
    }
}