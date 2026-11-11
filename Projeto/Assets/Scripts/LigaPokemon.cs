using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>Liga Pokémon: calendário, partidas, classificação e retomada automática.</summary>
[DefaultExecutionOrder(-300)]
[DisallowMultipleComponent]
public sealed class LigaPokemon : MonoBehaviour
{
    [Header("Identidade da liga")]
    public string nomeLiga = "Pokémon League";
    public string ligaId = "pokemon_liga";
    public string temporadaId = "temporada_01";
    public TemaCompeticao tema;

    [Header("Fundos")]
    [Tooltip("Imagem da arena. Vazio mantém o campo do tema.")]
    public Sprite fundoArena;
    [Tooltip("Imagem da classificação e abertura. Vazio mantém a apresentação do tema.")]
    public Sprite fundoClassificacao;

    [Header("Pokémon participantes")]
    [Min(2)] public int quantidadePokemon = 10;
    public List<PokemonDados> participantes = new List<PokemonDados>();

    [Header("Calendário e continuidade")]
    [Min(1)] public int quantidadeRodadas = 9;
    [Tooltip("Zero sorteia o calendário uma vez ao criar a temporada e grava a semente. O mesmo calendário é retomado depois.")]
    public int sementeCalendario;
    public bool umaRodadaPorPlay;
    [Min(0f)] public float intervaloEntreRodadas = 2f;

    [Header("Partidas")]
    [Min(1f)] public float duracaoPartida = 45f;
    public bool mostrarAbertura = true;

    [HideInInspector] public TournamentManager gerenciador;
    [HideInInspector] public LigaCampeonato campeonato;
    [HideInInspector] public ClassificacaoUI classificacaoUI;
    [HideInInspector] public PokemonHUD hud;
    [HideInInspector] public ArenaPokemon arena;
    [HideInInspector] public UniversalUI interfacePartida;
    [HideInInspector] public AberturaJogoUI aberturaJogoUI;

    [Header("Estado da liga")]
    public string situacao = "Pronta para configurar a liga.";
    [TextArea(2, 5)] public string ultimoErro;

    [Serializable]
    private sealed class AgendaSalva
    {
        public int versao = 1;
        public string liga;
        public string temporada;
        public int quantidadePokemon;
        public int quantidadeRodadas;
        public int sementeConfigurada;
        public LigaPokemonModelo.Calendario calendario;
    }

    private AgendaSalva agenda;
    private readonly List<TeamData> fichas = new List<TeamData>();
    private TemaCompeticao temaEmUso;
    private TextAsset csv;
    private bool preparada;
    private bool falhou;
    private bool concluida;
    private bool rodadaConfirmada;
    private bool aguardandoContinuacao;
    private double esperaEntreRodadas = -1d;
    private int rodadaAtual;

    public TemaCompeticao TemaEmUso { get { return temaEmUso; } }
    public bool Preparada { get { return preparada; } }
    public bool TemporadaConcluida { get { return concluida; } }
    public int RodadaAtual { get { return rodadaAtual; } }
    public string PastaDaTemporada
    {
        get
        {
            Exigir(IdentificadorValido(ligaId) && IdentificadorValido(temporadaId),
                "Liga Id e Temporada Id aceitam de 1 a 80 letras, números, _ e -.");
            return Path.Combine(Application.persistentDataPath, "Ligas", ligaId, temporadaId);
        }
    }

    private void Awake()
    {
        ResolverReferencias();
        if (gerenciador != null) gerenciador.iniciarAutomaticamente = false;
        preparada = Tentar(Preparar);
        if (!preparada) PausarComErro(ultimoErro);
    }

    private void ResolverReferencias()
    {
        if (gerenciador == null) gerenciador = GetComponent<TournamentManager>();
        if (campeonato == null) campeonato = GetComponent<LigaCampeonato>();
        if (classificacaoUI == null) classificacaoUI = GetComponent<ClassificacaoUI>();
        if (hud == null) hud = GetComponent<PokemonHUD>();
        if (arena == null) arena = GetComponent<ArenaPokemon>();
        if (interfacePartida == null) interfacePartida = GetComponent<UniversalUI>();
        if (interfacePartida == null) interfacePartida = FindFirstObjectByType<UniversalUI>();
        if (aberturaJogoUI == null) aberturaJogoUI = GetComponent<AberturaJogoUI>();
    }

