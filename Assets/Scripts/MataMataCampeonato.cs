using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public enum TamanhoMataMata
{
    Final = 2,
    Semifinais = 4,
    Quartas = 8,
    Oitavas = 16,
    DezesseisAvos = 32
}

public enum DistribuicaoMataMata
{
    ConfrontosNaOrdemDaLista,
    CabecasDeChave
}

/// <summary>Coordinates bracket, persistence and the existing gameplay.</summary>
public sealed class MataMataCampeonato : MonoBehaviour
{
    [Header("Identidade e apresentação")]
    public TemaCompeticao tema;
    public ChaveamentoUI apresentacao;
    public ArenaTema arena;
    public string edicaoId = "temporada_01";

    [Header("Participantes")]
    public TamanhoMataMata tamanhoChave = TamanhoMataMata.DezesseisAvos;
    [Tooltip("São usados os primeiros campos, conforme o tamanho da chave. Na ordem direta: 1 x 2, 3 x 4...")]
    public List<TeamData> participantes = new List<TeamData>();
    public DistribuicaoMataMata distribuicao = DistribuicaoMataMata.ConfrontosNaOrdemDaLista;
    public bool permitirFolgas = false;
    public bool sortearParticipantes = false;
    public int sementeSorteio = 2026;

    [Header("Gravação e continuidade")]
    public bool salvarProgresso = true;
    public bool gravarSomenteUmaFase = true;

    [Header("Duração das apresentações (segundos)")]
    [Min(0f)] public float tempoInicio = 15f;
    [Min(0f)] public float tempoPreJogo = 10f;

    [Min(0f)]
    [InspectorName("Tempo Fotos Jogadores")]
    [Tooltip("Segundos com as fichas ampliadas no centro da arena, após abrir a cortina. O jogo permanece pausado.")]
    public float tempoFotosJogadores = 5f;

    [Min(0f)]
    [InspectorName("Tempo Transição Fotos")]
    [Tooltip("Segundos para as fichas diminuírem e voltarem aos cantos. O cronômetro, o apito e as bolas começam depois.")]
    public float tempoTransicaoFotos = 0.8f;

    [Min(0f)] public float tempoFinal = 20f;

    [Header("Regravação de uma partida")]
    [Min(1)] public int faseParaRegravar = 1;
    [Min(1)] public int jogoParaRegravar = 1;

    [Header("Diagnóstico")]
    [TextArea(2, 5)] public string ultimoErro;

    [Serializable]
    public sealed class Arquivo
    {
        public int versao = 1;
        public string competicao;
        public string edicao;
        public int distribuicao;
        public bool sorteado;
        public int semente;
        public List<string> idsConfigurados = new List<string>();
        public MataMataModelo.Estado estado;
        public string salvoEmUtc;
    }

    private TournamentManager gerenciador;
    private Arquivo arquivoAtual;
    private List<TeamData> equipes = new List<TeamData>();
    private Dictionary<string, TeamData> porId;
    private Confronto confrontoAtual;
    private string partidaAtualId;
    private Coroutine rotina;
    private bool ocupado;
    private bool aguardandoPartida;
    private bool persistirSessao;
    private string pastaSessao;
    private int faseDaGravacao;
    private int versaoRotina;

    public bool Ocupado { get { return ocupado || aguardandoPartida; } }

    public string PastaTorneio
    {
        get
        {
            Exigir(tema != null && tema.PossuiIdentificadorValido(),
                "Escolha um tema com identificador válido.");
            Exigir(IdValido(edicaoId), "Edição aceita letras, números, _ e -.");
            return Path.Combine(Application.persistentDataPath,
                "MataMata", tema.competicaoId, edicaoId);
        }
    }

    public bool ValidarConfiguracao()
    {
        return Tentar(() => ConferirConfiguracao());
    }

