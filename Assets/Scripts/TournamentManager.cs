using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;

[Serializable]
public class Confronto
{
    public TeamData timeA;
    public TeamData timeB;

    public int golsA;
    public int golsB;

    public bool concluido;
    public bool pontosRegistrados;

    public TeamData vencedor;

    [HideInInspector]
    public int linhaCsv;

    public Confronto(TeamData a, TeamData b)
    {
        timeA = a;
        timeB = b;
    }
}

[Serializable]
public class StatusGrupoTime
{
    public TeamData time;
    public int pontos;
    public int saldoGols;
    public int golsPro;
}

public class TournamentManager : MonoBehaviour
{
    public static TournamentManager Instance;

    [Header("Mata-mata (cenas exclusivas)")]
    public bool modoMataMata = false;
    public MataMataCampeonato mataMata;

    [Header("Calendário")]
    public TextAsset arquivoCsvCalendario;
    public int rodadaDesejada = 1;

    public List<TeamData> todosOsTimesDisponiveis =
        new List<TeamData>();

    [Header("Campeonato do canal")]
    [Tooltip(
        "Desligado: teste sem salvar. " +
        "Ligado: grava o rascunho da rodada.")]
    public bool modoLiga = false;

    public LigaCampeonato campeonato;

    [Min(1)]
    public int jogoParaRegravar = 1;

    [Header("Classificação na gravação")]
    public bool mostrarClassificacao = true;
    public ClassificacaoUI classificacaoUI;

    [Header("Abertura antes de cada jogo")]
    public bool mostrarAbertura = true;
    public AberturaJogoUI aberturaJogoUI;

    [Header("Apresentação dos jogadores na arena")]
    [Min(0f)]
    [Tooltip("Segundos com as fichas ampliadas no centro, antes do início da partida.")]
    public float tempoApresentacaoJogadores = 5f;

    [Min(0.1f)]
    public float tempoDeslocamentoFichas = 0.8f;

    [Header("Ritmo entre os jogos")]
    public float intervaloEntreJogos = 4f;

    [HideInInspector]
    public bool jogandoFaseDeGrupos = false;

    [Header("Gameplay e interface")]
    public AutoBallPipe canoAutomatizado;
    public Sprite logoCampeonato;
    public TextMeshProUGUI textoTituloFase;
    public GameObject telaTorneio;

    [Header("Cores das bolas")]
    [Min(0f)]
    public float limiarCoresParecidas = 25f;

    [Header("Memória da rodada")]
    public List<Confronto> rodadaAtual =
        new List<Confronto>();

    private Confronto confrontoEmAndamento;

    private bool registroPendente;
    private bool ligaPreparada;

    private int placarPendenteA;
    private int placarPendenteB;

    private bool exibindoClassificacao;
    private bool aberturaConcluida;
    private bool encerramentoExibido;

    private Coroutine rotinaClassificacao;

    private Coroutine rotinaAbertura;
    private AberturaJogoUI aberturaEmUso;
    private bool preparandoPartida;
    private int versaoAbertura;

    private bool UsarTabela
    {
        get
        {
            return modoLiga && mostrarClassificacao;
        }
    }

    private bool TabelaAtiva
    {
        get
        {
            return exibindoClassificacao ||
                (classificacaoUI != null &&
                 classificacaoUI.EstaExibindo);
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (Instance != this) return;

        if (canoAutomatizado == null)
        {
            canoAutomatizado =
                FindAnyObjectByType<AutoBallPipe>();
        }

        if (modoMataMata)
        {
            modoLiga = false;
            mostrarClassificacao = false;
            mostrarAbertura = false;
            if (mataMata == null)
                mataMata = GetComponent<MataMataCampeonato>();
            if (mataMata == null)
                Debug.LogError("Adicione MataMataCampeonato à cena.", this);
            else
                mataMata.Iniciar(this);
            return;
        }

        CarregarRodadaDoCSVESeguir();
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            LimparApresentacao();
        }
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        LimparApresentacao();
        Instance = null;
    }

