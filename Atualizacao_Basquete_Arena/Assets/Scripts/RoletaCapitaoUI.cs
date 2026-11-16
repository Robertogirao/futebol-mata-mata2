using UnityEngine;
using UnityEngine.UI;

/// <summary>Two true-probability sectors inside a metallic dial. Zero points upward.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RoletaCapitaoUI : Image
{
    public const float ProporcaoRaioSetores = .82f;
    private const int Segmentos = 96;
    private float chance = .5f;
    public override Texture mainTexture { get { return Texture2D.whiteTexture; } }

    public void DefinirChance(float valor)
    {
        valor = Mathf.Clamp01(valor);
        if (Mathf.Approximately(chance, valor)) return;
        chance = valor;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        float raio = Mathf.Min(r.width, r.height) * .5f;
        if (raio <= 0f) return;
        Vector2 centro = r.center;

        // Only these shaded face triangles encode the actual HIT/MISS proportions.
        Setor(vh, centro, raio * ProporcaoRaioSetores, 0f, chance,
            new Color(.08f, .52f, .27f, 1f), new Color(.16f, .83f, .46f, 1f));
        Setor(vh, centro, raio * ProporcaoRaioSetores, chance, 1f,
            new Color(.72f, .12f, .13f, 1f), new Color(.96f, .25f, .20f, 1f));

        // Opaque rings, softly shaded as metal rather than a flashing glow.
        Anel(vh, centro, raio, .82f, .852f, new Color(.025f, .045f, .075f, 1f), new Color(.06f, .09f, .13f, 1f));
        Anel(vh, centro, raio, .852f, .94f, new Color(.36f, .25f, .09f, 1f), new Color(1f, .87f, .51f, 1f));
        Anel(vh, centro, raio, .94f, .974f, new Color(.025f, .04f, .065f, 1f), new Color(.12f, .15f, .18f, 1f));
        Anel(vh, centro, raio, .974f, 1f, new Color(.48f, .34f, .13f, 1f), new Color(1f, .9f, .63f, 1f));
        for (int i = 0; i < 48; i++)
        {
            bool maior = i % 4 == 0;
            float angulo = i / 48f;
            float metade = maior ? .0033f : .0017f;
            Faixa(vh, centro, raio * (maior ? .866f : .895f), raio * .927f,
                angulo - metade, angulo + metade,
                maior ? new Color(.98f, .91f, .66f, 1f) : new Color(.17f, .13f, .065f, 1f));
        }

        Divisor(vh, centro, raio, 0f);
        if (chance > 0f && chance < 1f) Divisor(vh, centro, raio, chance);
        Anel(vh, centro, raio, .135f, .18f, new Color(.44f, .29f, .075f, 1f), new Color(1f, .88f, .48f, 1f));
        Anel(vh, centro, raio, 0f, .135f, new Color(.025f, .045f, .075f, 1f), new Color(.10f, .14f, .19f, 1f));
    }

    private static void Setor(VertexHelper vh, Vector2 centro, float raio, float inicio, float fim, Color interna, Color externa)
    {
        if (fim <= inicio) return;
        Color meio = Color.Lerp(interna, externa, .40f);
        for (int i = 0; i < Segmentos; i++)
        {
            float a = Mathf.Lerp(inicio, fim, i / (float)Segmentos);
            float b = Mathf.Lerp(inicio, fim, (i + 1f) / Segmentos);
            int n = vh.currentVertCount;
            Vertice(vh, centro, interna);
            Vertice(vh, Ponto(centro, raio * .48f, a), meio);
            Vertice(vh, Ponto(centro, raio * .48f, b), meio);
            vh.AddTriangle(n, n + 1, n + 2);
            n = vh.currentVertCount;
            Vertice(vh, Ponto(centro, raio * .48f, a), meio);
            Vertice(vh, Ponto(centro, raio, a), externa);
            Vertice(vh, Ponto(centro, raio, b), externa);
            Vertice(vh, Ponto(centro, raio * .48f, b), meio);
            vh.AddTriangle(n, n + 1, n + 2);
            vh.AddTriangle(n, n + 2, n + 3);
        }
    }

    private static void Anel(VertexHelper vh, Vector2 centro, float raio, float interno, float externo, Color sombra, Color luz)
    {
        for (int i = 0; i < Segmentos; i++)
        {
            float a = i / (float)Segmentos, b = (i + 1f) / Segmentos;
            float reflexo = .5f + .5f * Mathf.Cos((a + .12f) * Mathf.PI * 2f);
            Faixa(vh, centro, raio * interno, raio * externo, a, b, Color.Lerp(sombra, luz, reflexo));
        }
    }

    private static void Divisor(VertexHelper vh, Vector2 centro, float raio, float volta)
    {
        Faixa(vh, centro, raio * .18f, raio * ProporcaoRaioSetores,
            volta - .0022f, volta + .0022f, new Color(.98f, .91f, .66f, 1f));
    }

    private static void Faixa(VertexHelper vh, Vector2 centro, float interno, float externo, float a, float b, Color cor)
    {
        int n = vh.currentVertCount;
        Vertice(vh, Ponto(centro, interno, a), cor);
        Vertice(vh, Ponto(centro, externo, a), cor);
        Vertice(vh, Ponto(centro, externo, b), cor);
        Vertice(vh, Ponto(centro, interno, b), cor);
        vh.AddTriangle(n, n + 1, n + 2);
        vh.AddTriangle(n, n + 2, n + 3);
    }

    private static Vector2 Ponto(Vector2 centro, float raio, float volta)
    {
        float a = volta * Mathf.PI * 2f;
        return centro + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * raio;
    }

    private static void Vertice(VertexHelper vh, Vector2 ponto, Color cor)
    {
        vh.AddVert(ponto, cor, new Vector2(.5f, .5f));
    }
}