    private List<TeamData> ConferirConfiguracao()
    {
        string pasta = PastaTorneio;
        int vagas = (int)tamanhoChave;
        Exigir(new[] { 2, 4, 8, 16, 32 }.Contains(vagas), "Tamanho de chave inválido.");
        Exigir(Enum.IsDefined(typeof(DistribuicaoMataMata), distribuicao), "Distribuição inválida.");
        var usados = (participantes ?? new List<TeamData>()).Take(vagas).Where(t => t != null).ToList();
        Exigir(usados.Count >= 2, "Cadastre pelo menos dois clubes.");
        Exigir(permitirFolgas || usados.Count == vagas,
            "Esta chave exige " + vagas + " clubes. Faltam " + (vagas - usados.Count) +
            ". Complete as fichas ou escolha um tamanho menor. As cenas DEMO já têm 16 clubes.");
        Exigir(usados.Select(Id).Distinct(StringComparer.Ordinal).Count() == usados.Count,
            "Há fichas repetidas ou nomes de assets repetidos nos participantes.");
        foreach (var t in usados)
            Exigir(t.elenco != null && t.elenco.Any(p => p != null &&
                !string.IsNullOrWhiteSpace(p.nomeJogador)),
                "Cadastre pelo menos um jogador em " + t.nomeEquipe + ".");
        return usados;
    }

    public void Iniciar(TournamentManager manager)
    {
        if (Ocupado) return;
        gerenciador = manager;
        if (!Tentar(() =>
        {
            Exigir(manager != null && manager.isActiveAndEnabled, "TournamentManager indisponível.");
            equipes = ConferirConfiguracao();
            porId = equipes.ToDictionary(Id, t => t, StringComparer.Ordinal);
            persistirSessao = salvarProgresso;
            pastaSessao = PastaTorneio;
            arquivoAtual = CarregarOuCriar();
            if (apresentacao == null) apresentacao = GetComponent<ChaveamentoUI>();
            if (apresentacao == null) apresentacao = gameObject.AddComponent<ChaveamentoUI>();
            Exigir(apresentacao.isActiveAndEnabled, "Ative ChaveamentoUI.");
            if (arena == null) arena = GetComponent<ArenaTema>();
            if (arena != null) arena.Aplicar(tema);
            if (manager.classificacaoUI == null) manager.classificacaoUI = manager.GetComponent<ClassificacaoUI>();
            if (manager.classificacaoUI != null) manager.classificacaoUI.Ocultar();
            if (manager.telaTorneio != null) manager.telaTorneio.SetActive(false);
            if (manager.aberturaJogoUI != null) manager.aberturaJogoUI.Ocultar();
            manager.modoLiga = false;
            manager.mostrarClassificacao = false;
            manager.mostrarAbertura = false;
            manager.logoCampeonato = tema.logoCompeticao;
            Exigir(UniversalUI.Instance != null && manager.canoAutomatizado != null,
                "Confira UniversalUI e o lançador da arena.");
            Exigir(UniversalUI.Instance.isActiveAndEnabled && manager.canoAutomatizado.isActiveAndEnabled,
                "Ative UniversalUI e AutoBallPipe antes de iniciar.");
            UniversalUI.Instance.ativarGoldenGoal = true;
            UniversalUI.Instance.PararCronometro();
            manager.canoAutomatizado.PararChuva();
            var proxima = MataMataModelo.Proxima(arquivoAtual.estado);
            faseDaGravacao = proxima != null ? proxima.fase : -1;
        }))
        {
            string erro = ultimoErro;
            Interromper(erro);
            return;
        }
        IniciarRotina(true);
    }

    private void IniciarRotina(bool aberturaGeral)
    {
        if (!isActiveAndEnabled || Ocupado) return;
        ocupado = true;
        int token = ++versaoRotina;
        Coroutine iniciada = StartCoroutine(ExecutarSeguro(Sequencia(aberturaGeral, token), token));
        if (ocupado && token == versaoRotina) rotina = iniciada;
    }

