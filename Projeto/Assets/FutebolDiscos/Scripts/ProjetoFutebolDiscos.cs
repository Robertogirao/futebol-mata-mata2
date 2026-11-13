using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace FutebolDeDiscos
{
    public enum FormatoDiscos { Liga = 0, MataMata = 1 }

    /// <summary>One configuration panel; the existing competition engines own their results.</summary>
    [DefaultExecutionOrder(-300), DisallowMultipleComponent]
    public sealed class ProjetoFutebolDiscos : MonoBehaviour
    {
        public string nomeCampeonato = "Disk Football Championship";
        public string competicaoId = "futebol_discos", temporadaId = "temporada_01";
        public FormatoDiscos formato = FormatoDiscos.Liga;
        public TemaCompeticao tema;
        public Sprite fundoCampo;
        public Sprite fundoExternoArena;
        public float opacidadeFundoExterno = 1f;
        public bool sobreporDecoracaoExterna;
        public int quantidadeTimes = 8, quantidadeRodadas = 7, semente;
        public List<TeamData> participantes = new List<TeamData>();
        public bool sortearConfrontos = true, permitirFolgas, umaRodadaOuFasePorPlay;
        public DistribuicaoMataMata distribuicao = DistribuicaoMataMata.ConfrontosNaOrdemDaLista;
        public int discosPorTime = 3;
        public float duracaoPartida = 45f, tempoAbertura = 5f, tempoFotos = 4f,
            tempoTransicaoFotos = 0.8f, tempoTabela = 7f, intervaloEntrePartidas = 2f;
        public float velocidadeDiscos = 3f, velocidadeMaximaBola = 15f, forcaChute = 7f,
            tamanhoDisco = 1f, tamanhoBola = 0.42f, pausaAposGol = 1.1f;
        public bool limitarDiscosNaMetade = true, usarForcaDosTimes = true;
        public bool reporBolaPresaNoCanto = true;
        public float tempoBolaPresaNoCanto = 3f, alcanceCantoBolaPresa = 1.6f, pausaReposicaoBolaPresa = .8f;
        public float limiarCores = 45f;
        public Color corGramado = new Color(0.035f, 0.22f, 0.22f), corLinhas = new Color(0.25f, 0.93f, 0.92f);
        public bool mostrarTagsJogadores = true, mostrarMarcaCapitao = true, usarFotoNoDiscoCapitao = true;
        public bool mostrarScannerFotos = true;
        public float velocidadeScannerFotos = 1f;
        public float tamanhoFonteTags = 4.5f, larguraTags = 2.6f;
        public string textoNoCampo = "DISC FOOTBALL", tituloRodape = "DISC FOOTBALL", subtituloRodape = "AUTOMATIC DISC FOOTBALL";
        public bool mostrarTextoNoCampo = true, mostrarTituloRodape = true, mostrarSubtituloRodape = true, mostrarDetalhesFormato = true;
        public RoteiroTransmissaoDiscos roteiroTransmissao = new RoteiroTransmissaoDiscos();
        [HideInInspector] public string situacao, ultimoErro;

        public bool Preparado { get; private set; }
        public bool CompeticaoConcluida { get; private set; }
        public int SementeEfetiva { get { return temporada != null ? temporada.sementeEfetiva : semente; } }
        public string PastaDaTemporada
        {
            get
            {
                if (!IdValido(competicaoId) || !IdValido(temporadaId))
                    throw new InvalidOperationException("Use letras, números, _ e - nos identificadores.");
                return Path.Combine(Application.persistentDataPath, "Discos", competicaoId, temporadaId);
            }
        }

        private TournamentManager manager;
        private LigaCampeonato liga;
        private MataMataCampeonato mata;
        private UniversalUI ui;
        private ClassificacaoUI tabela;
        private AberturaJogoUI abertura;
        private PartidaDiscos partida;
        private CampoDiscos campo;
        private HudFutebolDiscos hud;
        private TemporadaDiscos temporada;
        private TemaCompeticao temaEmUso;
        private TextAsset calendario;
        private bool rodadaConfirmada, aguardandoContinuar, rodadaEncerrada;
        private float espera;

        private void Awake()
        {
            // Enabled-state validation belongs in Start, after ALL scene Awakes.
            manager = GetComponent<TournamentManager>();
            if (manager != null) manager.iniciarAutomaticamente = false;
        }

        private void Start()
        {
            try
            {
                if (!ValidarConfiguracao()) throw new InvalidOperationException(ultimoErro);
                ObterComponentes();
                temaEmUso = tema != null ? Instantiate(tema) : ScriptableObject.CreateInstance<TemaCompeticao>();
                temaEmUso.hideFlags = HideFlags.DontSave;
                temaEmUso.competicaoId = competicaoId;
                temaEmUso.nomeCompeticao = nomeCampeonato;
                ConfigurarApresentacao();
                string pastaResultados = Path.Combine(Application.persistentDataPath,
                    formato == FormatoDiscos.Liga ? "Ligas" : "MataMata", competicaoId, temporadaId);
                bool historico = File.Exists(Path.Combine(pastaResultados,
                    formato == FormatoDiscos.Liga ? "temporada.json" : "torneio.json"));
                temporada = new RepositorioTemporadaDiscos(PastaDaTemporada, new SerializadorUnity())
                    .CarregarOuCriar(CriarDadosConfiguracao(), historico);

                campo = new GameObject("Disc Football Stadium").AddComponent<CampoDiscos>();
                campo.transform.SetParent(transform, false);
                campo.Construir(this);
                hud = gameObject.AddComponent<HudFutebolDiscos>();
                hud.Construir(this, ui, GetComponent<GoalBannerUI>());
                partida.Configurar(this, campo, hud);
                Preparado = true;
                situacao = "Temporada preparada. Semente: " + SementeEfetiva;
                if (formato == FormatoDiscos.Liga) PrepararLiga();
                else PrepararMataMata();
            }
            catch (Exception erro) { Falhar(erro.Message); }
        }

        private void ObterComponentes()
        {
            manager = ExigirComponente<TournamentManager>();
            liga = ExigirComponente<LigaCampeonato>();
            mata = ExigirComponente<MataMataCampeonato>();
            ui = ExigirComponente<UniversalUI>();
            tabela = ExigirComponente<ClassificacaoUI>();
            abertura = ExigirComponente<AberturaJogoUI>();
            partida = ExigirComponente<PartidaDiscos>();
            ExigirComponente<GoalBannerUI>();
            ExigirComponente<ChaveamentoUI>();
            if (manager.transform.lossyScale != Vector3.one || manager.transform.position != Vector3.zero)
                throw new InvalidOperationException("Mantenha o manager na origem e com escala 1.");
        }

        private T ExigirComponente<T>() where T : Behaviour
        {
            T componente = GetComponent<T>();
            if (componente == null || !componente.isActiveAndEnabled)
                throw new InvalidOperationException("Ative o componente " + typeof(T).Name + " antes do Play.");
            return componente;
        }

        private void ConfigurarApresentacao()
        {
            if (roteiroTransmissao == null) roteiroTransmissao = new RoteiroTransmissaoDiscos();
            abertura.ConfigurarRoteiro(roteiroTransmissao);
            tabela.ConfigurarRoteiro(roteiroTransmissao);
            GetComponent<ChaveamentoUI>().ConfigurarRoteiro(roteiroTransmissao);
            manager.iniciarAutomaticamente = false;
            manager.simuladorDiscos = partida;
            manager.campeonato = liga;
            manager.mataMata = mata;
            manager.classificacaoUI = tabela;
            manager.aberturaJogoUI = abertura;
            manager.todosOsTimesDisponiveis = participantes.Where(t => t != null).ToList();
            manager.logoCampeonato = temaEmUso.logoCompeticao;
            manager.tempoApresentacaoJogadores = tempoFotos;
            manager.tempoDeslocamentoFichas = tempoTransicaoFotos;
            // UniversalUI already owns the post-game pause and waits for goal banners.
            // Scheduling the same pause again doubled the interval in league mode.
            manager.intervaloEntreJogos = 0f;
            manager.limiarCoresParecidas = limiarCores;
            manager.jogandoFaseDeGrupos = false;
            ui.duracaoRealEmSegundos = duracaoPartida;
            ui.tempoCongeladoPosJogo = intervaloEntrePartidas;
            ui.ConfigurarEfeitoFotos(mostrarScannerFotos, velocidadeScannerFotos);
            tabela.nomeDaLiga = nomeCampeonato;
            tabela.logo = temaEmUso.logoCompeticao;
            tabela.logoCanal = temaEmUso.logoCanal;
            tabela.fonte = temaEmUso.fonteTexto != null ? temaEmUso.fonteTexto : TMP_Settings.defaultFontAsset;
            tabela.corFundo = temaEmUso.corPrimaria;
            tabela.corPainel = temaEmUso.corPainel;
            tabela.textoClaro = temaEmUso.corTexto;
            tabela.verde = temaEmUso.corDestaque;
            tabela.tempoLeituraAbertura = tabela.tempoLeituraEncerramento = tempoTabela;
            tabela.tempoEntradaAbertura = tabela.tempoEntradaEncerramento = 0.8f;
            tabela.tempoSaida = 0.6f;
            tabela.manterTabelaFinalNaTela = true;
            abertura.fundo = temaEmUso.fundoAbertura;
            abertura.corFundo = temaEmUso.corPrimaria;
            abertura.corPainel = temaEmUso.corPainel;
            abertura.corAcento = temaEmUso.corDestaque;
            abertura.logoCanal = temaEmUso.logoCanal;
            abertura.fonteTitulo = temaEmUso.fonteTitulo;
            abertura.fonteNomes = temaEmUso.fonteTexto;
            abertura.tempoTotal = tempoAbertura;
            abertura.somAbertura = temaEmUso.somAbertura;
        }

        private void PrepararLiga()
        {
            manager.modoLiga = true;
            manager.modoMataMata = false;
            manager.mostrarClassificacao = manager.mostrarAbertura = true;
            ui.ativarGoldenGoal = false;
            liga.ligaId = competicaoId;
            liga.temporadaId = temporadaId;
            calendario = new TextAsset(CalendarioDiscos.ParaCsv(temporada.calendarioLiga));
            calendario.hideFlags = HideFlags.DontSave;
            manager.arquivoCsvCalendario = calendario;
            int proxima;
            if (!liga.TentarObterProximaRodada(out proxima)) throw new InvalidOperationException(liga.ultimoErro);
            if (proxima > quantidadeRodadas)
            {
                CompeticaoConcluida = true;
                situacao = "Liga concluída. Classificação final carregada do salvamento.";
                StartCoroutine(ExibirLigaConcluida());
            }
            else IniciarRodada(proxima);
        }

        private void IniciarRodada(int numero)
        {
            rodadaConfirmada = rodadaEncerrada = aguardandoContinuar = false;
            espera = 0f;
            manager.rodadaDesejada = numero;
            tabela.Ocultar();
            manager.CarregarRodadaDoCSVESeguir();
            if (!manager.LigaPreparada)
                throw new InvalidOperationException(string.IsNullOrEmpty(liga.ultimoErro) ? "Não foi possível preparar a rodada." : liga.ultimoErro);
            situacao = "Rodada " + numero + " de " + quantidadeRodadas + ". Cada resultado será salvo automaticamente.";
        }

        private IEnumerator ExibirLigaConcluida()
        {
            // Export selectors must not change which finished season is shown.
            int anterior = liga.rodadaParaExportar;
            List<LigaCampeonato.Linha> final, inicial;
            List<LigaCampeonato.Jogo> jogos;
            try
            {
                liga.rodadaParaExportar = quantidadeRodadas;
                final = liga.ObterClassificacao(true);
                inicial = liga.ObterClassificacao(false);
                jogos = liga.ObterJogosDaRodada();
            }
            finally { liga.rodadaParaExportar = anterior; }
            if (final == null || inicial == null || jogos == null) { Falhar(liga.ultimoErro); yield break; }
            yield return tabela.ExibirTabela(final, inicial, manager.todosOsTimesDisponiveis, quantidadeRodadas, true, jogos);
        }

        private void PrepararMataMata()
        {
            manager.modoMataMata = true;
            manager.modoLiga = false;
            manager.mostrarClassificacao = manager.mostrarAbertura = false;
            mata.tema = temaEmUso;
            mata.apresentacao = GetComponent<ChaveamentoUI>();
            mata.edicaoId = temporadaId;
            mata.tamanhoChave = (TamanhoMataMata)quantidadeTimes;
            mata.participantes = new List<TeamData>(participantes);
            mata.distribuicao = distribuicao;
            mata.permitirFolgas = permitirFolgas;
            mata.sortearParticipantes = sortearConfrontos;
            mata.sementeSorteio = SementeEfetiva;
            mata.salvarProgresso = true;
            mata.gravarSomenteUmaFase = umaRodadaOuFasePorPlay;
            mata.tempoInicio = mata.tempoPreJogo = tempoAbertura;
            mata.tempoFotosJogadores = tempoFotos;
            mata.tempoTransicaoFotos = tempoTransicaoFotos;
            mata.tempoFinal = tempoTabela;
            mata.Iniciar(manager);
            if (!string.IsNullOrEmpty(mata.ultimoErro)) throw new InvalidOperationException(mata.ultimoErro);
            situacao = "Mata-mata preparado. O chaveamento e cada resultado serão salvos automaticamente.";
        }

        private void Update()
        {
            if (!Preparado || CompeticaoConcluida) return;
            if (manager.SalvamentoPendente)
            {
                situacao = "Salvamento pendente. Corrija o acesso à pasta e use Continuar no Play.";
                return;
            }
            if (formato == FormatoDiscos.MataMata)
            {
                if (!string.IsNullOrEmpty(mata.ultimoErro)) { Falhar(mata.ultimoErro); return; }
                if (mata.Concluido) { CompeticaoConcluida = true; situacao = "Mata-mata concluído. Campeão salvo."; }
                return;
            }
            if (!manager.RodadaConcluida) return;
            if (!rodadaConfirmada)
            {
                if (!liga.ConfirmarRodada()) { Falhar(liga.ultimoErro); return; }
                rodadaConfirmada = true;
            }
            if (!manager.EncerramentoRodadaConcluido) return;
            if (manager.rodadaDesejada >= quantidadeRodadas)
            {
                CompeticaoConcluida = true;
                situacao = "Liga concluída. Classificação final salva.";
                return;
            }
            if (!rodadaEncerrada)
            {
                rodadaEncerrada = true;
                // The preceding game has already completed its configured pause.
                espera = 0f;
                aguardandoContinuar = umaRodadaOuFasePorPlay;
                if (aguardandoContinuar)
                    situacao = "Rodada salva. Use Continuar no Play ou o próximo Play para seguir.";
            }
            if (aguardandoContinuar) return;
            espera -= Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;
            if (espera <= 0f)
            {
                try { IniciarRodada(manager.rodadaDesejada + 1); }
                catch (Exception erro) { Falhar(erro.Message); }
            }
        }

        public void ContinuarNoPlay()
        {
            if (!Preparado || CompeticaoConcluida || !Application.isPlaying) return;
            if (manager.SalvamentoPendente) { manager.TentarSalvarUltimoResultado(); return; }
            if (formato == FormatoDiscos.MataMata) { mata.ContinuarNoPlay(); return; }
            if (!aguardandoContinuar || !manager.EncerramentoRodadaConcluido) return;
            try { IniciarRodada(manager.rodadaDesejada + 1); }
            catch (Exception erro) { Falhar(erro.Message); }
        }

        public bool ValidarConfiguracao()
        {
            try
            {
                RepositorioTemporadaDiscos.ValidarConfiguracao(CriarDadosConfiguracao());
                if (string.IsNullOrWhiteSpace(nomeCampeonato)) throw new InvalidOperationException("Informe o nome da competição.");
                if (participantes == null || participantes.Count != quantidadeTimes)
                    throw new InvalidOperationException("Ajuste a lista para a quantidade de vagas escolhida.");
                foreach (TeamData equipe in participantes.Where(t => t != null))
                {
                    if (string.IsNullOrWhiteSpace(equipe.name) || equipe.name != equipe.name.Trim())
                        throw new InvalidOperationException("Os nomes dos assets TeamData não podem ter espaços nas extremidades.");
                    if (string.IsNullOrWhiteSpace(equipe.nomeEquipe) || equipe.elenco == null ||
                        !equipe.elenco.Any(j => j != null && !string.IsNullOrWhiteSpace(j.nomeJogador)))
                        throw new InvalidOperationException("Confira o nome e cadastre um representante em " + equipe.name + ".");
                }
                if (!Faixa(tamanhoDisco, 0.6f, 1.4f) || !Faixa(tamanhoBola, 0.25f, 0.65f) ||
                    !Faixa(velocidadeDiscos, 1f, 8f) || !Faixa(velocidadeMaximaBola, 4f, 25f) ||
                    !Faixa(forcaChute, 1f, 15f) || !Faixa(pausaAposGol, 0.2f, 5f))
                    throw new InvalidOperationException("Mantenha os controles da física dentro dos limites do painel.");
                foreach (float tempo in new[] { tempoAbertura, tempoFotos, tempoTransicaoFotos, tempoTabela, intervaloEntrePartidas })
                    if (!Faixa(tempo, 0f, 600f)) throw new InvalidOperationException("Os tempos devem ficar entre 0 e 600 segundos.");
                if (!Faixa(limiarCores, 0f, 100f)) throw new InvalidOperationException("O contraste de cores deve ficar entre 0 e 100.");
                if (!Faixa(tamanhoFonteTags, 2f, 8f) || !Faixa(larguraTags, 1f, 4f))
                    throw new InvalidOperationException("Confira o tamanho da fonte e a largura das tags dos jogadores.");
                if (!Faixa(opacidadeFundoExterno, 0f, 1f) || !Faixa(velocidadeScannerFotos, .1f, 3f))
                    throw new InvalidOperationException("Confira a opacidade do fundo externo e a velocidade da linha das fotos.");
                if (!Faixa(tempoBolaPresaNoCanto, 1f, 10f) || !Faixa(alcanceCantoBolaPresa, .8f, 3f) ||
                    !Faixa(pausaReposicaoBolaPresa, .2f, 3f))
                    throw new InvalidOperationException("Confira o tempo de bola presa, a área dos cantos e a pausa da reposição.");
                ultimoErro = "";
                return true;
            }
            catch (Exception erro) { ultimoErro = erro.Message; return false; }
        }

        private DadosConfiguracaoDiscos CriarDadosConfiguracao()
        {
            return new DadosConfiguracaoDiscos
            {
                competicaoId = competicaoId, temporadaId = temporadaId, formato = (int)formato,
                quantidadeTimes = quantidadeTimes, quantidadeRodadas = quantidadeRodadas, semente = semente,
                distribuicao = (int)distribuicao, sortearConfrontos = sortearConfrontos, permitirFolgas = permitirFolgas,
                ids = (participantes ?? new List<TeamData>()).Where(t => t != null).Select(t => t.name).ToList(),
                discosPorTime = discosPorTime, duracaoPartida = duracaoPartida, velocidadeDiscos = velocidadeDiscos,
                velocidadeMaximaBola = velocidadeMaximaBola, forcaChute = forcaChute, tamanhoDisco = tamanhoDisco,
                tamanhoBola = tamanhoBola, pausaAposGol = pausaAposGol, limitarDiscosNaMetade = limitarDiscosNaMetade,
                usarForcaDosTimes = usarForcaDosTimes
            };
        }

        private void Falhar(string mensagem)
        {
            ultimoErro = string.IsNullOrEmpty(mensagem) ? "Falha ao preparar a competição." : mensagem;
            situacao = "Sequência pausada. " + ultimoErro;
            Preparado = false;
            if (partida != null) partida.PararPartida();
            if (ui != null) ui.CancelarPartida();
            if (mata != null) mata.CancelarApresentacao();
            if (manager != null) manager.enabled = false;
            if (hud != null) hud.MostrarEstado("COMPETITION PAUSED — CHECK CONFIGURATION");
            Debug.LogError(ultimoErro, this);
        }

        private static bool IdValido(string value) { return value != null && Regex.IsMatch(value, @"\A[A-Za-z0-9_-]{1,80}\z"); }
        private static bool Faixa(float value, float min, float max) { return !float.IsNaN(value) && !float.IsInfinity(value) && value >= min && value <= max; }
        private void OnDisable()
        {
            Preparado = false;
            StopAllCoroutines();
            if (manager != null) manager.enabled = false;
            if (partida != null) partida.PararPartida();
            if (ui != null) ui.CancelarPartida();
            if (mata != null) mata.CancelarApresentacao();
        }
        private void OnDestroy()
        {
            if (hud != null) hud.Limpar();
            if (campo != null) campo.Limpar();
            if (temaEmUso != null) Destroy(temaEmUso);
            if (calendario != null) Destroy(calendario);
        }

        private sealed class SerializadorUnity : ISerializadorTemporadaDiscos
        {
            public string ParaJson(TemporadaDiscos dados) { return JsonUtility.ToJson(dados, true); }
            public TemporadaDiscos DeJson(string json) { return JsonUtility.FromJson<TemporadaDiscos>(json); }
        }
    }
}
