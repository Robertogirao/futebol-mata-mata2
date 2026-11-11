using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Eventos neutros para os dois Pokémon, com aviso, efeito e limpeza por partida.</summary>
[DisallowMultipleComponent]
public sealed class EventosPartidaPokemon : MonoBehaviour
{
    [Header("Referências (automáticas)")]
    public AutoBallPipe cano;
    public UniversalUI interfacePartida;
    public PokemonHUD hud;
    public ArenaPokemon arena;

    [Header("Eventos sorteados — Snorlax Rage e Rocket Frenzy")]
    public bool ativarSnorlax = true;
    public bool ativarRocket = true;
    [Tooltip("Máximo de usos somados dos dois especiais originais. O Gigante tem sua própria quantidade abaixo.")]
    [Range(0, 10)] public int eventosPorPartida = 2;
    [Min(2f)] public float intervaloMinimo = 8f;
    [Min(2f)] public float intervaloMaximo = 14f;
    [Range(0.5f, 5f)] public float duracaoAviso = 1.5f;
    [Tooltip("Zero sorteia uma agenda nova por partida. Outro valor repete a agenda na mesma sequência de jogos.")]
    public int sementeEventos = 0;

    [Header("SNORLAX RAGE")]
    [Min(0.5f)] public float raioRepulsao = 4f;
    [Range(1f, 25f)] public float velocidadeRepulsao = 12f;
    [Range(0.4f, 5f)] public float duracaoOnda = 1.2f;
    [Tooltip("Espessura visual e de contato da onda, em unidades do mundo.")]
    [Range(0.02f, 0.5f)] public float larguraOnda = 0.1f;

    [Header("SNORLAX — REPULSÃO E DESINTEGRAÇÃO")]
    [Tooltip("Tempo para a bola recuar após o impacto, antes de começar a se desfazer.")]
    [Range(0f, 2f)] public float atrasoDesintegracao = 0.18f;
    [Range(0.05f, 2f)] public float duracaoDesintegracao = 0.28f;

    [Header("SNORLAX GIGANTE — terceiro especial")]
    public bool ativarSnorlaxGigante = true;
    [Tooltip("Máximo de usos do Gigante, adicional à quantidade dos dois especiais originais.")]
    [Range(0, 10)] public int eventosGigantePorPartida = 1;
    [Range(1f, 15f)] public float duracaoGigante = 5f;
    [Tooltip("Multiplica o tamanho da imagem e do colisor do Snorlax durante o Gigante. 1.8 = 80% maior.")]
    [Range(1f, 4f)] public float multiplicadorTamanhoGigante = 1.8f;

    [Header("ROCKET FRENZY")]
    [Range(1f, 15f)] public float duracaoTrapaca = 5f;
    [Range(1f, 5f)] public float multiplicadorLancamentos = 3f;
    [Range(0, 20)] public int bolasExtras = 6;

    private RitmoEventosPokemon ritmo;
    private int partidaObservada = -1;
    private bool aplicouRajada;
    private LancadorEquipeRocket rocket;
    private SpriteRenderer visualSnorlax;
    private Color corSnorlax;
    private Vector3 escalaSnorlax;
    private Vector3 posicaoSnorlax;
    private Transform raizGigante;
    private Vector3 escalaRaizOriginal;
    private ContatoSnorlaxPokemon contatoSnorlax;
    private LineRenderer onda;
    private Vector3 centroOnda;
    private float raioOndaAnterior;
    private float raioOndaFinal = -1f;
    private bool ondaAtiva;
    private readonly Dictionary<BallController, Vector2> posicoesBolas = new Dictionary<BallController, Vector2>();
    private readonly List<BallController> posicoesVencidas = new List<BallController>();

    private void ResolverReferencias()
    {
        if (hud == null) hud = GetComponent<PokemonHUD>();
        if (arena == null) arena = GetComponent<ArenaPokemon>();
        if (interfacePartida == null) interfacePartida = GetComponent<UniversalUI>();
        if (interfacePartida == null && hud != null) interfacePartida = hud.universalUI;
        if (cano == null)
        {
            TournamentManager motor = GetComponent<TournamentManager>();
            if (motor != null) cano = motor.canoAutomatizado;
            if (cano == null) cano = FindFirstObjectByType<AutoBallPipe>();
        }
        if (rocket == null && cano != null) rocket = cano.GetComponent<LancadorEquipeRocket>();
        if (arena != null && arena.goleiro != null)
        {
            if (contatoSnorlax != null && contatoSnorlax.gameObject != arena.goleiro.gameObject)
                contatoSnorlax.Desvincular(this);
            contatoSnorlax = arena.goleiro.GetComponent<ContatoSnorlaxPokemon>();
            if (contatoSnorlax == null)
                contatoSnorlax = arena.goleiro.gameObject.AddComponent<ContatoSnorlaxPokemon>();
            contatoSnorlax.Vincular(this);
        }
    }