    private IEnumerator ExecutarSeguro(IEnumerator sequencia, int token)
    {
        var pilha = new Stack<IEnumerator>();
        pilha.Push(sequencia);
        while (pilha.Count > 0 && this != null && isActiveAndEnabled && token == versaoRotina)
        {
            bool continua = false;
            object passo = null;
            Exception falha = null;
            try
            {
                continua = pilha.Peek().MoveNext();
                if (continua) passo = pilha.Peek().Current;
            }
            catch (Exception e) { falha = e; }
            if (falha != null)
            {
                Interromper("Falha na sequência do mata-mata: " + falha.Message);
                yield break;
            }
            if (!continua) { pilha.Pop(); continue; }
            var aninhada = passo as IEnumerator;
            if (aninhada != null) pilha.Push(aninhada);
            else yield return passo;
        }
    }

    private IEnumerator Sequencia(bool aberturaGeral, int token)
    {
        if (aberturaGeral && MataMataModelo.Proxima(arquivoAtual.estado) != null)
        {
            apresentacao.CobrirTela();
            Time.timeScale = 0f;
            yield return apresentacao.Exibir(arquivoAtual.estado, equipes, tema, tempoInicio);
            if (!ExibicaoValida(token)) { Interromper("A apresentação inicial foi interrompida."); yield break; }
        }

        var proxima = MataMataModelo.Proxima(arquivoAtual.estado);
        bool fimDaFase = proxima != null && gravarSomenteUmaFase && proxima.fase != faseDaGravacao;
        if (proxima == null || fimDaFase)
        {
            apresentacao.CobrirTela();
            Time.timeScale = 0f;
            yield return apresentacao.Exibir(arquivoAtual.estado, equipes, tema, tempoFinal, null, true);
            if (!ExibicaoValida(token)) { Interromper("O encerramento foi interrompido."); yield break; }
            ocupado = false;
            rotina = null;
            Debug.Log(proxima == null ? "Mata-mata concluído. Campeão salvo." :
                "Fase concluída. O próximo Play retoma a fase seguinte. Para seguir agora, use Mata-mata/Continuar no Play.", this);
            yield break;
        }

        apresentacao.CobrirTela();
        Time.timeScale = 0f;
        yield return apresentacao.Exibir(arquivoAtual.estado, equipes, tema, tempoPreJogo, proxima.id);
        if (!ExibicaoValida(token)) { Interromper("A abertura da partida foi interrompida."); yield break; }

        if (!Tentar(() =>
        {
            ConferirSessao();
            var atual = MataMataModelo.Proxima(arquivoAtual.estado);
            Exigir(atual != null && atual.id == proxima.id, "A partida pendente mudou durante a abertura.");
            confrontoAtual = new Confronto(porId[atual.a], porId[atual.b]);
            partidaAtualId = atual.id;
            aguardandoPartida = true;
            UniversalUI.Instance.ativarGoldenGoal = true;
            if (gerenciador.textoTituloFase != null)
                gerenciador.textoTituloFase.text = MataMataModelo.NomeFase(
                    arquivoAtual.estado.fases[atual.fase].vagas);
            gerenciador.JogarConfronto(confrontoAtual, float.MaxValue);
            Exigir(gerenciador.EstaPreparandoConfronto(confrontoAtual),
                "A partida não foi preparada. Confira a arena e o prefab da bola.");
            apresentacao.RevelarArena();
        }))
        {
            string erro = ultimoErro;
            Interromper(erro);
            yield break;
        }

        // O cronômetro e as bolas só são liberados após a apresentação das fichas.
        while (this != null && isActiveAndEnabled && token == versaoRotina &&
               gerenciador != null && gerenciador.EstaPreparandoConfronto(confrontoAtual))
            yield return null;

        if (this == null || !isActiveAndEnabled || token != versaoRotina)
            yield break;

        if (!Tentar(() => Exigir(UniversalUI.Instance != null &&
            UniversalUI.Instance.partidaEmAndamento,
            "A partida não iniciou após a apresentação dos jogadores. Confira a arena.")))
        {
            string erro = ultimoErro;
            Interromper(erro);
            yield break;
        }
        ocupado = false;
        rotina = null;
    }

    private bool ExibicaoValida(int token)
    {
        return this != null && isActiveAndEnabled && token == versaoRotina &&
            apresentacao != null && apresentacao.UltimaExibicaoConcluida;
    }

