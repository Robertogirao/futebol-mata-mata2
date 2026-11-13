using System.Collections;
using FutebolDeDiscos;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Holographic league matchup with a vertically split, fully opaque handoff.</summary>
public sealed class AberturaJogoUI : MonoBehaviour
{
    [Header("Imagens")] public Sprite fundo;
    public Sprite logoCanal;
    [Header("Fontes opcionais")] public TMP_FontAsset fonteTitulo;
    public TMP_FontAsset fonteNomes;
    [Header("Cores da transmissão")]
    public Color corFundo = new Color(.025f, .12f, .13f);
    public Color corPainel = new Color(.012f, .06f, .075f);
    public Color corAcento = new Color(.25f, .93f, .92f);
    [Header("Duração da abertura"), Min(3f)] public float tempoTotal = 10f;
    [Header("Som opcional")] public AudioClip somAbertura;
    [Range(0f, 1f)] public float volume = .7f;

    public bool EstaExibindo { get; private set; }
    public bool UltimaExibicaoConcluida { get; private set; }

    private const float DuracaoRevelacao = .6f;
    private Canvas canvas;
    private RectTransform canvasRect, principal, cortina, painelSuperior, painelInferior;
    private RectTransform blocoA, blocoB, scannerA, scannerB, eixoEnergia;
    private CanvasGroup conteudo, grupoA, grupoB, grupoChamada;
    private Image imagemFundo, escudoA, escudoB, imagemLiga, imagemCanal;
    private Image linhaScannerA, linhaScannerB, luzEixo;
    private TextMeshProUGUI textoRodada, textoJogo, nomeA, nomeB, tituloArena, tituloDuelo, versus, chamada;
    private AudioSource audioAbertura;
    private Texture2D texturaGerada;
    private Sprite spriteGerado;
    private Coroutine rotinaRevelacao;
    private int versao;
    private RoteiroTransmissaoDiscos roteiro = new RoteiroTransmissaoDiscos();

    public void ConfigurarRoteiro(RoteiroTransmissaoDiscos novo)
    {
        roteiro = novo ?? new RoteiroTransmissaoDiscos();
        if (tituloArena != null) AtualizarPalavras();
    }

    public void CobrirTela()
    {
        if (!isActiveAndEnabled) return;
        CancelarSequencia();
        CriarInterface();
        MostrarCobertura();
    }

    public IEnumerator Exibir(TeamData timeA, TeamData timeB, Sprite logoLiga,
        int rodada, int numeroJogo, int totalJogos)
    {
        CancelarSequencia();
        if (this == null || !isActiveAndEnabled || timeA == null || timeB == null) yield break;
        CriarInterface();
        if (textoRodada.font == null || nomeA.font == null)
        {
            Debug.LogError("AberturaJogoUI: atribua fontes TMP aos campos Fonte Titulo e Fonte Nomes.", this);
            MostrarCobertura();
            yield break;
        }
        int minhaVersao = versao;
        float duracao = float.IsNaN(tempoTotal) || float.IsInfinity(tempoTotal) ? 10f : Mathf.Max(3f, tempoTotal);
        imagemFundo.sprite = fundo != null ? fundo : ObterFundoGerado();
        DefinirSprite(escudoA, timeA.escudoOuBandeira);
        DefinirSprite(escudoB, timeB.escudoOuBandeira);
        DefinirSprite(imagemLiga, logoLiga);
        DefinirSprite(imagemCanal, logoCanal);
        nomeA.text = NomeTime(timeA.nomeEquipe, "HOME TEAM");
        nomeB.text = NomeTime(timeB.nomeEquipe, "AWAY TEAM");
        AtualizarPalavras();
        textoRodada.text = RoteiroTransmissaoDiscos.Numero(roteiro.rodada, rodada);
        textoJogo.text = RoteiroTransmissaoDiscos.Numero(roteiro.partida, numeroJogo);
        principal.gameObject.SetActive(true);
        cortina.gameObject.SetActive(false);
        canvas.gameObject.SetActive(true);
        EstaExibindo = true;
        AnimarEntrada(0f);
        Canvas.ForceUpdateCanvases();
        TocarAudio();
        double tempo = 0d;
        while (SequenciaAtual(minhaVersao))
        {
            float atual = (float)tempo;
            AnimarEntrada(atual);
            // Both solid panels meet before the game can be prepared behind them.
            if (atual >= duracao - 1f)
            {
                cortina.gameObject.SetActive(true);
                float fechado = Suavizar((atual - (duracao - 1f)) / .65f);
                AplicarAbertura(1f - fechado);
                if (fechado >= 1f) principal.gameObject.SetActive(false);
            }
            if (tempo + .000001d >= duracao) break;
            yield return null;
            tempo += PassoTempo();
        }
        if (!SequenciaAtual(minhaVersao)) yield break;
        MostrarCobertura();
        PararAudio();
        UltimaExibicaoConcluida = true;
    }

