using UnityEngine;

/// <summary>Independent metallic colors for the helix body and its two boots.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class KickerAppearance : MonoBehaviour
{
    [Header("Visual references")]
    public SpriteRenderer helixRenderer;
    public SpriteRenderer[] bootRenderers = new SpriteRenderer[0];
    public Material fallbackPaletteMaterial;

    [Header("Color controls")]
    [Tooltip("Enable to override the competition palette for this kicker only.")]
    public bool useCustomColors;

    [ColorUsage(false)]
    public Color helixColor = new Color(0.48f, 0.75f, 1f, 1f);

    [ColorUsage(false)]
    public Color bootColor = new Color(1f, 0.7f, 0.2f, 1f);

    private static readonly int DarkColor = Shader.PropertyToID("_DarkColor");
    private static readonly int MidColor = Shader.PropertyToID("_MidColor");
    private static readonly int LightColor = Shader.PropertyToID("_LightColor");
    private static readonly int Strength = Shader.PropertyToID("_Strength");

    private Material themeMaterial;
    private Color themeTint = Color.white;
    private bool hasTheme;
    private MaterialPropertyBlock properties;

    private void OnEnable()
    {
        RefreshAppearance();
    }

    private void OnDisable()
    {
        CancelEditorRefresh();
        ApplyAppearance(false);
    }

    private void OnDestroy()
    {
        CancelEditorRefresh();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        // OnValidate can run while assets are loading. Renderer work waits for
        // the editor's main thread, and repeated picker changes share one call.
        UnityEditor.EditorApplication.delayCall -= RefreshFromEditor;
        UnityEditor.EditorApplication.delayCall += RefreshFromEditor;
#endif
    }

    /// <summary>Called after the competition applies its material and tint.</summary>
    public void ApplyTheme(Material material, Color tint)
    {
        themeMaterial = ResolvePaletteMaterial(material);
        themeTint = tint;
        hasTheme = true;
        RefreshAppearance();
    }

    /// <summary>Also available to scripts without opening the Inspector.</summary>
    public void SetColors(Color helix, Color boots)
    {
        RecordEditorUndo("Change Kicker Colors");
        helixColor = Opaque(helix);
        bootColor = Opaque(boots);
        useCustomColors = true;
        RefreshAppearance();
        MarkEditorChange();
    }

    [ContextMenu("Apply Custom Colors")]
    public void ApplyCustomColors()
    {
        RecordEditorUndo("Apply Custom Kicker Colors");
        useCustomColors = true;
        RefreshAppearance();
        MarkEditorChange();
    }

    [ContextMenu("Use Theme Colors")]
    public void UseThemeColors()
    {
        RecordEditorUndo("Use Kicker Theme Colors");
        useCustomColors = false;
        RefreshAppearance();
        MarkEditorChange();
    }

    public void RefreshAppearance()
    {
        ApplyAppearance(useCustomColors && isActiveAndEnabled);
    }

    private void ApplyAppearance(bool custom)
    {
        if (helixRenderer == null)
            helixRenderer = GetComponent<SpriteRenderer>();

        if (!hasTheme)
        {
            themeMaterial = helixRenderer != null
                ? helixRenderer.sharedMaterial : null;
            themeTint = helixRenderer != null
                ? helixRenderer.color : Color.white;
            hasTheme = true;
        }

        Material material = ResolvePaletteMaterial(themeMaterial);
        ApplyRenderer(helixRenderer, material, helixColor, custom);

        if (bootRenderers == null) return;
        foreach (SpriteRenderer renderer in bootRenderers)
            ApplyRenderer(renderer, material, bootColor, custom);
    }

    private void ApplyRenderer(
        SpriteRenderer renderer, Material material, Color color, bool custom)
    {
        if (renderer == null) return;
        if (material != null) renderer.sharedMaterial = material;

        if (!HasPalette(material))
        {
            // A plain grayscale sprite can still be tinted if no palette was set.
            renderer.color = custom ? Opaque(color) : themeTint;
            return;
        }

        renderer.color = custom ? Color.white : themeTint;
        if (properties == null) properties = new MaterialPropertyBlock();

        // Preserve properties owned by other components. Only the four palette
        // entries below belong to this component; shared materials stay intact.
        renderer.GetPropertyBlock(properties);
        Color primary = Opaque(color);
        properties.SetColor(DarkColor, custom
            ? Color.Lerp(Color.black, primary, 0.18f) : material.GetColor(DarkColor));
        properties.SetColor(MidColor, custom
            ? primary : material.GetColor(MidColor));
        properties.SetColor(LightColor, custom
            ? Color.Lerp(primary, Color.white, 0.72f) : material.GetColor(LightColor));
        properties.SetFloat(Strength, custom ? 1f : material.GetFloat(Strength));
        renderer.SetPropertyBlock(properties);
    }

    private Material ResolvePaletteMaterial(Material candidate)
    {
        return HasPalette(candidate) ? candidate
            : HasPalette(fallbackPaletteMaterial) ? fallbackPaletteMaterial
            : candidate;
    }

    private static bool HasPalette(Material material)
    {
        return material != null && material.HasProperty(DarkColor) &&
            material.HasProperty(MidColor) && material.HasProperty(LightColor) &&
            material.HasProperty(Strength);
    }

    private static Color Opaque(Color color)
    {
        color.a = 1f;
        return color;
    }

    private void CancelEditorRefresh()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= RefreshFromEditor;
#endif
    }

#if UNITY_EDITOR
    private void RefreshFromEditor()
    {
        if (this != null) RefreshAppearance();
    }
#endif

    private void RecordEditorUndo(string label)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.Undo.RecordObject(this, label);
#endif
    }

    private void MarkEditorChange()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) return;
        UnityEditor.EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }
}