    public bool ValidarConfiguracao()
    {
        return Tentar(() => { ConferirParticipantes(); });
    }

    private List<PokemonDados> ConferirParticipantes()
    {
        Exigir(!string.IsNullOrWhiteSpace(nomeLiga), "Informe o nome da liga.");
        string pasta = PastaDaTemporada;
        Exigir(tema != null, "Atribua um tema para a liga Pokémon.");
        Exigir(quantidadePokemon >= 2, "Escolha pelo menos dois Pokémon.");
        Exigir(quantidadeRodadas >= 1, "Escolha pelo menos uma rodada.");
        Exigir(TempoValido(duracaoPartida, 1f) && TempoValido(intervaloEntreRodadas, 0f),
            "Confira a duração da partida e o intervalo entre rodadas.");
        Exigir(participantes != null && participantes.Count == quantidadePokemon,
            "Preencha exatamente " + quantidadePokemon + " Pokémon ou use Sortear Pokémon do catálogo.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (PokemonDados pokemon in participantes)
        {
            Exigir(pokemon != null, "Preencha todas as vagas de Pokémon.");
            pokemon.ExigirValido();
            Exigir(ids.Add(pokemon.IdEstavel), "Pokémon repetido: " + pokemon.nomePokemon + ".");
        }
        return new List<PokemonDados>(participantes);
    }

    private void Preparar()
    {
        List<PokemonDados> usados = ConferirParticipantes();
        Exigir(gerenciador != null && campeonato != null && classificacaoUI != null &&
            hud != null && arena != null && interfacePartida != null,
            "Confira os componentes da cena de liga Pokémon.");
        Exigir(gerenciador.isActiveAndEnabled && campeonato.isActiveAndEnabled &&
            classificacaoUI.isActiveAndEnabled && hud.isActiveAndEnabled &&
            arena.isActiveAndEnabled && interfacePartida.isActiveAndEnabled,
            "Ative os componentes da liga antes do Play.");
        Exigir(!mostrarAbertura || (aberturaJogoUI != null && aberturaJogoUI.isActiveAndEnabled),
            "Ative Abertura Jogo UI para exibir a apresentação dos participantes.");
        campeonato.ligaId = ligaId;
        campeonato.temporadaId = temporadaId;
        List<string> ids = usados.Select(p => p.IdEstavel).ToList();
        agenda = CarregarOuCriarAgenda(ids);
        foreach (PokemonDados pokemon in usados) fichas.Add(pokemon.CriarFichaDeJogo());

        temaEmUso = UnityEngine.Object.Instantiate(tema);
        temaEmUso.hideFlags = HideFlags.HideAndDontSave;
        temaEmUso.nomeCompeticao = nomeLiga.Trim();
        temaEmUso.competicaoId = ligaId;
        temaEmUso.usarFiltrosMultiplosPokemon = false;
        temaEmUso.filtrarPorTipoPokemon = false;
        temaEmUso.geracaoPokemonPermitida = 0;
        if (fundoArena != null) temaEmUso.campo = fundoArena;
        if (fundoClassificacao != null)
        {
            temaEmUso.fundoAbertura = fundoClassificacao;
            temaEmUso.fundoChaveamento = fundoClassificacao;
        }
        arena.tema = temaEmUso;
        hud.tema = temaEmUso;
        hud.universalUI = interfacePartida;
        classificacaoUI.nomeDaLiga = nomeLiga.Trim();
        classificacaoUI.fundo = fundoClassificacao != null ? fundoClassificacao : temaEmUso.fundoChaveamento;
        classificacaoUI.logo = temaEmUso.logoCompeticao;
        classificacaoUI.logoCanal = temaEmUso.logoCanal;
        classificacaoUI.fonte = temaEmUso.fonteTexto;
        classificacaoUI.corFundo = temaEmUso.corPrimaria;
        classificacaoUI.corPainel = temaEmUso.corPainel;
        classificacaoUI.verde = temaEmUso.corDestaque;
        classificacaoUI.textoClaro = temaEmUso.corTexto;
        classificacaoUI.manterTabelaFinalNaTela = true;
        classificacaoUI.ordemCanvas = Math.Max(1400, classificacaoUI.ordemCanvas);
        if (aberturaJogoUI != null)
        {
            aberturaJogoUI.fundo = temaEmUso.fundoAbertura;
            aberturaJogoUI.logoCanal = temaEmUso.logoCanal;
            aberturaJogoUI.fonteTitulo = temaEmUso.fonteTitulo;
            aberturaJogoUI.fonteNomes = temaEmUso.fonteTexto;
        }
        csv = new TextAsset(LigaPokemonModelo.ParaCsv(agenda.calendario));
        csv.name = "Calendario_Liga_Pokemon";
        csv.hideFlags = HideFlags.HideAndDontSave;
        gerenciador.iniciarAutomaticamente = false;
        gerenciador.modoLiga = true;
        gerenciador.campeonato = campeonato;
        gerenciador.mostrarClassificacao = true;
        gerenciador.classificacaoUI = classificacaoUI;
        gerenciador.mostrarAbertura = mostrarAbertura;
        gerenciador.aberturaJogoUI = aberturaJogoUI;
        gerenciador.logoCampeonato = temaEmUso.logoCompeticao;
        gerenciador.todosOsTimesDisponiveis = new List<TeamData>(fichas);
        gerenciador.arquivoCsvCalendario = csv;
        if (gerenciador.canoAutomatizado == null)
            gerenciador.canoAutomatizado = FindFirstObjectByType<AutoBallPipe>();
        Exigir(gerenciador.canoAutomatizado != null, "Atribua o lançador de bolas da liga.");
        interfacePartida.duracaoRealEmSegundos = duracaoPartida;
        interfacePartida.ativarGoldenGoal = false;
        interfacePartida.cronometroRealRegressivo = true;
        situacao = "Liga preparada. O progresso será salvo automaticamente.";
    }

    private void Start()
    {
        if (!preparada || falhou) return;
        if (!Tentar(IniciarProximaRodada)) PausarComErro(ultimoErro);
    }

    private AgendaSalva CarregarOuCriarAgenda(List<string> ids)
    {
        string caminho = Path.Combine(PastaDaTemporada, "calendario_pokemon.json");
        if (File.Exists(caminho))
        {
            var salva = JsonUtility.FromJson<AgendaSalva>(File.ReadAllText(caminho, Encoding.UTF8));
            Exigir(salva != null && salva.versao == 1 && salva.liga == ligaId && salva.temporada == temporadaId &&
                salva.quantidadePokemon == quantidadePokemon && salva.quantidadeRodadas == quantidadeRodadas &&
                salva.sementeConfigurada == sementeCalendario,
                "A configuração difere da temporada salva. Restaure os ajustes ou use outra Temporada Id.");
            LigaPokemonModelo.Validar(salva.calendario);
            Exigir(salva.calendario.quantidadeRodadas == salva.quantidadeRodadas &&
                (salva.sementeConfigurada == 0 || salva.calendario.semente == salva.sementeConfigurada),
                "O calendário salvo não corresponde à quantidade de rodadas ou à semente da temporada.");
            Exigir(salva.calendario.participantes.SequenceEqual(ids),
                "Os Pokémon ou sua ordem mudaram. Restaure a lista ou use outra Temporada Id.");
            return salva;
        }
        Exigir(!File.Exists(Path.Combine(PastaDaTemporada, "temporada.json")),
            "Há um histórico salvo sem o calendário Pokémon. Preserve os arquivos e use outra Temporada Id.");
        int semente = sementeCalendario != 0 ? sementeCalendario : Guid.NewGuid().GetHashCode() & int.MaxValue;
        var nova = new AgendaSalva
        {
            liga = ligaId,
            temporada = temporadaId,
            quantidadePokemon = quantidadePokemon,
            quantidadeRodadas = quantidadeRodadas,
            sementeConfigurada = sementeCalendario,
            calendario = LigaPokemonModelo.Gerar(ids, quantidadeRodadas, semente)
        };
        Directory.CreateDirectory(PastaDaTemporada);
        string temporario = caminho + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporario, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true))
                { writer.Write(JsonUtility.ToJson(nova, true)); writer.Flush(); }
                stream.Flush(true);
            }
            File.Move(temporario, caminho);
        }
        finally { if (File.Exists(temporario)) File.Delete(temporario); }
        return nova;
    }

    private void ConferirConfiguracaoDaTemporada()
    {
        List<PokemonDados> atuais = ConferirParticipantes();
        Exigir(agenda != null && agenda.liga == ligaId && agenda.temporada == temporadaId &&
            agenda.quantidadePokemon == quantidadePokemon && agenda.quantidadeRodadas == quantidadeRodadas &&
            agenda.sementeConfigurada == sementeCalendario &&
            agenda.calendario.participantes.SequenceEqual(atuais.Select(p => p.IdEstavel)),
            "Não altere participantes, rodadas, semente ou identificação durante a temporada. Use outra Temporada Id.");
    }

    private void IniciarProximaRodada()
    {
        ConferirConfiguracaoDaTemporada();
        int proxima;
        Exigir(campeonato.TentarObterProximaRodada(out proxima), campeonato.ultimoErro);
        Exigir(proxima >= 1 && proxima <= quantidadeRodadas + 1,
            "O histórico contém rodadas fora do calendário desta temporada.");
        rodadaAtual = proxima;
        rodadaConfirmada = false;
        aguardandoContinuacao = false;
        esperaEntreRodadas = -1d;
        if (proxima > quantidadeRodadas)
        {
            concluida = true;
            rodadaAtual = quantidadeRodadas;
            situacao = "Temporada concluída. Classificação final salva.";
            StartCoroutine(MostrarClassificacaoFinalSalva());
            return;
        }
        gerenciador.rodadaDesejada = proxima;
        gerenciador.CarregarRodadaDoCSVESeguir();
        Exigir(gerenciador.LigaPreparada,
            string.IsNullOrEmpty(campeonato.ultimoErro) ? "Não foi possível preparar a rodada. Confira a classificação e a abertura." : campeonato.ultimoErro);
        string folga = agenda.calendario.rodadas[proxima - 1].folga;
        PokemonDados pokemonFolga = participantes.Find(p => p != null && p.IdEstavel == folga);
        situacao = "Rodada " + proxima + "/" + quantidadeRodadas + " em andamento." +
            (pokemonFolga != null ? " Folga: " + pokemonFolga.nomePokemon + "." : "");
    }

    private void Update()
    {
        if (!preparada || falhou || concluida || aguardandoContinuacao || gerenciador == null) return;
        if (!gerenciador.RodadaConcluida) return;
        if (!rodadaConfirmada)
        {
            if (!Tentar(() =>
            {
                ConferirConfiguracaoDaTemporada();
                Exigir(campeonato.ConfirmarRodada(), campeonato.ultimoErro);
                rodadaConfirmada = true;
                situacao = "Rodada " + rodadaAtual + " salva e confirmada. Exibindo classificação.";
            })) { PausarComErro(ultimoErro); return; }
        }
        if (!gerenciador.EncerramentoRodadaConcluido) return;
        if (rodadaAtual >= quantidadeRodadas)
        {
            concluida = true;
            situacao = "Temporada concluída. Classificação final salva.";
            return;
        }
        if (umaRodadaPorPlay)
        {
            aguardandoContinuacao = true;
            situacao = "Rodada salva. O próximo Play retoma a seguinte; use Continuar próxima rodada para seguir agora.";
            return;
        }
        if (esperaEntreRodadas < 0d) esperaEntreRodadas = Math.Max(0f, intervaloEntreRodadas);
        esperaEntreRodadas -= Time.unscaledDeltaTime;
        if (esperaEntreRodadas <= 0d && !Tentar(IniciarProximaRodada)) PausarComErro(ultimoErro);
    }

    private IEnumerator MostrarClassificacaoFinalSalva()
    {
        if (gerenciador.canoAutomatizado != null) gerenciador.canoAutomatizado.PararChuva();
        interfacePartida.PararCronometro();
        List<LigaCampeonato.Linha> tabela;
        List<LigaCampeonato.Linha> anterior;
        List<LigaCampeonato.Jogo> jogos;
        int rodadaDeExportacao = campeonato.rodadaParaExportar;
        try
        {
            // A tela final sempre usa a última rodada, mesmo se o usuário
            // deixou uma rodada antiga selecionada para exportar CSVs.
            campeonato.rodadaParaExportar = quantidadeRodadas;
            tabela = campeonato.ObterClassificacao(true);
            anterior = campeonato.ObterClassificacao(false);
            jogos = campeonato.ObterJogosDaRodada();
        }
        finally { campeonato.rodadaParaExportar = rodadaDeExportacao; }
        if (tabela == null || anterior == null || jogos == null)
        {
            PausarComErro(campeonato.ultimoErro);
            yield break;
        }
        yield return classificacaoUI.ExibirTabela(tabela, anterior, fichas, quantidadeRodadas, true, jogos);
    }

    [ContextMenu("Liga Pokémon/Continuar próxima rodada no Play")]
    public void ContinuarNoPlay()
    {
        if (!Application.isPlaying || !preparada || falhou || concluida) return;
        if (!aguardandoContinuacao || !gerenciador.EncerramentoRodadaConcluido) return;
        if (!Tentar(IniciarProximaRodada)) PausarComErro(ultimoErro);
    }

    private void PausarComErro(string mensagem)
    {
        falhou = true;
        ultimoErro = string.IsNullOrWhiteSpace(mensagem) ? "Não foi possível executar a liga." : mensagem;
        situacao = "Liga pausada: " + ultimoErro;
        if (gerenciador != null)
        {
            if (gerenciador.canoAutomatizado != null) gerenciador.canoAutomatizado.PararChuva();
            gerenciador.enabled = false;
        }
        if (interfacePartida != null) interfacePartida.CancelarPartida();
        EventosPartidaPokemon eventos = GetComponent<EventosPartidaPokemon>();
        if (eventos != null) eventos.LimparEventos();
        Debug.LogError(ultimoErro, this);
    }

    private bool Tentar(Action acao)
    {
        ultimoErro = "";
        try { acao(); return true; }
        catch (Exception erro) { ultimoErro = erro.Message; Debug.LogError(ultimoErro, this); return false; }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (gerenciador != null)
        {
            if (gerenciador.canoAutomatizado != null) gerenciador.canoAutomatizado.PararChuva();
            gerenciador.enabled = false;
        }
        if (interfacePartida != null) interfacePartida.CancelarPartida();
        EventosPartidaPokemon eventos = GetComponent<EventosPartidaPokemon>();
        if (eventos != null) eventos.LimparEventos();
    }

    private void OnDestroy()
    {
        foreach (TeamData ficha in fichas) DestruirTemporario(ficha);
        fichas.Clear();
        DestruirTemporario(temaEmUso);
        DestruirTemporario(csv);
    }

    private static void DestruirTemporario(UnityEngine.Object objeto)
    {
        if (objeto == null) return;
        if (Application.isPlaying) Destroy(objeto); else DestroyImmediate(objeto);
    }

    private static bool IdentificadorValido(string texto)
    {
        return !string.IsNullOrEmpty(texto) && Regex.IsMatch(texto, @"\A[A-Za-z0-9_-]{1,80}\z");
    }

    private static bool TempoValido(float valor, float minimo)
    {
        return !float.IsNaN(valor) && !float.IsInfinity(valor) && valor >= minimo;
    }

    private static void Exigir(bool condicao, string mensagem)
    {
        if (!condicao) throw new InvalidOperationException(mensagem);
    }
}