    private void LimparApresentacao()
    {
        if (modoMataMata && mataMata != null)
            mataMata.CancelarApresentacao();
        CancelarAbertura();

        CancelInvoke(
            nameof(PuxarProximoConfrontoPendente));

        if (rotinaClassificacao != null)
        {
            StopCoroutine(rotinaClassificacao);
        }

        rotinaClassificacao = null;
        exibindoClassificacao = false;
        aberturaConcluida = false;

        if (classificacaoUI != null)
        {
            classificacaoUI.Ocultar();
        }
    }

    public void CarregarRodadaDoCSVESeguir()
    {
        if (modoMataMata)
        {
            Debug.LogWarning("Nesta cena os confrontos vêm do chaveamento. Use os controles do MataMataCampeonato.", this);
            return;
        }
        if (confrontoEmAndamento != null ||
            TabelaAtiva)
        {
            Debug.LogWarning(
                "Aguarde a partida, a tabela ou o salvamento " +
                "pendente antes de recarregar.",
                this);

            return;
        }

        CancelInvoke(
            nameof(PuxarProximoConfrontoPendente));

        rodadaAtual.Clear();

        ligaPreparada = false;
        aberturaConcluida = false;
        encerramentoExibido = false;

        if (arquivoCsvCalendario == null)
        {
            Debug.LogError(
                "Atribua o CSV do calendário.",
                this);

            return;
        }

        var indice = CriarIndiceTimes();

        int avisos = 0;
        int recusados = 0;

        var linhas = CalendarioCsvParser.Ler(
            arquivoCsvCalendario.text,
            (numero, motivo) =>
            {
                avisos++;

                Debug.LogWarning(
                    $"CSV, linha {numero}: {motivo}",
                    this);
            });

        foreach (var linha in linhas)
        {
            if (linha.rodada != rodadaDesejada)
            {
                continue;
            }

            TeamData timeA = BuscarTime(
                indice,
                linha.timeA);

            TeamData timeB = BuscarTime(
                indice,
                linha.timeB);

            if (timeA == null ||
                timeB == null ||
                timeA == timeB)
            {
                recusados++;

                Debug.LogWarning(
                    $"CSV, linha {linha.numeroLinha}: confira " +
                    $"o cadastro de {linha.timeA} x {linha.timeB}.",
                    this);

                continue;
            }

            rodadaAtual.Add(
                new Confronto(timeA, timeB)
                {
                    linhaCsv = linha.numeroLinha
                });
        }

        Debug.Log(
            $"Rodada {rodadaDesejada}: " +
            $"{rodadaAtual.Count} jogos carregados; " +
            $"{recusados} recusados.",
            this);

        if (rodadaAtual.Count == 0)
        {
            Debug.LogWarning(
                "Nenhum jogo válido foi encontrado para a rodada.",
                this);

            return;
        }

        if (modoLiga)
        {
            if (avisos > 0 || recusados > 0)
            {
                Debug.LogError(
                    "Modo Liga: corrija os avisos do calendário " +
                    "antes de gravar.",
                    this);

                return;
            }

            if (campeonato == null)
            {
                campeonato =
                    FindAnyObjectByType<LigaCampeonato>();
            }

            if (campeonato == null)
            {
                Debug.LogError(
                    "Adicione LigaCampeonato e ligue " +
                    "a referência Campeonato.",
                    this);

                return;
            }

            if (UsarTabela &&
                !PrepararInterfaceTabela())
            {
                return;
            }

            bool preparada = campeonato.PrepararRodada(
                rodadaDesejada,
                todosOsTimesDisponiveis,
                rodadaAtual);

            if (!preparada)
            {
                return;
            }

            ligaPreparada = true;
        }

        if (telaTorneio != null)
        {
            telaTorneio.SetActive(!UsarTabela);
        }

        if (rodadaAtual.All(c => c.concluido))
        {
            aberturaConcluida = true;
            MostrarFimDaRodada();

            return;
        }

        if (textoTituloFase != null)
        {
            textoTituloFase.text =
                $"MATCHDAY {rodadaDesejada}";
        }

        if (UsarTabela)
        {
            IniciarTabela(false);
        }
        else
        {
            aberturaConcluida = true;
            AgendarProximoConfronto();
        }
    }

