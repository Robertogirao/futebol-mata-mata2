using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One portrait per contender, with the original match/scoring controller.</summary>
[DefaultExecutionOrder(-150)]
[DisallowMultipleComponent]
public sealed class PokemonHUD : MonoBehaviour
{
    [Header("Referências")]
    public UniversalUI universalUI;
    public TemaCompeticao tema;
    public Canvas canvasAntigo;
    public GoalBannerUI bannerOriginal;

    [Header("Apresentação da arena")]
    public bool mostrarLegendas = true;
    [Tooltip("Adiciona o lançador móvel no Cano_Automatico. Para salvar ajustes próprios, use Tools > Pokémon > Configurar lançador da Equipe Rocket antes do Play.")]
    public bool usarEquipeRocket = true;
    [Tooltip("Usa os três painéis inferiores para SNORLAX RAGE, GIANT SNORLAX e ROCKET FRENZY. Configuração em Tools > Pokémon > Configurar eventos da partida.")]
    public bool ativarEventosAleatorios = true;

    private Canvas canvas;
    private Image fotoA;
    private Image fotoB;
    private Image corA;
    private Image corB;
    private Image brilhoA;
    private Image brilhoB;
    private Image logoCanal;
    private Image logoCopa;
    private TextMeshProUGUI nomeA;
    private TextMeshProUGUI nomeB;
    private TextMeshProUGUI placarA;
    private TextMeshProUGUI placarB;
    private TextMeshProUGUI relogio;
    private PainelEvento painelRocket;
    private PainelEvento painelSnorlax;
    private PainelEvento painelGigante;
    private EventosPartidaPokemon eventos;
    private RectTransform raiz;
    private RectTransform raizPontos;
    private GameObject painelPonto;
    private TMP_FontAsset fonteTitulo;
    private TMP_FontAsset fonteTexto;
    private float escalaTitulo = 1f;
    private float escalaTexto = 1f;
    private Color primaria;
    private Color destaque;
    private Color texto;
    private Color participanteA = new Color(1f, 0.77f, 0.05f);
    private Color participanteB = new Color(1f, 0.35f, 0.07f);
    private readonly List<GameObject> pontosAtivos = new List<GameObject>();

    private void Awake()
    {
        if (isActiveAndEnabled) Preparar();
    }

    public bool Preparar()
    {
        if (canvas != null) return true;
        LigaPokemon liga = GetComponent<LigaPokemon>();
        if (liga != null)
        {
            TemaCompeticao temaLiga = liga.TemaEmUso != null ? liga.TemaEmUso : liga.tema;
            if (temaLiga != null) tema = temaLiga;
        }
        if (universalUI == null) universalUI = GetComponent<UniversalUI>();
        if (universalUI == null) universalUI = FindFirstObjectByType<UniversalUI>();
        if (universalUI == null)
        {
            Debug.LogError("PokemonHUD: atribua UniversalUI antes de iniciar a cena.", this);
            return false;
        }

        if (canvasAntigo == null) canvasAntigo = universalUI.GetComponent<Canvas>();
        if (bannerOriginal == null) bannerOriginal = FindFirstObjectByType<GoalBannerUI>();
        ConfigurarTema();
        CriarCanvas();
        CriarPlacar();
        CriarBanner();
        if (mostrarLegendas || ativarEventosAleatorios) CriarLegendas();
        PrepararLancadorEquipeRocket();
        if (ativarEventosAleatorios)
        {
            eventos = GetComponent<EventosPartidaPokemon>();
            if (eventos == null) eventos = gameObject.AddComponent<EventosPartidaPokemon>();
        }

        universalUI.hudPokemon = this;
        universalUI.cronometroRealRegressivo = true;
        universalUI.escudoTimeA = fotoA;
        universalUI.escudoTimeB = fotoB;
        universalUI.logoCampeonato = logoCopa;
        universalUI.placarTimeA = placarA;
        universalUI.placarTimeB = placarB;
        universalUI.textoCronometro = relogio;
        universalUI.bannerGol = painelPonto;
        universalUI.fotosTitularesA = new Image[0];
        universalUI.fotosTitularesB = new Image[0];
        universalUI.nomesTitularesA = new TextMeshProUGUI[0];
        universalUI.nomesTitularesB = new TextMeshProUGUI[0];
        universalUI.numerosTitularesA = new TextMeshProUGUI[0];
        universalUI.numerosTitularesB = new TextMeshProUGUI[0];

        // Keep the old GameObject and its scoring/banner behaviours alive.
        // The new canvas is a separate child of this tournament object.
        if (canvasAntigo != null) canvasAntigo.enabled = false;
        return true;
    }

