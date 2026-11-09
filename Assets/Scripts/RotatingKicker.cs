using UnityEngine;

public class RotatingKicker : MonoBehaviour
{
    [Header("Configuração da Hélice")]
    [Tooltip("Velocidade de rotação (valores positivos giram para um lado, negativos para o outro)")]
    public float velocidadeDeRotacao = 350f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
    }
}