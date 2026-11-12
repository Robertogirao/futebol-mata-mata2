using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GoalBannerUI : MonoBehaviour
{
    public static GoalBannerUI Instance;

    [Header("Componentes do Banner")]
    public GameObject painelBanner;
    public Image fotoJogador;
    public TextMeshProUGUI textoDados;
    public Image faixaCor;

    [Header("Texto do evento")]
    public string textoEvento = "GOAL!";

    [Header("Tempo na Tela")]
    [Min(0.1f)]
    public float tempoDeExibicao = 3f;

    [Min(0f)]
    public float intervaloEntreBanners = 0.15f;

    private class Comemoracao
    {
        public string nome;
        public Sprite foto;
        public Color cor;
    }

    private readonly Queue<Comemoracao> fila =
        new Queue<Comemoracao>();

    private Coroutine rotinaFila;
    private bool exibindo;

    public bool TemBannersPendentes
    {
        get { return exibindo || fila.Count > 0; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        OcultarPainel();
    }

    public void ExibirBannerDeGol(
        JogadorDados jogador,
        Color corDoTime)
    {
        // O painel deve ser filho de um objeto que permanece ativo.
        if (PainelDesativaEsteObjeto())
        {
            Debug.LogError(
                "GoalBannerUI: coloque este componente em um " +
                "objeto pai ativo e atribua um painel filho " +
                "ao campo Painel Banner.",
                this);

            return;
        }

        gameObject.SetActive(true);

        if (!isActiveAndEnabled)
        {
            Debug.LogWarning(
                "GoalBannerUI: mantenha o componente e seus " +
                "objetos pais ativos para exibir as comemorações.",
                this);

            return;
        }

        // Copiamos os dados agora para não depender da bola,
        // que será destruída depois do gol.
        var comemoracao = new Comemoracao();
        comemoracao.cor = corDoTime;

        if (jogador != null)
        {
            comemoracao.nome =
                string.IsNullOrWhiteSpace(jogador.nomeJogador)
                    ? "PLAYER"
                    : jogador.nomeJogador.ToUpperInvariant();

            comemoracao.foto = jogador.foto512x512;
        }

        fila.Enqueue(comemoracao);

        // Novos gols entram na fila sem interromper o atual.
        if (!exibindo)
        {
            exibindo = true;
            rotinaFila = StartCoroutine(ProcessarFila());
        }
    }

    private IEnumerator ProcessarFila()
    {
        while (fila.Count > 0)
        {
            Comemoracao atual = fila.Dequeue();

            if (fotoJogador != null)
            {
                fotoJogador.sprite = atual.foto;
                fotoJogador.enabled = atual.foto != null;
            }

            if (faixaCor != null) faixaCor.color = atual.cor;

            if (textoDados != null)
            {
                string evento = string.IsNullOrWhiteSpace(textoEvento)
                    ? "GOAL!" : textoEvento.Trim();
                textoDados.text = string.IsNullOrEmpty(atual.nome)
                    ? "<color=yellow>" + evento + "</color>"
                    : "<color=yellow>" + evento + "</color>\n" + atual.nome;
            }

            if (painelBanner != null)
            {
                painelBanner.SetActive(true);
            }

            // Continua funcionando mesmo com Time.timeScale = 0.
            yield return new WaitForSecondsRealtime(
                Mathf.Max(0.1f, tempoDeExibicao));

            OcultarPainel();

            // Pequena separação visual entre comemorações,
            // inclusive quando o mesmo jogador marca novamente.
            if (intervaloEntreBanners > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    intervaloEntreBanners);
            }
            else
            {
                yield return null;
            }
        }

        exibindo = false;
        rotinaFila = null;
    }

    public void LimparFila()
    {
        if (rotinaFila != null)
        {
            StopCoroutine(rotinaFila);
            rotinaFila = null;
        }

        fila.Clear();
        exibindo = false;

        OcultarPainel();
    }

    private bool PainelDesativaEsteObjeto()
    {
        return painelBanner != null &&
            transform.IsChildOf(painelBanner.transform);
    }

    private void OcultarPainel()
    {
        if (painelBanner != null &&
            !PainelDesativaEsteObjeto())
        {
            painelBanner.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            LimparFila();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}