using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AutoBallPipe : MonoBehaviour
{
    public static AutoBallPipe Instance;

    // Notifica somente lançamentos realizados; arena cheia não dispara o efeito.
    public event System.Action BolaLancada;
    public bool EstaLancando { get; private set; }
    public int IdDaPartida { get; private set; }
    public bool RajadaAtiva { get { return multiplicadorRajada > 1f || bolasExtrasRajada > 0; } }
    public int LimiteAtualDeBolas { get { return Mathf.Clamp(Mathf.Clamp(limiteBolasEmCampo, 2, 30) + bolasExtrasRajada, 2, 30); } }
    public float IntervaloAtual { get { return Mathf.Max(0.05f, intervaloDeSpaw / multiplicadorRajada); } }

    private float multiplicadorRajada = 1f;
    private int bolasExtrasRajada;

    [Header("Conexão")]
    public GameObject prefabBola;
    public Transform pontoSaidaDoCano;

    [Header("Ritmo da Arena")]
    public float intervaloDeSpaw = 0.8f;
    public Vector2 forcaCuspe = new Vector2(4.5f, -2f);

    [Header("Overall dos Times")]
    public bool usarOverallDosTimes = true;

    [Range(0f, 0.25f)]
    [Tooltip("0.10: diferença de 20 pontos dá 60% dos lançamentos ao mais forte. Limite: 75%/25%.")]
    public float vantagemPor20Pontos = 0.10f;

    [Range(2, 30)]
    [Tooltip("Limite TOTAL dos dois times. Permite bolas repetidas dos mesmos três titulares.")]
    public int limiteBolasEmCampo = 6;

    // Campo antigo preservado, mas não utilizado.
    [HideInInspector]
    public float variacaoMaximaForcaPorHabilidade = 0.15f;

    [Header("Diagnóstico (preenchido automaticamente)")]
    [Range(0f, 1f)]
    public float chanceLancamentoTimeA = 0.5f;

    public int bolasLancadasTimeA;
    public int bolasLancadasTimeB;

    private List<JogadorDados> titularesTimeA =
        new List<JogadorDados>();

    private List<JogadorDados> titularesTimeB =
        new List<JogadorDados>();

    private Color corDaPartidaA;
    private Color corDaPartidaB;

    private string nomeTimeA;
    private string nomeTimeB;

    private bool resumoPendente;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void IniciarChuvaDeBolas(
        TeamData timeA,
        TeamData timeB,
        float duracaoEmSegundos)
    {
        Color corA;
        Color corB;

        TeamData.EscolherCoresParaPartida(
            timeA, timeB, 25f, out corA, out corB);

        IniciarChuvaDeBolas(
            timeA,
            timeB,
            duracaoEmSegundos,
            timeA != null
                ? timeA.ObterTitularesDaPartida()
                : new List<JogadorDados>(),
            timeB != null
                ? timeB.ObterTitularesDaPartida()
                : new List<JogadorDados>(),
            corA,
            corB);
    }

    public void IniciarChuvaDeBolas(
        TeamData timeA,
        TeamData timeB,
        float duracaoEmSegundos,
        List<JogadorDados> titularesA,
        List<JogadorDados> titularesB,
        Color corA,
        Color corB)
    {
        PararChuva();

        bolasLancadasTimeA = 0;
        bolasLancadasTimeB = 0;

        BallController modelo = prefabBola != null
            ? prefabBola.GetComponent<BallController>()
            : null;

        if (prefabBola == null ||
            modelo == null ||
            prefabBola.GetComponent<Rigidbody2D>() == null)
        {
            Debug.LogError(
                "Lançador sem prefab válido: atribua uma bola com BallController e Rigidbody2D.");
            return;
        }

        titularesTimeA = CopiarTitularesValidos(titularesA);
        titularesTimeB = CopiarTitularesValidos(titularesB);

        if (timeA == null ||
            timeB == null ||
            titularesTimeA.Count == 0 ||
            titularesTimeB.Count == 0)
        {
            Debug.LogError(
                "Lançador: cada time precisa ter titulares válidos.");
            return;
        }

        if (modelo.overrideManual)
        {
            Debug.LogWarning(
                "Desligue Override Manual no prefab para usar as cores automáticas dos times.");
        }

        corDaPartidaA = corA;
        corDaPartidaB = corB;

        nomeTimeA = timeA.nomeEquipe;
        nomeTimeB = timeB.nomeEquipe;

        chanceLancamentoTimeA = usarOverallDosTimes
            ? TeamData.CalcularChanceLancamentoTimeA(
                timeA,
                timeB,
                vantagemPor20Pontos)
            : 0.5f;

        resumoPendente = true;
        IdDaPartida++;
        EstaLancando = true;

        Debug.Log(
            $"Overall: {nomeTimeA} ({timeA.overall}) x " +
            $"{nomeTimeB} ({timeB.overall}). " +
            $"Participação prevista nos novos lançamentos: " +
            $"{chanceLancamentoTimeA:P0} / " +
            $"{1f - chanceLancamentoTimeA:P0}. " +
            "Não representa taxa garantida de vitórias.");

        StartCoroutine(
            RotinaDeCuspir(timeA, timeB, duracaoEmSegundos));
    }

    private static List<JogadorDados> CopiarTitularesValidos(
        List<JogadorDados> titulares)
    {
        var resultado = new List<JogadorDados>();

        if (titulares == null) return resultado;

        foreach (JogadorDados jogador in titulares)
        {
            if (jogador != null &&
                !resultado.Contains(jogador))
            {
                resultado.Add(jogador);
            }
        }

        return resultado;
    }

    public void PararChuva()
    {
        EstaLancando = false;
        EncerrarRajada();
        StopAllCoroutines();

        if (resumoPendente)
        {
            int total =
                bolasLancadasTimeA + bolasLancadasTimeB;

            float participacaoA = total > 0
                ? (float)bolasLancadasTimeA / total
                : 0f;

            Debug.Log(
                $"Lançamentos realizados: " +
                $"{nomeTimeA} = {bolasLancadasTimeA}; " +
                $"{nomeTimeB} = {bolasLancadasTimeB}. " +
                $"Total = {total}; " +
                $"participação observada de A = {participacaoA:P0}. " +
                "Em partidas curtas, pode diferir da participação prevista.");

            resumoPendente = false;
        }

        BallController[] bolas =
            FindObjectsByType<BallController>(FindObjectsInactive.Include);

        foreach (BallController bola in bolas)
        {
            if (bola != null)
                Destroy(bola.gameObject);
        }
    }

    private IEnumerator RotinaDeCuspir(
        TeamData timeA,
        TeamData timeB,
        float duracao)
    {
        float cronometro = 0f;

        while (cronometro < duracao)
        {
            CuspirProximaBola(timeA, timeB);

            float intervalo =
                IntervaloAtual;

            yield return new WaitForSeconds(intervalo);

            cronometro += intervalo;
        }

        EstaLancando = false;
        EncerrarRajada();
    }

    public void IniciarRajada(float multiplicador, int bolasExtras)
    {
        if (!EstaLancando || !isActiveAndEnabled) return;
        multiplicadorRajada = float.IsNaN(multiplicador) || float.IsInfinity(multiplicador)
            ? 1f : Mathf.Clamp(multiplicador, 1f, 5f);
        bolasExtrasRajada = Mathf.Clamp(bolasExtras, 0, 20);
    }

    public void EncerrarRajada()
    {
        multiplicadorRajada = 1f;
        bolasExtrasRajada = 0;
    }

    private void OnDisable()
    {
        EstaLancando = false;
        EncerrarRajada();
        StopAllCoroutines();
    }

    private bool ArenaCheia()
    {
        BallController[] bolas =
            FindObjectsByType<BallController>(FindObjectsInactive.Exclude);

        return bolas.Length >=
            LimiteAtualDeBolas;
    }

    private void CuspirProximaBola(
        TeamData timeA,
        TeamData timeB)
    {
        // Aguarda espaço. Não destrói bolas para criar novas.
        if (ArenaCheia()) return;

        bool ehTimeA =
            Random.Range(0f, 1f) < chanceLancamentoTimeA;

        CuspirUmaBola(
            ehTimeA ? timeA : timeB,
            ehTimeA ? titularesTimeA : titularesTimeB,
            ehTimeA);
    }

    private void CuspirUmaBola(
        TeamData time,
        List<JogadorDados> titulares,
        bool ehTimeA)
    {
        if (prefabBola == null ||
            time == null ||
            ArenaCheia())
        {
            return;
        }

        List<JogadorDados> disponiveis =
            CopiarTitularesValidos(titulares);

        if (disponiveis.Count == 0) return;

        // Somente titulares da tela.
        // Um titular pode representar mais de uma bola simultânea.
        JogadorDados jogador =
            disponiveis[Random.Range(0, disponiveis.Count)];

        Vector3 pos = pontoSaidaDoCano != null
            ? pontoSaidaDoCano.position
            : transform.position;

        GameObject novaBola =
            Instantiate(prefabBola, pos, Quaternion.identity);

        BallController bola =
            novaBola.GetComponent<BallController>();

        bola.ConfigurarBolaComDados(
            ehTimeA ? corDaPartidaA : corDaPartidaB,
            jogador,
            ehTimeA);

        // A física é igual para ambos os times.
        // A vantagem vem da participação nos lançamentos.
        Rigidbody2D rb =
            novaBola.GetComponent<Rigidbody2D>();

        float variacaoX =
            Random.Range(-1.5f, 1.5f);

        rb.AddForce(
            new Vector2(
                forcaCuspe.x + variacaoX,
                forcaCuspe.y),
            ForceMode2D.Impulse);

        if (ehTimeA)
            bolasLancadasTimeA++;
        else
            bolasLancadasTimeB++;

        BolaLancada?.Invoke();
    }
}
