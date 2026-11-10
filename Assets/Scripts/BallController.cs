using UnityEngine;
using TMPro;

public class BallController : MonoBehaviour
{
    [Header("Componentes Visuais da Bola")]
    public SpriteRenderer visualBola;
    public TextMeshPro textoNumeroCamisa;

    [Header("Tag do Jogador")]
    [Tooltip("Mostra o nome acima da bola. Desmarque para voltar ao número.")]
    public bool usarNomeAcimaDaBola = true;

    public Vector2 deslocamentoTag = new Vector2(0f, 0.55f);

    [Range(0.1f, 2f)]
    public float escalaFonteDoNome = 0.45f;

    public Color corTextoTag = Color.white;

    [Range(0f, 1f)]
    public float contornoTag = 0.2f;

    [Header("Opção de Override Manual")]
    public bool overrideManual = false;
    public Color corPersonalizada = Color.yellow;
    public string numeroPersonalizado = "99";
    public string nomePersonalizado = "";

    [Header("Física e Segurança")]
    public bool removerBolasPresas = true;

    [InspectorName("Tempo Preso Para Remover")]
    [Min(0.1f)]
    [Tooltip("Segundos sem sair da área de detecção antes de remover a bola. Não é mais um limite total de vida.")]
    public float tempoMaximoDeVida = 0.5f;

    [Min(0.01f)]
    [Tooltip("Raio em unidades de mundo. Ao sair dessa área, o contador zera. Use um valor pequeno para preservar trajetórias lentas.")]
    public float raioConsideradoPreso = 0.25f;

    private float tempoSemSairDaArea;
    private Vector2 centroDaArea;

    [HideInInspector] public JogadorDados dadosJogador;
    [HideInInspector] public bool pertenceAoTimeA = true;
    [HideInInspector] public string numeroCamisaAtual = "";
    [HideInInspector] public bool jaMarcouGol = false;

    private bool textoPreparado;
    private float tamanhoFonteOriginal;
    private Vector3 posicaoLocalOriginal;
    private Quaternion rotacaoLocalOriginal;
    private float contornoOriginal;
    private Color corContornoOriginal;

    void Awake()
    {
        PrepararTexto();
        ReiniciarDetectorDeTravamento();
    }

    private void PrepararTexto()
    {
        if (textoPreparado || textoNumeroCamisa == null) return;

        textoPreparado = true;
        tamanhoFonteOriginal = textoNumeroCamisa.fontSize;
        posicaoLocalOriginal = textoNumeroCamisa.transform.localPosition;
        rotacaoLocalOriginal = textoNumeroCamisa.transform.localRotation;
        contornoOriginal = textoNumeroCamisa.outlineWidth;
        corContornoOriginal = textoNumeroCamisa.outlineColor;

        if (usarNomeAcimaDaBola && visualBola != null)
        {
            Renderer renderizadorTexto =
                textoNumeroCamisa.GetComponent<Renderer>();

            if (renderizadorTexto != null)
            {
                renderizadorTexto.sortingLayerID =
                    visualBola.sortingLayerID;

                renderizadorTexto.sortingOrder =
                    visualBola.sortingOrder + 1;
            }
        }
    }

    void Start()
    {
        if (overrideManual)
        {
            AplicarVisual(corPersonalizada, numeroPersonalizado);
        }

        AtualizarIdentificacao();
    }

    void Update()
    {
        if (!removerBolasPresas || jaMarcouGol)
        {
            ReiniciarDetectorDeTravamento();
            return;
        }

        // Não acumula tempo durante o congelamento da partida.
        if (Time.deltaTime <= 0f) return;

        float raio = Mathf.Max(0.01f, raioConsideradoPreso);

        float distancia = Vector2.Distance(
            centroDaArea,
            transform.position);

        if (distancia > raio)
        {
            // A bola está se deslocando: começa uma nova observação.
            ReiniciarDetectorDeTravamento();
            return;
        }

        tempoSemSairDaArea += Time.deltaTime;

        if (tempoSemSairDaArea >=
            Mathf.Max(0.1f, tempoMaximoDeVida))
        {
            Destroy(gameObject);
        }
    }

