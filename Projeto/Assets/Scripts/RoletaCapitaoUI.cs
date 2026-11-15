using UnityEngine;
using UnityEngine.UI;

/// <summary>Roleta com setores proporcionais à chance real. Zero graus aponta para cima.</summary>
public sealed class RoletaCapitaoUI : Graphic
{
    private float chance = .5f;
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
        for (int setor = 0; setor < 2; setor++)
        {
            float inicio = setor == 0 ? 0f : chance;
            float fim = setor == 0 ? chance : 1f;
            Color cor = setor == 0 ? new Color(.12f, .75f, .4f) : new Color(.9f, .22f, .18f);
            for (int i = 0; i < 64; i++)
            {
                float a = Mathf.Lerp(inicio, fim, i / 64f) * Mathf.PI * 2f;
                float b = Mathf.Lerp(inicio, fim, (i + 1) / 64f) * Mathf.PI * 2f;
                int n = vh.currentVertCount;
                vh.AddVert(r.center, cor, Vector2.zero);
                vh.AddVert(r.center + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * raio, cor, Vector2.zero);
                vh.AddVert(r.center + new Vector2(Mathf.Sin(b), Mathf.Cos(b)) * raio, cor, Vector2.zero);
                vh.AddTriangle(n, n + 1, n + 2);
            }
        }
    }
}