    private bool PartidaAtiva
    {
        get
        {
            return Application.isPlaying && isActiveAndEnabled && Time.timeScale > 0f &&
                cano != null && cano.isActiveAndEnabled && cano.EstaLancando &&
                interfacePartida != null && interfacePartida.partidaEmAndamento &&
                (hud == null || (hud.isActiveAndEnabled && hud.ativarEventosAleatorios));
        }
    }

    private bool DefesaSnorlaxAtiva
    {
        get
        {
            return PartidaAtiva && ritmo != null && ritmo.Etapa == EtapaEventoPokemon.Ativo &&
                (ritmo.Evento == EventoArenaPokemon.SnorlaxRage || ritmo.Evento == EventoArenaPokemon.SnorlaxGigante);
        }
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        ResolverReferencias();
        if (cano == null || !cano.isActiveAndEnabled || !cano.EstaLancando ||
            (hud != null && (!hud.isActiveAndEnabled || !hud.ativarEventosAleatorios)) ||
            interfacePartida == null || !interfacePartida.partidaEmAndamento)
        {
            if (ritmo != null) LimparEventos();
            return;
        }
        if (partidaObservada != cano.IdDaPartida || ritmo == null)
        {
            LimparEventos();
            partidaObservada = cano.IdDaPartida;
            int semente = sementeEventos == 0 ? Guid.NewGuid().GetHashCode() : unchecked(sementeEventos + partidaObservada);
            bool temGoleiro = arena != null && arena.goleiro != null;
            ritmo = new RitmoEventosPokemon(semente,
                ativarSnorlax && temGoleiro, ativarRocket,
                eventosPorPartida, intervaloMinimo, intervaloMaximo, duracaoAviso, duracaoOnda, duracaoTrapaca,
                ativarSnorlaxGigante && temGoleiro, eventosGigantePorPartida, duracaoGigante);
        }

        // Um frame longo não pode encerrar o evento antes de testar a última
        // faixa percorrida pela onda até o alcance máximo configurado.
        if (ondaAtiva && ritmo.Evento == EventoArenaPokemon.SnorlaxRage &&
            ritmo.Etapa == EtapaEventoPokemon.Ativo && PartidaAtiva &&
            Time.deltaTime > 0f && !float.IsInfinity(Time.deltaTime) &&
            Time.deltaTime >= ritmo.Restante)
        {
            raioOndaFinal = Valido(raioRepulsao, 4f, 0.5f, 12f);
            try { ProcessarBolasDaArena(); }
            finally { raioOndaFinal = -1f; }
        }

        bool ativou = ritmo.Avancar(Time.deltaTime);
        if (ativou && ritmo.Evento == EventoArenaPokemon.SnorlaxRage)
        {
            centroOnda = arena.goleiro.transform.position;
            raioOndaAnterior = 0f;
            ondaAtiva = true;
            PrepararOnda();
        }

        bool frenesi = ritmo.Evento == EventoArenaPokemon.RocketFrenzy && ritmo.Etapa == EtapaEventoPokemon.Ativo;
        if (frenesi)
        {
            cano.IniciarRajada(multiplicadorLancamentos, bolasExtras);
            aplicouRajada = true;
        }
        else if (aplicouRajada)
        {
            cano.EncerrarRajada();
            aplicouRajada = false;
        }
        if (rocket != null) rocket.DefinirModoTrapaca(ritmo.Evento == EventoArenaPokemon.RocketFrenzy &&
            (ritmo.Etapa == EtapaEventoPokemon.Aviso || frenesi));
        AtualizarSnorlax();
        ProcessarBolasDaArena();
        if (hud != null) hud.AtualizarEventos(ritmo.Evento, ritmo.Etapa, (float)ritmo.Restante, ritmo.Progresso);
    }