    private void ReiniciarDetectorDeTravamento()
    {
        centroDaArea = transform.position;
        tempoSemSairDaArea = 0f;
    }

    void LateUpdate()
    {
        if (!usarNomeAcimaDaBola || textoNumeroCamisa == null)
            return;

        if (textoNumeroCamisa.transform == transform) return;

        textoNumeroCamisa.transform.position =
            transform.position +
            new Vector3(
                deslocamentoTag.x,
                deslocamentoTag.y,
                posicaoLocalOriginal.z);

        textoNumeroCamisa.transform.rotation =
            Quaternion.identity;
    }

    public void ConfigurarBolaComDados(
        Color corDoTime,
        JogadorDados jogador,
        bool ehTimeA)
    {
        ReiniciarDetectorDeTravamento();

        jaMarcouGol = false;
        pertenceAoTimeA = ehTimeA;
        dadosJogador = jogador;

        string num = jogador != null
            ? jogador.numeroCamisa.ToString()
            : "10";

        numeroCamisaAtual = num;

        AplicarVisual(
            overrideManual ? corPersonalizada : corDoTime,
            overrideManual ? numeroPersonalizado : num);
    }

    public void ConfigurarBola(
        JogadorDados jogador,
        Color corDoTime,
        bool ehTimeA)
    {
        ConfigurarBolaComDados(corDoTime, jogador, ehTimeA);
    }

    public void ConfigurarBolaDireto(
        Color corDoTime,
        string numeroCamisa,
        bool ehTimeA)
    {
        ReiniciarDetectorDeTravamento();

        jaMarcouGol = false;
        dadosJogador = null;
        pertenceAoTimeA = ehTimeA;
        numeroCamisaAtual = numeroCamisa;

        AplicarVisual(
            overrideManual ? corPersonalizada : corDoTime,
            overrideManual ? numeroPersonalizado : numeroCamisa);
    }

    private void AplicarVisual(Color cor, string numero)
    {
        if (visualBola != null)
        {
            visualBola.color = cor;
        }

        AtualizarIdentificacao(numero, cor);
    }

    private void AtualizarIdentificacao()
    {
        AtualizarIdentificacao(
            overrideManual ? numeroPersonalizado : numeroCamisaAtual,
            visualBola != null ? visualBola.color : corPersonalizada);
    }

    private void AtualizarIdentificacao(string numero, Color cor)
    {
        if (textoNumeroCamisa == null) return;

        PrepararTexto();

        if (usarNomeAcimaDaBola)
        {
            string nome =
                overrideManual &&
                !string.IsNullOrWhiteSpace(nomePersonalizado)
                    ? nomePersonalizado
                    : dadosJogador != null
                        ? dadosJogador.nomeJogador
                        : null;

            textoNumeroCamisa.text =
                !string.IsNullOrWhiteSpace(nome) ? nome : numero;

            textoNumeroCamisa.richText = false;
            textoNumeroCamisa.enableWordWrapping = false;
            textoNumeroCamisa.overflowMode =
                TextOverflowModes.Overflow;

            textoNumeroCamisa.alignment =
                TextAlignmentOptions.Center;

            textoNumeroCamisa.enableAutoSizing = false;

            textoNumeroCamisa.fontSize =
                tamanhoFonteOriginal * escalaFonteDoNome;

            textoNumeroCamisa.color = corTextoTag;
            textoNumeroCamisa.outlineColor = Color.black;
            textoNumeroCamisa.outlineWidth = contornoTag;
        }
        else
        {
            textoNumeroCamisa.text = numero;
            textoNumeroCamisa.fontSize = tamanhoFonteOriginal;

            textoNumeroCamisa.transform.localPosition =
                posicaoLocalOriginal;

            textoNumeroCamisa.transform.localRotation =
                rotacaoLocalOriginal;

            textoNumeroCamisa.outlineWidth = contornoOriginal;
            textoNumeroCamisa.outlineColor = corContornoOriginal;

            float brilho =
                cor.r * 0.299f +
                cor.g * 0.587f +
                cor.b * 0.114f;

            textoNumeroCamisa.color =
                brilho > 0.5f ? Color.black : Color.white;
        }
    }
}