    private void PrepararLancadorEquipeRocket()
    {
        if (!usarEquipeRocket) return;
        TournamentManager gerenciador = GetComponent<TournamentManager>();
        AutoBallPipe cano = gerenciador != null ? gerenciador.canoAutomatizado : null;
        if (cano == null) cano = FindFirstObjectByType<AutoBallPipe>();
        if (cano != null && cano.GetComponent<LancadorEquipeRocket>() == null)
            cano.gameObject.AddComponent<LancadorEquipeRocket>();
    }

    public void ConfigurarPartida(
        TeamData timeA, TeamData timeB, Sprite trofeu,
        List<JogadorDados> titularesA, List<JogadorDados> titularesB)
    {
        if (canvas == null && !Preparar()) return;
        LimparPontos();
        if (eventos != null) eventos.LimparEventos();
        participanteA = timeA != null ? CorValida(timeA.corPrincipal, participanteA) : participanteA;
        participanteB = timeB != null ? CorValida(timeB.corPrincipal, participanteB) : participanteB;
        corA.color = participanteA;
        corB.color = participanteB;
        brilhoA.color = ComAlpha(participanteA, 0.55f);
        brilhoB.color = ComAlpha(participanteB, 0.55f);
        nomeA.text = Nome(timeA != null ? timeA.nomeEquipe : null, titularesA, "POKÉMON A");
        nomeB.text = Nome(timeB != null ? timeB.nomeEquipe : null, titularesB, "POKÉMON B");
        nomeA.color = CorSobre( participanteA );
        nomeB.color = CorSobre( participanteB );
        AtribuirRetrato(fotoA, timeA, titularesA);
        AtribuirRetrato(fotoB, timeB, titularesB);
        logoCopa.sprite = trofeu != null ? trofeu : tema != null ? tema.logoCompeticao : null;
        logoCopa.enabled = logoCopa.sprite != null;
        relogio.text = FormatarTempo(universalUI.duracaoRealEmSegundos);
        painelPonto.SetActive(false);
    }

    public void MostrarPonto(bool ladoA)
    {
        if (!isActiveAndEnabled || raizPontos == null) return;
        Color cor = ladoA ? participanteA : participanteB;
        int faixa = pontosAtivos.Count % 3;
        RectTransform ponto = Caixa("PontoMaisUm", raizPontos,
            new Vector2(710f - faixa * 25f, 20f + faixa * 30f), new Vector2(240f, 140f));
        CanvasGroup grupo = ponto.gameObject.AddComponent<CanvasGroup>();
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
        Texto("Sombra", ponto, new Vector2(4f, -4f), new Vector2(230f, 130f), "+1!",
            94f, new Color(0.005f, 0.015f, 0.04f), true);
        Texto("Valor", ponto, Vector2.zero, new Vector2(230f, 130f), "+1!",
            94f, Color.Lerp(cor, Color.white, 0.25f), true);
        pontosAtivos.Add(ponto.gameObject);
        StartCoroutine(AnimarPonto(ponto, grupo));
    }

    private IEnumerator AnimarPonto(RectTransform ponto, CanvasGroup grupo)
    {
        Vector2 origem = ponto.anchoredPosition;
        double tempo = 0d;
        const float duracao = 1.35f;
        while (ponto != null && tempo < duracao)
        {
            float t = Mathf.Clamp01((float)tempo / duracao);
            ponto.anchoredPosition = origem + new Vector2(0f, 115f * t);
            grupo.alpha = t < 0.65f ? 1f : 1f - (t - 0.65f) / 0.35f;
            yield return null;
            tempo += PassoTempo();
        }
        if (ponto != null)
        {
            pontosAtivos.Remove(ponto.gameObject);
            Destroy(ponto.gameObject);
        }
    }