    private bool PrepararInterfaceTabela()
    {
        if (classificacaoUI == null)
        {
            classificacaoUI =
                FindAnyObjectByType<ClassificacaoUI>();
        }

        if (classificacaoUI == null ||
            !classificacaoUI.isActiveAndEnabled)
        {
            Debug.LogError(
                "Adicione e habilite ClassificacaoUI " +
                "antes de gravar a tabela.",
                this);

            return false;
        }

        if (classificacaoUI.logo == null)
        {
            classificacaoUI.logo = logoCampeonato;
        }

        return true;
    }

    private bool IniciarTabela(bool encerramento)
    {
        if (!isActiveAndEnabled ||
            !UsarTabela ||
            !ligaPreparada ||
            TabelaAtiva ||
            !PrepararInterfaceTabela())
        {
            return false;
        }

        var tabela =
            campeonato.ObterClassificacao(encerramento);

        var anterior = encerramento
            ? campeonato.ObterClassificacao(false)
            : null;

        if (tabela == null ||
            (encerramento && anterior == null))
        {
            Debug.LogError(
                "A classificação não pôde ser lida. " +
                "A sequência permanece pausada.",
                this);

            return false;
        }

        exibindoClassificacao = true;

        rotinaClassificacao = StartCoroutine(
            ApresentarTabela(
                tabela,
                anterior,
                encerramento));

        return true;
    }

    private IEnumerator ApresentarTabela(
        List<LigaCampeonato.Linha> tabela,
        List<LigaCampeonato.Linha> anterior,
        bool encerramento)
    {
        var jogos =
            campeonato.ObterJogosDaRodada();

        if (jogos == null || jogos.Count == 0)
        {
            exibindoClassificacao = false;
            rotinaClassificacao = null;

            if (classificacaoUI != null)
            {
                classificacaoUI.Ocultar();
            }

            Debug.LogError(
                "Não foi possível carregar os confrontos da rodada.",
                this);

            yield break;
        }

        if (canoAutomatizado != null)
        {
            canoAutomatizado.PararChuva();
        }

        if (UniversalUI.Instance != null)
        {
            UniversalUI.Instance.PararCronometro();
        }

        yield return classificacaoUI.ExibirTabela(
            tabela,
            anterior,
            todosOsTimesDisponiveis,
            rodadaDesejada,
            encerramento,
            jogos);

        bool concluida =
            isActiveAndEnabled &&
            classificacaoUI != null &&
            classificacaoUI.UltimaExibicaoConcluida;

        exibindoClassificacao = false;
        rotinaClassificacao = null;

        if (!concluida)
        {
            Debug.LogWarning(
                "A apresentação foi interrompida. " +
                "Os jogos não serão iniciados automaticamente.",
                this);

            yield break;
        }

        if (encerramento)
        {
            encerramentoExibido = true;
        }
        else
        {
            aberturaConcluida = true;
            PuxarProximoConfrontoPendente();
        }
    }

    private Dictionary<string, List<TeamData>>
        CriarIndiceTimes()
    {
        var indice =
            new Dictionary<string, List<TeamData>>(
                StringComparer.OrdinalIgnoreCase);

        if (todosOsTimesDisponiveis == null)
        {
            return indice;
        }

        foreach (var time in todosOsTimesDisponiveis)
        {
            if (time == null) continue;

            foreach (var nome in new[]
            {
                time.nomeEquipe,
                time.name,
                time.sigla
            })
            {
                string chave = Normalizar(nome);

                if (chave.Length == 0) continue;

                List<TeamData> lista;

                if (!indice.TryGetValue(chave, out lista))
                {
                    lista = new List<TeamData>();
                    indice.Add(chave, lista);
                }

                if (!lista.Contains(time))
                {
                    lista.Add(time);
                }
            }
        }

        return indice;
    }

