using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class DesprenderBolaDaHelice : MonoBehaviour
{
    [Header("Detecção")]
    [Min(0.1f)]
    public float tempoMaximoDeContato = 1.2f;

    [Header("Desprendimento")]
    [Min(0.1f)]
    public float velocidadeDeSaida = 4f;

    [Min(0.1f)]
    public float intervaloEntreTentativas = 1f;

    private Rigidbody2D corpo;
    private BallController bola;

    private float tempoDeContato;
    private float proximaTentativa;

    private readonly Dictionary<Collider2D, Vector2> contatos =
        new Dictionary<Collider2D, Vector2>();

    private readonly List<Collider2D> remover =
        new List<Collider2D>();

    void Awake()
    {
        corpo = GetComponent<Rigidbody2D>();
        bola = GetComponent<BallController>();
    }

    void OnCollisionEnter2D(Collision2D colisao)
    {
        RegistrarContato(colisao);
    }

    void OnCollisionStay2D(Collision2D colisao)
    {
        RegistrarContato(colisao);
    }

    void OnCollisionExit2D(Collision2D colisao)
    {
        contatos.Remove(colisao.collider);

        if (contatos.Count == 0)
        {
            tempoDeContato = 0f;
        }
    }

    private void RegistrarContato(Collision2D colisao)
    {
        Collider2D colisor = colisao.collider;

        if (colisor == null)
            return;

        RotatingKicker helice =
            colisor.GetComponentInParent<RotatingKicker>();

        // Chão, goleiro e paredes não ativam esta proteção.
        if (helice == null)
            return;

        Vector2 normal = Vector2.zero;

        for (int i = 0; i < colisao.contactCount; i++)
        {
            normal += colisao.GetContact(i).normal;
        }

        if (normal.sqrMagnitude > 0.0001f)
        {
            normal.Normalize();
        }

        contatos[colisor] = normal;
    }

    void FixedUpdate()
    {
        if (corpo == null ||
            corpo.bodyType != RigidbodyType2D.Dynamic ||
            Time.timeScale <= 0f)
        {
            return;
        }

        if (bola != null && bola.jaMarcouGol)
        {
            contatos.Clear();
            tempoDeContato = 0f;
            return;
        }

        remover.Clear();

        Vector2 normaisSomadas = Vector2.zero;
        RotatingKicker heliceAtual = null;

        foreach (var contato in contatos)
        {
            Collider2D colisor = contato.Key;

            if (colisor == null ||
                !colisor.enabled ||
                !colisor.gameObject.activeInHierarchy ||
                !corpo.IsTouching(colisor))
            {
                remover.Add(colisor);
                continue;
            }

            RotatingKicker helice =
                colisor.GetComponentInParent<RotatingKicker>();

            if (helice == null)
            {
                remover.Add(colisor);
                continue;
            }

            if (heliceAtual == null)
            {
                heliceAtual = helice;
            }

            normaisSomadas += contato.Value;
        }

        foreach (Collider2D colisor in remover)
        {
            contatos.Remove(colisor);
        }

        if (contatos.Count == 0 || heliceAtual == null)
        {
            tempoDeContato = 0f;
            return;
        }

        tempoDeContato += Time.fixedDeltaTime;

        if (tempoDeContato <
                Mathf.Max(0.1f, tempoMaximoDeContato) ||
            Time.time < proximaTentativa)
        {
            return;
        }

        Desprender(heliceAtual, normaisSomadas);

        tempoDeContato = 0f;

        proximaTentativa =
            Time.time +
            Mathf.Max(0.1f, intervaloEntreTentativas);
    }

    private void Desprender(
        RotatingKicker helice,
        Vector2 normaisSomadas)
    {
        // Prefere a direção para fora da superfície de contato.
        Vector2 direcao = normaisSomadas;

        if (direcao.sqrMagnitude < 0.0001f)
        {
            direcao =
                corpo.position -
                (Vector2)helice.transform.position;
        }

        if (direcao.sqrMagnitude < 0.0001f)
        {
            direcao = Vector2.up;
        }

        direcao.Normalize();

        Rigidbody2D corpoDaHelice =
            helice.GetComponentInParent<Rigidbody2D>();

        Vector2 velocidadeDaSuperficie =
            corpoDaHelice != null
                ? corpoDaHelice.GetPointVelocity(corpo.position)
                : Vector2.zero;

        Vector2 velocidadeRelativa =
            corpo.linearVelocity - velocidadeDaSuperficie;

        float velocidadeAtualDeSaida =
            Vector2.Dot(velocidadeRelativa, direcao);

        float aumentoNecessario = Mathf.Max(
            0f,
            Mathf.Max(0.1f, velocidadeDeSaida) -
            velocidadeAtualDeSaida);

        if (aumentoNecessario <= 0f)
            return;

        corpo.WakeUp();

        // Considera a massa para obter uma saída consistente.
        corpo.AddForce(
            direcao * aumentoNecessario * corpo.mass,
            ForceMode2D.Impulse);
    }

    void OnDisable()
    {
        contatos.Clear();
        remover.Clear();

        tempoDeContato = 0f;
        proximaTentativa = 0f;
    }
}