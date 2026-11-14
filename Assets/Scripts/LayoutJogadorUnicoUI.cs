using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Compact representative cards below the pitch, enlarged for the match introduction.</summary>
[DisallowMultipleComponent]
public sealed class LayoutJogadorUnicoUI : MonoBehaviour
{
    private sealed class Ficha
    {
        public RectTransform retangulo;
        public Vector2 minimoOriginal;
        public Vector2 maximoOriginal;
        public Vector3 escalaOriginal;
        public Image faixa;
        public Image escudo;
        public Image foto;
        public TextMeshProUGUI equipe;
        public TextMeshProUGUI jogador;
        public TextMeshProUGUI semFoto;
        public CanvasGroup grupo;
        public RectTransform scanner;
        public Image luzScanner;
    }

    private Ficha fichaA;
    private Ficha fichaB;
    private TMP_FontAsset fonte;
    private int versaoApresentacao;
    private bool apresentacaoPreparada;
    private bool mostrarScanner = true;
    private float velocidadeScanner = 1f;

    public bool ApresentacaoConcluida { get; private set; }

    public void ConfigurarScanner(bool mostrar, float velocidade)
    {
        mostrarScanner = mostrar;
        velocidadeScanner = Mathf.Clamp(velocidade, .1f, 3f);
        if (!mostrar)
        {
            if (fichaA != null && fichaA.scanner != null) fichaA.scanner.gameObject.SetActive(false);
            if (fichaB != null && fichaB.scanner != null) fichaB.scanner.gameObject.SetActive(false);
        }
    }

    public static LayoutJogadorUnicoUI Criar(Canvas canvas, TMP_FontAsset fonte)
    {
        if (canvas == null) return null;

        RectTransform raiz = CriarRetangulo(
            "Escalacao_JogadorUnico", canvas.transform,
            Vector2.zero, Vector2.one);
        // Existing score, goals and other overlays keep their original order.
        raiz.SetAsFirstSibling();
        LayoutJogadorUnicoUI layout =
            raiz.gameObject.AddComponent<LayoutJogadorUnicoUI>();
        layout.fonte = fonte != null ? fonte : TMP_Settings.defaultFontAsset;
        layout.fichaA = layout.CriarFicha(
            "Time_A", new Vector2(0.015f, 0.012f), new Vector2(0.151f, 0.175f));
        layout.fichaB = layout.CriarFicha(
            "Time_B", new Vector2(0.849f, 0.012f), new Vector2(0.985f, 0.175f));
        raiz.gameObject.SetActive(false);
        return layout;
    }

    public void Configurar(
        TeamData timeA, TeamData timeB,
        List<JogadorDados> titularesA, List<JogadorDados> titularesB)
    {
        CancelarApresentacao();
        Preencher(fichaA, timeA, PrimeiroValido(titularesA), "TEAM A");
        Preencher(fichaB, timeB, PrimeiroValido(titularesB), "TEAM B");
        gameObject.SetActive(true);
    }

    public bool PrepararApresentacao()
    {
        CancelarApresentacao();
        if (!isActiveAndEnabled || fichaA == null || fichaB == null ||
            fichaA.retangulo == null || fichaB.retangulo == null)
            return false;

        apresentacaoPreparada = true;
        MaterializarFicha(fichaA, new Vector2(0.31f, 0.5f), 0f, 0f);
        MaterializarFicha(fichaB, new Vector2(0.69f, 0.5f), 0f, 0f);
        return true;
    }