    public void RevelarArena()
    {
        if (!isActiveAndEnabled) { Ocultar(); return; }
        if (rotinaRevelacao != null) return;
        if (canvas == null || !canvas.gameObject.activeSelf) { EstaExibindo = false; return; }
        CancelarSequencia(false);
        MostrarCobertura();
        rotinaRevelacao = StartCoroutine(Revelar(versao));
    }

    public void Ocultar()
    {
        CancelarSequencia();
        if (canvas != null) canvas.gameObject.SetActive(false);
        EstaExibindo = false;
    }

    private IEnumerator Revelar(int minhaVersao)
    {
        Canvas.ForceUpdateCanvases();
        double tempo = 0d;
        while (SequenciaAtual(minhaVersao))
        {
            AplicarAbertura(Suavizar((float)(tempo / DuracaoRevelacao)));
            if (tempo + .000001d >= DuracaoRevelacao) break;
            yield return null;
            tempo += PassoTempo();
        }
        if (!SequenciaAtual(minhaVersao)) yield break;
        canvas.gameObject.SetActive(false);
        EstaExibindo = false;
        rotinaRevelacao = null;
    }

    private void AplicarAbertura(float aberta)
    {
        float altura = canvasRect.rect.height > 0f ? canvasRect.rect.height : 1080f;
        float deslocamento = (altura * .55f + 4f) * Mathf.Clamp01(aberta);
        painelSuperior.anchoredPosition = new Vector2(0f, deslocamento);
        painelInferior.anchoredPosition = new Vector2(0f, -deslocamento);
    }

    private void AnimarEntrada(float tempo)
    {
        conteudo.alpha = Suavizar(tempo / .35f);
        float entradaA = Suavizar((tempo - .12f) / .72f);
        float entradaB = Suavizar((tempo - .3f) / .72f);
        grupoA.alpha = entradaA;
        grupoB.alpha = entradaB;
        // Vertical projection assembly replaces lateral sliding and uniform zoom.
        blocoA.anchoredPosition = new Vector2(0f, -110f * (1f - entradaA));
        blocoB.anchoredPosition = new Vector2(0f, 110f * (1f - entradaB));
        blocoA.localScale = new Vector3(1f, Mathf.Lerp(.18f, 1f, entradaA), 1f);
        blocoB.localScale = new Vector3(1f, Mathf.Lerp(.18f, 1f, entradaB), 1f);
        float pulso = .5f + .5f * Mathf.Sin(tempo * 2.8f);
        scannerA.anchoredPosition = new Vector2(0f, Mathf.Sin(tempo * 1.8f) * 185f);
        scannerB.anchoredPosition = new Vector2(0f, Mathf.Sin(tempo * 1.8f + 1.6f) * 185f);
        linhaScannerA.color = Transparente(corAcento, .12f + .12f * pulso);
        linhaScannerB.color = Transparente(corAcento, .24f - .12f * pulso);
        eixoEnergia.localScale = new Vector3(.7f + .3f * pulso, 1f, 1f);
        luzEixo.color = Transparente(corAcento, .035f + pulso * .045f);
        grupoChamada.alpha = Suavizar((tempo - .7f) / .35f);
        tituloArena.characterSpacing = Mathf.Lerp(15f, 7f, Suavizar(tempo / .7f));
    }

    private void MostrarCobertura()
    {
        cortina.anchoredPosition = Vector2.zero;
        AplicarAbertura(0f);
        cortina.gameObject.SetActive(true);
        principal.gameObject.SetActive(false);
        canvas.gameObject.SetActive(true);
        EstaExibindo = true;
    }

