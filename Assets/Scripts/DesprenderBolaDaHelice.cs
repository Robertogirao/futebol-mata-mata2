using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class DesprenderBolaDaHelice : MonoBehaviour
{
    [Header("Kicker retention detection")]
    [InspectorName("Relative Motion Time Limit")]
    [Min(0.1f)]
    [Tooltip("Seconds near the same place on the rotating kicker before attempting a release.")]
    public float tempoMaximoDeContato = 1.2f;

    [Min(0.1f)]
    [Tooltip("Maximum time retained near the same kicker, even if the ball jitters or slides inside a boot. Never shorter than the relative-motion time limit.")]
    [InspectorName("Maximum Kicker Retention")]
    public float tempoMaximoDeRetencao = 3f;

    [Min(0.01f)]
    [Tooltip("Required movement relative to the kicker, measured in world units. Rotation alone does not reset the timer.")]
    [InspectorName("Relative Motion Radius")]
    public float raioMovimentoRelativo = 0.25f;

    [Min(0.01f)]
    [Tooltip("Tolerates brief gaps between collision callbacks. A release still requires current geometric proximity.")]
    [InspectorName("Contact Gap Tolerance")]
    public float toleranciaSemContato = 0.2f;

    [Min(0f)]
    [Tooltip("Maximum surface gap considered close to the kicker, in world units.")]
    [InspectorName("Proximity Margin")]
    public float margemProximidade = 0.08f;

    [Header("Release")]
    [InspectorName("Outward Relative Speed")]
    [Min(0.1f)]
    public float velocidadeDeSaida = 4f;

    [InspectorName("Retry Interval")]
    [Min(0.1f)]
    public float intervaloEntreTentativas = 1f;

    [Range(0.02f, 0.75f)]
    [Tooltip("Briefly ignores only this ball's collisions with this kicker, so the same boot cannot immediately recapture it. Uses real time.")]
    [InspectorName("Collision Release Window")]
    public float tempoSemColisao = 0.25f;

    private struct ParIgnorado
    {
        public Collider2D bola;
        public Collider2D helice;
        public float restaurarEm;
    }

    private Rigidbody2D corpo;
    private BallController bola;
    private Collider2D[] colisoresDaBola;
    private RotatingKicker heliceObservada;
    private Rigidbody2D corpoDaHelice;
    private Collider2D[] colisoresDaHelice;
    private Vector3 ancoraLocal;
    private Vector2 ultimaNormal;
    private float tempoDeContato;
    private float tempoNaHelice;
    private float ultimoContatoOuProximidade;
    private float proximaTentativa;

    private readonly List<ParIgnorado> paresIgnorados = new List<ParIgnorado>();

    void Awake()
    {
        corpo = GetComponent<Rigidbody2D>();
        bola = GetComponent<BallController>();
        colisoresDaBola = GetComponentsInChildren<Collider2D>(true);
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
        // Exit can be a single solver step in a rotating boot, or the result of
        // our own temporary IgnoreCollision. Neither cancels the observation.
    }

    private void RegistrarContato(Collision2D colisao)
    {
        if (corpo == null || paresIgnorados.Count > 0) return;
        Collider2D colisor = colisao.collider;
        if (!ColisorAtivo(colisor)) return;
        RotatingKicker helice = colisor.GetComponentInParent<RotatingKicker>();
        if (helice == null || !helice.isActiveAndEnabled) return;

        if (heliceObservada != helice)
        {
            LimparObservacao();
            heliceObservada = helice;
            corpoDaHelice = helice.GetComponentInParent<Rigidbody2D>();
            colisoresDaHelice = helice.GetComponentsInChildren<Collider2D>(true);
            ancoraLocal = helice.transform.InverseTransformPoint(corpo.position);
        }

        ultimoContatoOuProximidade = Time.time;
        ultimaNormal = Vector2.zero;
        for (int i = 0; i < colisao.contactCount; i++)
            ultimaNormal += colisao.GetContact(i).normal;
    }

    void Update()
    {
        // FixedUpdate/WaitForSeconds do not advance when the match freezes.
        // The temporary pair lease must still expire while timeScale is zero.
        RestaurarColisoesExpiradas();
        if (bola != null && bola.jaMarcouGol)
        {
            RestaurarTodasColisoes();
            LimparObservacao();
        }
    }

    void FixedUpdate()
    {
        RestaurarColisoesExpiradas();
        if (corpo == null || corpo.bodyType != RigidbodyType2D.Dynamic ||
            (bola != null && bola.jaMarcouGol))
        {
            RestaurarTodasColisoes();
            LimparObservacao();
            return;
        }
        if (Time.timeScale <= 0f || Time.fixedDeltaTime <= 0f) return;
        if (paresIgnorados.Count > 0)
        {
            LimparObservacao();
            return;
        }
        if (heliceObservada == null || !heliceObservada.isActiveAndEnabled)
        {
            LimparObservacao();
            return;
        }

        bool perto = EstaPertoDaHelice();
        if (perto)
            ultimoContatoOuProximidade = Time.time;
        else if (Time.time - ultimoContatoOuProximidade >
                 Mathf.Max(0.01f, toleranciaSemContato))
        {
            LimparObservacao();
            return;
        }

        tempoNaHelice += Time.fixedDeltaTime;
        Vector2 posicaoEsperada = heliceObservada.transform.TransformPoint(ancoraLocal);
        if (Vector2.Distance(corpo.position, posicaoEsperada) >
            Mathf.Max(0.01f, raioMovimentoRelativo))
        {
            // Rolling or flying relative to the boot is progress. A world-space
            // orbit with an unchanged local anchor is retained instead.
            ancoraLocal = heliceObservada.transform.InverseTransformPoint(corpo.position);
            tempoDeContato = 0f;
        }
        else
            tempoDeContato += Time.fixedDeltaTime;

        float limiteContato = Mathf.Max(0.1f, tempoMaximoDeContato);
        bool retida = tempoDeContato >= limiteContato ||
            tempoNaHelice >= Mathf.Max(limiteContato, tempoMaximoDeRetencao);
        if (!perto || !retida || Time.time < proximaTentativa)
            return;

        Desprender();
        proximaTentativa = Time.time + Mathf.Max(0.1f, intervaloEntreTentativas);
        LimparObservacao();
    }

    private bool EstaPertoDaHelice()
    {
        if (colisoresDaBola == null || colisoresDaHelice == null) return false;
        foreach (Collider2D proprio in colisoresDaBola)
        {
            if (!ColisorDaBola(proprio)) continue;
            foreach (Collider2D outro in colisoresDaHelice)
            {
                if (!ColisorDaHelice(outro)) continue;
                ColliderDistance2D distancia = proprio.Distance(outro);
                if (distancia.isValid && (distancia.isOverlapped ||
                    distancia.distance <= Mathf.Max(0f, margemProximidade)))
                    return true;
            }
        }
        return false;
    }

    private void Desprender()
    {
        Vector2 local = heliceObservada.transform.InverseTransformPoint(corpo.position);
        Vector2 direcao;
        if (Mathf.Abs(local.y) < 1.2f)
        {
            // The shaft's shortest escape is sideways, not along the shaft
            // toward one of the boots. The existing shaft is local Y aligned.
            Vector2 lateral = heliceObservada.transform.right;
            float lado = local.x;
            if (Mathf.Abs(lado) < 0.0001f)
                lado = Vector2.Dot(ultimaNormal, lateral);
            direcao = lado < 0f ? -lateral : lateral;
        }
        else
            direcao = corpo.position - (Vector2)heliceObservada.transform.position;

        if (direcao.sqrMagnitude < 0.0001f) direcao = Vector2.up;
        direcao.Normalize();
        Vector2 velocidadeDaSuperficie = corpoDaHelice != null
            ? corpoDaHelice.GetPointVelocity(corpo.position) : Vector2.zero;
        float velocidadeAtual = Vector2.Dot(
            corpo.linearVelocity - velocidadeDaSuperficie, direcao);
        float aumento = Mathf.Max(0f, Mathf.Max(0.1f, velocidadeDeSaida) - velocidadeAtual);

        // Establish the escape window even if the ball already has sufficient
        // velocity: active contact was what prevented it from leaving.
        IgnorarColisoesDaHelice();
        corpo.WakeUp();
        if (aumento > 0f)
            corpo.AddForce(direcao * aumento * corpo.mass, ForceMode2D.Impulse);
    }

    private void IgnorarColisoesDaHelice()
    {
        float prazo = Time.unscaledTime + Mathf.Clamp(tempoSemColisao, 0.02f, 0.75f);
        foreach (Collider2D proprio in colisoresDaBola)
        {
            if (!ColisorDaBola(proprio)) continue;
            foreach (Collider2D outro in colisoresDaHelice)
            {
                if (!ColisorDaHelice(outro)) continue;
                // An ignore established by another component belongs to it.
                if (Physics2D.GetIgnoreCollision(proprio, outro)) continue;
                Physics2D.IgnoreCollision(proprio, outro, true);
                paresIgnorados.Add(new ParIgnorado
                {
                    bola = proprio, helice = outro, restaurarEm = prazo
                });
            }
        }
    }

    private bool ColisorDaBola(Collider2D colisor)
    {
        return ColisorAtivo(colisor) && !colisor.isTrigger &&
            colisor.attachedRigidbody == corpo;
    }

    private bool ColisorDaHelice(Collider2D colisor)
    {
        return ColisorAtivo(colisor) && !colisor.isTrigger &&
            colisor.GetComponentInParent<RotatingKicker>() == heliceObservada &&
            (corpoDaHelice == null || colisor.attachedRigidbody == corpoDaHelice);
    }

    private static bool ColisorAtivo(Collider2D colisor)
    {
        return colisor != null && colisor.enabled && colisor.gameObject.activeInHierarchy;
    }

    private void RestaurarColisoesExpiradas()
    {
        for (int i = paresIgnorados.Count - 1; i >= 0; i--)
        {
            ParIgnorado par = paresIgnorados[i];
            if (Time.unscaledTime < par.restaurarEm &&
                ColisorAtivo(par.bola) && ColisorAtivo(par.helice))
                continue;
            RestaurarPar(par);
            paresIgnorados.RemoveAt(i);
        }
    }

    private static void RestaurarPar(ParIgnorado par)
    {
        // Unity drops ignored-pair state when a collider is disabled. Calling
        // IgnoreCollision on inactive/destroyed colliders is not supported.
        if (ColisorAtivo(par.bola) && ColisorAtivo(par.helice))
            Physics2D.IgnoreCollision(par.bola, par.helice, false);
    }

    private void RestaurarTodasColisoes()
    {
        foreach (ParIgnorado par in paresIgnorados) RestaurarPar(par);
        paresIgnorados.Clear();
    }

    private void LimparObservacao()
    {
        heliceObservada = null;
        corpoDaHelice = null;
        colisoresDaHelice = null;
        ultimaNormal = Vector2.zero;
        tempoDeContato = 0f;
        tempoNaHelice = 0f;
    }

    void OnDisable()
    {
        RestaurarTodasColisoes();
        LimparObservacao();
        proximaTentativa = 0f;
    }

    void OnDestroy()
    {
        RestaurarTodasColisoes();
    }

    void OnApplicationPause(bool pausado)
    {
        if (pausado) OnDisable();
    }
}
