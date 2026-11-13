using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ClassificacaoUI : MonoBehaviour
{
    [Header("Identidade")]
    public string nomeDaLiga = "Channel League";
    public Sprite logo;
    public Sprite logoCanal;
    public TMP_FontAsset fonte;

    [Header("Tempos de leitura")]
    [Min(0f)]
    public float tempoLeituraAbertura = 15f;

    [Min(0f)]
    public float tempoLeituraEncerramento = 20f;

    [Header("Animação")]
    [Min(0f)]
    public float tempoEntradaAbertura = 3f;

    [Min(0f)]
    public float tempoEntradaEncerramento = 2f;

    [Min(0f)]
    public float tempoSaida = 1f;

    public bool mostrarVariacaoNoFinal = true;
    public bool manterTabelaFinalNaTela = true;

    [Header("Cores")]
    public Color corFundo =
        new Color(0.025f, 0.045f, 0.085f);

    public Color corPainel =
        new Color(0.055f, 0.095f, 0.17f);

    public Color textoClaro =
        new Color(0.94f, 0.97f, 1f);

    public Color textoDiscreto =
        new Color(0.64f, 0.74f, 0.86f);

    [InspectorName("Cor Destaque")]
    public Color verde =
        new Color(0.64f, 0.93f, 0.51f);

    public bool EstaExibindo { get; private set; }
    public bool UltimaExibicaoConcluida { get; private set; }

    private class ItemAnimado
    {
        public RectTransform rect;
        public CanvasGroup grupo;
        public Vector2 destino;
        public float atraso;
    }

    private readonly List<ItemAnimado> itens =
        new List<ItemAnimado>();

    private GameObject raiz;
    private RectTransform painel;
    private CanvasGroup grupoPainel;
    private int versao;

    public IEnumerator ExibirTabela(
        List<LigaCampeonato.Linha> tabela,
        List<LigaCampeonato.Linha> anterior,
        List<TeamData> clubes,
        int rodada,
        bool encerramento,
        List<LigaCampeonato.Jogo> jogos = null)
    {
        Ocultar();
        int exibicao = versao;

        if (!isActiveAndEnabled)
        {
            Debug.LogError(
                "Ative o componente ClassificacaoUI.",
                this);

            yield break;
        }

        TMP_FontAsset fonteUsada =
            fonte != null
                ? fonte
                : TMP_Settings.defaultFontAsset;

        if (tabela == null ||
            tabela.Count == 0 ||
            fonteUsada == null)
        {
            Debug.LogError(
                "ClassificacaoUI: confira os dados e a fonte TMP.",
                this);

            yield break;
        }

        Construir(
            tabela,
            anterior,
            clubes,
            rodada,
            encerramento,
            jogos,
            fonteUsada);

        EstaExibindo = true;
        raiz.SetActive(true);

        AjustarEnquadramento();

        float entrada = encerramento
            ? tempoEntradaEncerramento
            : tempoEntradaAbertura;

        float leitura = encerramento
            ? tempoLeituraEncerramento
            : tempoLeituraAbertura;

        yield return AnimarEntrada(
            Mathf.Max(0f, entrada),
            exibicao);

        if (!Valida(exibicao))
            yield break;

        // A leitura começa somente após a entrada completa.
        double tempo = 0d;

        while (tempo + 0.000001d < Mathf.Max(0f, leitura))
        {
            if (!Valida(exibicao))
                yield break;

            yield return null;
            tempo += PassoTempo();
        }

        if (!encerramento || !manterTabelaFinalNaTela)
        {
            yield return AnimarSaida(
                Mathf.Max(0f, tempoSaida),
                exibicao);
        }

        if (!Valida(exibicao))
            yield break;

        // O fundo permanece opaco, escondendo a tela antiga.
        // No encerramento, a tabela também permanece visível.
        if (encerramento && manterTabelaFinalNaTela)
        {
            grupoPainel.alpha = 1f;
        }

        EstaExibindo = false;
        UltimaExibicaoConcluida = true;
    }

    public void Ocultar()
    {
        versao++;

        EstaExibindo = false;
        UltimaExibicaoConcluida = false;

        if (raiz != null)
        {
            raiz.SetActive(false);
        }
    }

    private bool Valida(int exibicao)
    {
        return exibicao == versao &&
            EstaExibindo &&
            isActiveAndEnabled &&
            raiz != null;
    }

    private IEnumerator AnimarEntrada(
        float duracao,
        int exibicao)
    {
        double tempo = 0d;

        while (tempo + 0.000001d < duracao)
        {
            if (!Valida(exibicao))
                yield break;

            tempo += PassoTempo();

            AplicarEntrada(
                Mathf.Clamp01((float)(tempo / duracao)));

            yield return null;
        }

        if (Valida(exibicao))
        {
            AplicarEntrada(1f);
        }
    }

    private void AplicarEntrada(float progresso)
    {
        grupoPainel.alpha = Mathf.SmoothStep(
            0f,
            1f,
            Mathf.Clamp01(progresso / 0.2f));

        foreach (ItemAnimado item in itens)
        {
            float p = Mathf.Clamp01(
                (progresso - item.atraso) / 0.3f);

            float suave = Mathf.SmoothStep(
                0f,
                1f,
                p);

            item.grupo.alpha = suave;

            item.rect.anchoredPosition =
                item.destino +
                new Vector2(-35f * (1f - suave), 0f);
        }
    }

    private IEnumerator AnimarSaida(
        float duracao,
        int exibicao)
    {
        double tempo = 0d;

        while (tempo + 0.000001d < duracao)
        {
            if (!Valida(exibicao))
                yield break;

            tempo += PassoTempo();

            grupoPainel.alpha =
                1f - Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01((float)(tempo / duracao)));

            yield return null;
        }

        if (Valida(exibicao))
        {
            grupoPainel.alpha = 0f;
        }
    }

    private static float PassoTempo()
    {
        float passo = Time.captureDeltaTime > 0f
            ? Time.captureDeltaTime
            : Time.unscaledDeltaTime;
        return float.IsNaN(passo) || float.IsInfinity(passo)
            ? 0f : Mathf.Max(0f, passo);
    }

    private void AdicionarAnimacao(
        RectTransform rect,
        int indice,
        int quantidade)
    {
        CanvasGroup grupo =
            rect.gameObject.AddComponent<CanvasGroup>();

        grupo.alpha = 0f;

        itens.Add(new ItemAnimado
        {
            rect = rect,
            grupo = grupo,
            destino = rect.anchoredPosition,
            atraso =
                0.12f +
                0.5f * indice / Mathf.Max(1, quantidade - 1)
        });
    }

    private void Construir(
        List<LigaCampeonato.Linha> tabela,
        List<LigaCampeonato.Linha> anterior,
        List<TeamData> clubes,
        int rodada,
        bool encerramento,
        List<LigaCampeonato.Jogo> jogos,
        TMP_FontAsset fonteUsada)
    {
        if (raiz != null)
        {
            raiz.SetActive(false);
            Destroy(raiz);
        }

        itens.Clear();

        raiz = new GameObject(
            "Standings - Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        raiz.layer = 5;

        Canvas canvas =
            raiz.GetComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.overrideSorting = true;
        canvas.sortingOrder = 300;

        CanvasScaler scaler =
            raiz.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920f, 1080f);

        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        scaler.matchWidthOrHeight = 0.5f;

        // O fundo fica fora do CanvasGroup animado.
        Image fundo = raiz.AddComponent<Image>();

        Color opaco = corFundo;
        opaco.a = 1f;

        fundo.color = opaco;
        fundo.raycastTarget = true;

        painel = Retangulo(
            raiz.transform,
            "Panel",
            0f,
            0f,
            1720f,
            960f);

        painel.anchorMin = painel.anchorMax =
            new Vector2(0.5f, 0.5f);

        painel.pivot =
            new Vector2(0.5f, 0.5f);

        painel.anchoredPosition =
            Vector2.zero;

        painel.gameObject.AddComponent<Image>().color =
            corPainel;

        grupoPainel =
            painel.gameObject.AddComponent<CanvasGroup>();

        grupoPainel.alpha = 0f;
        grupoPainel.interactable = false;
        grupoPainel.blocksRaycasts = true;

        ColocarEscudo(
            painel,
            "League Logo",
            logo,
            40f,
            25f,
            88f);

        ColocarEscudo(
            painel,
            "Channel Logo",
            logoCanal,
            1560f,
            25f,
            88f,
            120f);

        Texto(
            painel,
            "League Name",
            nomeDaLiga,
            150f,
            27f,
            1320f,
            36f,
            26f,
            textoDiscreto,
            fonteUsada);

        Texto(
            painel,
            "Title",
            "STANDINGS",
            150f,
            69f,
            1320f,
            48f,
            35f,
            textoClaro,
            fonteUsada,
            true);

        Imagem(
            painel,
            "Accent",
            40f,
            128f,
            1640f,
            3f,
            verde);

        RectTransform cabecalho = Retangulo(
            painel,
            "Standings Header",
            40f,
            142f,
            1070f,
            38f);

        Celulas(
            cabecalho,
            new[]
            {
                "POS", "TEAM", "PTS",
                "P", "W", "D", "L", "GD"
            },
            19f,
            fonteUsada,
            true);

        Texto(
            painel,
            "Fixtures Title",
            encerramento
                ? "RESULTS"
                : "FIXTURES",
            1140f,
            142f,
            540f,
            38f,
            23f,
            verde,
            fonteUsada,
            true);

        var escudos =
            new Dictionary<string, Sprite>();

        if (clubes != null)
        {
            foreach (TeamData clube in clubes)
            {
                if (clube == null)
                    continue;

                string id = clube.name.Trim();

                if (!escudos.ContainsKey(id))
                {
                    escudos.Add(
                        id,
                        clube.escudoOuBandeira);
                }
            }
        }

        var posicoesAnteriores =
            new Dictionary<string, int>();

        bool haviaJogos = false;

        if (anterior != null)
        {
            foreach (var linha in anterior)
            {
                if (linha == null || linha.id == null)
                    continue;

                posicoesAnteriores[linha.id] =
                    linha.POS;

                if (linha.P > 0)
                {
                    haviaJogos = true;
                }
            }
        }

        bool temJogos =
            tabela.Exists(t => t != null && t.P > 0);

        float passo = Mathf.Min(
            34f,
            680f / Mathf.Max(1, tabela.Count));

        float altura = Mathf.Max(
            16f,
            passo - 2f);

        float tamanho = Mathf.Clamp(
            passo * 0.67f,
            14f,
            23f);

        for (int i = 0; i < tabela.Count; i++)
        {
            LigaCampeonato.Linha linha = tabela[i];

            if (linha == null)
                continue;

            RectTransform rt = Retangulo(
                painel,
                "Team " + i,
                40f,
                188f + i * passo,
                1070f,
                altura);

            bool lider =
                temJogos && linha.POS == 1;

            rt.gameObject.AddComponent<Image>().color =
                lider
                    ? Color.Lerp(
                        corPainel,
                        verde,
                        0.15f)
                    : Color.Lerp(
                        corPainel,
                        corFundo,
                        i % 2 == 0 ? 0.25f : 0.5f);

            if (lider)
            {
                Imagem(
                    rt,
                    "Leader",
                    0f,
                    0f,
                    3f,
                    altura,
                    verde);
            }

            Celulas(
                rt,
                new[]
                {
                    linha.POS.ToString(),
                    linha.TEAM,
                    linha.PTS.ToString(),
                    linha.P.ToString(),
                    linha.W.ToString(),
                    linha.D.ToString(),
                    linha.L.ToString(),
                    linha.GD.ToString()
                },
                tamanho,
                fonteUsada,
                false);

            ColocarEscudo(
                rt,
                "Badge",
                BuscarEscudo(escudos, linha.id),
                57f,
                1f,
                altura - 2f);

            int antes;

            if (encerramento &&
                mostrarVariacaoNoFinal &&
                haviaJogos &&
                linha.id != null &&
                posicoesAnteriores.TryGetValue(
                    linha.id,
                    out antes))
            {
                int diferenca =
                    antes - linha.POS;

                if (diferenca != 0)
                {
                    Texto(
                        rt,
                        "Position Change",
                        diferenca > 0
                            ? "↑" + diferenca
                            : "↓" + (-diferenca),
                        996f,
                        0f,
                        68f,
                        altura,
                        tamanho * 0.8f,
                        diferenca > 0
                            ? verde
                            : new Color(1f, 0.45f, 0.43f),
                        fonteUsada);
                }
            }

            AdicionarAnimacao(
                rt,
                i,
                tabela.Count);
        }

        ConstruirJogos(
            jogos,
            escudos,
            encerramento,
            fonteUsada);

        Imagem(
            painel,
            "Footer Divider",
            40f,
            898f,
            1640f,
            1f,
            textoDiscreto * new Color(1f, 1f, 1f, 0.3f));

        Texto(
            painel,
            "Footer",
            encerramento
                ? $"AFTER MATCHDAY {rodada}"
                : $"BEFORE MATCHDAY {rodada}",
            40f,
            913f,
            1640f,
            30f,
            21f,
            textoDiscreto,
            fonteUsada);
    }

    private void ConstruirJogos(
        List<LigaCampeonato.Jogo> jogos,
        Dictionary<string, Sprite> escudos,
        bool encerramento,
        TMP_FontAsset fonteUsada)
    {
        if (jogos == null || jogos.Count == 0)
        {
            Texto(
                painel,
                "No Fixtures",
                "No fixtures available.",
                1140f,
                210f,
                540f,
                50f,
                22f,
                textoDiscreto,
                fonteUsada);

            return;
        }

        float passo = Mathf.Min(
            68f,
            680f / jogos.Count);

        float altura = passo - 5f;

        // Dense rounds keep both team rows readable without overlapping the
        // next fixture. The small match-number caption is omitted above 12 games.
        bool compacto = jogos.Count > 12;
        float topoTime = compacto ? 1f : 15f;
        float alturaTime = Mathf.Max(
            10f,
            (altura - (compacto ? 2f : 16f)) / 2f);

        float tamanho = Mathf.Clamp(
            alturaTime * 0.85f,
            13f,
            20f);

        for (int i = 0; i < jogos.Count; i++)
        {
            LigaCampeonato.Jogo jogo = jogos[i];

            if (jogo == null)
                continue;

            RectTransform rt = Retangulo(
                painel,
                "Match " + (i + 1),
                1140f,
                188f + i * passo,
                540f,
                altura);

            rt.gameObject.AddComponent<Image>().color =
                Color.Lerp(
                    corPainel,
                    corFundo,
                    0.55f);

            if (!compacto)
            {
                Texto(
                    rt,
                    "Match Number",
                    "MATCH " + (i + 1).ToString("D2"),
                    12f,
                    0f,
                    500f,
                    15f,
                    11f,
                    textoDiscreto,
                    fonteUsada);
            }

            bool exibirPlacar = encerramento;

            string placarA = jogo.registrado
                ? jogo.golsA.ToString()
                : "—";

            string placarB = jogo.registrado
                ? jogo.golsB.ToString()
                : "—";

            float larguraNome =
                exibirPlacar ? 420f : 482f;

            ColocarEscudo(
                rt,
                "Home Badge",
                BuscarEscudo(escudos, jogo.a),
                12f,
                topoTime + 1f,
                Mathf.Min(20f, alturaTime - 1f));

            ColocarEscudo(
                rt,
                "Away Badge",
                BuscarEscudo(escudos, jogo.b),
                12f,
                topoTime + 1f + alturaTime,
                Mathf.Min(20f, alturaTime - 1f));

            Texto(
                rt,
                "Home Team",
                jogo.nomeA,
                42f,
                topoTime,
                larguraNome,
                alturaTime,
                tamanho,
                textoClaro,
                fonteUsada);

            Texto(
                rt,
                "Away Team",
                jogo.nomeB,
                42f,
                topoTime + alturaTime,
                larguraNome,
                alturaTime,
                tamanho,
                textoClaro,
                fonteUsada);

            // A abertura nunca revela placares.
            if (exibirPlacar)
            {
                Texto(
                    rt,
                    "Home Goals",
                    placarA,
                    482f,
                    topoTime,
                    45f,
                    alturaTime,
                    tamanho + 1f,
                    verde,
                    fonteUsada,
                    true)
                    .alignment = TextAlignmentOptions.Center;

                Texto(
                    rt,
                    "Away Goals",
                    placarB,
                    482f,
                    topoTime + alturaTime,
                    45f,
                    alturaTime,
                    tamanho + 1f,
                    verde,
                    fonteUsada,
                    true)
                    .alignment = TextAlignmentOptions.Center;
            }

            AdicionarAnimacao(
                rt,
                i,
                jogos.Count);
        }
    }

    private void Celulas(
        Transform pai,
        string[] valores,
        float tamanho,
        TMP_FontAsset fonteUsada,
        bool cabecalho)
    {
        float[] x =
        {
            3f, 92f, 470f, 570f,
            650f, 730f, 810f, 900f
        };

        float[] largura =
        {
            48f, 355f, 74f, 58f,
            58f, 58f, 58f, 80f
        };

        float altura =
            ((RectTransform)pai).sizeDelta.y;

        for (int i = 0; i < valores.Length; i++)
        {
            TextMeshProUGUI texto = Texto(
                pai,
                "Column " + i,
                valores[i],
                x[i],
                0f,
                largura[i],
                altura,
                tamanho,
                i == 2
                    ? verde
                    : cabecalho
                        ? textoDiscreto
                        : textoClaro,
                fonteUsada,
                cabecalho || i == 2);

            texto.alignment = i == 1
                ? TextAlignmentOptions.Left
                : TextAlignmentOptions.Center;
        }
    }

    private static Sprite BuscarEscudo(
        Dictionary<string, Sprite> escudos,
        string id)
    {
        Sprite sprite;

        return id != null &&
            escudos.TryGetValue(id, out sprite)
                ? sprite
                : null;
    }

    private void AjustarEnquadramento()
    {
        Canvas.ForceUpdateCanvases();

        Rect rect =
            ((RectTransform)raiz.transform).rect;

        float escala = Mathf.Min(
            Mathf.Max(1f, rect.width - 80f) / 1720f,
            Mathf.Max(1f, rect.height - 60f) / 960f);

        painel.localScale =
            Vector3.one * escala;
    }

    private static RectTransform Retangulo(
        Transform pai,
        string nome,
        float x,
        float y,
        float largura,
        float altura)
    {
        GameObject go =
            new GameObject(
                nome,
                typeof(RectTransform));

        go.layer = 5;

        RectTransform rt =
            go.GetComponent<RectTransform>();

        rt.SetParent(pai, false);

        rt.anchorMin = rt.anchorMax =
            new Vector2(0f, 1f);

        rt.pivot =
            new Vector2(0f, 1f);

        rt.anchoredPosition =
            new Vector2(x, -y);

        rt.sizeDelta =
            new Vector2(largura, altura);

        return rt;
    }

    private static Image Imagem(
        Transform pai,
        string nome,
        float x,
        float y,
        float largura,
        float altura,
        Color cor)
    {
        Image imagem = Retangulo(
            pai,
            nome,
            x,
            y,
            largura,
            altura)
            .gameObject.AddComponent<Image>();

        imagem.color = cor;
        imagem.raycastTarget = false;

        return imagem;
    }

    private static void ColocarEscudo(
        Transform pai,
        string nome,
        Sprite sprite,
        float x,
        float y,
        float altura,
        float largura = -1f)
    {
        Image imagem = Imagem(
            pai,
            nome,
            x,
            y,
            largura > 0f ? largura : altura,
            altura,
            Color.white);

        imagem.sprite = sprite;
        imagem.preserveAspect = true;
        imagem.enabled = sprite != null;
    }

    private static TextMeshProUGUI Texto(
        Transform pai,
        string nome,
        string valor,
        float x,
        float y,
        float largura,
        float altura,
        float tamanho,
        Color cor,
        TMP_FontAsset fonteUsada,
        bool negrito = false)
    {
        TextMeshProUGUI texto = Retangulo(
            pai,
            nome,
            x,
            y,
            largura,
            altura)
            .gameObject.AddComponent<TextMeshProUGUI>();

        texto.font = fonteUsada;
        texto.text = valor ?? "";
        texto.fontSize = tamanho;

        texto.fontStyle =
            negrito
                ? FontStyles.Bold
                : FontStyles.Normal;

        texto.color = cor;
        texto.alignment = TextAlignmentOptions.Left;

        texto.enableAutoSizing = false;
        texto.textWrappingMode = TextWrappingModes.NoWrap;
        texto.overflowMode = TextOverflowModes.Ellipsis;
        texto.richText = false;
        texto.raycastTarget = false;

        return texto;
    }

    void OnDisable()
    {
        Ocultar();
    }

    void OnDestroy()
    {
        Ocultar();

        if (raiz != null)
        {
            Destroy(raiz);
        }
    }
}