    private void CancelarSequencia(bool limparResultado = true)
    {
        unchecked { versao++; }
        if (rotinaRevelacao != null) { StopCoroutine(rotinaRevelacao); rotinaRevelacao = null; }
        PararAudio();
        if (limparResultado) UltimaExibicaoConcluida = false;
    }

    private bool SequenciaAtual(int minhaVersao)
    {
        return this != null && isActiveAndEnabled && minhaVersao == versao;
    }

    private static float PassoTempo()
    {
        float passo = Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;
        return float.IsNaN(passo) || float.IsInfinity(passo) ? 0f : Mathf.Max(0f, passo);
    }

    private static float Suavizar(float valor)
    {
        valor = Mathf.Clamp01(valor);
        return valor * valor * (3f - 2f * valor);
    }

    private void AtualizarPalavras()
    {
        tituloArena.text = RoteiroTransmissaoDiscos.Texto(roteiro.tituloPrevia);
        tituloDuelo.text = RoteiroTransmissaoDiscos.Texto(roteiro.tituloDuelo);
        versus.text = RoteiroTransmissaoDiscos.Texto(roteiro.separador);
        chamada.text = RoteiroTransmissaoDiscos.Texto(roteiro.chamadaJogo);
    }

    private void CriarInterface()
    {
        if (canvas != null) return;
        var objeto = new GameObject("AberturaJogoUI.Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        objeto.SetActive(false);
        canvas = objeto.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;
        canvasRect = objeto.GetComponent<RectTransform>();
        var escala = objeto.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escala.matchWidthOrHeight = .5f;
        principal = TelaInteira("Apresentacao", canvasRect);
        Imagem(TelaInteira("BaseOpaca", principal), Opaca(corFundo));
        imagemFundo = Imagem(TelaInteira("Fundo", principal), Color.white);
        Imagem(TelaInteira("Tonalidade", principal), Transparente(Color.Lerp(corFundo, Color.black, .4f), .68f));
        for (int i = 1; i < 12; i++)
            Imagem(Caixa("GradeVertical", principal, new Vector2(i / 12f, .5f), new Vector2(1f, 1080f)), Transparente(corAcento, .065f));
        for (int i = 1; i < 8; i++)
            Imagem(Caixa("GradeHorizontal", principal, new Vector2(.5f, i / 8f), new Vector2(1920f, 1f)), Transparente(corAcento, .065f));
        eixoEnergia = Caixa("EixoEnergia", principal, new Vector2(.5f, .46f), new Vector2(84f, 610f));
        luzEixo = Imagem(eixoEnergia, Transparente(corAcento, .06f));
        Imagem(Caixa("NucleoEnergia", principal, new Vector2(.5f, .46f), new Vector2(2f, 610f)), Transparente(corAcento, .44f));
        Imagem(Caixa("TrilhoSuperior", principal, new Vector2(.5f, .962f), new Vector2(1760f, 2f)), Transparente(corAcento, .8f));
        Imagem(Caixa("TrilhoInferior", principal, new Vector2(.5f, .075f), new Vector2(1360f, 2f)), Transparente(corAcento, .55f));
        RectTransform area = TelaInteira("Conteudo", principal);
        conteudo = Grupo(area);
        tituloArena = Texto("TituloArena", area, new Vector2(.5f, .901f), new Vector2(1340f, 80f), 53f, fonteTitulo);
        textoRodada = Texto("Rodada", area, new Vector2(.5f, .838f), new Vector2(940f, 40f), 27f, fonteTitulo);
        textoRodada.color = corAcento;
        tituloDuelo = Texto("TituloDuelo", area, new Vector2(.5f, .768f), new Vector2(1340f, 46f), 30f, fonteTitulo);
        tituloDuelo.characterSpacing = 8f;
        textoJogo = Texto("Jogo", area, new Vector2(.5f, .712f), new Vector2(700f, 35f), 20f, fonteNomes);
        textoJogo.color = Transparente(corAcento, .88f);
        blocoA = CriarTime(area, "TimeA", .25f, out grupoA, out escudoA, out nomeA, out scannerA, out linhaScannerA);
        blocoB = CriarTime(area, "TimeB", .75f, out grupoB, out escudoB, out nomeB, out scannerB, out linhaScannerB);
        versus = Texto("Versus", area, new Vector2(.5f, .46f), new Vector2(270f, 85f), 44f, fonteTitulo);
        versus.color = corAcento;
        versus.characterSpacing = 3f;
        RectTransform diamante = Caixa("NucleoDiamante", area, new Vector2(.5f, .46f), new Vector2(95f, 95f));
        diamante.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Imagem(diamante, Transparente(corAcento, .065f));
        // Keep the separator above its translucent energy node.
        versus.transform.SetAsLastSibling();
        imagemLiga = Imagem(Caixa("LogoLiga", area, new Vector2(.065f, .886f), new Vector2(118f, 118f)), Color.white);
        imagemCanal = Imagem(Caixa("LogoCanal", area, new Vector2(.935f, .886f), new Vector2(118f, 118f)), Color.white);
        imagemLiga.preserveAspect = imagemCanal.preserveAspect = true;
        RectTransform cta = Caixa("ChamadaEnergia", area, new Vector2(.5f, .131f), new Vector2(320f, 64f));
        grupoChamada = Grupo(cta);
        Imagem(cta, Opaca(corPainel));
        Imagem(Caixa("AcentoChamada", cta, new Vector2(.5f, 0f), new Vector2(320f, 3f)), corAcento);
        chamada = Texto("Inscrever", cta, new Vector2(.5f, .5f), new Vector2(300f, 54f), 30f, fonteTitulo);
        chamada.color = corAcento;
        chamada.characterSpacing = 5f;
        AtualizarPalavras();
        cortina = TelaInteira("Cortina", canvasRect);
        painelSuperior = TelaInteira("PainelSuperior", cortina);
        painelSuperior.anchorMin = new Vector2(0f, .5f);
        painelSuperior.offsetMin = new Vector2(0f, -2f);
        painelSuperior.offsetMax = Vector2.zero;
        Imagem(painelSuperior, Opaca(corFundo));
        painelInferior = TelaInteira("PainelInferior", cortina);
        painelInferior.anchorMax = new Vector2(1f, .5f);
        painelInferior.offsetMin = Vector2.zero;
        painelInferior.offsetMax = new Vector2(0f, 2f);
        Imagem(painelInferior, Opaca(corFundo));
        Imagem(Caixa("LuzFechoSuperior", painelSuperior, new Vector2(.5f, 0f), new Vector2(3840f, 3f)), corAcento);
        Imagem(Caixa("LuzFechoInferior", painelInferior, new Vector2(.5f, 1f), new Vector2(3840f, 3f)), corAcento);
    }

    private RectTransform CriarTime(Transform pai, string id, float x, out CanvasGroup grupo,
        out Image escudo, out TextMeshProUGUI nome, out RectTransform scanner, out Image linha)
    {
        RectTransform bloco = Caixa(id, pai, new Vector2(x, .465f), new Vector2(660f, 530f));
        grupo = Grupo(bloco);
        Imagem(TelaInteira("PlacaHolografica", bloco), Transparente(corPainel, .94f));
        Imagem(Caixa("LuzTopo", bloco, new Vector2(.5f, 1f), new Vector2(660f, 3f)), Transparente(corAcento, .9f));
        Imagem(Caixa("LuzBase", bloco, new Vector2(.5f, 0f), new Vector2(660f, 3f)), Transparente(corAcento, .45f));
        for (int i = 0; i < 4; i++)
        {
            Vector2 canto = new Vector2(i % 2 == 0 ? 0f : 1f, i < 2 ? 0f : 1f);
            Imagem(Caixa("TerminalHorizontal", bloco, canto, new Vector2(40f, 5f)), corAcento);
            Imagem(Caixa("TerminalVertical", bloco, canto, new Vector2(5f, 40f)), corAcento);
        }
        escudo = Imagem(Caixa("Escudo", bloco, new Vector2(.5f, .59f), new Vector2(270f, 270f)), Color.white);
        escudo.preserveAspect = true;
        nome = Texto("Nome", bloco, new Vector2(.5f, .18f), new Vector2(610f, 108f), 54f, fonteNomes);
        nome.fontSizeMin = 27f;
        nome.textWrappingMode = TextWrappingModes.Normal;
        nome.maxVisibleLines = 2;
        scanner = Caixa("Scanner", bloco, new Vector2(.5f, .5f), new Vector2(614f, 3f));
        linha = Imagem(scanner, Transparente(corAcento, .2f));
        return bloco;
    }

    private static Color Transparente(Color cor, float alpha) { cor.a = alpha; return cor; }
    private static Color Opaca(Color cor) { cor.a = 1f; return cor; }
    private static RectTransform Caixa(string nome, Transform pai, Vector2 ancora, Vector2 tamanho)
    {
        var objeto = new GameObject(nome, typeof(RectTransform));
        var area = objeto.GetComponent<RectTransform>();
        area.SetParent(pai, false);
        area.anchorMin = area.anchorMax = ancora;
        area.pivot = new Vector2(.5f, .5f);
        area.sizeDelta = tamanho;
        area.anchoredPosition = Vector2.zero;
        return area;
    }
    private static RectTransform TelaInteira(string nome, Transform pai)
    {
        var area = Caixa(nome, pai, Vector2.zero, Vector2.zero);
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = area.offsetMax = Vector2.zero;
        return area;
    }
    private static Image Imagem(RectTransform area, Color cor)
    {
        var imagem = area.gameObject.AddComponent<Image>();
        imagem.color = cor;
        imagem.raycastTarget = false;
        return imagem;
    }
    private static CanvasGroup Grupo(RectTransform area)
    {
        var grupo = area.gameObject.AddComponent<CanvasGroup>();
        grupo.interactable = grupo.blocksRaycasts = false;
        return grupo;
    }
    private static TextMeshProUGUI Texto(string nome, Transform pai, Vector2 ancora, Vector2 tamanho,
        float tamanhoFonte, TMP_FontAsset fonte)
    {
        var texto = Caixa(nome, pai, ancora, tamanho).gameObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset escolhida = fonte != null ? fonte : TMP_Settings.defaultFontAsset;
        if (escolhida != null) texto.font = escolhida;
        texto.fontSize = tamanhoFonte;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = Mathf.Min(tamanhoFonte, 15f);
        texto.fontSizeMax = tamanhoFonte;
        texto.fontStyle = FontStyles.Bold;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = Color.white;
        texto.richText = false;
        texto.textWrappingMode = TextWrappingModes.NoWrap;
        texto.overflowMode = TextOverflowModes.Ellipsis;
        texto.raycastTarget = false;
        return texto;
    }
    private static void DefinirSprite(Image imagem, Sprite sprite) { imagem.sprite = sprite; imagem.enabled = sprite != null; }
    private static string NomeTime(string valor, string alternativa)
    {
        return string.IsNullOrWhiteSpace(valor) ? alternativa : valor.Replace("\r", " ").Replace("\n", " ").Trim().ToUpperInvariant();
    }
    private Sprite ObterFundoGerado()
    {
        if (spriteGerado != null) return spriteGerado;
        const int largura = 256, altura = 128;
        var pixels = new Color32[largura * altura];
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
                pixels[y * largura + x] = Color.Lerp(Opaca(corFundo), Opaca(corPainel), .72f * x / (largura - 1f) + .28f * y / (altura - 1f));
        texturaGerada = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
        texturaGerada.name = "FundoAberturaHolografica";
        texturaGerada.hideFlags = HideFlags.DontSave;
        texturaGerada.wrapMode = TextureWrapMode.Clamp;
        texturaGerada.filterMode = FilterMode.Bilinear;
        texturaGerada.SetPixels32(pixels);
        texturaGerada.Apply(false, true);
        spriteGerado = Sprite.Create(texturaGerada, new Rect(0f, 0f, largura, altura), new Vector2(.5f, .5f));
        spriteGerado.hideFlags = HideFlags.DontSave;
        return spriteGerado;
    }
    private void TocarAudio()
    {
        if (somAbertura == null) return;
        if (audioAbertura == null)
        {
            audioAbertura = gameObject.AddComponent<AudioSource>();
            audioAbertura.playOnAwake = audioAbertura.loop = false;
            audioAbertura.spatialBlend = 0f;
            audioAbertura.ignoreListenerPause = true;
        }
        audioAbertura.PlayOneShot(somAbertura, Mathf.Clamp01(volume));
    }
    private void PararAudio() { if (audioAbertura != null) audioAbertura.Stop(); }
    private void OnDisable() { Ocultar(); }
    private void OnDestroy()
    {
        Ocultar();
        if (canvas != null) Destroy(canvas.gameObject);
        if (spriteGerado != null) Destroy(spriteGerado);
        if (texturaGerada != null) Destroy(texturaGerada);
        if (audioAbertura != null) Destroy(audioAbertura);
    }
}