    private void ConfigurarTema()
    {
        primaria = tema != null ? tema.corPrimaria : new Color(0.025f, 0.055f, 0.14f);
        destaque = tema != null ? tema.corDestaque : new Color(0.10f, 0.91f, 1f);
        texto = tema != null ? tema.corTexto : Color.white;
        fonteTitulo = tema != null && tema.fonteTitulo != null
            ? tema.fonteTitulo : TMP_Settings.defaultFontAsset;
        fonteTexto = tema != null && tema.fonteTexto != null
            ? tema.fonteTexto : TMP_Settings.defaultFontAsset;
        escalaTitulo = Escala(tema != null ? tema.escalaFonteTitulo : 1f);
        escalaTexto = Escala(tema != null ? tema.escalaFonteTexto : 1f);
    }

    private void CriarCanvas()
    {
        GameObject objeto = new GameObject("PokemonHUD.Canvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler));
        objeto.transform.SetParent(transform, false);
        canvas = objeto.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1200;
        CanvasScaler scaler = objeto.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        raiz = Caixa("TransmissaoPokemon", objeto.transform, Vector2.zero, new Vector2(1920f, 1080f));
    }

    private void CriarPlacar()
    {
        // Artwork/logo at the left, with one compact contender card per side.
        logoCanal = Retrato("LogoCanal", raiz, new Vector2(-755f, 442f),
            new Vector2(350f, 142f), tema != null ? tema.logoCanal : null);
        if (logoCanal.sprite == null)
        {
            Texto("MarcaPokemon", raiz, new Vector2(-755f, 467f), new Vector2(350f, 65f),
                "POKÉMON", 61f, new Color(1f, 0.86f, 0.08f), true);
            Texto("MarcaCanal", raiz, new Vector2(-755f, 411f), new Vector2(350f, 42f),
                "BEAT THE SNORLAX", 29f, texto, true);
        }

        Painel("BasePlacar", raiz, new Vector2(140f, 456f), new Vector2(1320f, 128f),
            destaque, Color.Lerp(primaria, Color.black, 0.45f), 4f);
        Imagem(Caixa("LuzSuperior", raiz, new Vector2(140f, 522f), new Vector2(1160f, 4f)), destaque);
        brilhoA = Imagem(Caixa("BrilhoA", raiz, new Vector2(-475f, 458f), new Vector2(166f, 166f)),
            ComAlpha(participanteA, 0.55f));
        brilhoB = Imagem(Caixa("BrilhoB", raiz, new Vector2(665f, 458f), new Vector2(166f, 166f)),
            ComAlpha(participanteB, 0.55f));
        Painel("BaseRetratoA", raiz, new Vector2(-475f, 458f), new Vector2(156f, 156f),
            destaque, primaria, 3f);
        Painel("BaseRetratoB", raiz, new Vector2(665f, 458f), new Vector2(156f, 156f),
            destaque, primaria, 3f);
        corA = Imagem(Caixa("CorParticipanteA", raiz, new Vector2(-235f, 460f), new Vector2(310f, 88f)), participanteA);
        corB = Imagem(Caixa("CorParticipanteB", raiz, new Vector2(425f, 460f), new Vector2(310f, 88f)), participanteB);
        AcentoInclinado("EntradaA", raiz, new Vector2(-402f, 460f), 1f);
        AcentoInclinado("EntradaB", raiz, new Vector2(592f, 460f), -1f);

        fotoA = Retrato("RetratoPokemonA", raiz, new Vector2(-475f, 463f), new Vector2(146f, 146f), null);
        fotoB = Retrato("RetratoPokemonB", raiz, new Vector2(665f, 463f), new Vector2(146f, 146f), null);
        nomeA = Texto("NomePokemonA", raiz, new Vector2(-235f, 460f), new Vector2(292f, 78f),
            "POKÉMON A", 48f, CorSobre(participanteA), true);
        nomeB = Texto("NomePokemonB", raiz, new Vector2(425f, 460f), new Vector2(292f, 78f),
            "POKÉMON B", 48f, CorSobre(participanteB), true);
        placarA = Texto("PlacarA", raiz, new Vector2(-5f, 460f), new Vector2(104f, 110f), "0", 104f, texto, true);
        Texto("SeparadorPlacar", raiz, new Vector2(95f, 460f), new Vector2(70f, 95f), "–", 77f, texto, true);
        placarB = Texto("PlacarB", raiz, new Vector2(190f, 460f), new Vector2(104f, 110f), "0", 104f, texto, true);

        Painel("MolduraCronometro", raiz, new Vector2(95f, 336f), new Vector2(385f, 94f),
            destaque, Color.Lerp(primaria, Color.black, 0.38f), 4f);
        relogio = Texto("Cronometro", raiz, new Vector2(95f, 336f), new Vector2(354f, 86f),
            FormatarTempo(universalUI.duracaoRealEmSegundos), 67f, texto, true, 2);
        relogio.richText = true;
        logoCopa = Retrato("LogoTorneio", raiz, new Vector2(-907f, 332f), new Vector2(49f, 49f),
            tema != null ? tema.logoCompeticao : null);
    }

    private void CriarBanner()
    {
        RectTransform painel = Painel("ComemoracaoPonto", raiz, new Vector2(95f, 206f),
            new Vector2(650f, 112f), destaque, Color.Lerp(primaria, Color.black, 0.30f), 3f);
        painelPonto = painel.gameObject;
        Image faixa = Imagem(Caixa("CorPonto", painel, new Vector2(-317f, 0f), new Vector2(10f, 102f)), destaque);
        Image foto = Retrato("PokemonMarcou", painel, new Vector2(-250f, 0f), new Vector2(100f, 100f), null);
        TextMeshProUGUI mensagem = Texto("TextoPonto", painel, new Vector2(52f, 0f),
            new Vector2(475f, 100f), "POINT!", 39f, texto, true, 2);
        mensagem.richText = true;
        if (bannerOriginal != null)
        {
            bannerOriginal.LimparFila();
            bannerOriginal.painelBanner = painelPonto;
            bannerOriginal.fotoJogador = foto;
            bannerOriginal.textoDados = mensagem;
            bannerOriginal.faixaCor = faixa;
            bannerOriginal.textoEvento = "POINT!";
            bannerOriginal.LimparFila();
        }
        else Debug.LogWarning("PokemonHUD: atribua GoalBannerUI para exibir a fila de comemorações.", this);
        painelPonto.SetActive(false);
        raizPontos = Caixa("PontosFlutuantes", raiz, Vector2.zero, new Vector2(1920f, 1080f));
    }

    private void CriarLegendas()
    {
        if (ativarEventosAleatorios)
        {
            painelRocket = CriarPainelEvento("EstadoRocket", new Vector2(-400f, -476f));
            painelSnorlax = CriarPainelEvento("EstadoSnorlax", new Vector2(595f, -476f));
            painelGigante = CriarPainelEvento("EstadoGigante", new Vector2(100f, -476f));
            AtualizarEventos(EventoArenaPokemon.Nenhum, EtapaEventoPokemon.Concluido, 0f, 0f);
        }
        else
        {
            CriarLegenda("LegendaHelice", new Vector2(-400f, -476f), new Vector2(470f, 55f), "ENERGY SPINNER");
            CriarLegenda("LegendaPortal", new Vector2(595f, -476f), new Vector2(470f, 55f), "SCORING PORTAL");
        }
    }

    private sealed class PainelEvento
    {
        public TextMeshProUGUI titulo;
        public TextMeshProUGUI estado;
        public Image borda;
        public Image barra;
    }

    private PainelEvento CriarPainelEvento(string nome, Vector2 posicao)
    {
        RectTransform rect = Painel(nome, raiz, posicao, new Vector2(470f, 80f), destaque,
            ComAlpha(Color.Lerp(primaria, Color.black, 0.4f), 0.96f), 3f);
        var painel = new PainelEvento();
        painel.borda = rect.GetComponent<Image>();
        painel.titulo = Texto("Titulo", rect, new Vector2(0f, 15f), new Vector2(440f, 34f), "", 27f, texto, true);
        painel.estado = Texto("Estado", rect, new Vector2(0f, -13f), new Vector2(440f, 23f), "", 18f, destaque, false);
        Imagem(Caixa("Trilho", rect, new Vector2(0f, -32f), new Vector2(436f, 5f)), ComAlpha(destaque, 0.20f));
        RectTransform barra = Caixa("Progresso", rect, new Vector2(-218f, -32f), new Vector2(436f, 5f));
        barra.pivot = new Vector2(0f, 0.5f);
        painel.barra = Imagem(barra, destaque);
        return painel;
    }

    public void AtualizarEventos(EventoArenaPokemon evento, EtapaEventoPokemon etapa, float restante, float progresso)
    {
        AtualizarPainelEvento(painelRocket, evento == EventoArenaPokemon.RocketFrenzy, etapa,
            restante, progresso, "TEAM ROCKET", "PLOTTING...", "ROCKET FRENZY", "EXTRA BALLS!", new Color(1f, 0.28f, 0.65f));
        AtualizarPainelEvento(painelSnorlax, evento == EventoArenaPokemon.SnorlaxRage, etapa,
            restante, progresso, "SNORLAX", "KEEP YOUR DISTANCE", "SNORLAX RAGE", "REPEL & DISINTEGRATE!", new Color(1f, 0.48f, 0.12f));
        AtualizarPainelEvento(painelGigante, evento == EventoArenaPokemon.SnorlaxGigante, etapa,
            restante, progresso, "GIANT SNORLAX", "PORTAL GUARD", "GIANT SNORLAX", "PORTAL BLOCKED!", new Color(0.35f, 0.82f, 1f));
    }

    private void AtualizarPainelEvento(PainelEvento painel, bool selecionado, EtapaEventoPokemon etapa,
        float restante, float progresso, string tituloNormal, string estadoNormal, string tituloEvento, string acao, Color cor)
    {
        if (painel == null) return;
        bool aviso = selecionado && etapa == EtapaEventoPokemon.Aviso;
        bool ativo = selecionado && etapa == EtapaEventoPokemon.Ativo;
        painel.titulo.text = aviso || ativo ? tituloEvento : tituloNormal;
        painel.estado.text = aviso ? "GET READY!" : ativo ? acao + "  " + Mathf.CeilToInt(restante) + "s" : estadoNormal;
        painel.borda.color = aviso || ativo ? cor : destaque;
        painel.titulo.color = aviso || ativo ? cor : texto;
        painel.barra.color = cor;
        painel.barra.rectTransform.localScale = new Vector3(aviso ? Mathf.Clamp01(progresso) : ativo ? 1f - Mathf.Clamp01(progresso) : 0f, 1f, 1f);
    }

    private void CriarLegenda(string nome, Vector2 posicao, Vector2 tamanho, string legenda)
    {
        RectTransform painel = Painel(nome, raiz, posicao, tamanho, destaque,
            ComAlpha(Color.Lerp(primaria, Color.black, 0.4f), 0.94f), 3f);
        Texto("Texto", painel, Vector2.zero, tamanho - new Vector2(16f, 6f), legenda, 30f, texto, true);
    }

    private void AcentoInclinado(string nome, Transform pai, Vector2 posicao, float lado)
    {
        RectTransform rect = Caixa(nome, pai, posicao, new Vector2(8f, 87f));
        rect.localRotation = Quaternion.Euler(0f, 0f, -lado * 18f);
        Imagem(rect, destaque);
    }

    private TextMeshProUGUI Texto(string nome, Transform pai, Vector2 posicao,
        Vector2 tamanho, string valor, float fonte, Color cor, bool titulo, int linhas = 1)
    {
        TextMeshProUGUI label = Caixa(nome, pai, posicao, tamanho).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = titulo ? fonteTitulo : fonteTexto;
        label.text = valor;
        float escala = titulo ? escalaTitulo : escalaTexto;
        label.fontSize = fonte * escala;
        label.enableAutoSizing = true;
        label.fontSizeMax = fonte * escala;
        label.fontSizeMin = Mathf.Min(fonte * 0.46f, fonte * escala);
        label.fontStyle = titulo ? FontStyles.Bold : FontStyles.Normal;
        label.color = cor;
        label.alignment = TextAlignmentOptions.Center;
        label.richText = false;
        label.raycastTarget = false;
        label.textWrappingMode = linhas > 1 ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        label.maxVisibleLines = linhas;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    private static RectTransform Painel(string nome, Transform pai, Vector2 posicao,
        Vector2 tamanho, Color borda, Color fundo, float espessura)
    {
        RectTransform painel = Caixa(nome, pai, posicao, tamanho);
        Imagem(painel, borda);
        Imagem(Caixa("Interior", painel, Vector2.zero,
            tamanho - new Vector2(espessura * 2f, espessura * 2f)), fundo);
        return painel;
    }

    private static Image Retrato(string nome, Transform pai, Vector2 posicao, Vector2 tamanho, Sprite sprite)
    {
        Image imagem = Imagem(Caixa(nome, pai, posicao, tamanho), Color.white);
        imagem.sprite = sprite;
        imagem.preserveAspect = true;
        imagem.enabled = sprite != null;
        return imagem;
    }

    private static Image Imagem(RectTransform rect, Color cor)
    {
        Image imagem = rect.gameObject.AddComponent<Image>();
        imagem.color = cor;
        imagem.raycastTarget = false;
        return imagem;
    }

    private static RectTransform Caixa(string nome, Transform pai, Vector2 posicao, Vector2 tamanho)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform));
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.SetParent(pai, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;
        return rect;
    }

