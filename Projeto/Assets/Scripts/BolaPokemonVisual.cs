using UnityEngine;

/// <summary>Portrait medallion, independent from the existing ball's collision and rigidbody.</summary>
[DisallowMultipleComponent]
public sealed class BolaPokemonVisual : MonoBehaviour
{
    [Range(0.3f, 0.95f)] public float ocupacaoRetrato = 0.72f;
    public bool manterRetratoEmPe = true;

    private SpriteRenderer retrato;
    private SpriteRenderer aro;

    public void Configurar(BallController bola)
    {
        if (bola == null || bola.visualBola == null) return;
        if (bola.textoNumeroCamisa != null) bola.textoNumeroCamisa.gameObject.SetActive(false);
        Sprite foto = bola.dadosJogador != null
            ? bola.dadosJogador.fotoBola != null ? bola.dadosJogador.fotoBola : bola.dadosJogador.foto512x512
            : null;
        if (foto == null)
        {
            if (retrato != null) retrato.enabled = false;
            if (aro != null) aro.enabled = false;
            return;
        }

        SpriteRenderer baseBola = bola.visualBola;
        if (retrato == null)
        {
            var objeto = new GameObject("RetratoPokemon");
            objeto.transform.SetParent(baseBola.transform, false);
            retrato = objeto.AddComponent<SpriteRenderer>();
        }
        if (aro == null)
        {
            var objeto = new GameObject("AroPokemon");
            objeto.transform.SetParent(baseBola.transform, false);
            aro = objeto.AddComponent<SpriteRenderer>();
        }

        retrato.sprite = foto;
        retrato.color = Color.white;
        retrato.sortingLayerID = baseBola.sortingLayerID;
        retrato.sortingOrder = baseBola.sortingOrder + 2;
        retrato.sharedMaterial = baseBola.sharedMaterial;
        retrato.enabled = true;
        float diametro = baseBola.sprite != null
            ? Mathf.Min(baseBola.sprite.bounds.size.x, baseBola.sprite.bounds.size.y) : 1f;
        float maiorLado = Mathf.Max(foto.bounds.size.x, foto.bounds.size.y);
        float fator = maiorLado > 0f ? diametro * Mathf.Clamp(ocupacaoRetrato, 0.3f, 0.95f) / maiorLado : 1f;
        retrato.transform.localPosition = Vector3.zero;
        retrato.transform.localScale = new Vector3(fator, fator, 1f);

        aro.sprite = baseBola.sprite;
        aro.color = Color.Lerp(baseBola.color, Color.black, 0.8f);
        aro.sortingLayerID = baseBola.sortingLayerID;
        aro.sortingOrder = baseBola.sortingOrder - 1;
        aro.sharedMaterial = baseBola.sharedMaterial;
        aro.transform.localPosition = Vector3.zero;
        aro.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
        aro.enabled = aro.sprite != null;
    }

    private void LateUpdate()
    {
        if (manterRetratoEmPe && retrato != null)
            retrato.transform.rotation = Quaternion.identity;
    }
}
