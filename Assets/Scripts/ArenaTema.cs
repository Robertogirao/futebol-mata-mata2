using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Aplica somente a aparência da arena. O controlador escolhe o tema e chama
/// Aplicar antes da abertura, sem alterar componentes ou parâmetros de jogo.
/// </summary>
[DisallowMultipleComponent]
public sealed class ArenaTema : MonoBehaviour
{
    [Header("Renderizadores da arena")]
    public SpriteRenderer fundoArena;
    public SpriteRenderer campo;
    public SpriteRenderer gol;
    public SpriteRenderer helice2;
    public SpriteRenderer goleiro;

    [Tooltip("Procura referências vazias pelos nomes originais, somente nesta cena.")]
    public bool localizarAutomaticamente = true;

    private struct AparenciaOriginal
    {
        public Sprite sprite;
        public Color cor;
        public Material material;

        public AparenciaOriginal(SpriteRenderer renderizador)
        {
            sprite = renderizador.sprite;
            cor = renderizador.color;
            material = renderizador.sharedMaterial;
        }
    }

    private readonly Dictionary<SpriteRenderer, AparenciaOriginal> originais =
        new Dictionary<SpriteRenderer, AparenciaOriginal>();

    public void Aplicar(TemaCompeticao tema)
    {
        if (tema == null)
            return;

        LocalizarReferencias();

        AplicarAparencia(fundoArena, tema.fundoArena, tema.tintFundoArena);
        AplicarAparencia(campo, tema.campo, tema.tintCampo);
        AplicarAparencia(gol, tema.gol, tema.tintGol, tema.materialGol);
        AplicarAparencia(helice2, tema.helice2, tema.tintHelice, tema.materialHelice);
        AplicarAparencia(goleiro, tema.goleiro, tema.tintGoleiro, tema.materialGoleiro);
    }

    /// <summary>
    /// Restaura os valores capturados na primeira aplicação a cada renderizador.
    /// Desabilitar este componente não desfaz a aparência da competição.
    /// </summary>
    public void Restaurar()
    {
        foreach (KeyValuePair<SpriteRenderer, AparenciaOriginal> item in originais)
        {
            if (item.Key == null)
                continue;

            item.Key.sprite = item.Value.sprite;
            item.Key.color = item.Value.cor;
            item.Key.sharedMaterial = item.Value.material;
        }
    }

    private void AplicarAparencia(
        SpriteRenderer renderizador,
        Sprite sprite,
        Color cor,
        Material material = null)
    {
        if (renderizador == null)
            return;

        AparenciaOriginal original;
        if (!originais.TryGetValue(renderizador, out original))
        {
            original = new AparenciaOriginal(renderizador);
            originais.Add(renderizador, original);
        }

        // Cada tema parte da aparência original, mesmo após outra aplicação.
        // Sprites com outro tamanho/pivô não podem deslocar a arte da colisão.
        if (sprite != null && !PossuiMesmosLimites(sprite, original.sprite))
        {
            Debug.LogWarning(
                "ArenaTema: o sprite de '" + renderizador.name +
                "' tem limites diferentes do original. A arena usará " +
                "o sprite original com as cores e o material do tema.",
                this);
            sprite = null;
        }

        renderizador.sprite = sprite != null ? sprite : original.sprite;
        renderizador.color = cor;
        renderizador.sharedMaterial = material != null ? material : original.material;
    }

    private static bool PossuiMesmosLimites(Sprite sprite, Sprite original)
    {
        if (sprite == original)
            return true;

        if (original == null)
            return false;

        Bounds esperado = original.bounds;
        Bounds recebido = sprite.bounds;
        const float tolerancia = 0.001f;

        return (esperado.size - recebido.size).sqrMagnitude <= tolerancia * tolerancia &&
            (esperado.center - recebido.center).sqrMagnitude <= tolerancia * tolerancia;
    }

    private void LocalizarReferencias()
    {
        if (!localizarAutomaticamente ||
            (fundoArena != null && campo != null && gol != null &&
             helice2 != null && goleiro != null))
        {
            return;
        }

        // Inclui objetos inativos sem procurar em outras cenas ou em assets.
        GameObject[] raizes = gameObject.scene.GetRootGameObjects();
        List<SpriteRenderer> renderizadores = new List<SpriteRenderer>();

        foreach (GameObject raiz in raizes)
            renderizadores.AddRange(raiz.GetComponentsInChildren<SpriteRenderer>(true));

        if (fundoArena == null)
            fundoArena = Procurar(renderizadores, "Fundo_Gramado");
        if (campo == null)
            campo = Procurar(renderizadores, "CAMPO_0");
        if (gol == null)
            gol = Procurar(renderizadores, "GOL_00000_0");
        if (helice2 == null)
            helice2 = Procurar(renderizadores, "HELICE2_00000_0");
        if (goleiro == null)
        {
            goleiro = Procurar(renderizadores, "Goleiro_0");
            if (goleiro == null)
                goleiro = Procurar(renderizadores, "Goleiro_2D");
        }
    }

    private static SpriteRenderer Procurar(
        List<SpriteRenderer> renderizadores,
        string nome)
    {
        SpriteRenderer inativo = null;

        foreach (SpriteRenderer renderizador in renderizadores)
        {
            if (renderizador.gameObject.name != nome)
                continue;

            if (renderizador.gameObject.activeInHierarchy)
                return renderizador;

            if (inativo == null)
                inativo = renderizador;
        }

        return inativo;
    }
}
