using UnityEngine;

public class GoalkeeperController : MonoBehaviour
{
    [Header("Configurações de Dificuldade (Nerf)")]
    [Tooltip("Quanto menor a velocidade, mais fácil fazer gol. Recomendado: 4 ou 5.")]
    public float velocidadeSeguimento = 4.5f;

    [Tooltip("Cria uma falha na precisão do goleiro para ele errar bolas nos cantos.")]
    public float margemDeErro = 0.6f;

    [Header("Limites do Gol")]
    public float limiteSuperior = 2f;
    public float limiteInferior = -3f;

    private float alvoY;
    private float erroAtual = 0f;
    private float tempoParaMudarErro = 0f;

    void Start()
    {
        alvoY = transform.position.y;
    }

    void Update()
    {
        if (Time.time >= tempoParaMudarErro)
        {
            erroAtual = Random.Range(
                -margemDeErro,
                margemDeErro);

            tempoParaMudarErro = Time.time + 1.2f;
        }

        BallController[] bolasNoCampo =
            FindObjectsByType<BallController>(FindObjectsInactive.Exclude);

        float alvoYDesejado = 0f;
        bool achouBolaPerigosa = false;
        float menorDistancia = 999f;

        foreach (BallController bola in bolasNoCampo)
        {
            if (bola != null && !bola.jaMarcouGol && !bola.EmDesintegracao)
            {
                float dist = Vector2.Distance(
                    transform.position,
                    bola.transform.position);

                if (dist < menorDistancia)
                {
                    menorDistancia = dist;
                    alvoYDesejado =
                        bola.transform.position.y + erroAtual;

                    achouBolaPerigosa = true;
                }
            }
        }

        if (!achouBolaPerigosa)
        {
            alvoYDesejado =
                (limiteSuperior + limiteInferior) / 2f;
        }

        alvoY = Mathf.Lerp(
            alvoY,
            alvoYDesejado,
            velocidadeSeguimento * Time.deltaTime);

        float yFinal = Mathf.Clamp(
            alvoY,
            limiteInferior,
            limiteSuperior);

        transform.position = new Vector3(
            transform.position.x,
            yFinal,
            transform.position.z);
    }
}