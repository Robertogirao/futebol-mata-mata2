using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Automatic, recording-safe presentation of a knockout bracket.</summary>
public sealed class ChaveamentoUI : MonoBehaviour
{
    [Header("Destaque do próximo confronto")]
    public bool destacarProximoConfronto = true;

    [Range(0.35f, 1f)]
    public float opacidadeOutrosConfrontos = 0.65f;

    public bool EstaExibindo { get; private set; }
    public bool UltimaExibicaoConcluida { get; private set; }

    private const float Largura = 1920f;
    private const float Altura = 1080f;
    private const float CentroQuadro = -35f;
    private const float AlturaQuadro = 720f;
    private const float TempoRevelacao = 0.65f;

    private sealed class EquipeVista
    {
        public string nome;
        public string sigla;
        public Sprite escudo;
    }

    private sealed class JogoVista
    {
        public string id;
        public string a;
        public string b;
        public string vencedor;
        public int indice;
        public int golsA;
        public int golsB;
        public bool resolvidaA;
        public bool resolvidaB;
        public bool concluida;
        public bool bye;
    }

    private sealed class FaseVista
    {
        public int vagas;
        public readonly List<JogoVista> jogos = new List<JogoVista>();
    }

    private sealed class NoVisual
    {
        public JogoVista jogo;
        public Vector2 posicao;
        public float largura;
        public int lado;
    }

    private sealed class Fragmento
    {
        public RectTransform rect;
        public Vector2 origem;
        public float fase;
    }

    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform desenho;
    private RectTransform cortina;
    private Image fundoOpaco;
    private Image imagemCortina;
    private Image bordaCortina;
    private RectTransform conteudo;
    private RectTransform quadro;
    private RectTransform destaque;
    private CanvasGroup grupoConteudo;
    private CanvasGroup grupoQuadro;
    private CanvasGroup grupoDestaque;
    private CanvasGroup grupoFundoDestaque;
    private CanvasGroup grupoHaloFoco;
    private Image bordaFoco;
    private Image luzFoco;
    private RectTransform progresso;
    private AudioSource audioAbertura;
    private Coroutine revelacao;
    private int versao;

    private TMP_FontAsset fonteTitulo;
    private TMP_FontAsset fonteTexto;
    private float multiplicadorFonteTitulo = 1f;
    private float multiplicadorFonteTexto = 1f;
    private Color primaria;
    private Color secundaria;
    private Color acento;
    private Color texto;
    private Color painel;
    private Color discreto;
    private readonly List<Fragmento> fragmentos = new List<Fragmento>();
    private readonly Dictionary<string, EquipeVista> equipes =
        new Dictionary<string, EquipeVista>(StringComparer.Ordinal);
    private readonly Dictionary<string, EquipeVista> aliases =
        new Dictionary<string, EquipeVista>(StringComparer.OrdinalIgnoreCase);

    public void CobrirTela()
    {
        Cancelar();
        if (!isActiveAndEnabled) return;
        CriarCanvas();
        MostrarCobertura();
    }

    public IEnumerator Exibir(
        MataMataModelo.Estado estado,
        List<TeamData> equipes,
        TemaCompeticao tema,
        float segundos,
        string partidaFocoId = null,
        bool encerramento = false)
    {
        Cancelar();
        if (!isActiveAndEnabled) yield break;

        CriarCanvas();
        ConfigurarTema(tema);
        MostrarCobertura();

        List<FaseVista> fases = CopiarFases(estado);
        if (fases.Count == 0 || fonteTitulo == null || fonteTexto == null)
        {
            Debug.LogError("ChaveamentoUI: confira o chaveamento e a fonte TMP padrão.", this);
            yield break;
        }

        CopiarEquipes(equipes);
        JogoVista foco = null;
        FaseVista faseFoco = null;
        if (!encerramento && !string.IsNullOrEmpty(partidaFocoId))
        {
            foreach (FaseVista fase in fases)
                foreach (JogoVista jogo in fase.jogos)
                    if (string.Equals(jogo.id, partidaFocoId, StringComparison.Ordinal))
                    {
                        foco = jogo;
                        faseFoco = fase;
                        break;
                    }
        }

        // Automatic overview focus does not enable the pregame matchup hero.
        JogoVista focoQuadro = null;
        if (!encerramento && destacarProximoConfronto)
        {
            focoQuadro = foco;
            if (focoQuadro == null)
            {
                MataMataModelo.Partida proxima = MataMataModelo.Proxima(estado);
                if (proxima != null)
                    foreach (FaseVista fase in fases)
                        foreach (JogoVista jogo in fase.jogos)
                            if (string.Equals(jogo.id, proxima.id, StringComparison.Ordinal))
                                focoQuadro = jogo;
            }
        }

        string campeao = estado != null ? estado.campeao : null;
        ConstruirApresentacao(fases, foco, focoQuadro, faseFoco, campeao, encerramento, tema);
        int minhaVersao = versao;
        float duracao = float.IsNaN(segundos) || float.IsInfinity(segundos)
            ? (encerramento ? 20f : foco != null ? 10f : 15f)
            : Mathf.Max(3f, segundos);

        float fimLeitura = Mathf.Max(1f, duracao - 1f);
        bool temDestaque = foco != null || (encerramento && !string.IsNullOrEmpty(campeao));
        float inicioDestaque = Mathf.Min(4f, fimLeitura * 0.48f);
        canvas.gameObject.SetActive(true);
        conteudo.gameObject.SetActive(true);
        cortina.gameObject.SetActive(false);
        EstaExibindo = true;
        Canvas.ForceUpdateCanvases();
        AjustarDesenho();
        TocarAudio(!encerramento && tema != null ? tema.somAbertura : null);

        double tempo = 0d;
        while (SequenciaAtual(minhaVersao))
        {
            float atual = (float)tempo;
            Animar(atual, duracao, inicioDestaque, temDestaque);
            if (!encerramento && atual >= fimLeitura)
            {
                cortina.gameObject.SetActive(true);
                cortina.anchoredPosition = new Vector2(
                    Mathf.Lerp(-LarguraCanvas(), 0f, Suavizar((atual - fimLeitura) / 0.72f)), 0f);
            }

            if (tempo + 0.000001d >= duracao) break;
            yield return null;
            tempo += PassoTempo();
        }

        if (!SequenciaAtual(minhaVersao)) yield break;
        // Final/phase presentations stay visible until the controller explicitly
        // requests another presentation or covers the next arena setup.
        if (!encerramento) MostrarCobertura();
        PararAudio();
        UltimaExibicaoConcluida = true;
    }

