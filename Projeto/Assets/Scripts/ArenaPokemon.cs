using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Monta a arte Pokémon em camadas e adapta somente os contatos da nova hélice
/// e do goleiro. Os controladores de movimento e o sensor de pontuação continuam
/// nos objetos originais da cena.
/// </summary>
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public sealed class ArenaPokemon : MonoBehaviour
{
    [Header("Tema e renderizadores da cena")]
    public TemaCompeticao tema;
    public SpriteRenderer campo;
    public SpriteRenderer helice2;
    public SpriteRenderer goleiro;
    public SpriteRenderer gol;
    public SpriteRenderer fundoArena;
    public bool localizarAutomaticamente = true;

    [Tooltip("Material de sprite sem recoloração. Materiais específicos do tema têm prioridade.")]
    public Material materialSprite;

    [Header("Tamanhos em unidades do mundo")]
    public Vector2 tamanhoFundo = new Vector2(23.408f, 12.233f);
    [Tooltip("X = espessura; Y = comprimento total. Ao iniciar o Play, a arte e o contato usam este mesmo tamanho.")]
    public Vector2 tamanhoHelice = new Vector2(0.30f, 3.60f);
    public Vector2 tamanhoGoleiro = new Vector2(1.80f, 2.25f);
    public Vector2 tamanhoPortal = new Vector2(2.80f, 3.50f);
    [Min(0.05f)] public float raioNucleo = 0.40f;

    [Header("Contatos e acabamento")]
    public bool ajustarColisores = true;
    public bool ocultarArteDosLimites = true;

    [Header("Brilho do portal ao pontuar")]
    public bool brilharAoPontuar = true;
    [Range(0.1f, 1.5f)] public float duracaoBrilhoPortal = 0.45f;
    [Range(0f, 1f)] public float intensidadeBrilhoPortal = 0.85f;

    private readonly Dictionary<SpriteRenderer, bool> renderizadoresOriginais =
        new Dictionary<SpriteRenderer, bool>();
    private readonly Dictionary<Collider2D, bool> colisoresOriginais =
        new Dictionary<Collider2D, bool>();
    private readonly Dictionary<string, SpriteRenderer> visuais =
        new Dictionary<string, SpriteRenderer>();
    private readonly Dictionary<SpriteRenderer, Material> materiaisPadrao =
        new Dictionary<SpriteRenderer, Material>();
    private CapsuleCollider2D contatoHelice;
    private CircleCollider2D contatoNucleo;
    private CapsuleCollider2D contatoGoleiro;
    private Color corFundoOriginal;
    private bool capturouCorFundo;
    private SpriteRenderer portalComBrilho;
    private SpriteRenderer haloPortal;
    private Color corPortalEmRepouso;
    private Color corDoHalo;
    private float tempoBrilhoPortal;
    private bool brilhoAtivo;

    public SpriteRenderer ObterVisualGoleiro()
    {
        SpriteRenderer visual;
        return visuais.TryGetValue("GoleiroPokemon", out visual) && visual != null
            ? visual : null;
    }

    private void Awake()
    {
        LigaPokemon liga = GetComponent<LigaPokemon>();
        if (liga != null)
        {
            TemaCompeticao temaLiga = liga.TemaEmUso != null ? liga.TemaEmUso : liga.tema;
            if (temaLiga != null) tema = temaLiga;
        }
        if (tema != null) Aplicar(tema);
    }

    public void Aplicar(TemaCompeticao novoTema)
    {
        if (novoTema == null) return;
        tema = novoTema;
        LocalizarReferencias();
        Restaurar();

        if (fundoArena != null)
        {
            if (!capturouCorFundo)
            {
                corFundoOriginal = fundoArena.color;
                capturouCorFundo = true;
            }
            fundoArena.color = tema.tintFundoArena;
        }

        CriarVisual("CenarioPokemon", campo, tema.campo, tamanhoFundo,
            tema.tintCampo, materialSprite, campo != null ? campo.sortingOrder : -2);
        bool criouHelice = CriarVisual("HeliceDeEnergia", helice2, tema.helice2,
            tamanhoHelice, tema.tintHelice,
            tema.materialHelice != null ? tema.materialHelice : materialSprite, 0);
        bool criouGoleiro = CriarVisual("GoleiroPokemon", goleiro, tema.goleiro,
            tamanhoGoleiro, tema.tintGoleiro,
            tema.materialGoleiro != null ? tema.materialGoleiro : materialSprite, 5);
        CriarVisual("PortalDePontuacao", gol, tema.gol, tamanhoPortal,
            tema.tintGol, tema.materialGol != null ? tema.materialGol : materialSprite, -1);
        CriarVisual("NucleoDaHelice", helice2, tema.centroHelice,
            Vector2.one * (RaioValido(raioNucleo) * 2f), Color.white, materialSprite, 1, false);

        if (ajustarColisores)
        {
            if (criouHelice) ConfigurarContatosHelice();
            if (criouGoleiro) ConfigurarContatoGoleiro();
        }
        if (ocultarArteDosLimites) OcultarArteDosLimites();
    }

    /// <summary>Feedback apenas visual; não altera o portal nem seu sensor.</summary>
    public void PiscarPortal(Color corDoPokemon)
    {
        if (!isActiveAndEnabled || !brilharAoPontuar) return;
        SpriteRenderer portal;
        if (!visuais.TryGetValue("PortalDePontuacao", out portal) ||
            portal == null || !portal.enabled || portal.sprite == null) return;

        // Pontos próximos reiniciam o pulso sem capturar a cor já iluminada.
        if (!brilhoAtivo) corPortalEmRepouso = portal.color;
        portalComBrilho = portal;
        if (haloPortal == null)
        {
            var objeto = new GameObject("HaloDeAbsorcao");
            objeto.transform.SetParent(portal.transform, false);
            haloPortal = objeto.AddComponent<SpriteRenderer>();
        }
        haloPortal.sprite = portal.sprite;
        haloPortal.sharedMaterial = portal.sharedMaterial;
        haloPortal.sortingLayerID = portal.sortingLayerID;
        haloPortal.sortingOrder = portal.sortingOrder + 1;
        haloPortal.transform.localPosition = Vector3.zero;
        haloPortal.transform.localRotation = Quaternion.identity;
        corDoHalo = Color.Lerp(Color.white, corDoPokemon, 0.25f);
        tempoBrilhoPortal = 0f;
        brilhoAtivo = true;
        DesenharBrilhoPortal(0f);
    }

    private void Update()
    {
        if (!brilhoAtivo) return;
        if (!brilharAoPontuar || portalComBrilho == null || !portalComBrilho.enabled)
        {
            EncerrarBrilhoPortal();
            return;
        }

        // Continua no ponto decisivo, mesmo com a partida congelada.
        // captureDeltaTime mantém a duração consistente ao gravar com Recorder.
        float passo = Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;
        if (!float.IsNaN(passo) && !float.IsInfinity(passo) && passo > 0f)
            tempoBrilhoPortal += passo;
        float progresso = Mathf.Clamp01(tempoBrilhoPortal / ValorPositivo(duracaoBrilhoPortal, 0.45f));
        if (progresso >= 1f) EncerrarBrilhoPortal();
        else DesenharBrilhoPortal(progresso);
    }

    private void DesenharBrilhoPortal(float progresso)
    {
        float intensidade = Mathf.Clamp01(intensidadeBrilhoPortal);
        float luz = (1f - progresso) * (1f - progresso);
        portalComBrilho.color = Color.Lerp(corPortalEmRepouso, Color.white, luz * intensidade);
        Color cor = corDoHalo;
        cor.a = (1f - progresso) * intensidade * 0.75f;
        haloPortal.color = cor;
        // O halo converge para o centro, sugerindo a absorção da bola.
        haloPortal.transform.localScale = Vector3.one * Mathf.Lerp(1.16f, 0.92f, progresso);
        haloPortal.enabled = true;
    }

    private void EncerrarBrilhoPortal()
    {
        if (brilhoAtivo && portalComBrilho != null) portalComBrilho.color = corPortalEmRepouso;
        if (haloPortal != null) haloPortal.enabled = false;
        brilhoAtivo = false;
        tempoBrilhoPortal = 0f;
    }

    /// <summary>Restaura o estado capturado antes da primeira aplicação.</summary>
    public void Restaurar()
    {
        EncerrarBrilhoPortal();
        foreach (KeyValuePair<SpriteRenderer, bool> item in renderizadoresOriginais)
            if (item.Key != null) item.Key.enabled = item.Value;
        foreach (KeyValuePair<Collider2D, bool> item in colisoresOriginais)
            if (item.Key != null) item.Key.enabled = item.Value;
        foreach (SpriteRenderer visual in visuais.Values)
            if (visual != null) visual.enabled = false;
        if (contatoHelice != null) contatoHelice.enabled = false;
        if (contatoNucleo != null) contatoNucleo.enabled = false;
        if (contatoGoleiro != null) contatoGoleiro.enabled = false;
        if (capturouCorFundo && fundoArena != null) fundoArena.color = corFundoOriginal;
    }

    private bool CriarVisual(string nome, SpriteRenderer origem, Sprite sprite,
        Vector2 tamanhoMundo, Color cor, Material material, int ordem, bool ocultarOrigem = true)
    {
        if (origem == null || sprite == null) return false;
        SpriteRenderer visual;
        if (!visuais.TryGetValue(nome, out visual) || visual == null)
        {
            var objeto = new GameObject(nome);
            objeto.transform.SetParent(origem.transform, false);
            visual = objeto.AddComponent<SpriteRenderer>();
            materiaisPadrao.Add(visual, visual.sharedMaterial);
            visuais[nome] = visual;
        }
        Vector3 escalaPai = origem.transform.lossyScale;
        Vector3 limites = sprite.bounds.size;
        Vector2 tamanho = TamanhoValido(tamanhoMundo);
        float x = tamanho.x / (EscalaValida(escalaPai.x) * Mathf.Max(0.0001f, limites.x));
        float y = tamanho.y / (EscalaValida(escalaPai.y) * Mathf.Max(0.0001f, limites.y));
        visual.transform.localScale = new Vector3(x, y, 1f);
        Vector3 centro = sprite.bounds.center;
        visual.transform.localPosition = new Vector3(-centro.x * x, -centro.y * y, 0f);
        visual.transform.localRotation = Quaternion.identity;
        visual.sprite = sprite;
        visual.color = cor;
        visual.sharedMaterial = material != null ? material : materiaisPadrao[visual];
        visual.sortingLayerID = origem.sortingLayerID;
        visual.sortingOrder = ordem;
        visual.enabled = true;
        if (ocultarOrigem) Ocultar(origem);
        return true;
    }

    private void ConfigurarContatosHelice()
    {
        PhysicsMaterial2D material = DesativarContatosOriginais(helice2);
        if (contatoHelice == null) contatoHelice = helice2.gameObject.AddComponent<CapsuleCollider2D>();
        contatoHelice.offset = Vector2.zero;
        contatoHelice.size = ParaTamanhoLocal(helice2.transform, tamanhoHelice);
        contatoHelice.direction = contatoHelice.size.y >= contatoHelice.size.x
            ? CapsuleDirection2D.Vertical : CapsuleDirection2D.Horizontal;
        contatoHelice.isTrigger = false;
        contatoHelice.sharedMaterial = material;
        contatoHelice.enabled = true;

        if (tema.centroHelice != null)
        {
            if (contatoNucleo == null) contatoNucleo = helice2.gameObject.AddComponent<CircleCollider2D>();
            Vector3 escala = helice2.transform.lossyScale;
            contatoNucleo.radius = RaioValido(raioNucleo) /
                Mathf.Max(EscalaValida(escala.x), EscalaValida(escala.y));
            contatoNucleo.offset = Vector2.zero;
            contatoNucleo.isTrigger = false;
            contatoNucleo.sharedMaterial = material;
            contatoNucleo.enabled = true;
        }
    }

    private void ConfigurarContatoGoleiro()
    {
        PhysicsMaterial2D material = DesativarContatosOriginais(goleiro);
        if (contatoGoleiro == null) contatoGoleiro = goleiro.gameObject.AddComponent<CapsuleCollider2D>();
        contatoGoleiro.offset = Vector2.zero;
        contatoGoleiro.size = ParaTamanhoLocal(goleiro.transform, tamanhoGoleiro);
        contatoGoleiro.direction = contatoGoleiro.size.y >= contatoGoleiro.size.x
            ? CapsuleDirection2D.Vertical : CapsuleDirection2D.Horizontal;
        contatoGoleiro.isTrigger = false;
        contatoGoleiro.sharedMaterial = material;
        contatoGoleiro.enabled = true;
    }

    private PhysicsMaterial2D DesativarContatosOriginais(SpriteRenderer origem)
    {
        PhysicsMaterial2D material = null;
        // Only contacts attached to this actor are replaced. Portal/sensor and
        // arena-wall colliders are never searched or disabled here.
        foreach (Collider2D colisor in origem.GetComponents<Collider2D>())
        {
            if (colisor == contatoHelice || colisor == contatoNucleo || colisor == contatoGoleiro) continue;
            if (!colisoresOriginais.ContainsKey(colisor)) colisoresOriginais.Add(colisor, colisor.enabled);
            if (material == null) material = colisor.sharedMaterial;
            colisor.enabled = false;
        }
        return material;
    }

    private static Vector2 ParaTamanhoLocal(Transform pai, Vector2 tamanhoMundo)
    {
        Vector2 tamanho = TamanhoValido(tamanhoMundo);
        Vector3 escala = pai.lossyScale;
        return new Vector2(tamanho.x / EscalaValida(escala.x), tamanho.y / EscalaValida(escala.y));
    }

    private static float EscalaValida(float valor)
    {
        return float.IsNaN(valor) || float.IsInfinity(valor) ? 1f : Mathf.Max(0.0001f, Mathf.Abs(valor));
    }

    private static Vector2 TamanhoValido(Vector2 tamanho)
    {
        return new Vector2(ValorPositivo(tamanho.x, 1f), ValorPositivo(tamanho.y, 1f));
    }

    private static float RaioValido(float raio) { return ValorPositivo(raio, 0.4f); }

    private static float ValorPositivo(float valor, float padrao)
    {
        return float.IsNaN(valor) || float.IsInfinity(valor) || valor <= 0f ? padrao : valor;
    }

    private void Ocultar(SpriteRenderer renderizador)
    {
        if (!renderizadoresOriginais.ContainsKey(renderizador))
            renderizadoresOriginais.Add(renderizador, renderizador.enabled);
        renderizador.enabled = false;
    }

    private void LocalizarReferencias()
    {
        if (!localizarAutomaticamente) return;
        foreach (GameObject raiz in gameObject.scene.GetRootGameObjects())
            foreach (SpriteRenderer renderizador in raiz.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string nome = renderizador.gameObject.name;
                if (campo == null && nome == "CAMPO_0") campo = renderizador;
                if (helice2 == null && nome == "HELICE2_00000_0") helice2 = renderizador;
                if (goleiro == null && nome == "Goleiro_0") goleiro = renderizador;
                if (gol == null && nome == "GOL_00000_0") gol = renderizador;
                if (fundoArena == null && nome == "Fundo_Gramado") fundoArena = renderizador;
            }
    }

    private void OcultarArteDosLimites()
    {
        foreach (GameObject raiz in gameObject.scene.GetRootGameObjects())
            foreach (SpriteRenderer renderizador in raiz.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string nome = renderizador.gameObject.name;
                if (nome == "Chao" || nome == "Teto" || nome == "Parede_Esquerda" || nome == "Parede_Direita")
                    Ocultar(renderizador);
            }
    }

    private void OnDisable() { Restaurar(); }
}
