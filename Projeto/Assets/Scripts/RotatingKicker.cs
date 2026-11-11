using UnityEngine;

public class RotatingKicker : MonoBehaviour
{
    [Header("Configuração da Hélice")]
    [Tooltip("Velocidade de rotação (valores positivos giram para um lado, negativos para o outro)")]
    public float velocidadeDeRotacao = 350f;

    [Header("Movimento de ida e volta")]
    public bool moverDeUmLadoParaOutro = false;
    [Tooltip("Distância máxima a partir da posição inicial, em unidades de mundo. X = horizontal; Y = vertical. Use Y = 0 para mover só para os lados.")]
    public Vector2 amplitudeMovimento = new Vector2(1.2f, 0f);
    [Min(0.05f)] public float velocidadeMinimaMovimento = 0.6f;
    [Min(0.05f)] public float velocidadeMaximaMovimento = 1.4f;

    private Rigidbody2D rb;
    private Vector2 origemMovimento;
    private float sentidoMovimento;
    private float velocidadeAtualMovimento;
    private bool destinoPreparado;
    private bool moveuNoPassoAnterior;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        origemMovimento = rb != null ? rb.position : (Vector2)transform.position;
    }

    void FixedUpdate()
    {
        // Gira o objeto usando a física para transferir um impacto real e violento para a bola!
        if (rb != null)
        {
            rb.MoveRotation(rb.rotation + velocidadeDeRotacao * Time.fixedDeltaTime);
        }
        else
        {
            transform.Rotate(0, 0, velocidadeDeRotacao * Time.deltaTime);
        }

        // A apresentação e o congelamento de fim de jogo não deslocam a hélice.
        if (!moverDeUmLadoParaOutro || amplitudeMovimento.sqrMagnitude < 0.000001f ||
            (UniversalUI.Instance != null && !UniversalUI.Instance.partidaEmAndamento))
        {
            PararDeslocamento();
            return;
        }

        if (!destinoPreparado)
        {
            sentidoMovimento = Random.Range(0, 2) == 0 ? -1f : 1f;
            SortearVelocidade();
            destinoPreparado = true;
        }

        Vector2 alvo = origemMovimento + amplitudeMovimento * sentidoMovimento;
        Vector2 atual = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 proxima = Vector2.MoveTowards(atual, alvo,
            velocidadeAtualMovimento * Time.fixedDeltaTime);
        if (rb != null) rb.MovePosition(proxima);
        else transform.position = new Vector3(proxima.x, proxima.y, transform.position.z);
        moveuNoPassoAnterior = true;

        if (Vector2.Distance(proxima, alvo) <= 0.0001f)
        {
            sentidoMovimento = -sentidoMovimento;
            SortearVelocidade();
        }
    }

    private void SortearVelocidade()
    {
        float minima = VelocidadeValida(velocidadeMinimaMovimento, 0.6f);
        float maxima = Mathf.Max(minima, VelocidadeValida(velocidadeMaximaMovimento, 1.4f));
        velocidadeAtualMovimento = Random.Range(minima, maxima);
    }

    private static float VelocidadeValida(float valor, float padrao)
    {
        return float.IsNaN(valor) || float.IsInfinity(valor) ? padrao : Mathf.Max(0.05f, valor);
    }

    private void PararDeslocamento()
    {
        if (moveuNoPassoAnterior && rb != null) rb.linearVelocity = Vector2.zero;
        moveuNoPassoAnterior = false;
    }

    private void OnDisable()
    {
        PararDeslocamento();
        destinoPreparado = false;
    }
}