    private static void AtribuirRetrato(Image imagem, TeamData time, List<JogadorDados> titulares)
    {
        Sprite sprite = time != null ? time.escudoOuBandeira : null;
        if (titulares != null && titulares.Count > 0 && titulares[0] != null && titulares[0].foto512x512 != null)
            sprite = titulares[0].foto512x512;
        imagem.sprite = sprite;
        imagem.enabled = sprite != null;
    }

    private static string Nome(string nome, List<JogadorDados> titulares, string fallback)
    {
        if (titulares != null && titulares.Count > 0 && titulares[0] != null &&
            !string.IsNullOrWhiteSpace(titulares[0].nomeJogador)) nome = titulares[0].nomeJogador;
        return string.IsNullOrWhiteSpace(nome) ? fallback : nome.Trim().ToUpperInvariant();
    }

    private static string FormatarTempo(float duracao)
    {
        int segundos = float.IsNaN(duracao) || float.IsInfinity(duracao)
            ? 45 : Mathf.CeilToInt(Mathf.Max(0f, duracao));
        return string.Format("{0:00}:{1:00}", segundos / 60, segundos % 60);
    }

    private static Color CorSobre(Color fundo)
    {
        return fundo.r * 0.2126f + fundo.g * 0.7152f + fundo.b * 0.0722f > 0.52f
            ? new Color(0.015f, 0.025f, 0.07f) : Color.white;
    }

    private static Color CorValida(Color cor, Color fallback)
    {
        return cor.a <= 0f ? fallback : cor;
    }

    private static Color ComAlpha(Color cor, float alpha) { cor.a = alpha; return cor; }
    private static float Escala(float valor)
    {
        return float.IsNaN(valor) || float.IsInfinity(valor) ? 1f : Mathf.Clamp(valor, 0.5f, 1.75f);
    }

    private static float PassoTempo()
    {
        float passo = Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;
        return float.IsNaN(passo) || float.IsInfinity(passo) ? 0f : Mathf.Max(0f, passo);
    }

    private void LimparPontos()
    {
        StopAllCoroutines();
        foreach (GameObject ponto in pontosAtivos)
            if (ponto != null) Destroy(ponto);
        pontosAtivos.Clear();
    }

    private void OnEnable() { if (canvas != null) canvas.gameObject.SetActive(true); }
    private void OnDisable()
    {
        if (eventos != null) eventos.LimparEventos();
        LimparPontos();
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        LimparPontos();
        if (universalUI != null && universalUI.hudPokemon == this) universalUI.hudPokemon = null;
        if (canvas != null) Destroy(canvas.gameObject);
    }
}
