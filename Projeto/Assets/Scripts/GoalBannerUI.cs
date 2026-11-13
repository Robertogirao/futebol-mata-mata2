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

    [Header("Tempo na Tela")]
    [Min(0.1f)]
    public float tempoDeExibicao = 3f;

    [Min(0f)]
    public float intervaloEntreBanners = 0.15f;

    private class Comemoracao
    {
        public string nome;
        public Sprite foto;
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
        ExibirBannerDeGol(jogador != null ? jogador.nomeJogador : null,
            jogador != null ? jogador.foto512x512 : null, corDoTime);
    }

    public void ExibirBannerDeGol(string nome, Sprite imagem, Color corDoTime)
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

        // Snapshot the name and chosen image; queued goals never depend on
        // the actor lifetime and never change the source player/team assets.
        var comemoracao = new Comemoracao
        {
            nome = nome == null ? null : string.IsNullOrWhiteSpace(nome) ? "PLAYER" : nome.ToUpperInvariant(),
            foto = imagem
        };

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

            if (textoDados != null)
            {
                textoDados.text =
                    string.IsNullOrEmpty(atual.nome)
                        ? "<color=yellow>GOAL!</color>"
                        : "<color=yellow>GOAL!</color>\n" +
                          atual.nome;
            }

            if (painelBanner != null)
            {
                painelBanner.SetActive(true);
            }

            // Usa o tempo do vídeo no Recorder e continua com Time.timeScale = 0.
            yield return AguardarTempo(
                Mathf.Max(0.1f, tempoDeExibicao));

            OcultarPainel();

            // Pequena separação visual entre comemorações,
            // inclusive quando o mesmo jogador marca novamente.
            if (intervaloEntreBanners > 0f)
            {
                yield return AguardarTempo(
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

    private static IEnumerator AguardarTempo(float duracao)
    {
        double tempo = 0d;
        while (tempo + 0.000001d < duracao)
        {
            yield return null;
            float passo = Time.captureDeltaTime > 0f
                ? Time.captureDeltaTime
                : Time.unscaledDeltaTime;
            if (!float.IsNaN(passo) && !float.IsInfinity(passo))
                tempo += Mathf.Max(0f, passo);
        }
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