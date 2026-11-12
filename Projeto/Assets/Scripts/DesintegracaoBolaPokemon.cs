using UnityEngine;

/// <summary>Recuo curto, dissolução e fragmentos pertencentes à própria bola.</summary>
[DisallowMultipleComponent]
public sealed class DesintegracaoBolaPokemon : MonoBehaviour
{
    private const int QuantidadeFragmentos = 10;
    private SpriteRenderer[] visuais;
    private Color[] cores;
    private readonly SpriteRenderer[] fragmentos = new SpriteRenderer[QuantidadeFragmentos];
    private readonly Vector3[] direcoes = new Vector3[QuantidadeFragmentos];
    private readonly Vector3[] escalas = new Vector3[QuantidadeFragmentos];
    private Color corFragmentos;
    private Vector3 centroFragmentos;
    private Rigidbody2D corpo;
    private SpriteRenderer origemFragmentos;
    private float atraso;
    private float duracao;
    private double tempo;
    private bool iniciado;
    private bool dissolvendo;
    private bool concluido;

    public void Iniciar(BallController bola, float segundosDeRecuo, float segundosDeDissolucao)
    {
        if (iniciado || bola == null) return;
        iniciado = true;
        atraso = Seguro(segundosDeRecuo, 0.18f, 0f, 2f);
        duracao = Seguro(segundosDeDissolucao, 0.28f, 0.05f, 2f);
        corpo = bola.GetComponent<Rigidbody2D>();

        // A bola continua se afastando, mas deixa de colidir e de poder pontuar.
        DesprenderBolaDaHelice antiTravamento = bola.GetComponent<DesprenderBolaDaHelice>();
        if (antiTravamento != null) antiTravamento.enabled = false;
        foreach (Collider2D colisor in bola.GetComponentsInChildren<Collider2D>(true))
            colisor.enabled = false;

        if (bola.textoNumeroCamisa != null) bola.textoNumeroCamisa.enabled = false;
        foreach (TMPro.TextMeshPro tag in bola.GetComponentsInChildren<TMPro.TextMeshPro>(true))
            tag.enabled = false;

        visuais = bola.GetComponentsInChildren<SpriteRenderer>(true);
        cores = new Color[visuais.Length];
        for (int i = 0; i < visuais.Length; i++) cores[i] = visuais[i].color;
        origemFragmentos = bola.visualBola;
        if (origemFragmentos == null || origemFragmentos.sprite == null)
            foreach (SpriteRenderer visual in visuais)
                if (visual.enabled && visual.sprite != null) { origemFragmentos = visual; break; }
    }

    private void Update()
    {
        if (!iniciado || concluido || float.IsNaN(Time.deltaTime) ||
            float.IsInfinity(Time.deltaTime) || Time.deltaTime <= 0f) return;
        tempo += Time.deltaTime;
        if (tempo < atraso) return;

        if (!dissolvendo)
        {
            dissolvendo = true;
            // Primeiro o impulso afasta a bola; depois os fragmentos se dissolvem no ponto atingido.
            if (corpo != null) corpo.simulated = false;
            CriarFragmentos();
        }

        float progresso = Mathf.Clamp01((float)((tempo - atraso) / duracao));
        for (int i = 0; i < visuais.Length; i++)
        {
            if (visuais[i] == null) continue;
            Color cor = Color.Lerp(cores[i], Color.white, 0.65f * (1f - progresso));
            cor.a = cores[i].a * (1f - progresso);
            visuais[i].color = cor;
        }
        for (int i = 0; i < fragmentos.Length; i++)
        {
            SpriteRenderer fragmento = fragmentos[i];
            if (fragmento == null) continue;
            fragmento.transform.localPosition = centroFragmentos + direcoes[i] * progresso;
            fragmento.transform.localScale = escalas[i] * (1f - progresso);
            Color cor = corFragmentos;
            cor.a *= 1f - progresso;
            fragmento.color = cor;
        }
        if (progresso >= 1f) Concluir();
    }

    private void CriarFragmentos()
    {
        if (origemFragmentos == null || origemFragmentos.sprite == null) return;
        centroFragmentos = transform.InverseTransformPoint(origemFragmentos.bounds.center);
        Vector3 escalaMundo = origemFragmentos.transform.lossyScale;
        Vector3 escalaRaiz = transform.lossyScale;
        Vector3 escalaRelativa = new Vector3(
            Mathf.Abs(escalaMundo.x) / Mathf.Max(0.0001f, Mathf.Abs(escalaRaiz.x)),
            Mathf.Abs(escalaMundo.y) / Mathf.Max(0.0001f, Mathf.Abs(escalaRaiz.y)), 1f);
        corFragmentos = Color.Lerp(origemFragmentos.color, Color.white, 0.45f);
        for (int i = 0; i < fragmentos.Length; i++)
        {
            var objeto = new GameObject("Snorlax.FragmentoBola");
            objeto.transform.SetParent(transform, false);
            SpriteRenderer fragmento = objeto.AddComponent<SpriteRenderer>();
            fragmento.sprite = origemFragmentos.sprite;
            fragmento.sharedMaterial = origemFragmentos.sharedMaterial;
            fragmento.sortingLayerID = origemFragmentos.sortingLayerID;
            fragmento.sortingOrder = origemFragmentos.sortingOrder + 4;
            fragmento.color = corFragmentos;
            float angulo = i * 2f * Mathf.PI / fragmentos.Length;
            direcoes[i] = new Vector3(Mathf.Cos(angulo), Mathf.Sin(angulo), 0f) * (1.1f + i % 3 * 0.3f);
            escalas[i] = escalaRelativa * (0.10f + i % 3 * 0.025f);
            fragmento.transform.localPosition = centroFragmentos;
            fragmento.transform.localScale = escalas[i];
            fragmentos[i] = fragmento;
        }
    }

    private void Concluir()
    {
        if (concluido) return;
        concluido = true;
        // Todos os fragmentos são filhos da bola e usam somente sprites/materiais compartilhados.
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        // Desativar apenas o componente não pode deixar uma bola condenada presa na arena.
        if (iniciado && !concluido) Concluir();
    }

    private static float Seguro(float valor, float padrao, float minimo, float maximo)
    {
        return float.IsNaN(valor) || float.IsInfinity(valor) ? padrao : Mathf.Clamp(valor, minimo, maximo);
    }
}