    public bool RegistrarResultado(Confronto confronto, int golsA, int golsB)
    {
        return Tentar(() =>
        {
            Exigir(aguardandoPartida && ReferenceEquals(confrontoAtual, confronto),
                "O resultado não pertence à partida de mata-mata em andamento.");
            ConferirSessao();
            var copia = JsonUtility.FromJson<Arquivo>(JsonUtility.ToJson(arquivoAtual));
            MataMataModelo.Registrar(copia.estado, partidaAtualId, golsA, golsB);
            Salvar(copia);
            arquivoAtual = copia;
            aguardandoPartida = false;
            confrontoAtual = null;
            partidaAtualId = null;
            ExportarSemInterromper();
        });
    }

    public void AvancarAposResultado()
    {
        IniciarRotina(false);
    }

    [ContextMenu("Mata-mata/Continuar no Play")]
    private void ContinuarNoPlay()
    {
        if (!Application.isPlaying || Ocupado || arquivoAtual == null) return;
        var proxima = MataMataModelo.Proxima(arquivoAtual.estado);
        if (proxima != null) faseDaGravacao = proxima.fase;
        IniciarRotina(true);
    }

    private Arquivo CarregarOuCriar()
    {
        string caminho = Path.Combine(pastaSessao, "torneio.json");
        if (persistirSessao && File.Exists(caminho))
        {
            var salvo = JsonUtility.FromJson<Arquivo>(File.ReadAllText(caminho, Encoding.UTF8));
            ConferirArquivo(salvo);
            return salvo;
        }

        var ids = equipes.Select(Id).ToList();
        var novo = new Arquivo
        {
            competicao = tema.competicaoId,
            edicao = edicaoId,
            distribuicao = (int)distribuicao,
            sorteado = sortearParticipantes,
            semente = sortearParticipantes ? sementeSorteio : 0,
            idsConfigurados = ids,
            estado = MataMataModelo.Criar(CriarSementes(ids), (int)tamanhoChave)
        };
        Salvar(novo);
        return novo;
    }

    private List<string> CriarSementes(List<string> ids)
    {
        var entradas = new List<string>(ids);
        if (sortearParticipantes)
        {
            var random = new System.Random(sementeSorteio);
            for (int i = entradas.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                string t = entradas[i]; entradas[i] = entradas[j]; entradas[j] = t;
            }
        }
        int vagas = (int)tamanhoChave;
        if (distribuicao == DistribuicaoMataMata.ConfrontosNaOrdemDaLista)
        {
            var ordem = MataMataModelo.OrdemSementes(vagas);
            var sementes = new string[entradas.Count];
            int entrada = 0;
            foreach (int rank in ordem)
                if (rank <= sementes.Length) sementes[rank - 1] = entradas[entrada++];
            entradas = sementes.ToList();
        }

        return entradas;
    }

    private void ConferirArquivo(Arquivo salvo)
    {
        Exigir(salvo != null && salvo.versao == 1 && salvo.competicao == tema.competicaoId &&
            salvo.edicao == edicaoId, "Arquivo de outra competição/edição ou versão incompatível.");
        MataMataModelo.Validar(salvo.estado);
        Exigir(salvo.estado.vagas == (int)tamanhoChave && salvo.idsConfigurados != null &&
            salvo.idsConfigurados.SequenceEqual(equipes.Select(Id)) &&
            salvo.distribuicao == (int)distribuicao && salvo.sorteado == sortearParticipantes &&
            salvo.semente == (sortearParticipantes ? sementeSorteio : 0),
            "A configuração difere do torneio salvo. Restaure participantes/ordem/tamanho/sorteio " +
            "ou use outra Edição Id. Para reiniciar, arquive o progresso pelo menu.");
        Exigir(new HashSet<string>(salvo.estado.participantes, StringComparer.Ordinal)
            .SetEquals(salvo.idsConfigurados), "Os clubes do arquivo não correspondem à configuração.");
        Exigir(salvo.estado.sementes.SequenceEqual(CriarSementes(salvo.idsConfigurados)),
            "A distribuição da chave salva não corresponde à ordem ou ao sorteio configurado.");
    }

