using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AberturaJogoUI : MonoBehaviour
{
    [Header("Imagens")]
    public Sprite fundo;
    public Sprite logoCanal;

    [Header("Fontes opcionais")]
    public TMP_FontAsset fonteTitulo;
    public TMP_FontAsset fonteNomes;

    [Header("Duração da abertura")]
    [Min(3f)]
    public float tempoTotal = 10f;

    [Header("Som opcional")]
    public AudioClip somAbertura;

    [Range(0f, 1f)]
    public float volume = 0.7f;

    public bool EstaExibindo { get; private set; }

    public bool UltimaExibicaoConcluida
    {
        get;
        private set;
    }

    private const float DuracaoRevelacao = 0.6f;

    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform principal;
    private RectTransform cortina;
    private RectTransform blocoA;
    private RectTransform blocoB;

    private CanvasGroup conteudo;
    private CanvasGroup grupoA;
    private CanvasGroup grupoB;

    private Image imagemFundo;
    private Image escudoA;
    private Image escudoB;
    private Image imagemLiga;
    private Image imagemCanal;

    private TextMeshProUGUI textoRodada;
    private TextMeshProUGUI textoJogo;
    private TextMeshProUGUI nomeA;
    private TextMeshProUGUI nomeB;

    private AudioSource audioAbertura;
    private Texture2D texturaGerada;
    private Sprite spriteGerado;

    private Coroutine rotinaRevelacao;
    private int versao;

    public void CobrirTela()
    {
        if (!isActiveAndEnabled) return;

        CancelarSequencia();
        CriarInterface();
        MostrarCobertura();
    }

    public IEnumerator Exibir(
        TeamData timeA,
        TeamData timeB,
        Sprite logoLiga,
        int rodada,
        int numeroJogo,
        int totalJogos)
    {
        CancelarSequencia();

        if (this == null ||
            !isActiveAndEnabled ||
            timeA == null ||
            timeB == null)
        {
            yield break;
        }

        CriarInterface();

        if (textoRodada.font == null ||
            nomeA.font == null)
        {
            Debug.LogError(
                "AberturaJogoUI: atribua fontes TMP aos " +
                "campos Fonte Titulo e Fonte Nomes.",
                this);

            MostrarCobertura();
            yield break;
        }

        int minhaVersao = versao;

        float duracao =
            float.IsNaN(tempoTotal) ||
            float.IsInfinity(tempoTotal)
                ? 10f
                : Mathf.Max(3f, tempoTotal);

        imagemFundo.sprite = fundo != null
            ? fundo
            : ObterFundoGerado();

        DefinirSprite(
            escudoA, timeA.escudoOuBandeira);

        DefinirSprite(
            escudoB, timeB.escudoOuBandeira);

        DefinirSprite(imagemLiga, logoLiga);
        DefinirSprite(imagemCanal, logoCanal);

        nomeA.text = NomeTime(
            timeA.nomeEquipe, "HOME TEAM");

        nomeB.text = NomeTime(
            timeB.nomeEquipe, "AWAY TEAM");

        textoRodada.text =
            $"MATCHDAY {Mathf.Max(1, rodada)}  /  GIORNATA {Mathf.Max(1, rodada)}";

        textoJogo.text =
            $"MATCH {Mathf.Max(1, numeroJogo)}  /  PARTITA {Mathf.Max(1, numeroJogo)}";

        principal.gameObject.SetActive(true);
        cortina.gameObject.SetActive(false);
        canvas.gameObject.SetActive(true);

        EstaExibindo = true;
        AnimarEntrada(0f);

        Canvas.ForceUpdateCanvases();

        float largura = LarguraCanvas();

        TocarAudio();

        double tempo = 0;

        while (SequenciaAtual(minhaVersao))
        {
            float atual = (float)tempo;

            AnimarEntrada(atual);

            // A cortina fecha no último segundo.
            // O próximo jogo será preparado por trás dela.
            if (atual >= duracao - 1f)
            {
                cortina.gameObject.SetActive(true);

                float progresso = Suavizar(
                    (atual - (duracao - 1f)) / 0.65f);

                cortina.anchoredPosition = new Vector2(
                    Mathf.Lerp(
                        -largura, 0f, progresso),
                    0f);

                if (progresso >= 1f)
                {
                    cortina.anchoredPosition =
                        Vector2.zero;

                    principal.gameObject.SetActive(false);
                }
            }

            if (tempo + 0.000001 >= duracao)
                break;

            yield return null;

            tempo += PassoTempo();
        }

        if (!SequenciaAtual(minhaVersao))
            yield break;

        MostrarCobertura();
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

        if (rotinaRevelacao != null)
            return;

        if (canvas == null ||
            !canvas.gameObject.activeSelf)
        {
            EstaExibindo = false;
            return;
        }

        CancelarSequencia(false);
        MostrarCobertura();

        rotinaRevelacao = StartCoroutine(
            Revelar(versao));
    }

    public void Ocultar()
    {
        CancelarSequencia();

        if (canvas != null)
            canvas.gameObject.SetActive(false);

        EstaExibindo = false;
    }

    private IEnumerator Revelar(int minhaVersao)
    {
        Canvas.ForceUpdateCanvases();

        float largura = LarguraCanvas();
        double tempo = 0;

        while (SequenciaAtual(minhaVersao))
        {
            float progresso =
                (float)(tempo / DuracaoRevelacao);

            cortina.anchoredPosition = new Vector2(
                largura * Suavizar(progresso),
                0f);

            if (tempo + 0.000001 >= DuracaoRevelacao)
                break;

            yield return null;

            tempo += PassoTempo();
        }

        if (!SequenciaAtual(minhaVersao))
            yield break;

        canvas.gameObject.SetActive(false);

        EstaExibindo = false;
        rotinaRevelacao = null;

        // Preserva o resultado de uma exibição concluída.
    }

    private static float PassoTempo()
    {
        // No Recorder com tempo de captura fixo,
        // acompanha a duração do vídeo gravado.
        float captura = Time.captureDeltaTime;

        return captura > 0f
            ? captura
            : Time.unscaledDeltaTime;
    }

    private void AnimarEntrada(float tempo)
    {
        conteudo.alpha = Suavizar(tempo / 0.55f);

        float entradaA =
            Suavizar(tempo / 0.9f);

        float entradaB =
            Suavizar((tempo - 0.12f) / 0.9f);

        grupoA.alpha = entradaA;
        grupoB.alpha = entradaB;

        blocoA.anchoredPosition = new Vector2(
            -90f * (1f - entradaA), 0f);

        blocoB.anchoredPosition = new Vector2(
            90f * (1f - entradaB), 0f);

        blocoA.localScale = Vector3.one *
            Mathf.Lerp(0.92f, 1f, entradaA);

        blocoB.localScale = Vector3.one *
            Mathf.Lerp(0.92f, 1f, entradaB);

        // Movimento discreto do fundo durante a apresentação.
        imagemFundo.rectTransform.localScale =
            Vector3.one *
            (1.03f + Mathf.Sin(tempo * 0.65f) * 0.006f);

        imagemFundo.rectTransform.anchoredPosition =
            new Vector2(
                Mathf.Sin(tempo * 0.35f) * 7f,
                Mathf.Cos(tempo * 0.4f) * 3f);
    }

    private void MostrarCobertura()
    {
        cortina.anchoredPosition = Vector2.zero;

        cortina.gameObject.SetActive(true);
        principal.gameObject.SetActive(false);
        canvas.gameObject.SetActive(true);

        EstaExibindo = true;
    }

    private void CancelarSequencia(
        bool limparResultado = true)
    {
        unchecked
        {
            versao++;
        }

        if (rotinaRevelacao != null)
        {
            StopCoroutine(rotinaRevelacao);
            rotinaRevelacao = null;
        }

        PararAudio();

        if (limparResultado)
            UltimaExibicaoConcluida = false;
    }

    private bool SequenciaAtual(int minhaVersao)
    {
        return this != null &&
            isActiveAndEnabled &&
            minhaVersao == versao;
    }

    private float LarguraCanvas()
    {
        float largura = canvasRect.rect.width;

        return largura > 0f ? largura : 1920f;
    }

    private static float Suavizar(float valor)
    {
        valor = Mathf.Clamp01(valor);

        return valor * valor * (3f - 2f * valor);
    }

    private void CriarInterface()
    {
        if (canvas != null) return;

        var objeto = new GameObject(
            "AberturaJogoUI.Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));

        objeto.SetActive(false);

        canvas = objeto.GetComponent<Canvas>();
        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder = 2000;

        canvasRect =
            objeto.GetComponent<RectTransform>();

        var escala =
            objeto.GetComponent<CanvasScaler>();

        escala.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        escala.referenceResolution =
            new Vector2(1920f, 1080f);

        escala.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        escala.matchWidthOrHeight = 0.5f;

        principal = TelaInteira(
            "Apresentacao", canvasRect);

        imagemFundo = Imagem(
            TelaInteira("Fundo", principal),
            Color.white);

        Imagem(
            TelaInteira("Tonalidade", principal),
            new Color(0f, 0.03f, 0.12f, 0.40f));

        Imagem(
            Caixa(
                "LinhaJogoEsquerda",
                principal,
                new Vector2(0.34f, 0.795f),
                new Vector2(130f, 3f)),
            new Color(0.65f, 0.88f, 1f, 0.7f));

        Imagem(
            Caixa(
                "LinhaJogoDireita",
                principal,
                new Vector2(0.66f, 0.795f),
                new Vector2(130f, 3f)),
            new Color(0.65f, 0.88f, 1f, 0.7f));

        Imagem(
            Caixa(
                "LinhaRodape",
                principal,
                new Vector2(0.5f, 0.085f),
                new Vector2(900f, 2f)),
            new Color(0.4f, 0.75f, 1f, 0.3f));

        RectTransform areaConteudo =
            TelaInteira("Conteudo", principal);

        conteudo = Grupo(areaConteudo);

        textoRodada = Texto(
            "Rodada",
            areaConteudo,
            new Vector2(0.5f, 0.905f),
            new Vector2(1340f, 120f),
            66f,
            fonteTitulo);

        textoJogo = Texto(
            "Jogo",
            areaConteudo,
            new Vector2(0.5f, 0.795f),
            new Vector2(900f, 95f),
            48f,
            fonteTitulo);

        textoRodada.characterSpacing = 3f;
        textoJogo.characterSpacing = 3f;

        textoJogo.color =
            new Color(0.75f, 0.94f, 1f);

        blocoA = CriarTime(
            areaConteudo,
            "TimeA",
            0.235f,
            out grupoA,
            out escudoA,
            out nomeA);

        blocoB = CriarTime(
            areaConteudo,
            "TimeB",
            0.765f,
            out grupoB,
            out escudoB,
            out nomeB);

        var versus = Texto(
            "Versus",
            areaConteudo,
            new Vector2(0.5f, 0.56f),
            new Vector2(280f, 180f),
            130f,
            fonteTitulo);

        versus.text = "VS";
        versus.characterSpacing = 8f;

        imagemLiga = Imagem(
            Caixa(
                "LogoLiga",
                areaConteudo,
                new Vector2(0.5f, 0.175f),
                new Vector2(200f, 160f)),
            Color.white);

        imagemLiga.preserveAspect = true;

        RectTransform areaCanal = Caixa(
            "LogoCanal",
            areaConteudo,
            Vector2.one,
            new Vector2(180f, 180f));

        areaCanal.pivot = Vector2.one;

        areaCanal.anchoredPosition =
            new Vector2(-40f, -28f);

        imagemCanal =
            Imagem(areaCanal, Color.white);

        imagemCanal.preserveAspect = true;

        var inscrever = Texto(
            "Inscrever",
            areaConteudo,
            new Vector2(0.5f, 0.055f),
            new Vector2(500f, 40f),
            32f,
            fonteTitulo);

        inscrever.text = "SUBSCRIBE";
        inscrever.characterSpacing = 6f;

        inscrever.color = Color.red;

        cortina = TelaInteira(
            "Cortina", canvasRect);

        Imagem(
            cortina,
            new Color(0.035f, 0.018f, 0.11f, 1f));

        RectTransform borda =
            TelaInteira("Borda", cortina);

        borda.anchorMin =
            new Vector2(1f, 0f);

        borda.anchorMax = Vector2.one;

        borda.sizeDelta =
            new Vector2(12f, 0f);

        Imagem(
            borda,
            new Color(0.2f, 0.95f, 1f, 1f));
    }

    private RectTransform CriarTime(
        Transform pai,
        string identificacao,
        float x,
        out CanvasGroup grupo,
        out Image escudo,
        out TextMeshProUGUI nome)
    {
        RectTransform bloco = Caixa(
            identificacao,
            pai,
            new Vector2(x, 0.5f),
            new Vector2(680f, 660f));

        grupo = Grupo(bloco);

        escudo = Imagem(
            Caixa(
                "Escudo",
                bloco,
                new Vector2(0.5f, 0.63f),
                new Vector2(360f, 360f)),
            Color.white);

        escudo.preserveAspect = true;

        nome = Texto(
            "Nome",
            bloco,
            new Vector2(0.5f, 0.19f),
            new Vector2(650f, 150f),
            64f,
            fonteNomes);

        nome.enableAutoSizing = true;
        nome.fontSizeMin = 30f;
        nome.fontSizeMax = 64f;
        nome.textWrappingMode = TextWrappingModes.Normal;
        nome.maxVisibleLines = 2;
        nome.overflowMode = TextOverflowModes.Ellipsis;

        return bloco;
    }

    private static RectTransform Caixa(
        string nome,
        Transform pai,
        Vector2 ancora,
        Vector2 tamanho)
    {
        var objeto = new GameObject(
            nome, typeof(RectTransform));

        var area =
            objeto.GetComponent<RectTransform>();

        area.SetParent(pai, false);
        area.anchorMin = ancora;
        area.anchorMax = ancora;

        area.pivot =
            new Vector2(0.5f, 0.5f);

        area.sizeDelta = tamanho;
        area.anchoredPosition = Vector2.zero;

        return area;
    }

    private static RectTransform TelaInteira(
        string nome, Transform pai)
    {
        var area = Caixa(
            nome,
            pai,
            Vector2.zero,
            Vector2.zero);

        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;

        return area;
    }

    private static Image Imagem(
        RectTransform area, Color cor)
    {
        var imagem =
            area.gameObject.AddComponent<Image>();

        imagem.color = cor;
        imagem.raycastTarget = false;

        return imagem;
    }

    private static CanvasGroup Grupo(
        RectTransform area)
    {
        var grupo =
            area.gameObject.AddComponent<CanvasGroup>();

        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        return grupo;
    }

    private static TextMeshProUGUI Texto(
        string nome,
        Transform pai,
        Vector2 ancora,
        Vector2 tamanho,
        float tamanhoFonte,
        TMP_FontAsset fonte)
    {
        var texto = Caixa(
            nome, pai, ancora, tamanho)
            .gameObject
            .AddComponent<TextMeshProUGUI>();

        TMP_FontAsset escolhida =
            fonte != null
                ? fonte
                : TMP_Settings.defaultFontAsset;

        if (escolhida != null)
            texto.font = escolhida;

        texto.fontSize = tamanhoFonte;
        texto.fontStyle = FontStyles.Bold;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = Color.white;
        texto.richText = false;
        texto.textWrappingMode = TextWrappingModes.NoWrap;
        texto.raycastTarget = false;

        return texto;
    }

    private static void DefinirSprite(
        Image imagem, Sprite sprite)
    {
        imagem.sprite = sprite;
        imagem.enabled = sprite != null;
    }

    private static string NomeTime(
        string valor, string alternativa)
    {
        return string.IsNullOrWhiteSpace(valor)
            ? alternativa
            : valor.Replace("\r", " ")
                .Replace("\n", " ")
                .Trim()
                .ToUpperInvariant();
    }

    private Sprite ObterFundoGerado()
    {
        if (spriteGerado != null)
            return spriteGerado;

        const int largura = 256;
        const int altura = 128;

        var pixels =
            new Color32[largura * altura];

        Color azul =
            new Color(0.015f, 0.25f, 0.38f);

        Color roxo =
            new Color(0.25f, 0.035f, 0.36f);

        for (int y = 0; y < altura; y++)
        {
            for (int x = 0; x < largura; x++)
            {
                float t =
                    0.85f * x / (largura - 1f) +
                    0.15f * y / (altura - 1f);

                pixels[y * largura + x] =
                    Color.Lerp(azul, roxo, t);
            }
        }

        texturaGerada = new Texture2D(
            largura,
            altura,
            TextureFormat.RGBA32,
            false);

        texturaGerada.name = "FundoAbertura";
        texturaGerada.hideFlags = HideFlags.DontSave;
        texturaGerada.wrapMode = TextureWrapMode.Clamp;
        texturaGerada.filterMode = FilterMode.Bilinear;

        texturaGerada.SetPixels32(pixels);
        texturaGerada.Apply(false, true);

        spriteGerado = Sprite.Create(
            texturaGerada,
            new UnityEngine.Rect(
                0f, 0f, largura, altura),
            new Vector2(0.5f, 0.5f));

        spriteGerado.hideFlags = HideFlags.DontSave;

        return spriteGerado;
    }

    private void TocarAudio()
    {
        if (somAbertura == null) return;

        if (audioAbertura == null)
        {
            audioAbertura =
                gameObject.AddComponent<AudioSource>();

            audioAbertura.playOnAwake = false;
            audioAbertura.loop = false;
            audioAbertura.spatialBlend = 0f;
            audioAbertura.ignoreListenerPause = true;
        }

        audioAbertura.PlayOneShot(
            somAbertura,
            Mathf.Clamp01(volume));
    }

    private void PararAudio()
    {
        if (audioAbertura != null)
            audioAbertura.Stop();
    }

    private void OnDisable()
    {
        Ocultar();
    }

    private void OnDestroy()
    {
        Ocultar();

        if (canvas != null)
            Destroy(canvas.gameObject);

        if (spriteGerado != null)
            Destroy(spriteGerado);

        if (texturaGerada != null)
            Destroy(texturaGerada);

        if (audioAbertura != null)
            Destroy(audioAbertura);
    }
}