    private void ProcessarBolasDaArena()
    {
        // Nem a onda nem o histórico de posições avançam durante o congelamento.
        if (!PartidaAtiva || Time.deltaTime <= 0f) return;
        BallController[] bolas = FindObjectsByType<BallController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var presentes = new HashSet<BallController>();
        foreach (BallController bola in bolas)
        {
            if (bola == null || bola.jaMarcouGol || bola.EmDesintegracao) continue;
            TentarInterceptarBola(bola);
            if (bola.EmDesintegracao) continue;
            presentes.Add(bola);
            posicoesBolas[bola] = bola.transform.position;
        }
        posicoesVencidas.Clear();
        foreach (var registro in posicoesBolas)
            if (registro.Key == null || !presentes.Contains(registro.Key)) posicoesVencidas.Add(registro.Key);
        foreach (BallController bola in posicoesVencidas) posicoesBolas.Remove(bola);
        if (ondaAtiva) raioOndaAnterior = RaioAtualDaOnda();
    }

    /// <summary>Também usado pelo sensor antes de pontuar, independente da ordem dos callbacks de física.</summary>
    public bool TentarInterceptarBola(BallController bola)
    {
        if (!DefesaSnorlaxAtiva || bola == null || bola.jaMarcouGol || bola.EmDesintegracao ||
            !bola.gameObject.activeInHierarchy || arena == null || arena.goleiro == null) return false;
        Rigidbody2D corpo = bola.GetComponent<Rigidbody2D>();
        if (corpo == null || corpo.bodyType != RigidbodyType2D.Dynamic) return false;
        Vector2 atual = bola.transform.position;
        Vector2 anterior;
        if (!posicoesBolas.TryGetValue(bola, out anterior))
            anterior = atual - corpo.linearVelocity * Mathf.Max(0f, Time.fixedDeltaTime);
        float raioBola = RaioFisicoDaBola(bola);
        bool corpoSnorlax = TocouCorpo(bola, anterior, atual, raioBola);
        bool tocouOnda = ritmo.Evento == EventoArenaPokemon.SnorlaxRage && ondaAtiva &&
            ContatoOndaPokemon.Intercepta(anterior, atual, centroOnda, centroOnda,
                raioOndaAnterior, RaioAtualDaOnda(), raioBola + Valido(larguraOnda, 0.1f, 0.02f, 0.5f) * 0.5f);
        if (!corpoSnorlax && !tocouOnda) return false;
        return RepelirEDesintegrar(bola, corpo, corpoSnorlax ? arena.goleiro.transform.position : centroOnda);
    }

    internal void ProcessarContatoSnorlax(BallController bola)
    {
        if (!DefesaSnorlaxAtiva || bola == null || bola.jaMarcouGol || bola.EmDesintegracao ||
            arena == null || arena.goleiro == null) return;
        Rigidbody2D corpo = bola.GetComponent<Rigidbody2D>();
        if (corpo == null || corpo.bodyType != RigidbodyType2D.Dynamic) return;
        RepelirEDesintegrar(bola, corpo, arena.goleiro.transform.position);
    }

    private bool RepelirEDesintegrar(BallController bola, Rigidbody2D corpo, Vector2 centro)
    {
        Vector2 deslocamento = (Vector2)bola.transform.position - centro;
        float alcance = Mathf.Max(Valido(raioRepulsao, 4f, 0.5f, 12f), deslocamento.magnitude + 0.001f);
        Vector2 impulso = CalcularImpulso(deslocamento, corpo.linearVelocity, corpo.mass, alcance, velocidadeRepulsao);
        // A condenação é independente do impulso: uma bola já saindo rápido também deve se desfazer.
        if (!bola.IniciarDesintegracao(atrasoDesintegracao, duracaoDesintegracao)) return false;
        if (impulso.sqrMagnitude > 0f) corpo.AddForce(impulso, ForceMode2D.Impulse);
        return true;
    }