    private void ConferirSessao()
    {
        Exigir(PastaTorneio == pastaSessao && salvarProgresso == persistirSessao,
            "Não altere a identificação ou o modo de salvar durante a gravação.");
        var atuais = ConferirConfiguracao();
        Exigir(atuais.Select(Id).SequenceEqual(equipes.Select(Id)), "Os participantes mudaram durante a gravação.");
        ConferirArquivo(arquivoAtual);
    }

    private void Salvar(Arquivo arquivo)
    {
        MataMataModelo.Validar(arquivo.estado);
        arquivo.salvoEmUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        if (!persistirSessao) return;
        Directory.CreateDirectory(pastaSessao);
        EscreverAtomico(Path.Combine(pastaSessao, "torneio.json"), JsonUtility.ToJson(arquivo, true));
    }

    private static void EscreverAtomico(string caminho, string texto)
    {
        string temporario = caminho + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporario, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true))
                { writer.Write(texto); writer.Flush(); }
                stream.Flush(true);
            }
            if (File.Exists(caminho)) File.Replace(temporario, caminho, caminho + ".bak");
            else File.Move(temporario, caminho);
        }
        finally
        {
            if (File.Exists(temporario)) { try { File.Delete(temporario); } catch { } }
        }
    }

    [ContextMenu("Mata-mata/Arquivar progresso e iniciar novo torneio")]
    private void MenuNovoTorneio() { NovoTorneio(); }

    public bool NovoTorneio()
    {
        return Tentar(() =>
        {
            Exigir(!Application.isPlaying, "Saia do Play Mode antes de arquivar um torneio.");
            string pasta = PastaTorneio;
            string destino = BackupDoProgresso(pasta, "novo_torneio");
            string caminho = Path.Combine(pasta, "torneio.json");
            Exigir(File.Exists(caminho), "Não há progresso para arquivar.");
            File.Move(caminho, Path.Combine(destino, "torneio_arquivado.json"));
            foreach (string auxiliar in new[] { "torneio.json.bak", "resultados.csv" })
            {
                string origem = Path.Combine(pasta, auxiliar);
                if (File.Exists(origem)) File.Delete(origem); // Already copied into the archive.
            }
            arquivoAtual = null;
            Debug.Log("Torneio arquivado em " + destino + ". O próximo Play inicia um novo.", this);
        });
    }

    [ContextMenu("Mata-mata/Regravar partida selecionada")]
    private void MenuRegravarPartida() { RegravarPartida(); }

    public bool RegravarPartida()
    {
        return Tentar(() =>
        {
            Exigir(!Application.isPlaying, "Saia do Play Mode antes de reabrir uma partida.");
            Exigir(salvarProgresso, "A demonstração não possui progresso salvo.");
            equipes = ConferirConfiguracao();
            porId = equipes.ToDictionary(Id, t => t, StringComparer.Ordinal);
            pastaSessao = PastaTorneio;
            persistirSessao = true;
            string caminho = Path.Combine(pastaSessao, "torneio.json");
            Exigir(File.Exists(caminho), "Não há torneio salvo para regravar.");
            var salvo = JsonUtility.FromJson<Arquivo>(File.ReadAllText(caminho, Encoding.UTF8));
            ConferirArquivo(salvo);
            int f = faseParaRegravar - 1, p = jogoParaRegravar - 1;
            Exigir(f >= 0 && f < salvo.estado.fases.Count && p >= 0 &&
                p < salvo.estado.fases[f].partidas.Count, "Fase ou partida fora da chave.");
            var alvo = salvo.estado.fases[f].partidas[p];
            Exigir(alvo.concluida && !alvo.bye, "Escolha uma partida real já concluída.");
            string backup = BackupDoProgresso(pastaSessao, "antes_de_regravar");
            salvo.estado = MataMataModelo.Reabrir(salvo.estado, alvo.id);
            Salvar(salvo);
            arquivoAtual = salvo;
            ExportarSemInterromper();
            Debug.Log("Partida reaberta. Resultados dependentes foram removidos; outros confrontos preservados. Backup: " + backup, this);
        });
    }

    private static string BackupDoProgresso(string pasta, string motivo)
    {
        string caminho = Path.Combine(pasta, "torneio.json");
        Exigir(File.Exists(caminho), "Não existe progresso salvo nesta edição.");
        string destino = Path.Combine(pasta, "backups", motivo + "_" +
            DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(destino);
        foreach (string nome in new[] { "torneio.json", "torneio.json.bak", "resultados.csv" })
        {
            string origem = Path.Combine(pasta, nome);
            if (File.Exists(origem)) File.Copy(origem, Path.Combine(destino, nome));
        }
        return destino;
    }

    private void ExportarSemInterromper()
    {
        if (!persistirSessao || arquivoAtual == null) return;
        try
        {
            var linhas = new List<string> { "PHASE,MATCH,HOME,AWAY,HOME GOALS,AWAY GOALS,WINNER" };
            foreach (var fase in arquivoAtual.estado.fases)
                foreach (var p in fase.partidas.Where(p => p.concluida))
                    linhas.Add(string.Join(",", new[] { MataMataModelo.NomeFase(fase.vagas),
                        (p.indice + 1).ToString(CultureInfo.InvariantCulture), Nome(p.a), Nome(p.b),
                        p.bye ? "" : p.golsA.ToString(CultureInfo.InvariantCulture),
                        p.bye ? "" : p.golsB.ToString(CultureInfo.InvariantCulture), Nome(p.vencedor) }.Select(Csv)));
            File.WriteAllText(Path.Combine(pastaSessao, "resultados.csv"),
                string.Join("\r\n", linhas) + "\r\n", new UTF8Encoding(true));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Progresso salvo; o CSV auxiliar não foi atualizado: " + e.Message, this);
        }
    }

    private string Nome(string id)
    {
        if (string.IsNullOrEmpty(id)) return "BYE";
        TeamData t;
        return porId != null && porId.TryGetValue(id, out t) ? t.nomeEquipe : id;
    }

    private static string Csv(string texto)
    {
        texto = texto ?? "";
        if (Regex.IsMatch(texto, @"\A\s*[=+@-]")) texto = "'" + texto;
        return "\"" + texto.Replace("\"", "\"\"") + "\"";
    }

    private void Interromper(string mensagem)
    {
        var confrontoCancelado = confrontoAtual;
        versaoRotina++;
        rotina = null;
        ocupado = false;
        aguardandoPartida = false;
        confrontoAtual = null;
        partidaAtualId = null;
        if (apresentacao != null) apresentacao.Ocultar();
        if (gerenciador != null) gerenciador.CancelarConfrontoMataMata(confrontoCancelado);
        if (gerenciador != null && gerenciador.canoAutomatizado != null) gerenciador.canoAutomatizado.PararChuva();
        if (UniversalUI.Instance != null) UniversalUI.Instance.CancelarPartida();
        Time.timeScale = 1f;
        ultimoErro = mensagem;
        Debug.LogWarning(mensagem, this);
    }

    public void CancelarApresentacao()
    {
        if (rotina != null) StopCoroutine(rotina);
        rotina = null;
        if (ocupado || aguardandoPartida || (apresentacao != null && apresentacao.EstaExibindo))
            Interromper("Mata-mata interrompido. Jogos já salvos serão retomados no próximo Play.");
    }

    private void OnDisable() { CancelarApresentacao(); }

    private bool Tentar(Action acao)
    {
        ultimoErro = "";
        try { acao(); return true; }
        catch (Exception e) { ultimoErro = e.Message; Debug.LogError("Mata-mata: " + e.Message, this); return false; }
    }

    private static string Id(TeamData t)
    {
        Exigir(t != null && !string.IsNullOrWhiteSpace(t.name), "Cada clube precisa de um nome de asset válido.");
        return t.name.Trim();
    }

    private static bool IdValido(string id)
    {
        return !string.IsNullOrEmpty(id) && Regex.IsMatch(id, @"\A[A-Za-z0-9_-]{1,80}\z");
    }

    private static void Exigir(bool teste, string mensagem)
    {
        if (!teste) throw new InvalidOperationException(mensagem);
    }
}
