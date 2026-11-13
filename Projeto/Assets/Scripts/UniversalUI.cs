using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class UniversalUI : MonoBehaviour
{
    public static UniversalUI Instance;

    [Header("Configuração de Tempo (Vídeo)")]
    public float duracaoRealEmSegundos = 45f;

    [Tooltip(
        "Tempo mínimo de pausa após a partida. Se ainda houver " +
        "comemorações na fila, aguarda todas terminarem antes de seguir.")]
    public float tempoCongeladoPosJogo = 5f;

    [Header("Regras de Partida")]
    [Tooltip(
        "Ligado: empate vai para Golden Goal. " +
        "Desligado: a partida pode terminar empatada.")]
    public bool ativarGoldenGoal = true;

    [Header("Sons da Partida")]
    [Tooltip(
        "Opcional. Se ficar vazio, procura ou cria um AudioSource " +
        "neste objeto. Use uma fonte exclusiva para os efeitos.")]
    public AudioSource fonteDeAudio;

    public AudioClip somGol;
    public AudioClip somApitoInicial;
    public AudioClip somApitoFinal;

    [Range(0f, 1f)]
    public float volumeSons = 0.8f;

    [Header("Elementos do Topo da TV")]
    public Image escudoTimeA;
    public Image escudoTimeB;
    public Image logoCampeonato;
    public TextMeshProUGUI placarTimeA;
    public TextMeshProUGUI placarTimeB;
    public TextMeshProUGUI textoCronometro;

    [Header("Overlays e Banners")]
    public GameObject bannerGol;

    [Header("Escalação na Tela - Time A")]
    public Image[] fotosTitularesA = new Image[1];
    public TextMeshProUGUI[] nomesTitularesA =
        new TextMeshProUGUI[1];
    public TextMeshProUGUI[] numerosTitularesA =
        new TextMeshProUGUI[1];

    [Header("Escalação na Tela - Time B")]
    public Image[] fotosTitularesB = new Image[1];
    public TextMeshProUGUI[] nomesTitularesB =
        new TextMeshProUGUI[1];
    public TextMeshProUGUI[] numerosTitularesB =
        new TextMeshProUGUI[1];

    private LayoutJogadorUnicoUI layoutJogadorUnico;

    public bool ApresentacaoJogadoresConcluida
    {
        get { return layoutJogadorUnico != null && layoutJogadorUnico.ApresentacaoConcluida; }
    }

    private int golsA = 0;
    private int golsB = 0;
    private float tempoAtual = 0f;

    public bool partidaEmAndamento = false;

    private bool emProrrogacao = false;
    private bool partidaEncerrando = false;

    public int GolsA { get { return golsA; } }
    public int GolsB { get { return golsB; } }
    public float TempoAtual { get { return tempoAtual; } }
    public bool EmProrrogacao { get { return emProrrogacao; } }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        PrepararAudio();
        PrepararEscalacao();
    }

    private void OnDisable()
    {
        CancelarApresentacaoJogadores();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        CancelarPartida();
        Instance = null;
    }

    private void PrepararAudio()
    {
        if (fonteDeAudio == null)
        {
            fonteDeAudio = GetComponent<AudioSource>();
        }

        if (fonteDeAudio == null)
        {
            fonteDeAudio = gameObject.AddComponent<AudioSource>();
        }

        fonteDeAudio.playOnAwake = false;
        fonteDeAudio.loop = false;

        // Som 2D: não depende da distância da câmera.
        fonteDeAudio.spatialBlend = 0f;
    }

    private void TocarSom(AudioClip clip)
    {
        if (clip == null) return;

        if (fonteDeAudio == null)
        {
            PrepararAudio();
        }

        fonteDeAudio.PlayOneShot(
            clip,
            Mathf.Clamp01(volumeSons));
    }

    void Update()
    {
        if (partidaEmAndamento && !partidaEncerrando &&
            (FutebolDeDiscos.PartidaDiscos.Instance == null ||
             !FutebolDeDiscos.PartidaDiscos.Instance.EmReposicao))
        {
            tempoAtual += PassoTempoVideo();

            AtualizarRelogioUI();

            if (tempoAtual >= duracaoRealEmSegundos &&
                !emProrrogacao)
            {
                VerificarFimOuProrrogacao();
            }
        }
    }

    public void ConfigurarPartida(
        TeamData timeA,
        TeamData timeB,
        Sprite trofeu,
        List<JogadorDados> titularesA,
        List<JogadorDados> titularesB)
    {
        CancelarApresentacaoJogadores();
        // Uma partida nova começa sem banners antigos.
        if (GoalBannerUI.Instance != null)
        {
            GoalBannerUI.Instance.LimparFila();
        }

        golsA = 0;
        golsB = 0;
        tempoAtual = 0f;
        partidaEmAndamento = false;
        emProrrogacao = false;
        partidaEncerrando = false;

        if (textoCronometro != null)
        {
            textoCronometro.color = Color.white;
        }

        if (bannerGol != null)
        {
            bannerGol.SetActive(false);
        }

        AtualizarPlacarUI();
        AtualizarRelogioUI();

        if (escudoTimeA != null && timeA != null)
        {
            escudoTimeA.sprite = timeA.escudoOuBandeira;
        }

        if (escudoTimeB != null && timeB != null)
        {
            escudoTimeB.sprite = timeB.escudoOuBandeira;
        }

        if (logoCampeonato != null && trofeu != null)
        {
            logoCampeonato.sprite = trofeu;
        }

        PrepararEscalacao();
        if (layoutJogadorUnico != null)
        {
            layoutJogadorUnico.Configurar(timeA, timeB, titularesA, titularesB);
        }

        Time.timeScale = 1f;
    }

    public bool PrepararApresentacaoJogadores()
    {
        return isActiveAndEnabled && layoutJogadorUnico != null &&
            layoutJogadorUnico.PrepararApresentacao();
    }

    public IEnumerator ExibirApresentacaoJogadores(float tempoLeitura, float tempoDeslocamento)
    {
        if (!isActiveAndEnabled || layoutJogadorUnico == null) yield break;
        IEnumerator animacao = layoutJogadorUnico.ExibirApresentacao(tempoLeitura, tempoDeslocamento);
        while (isActiveAndEnabled && animacao.MoveNext())
            yield return animacao.Current;
    }

    public void CancelarApresentacaoJogadores()
    {
        if (layoutJogadorUnico != null) layoutJogadorUnico.CancelarApresentacao();
    }

    private void PrepararEscalacao()
    {
        // Keep serialized bindings from existing scenes, but clear every old slot.
        LimparFotos(fotosTitularesA);
        LimparFotos(fotosTitularesB);
        LimparTextos(nomesTitularesA);
        LimparTextos(nomesTitularesB);
        LimparTextos(numerosTitularesA);
        LimparTextos(numerosTitularesB);

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null) return;

        // These two containers contain only the six old player cards.
        // Do not disable any parent shared with the score or goal banners.
        Transform antigoA = canvas.transform.Find("Container_TimeA");
        Transform antigoB = canvas.transform.Find("Container_TimeB");
        if (antigoA != null) antigoA.gameObject.SetActive(false);
        if (antigoB != null) antigoB.gameObject.SetActive(false);

        if (layoutJogadorUnico == null)
        {
            TMP_FontAsset fonte = FonteDosNomes(nomesTitularesA)
                ?? FonteDosNomes(nomesTitularesB);
            if (fonte == null && textoCronometro != null)
                fonte = textoCronometro.font;
            layoutJogadorUnico = LayoutJogadorUnicoUI.Criar(canvas, fonte);
        }
    }

    private static TMP_FontAsset FonteDosNomes(TextMeshProUGUI[] nomes)
    {
        if (nomes != null)
            foreach (TextMeshProUGUI nome in nomes)
                if (nome != null && nome.font != null) return nome.font;
        return null;
    }

    private static void LimparFotos(Image[] fotos)
    {
        if (fotos == null) return;
        foreach (Image foto in fotos)
        {
            if (foto == null) continue;
            foto.sprite = null;
            foto.enabled = false;
            foto.raycastTarget = false;
        }
    }

    private static void LimparTextos(TextMeshProUGUI[] textos)
    {
        if (textos == null) return;
        foreach (TextMeshProUGUI texto in textos)
        {
            if (texto == null) continue;
            texto.text = "";
            texto.enabled = false;
            texto.raycastTarget = false;
        }
    }

    public void IniciarCronometro()
    {
        if (partidaEmAndamento || partidaEncerrando) return;

        partidaEmAndamento = true;
        partidaEncerrando = false;
        Time.timeScale = 1f;

        TocarSom(somApitoInicial);
    }

    public void PararCronometro()
    {
        partidaEmAndamento = false;
        PararSimulacao();
        Time.timeScale = 1f;
    }

    public void CancelarPartida()
    {
        StopAllCoroutines();
        CancelarApresentacaoJogadores();
        partidaEmAndamento = false;
        partidaEncerrando = false;
        emProrrogacao = false;
        PararSimulacao();
        if (GoalBannerUI.Instance != null) GoalBannerUI.Instance.LimparFila();
        Time.timeScale = 1f;
    }

    public void RegistrarGol(bool golDoTimeA, float tempo)
    {
        if (!partidaEmAndamento || partidaEncerrando) return;

        if (golDoTimeA)
        {
            golsA++;
        }
        else
        {
            golsB++;
        }

        // O placar muda imediatamente, sem esperar o banner.
        TocarSom(somGol);
        AtualizarPlacarUI();

        if (emProrrogacao && !partidaEncerrando)
        {
            Debug.Log(
                "GOLDEN GOAL! O jogo acabou na morte súbita!");

            EncerrarPartidaComRespiro(golDoTimeA);
        }
    }

    private void AtualizarPlacarUI()
    {
        if (placarTimeA != null)
        {
            placarTimeA.text = golsA.ToString();
        }

        if (placarTimeB != null)
        {
            placarTimeB.text = golsB.ToString();
        }
    }

    private void AtualizarRelogioUI()
    {
        if (textoCronometro == null) return;

        if (emProrrogacao)
        {
            textoCronometro.text =
                "90:00+\n<size=60%><color=yellow>" +
                "GOLDEN GOAL</color></size>";

            return;
        }

        float progresso =
            Mathf.Clamp01(tempoAtual / Mathf.Max(0.01f, duracaoRealEmSegundos));

        float minutosSimulados = progresso * 90f;

        int minutos = Mathf.FloorToInt(minutosSimulados);

        int segundos = Mathf.FloorToInt(
            (minutosSimulados - minutos) * 60f);

        textoCronometro.text =
            string.Format("{0:00}:{1:00}", minutos, segundos);
    }

    private void VerificarFimOuProrrogacao()
    {
        if (golsA == golsB && ativarGoldenGoal)
        {
            emProrrogacao = true;

            if (textoCronometro != null)
            {
                textoCronometro.color = Color.yellow;
            }

            Debug.Log(
                "Empate no Mata-Mata! Entrando em Golden Goal.");
            AtualizarRelogioUI();
        }
        else
        {
            if (golsA == golsB)
            {
                Debug.Log(
                    "Fim de partida: empate permitido nesta rodada.");
            }

            EncerrarPartidaComRespiro(golsA > golsB);
        }
    }

    private void EncerrarPartidaComRespiro(bool timeAVenceu)
    {
        if (partidaEncerrando || !partidaEmAndamento) return;

        partidaEncerrando = true;
        partidaEmAndamento = false;

        TocarSom(somApitoFinal);

        PararSimulacao();

        Time.timeScale = 0f;

        StartCoroutine(RotinaFimDeJogo(timeAVenceu));
    }

    private System.Collections.IEnumerator RotinaFimDeJogo(
        bool timeAVenceu)
    {
        // No Golden Goal, a partida de discos chama RegistrarGol
        // antes de adicionar a comemoração ao banner.
        // Este frame permite que essa chamada seja concluída.
        yield return null;

        // A pausa mínima acontece enquanto os banners continuam.
        float pausa = 0f;
        float duracaoPausa = Mathf.Max(0f, tempoCongeladoPosJogo);
        while (pausa < duracaoPausa)
        {
            pausa += PassoTempoVideo();
            yield return null;
        }

        // Não libera o próximo jogo ou a tabela enquanto
        // existir alguma comemoração sendo exibida ou na fila.
        while (GoalBannerUI.Instance != null &&
               GoalBannerUI.Instance.TemBannersPendentes)
        {
            yield return null;
        }

        Time.timeScale = 1f;
        emProrrogacao = false;
        partidaEncerrando = false;

        if (bannerGol != null)
        {
            bannerGol.SetActive(false);
        }

        PararSimulacao();

        // O gerenciador só recebe o resultado e avança
        // depois da pausa mínima e do fim das comemorações.
        if (TournamentManager.Instance != null)
        {
            TournamentManager.Instance.FinalizarPartidaEAvancar(
                timeAVenceu,
                golsA,
                golsB);
        }
    }

    private static void PararSimulacao()
    {
        FutebolDeDiscos.PartidaDiscos partida = FutebolDeDiscos.PartidaDiscos.Instance;
        if (partida != null) partida.PararPartida();
    }

    private static float PassoTempoVideo()
    {
        float captura = Time.captureDeltaTime;
        float passo = captura > 0f && !float.IsNaN(captura) && !float.IsInfinity(captura)
            ? captura : Time.unscaledDeltaTime;
        return float.IsNaN(passo) || float.IsInfinity(passo) ? 0f : Mathf.Max(0f, passo);
    }
}