    private bool TocouCorpo(BallController bola, Vector2 anterior, Vector2 atual, float raioBola)
    {
        Collider2D[] contatos = arena.goleiro.GetComponents<Collider2D>();
        Collider2D[] contatosBola = bola.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D contato in contatos)
        {
            if (contato == null || !contato.enabled || contato.isTrigger || !contato.gameObject.activeInHierarchy) continue;
            foreach (Collider2D contatoBola in contatosBola)
            {
                if (contatoBola == null || !contatoBola.enabled || !contatoBola.gameObject.activeInHierarchy) continue;
                ColliderDistance2D distancia = contato.Distance(contatoBola);
                if (distancia.isValid && distancia.distance <= 0.001f) return true;
            }
        }
        Vector2 movimento = atual - anterior;
        if (movimento.sqrMagnitude <= 0.000001f) return false;
        // Varrimento contínuo evita atravessar o corpo e pontuar num único passo de física.
        foreach (RaycastHit2D toque in Physics2D.CircleCastAll(anterior, raioBola,
            movimento.normalized, movimento.magnitude, ~0))
        {
            foreach (Collider2D contato in contatos)
                if (contato != null && contato.enabled && !contato.isTrigger && toque.collider == contato)
                    return true;
        }
        return false;
    }

    private static float RaioFisicoDaBola(BallController bola)
    {
        float raio = 0f;
        foreach (Collider2D contato in bola.GetComponentsInChildren<Collider2D>())
        {
            if (contato == null || !contato.enabled || contato.isTrigger) continue;
            CircleCollider2D circulo = contato as CircleCollider2D;
            if (circulo != null)
            {
                Vector3 escala = circulo.transform.lossyScale;
                raio = Mathf.Max(raio, circulo.radius * Mathf.Max(Mathf.Abs(escala.x), Mathf.Abs(escala.y)));
            }
            else raio = Mathf.Max(raio, Mathf.Max(contato.bounds.extents.x, contato.bounds.extents.y));
        }
        return Mathf.Max(0.01f, raio);
    }

    internal static Vector2 CalcularImpulso(Vector2 deslocamento, Vector2 velocidade,
        float massa, float raio, float rapidez)
    {
        float distancia = deslocamento.magnitude;
        raio = Valido(raio, 4f, 0.5f, 12f);
        if (float.IsNaN(distancia) || float.IsInfinity(distancia) || distancia > raio ||
            float.IsNaN(velocidade.x) || float.IsInfinity(velocidade.x) ||
            float.IsNaN(velocidade.y) || float.IsInfinity(velocidade.y)) return Vector2.zero;
        Vector2 direcao = distancia > 0.001f ? deslocamento.normalized : new Vector2(-1f, 0f);
        float desejada = Valido(rapidez, 12f, 1f, 25f) * (1f - 0.35f * distancia / raio);
        float adicional = Mathf.Max(0f, desejada - Vector2.Dot(velocidade, direcao));
        return direcao * (adicional * Valido(massa, 1f, 0.01f, 1000f));
    }

    private void PrepararOnda()
    {
        if (onda != null) return;
        var objeto = new GameObject("Snorlax.OndaDeRepulsao");
        objeto.transform.SetParent(transform, false);
        onda = objeto.AddComponent<LineRenderer>();
        onda.useWorldSpace = true;
        onda.loop = true;
        onda.positionCount = 64;
        onda.sortingOrder = 30;
        onda.sortingLayerID = arena.goleiro.sortingLayerID;
        onda.sharedMaterial = arena.materialSprite != null ? arena.materialSprite : arena.goleiro.sharedMaterial;
        onda.enabled = false;
    }

    private float RaioAtualDaOnda()
    {
        if (raioOndaFinal >= 0f) return raioOndaFinal;
        return Valido(raioRepulsao, 4f, 0.5f, 12f) * Mathf.Lerp(0.12f, 1f, ritmo.Progresso);
    }

    private void AtualizarSnorlax()
    {
        bool snorlax = ritmo.Evento == EventoArenaPokemon.SnorlaxRage || ritmo.Evento == EventoArenaPokemon.SnorlaxGigante;
        bool aparecendo = snorlax && (ritmo.Etapa == EtapaEventoPokemon.Aviso || ritmo.Etapa == EtapaEventoPokemon.Ativo);
        if (!aparecendo)
        {
            RestaurarSnorlax();
            return;
        }
        if (visualSnorlax == null && arena != null)
        {
            visualSnorlax = arena.ObterVisualGoleiro();
            if (visualSnorlax != null)
            {
                corSnorlax = visualSnorlax.color;
                escalaSnorlax = visualSnorlax.transform.localScale;
                posicaoSnorlax = visualSnorlax.transform.localPosition;
            }
        }
        bool gigante = ritmo.Evento == EventoArenaPokemon.SnorlaxGigante && ritmo.Etapa == EtapaEventoPokemon.Ativo;
        if (gigante && arena != null && arena.goleiro != null)
        {
            if (raizGigante == null)
            {
                raizGigante = arena.goleiro.transform;
                escalaRaizOriginal = raizGigante.localScale;
            }
            float entrada = Mathf.SmoothStep(0f, 1f, ritmo.Progresso * (float)ritmo.DuracaoEtapa / 0.2f);
            float saida = Mathf.SmoothStep(0f, 1f, (float)ritmo.Restante / 0.2f);
            float fator = Mathf.Lerp(1f, Valido(multiplicadorTamanhoGigante, 1.8f, 1f, 4f), Mathf.Min(entrada, saida));
            Vector3 escala = escalaRaizOriginal * fator;
            if (raizGigante.localScale != escala)
            {
                raizGigante.localScale = escala;
                Physics2D.SyncTransforms();
            }
        }
        else RestaurarEscalaGigante();

        float progresso = ritmo.Progresso;
        float pulso = 0.5f + 0.5f * Mathf.Sin(progresso * 45f);
        if (visualSnorlax != null)
        {
            Color destaque = ritmo.Evento == EventoArenaPokemon.SnorlaxGigante
                ? new Color(0.3f, 0.85f, 1f, corSnorlax.a) : new Color(1f, 0.22f, 0.12f, corSnorlax.a);
            visualSnorlax.color = Color.Lerp(corSnorlax, destaque, 0.35f + pulso * 0.45f);
            visualSnorlax.transform.localScale = escalaSnorlax * (gigante ? 1f : 1f + pulso * 0.06f);
            visualSnorlax.transform.localPosition = posicaoSnorlax + (gigante ? Vector3.zero : new Vector3((pulso - 0.5f) * 0.09f, 0f, 0f));
        }
        bool ativa = ritmo.Evento == EventoArenaPokemon.SnorlaxRage && ritmo.Etapa == EtapaEventoPokemon.Ativo;
        if (!ativa)
        {
            ondaAtiva = false;
            raioOndaAnterior = 0f;
        }
        if (onda == null) return;
        onda.enabled = ativa;
        if (!ativa) return;
        float raio = RaioAtualDaOnda();
        onda.startWidth = onda.endWidth = Valido(larguraOnda, 0.1f, 0.02f, 0.5f);
        Color cor = new Color(1f, 0.42f, 0.12f, 1f - progresso);
        onda.startColor = onda.endColor = cor;
        for (int i = 0; i < onda.positionCount; i++)
        {
            float angulo = i * 2f * Mathf.PI / onda.positionCount;
            onda.SetPosition(i, centroOnda + new Vector3(Mathf.Cos(angulo) * raio, Mathf.Sin(angulo) * raio, 0f));
        }
    }

    private void RestaurarEscalaGigante()
    {
        if (raizGigante == null) return;
        raizGigante.localScale = escalaRaizOriginal;
        raizGigante = null;
        Physics2D.SyncTransforms();
    }

    private void RestaurarSnorlax()
    {
        RestaurarEscalaGigante();
        if (visualSnorlax != null)
        {
            visualSnorlax.color = corSnorlax;
            visualSnorlax.transform.localScale = escalaSnorlax;
            visualSnorlax.transform.localPosition = posicaoSnorlax;
            visualSnorlax = null;
        }
        if (onda != null) onda.enabled = false;
        ondaAtiva = false;
        raioOndaAnterior = 0f;
        raioOndaFinal = -1f;
    }

    public void LimparEventos()
    {
        if (aplicouRajada && cano != null) cano.EncerrarRajada();
        aplicouRajada = false;
        if (rocket != null) rocket.DefinirModoTrapaca(false);
        RestaurarSnorlax();
        posicoesBolas.Clear();
        ritmo = null;
        partidaObservada = -1;
        if (hud != null) hud.AtualizarEventos(EventoArenaPokemon.Nenhum, EtapaEventoPokemon.Concluido, 0f, 0f);
    }

    private void OnDisable()
    {
        LimparEventos();
        if (contatoSnorlax != null) contatoSnorlax.Desvincular(this);
    }

    private void OnDestroy()
    {
        LimparEventos();
        if (contatoSnorlax != null)
        {
            contatoSnorlax.Desvincular(this);
            Destroy(contatoSnorlax);
        }
        if (onda != null) Destroy(onda.gameObject);
    }

    private static float Valido(float valor, float padrao, float min, float max)
    {
        return float.IsNaN(valor) || float.IsInfinity(valor) ? padrao : Mathf.Clamp(valor, min, max);
    }
}