    public IEnumerator ExibirApresentacao(float tempoLeitura, float tempoDeslocamento)
    {
        if (!apresentacaoPreparada || !isActiveAndEnabled)
            yield break;

        int minhaVersao;
        unchecked { minhaVersao = ++versaoApresentacao; }
        ApresentacaoConcluida = false;
        float leitura = DuracaoValida(tempoLeitura);
        float deslocamento = DuracaoValida(tempoDeslocamento);
        double tempo = 0d;

        while (SequenciaAtual(minhaVersao) && tempo + 0.000001d < leitura)
        {
            float carga = Mathf.Clamp01((float)tempo / Mathf.Max(0.05f, Mathf.Min(0.6f, leitura)));
            MaterializarFicha(fichaA, new Vector2(0.31f, 0.5f), carga, (float)tempo);
            MaterializarFicha(fichaB, new Vector2(0.69f, 0.5f), carga, (float)tempo + 0.3f);
            yield return null;
            tempo += PassoTempo();
        }

        if (!SequenciaAtual(minhaVersao)) yield break;
        fichaA.grupo.alpha = fichaB.grupo.alpha = 1f;
        fichaA.scanner.gameObject.SetActive(false);
        fichaB.scanner.gameObject.SetActive(false);
        tempo = 0d;
        while (SequenciaAtual(minhaVersao) && tempo + 0.000001d < deslocamento)
        {
            float progresso = Mathf.SmoothStep(0f, 1f, (float)tempo / deslocamento);
            PosicionarFicha(fichaA, new Vector2(0.31f, 0.5f), progresso);
            PosicionarFicha(fichaB, new Vector2(0.69f, 0.5f), progresso);
            yield return null;
            tempo += PassoTempo();
        }

        if (!SequenciaAtual(minhaVersao)) yield break;
        RestaurarFicha(fichaA);
        RestaurarFicha(fichaB);
        apresentacaoPreparada = false;
        ApresentacaoConcluida = true;
    }

    public void CancelarApresentacao()
    {
        unchecked { versaoApresentacao++; }
        apresentacaoPreparada = false;
        ApresentacaoConcluida = false;
        RestaurarFicha(fichaA);
        RestaurarFicha(fichaB);
    }

    private void OnDisable()
    {
        CancelarApresentacao();
    }

    private bool SequenciaAtual(int minhaVersao)
    {
        return this != null && isActiveAndEnabled && apresentacaoPreparada &&
            minhaVersao == versaoApresentacao && fichaA != null && fichaB != null &&
            fichaA.retangulo != null && fichaB.retangulo != null;
    }

    private static void PosicionarFicha(Ficha ficha, Vector2 centro, float progresso)
    {
        Vector2 metade = (ficha.maximoOriginal - ficha.minimoOriginal) * 0.5f;
        float arco = Mathf.Sin(Mathf.Clamp01(progresso) * Mathf.PI);
        Vector2 desvio = new Vector2((centro.x < .5f ? -.07f : .07f) * arco, .12f * arco);
        ficha.retangulo.anchorMin = Vector2.Lerp(centro - metade, ficha.minimoOriginal, progresso) + desvio;
        ficha.retangulo.anchorMax = Vector2.Lerp(centro + metade, ficha.maximoOriginal, progresso) + desvio;
        ficha.retangulo.offsetMin = Vector2.zero;
        ficha.retangulo.offsetMax = Vector2.zero;
        ficha.retangulo.localScale = Vector3.Lerp(
            Vector3.Scale(ficha.escalaOriginal, new Vector3(1.65f, 1.65f, 1f)),
            ficha.escalaOriginal, progresso);
    }

    private void MaterializarFicha(Ficha ficha, Vector2 centro, float progresso, float tempo)
    {
        float p = Mathf.SmoothStep(0f, 1f, progresso);
        PosicionarFicha(ficha, centro - new Vector2(0f, .12f * (1f - p)), 0f);
        ficha.retangulo.localScale *= Mathf.Lerp(.72f, 1f, p);
        ficha.grupo.alpha = p;
        float y = .24f + .55f * (.5f + .5f * Mathf.Sin(tempo * velocidadeScanner));
        ficha.scanner.anchorMin = new Vector2(.07f, y);
        ficha.scanner.anchorMax = new Vector2(.93f, y + .008f);
        ficha.scanner.gameObject.SetActive(mostrarScanner);
        Color cor = ficha.luzScanner.color;
        cor.a = .18f + .12f * (.5f + .5f * Mathf.Sin(tempo * velocidadeScanner * 1.2f));
        ficha.luzScanner.color = cor;
    }

