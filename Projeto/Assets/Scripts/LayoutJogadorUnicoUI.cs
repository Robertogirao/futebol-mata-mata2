using System.Collections;
using System.Collections.Generic;
using FutebolDeDiscos;
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
        public Image carga;
        public RectTransform painelEspecial;
        public Image fundoEspecial;
        public float tempoEsperaEspecial;
        public Image brilhoCarga;
        public Image[] marcadoresCarga;
        public RectTransform indicadorCarga;
        public TextMeshProUGUI porcentagemCarga;
        public TextMeshProUGUI estadoCarga;
        public float progressoCarga;
        public TextMeshProUGUI textoCarga;
        public RectTransform especial;
        public RoletaCapitaoUI roleta;
        public RectTransform ponteiro;
        public TextMeshProUGUI resultado;
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
        bool esquerda = minimo.x < .5f;
        // The horizontal cards occupy the lower wings, leaving the custom footer centered.
        const float larguraPainel = 1.84f;
        float inicio = esquerda ? 1.04f : -larguraPainel - .04f;
        float conteudoMin = esquerda ? .045f : .335f;
        float conteudoMax = esquerda ? .665f : .955f;
        float rodaMin = esquerda ? .705f : .035f;
        float rodaMax = esquerda ? .965f : .295f;
        ficha.painelEspecial = CriarRetangulo("Captain_Special_Panel", bloco,
            new Vector2(inicio, .015f), new Vector2(inicio + larguraPainel, .835f));
        ficha.fundoEspecial = ficha.painelEspecial.gameObject.AddComponent<Image>();
        ficha.fundoEspecial.color = new Color(.018f, .028f, .055f, 1f);
        ficha.fundoEspecial.raycastTarget = false;
        Image borda = CriarImagem("Panel_Accent", ficha.painelEspecial,
            new Vector2(0f, .98f), Vector2.one);
        borda.color = new Color(.28f, .85f, .94f, 1f);
        Image basePainel = CriarImagem("Panel_Base", ficha.painelEspecial,
            Vector2.zero, new Vector2(1f, .015f));
        basePainel.color = new Color(.28f, .85f, .94f, .32f);
        float divisorX = esquerda ? .685f : .315f;
        Image divisor = CriarImagem("Panel_Divider", ficha.painelEspecial,
            new Vector2(divisorX, .10f), new Vector2(divisorX + .002f, .90f));
        divisor.color = new Color(.42f, .67f, .77f, .3f);
        TextMeshProUGUI tituloEspecial = CriarTexto("Power_Label", ficha.painelEspecial,
            new Vector2(conteudoMin, .81f), new Vector2(conteudoMax, .96f), 16f);
        tituloEspecial.text = "CAPTAIN POWER";
        tituloEspecial.characterSpacing = 2f;
        tituloEspecial.color = new Color(.62f, .76f, .85f, 1f);
        ficha.textoCarga = CriarTexto("Touches", ficha.painelEspecial,
            new Vector2(conteudoMin, .60f), new Vector2(conteudoMax, .81f), 24f);
        Image fundoCarga = CriarImagem("Charge_Background", ficha.painelEspecial,
            new Vector2(conteudoMin, .43f), new Vector2(conteudoMax, .56f));
        fundoCarga.color = new Color(.11f, .15f, .22f, 1f);
        ficha.carga = CriarImagem("Charge_Fill", fundoCarga.transform, Vector2.zero, Vector2.one);
        ficha.carga.preserveAspect = false;
        ficha.carga.color = new Color(.2f, .95f, .7f, 1f);
        ficha.brilhoCarga = CriarImagem("Charge_Shimmer", ficha.carga.transform,
            new Vector2(0f, .05f), new Vector2(.15f, .95f));
        ficha.brilhoCarga.color = new Color(1f, 1f, 1f, .35f);
        RectTransform etapas = CriarRetangulo("Charge_Steps", ficha.painelEspecial,
            new Vector2(conteudoMin, .30f), new Vector2(conteudoMax, .37f));
        ficha.marcadoresCarga = new Image[PartidaDiscos.ToquesParaEspecial];
        for (int i = 0; i < ficha.marcadoresCarga.Length; i++)
        {
            float x = i / (float)ficha.marcadoresCarga.Length;
            float fim = (i + 1f) / ficha.marcadoresCarga.Length - .015f;
            ficha.marcadoresCarga[i] = CriarImagem("Charge_Step_" + (i + 1), etapas,
                new Vector2(x, 0f), new Vector2(fim, 1f));
            ficha.marcadoresCarga[i].color = new Color(.12f, .18f, .25f, 1f);
        }
        ficha.estadoCarga = CriarTexto("Charge_State", ficha.painelEspecial,
            new Vector2(conteudoMin, .055f), new Vector2(conteudoMax, .24f), 16f);
        ficha.estadoCarga.color = new Color(.55f, .68f, .77f, 1f);
        ficha.indicadorCarga = CriarRetangulo("Charge_Indicator", ficha.painelEspecial,
            new Vector2(rodaMin, .12f), new Vector2(rodaMax, .95f));
        Image fundoIndicador = ficha.indicadorCarga.gameObject.AddComponent<Image>();
        fundoIndicador.color = new Color(.045f, .08f, .125f, 1f);
        fundoIndicador.raycastTarget = false;
        Image acentoIndicador = CriarImagem("Indicator_Accent", ficha.indicadorCarga,
            new Vector2(.08f, .94f), new Vector2(.92f, .96f));
        acentoIndicador.color = new Color(.28f, .85f, .94f, .6f);
        ficha.porcentagemCarga = CriarTexto("Charge_Percent", ficha.indicadorCarga,
            new Vector2(.04f, .28f), new Vector2(.96f, .80f), 38f);
        TextMeshProUGUI legendaCarga = CriarTexto("Charge_Caption", ficha.indicadorCarga,
            new Vector2(.04f, .11f), new Vector2(.96f, .28f), 12f);
        legendaCarga.text = "CHARGE";
        legendaCarga.characterSpacing = 2f;
        legendaCarga.color = new Color(.55f, .68f, .77f, 1f);
        ficha.especial = CriarRetangulo("Roulette", ficha.painelEspecial, Vector2.zero, Vector2.one);
        RectTransform disco = CriarRetangulo("Roulette_Wheel", ficha.especial,
            new Vector2(rodaMin, .12f), new Vector2(rodaMax, .95f));
        // Use the native Image lifecycle with an explicit renderer for the generated wheel.
        disco.gameObject.AddComponent<CanvasRenderer>();
        ficha.roleta = disco.gameObject.AddComponent<RoletaCapitaoUI>();
        ficha.roleta.color = Color.white;
        ficha.roleta.raycastTarget = false;
        ficha.ponteiro = CriarRetangulo("Pointer", disco, Vector2.zero, Vector2.one);
        Image sombraAgulha = CriarImagem("Needle_Shadow", ficha.ponteiro,
            new Vector2(.503f, .49f), new Vector2(.529f, .91f));
        sombraAgulha.color = new Color(.025f, .035f, .055f, 1f);
        Image agulha = CriarImagem("Needle", ficha.ponteiro,
            new Vector2(.488f, .50f), new Vector2(.512f, .91f));
        agulha.color = new Color(1f, .89f, .51f, 1f);
        Image pontaAgulha = CriarImagem("Needle_Tip", ficha.ponteiro,
            new Vector2(.475f, .875f), new Vector2(.525f, .925f));
        pontaAgulha.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        pontaAgulha.color = new Color(1f, .94f, .7f, 1f);
        Image eixoAgulha = CriarImagem("Needle_Cap", ficha.ponteiro,
            new Vector2(.464f, .464f), new Vector2(.536f, .536f));
        eixoAgulha.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        eixoAgulha.color = new Color(1f, .89f, .51f, 1f);
        ficha.resultado = CriarTexto("Result", ficha.especial,
            new Vector2(conteudoMin, .025f), new Vector2(conteudoMax, .25f), 17f);
        ficha.resultado.richText = true;
        ficha.especial.gameObject.SetActive(false);
        ficha.painelEspecial.gameObject.SetActive(false);
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

    private void Update()
    {
        PartidaDiscos partida = PartidaDiscos.Instance;
        AtualizarEspecial(fichaA, partida, true);
        AtualizarEspecial(fichaB, partida, false);
    }

    private void AtualizarEspecial(Ficha ficha, PartidaDiscos partida, bool timeA)
    {
        if (ficha == null) return;
        bool aoVivo = partida != null && partida.EmAndamento && !apresentacaoPreparada;
        ficha.painelEspecial.gameObject.SetActive(aoVivo);
        bool girando = aoVivo && !partida.EmReposicao && partida.RoletaAtiva(timeA);
        bool armado = aoVivo && !partida.EmReposicao && partida.EspecialArmado(timeA);
        ficha.especial.gameObject.SetActive(girando || armado);
        ficha.indicadorCarga.gameObject.SetActive(aoVivo && !girando && !armado);
        ficha.estadoCarga.gameObject.SetActive(aoVivo && !girando && !armado);
        if (!aoVivo)
        {
            ficha.progressoCarga = 0f;
            ficha.tempoEsperaEspecial = 0f;
            ficha.fundoEspecial.color = new Color(.018f, .028f, .055f, 1f);
            return;
        }
        int carga = partida.CargaCapitao(timeA);
        float alvoCarga = carga / (float)PartidaDiscos.ToquesParaEspecial;
        ficha.progressoCarga = Mathf.MoveTowards(ficha.progressoCarga, alvoCarga,
            Mathf.Max(.001f, Time.unscaledDeltaTime) * 2.8f);
        ficha.carga.rectTransform.anchorMax = new Vector2(ficha.progressoCarga, 1f);
        Color corCarga = partida.Configuracao != null ? partida.Configuracao.corBarraCapitao : new Color(.2f, .95f, .7f, 1f);
        corCarga.a = 1f;
        // Pulso suave de 2,6 s: realça o painel sem apagar o fundo nem a leitura.
        ficha.tempoEsperaEspecial = armado ? ficha.tempoEsperaEspecial + PassoTempo() : 0f;
        float pulsoEspera = armado ? .5f - .5f * Mathf.Cos(ficha.tempoEsperaEspecial * (2f * Mathf.PI / 2.6f)) : 0f;
        Color fundoBase = new Color(.018f, .028f, .055f, 1f);
        Color fundoAceso = Color.Lerp(new Color(.12f, .17f, .23f, 1f), corCarga, .15f);
        ficha.fundoEspecial.color = Color.Lerp(fundoBase, fundoAceso, pulsoEspera);
        float pulso = armado ? pulsoEspera : .5f + .5f * Mathf.Sin(Time.unscaledTime * 4f);
        ficha.carga.color = Color.Lerp(corCarga, Color.white, .08f + pulso * .18f);
        ficha.brilhoCarga.gameObject.SetActive(ficha.progressoCarga > .01f);
        float brilho = Mathf.Repeat(Time.unscaledTime * .65f, 1f) * .82f;
        ficha.brilhoCarga.rectTransform.anchorMin = new Vector2(brilho, .08f);
        ficha.brilhoCarga.rectTransform.anchorMax = new Vector2(brilho + .18f, .92f);
        ficha.textoCarga.text = armado ? "SPECIAL READY" : carga + "/" + PartidaDiscos.ToquesParaEspecial + " TOUCHES";
        ficha.textoCarga.color = armado ? Color.Lerp(corCarga, Color.white, pulso * .45f) : Color.white;
        ficha.porcentagemCarga.text = Mathf.RoundToInt(ficha.progressoCarga * 100f) + "%";
        ficha.porcentagemCarga.color = Color.Lerp(corCarga, Color.white, .12f);
        ficha.estadoCarga.text = carga == 0 ? "WAITING FOR TOUCHES" : "BUILDING POWER";
        for (int i = 0; i < ficha.marcadoresCarga.Length; i++)
            ficha.marcadoresCarga[i].color = i < carga
                ? Color.Lerp(corCarga, Color.white, armado ? pulsoEspera * .18f : .04f)
                : new Color(.12f, .18f, .25f, 1f);
        if (!girando && !armado) return;
        DiscoAtleta capitao = null;
        foreach (DiscoAtleta atleta in partida.Atletas)
            if (atleta.Capitao && atleta.TimeA == timeA) { capitao = atleta; break; }
        float chance = PartidaDiscos.ChanceEspecial(capitao != null ? capitao.OverallEfetivo : 50);
        ficha.roleta.DefinirChance(chance);
        float p = armado ? 1f : partida.ProgressoRoleta;
        bool acerto = armado ? partida.AcertoArmado(timeA) : partida.AcertoEspecial;
        float destino = acerto ? chance * 180f : chance * 360f + (1f - chance) * 180f;
        float angulo = Mathf.Lerp(0f, 1440f + destino, 1f - Mathf.Pow(1f - p, 3f));
        ficha.ponteiro.localRotation = Quaternion.Euler(0f, 0f, -angulo);
        ficha.resultado.text = p < 1f
            ? "<color=#33EE99>HIT " + Mathf.RoundToInt(chance * 100f) + "%</color>  <color=#FF6655>MISS " + Mathf.RoundToInt((1f - chance) * 100f) + "%</color>"
            : acerto ? "<color=#33EE99>HIT READY</color>\nNEXT TOUCH" : "<color=#FF6655>MISS READY</color>\nNEXT TOUCH";
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