    private TeamData BuscarTime(
        Dictionary<string, List<TeamData>> indice,
        string nome)
    {
        List<TeamData> encontrados;

        if (indice.TryGetValue(
                Normalizar(nome),
                out encontrados) &&
            encontrados.Count == 1)
        {
            return encontrados[0];
        }

        Debug.LogWarning(
            $"Time não encontrado ou ambíguo: '{nome}'.",
            this);

        return null;
    }

    private static string Normalizar(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return "";
        }

        return string.Join(
            " ",
            nome.Trim()
                .TrimStart('\uFEFF')
                .Split(
                    (char[])null,
                    StringSplitOptions.RemoveEmptyEntries))
            .Normalize(NormalizationForm.FormC);
    }

    public void JogarConfronto(
        Confronto confronto,
        float duracao)
    {
        if (!isActiveAndEnabled ||
            preparandoPartida ||
            TabelaAtiva ||
            (UsarTabela && !aberturaConcluida))
        {
            Debug.LogWarning(
                "Aguarde a apresentação atual.",
                this);

            return;
        }

        if (modoLiga && !ligaPreparada)
        {
            Debug.LogError(
                "Prepare uma rodada válida da liga.",
                this);

            return;
        }

        if (confronto == null ||
            confronto.timeA == null ||
            confronto.timeB == null ||
            confronto.concluido ||
            confronto.pontosRegistrados)
        {
            return;
        }

        if (confrontoEmAndamento != null)
        {
            Debug.LogWarning(
                "Já há uma partida em andamento " +
                "ou com salvamento pendente.",
                this);

            return;
        }

        if (UniversalUI.Instance == null ||
            canoAutomatizado == null ||
            canoAutomatizado.prefabBola == null ||
            canoAutomatizado.prefabBola
                .GetComponent<BallController>() == null ||
            canoAutomatizado.prefabBola
                .GetComponent<Rigidbody2D>() == null)
        {
            Debug.LogError(
                "Confira UniversalUI, AutoBallPipe " +
                "e o prefab da bola.",
                this);

            return;
        }

        if (mostrarAbertura)
        {
            if (aberturaJogoUI == null)
            {
                var encontradas =
                    UnityEngine.Object
                        .FindObjectsByType<AberturaJogoUI>(
                            FindObjectsSortMode.None);

                if (encontradas.Length == 1)
                {
                    aberturaJogoUI = encontradas[0];
                }
            }

            if (aberturaJogoUI == null ||
                !aberturaJogoUI.isActiveAndEnabled)
            {
                Debug.LogError(
                    "Adicione AberturaJogoUI a um objeto " +
                    "ativo e preencha a referência " +
                    "Abertura Jogo UI no TournamentManager.",
                    this);

                return;
            }
        }

        // A escalação é escolhida uma única vez.
        var titularesA =
            confronto.timeA.ObterTitularesDaPartida();

        var titularesB =
            confronto.timeB.ObterTitularesDaPartida();

        if (titularesA.Count == 0 ||
            titularesB.Count == 0)
        {
            Debug.LogError(
                "Cadastre jogadores nos dois times.",
                this);

            return;
        }

        Color corA;
        Color corB;

        TeamData.EscolherCoresParaPartida(
            confronto.timeA,
            confronto.timeB,
            limiarCoresParecidas,
            out corA,
            out corB);

        CancelInvoke(
            nameof(PuxarProximoConfrontoPendente));

        // Cancela uma apresentação anterior antes
        // de reservar o novo confronto.
        CancelarAbertura();

        confrontoEmAndamento = confronto;
        registroPendente = false;
        preparandoPartida = true;

        int geracao = ++versaoAbertura;

        int numero = Mathf.Max(
            1,
            rodadaAtual.IndexOf(confronto) + 1);

        int total = rodadaAtual.Count;

        bool usarAberturaNestaPartida =
            mostrarAbertura;

        aberturaEmUso = usarAberturaNestaPartida
            ? aberturaJogoUI
            : null;

        try
        {
            if (aberturaEmUso != null)
            {
                aberturaEmUso.CobrirTela();
            }

            canoAutomatizado.PararChuva();
            UniversalUI.Instance.PararCronometro();

            UniversalUI.Instance.ConfigurarPartida(
                confronto.timeA,
                confronto.timeB,
                logoCampeonato,
                titularesA,
                titularesB);

            if (telaTorneio != null)
            {
                telaTorneio.SetActive(false);
            }

            if (classificacaoUI != null)
            {
                classificacaoUI.Ocultar();
            }

            if (usarAberturaNestaPartida)
            {
                Time.timeScale = 0f;

                // Reafirma a cobertura após ocultar a tabela.
                aberturaEmUso.CobrirTela();

                Coroutine iniciada = StartCoroutine(
                    RodarAbertura(
                        confronto,
                        duracao,
                        titularesA,
                        titularesB,
                        corA,
                        corB,
                        geracao,
                        numero,
                        total,
                        rodadaDesejada));

                if (PreparacaoAtual(
                    confronto, geracao))
                {
                    rotinaAbertura = iniciada;
                }
            }
            else
            {
                Coroutine iniciada = StartCoroutine(
                    RodarApresentacaoJogadores(
                        confronto,
                        duracao,
                        titularesA,
                        titularesB,
                        corA,
                        corB,
                        geracao,
                        numero,
                        total,
                        false));

                if (PreparacaoAtual(confronto, geracao))
                    rotinaAbertura = iniciada;
            }
        }
        catch (Exception e)
        {
            CancelarAbertura();

            Debug.LogError(
                "Não foi possível preparar a partida: " +
                e.Message,
                this);
        }
    }

    private bool PreparacaoAtual(
        Confronto confronto,
        int geracao)
    {
        return this != null &&
            isActiveAndEnabled &&
            preparandoPartida &&
            geracao == versaoAbertura &&
            confrontoEmAndamento == confronto;
    }

    public bool EstaPreparandoConfronto(Confronto confronto)
    {
        return preparandoPartida &&
            ReferenceEquals(confrontoEmAndamento, confronto);
    }

    private IEnumerator RodarAbertura(
        Confronto confronto,
        float duracao,
        List<JogadorDados> titularesA,
        List<JogadorDados> titularesB,
        Color corA,
        Color corB,
        int geracao,
        int numero,
        int total,
        int rodada)
    {
        var abertura = aberturaEmUso;

        if (!PreparacaoAtual(
            confronto, geracao))
        {
            yield break;
        }

        if (abertura == null)
        {
            CancelarAbertura();
            yield break;
        }

        IEnumerator animacao = abertura.Exibir(
            confronto.timeA,
            confronto.timeB,
            logoCampeonato,
            rodada,
            numero,
            total);

        // Permite tratar uma falha da animação
        // sem deixar a partida congelada.
        while (PreparacaoAtual(
            confronto, geracao))
        {
            bool continuar = false;
            object passo = null;
            Exception falha = null;

            try
            {
                continuar = animacao.MoveNext();

                if (continuar)
                {
                    passo = animacao.Current;
                }
            }
            catch (Exception e)
            {
                falha = e;
            }

            if (falha != null)
            {
                CancelarAbertura();

                Debug.LogError(
                    "Falha na abertura: " +
                    falha.Message,
                    this);

                yield break;
            }

            if (!continuar)
            {
                break;
            }

            yield return passo;
        }

        if (!PreparacaoAtual(
            confronto, geracao))
        {
            yield break;
        }

        if (abertura == null ||
            !abertura.UltimaExibicaoConcluida)
        {
            CancelarAbertura();

            Debug.LogWarning(
                "A abertura foi interrompida. " +
                "A partida não foi iniciada.",
                this);

            yield break;
        }

        yield return RodarApresentacaoJogadores(
            confronto,
            duracao,
            titularesA,
            titularesB,
            corA,
            corB,
            geracao,
            numero,
            total,
            true);
    }

    private IEnumerator RodarApresentacaoJogadores(
        Confronto confronto,
        float duracao,
        List<JogadorDados> titularesA,
        List<JogadorDados> titularesB,
        Color corA,
        Color corB,
        int geracao,
        int numero,
        int total,
        bool revelarArena)
    {
        if (!PreparacaoAtual(confronto, geracao)) yield break;

        var interfacePartida = UniversalUI.Instance;
        IEnumerator animacao = null;
        Exception falhaPreparacao = null;
        try
        {
            if (interfacePartida == null ||
                !interfacePartida.isActiveAndEnabled ||
                !interfacePartida.PrepararApresentacaoJogadores())
            {
                throw new InvalidOperationException(
                    "Não foi possível preparar as fichas dos jogadores no Canvas.");
            }

            Time.timeScale = 0f;
            animacao = interfacePartida.ExibirApresentacaoJogadores(
                tempoApresentacaoJogadores, tempoDeslocamentoFichas);
            if (revelarArena && aberturaEmUso != null)
                aberturaEmUso.RevelarArena();
        }
        catch (Exception e)
        {
            falhaPreparacao = e;
        }

        if (falhaPreparacao != null)
        {
            CancelarAbertura();
            Debug.LogError("Falha na apresentação dos jogadores: " +
                falhaPreparacao.Message, this);
            yield break;
        }

        // No mata-mata, o controlador revela a cortina após aceitar o confronto.
        // As fichas já estão centralizadas neste primeiro frame.
        yield return null;
        while (PreparacaoAtual(confronto, geracao) &&
            ((revelarArena && aberturaEmUso != null && aberturaEmUso.EstaExibindo) ||
             (modoMataMata && mataMata != null && mataMata.apresentacao != null &&
              mataMata.apresentacao.EstaExibindo)))
        {
            yield return null;
        }

        if (!PreparacaoAtual(confronto, geracao)) yield break;

        while (PreparacaoAtual(confronto, geracao))
        {
            bool continuar = false;
            object passo = null;
            Exception falha = null;
            try
            {
                if (interfacePartida == null ||
                    interfacePartida != UniversalUI.Instance ||
                    !interfacePartida.isActiveAndEnabled)
                    throw new InvalidOperationException("UniversalUI indisponível.");

                continuar = animacao.MoveNext();
                if (continuar) passo = animacao.Current;
            }
            catch (Exception e)
            {
                falha = e;
            }

            if (falha != null)
            {
                CancelarAbertura();
                Debug.LogError("Falha na apresentação dos jogadores: " +
                    falha.Message, this);
                yield break;
            }

            if (!continuar) break;
            yield return passo;
        }

        if (!PreparacaoAtual(confronto, geracao)) yield break;

        if (!interfacePartida.ApresentacaoJogadoresConcluida)
        {
            CancelarAbertura();
            Debug.LogWarning("A apresentação dos jogadores foi interrompida. " +
                "A partida não foi iniciada.", this);
            yield break;
        }

        IniciarJogoPreparado(confronto, duracao, titularesA, titularesB,
            corA, corB, geracao, numero, total);
    }

    private void IniciarJogoPreparado(
        Confronto confronto,
        float duracao,
        List<JogadorDados> titularesA,
        List<JogadorDados> titularesB,
        Color corA,
        Color corB,
        int geracao,
        int numero,
        int total)
    {
        if (!PreparacaoAtual(
            confronto, geracao))
        {
            return;
        }

        try
        {
            if (UniversalUI.Instance == null ||
                canoAutomatizado == null ||
                !UniversalUI.Instance.isActiveAndEnabled ||
                !canoAutomatizado.isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "UniversalUI ou AutoBallPipe indisponível.");
            }

            // Mantém a ordem de início do jogo atual.
            UniversalUI.Instance.IniciarCronometro();

            canoAutomatizado.IniciarChuvaDeBolas(
                confronto.timeA,
                confronto.timeB,
                duracao,
                titularesA,
                titularesB,
                corA,
                corB);

            preparandoPartida = false;
            rotinaAbertura = null;

            Debug.Log(
                $"Jogo {numero}/{total}: " +
                $"{confronto.timeA.nomeEquipe} x " +
                $"{confronto.timeB.nomeEquipe}.",
                this);
        }
        catch (Exception e)
        {
            CancelarAbertura();

            Debug.LogError(
                "Não foi possível iniciar a partida: " +
                e.Message,
                this);
        }
    }

    private void CancelarAbertura()
    {
        unchecked
        {
            versaoAbertura++;
        }

        if (rotinaAbertura != null)
        {
            StopCoroutine(rotinaAbertura);
            rotinaAbertura = null;
        }

        if (aberturaEmUso != null)
        {
            aberturaEmUso.Ocultar();
        }

        aberturaEmUso = null;

        if (UniversalUI.Instance != null)
            UniversalUI.Instance.CancelarApresentacaoJogadores();

        if (!preparandoPartida)
        {
            return;
        }

        preparandoPartida = false;
        confrontoEmAndamento = null;
        registroPendente = false;

        if (canoAutomatizado != null)
        {
            canoAutomatizado.PararChuva();
        }

        if (UniversalUI.Instance != null)
        {
            UniversalUI.Instance.PararCronometro();
        }

        Time.timeScale = 1f;
    }

    public void CancelarConfrontoMataMata(Confronto esperado)
    {
        if (!modoMataMata || esperado == null ||
            !ReferenceEquals(confrontoEmAndamento, esperado)) return;
        CancelarAbertura();
        preparandoPartida = false;
        confrontoEmAndamento = null;
        registroPendente = false;
    }

    public void FinalizarPartidaEAvancar(
        bool timeAVenceu,
        int golsA,
        int golsB)
    {
        if (preparandoPartida ||
            confrontoEmAndamento == null ||
            confrontoEmAndamento.pontosRegistrados)
        {
            return;
        }

        placarPendenteA = golsA;
        placarPendenteB = golsB;

        bool salvo = modoMataMata
            ? mataMata != null && mataMata.RegistrarResultado(
                confrontoEmAndamento, golsA, golsB)
            : !modoLiga || (campeonato != null && campeonato.RegistrarResultado(
                confrontoEmAndamento, golsA, golsB));

        if (!salvo)
        {
            registroPendente = true;

            Debug.LogError(
                "Salvamento pendente. Resolva o erro e use " +
                "Liga > Tentar salvar último resultado. " +
                "A sequência foi pausada.",
                this);

            return;
        }

        var jogo = confrontoEmAndamento;

        jogo.golsA = golsA;
        jogo.golsB = golsB;

        // Empate não tem vencedor.
        jogo.vencedor = golsA == golsB
            ? null
            : golsA > golsB
                ? jogo.timeA
                : jogo.timeB;

        jogo.concluido = true;
        jogo.pontosRegistrados = true;

        registroPendente = false;
        confrontoEmAndamento = null;

        Debug.Log(
            $"Fim: {jogo.timeA.nomeEquipe} {golsA} x " +
            $"{golsB} {jogo.timeB.nomeEquipe}.",
            this);

        if (modoMataMata)
        {
            mataMata.AvancarAposResultado();
        }
        else if (rodadaAtual.All(c => c.concluido))
        {
            MostrarFimDaRodada();
        }
        else
        {
            AgendarProximoConfronto();
        }
    }

    private void MostrarFimDaRodada()
    {
        if (telaTorneio != null)
        {
            telaTorneio.SetActive(!UsarTabela);
        }

        if (textoTituloFase != null)
        {
            textoTituloFase.text =
                $"MATCHDAY {rodadaDesejada} COMPLETE";
        }

        Debug.Log(
            modoLiga
                ? "Rodada gravada. Confira os takes e confirme " +
                  "a rodada pelo menu quando estiver satisfeito."
                : "Rodada finalizada.",
            this);

        if (UsarTabela &&
            !encerramentoExibido &&
            !TabelaAtiva)
        {
            IniciarTabela(true);
        }
    }

    private void AgendarProximoConfronto()
    {
        CancelInvoke(
            nameof(PuxarProximoConfrontoPendente));

        if (!isActiveAndEnabled || TabelaAtiva)
        {
            return;
        }

        Invoke(
            nameof(PuxarProximoConfrontoPendente),
            Mathf.Max(0f, intervaloEntreJogos));
    }

    private void PuxarProximoConfrontoPendente()
    {
        if (!isActiveAndEnabled ||
            TabelaAtiva ||
            confrontoEmAndamento != null)
        {
            return;
        }

        var proximo = rodadaAtual
            .FirstOrDefault(c => !c.concluido);

        if (proximo != null)
        {
            JogarConfronto(
                proximo,
                float.MaxValue);
        }
    }

    [ContextMenu("Liga/Tentar salvar último resultado")]
    public void TentarSalvarUltimoResultado()
    {
        if (TabelaAtiva) return;

        if (registroPendente &&
            confrontoEmAndamento != null)
        {
            FinalizarPartidaEAvancar(
                placarPendenteA > placarPendenteB,
                placarPendenteA,
                placarPendenteB);
        }
    }

    [ContextMenu("Liga/Regravar jogo selecionado")]
    public void RegravarJogoSelecionado()
    {
        if (!Application.isPlaying ||
            !isActiveAndEnabled ||
            TabelaAtiva ||
            !modoLiga ||
            !ligaPreparada ||
            campeonato == null ||
            (UsarTabela && !aberturaConcluida) ||
            confrontoEmAndamento != null ||
            jogoParaRegravar < 1 ||
            jogoParaRegravar > rodadaAtual.Count)
        {
            Debug.LogWarning(
                "Aguarde a tabela e o jogo atual terminarem. " +
                "Selecione uma partida válida no Play Mode.",
                this);

            return;
        }

        var jogo =
            rodadaAtual[jogoParaRegravar - 1];

        CancelInvoke(
            nameof(PuxarProximoConfrontoPendente));

        if (!campeonato.LimparResultado(jogo))
        {
            Debug.LogError(
                "Regravação pausada. Resolva o erro " +
                "e repita o comando.",
                this);

            return;
        }

        jogo.concluido = false;
        jogo.pontosRegistrados = false;
        jogo.golsA = 0;
        jogo.golsB = 0;
        jogo.vencedor = null;

        encerramentoExibido = false;

        JogarConfronto(
            jogo,
            float.MaxValue);
    }

    [ContextMenu("Liga/Confirmar rodada gravada")]
    public void ConfirmarRodadaGravada()
    {
        if (TabelaAtiva ||
            !modoLiga ||
            !ligaPreparada ||
            campeonato == null ||
            confrontoEmAndamento != null)
        {
            Debug.LogWarning(
                "Aguarde os jogos e a classificação " +
                "terminarem antes de confirmar.",
                this);

            return;
        }

        campeonato.ConfirmarRodada();
    }

    [ContextMenu("Classificação/Reexibir encerramento")]
    public void ReexibirEncerramento()
    {
        if (!Application.isPlaying ||
            !isActiveAndEnabled ||
            !UsarTabela ||
            !ligaPreparada ||
            TabelaAtiva ||
            confrontoEmAndamento != null ||
            rodadaAtual.Count == 0 ||
            !rodadaAtual.All(c => c.concluido))
        {
            Debug.LogWarning(
                "Finalize os jogos e aguarde a apresentação " +
                "atual antes de reexibir.",
                this);

            return;
        }

        encerramentoExibido = false;
        IniciarTabela(true);
    }
}
