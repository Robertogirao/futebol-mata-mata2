using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UniversalUI : MonoBehaviour
{
    public static UniversalUI Instance;

    [Header("Configuração de Tempo (Vídeo)")]
    public float duracaoRealEmSegundos = 45f;

    [Tooltip("Exibe o tempo real restante, por exemplo 00:45, nas cenas Pokémon.")]
    public bool cronometroRealRegressivo = false;

    [HideInInspector]
    public PokemonHUD hudPokemon;

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
    public Image[] fotosTitularesA = new Image[3];
    public TextMeshProUGUI[] nomesTitularesA =
        new TextMeshProUGUI[3];
    public TextMeshProUGUI[] numerosTitularesA =
        new TextMeshProUGUI[3];

    [Header("Escalação na Tela - Time B")]
    public Image[] fotosTitularesB = new Image[3];
    public TextMeshProUGUI[] nomesTitularesB =
        new TextMeshProUGUI[3];
    public TextMeshProUGUI[] numerosTitularesB =
        new TextMeshProUGUI[3];

    private int golsA = 0;
    private int golsB = 0;
    private float tempoAtual = 0f;

    public bool partidaEmAndamento = false;

    private bool emProrrogacao = false;
    private bool partidaEncerrando = false;

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
        if (partidaEmAndamento && !partidaEncerrando)
        {
            tempoAtual += cronometroRealRegressivo && Time.captureDeltaTime > 0f
                ? Time.captureDeltaTime : Time.unscaledDeltaTime;

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
        // Uma partida nova começa sem banners antigos.
        if (GoalBannerUI.Instance != null)
        {
            GoalBannerUI.Instance.LimparFila();
        }

        golsA = 0;
        golsB = 0;
        tempoAtual = 0f;
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

        if (hudPokemon != null && hudPokemon.isActiveAndEnabled)
        {
            hudPokemon.ConfigurarPartida(timeA, timeB, trofeu, titularesA, titularesB);
        }
        else
        {
            PreencherLateral(titularesA, fotosTitularesA, nomesTitularesA, numerosTitularesA);
            PreencherLateral(titularesB, fotosTitularesB, nomesTitularesB, numerosTitularesB);
        }

        Time.timeScale = 1f;
    }

    private void PreencherLateral(
        List<JogadorDados> titulares,
        Image[] fotos,
        TextMeshProUGUI[] nomes,
        TextMeshProUGUI[] numeros)
    {
        for (int i = 0; i < 3; i++)
        {
            if (titulares != null &&
                i < titulares.Count &&
                titulares[i] != null)
            {
                if (fotos != null &&
                    i < fotos.Length &&
                    fotos[i] != null)
                {
                    fotos[i].sprite = titulares[i].foto512x512;
                    fotos[i].enabled =
                        titulares[i].foto512x512 != null;
                }

                if (nomes != null &&
                    i < nomes.Length &&
                    nomes[i] != null)
                {
                    nomes[i].text = titulares[i].nomeJogador;
                }

                if (numeros != null &&
                    i < numeros.Length &&
                    numeros[i] != null)
                {
                    numeros[i].text = "";
                    numeros[i].enabled = false;
                }
            }
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
        Time.timeScale = 1f;
    }

    public void CancelarPartida()
    {
        StopAllCoroutines();
        partidaEmAndamento = false;
        partidaEncerrando = false;
        emProrrogacao = false;
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
        if (hudPokemon != null && hudPokemon.isActiveAndEnabled)
            hudPokemon.MostrarPonto(golDoTimeA);

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

        if (cronometroRealRegressivo)
        {
            if (emProrrogacao)
            {
                textoCronometro.text = "00:00+\n<size=42%>SUDDEN DEATH</size>";
                return;
            }

            float restante = Mathf.Max(0f, duracaoRealEmSegundos - tempoAtual);
            int segundosRestantes = Mathf.CeilToInt(restante);
            textoCronometro.text = string.Format("{0:00}:{1:00}",
                segundosRestantes / 60, segundosRestantes % 60);
            return;
        }

        if (emProrrogacao)
        {
            textoCronometro.text =
                "90:00+\n<size=60%><color=yellow>" +
                "GOLDEN GOAL</color></size>";

            return;
        }

        float progresso =
            Mathf.Clamp01(tempoAtual / duracaoRealEmSegundos);

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

        if (AutoBallPipe.Instance != null)
        {
            AutoBallPipe.Instance.PararChuva();
        }

        Time.timeScale = 0f;

        StartCoroutine(RotinaFimDeJogo(timeAVenceu));
    }

    private System.Collections.IEnumerator RotinaFimDeJogo(
        bool timeAVenceu)
    {
        // No Golden Goal, o GoalDetector chama RegistrarGol
        // antes de adicionar a comemoração ao banner.
        // Este frame permite que essa chamada seja concluída.
        yield return null;

        // A pausa mínima acontece enquanto os banners continuam.
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, tempoCongeladoPosJogo));

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

        if (AutoBallPipe.Instance != null)
        {
            AutoBallPipe.Instance.PararChuva();
        }

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
}
