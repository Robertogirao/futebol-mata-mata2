using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LinhaTabelaUI : MonoBehaviour
{
    [Header("Elementos da Linha")]
    public TextMeshProUGUI textoPosicao; // Ex: "1º"
    public Image fotoEscudo;             // Bandeira do Time
    public TextMeshProUGUI textoNome;    // Ex: "BRA"
    public TextMeshProUGUI textoPontos;  // Ex: "6 PTS"
    public TextMeshProUGUI textoSaldo;   // Ex: "SG: +3"

    public void ConfigurarLinha(int posicao, StatusGrupoTime status)
    {
        if (status == null || status.time == null) return;

        if (textoPosicao != null) textoPosicao.text = $"{posicao}º";
        if (textoNome != null) textoNome.text = status.time.sigla; // Ou status.time.nomeEquipe
        if (textoPontos != null) textoPontos.text = $"{status.pontos} PTS";
        if (textoSaldo != null) textoSaldo.text = $"SG: {status.saldoGols}";

        if (fotoEscudo != null)
        {
            fotoEscudo.sprite = status.time.escudoOuBandeira;
            fotoEscudo.enabled = (status.time.escudoOuBandeira != null);
        }
    }
}