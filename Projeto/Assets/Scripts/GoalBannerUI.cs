using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GoalBannerUI : MonoBehaviour
{
    public static GoalBannerUI Instance;

    [Header("Componentes do Banner")]
    public GameObject painelBanner;
    public Image fotoJogador;
    public TextMeshProUGUI textoDados;

    [Header("Tempo na Tela")]
    [Min(0.1f)]
    public float tempoDeExibicao = 3f;

    [Min(0f)]
    public float intervaloEntreBanners = 0.15f;

    private class Comemoracao
    {
        public string nome;
        public Sprite foto;
        public bool especial;
    }

    private readonly Queue<Comemoracao> fila =
        new Queue<Comemoracao>();

    private Coroutine rotinaFila;
    private bool exibindo;
    private GameObject painelEstilizado;
    private RectTransform retanguloEstilizado;
    private Vector2 posicaoPainelOriginal;
    private Vector3 posicaoLocalOriginal, escalaPainelOriginal, escalaFotoOriginal;
    private CanvasGroup grupoPainel, grupoCriado;
    private float alphaPainelOriginal;
    private Image fundoEstilizado, fotoEstilizada;
    private TextMeshProUGUI textoEstilizado;
    private Color corFundoOriginal, corFotoOriginal, corTextoOriginal;
    private bool richTextOriginal;
    private RectTransform efeitosEspeciais, faixaEnergia;
    private CanvasGroup grupoEfeitos;
    private Image brilhoEspecial;

    public bool TemBannersPendentes
    {
        get { return exibindo || fila.Count > 0; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        OcultarPainel();
    }

    public void ExibirBannerDeGol(
        JogadorDados jogador,
        Color corDoTime)
    {
        ExibirBannerDeGol(jogador != null ? jogador.nomeJogador : null,
            jogador != null ? jogador.foto512x512 : null, corDoTime);
    }

    public void ExibirBannerDeGol(string nome, Sprite imagem, Color corDoTime)
    {
        EnfileirarGol(nome, imagem, false);
    }

    public void ExibirBannerDeGolEspecial(string nome, Sprite imagem, Color corDoTime)
    {
        EnfileirarGol(nome, imagem, true);
    }

    private void EnfileirarGol(string nome, Sprite imagem, bool especial)
    {
        // O painel deve ser filho de um objeto que permanece ativo.
        if (PainelDesativaEsteObjeto())
        {
            Debug.LogError(
                "GoalBannerUI: coloque este componente em um " +
                "objeto pai ativo e atribua um painel filho " +
                "ao campo Painel Banner.",
                this);

            return;
        }

        gameObject.SetActive(true);

        if (!isActiveAndEnabled)
        {
            Debug.LogWarning(
                "GoalBannerUI: mantenha o componente e seus " +
                "objetos pais ativos para exibir as comemorações.",
                this);

            return;
        }

        // Snapshot the name and chosen image; queued goals never depend on
        // the actor lifetime and never change the source player/team assets.
        var comemoracao = new Comemoracao
        {
            nome = nome == null ? null : string.IsNullOrWhiteSpace(nome) ? "PLAYER" : nome.ToUpperInvariant(),
            foto = imagem,
            especial = especial
        };

        fila.Enqueue(comemoracao);

        // Novos gols entram na fila sem interromper o atual.
        if (!exibindo)
        {
            exibindo = true;
            rotinaFila = StartCoroutine(ProcessarFila());
        }
    }

    private IEnumerator ProcessarFila()
    {
        while (fila.Count > 0)
        {
            Comemoracao atual = fila.Dequeue();
            RestaurarEstiloEspecial();
            if (atual.especial) PrepararEstiloEspecial();

            if (fotoJogador != null)
            {
                fotoJogador.sprite = atual.foto;
                fotoJogador.enabled = atual.foto != null;
            }

            if (textoDados != null)
            {
                string chamada = atual.especial
                    ? "<color=#FFD75E>SPECIAL GOAL!</color>"
                    : "<color=yellow>GOAL!</color>";
                textoDados.text = string.IsNullOrEmpty(atual.nome) ? chamada : chamada + "\n" + atual.nome;
            }

            if (painelBanner != null)
            {
                painelBanner.SetActive(true);
            }

            // Usa o tempo do vídeo no Recorder e continua com Time.timeScale = 0.
            if (atual.especial)
                yield return AnimarGolEspecial(Mathf.Max(0.1f, tempoDeExibicao));
            else
                yield return AguardarTempo(Mathf.Max(0.1f, tempoDeExibicao));

            OcultarPainel();

            // Pequena separação visual entre comemorações,
            // inclusive quando o mesmo jogador marca novamente.
            if (intervaloEntreBanners > 0f)
            {
                yield return AguardarTempo(
                    intervaloEntreBanners);
            }
            else
            {
                yield return null;
            }
        }

        exibindo = false;
        rotinaFila = null;
    }

    private static IEnumerator AguardarTempo(float duracao)
    {
        double tempo = 0d;
        while (tempo + 0.000001d < duracao)
        {
            yield return null;
            tempo += PassoTempo();
        }
    }

    private static float PassoTempo()
    {
        float passo = Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;
        return float.IsNaN(passo) || float.IsInfinity(passo) ? 0f : Mathf.Max(0f, passo);
    }

    private void PrepararEstiloEspecial()
    {
        if (painelBanner == null) return;
        // A single owned CanvasGroup is reused for this panel, never shared with another binding.
        if (grupoCriado != null && grupoCriado.gameObject != painelBanner)
        {
            Destroy(grupoCriado);
            grupoCriado = null;
        }
        painelEstilizado = painelBanner;
        retanguloEstilizado = painelEstilizado.transform as RectTransform;
        posicaoLocalOriginal = painelEstilizado.transform.localPosition;
        posicaoPainelOriginal = retanguloEstilizado != null ? retanguloEstilizado.anchoredPosition : Vector2.zero;
        escalaPainelOriginal = painelEstilizado.transform.localScale;
        grupoPainel = painelEstilizado.GetComponent<CanvasGroup>();
        if (grupoPainel == null)
        {
            grupoPainel = painelEstilizado.AddComponent<CanvasGroup>();
            // Identity interaction settings preserve the panel's normal behavior.
            grupoPainel.interactable = true;
            grupoPainel.blocksRaycasts = true;
            grupoCriado = grupoPainel;
        }
        alphaPainelOriginal = grupoPainel.alpha;
        fundoEstilizado = painelEstilizado.GetComponent<Image>();
        if (fundoEstilizado != null)
        {
            corFundoOriginal = fundoEstilizado.color;
            fundoEstilizado.color = new Color(.055f, .035f, .012f, corFundoOriginal.a);
        }
        fotoEstilizada = fotoJogador;
        if (fotoEstilizada != null)
        {
            corFotoOriginal = fotoEstilizada.color;
            escalaFotoOriginal = fotoEstilizada.transform.localScale;
        }
        textoEstilizado = textoDados;
        if (textoEstilizado != null)
        {
            corTextoOriginal = textoEstilizado.color;
            richTextOriginal = textoEstilizado.richText;
            textoEstilizado.color = Color.white;
            textoEstilizado.richText = true;
        }
        efeitosEspeciais = RetanguloEspecial("Special_Goal_FX", painelEstilizado.transform, Vector2.zero, Vector2.one);
        efeitosEspeciais.SetAsFirstSibling();
        grupoEfeitos = efeitosEspeciais.gameObject.AddComponent<CanvasGroup>();
        grupoEfeitos.interactable = false;
        grupoEfeitos.blocksRaycasts = false;
        Color ouro = new Color(1f, .73f, .18f, 1f);
        ImagemEspecial("Special_Frame_Top", efeitosEspeciais, new Vector2(0f, .972f), Vector2.one, ouro);
        ImagemEspecial("Special_Frame_Bottom", efeitosEspeciais, Vector2.zero, new Vector2(1f, .028f), ouro);
        ImagemEspecial("Special_Frame_Left", efeitosEspeciais, new Vector2(0f, .028f), new Vector2(.008f, .972f), ouro);
        ImagemEspecial("Special_Frame_Right", efeitosEspeciais, new Vector2(.992f, .028f), new Vector2(1f, .972f), ouro);
        brilhoEspecial = ImagemEspecial("Special_Gold_Glow", efeitosEspeciais,
            new Vector2(.015f, .06f), new Vector2(.985f, .94f), new Color(1f, .58f, .08f, .08f));
        faixaEnergia = ImagemEspecial("Special_Energy_Sweep", efeitosEspeciais,
            new Vector2(.02f, .09f), new Vector2(.14f, .91f), new Color(1f, .86f, .35f, .18f)).rectTransform;
        for (int i = 0; i < 3; i++)
        {
            float x = .72f + i * .06f;
            RectTransform raio = ImagemEspecial("Special_Energy_Bar_" + i, efeitosEspeciais,
                new Vector2(x, .04f), new Vector2(x + .035f, .095f), ouro).rectTransform;
            raio.localRotation = Quaternion.Euler(0f, 0f, 24f);
        }
        AtualizarEstiloEspecial(0f, Mathf.Max(.1f, tempoDeExibicao));
    }

    private IEnumerator AnimarGolEspecial(float duracao)
    {
        double tempo = 0d;
        while (tempo + .000001d < duracao)
        {
            AtualizarEstiloEspecial((float)tempo, duracao);
            yield return null;
            tempo += PassoTempo();
        }
    }

    private void AtualizarEstiloEspecial(float tempo, float duracao)
    {
        if (painelEstilizado == null) return;
        float entrada = Mathf.SmoothStep(0f, 1f, tempo / Mathf.Min(.42f, duracao * .25f));
        float saida = Mathf.SmoothStep(0f, 1f, (duracao - tempo) / Mathf.Min(.32f, duracao * .22f));
        float visibilidade = Mathf.Min(entrada, saida);
        float pulso = Mathf.Sin(Mathf.Clamp01(tempo / duracao) * Mathf.PI);
        if (grupoPainel != null) grupoPainel.alpha = alphaPainelOriginal * visibilidade;
        painelEstilizado.transform.localScale = escalaPainelOriginal * (Mathf.Lerp(.86f, 1f, entrada) + pulso * .025f);
        float deslocamento = (1f - entrada) * 72f - (1f - saida) * 22f;
        if (retanguloEstilizado != null)
            retanguloEstilizado.anchoredPosition = posicaoPainelOriginal + new Vector2(deslocamento, 0f);
        else
            painelEstilizado.transform.localPosition = posicaoLocalOriginal + new Vector3(deslocamento, 0f, 0f);
        if (fotoEstilizada != null)
        {
            fotoEstilizada.transform.localScale = escalaFotoOriginal * (1f + pulso * .06f);
            fotoEstilizada.color = Color.Lerp(corFotoOriginal, new Color(1f, .93f, .73f, corFotoOriginal.a), pulso * .18f);
        }
        if (grupoEfeitos != null) grupoEfeitos.alpha = .76f + pulso * .24f;
        if (brilhoEspecial != null) brilhoEspecial.color = new Color(1f, .58f, .08f, .045f + pulso * .055f);
        if (faixaEnergia != null)
        {
            float x = Mathf.Lerp(.02f, .86f, Mathf.Clamp01(tempo / duracao));
            faixaEnergia.anchorMin = new Vector2(x, .09f);
            faixaEnergia.anchorMax = new Vector2(x + .12f, .91f);
        }
    }

    private void RestaurarEstiloEspecial()
    {
        if (painelEstilizado != null)
        {
            painelEstilizado.transform.localScale = escalaPainelOriginal;
            if (retanguloEstilizado != null) retanguloEstilizado.anchoredPosition = posicaoPainelOriginal;
            else painelEstilizado.transform.localPosition = posicaoLocalOriginal;
        }
        if (grupoPainel != null) grupoPainel.alpha = alphaPainelOriginal;
        if (fundoEstilizado != null) fundoEstilizado.color = corFundoOriginal;
        if (fotoEstilizada != null)
        {
            fotoEstilizada.color = corFotoOriginal;
            fotoEstilizada.transform.localScale = escalaFotoOriginal;
        }
        if (textoEstilizado != null)
        {
            textoEstilizado.color = corTextoOriginal;
            textoEstilizado.richText = richTextOriginal;
        }
        if (efeitosEspeciais != null)
        {
            efeitosEspeciais.gameObject.SetActive(false);
            Destroy(efeitosEspeciais.gameObject);
        }
        painelEstilizado = null;
        retanguloEstilizado = null;
        grupoPainel = null;
        fundoEstilizado = fotoEstilizada = null;
        textoEstilizado = null;
        efeitosEspeciais = faixaEnergia = null;
        grupoEfeitos = null;
        brilhoEspecial = null;
    }

    private static RectTransform RetanguloEspecial(string nome, Transform pai, Vector2 minimo, Vector2 maximo)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform));
        objeto.layer = pai.gameObject.layer;
        RectTransform rect = (RectTransform)objeto.transform;
        rect.SetParent(pai, false);
        rect.anchorMin = minimo;
        rect.anchorMax = maximo;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static Image ImagemEspecial(string nome, Transform pai, Vector2 minimo, Vector2 maximo, Color cor)
    {
        Image imagem = RetanguloEspecial(nome, pai, minimo, maximo).gameObject.AddComponent<Image>();
        imagem.color = cor;
        imagem.raycastTarget = false;
        return imagem;
    }

    public void LimparFila()
    {
        if (rotinaFila != null)
        {
            StopCoroutine(rotinaFila);
            rotinaFila = null;
        }

        fila.Clear();
        exibindo = false;

        OcultarPainel();
    }

    private bool PainelDesativaEsteObjeto()
    {
        return painelBanner != null &&
            transform.IsChildOf(painelBanner.transform);
    }

    private void OcultarPainel()
    {
        RestaurarEstiloEspecial();
        if (painelBanner != null &&
            !PainelDesativaEsteObjeto())
        {
            painelBanner.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            LimparFila();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            LimparFila();
            Instance = null;
        }
        else RestaurarEstiloEspecial();
        if (grupoCriado != null) Destroy(grupoCriado);
        grupoCriado = null;
    }
}