    private static void RestaurarFicha(Ficha ficha)
    {
        if (ficha == null || ficha.retangulo == null) return;
        ficha.retangulo.anchorMin = ficha.minimoOriginal;
        ficha.retangulo.anchorMax = ficha.maximoOriginal;
        ficha.retangulo.offsetMin = Vector2.zero;
        ficha.retangulo.offsetMax = Vector2.zero;
        ficha.retangulo.localScale = ficha.escalaOriginal;
        if (ficha.grupo != null) ficha.grupo.alpha = 1f;
        if (ficha.scanner != null) ficha.scanner.gameObject.SetActive(false);
    }

    private static float DuracaoValida(float segundos)
    {
        return float.IsNaN(segundos) || float.IsInfinity(segundos) ? 0f : Mathf.Max(0f, segundos);
    }

    private static float PassoTempo()
    {
        float passo = Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;
        return float.IsNaN(passo) || float.IsInfinity(passo) ? 0f : Mathf.Max(0f, passo);
    }

    private Ficha CriarFicha(string nome, Vector2 minimo, Vector2 maximo)
    {
        RectTransform bloco = CriarRetangulo(nome, transform, minimo, maximo);
        Image fundo = bloco.gameObject.AddComponent<Image>();
        fundo.color = new Color(0.025f, 0.045f, 0.09f, 0.94f);
        fundo.raycastTarget = false;

        Ficha ficha = new Ficha();
        ficha.retangulo = bloco;
        ficha.grupo = bloco.gameObject.AddComponent<CanvasGroup>();
        ficha.minimoOriginal = minimo;
        ficha.maximoOriginal = maximo;
        ficha.escalaOriginal = bloco.localScale;
        ficha.faixa = CriarImagem("Cor_Time", bloco,
            new Vector2(0f, 0.985f), Vector2.one);
        Image linha = CriarImagem("Divisor", bloco,
            new Vector2(0.07f, 0.83f), new Vector2(0.93f, 0.833f));
        linha.color = new Color(1f, 1f, 1f, 0.14f);

        ficha.escudo = CriarImagem("Escudo", bloco,
            new Vector2(0.07f, 0.855f), new Vector2(0.25f, 0.965f));
        ficha.equipe = CriarTexto("Equipe", bloco,
            new Vector2(0.30f, 0.85f), new Vector2(0.93f, 0.97f), 20.8f);
        ficha.equipe.alignment = TextAlignmentOptions.MidlineLeft;

        Image moldura = CriarImagem("Fundo_Foto", bloco,
            new Vector2(0.07f, 0.24f), new Vector2(0.93f, 0.81f));
        moldura.color = new Color(0.07f, 0.105f, 0.17f, 1f);
        ficha.semFoto = CriarTexto("Sem_Foto", bloco,
            new Vector2(0.09f, 0.28f), new Vector2(0.91f, 0.77f), 46.4f);
        ficha.semFoto.richText = true;
        ficha.semFoto.color = new Color(0.58f, 0.66f, 0.80f, 1f);
        ficha.foto = CriarImagem("Foto_Jogador", bloco,
            new Vector2(0.07f, 0.24f), new Vector2(0.93f, 0.81f));
        Image cantoA = CriarImagem("Holo_Corner_Left", bloco, new Vector2(.015f, .27f), new Vector2(.025f, .76f));
        Image cantoB = CriarImagem("Holo_Corner_Right", bloco, new Vector2(.975f, .27f), new Vector2(.985f, .76f));
        cantoA.color = cantoB.color = new Color(.25f, .93f, .92f, .55f);
        ficha.luzScanner = CriarImagem("Holo_Scanner", bloco, new Vector2(.07f, .24f), new Vector2(.93f, .248f));
        ficha.scanner = ficha.luzScanner.rectTransform;
        ficha.luzScanner.color = new Color(.25f, .93f, .92f, .24f);
        // Keep the scan above the photo frame but behind the portrait/fallback text.
        ficha.scanner.SetSiblingIndex(ficha.semFoto.transform.GetSiblingIndex());
        ficha.scanner.gameObject.SetActive(false);

        TextMeshProUGUI legenda = CriarTexto("Legenda", bloco,
            new Vector2(0.07f, 0.181f), new Vector2(0.93f, 0.23f), 12.8f);
        legenda.text = "CAPTAIN";
        legenda.characterSpacing = 2.4f;
        legenda.fontStyle = FontStyles.Normal;
        legenda.color = new Color(0.58f, 0.66f, 0.80f, 1f);
        ficha.jogador = CriarTexto("Nome_Jogador", bloco,
            new Vector2(0.07f, 0.035f), new Vector2(0.93f, 0.177f), 27.2f);
        return ficha;
    }