    public void RevelarArena()
    {
        if (!isActiveAndEnabled)
        {
            Ocultar();
            return;
        }
        if (revelacao != null) return;
        if (canvas == null || !canvas.gameObject.activeSelf)
        {
            EstaExibindo = false;
            return;
        }

        Cancelar(false);
        MostrarCobertura();
        // The moving curtain must uncover gameplay, including the letterbox
        // area that was kept opaque throughout the bracket presentation.
        fundoOpaco.enabled = false;
        revelacao = StartCoroutine(Revelar(versao));
    }

    public void Ocultar()
    {
        Cancelar();
        if (canvas != null) canvas.gameObject.SetActive(false);
        EstaExibindo = false;
    }

    private IEnumerator Revelar(int minhaVersao)
    {
        double tempo = 0d;
        while (SequenciaAtual(minhaVersao))
        {
            cortina.anchoredPosition = new Vector2(
                LarguraCanvas() * Suavizar((float)tempo / TempoRevelacao), 0f);
            if (tempo + 0.000001d >= TempoRevelacao) break;
            yield return null;
            tempo += PassoTempo();
        }
        if (!SequenciaAtual(minhaVersao)) yield break;
        canvas.gameObject.SetActive(false);
        EstaExibindo = false;
        revelacao = null;
    }

    private void Cancelar(bool limparResultado = true)
    {
        unchecked { versao++; }
        if (revelacao != null)
        {
            StopCoroutine(revelacao);
            revelacao = null;
        }
        PararAudio();
        EstaExibindo = false;
        if (limparResultado) UltimaExibicaoConcluida = false;
    }

    private bool SequenciaAtual(int minhaVersao)
    {
        return this != null && isActiveAndEnabled &&
            minhaVersao == versao && canvas != null && canvas.gameObject.activeSelf;
    }

    private static float PassoTempo()
    {
        float passo = Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;
        return float.IsNaN(passo) || float.IsInfinity(passo) ? 0f : Mathf.Max(0f, passo);
    }

    private void TocarAudio(AudioClip clip)
    {
        if (clip == null) return;
        if (audioAbertura == null)
        {
            // A dedicated source prevents cancellation from stopping gameplay FX.
            audioAbertura = gameObject.AddComponent<AudioSource>();
            audioAbertura.playOnAwake = false;
            audioAbertura.loop = false;
            audioAbertura.spatialBlend = 0f;
        }
        audioAbertura.PlayOneShot(clip, 0.7f);
    }

    private void PararAudio()
    {
        if (audioAbertura != null) audioAbertura.Stop();
    }

    private static List<FaseVista> CopiarFases(MataMataModelo.Estado estado)
    {
        var copia = new List<FaseVista>();
        if (estado == null || estado.fases == null) return copia;
        foreach (MataMataModelo.Fase fase in estado.fases)
        {
            if (fase == null || fase.partidas == null || fase.partidas.Count == 0) continue;
            var vista = new FaseVista { vagas = fase.vagas };
            foreach (MataMataModelo.Partida jogo in fase.partidas)
            {
                if (jogo == null) continue;
                vista.jogos.Add(new JogoVista
                {
                    id = jogo.id, a = jogo.a, b = jogo.b, vencedor = jogo.vencedor,
                    indice = jogo.indice, golsA = jogo.golsA, golsB = jogo.golsB,
                    resolvidaA = jogo.resolvidaA, resolvidaB = jogo.resolvidaB,
                    concluida = jogo.concluida, bye = jogo.bye
                });
            }
            vista.jogos.Sort((a, b) => a.indice.CompareTo(b.indice));
            if (vista.jogos.Count > 0) copia.Add(vista);
        }
        copia.Sort((a, b) => b.vagas.CompareTo(a.vagas));
        return copia;
    }

    private void CopiarEquipes(List<TeamData> times)
    {
        equipes.Clear();
        aliases.Clear();
        if (times == null) return;
        foreach (TeamData time in times)
        {
            if (time == null) continue;
            var vista = new EquipeVista
            {
                nome = Limpar(time.nomeEquipe, Limpar(time.name, "TEAM")),
                sigla = Limpar(time.sigla, Abreviar(time.nomeEquipe)),
                escudo = time.escudoOuBandeira
            };
            string id = (time.name ?? string.Empty).Trim();
            if (!equipes.ContainsKey(id)) equipes.Add(id, vista);
            AdicionarAlias(id, vista);
            AdicionarAlias(time.nomeEquipe, vista);
            AdicionarAlias(time.sigla, vista);
        }
    }

