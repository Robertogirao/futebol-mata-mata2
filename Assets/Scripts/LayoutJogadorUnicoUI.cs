using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Compact representative cards anchored to the upper corners.</summary>
[DisallowMultipleComponent]
public sealed class LayoutJogadorUnicoUI : MonoBehaviour
{
    private sealed class Ficha
    {
        public Image faixa;
        public Image escudo;
        public Image foto;
        public TextMeshProUGUI equipe;
        public TextMeshProUGUI jogador;
        public TextMeshProUGUI semFoto;
    }

    private Ficha fichaA;
    private Ficha fichaB;
    private TMP_FontAsset fonte;

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
            "Time_A", new Vector2(0.015f, 0.62f), new Vector2(0.151f, 0.98f));
        layout.fichaB = layout.CriarFicha(
            "Time_B", new Vector2(0.849f, 0.62f), new Vector2(0.985f, 0.98f));
        raiz.gameObject.SetActive(false);
        return layout;
    }

    public void Configurar(
        TeamData timeA, TeamData timeB,
        List<JogadorDados> titularesA, List<JogadorDados> titularesB)
    {
        Preencher(fichaA, timeA, PrimeiroValido(titularesA), "TIME A");
        Preencher(fichaB, timeB, PrimeiroValido(titularesB), "TIME B");
        gameObject.SetActive(true);
    }

    private Ficha CriarFicha(string nome, Vector2 minimo, Vector2 maximo)
    {
        RectTransform bloco = CriarRetangulo(nome, transform, minimo, maximo);
        Image fundo = bloco.gameObject.AddComponent<Image>();
        fundo.color = new Color(0.025f, 0.045f, 0.09f, 0.94f);
        fundo.raycastTarget = false;

        Ficha ficha = new Ficha();
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

        TextMeshProUGUI legenda = CriarTexto("Legenda", bloco,
            new Vector2(0.07f, 0.181f), new Vector2(0.93f, 0.23f), 12.8f);
        legenda.text = "EM CAMPO";
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
            + "\n<size=28%>SEM FOTO</size>";
        ficha.semFoto.enabled = ficha.foto.sprite == null;
        ficha.jogador.text = jogador != null && !string.IsNullOrWhiteSpace(jogador.nomeJogador)
            ? jogador.nomeJogador.Trim() : "JOGADOR NÃO DEFINIDO";
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