    private void Preencher(Ficha ficha, TeamData time, JogadorDados jogador, string padrao)
    {
        if (ficha == null) return;

        string sigla = time != null && !string.IsNullOrWhiteSpace(time.sigla)
            ? time.sigla.Trim() : padrao;
        ficha.equipe.text = time != null && !string.IsNullOrWhiteSpace(time.nomeEquipe)
            ? time.nomeEquipe.Trim() : sigla;
        ficha.faixa.color = time != null ? CorVisivel(time.corPrincipal) : Color.white;

        ficha.escudo.sprite = time != null ? time.escudoOuBandeira : null;
        ficha.escudo.enabled = ficha.escudo.sprite != null;
        ficha.foto.sprite = jogador != null ? jogador.foto512x512 : null;
        ficha.foto.enabled = ficha.foto.sprite != null;
        ficha.semFoto.text = sigla.Replace("<", "‹").Replace(">", "›")
            + "\n<size=28%>NO PHOTO</size>";
        ficha.semFoto.enabled = ficha.foto.sprite == null;
        ficha.jogador.text = jogador != null && !string.IsNullOrWhiteSpace(jogador.nomeJogador)
            ? jogador.nomeJogador.Trim() : "PLAYER NOT SET";
    }

    private static Color CorVisivel(Color cor)
    {
        cor.a = 1f;
        // A black kit still needs a visible accent on the dark card.
        return cor.maxColorComponent < 0.2f
            ? Color.Lerp(cor, Color.white, 0.45f) : cor;
    }

    private static JogadorDados PrimeiroValido(List<JogadorDados> jogadores)
    {
        if (jogadores == null) return null;
        foreach (JogadorDados jogador in jogadores)
            if (jogador != null) return jogador;
        return null;
    }

    private static RectTransform CriarRetangulo(
        string nome, Transform pai, Vector2 minimo, Vector2 maximo)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform));
        objeto.layer = pai.gameObject.layer;
        RectTransform retangulo = objeto.GetComponent<RectTransform>();
        retangulo.SetParent(pai, false);
        retangulo.anchorMin = minimo;
        retangulo.anchorMax = maximo;
        retangulo.offsetMin = Vector2.zero;
        retangulo.offsetMax = Vector2.zero;
        return retangulo;
    }

    private static Image CriarImagem(
        string nome, Transform pai, Vector2 minimo, Vector2 maximo)
    {
        Image imagem = CriarRetangulo(nome, pai, minimo, maximo)
            .gameObject.AddComponent<Image>();
        imagem.raycastTarget = false;
        imagem.preserveAspect = true;
        return imagem;
    }

    private TextMeshProUGUI CriarTexto(
        string nome, Transform pai, Vector2 minimo, Vector2 maximo, float tamanho)
    {
        TextMeshProUGUI texto = CriarRetangulo(nome, pai, minimo, maximo)
            .gameObject.AddComponent<TextMeshProUGUI>();
        if (fonte != null) texto.font = fonte;
        texto.fontSize = tamanho;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = 8f;
        texto.fontSizeMax = tamanho;
        texto.fontStyle = FontStyles.Bold;
        texto.alignment = TextAlignmentOptions.Center;
        texto.overflowMode = TextOverflowModes.Ellipsis;
        texto.textWrappingMode = TextWrappingModes.Normal;
        texto.richText = false;
        texto.color = Color.white;
        texto.raycastTarget = false;
        texto.margin = Vector4.zero;
        return texto;
    }
}