    private void AdicionarAlias(string id, EquipeVista equipe)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        id = id.Trim();
        EquipeVista existente;
        if (aliases.TryGetValue(id, out existente))
        {
            if (existente != equipe) aliases[id] = null;
        }
        else aliases.Add(id, equipe);
    }

    private EquipeVista ObterEquipe(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        EquipeVista equipe;
        if (equipes.TryGetValue(id, out equipe)) return equipe;
        if (aliases.TryGetValue(id, out equipe) && equipe != null) return equipe;
        return new EquipeVista { nome = Limpar(id, "TEAM"), sigla = Abreviar(id) };
    }

    private void ConfigurarTema(TemaCompeticao tema)
    {
        primaria = Cor(tema != null ? tema.corPrimaria : Color.clear,
            new Color(0.025f, 0.055f, 0.13f));
        secundaria = Cor(tema != null ? tema.corSecundaria : Color.clear,
            new Color(0.08f, 0.17f, 0.30f));
        acento = Cor(tema != null ? tema.corDestaque : Color.clear,
            new Color(0.40f, 0.91f, 0.96f));
        texto = Cor(tema != null ? tema.corTexto : Color.clear,
            new Color(0.96f, 0.98f, 1f));
        painel = Cor(tema != null ? tema.corPainel : Color.clear,
            new Color(0.055f, 0.095f, 0.18f));
        discreto = Color.Lerp(texto, painel, 0.43f);
        fonteTitulo = tema != null && tema.fonteTitulo != null
            ? tema.fonteTitulo : TMP_Settings.defaultFontAsset;
        fonteTexto = tema != null && tema.fonteTexto != null
            ? tema.fonteTexto : TMP_Settings.defaultFontAsset;
        multiplicadorFonteTitulo = EscalaFonteValida(tema != null ? tema.escalaFonteTitulo : 1f);
        multiplicadorFonteTexto = EscalaFonteValida(tema != null ? tema.escalaFonteTexto : 1f);
        imagemCortina.color = Color.Lerp(primaria, Color.black, 0.28f);
        bordaCortina.color = acento;
    }

    private void CriarCanvas()
    {
        if (canvas != null) return;
        var objeto = new GameObject("ChaveamentoUI.Canvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler));
        objeto.SetActive(false);
        objeto.transform.SetParent(transform, false);
        canvas = objeto.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2100;
        canvasRect = objeto.GetComponent<RectTransform>();
        CanvasScaler escala = objeto.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(Largura, Altura);
        escala.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escala.matchWidthOrHeight = 0.5f;

        // The canvas is visual only. This MonoBehaviour remains active while it is hidden.
        fundoOpaco = Imagem(TelaInteira("FundoOpaco", canvasRect), new Color(0.012f, 0.02f, 0.045f));
        desenho = Caixa("Enquadramento1920x1080", canvasRect, Vector2.zero,
            new Vector2(Largura, Altura));
        cortina = TelaInteira("Cortina", canvasRect);
        imagemCortina = Imagem(cortina, new Color(0.02f, 0.035f, 0.09f));
        RectTransform borda = TelaInteira("Borda", cortina);
        borda.anchorMin = new Vector2(1f, 0f);
        borda.anchorMax = Vector2.one;
        borda.sizeDelta = new Vector2(10f, 0f);
        bordaCortina = Imagem(borda, new Color(0.4f, 0.9f, 1f));
    }

    private void ConstruirApresentacao(List<FaseVista> fases, JogoVista foco, JogoVista focoQuadro,
        FaseVista faseFoco, string campeao, bool encerramento, TemaCompeticao tema)
    {
        if (conteudo != null)
        {
            conteudo.gameObject.SetActive(false);
            Destroy(conteudo.gameObject);
        }
        fragmentos.Clear();
        bordaFoco = null;
        grupoHaloFoco = null;
        luzFoco = null;
        conteudo = TelaInteira("Apresentacao", desenho);
        grupoConteudo = Grupo(conteudo);
        Image fundo = Imagem(TelaInteira("FundoTema", conteudo), primaria);
        if (tema != null && tema.fundoChaveamento != null)
        {
            fundo.sprite = tema.fundoChaveamento;
            fundo.color = Color.white;
        }
        Imagem(TelaInteira("Tonalidade", conteudo), ComAlpha(Color.Lerp(primaria, Color.black, 0.32f), 0.85f));
        grupoFundoDestaque = null;
        if (tema != null && tema.fundoAbertura != null)
        {
            RectTransform fundoDestaque = TelaInteira("FundoDestaque", conteudo);
            grupoFundoDestaque = Grupo(fundoDestaque);
            grupoFundoDestaque.alpha = 0f;
            Image arte = Imagem(TelaInteira("Arte", fundoDestaque), Color.white);
            arte.sprite = tema.fundoAbertura;
            Imagem(TelaInteira("Tonalidade", fundoDestaque),
                ComAlpha(Color.Lerp(primaria, Color.black, 0.32f), 0.85f));
        }
        Imagem(Caixa("FaixaSuperior", conteudo, new Vector2(0f, 505f), new Vector2(1920f, 70f)),
            ComAlpha(secundaria, 0.6f));
        Imagem(Caixa("AcentoSuperior", conteudo, new Vector2(0f, 538f), new Vector2(1920f, 4f)), acento);

        string nome = tema != null ? Limpar(tema.nomeCompeticao, "KNOCKOUT CUP") : "KNOCKOUT CUP";
        Texto("Competicao", conteudo, new Vector2(0f, 458f), new Vector2(1430f, 70f), nome, 46f,
            texto, true, TextAlignmentOptions.Center, 26f);
        TextMeshProUGUI subtitulo = Texto("Subtitulo", conteudo, new Vector2(0f, 401f),
            new Vector2(1400f, 36f), encerramento
                ? string.IsNullOrEmpty(campeao) ? "ROUND COMPLETE" : "THE ROAD TO THE TITLE"
                : "TOURNAMENT BRACKET",
            25f, acento, true);
        subtitulo.characterSpacing = 7f;
        SpriteNoQuadro("LogoCompeticao", conteudo, new Vector2(-829f, 450f), new Vector2(108f, 108f),
            tema != null ? tema.logoCompeticao : null);
        SpriteNoQuadro("LogoCanal", conteudo, new Vector2(829f, 450f), new Vector2(108f, 108f),
            tema != null ? tema.logoCanal : null);

        quadro = TelaInteira("Chaveamento", conteudo);
        grupoQuadro = Grupo(quadro);
        Sprite trofeuCompeticao = tema != null ? tema.logoCompeticao : null;
        ConstruirQuadro(fases, focoQuadro, campeao, trofeuCompeticao);
        destaque = TelaInteira("Destaque", conteudo);
        grupoDestaque = Grupo(destaque);
        grupoDestaque.alpha = 0f;
        if (encerramento && !string.IsNullOrEmpty(campeao)) ConstruirCampeao(campeao, trofeuCompeticao);
        else if (foco != null) ConstruirConfronto(foco, faseFoco);

        Imagem(Caixa("DivisorRodape", conteudo, new Vector2(0f, -433f), new Vector2(1760f, 1.5f)),
            ComAlpha(acento, 0.35f));
        string rodape = foco != null && faseFoco != null
            ? "UP NEXT  |  " + NomeFase(faseFoco.vagas) + "  |  MATCH " + (foco.indice + 1)
            : encerramento && !string.IsNullOrEmpty(campeao) ? "ONE CHAMPION. ONE UNFORGETTABLE JOURNEY."
            : "WINNERS ADVANCE  |  TBD: TO BE DECIDED  |  BYE: AUTOMATIC ADVANCE";
        Texto("Rodape", conteudo, new Vector2(0f, -474f), new Vector2(1720f, 45f), rodape,
            21f, discreto, false, TextAlignmentOptions.Center, 15f);
        RectTransform trilho = Caixa("TrilhoTempo", conteudo, new Vector2(0f, -522f), new Vector2(1760f, 3f));
        Imagem(trilho, ComAlpha(acento, 0.13f));
        progresso = Caixa("Tempo", trilho, Vector2.zero, new Vector2(1760f, 3f));
        progresso.pivot = new Vector2(0f, 0.5f);
        progresso.anchoredPosition = new Vector2(-880f, 0f);
        Imagem(progresso, acento);
    }

    private void ConstruirQuadro(List<FaseVista> fases, JogoVista foco, string campeao,
        Sprite trofeuCompeticao)
    {
        RectTransform linhas = TelaInteira("Conexoes", quadro);
        RectTransform jogos = TelaInteira("Partidas", quadro);
        var nos = new List<List<NoVisual>>();
        int colunas = Mathf.Max(0, fases.Count - 1);
        float largura = colunas >= 4 ? 166f : colunas == 3 ? 218f : colunas == 2 ? 292f
            : colunas == 1 ? 390f : 600f;
        float passo = colunas >= 4 ? 203f : colunas == 3 ? 260f : colunas == 2 ? 360f : 570f;
        float altura = colunas >= 4 ? 76f : colunas == 3 ? 100f : colunas == 2 ? 132f
            : colunas == 1 ? 164f : 220f;

        for (int f = 0; f < fases.Count; f++)
        {
            FaseVista fase = fases[f];
            bool final = f == fases.Count - 1;
            var lista = new List<NoVisual>();
            nos.Add(lista);
            int metade = Mathf.Max(1, fase.jogos.Count / 2);
            if (!final)
            {
                float x = -(colunas - f) * passo;
                Texto("NomeFaseEsquerda", quadro, new Vector2(x, 351f), new Vector2(largura + 20f, 42f),
                    NomeFase(fase.vagas), colunas >= 4 ? 19f : 23f, discreto, true,
                    TextAlignmentOptions.Center, 15f);
                Texto("NomeFaseDireita", quadro, new Vector2(-x, 351f), new Vector2(largura + 20f, 42f),
                    NomeFase(fase.vagas), colunas >= 4 ? 19f : 23f, discreto, true,
                    TextAlignmentOptions.Center, 15f);
            }

            for (int i = 0; i < fase.jogos.Count; i++)
            {
                JogoVista jogo = fase.jogos[i];
                int lado = final ? 0 : i < metade ? -1 : 1;
                int linha = lado == 1 ? i - metade : i;
                float x = final ? 0f : lado * (colunas - f) * passo;
                float y = final ? CentroQuadro : CentroQuadro + (0.5f - (linha + 0.5f) / metade) * AlturaQuadro;
                float larguraNo = final && colunas > 0 ? Mathf.Min(largura + 28f, passo - 12f) : largura;
                float alturaNo = final && colunas >= 3 ? altura + 16f : altura;
                var no = new NoVisual
                {
                    jogo = jogo, lado = lado, largura = larguraNo, posicao = new Vector2(x, y)
                };
                lista.Add(no);
                ConstruirJogo(jogos, no, alturaNo, colunas >= 3, foco == jogo,
                    foco != null && foco != jogo);
            }
        }

        for (int f = 0; f + 1 < nos.Count; f++)
        {
            for (int i = 0; i < nos[f].Count; i++)
            {
                if (nos[f + 1].Count == 0) continue;
                NoVisual origem = nos[f][i];
                NoVisual destino = nos[f + 1][Mathf.Min(i / 2, nos[f + 1].Count - 1)];
                int sentido = origem.lado < 0 ? 1 : -1;
                Vector2 a = origem.posicao + new Vector2(sentido * origem.largura * 0.5f, 0f);
                Vector2 b = destino.posicao - new Vector2(sentido * destino.largura * 0.5f, 0f);
                float meio = (a.x + b.x) * 0.5f;
                Color cor = origem.jogo == foco || destino.jogo == foco ? ComAlpha(acento, 0.90f)
                    : origem.jogo.concluida ? ComAlpha(acento, 0.44f) : ComAlpha(discreto, 0.25f);
                Linha(linhas, a, new Vector2(meio, a.y), cor);
                Linha(linhas, new Vector2(meio, a.y), new Vector2(meio, b.y), cor);
                Linha(linhas, new Vector2(meio, b.y), b, cor);
            }
        }

        if (colunas > 0)
        {
            float larguraCentro = colunas >= 4 ? 180f : colunas == 3 ? 230f : 330f;
            float tituloFinalY = Mathf.Max(64f, CentroQuadro + altura * 0.5f + 40f);
            Texto("Final", quadro, new Vector2(0f, tituloFinalY), new Vector2(larguraCentro, 52f), "FINAL",
                colunas >= 3 ? 30f : 38f, acento, true);
            Trofeu(quadro, new Vector2(0f, 202f), colunas >= 4 ? 0.62f : 0.85f, trofeuCompeticao);
            EquipeVista vencedor = ObterEquipe(campeao);
            if (vencedor != null)
            {
                SpriteNoQuadro("EscudoCampeaoResumo", quadro, new Vector2(0f, -215f),
                    new Vector2(colunas >= 4 ? 108f : 132f, colunas >= 4 ? 108f : 132f), vencedor.escudo);
                Texto("CampeaoResumo", quadro, new Vector2(0f, colunas >= 4 ? -310f : -326f),
                    new Vector2(larguraCentro, 68f), colunas >= 3 ? vencedor.sigla : vencedor.nome,
                    28f, acento, true, TextAlignmentOptions.Center, 18f, true);
                Texto("CampeaoLegenda", quadro, new Vector2(0f, colunas >= 4 ? -365f : -390f),
                    new Vector2(larguraCentro, 30f), "CHAMPIONS", 15f, discreto, true);
            }
            else
            {
                Texto("RumoAoTitulo", quadro, new Vector2(0f, -215f), new Vector2(larguraCentro, 100f),
                    "ROAD TO\nTHE TITLE", colunas >= 4 ? 25f : 33f, ComAlpha(discreto, 0.62f), true,
                    TextAlignmentOptions.Center, 20f, true);
            }
        }
        else
        {
            Texto("Final", quadro, new Vector2(0f, 170f), new Vector2(900f, 80f), "THE FINAL",
                50f, acento, true);
            Trofeu(quadro, new Vector2(0f, -267f), 0.9f, trofeuCompeticao);
        }
    }

    private void ConstruirJogo(RectTransform pai, NoVisual no, float altura, bool compacto, bool foco, bool atenuar)
    {
        RectTransform area = Caixa("Jogo_" + no.jogo.id, pai, no.posicao, new Vector2(no.largura, altura));
        if (atenuar)
            Grupo(area).alpha = Mathf.Clamp(opacidadeOutrosConfrontos, 0.35f, 1f);
        if (foco)
        {
            RectTransform halo = TelaInteira("HaloProximoConfronto", area);
            grupoHaloFoco = Grupo(halo);
            Imagem(Caixa("BrilhoExterno", halo, Vector2.zero,
                new Vector2(no.largura + 14f, altura + 12f)), ComAlpha(acento, 0.18f));
            Imagem(Caixa("BrilhoInterno", halo, Vector2.zero,
                new Vector2(no.largura + 8f, altura + 8f)), ComAlpha(acento, 0.48f));
        }
        Imagem(Caixa("Sombra", area, new Vector2(0f, -4f), new Vector2(no.largura + 6f, altura + 6f)),
            new Color(0f, 0f, 0f, 0.28f));
        Image borda = Imagem(TelaInteira("Borda", area), foco ? acento : ComAlpha(discreto, 0.22f));
        float recuo = foco ? 10f : 3f;
        RectTransform interior = Caixa("Interior", area, Vector2.zero,
            new Vector2(no.largura - recuo, altura - recuo));
        Imagem(interior, Color.Lerp(painel, primaria, 0.2f));
        if (foco)
        {
            bordaFoco = borda;
            luzFoco = Imagem(TelaInteira("LuzProximoConfronto", interior), ComAlpha(acento, 0.08f));
        }

        float linha = (altura - 4f) * 0.5f;
        ConstruirLinhaEquipe(area, no.jogo, true, no.largura - 4f, linha, compacto);
        ConstruirLinhaEquipe(area, no.jogo, false, no.largura - 4f, linha, compacto);
        Imagem(Caixa("Divisor", area, Vector2.zero, new Vector2(no.largura - 16f, 1f)),
            ComAlpha(discreto, 0.16f));
        if (foco)
        {
            // This badge straddles the top-right border. Its nine-pixel outside
            // extent fits the fourteen-pixel gap in the dense 32-slot layout.
            float alturaSelo = altura <= 80f ? 14f : altura <= 120f ? 18f : 20f;
            float larguraSelo = altura <= 80f ? 76f : altura <= 120f ? 92f : 110f;
            RectTransform selo = Caixa("SeloProximoConfronto", area,
                new Vector2((no.largura - larguraSelo) * 0.5f - 7f, altura * 0.5f + 2f),
                new Vector2(larguraSelo, alturaSelo));
            Imagem(selo, acento);
            Texto("UpNext", selo, Vector2.zero, new Vector2(larguraSelo - 6f, alturaSelo),
                "UP NEXT", alturaSelo - 3f, Color.Lerp(primaria, Color.black, 0.65f), true,
                TextAlignmentOptions.Center, alturaSelo - 3f);
        }
    }

    private void ConstruirLinhaEquipe(RectTransform pai, JogoVista jogo, bool ladoA,
        float largura, float altura, bool compacto)
    {
        string id = ladoA ? jogo.a : jogo.b;
        bool resolvida = ladoA ? jogo.resolvidaA : jogo.resolvidaB;
        bool vazia = string.IsNullOrEmpty(id);
        bool venceu = jogo.concluida && !vazia && string.Equals(id, jogo.vencedor, StringComparison.Ordinal);
        bool perdeu = jogo.concluida && !vazia && !venceu;
        EquipeVista equipe = ObterEquipe(id);
        RectTransform linha = Caixa(ladoA ? "EquipeA" : "EquipeB", pai,
            new Vector2(0f, ladoA ? altura * 0.5f : -altura * 0.5f), new Vector2(largura, altura));
        if (venceu)
        {
            Imagem(TelaInteira("Vencedor", linha), ComAlpha(acento, 0.15f));
            Imagem(Caixa("MarcaVencedor", linha, new Vector2(-largura * 0.5f + 2f, 0f),
                new Vector2(3f, altura - 4f)), acento);
        }

        float escudo = compacto ? Mathf.Min(29f, altura - 8f) : Mathf.Min(64f, altura - 12f);
        float margem = compacto ? 8f : 15f;
        float xEscudo = -largura * 0.5f + margem + escudo * 0.5f;
        Image imagem = SpriteNoQuadro("Escudo", linha, new Vector2(xEscudo, 0f),
            new Vector2(escudo, escudo), equipe != null ? equipe.escudo : null);
        imagem.color = perdeu ? ComAlpha(Color.white, 0.42f) : Color.white;
        bool possuiEscudo = equipe != null && equipe.escudo != null;
        float inicioNome = -largura * 0.5f + margem + (possuiEscudo ? escudo + 8f : 2f);
        float larguraPlacar = compacto ? 27f : 48f;
        float finalNome = largura * 0.5f - larguraPlacar - 7f;
        string nome = vazia ? (resolvida ? "BYE" : "TBD") : compacto ? equipe.sigla : equipe.nome;
        Color corNome = perdeu || vazia ? discreto : venceu ? acento : texto;
        Texto("Nome", linha, new Vector2((inicioNome + finalNome) * 0.5f, 0f),
            new Vector2(Mathf.Max(20f, finalNome - inicioNome), altura - 4f), nome,
            compacto ? 23f : Mathf.Min(42f, altura * 0.44f), corNome, venceu,
            TextAlignmentOptions.MidlineLeft, compacto ? 17f : 18f);
        string placar = jogo.concluida && !jogo.bye && !vazia
            ? (ladoA ? jogo.golsA : jogo.golsB).ToString() : "-";
        Texto("Placar", linha, new Vector2(largura * 0.5f - larguraPlacar * 0.5f - 5f, 0f),
            new Vector2(larguraPlacar, altura - 4f), placar,
            compacto ? 24f : Mathf.Min(42f, altura * 0.5f), corNome, true,
            TextAlignmentOptions.Center, compacto ? 18f : 24f);
    }

    private void ConstruirConfronto(JogoVista jogo, FaseVista fase)
    {
        Texto("UpNext", destaque, new Vector2(0f, 310f), new Vector2(1300f, 60f), "UP NEXT", 48f, acento, true);
        Texto("Etapa", destaque, new Vector2(0f, 246f), new Vector2(1400f, 50f),
            (fase != null ? NomeFase(fase.vagas) : "KNOCKOUT STAGE") + "  |  MATCH " + (jogo.indice + 1),
            29f, discreto, true);
        Texto("Versus", destaque, new Vector2(0f, 5f), new Vector2(160f, 100f), "VS", 67f, acento, true);
        Linha(destaque, new Vector2(-110f, -116f), new Vector2(110f, -116f), ComAlpha(acento, 0.5f));
        ConstruirEquipeDestaque(ObterEquipe(jogo.a), jogo.resolvidaA, -445f);
        ConstruirEquipeDestaque(ObterEquipe(jogo.b), jogo.resolvidaB, 445f);
        Texto("Avanco", destaque, new Vector2(0f, -343f), new Vector2(1500f, 55f),
            fase != null && fase.vagas <= 2 ? "PLAYING FOR THE TITLE" : "WIN AND KEEP THE DREAM ALIVE",
            25f, acento, true);
    }

    private void ConstruirEquipeDestaque(EquipeVista equipe, bool resolvida, float x)
    {
        Imagem(Caixa("BaseEscudo", destaque, new Vector2(x, -6f), new Vector2(390f, 3f)),
            ComAlpha(acento, 0.17f));
        SpriteNoQuadro("EscudoDestaque", destaque, new Vector2(x, 58f), new Vector2(270f, 270f),
            equipe != null ? equipe.escudo : null);
        Texto("NomeDestaque", destaque, new Vector2(x, -179f), new Vector2(670f, 144f),
            equipe != null ? equipe.nome : resolvida ? "BYE" : "TBD", 62f, texto, true,
            TextAlignmentOptions.Center, 28f, true);
        if (equipe != null && equipe.escudo == null)
            Texto("SiglaDestaque", destaque, new Vector2(x, 58f), new Vector2(350f, 150f),
                equipe.sigla, 88f, acento, true, TextAlignmentOptions.Center, 42f);
    }

    private void ConstruirCampeao(string id, Sprite trofeuCompeticao)
    {
        EquipeVista equipe = ObterEquipe(id);
        if (equipe == null) return;
        for (int i = 0; i < 34; i++)
        {
            float x = -820f + (i * 157 % 1640);
            float y = -330f + (i * 83 % 650);
            if (Mathf.Abs(x) < 240f) x += x < 0f ? -270f : 270f;
            RectTransform parte = Caixa("Celebracao", destaque, new Vector2(x, y),
                new Vector2(i % 3 == 0 ? 7f : 4f, 16f + i % 4 * 4f));
            parte.localRotation = Quaternion.Euler(0f, 0f, (i * 37) % 160 - 80f);
            Imagem(parte, ComAlpha(i % 3 == 0 ? texto : acento, 0.35f + i % 3 * 0.12f));
            fragmentos.Add(new Fragmento { rect = parte, origem = parte.anchoredPosition, fase = i * 0.7f });
        }
        Trofeu(destaque, new Vector2(0f, 299f), 0.68f, trofeuCompeticao);
        Texto("TituloCampeao", destaque, new Vector2(0f, 208f), new Vector2(1400f, 80f),
            "CHAMPIONS", 66f, acento, true);
        SpriteNoQuadro("EscudoCampeao", destaque, new Vector2(0f, 15f), new Vector2(260f, 260f), equipe.escudo);
        if (equipe.escudo == null)
            Texto("SiglaCampeao", destaque, new Vector2(0f, 15f), new Vector2(1000f, 180f),
                equipe.sigla, 100f, acento, true, TextAlignmentOptions.Center, 48f);
        Texto("NomeCampeao", destaque, new Vector2(0f, -194f), new Vector2(1580f, 140f),
            equipe.nome, 78f, texto, true, TextAlignmentOptions.Center, 32f, true);
        Texto("LegendaCampeao", destaque, new Vector2(0f, -321f), new Vector2(1400f, 55f),
            "TOURNAMENT WINNER", 30f, acento, true);
    }

    private void Animar(float tempo, float duracao, float inicioDestaque, bool temDestaque)
    {
        grupoConteudo.alpha = Suavizar(tempo / 0.6f);
        float troca = temDestaque ? Suavizar((tempo - inicioDestaque) / 0.65f) : 0f;
        if (grupoFundoDestaque != null) grupoFundoDestaque.alpha = troca;
        grupoQuadro.alpha = 1f - troca;
        quadro.anchoredPosition = new Vector2(0f, -20f * (1f - Suavizar(tempo / 0.8f)) - troca * 20f);
        grupoDestaque.alpha = troca;
        destaque.anchoredPosition = new Vector2(0f, 20f * (1f - troca));
        float pulso = 0.5f + 0.5f * Mathf.Sin(tempo * 3f);
        if (bordaFoco != null)
            bordaFoco.color = Color.Lerp(acento, texto, 0.10f + 0.30f * pulso);
        if (grupoHaloFoco != null) grupoHaloFoco.alpha = 0.5f + 0.5f * pulso;
        if (luzFoco != null) luzFoco.color = ComAlpha(acento, 0.055f + 0.075f * pulso);
        progresso.sizeDelta = new Vector2(1760f * Mathf.Clamp01(tempo / duracao), 3f);
        foreach (Fragmento fragmento in fragmentos)
            fragmento.rect.anchoredPosition = fragmento.origem +
                new Vector2(Mathf.Sin(tempo * 0.45f + fragmento.fase) * 12f,
                    Mathf.Sin(tempo * 0.32f + fragmento.fase) * 21f);
    }

    private void MostrarCobertura()
    {
        fundoOpaco.enabled = true;
        cortina.anchoredPosition = Vector2.zero;
        cortina.gameObject.SetActive(true);
        if (conteudo != null) conteudo.gameObject.SetActive(false);
        canvas.gameObject.SetActive(true);
        EstaExibindo = true;
    }

    private void LateUpdate()
    {
        if (canvas != null && canvas.gameObject.activeSelf) AjustarDesenho();
    }

    private void AjustarDesenho()
    {
        if (desenho == null || canvasRect == null) return;
        float x = canvasRect.rect.width;
        float y = canvasRect.rect.height;
        float escala = x > 0f && y > 0f ? Mathf.Min(x / Largura, y / Altura) : 1f;
        desenho.localScale = Vector3.one * escala;
    }

    private float LarguraCanvas()
    {
        return canvasRect != null && canvasRect.rect.width > 0f ? canvasRect.rect.width : Largura;
    }

    private void OnDisable()
    {
        Ocultar();
    }

    private void OnDestroy()
    {
        Cancelar();
        if (canvas != null) Destroy(canvas.gameObject);
    }

    private static string NomeFase(int vagas)
    {
        return Limpar(MataMataModelo.NomeFase(vagas), "KNOCKOUT STAGE");
    }

    private static string Limpar(string valor, string alternativa)
    {
        return string.IsNullOrWhiteSpace(valor) ? alternativa
            : valor.Replace('\r', ' ').Replace('\n', ' ').Trim().ToUpperInvariant();
    }

    private static string Abreviar(string valor)
    {
        string nome = Limpar(valor, "TEAM");
        string[] palavras = nome.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        string sigla = string.Empty;
        if (palavras.Length > 1)
        {
            foreach (string palavra in palavras)
            {
                sigla += palavra[0];
                if (sigla.Length == 4) break;
            }
        }
        else sigla = nome.Length <= 4 ? nome : nome.Substring(0, 3);
        return sigla;
    }

    private static Color Cor(Color valor, Color alternativa)
    {
        return valor.a <= 0f ? alternativa : ComAlpha(valor, 1f);
    }

    private static Color ComAlpha(Color valor, float alpha)
    {
        valor.a = alpha;
        return valor;
    }

    private static float Suavizar(float valor)
    {
        valor = Mathf.Clamp01(valor);
        return valor * valor * (3f - 2f * valor);
    }

    private static RectTransform Caixa(string nome, Transform pai, Vector2 posicao, Vector2 tamanho)
    {
        var objeto = new GameObject(nome, typeof(RectTransform));
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.SetParent(pai, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = tamanho;
        rect.anchoredPosition = posicao;
        return rect;
    }

    private static RectTransform TelaInteira(string nome, Transform pai)
    {
        RectTransform rect = Caixa(nome, pai, Vector2.zero, Vector2.zero);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        return rect;
    }

    private static CanvasGroup Grupo(RectTransform rect)
    {
        CanvasGroup grupo = rect.gameObject.AddComponent<CanvasGroup>();
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
        return grupo;
    }

    private static Image Imagem(RectTransform rect, Color cor)
    {
        Image imagem = rect.gameObject.AddComponent<Image>();
        imagem.color = cor;
        imagem.raycastTarget = false;
        return imagem;
    }

    private static Image SpriteNoQuadro(string nome, Transform pai, Vector2 posicao, Vector2 tamanho, Sprite sprite)
    {
        Image imagem = Imagem(Caixa(nome, pai, posicao, tamanho), Color.white);
        imagem.sprite = sprite;
        imagem.preserveAspect = true;
        imagem.enabled = sprite != null;
        return imagem;
    }

    private TextMeshProUGUI Texto(string nome, Transform pai, Vector2 posicao, Vector2 tamanho,
        string valor, float fonte, Color cor, bool titulo, TextAlignmentOptions alinhamento = TextAlignmentOptions.Center,
        float minimo = 16f, bool duasLinhas = false)
    {
        float escala = titulo ? multiplicadorFonteTitulo : multiplicadorFonteTexto;
        float tamanhoPedido = fonte * escala;
        // Increasing the requested size keeps the original fitting minimum.
        // Reducing the size also reduces that minimum proportionally.
        float tamanhoMinimo = Mathf.Min(minimo * Mathf.Min(escala, 1f), tamanhoPedido);
        TextMeshProUGUI label = Caixa(nome, pai, posicao, tamanho).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = titulo ? fonteTitulo : fonteTexto;
        label.text = valor;
        label.fontSize = tamanhoPedido;
        label.enableAutoSizing = true;
        label.fontSizeMax = tamanhoPedido;
        label.fontSizeMin = tamanhoMinimo;
        label.fontStyle = titulo ? FontStyles.Bold : FontStyles.Normal;
        label.alignment = alinhamento;
        label.color = cor;
        label.richText = false;
        label.raycastTarget = false;
        label.textWrappingMode = duasLinhas ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        label.maxVisibleLines = duasLinhas ? 2 : 1;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    private static float EscalaFonteValida(float escala)
    {
        return float.IsNaN(escala) || float.IsInfinity(escala)
            ? 1f : Mathf.Clamp(escala, 0.5f, 1.75f);
    }

    private static void Linha(Transform pai, Vector2 a, Vector2 b, Color cor)
    {
        Vector2 diferenca = b - a;
        if (diferenca.sqrMagnitude < 0.01f) return;
        RectTransform rect = Caixa("Conexao", pai, (a + b) * 0.5f,
            new Vector2(diferenca.magnitude, 2f));
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(diferenca.y, diferenca.x) * Mathf.Rad2Deg);
        Imagem(rect, cor);
    }

    private void Trofeu(Transform pai, Vector2 posicao, float escala, Sprite sprite)
    {
        RectTransform trofeu = Caixa("Trofeu", pai, posicao, new Vector2(160f, 170f));
        trofeu.localScale = Vector3.one * escala;
        if (sprite != null)
        {
            SpriteNoQuadro("ImagemTrofeu", trofeu, Vector2.zero, new Vector2(160f, 170f), sprite);
            return;
        }

        Color ouro = Color.Lerp(acento, new Color(1f, 0.78f, 0.30f), 0.68f);
        Imagem(Caixa("Taca", trofeu, new Vector2(0f, 32f), new Vector2(80f, 65f)), ouro);
        Imagem(Caixa("Boca", trofeu, new Vector2(0f, 67f), new Vector2(96f, 10f)), texto);
        Imagem(Caixa("Brilho", trofeu, new Vector2(-23f, 32f), new Vector2(8f, 52f)), ComAlpha(texto, 0.33f));
        Linha(trofeu, new Vector2(-43f, 56f), new Vector2(-67f, 56f), ouro);
        Linha(trofeu, new Vector2(-67f, 56f), new Vector2(-67f, 13f), ouro);
        Linha(trofeu, new Vector2(-67f, 13f), new Vector2(-38f, 0f), ouro);
        Linha(trofeu, new Vector2(43f, 56f), new Vector2(67f, 56f), ouro);
        Linha(trofeu, new Vector2(67f, 56f), new Vector2(67f, 13f), ouro);
        Linha(trofeu, new Vector2(67f, 13f), new Vector2(38f, 0f), ouro);
        Imagem(Caixa("Haste", trofeu, new Vector2(0f, -20f), new Vector2(16f, 42f)), ouro);
        Imagem(Caixa("Base", trofeu, new Vector2(0f, -47f), new Vector2(76f, 13f)), ouro);
        Imagem(Caixa("Pedestal", trofeu, new Vector2(0f, -61f), new Vector2(104f, 14f)), ouro);
    }
}
