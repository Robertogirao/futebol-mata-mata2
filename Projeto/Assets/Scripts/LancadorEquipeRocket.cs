using System;
using UnityEngine;

/// <summary>
/// Arte e saída móveis do lançador Pokémon. A escolha dos participantes,
/// o intervalo, o limite de bolas e a pontuação continuam no AutoBallPipe.
/// Não move objetos que tenham os contatos físicos da arena.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AutoBallPipe))]
public sealed class LancadorEquipeRocket : MonoBehaviour
{
    public const string RecursoArte = "EquipeRocket_Lancador";

    [Header("Arte da Equipe Rocket")]
    [Tooltip("Vazio usa a arte instalada em Pokemon/Resources. PNG com transparência, importado como Sprite.")]
    public Sprite arte;
    public Material materialSprite;
    [Min(0.1f)] public float alturaImagem = 1.55f;
    public int ordemNaTela = 20;

    [Header("Faixa horizontal (unidades do mundo)")]
    public Vector2 centroDoPercurso = new Vector2(-7.4f, 3.15f);
    [Tooltip("Distância total entre os extremos. Zero mantém o ponto parado.")]
    [Min(0f)] public float distanciaHorizontal = 1.5f;
    [Tooltip("Velocidade média de uma ponta até a outra. O movimento desacelera nas pontas.")]
    [Min(0f)] public float velocidadeHorizontal = 0.9f;

    [Header("Saída das bolas")]
    [Tooltip("Deslocamento em relação ao centro da imagem. O padrão nasce abaixo do teto físico, dentro da arena.")]
    public Vector2 deslocamentoSaida = new Vector2(0.18f, -0.98f);
    [Tooltip("Impulso igual para os dois participantes, dirigido para baixo e para a direita.")]
    public Vector2 forcaLancamento = new Vector2(3.5f, -2.5f);

    [Header("Gesto de lançamento")]
    [Min(0.01f)] public float duracaoGesto = 0.18f;
    [Range(0f, 0.2f)] public float intensidadeGesto = 0.07f;

    private AutoBallPipe cano;
    private Transform saidaAnterior;
    private Vector2 forcaAnterior;
    private Transform saida;
    private SpriteRenderer imagem;
    private bool conectado;
    private bool avisoEmitido;
    private double tempoMovimento;
    private float tempoGesto;
    private bool gesticulando;
    private bool modoTrapaca;

    public void DefinirModoTrapaca(bool ativo)
    {
        modoTrapaca = ativo;
        if (imagem != null) imagem.color = ativo ? new Color(1f, 0.5f, 0.82f) : Color.white;
    }

    private void OnEnable()
    {
        // Não cria objetos enquanto a cena está apenas sendo configurada.
        if (Application.isPlaying) Conectar();
    }

    private void Conectar()
    {
        if (conectado) return;
        cano = GetComponent<AutoBallPipe>();
        if (arte == null) arte = Resources.Load<Sprite>(RecursoArte);
        if (cano == null || arte == null)
        {
            if (!avisoEmitido)
                Debug.LogWarning("Equipe Rocket: atribua a arte ou instale Pokemon/Resources/EquipeRocket_Lancador.png. O lançamento original foi preservado.", this);
            avisoEmitido = true;
            return;
        }

        if (materialSprite == null)
        {
            ArenaPokemon arena = FindFirstObjectByType<ArenaPokemon>();
            if (arena != null) materialSprite = arena.materialSprite;
        }

        saidaAnterior = cano.pontoSaidaDoCano;
        forcaAnterior = cano.forcaCuspe;
        if (saida == null)
        {
            var objeto = new GameObject("SaidaEquipeRocket");
            objeto.transform.SetParent(transform, false);
            saida = objeto.transform;
        }
        if (imagem == null)
        {
            var objeto = new GameObject("JessieJamesMeowth");
            objeto.transform.SetParent(transform, false);
            imagem = objeto.AddComponent<SpriteRenderer>();
        }
        imagem.sprite = arte;
        if (materialSprite != null) imagem.sharedMaterial = materialSprite;
        imagem.enabled = true;
        DefinirModoTrapaca(false);
        tempoMovimento = 0d;
        gesticulando = false;
        cano.pontoSaidaDoCano = saida;
        cano.BolaLancada += AoLancarBola;
        conectado = true;
        AtualizarPosicoes(0f);
    }

    private void Update()
    {
        if (!conectado) return;
        bool jogando = cano != null && cano.isActiveAndEnabled && cano.EstaLancando &&
            UniversalUI.Instance != null && UniversalUI.Instance.partidaEmAndamento;
        float passo = Time.deltaTime;
        if (jogando && !float.IsNaN(passo) && !float.IsInfinity(passo) && passo > 0f)
        {
            tempoMovimento += passo;
            if (gesticulando)
            {
                tempoGesto += passo;
                if (tempoGesto >= Positivo(duracaoGesto, 0.18f)) gesticulando = false;
            }
        }
        float gesto = gesticulando
            ? (float)Math.Sin(Math.PI * Math.Min(1d, tempoGesto / Positivo(duracaoGesto, 0.18f)))
            : 0f;
        AtualizarPosicoes(gesto);
    }

    private void AtualizarPosicoes(float gesto)
    {
        float deslocamento = DeslocamentoHorizontal(distanciaHorizontal, velocidadeHorizontal, tempoMovimento);
        float x = Finito(centroDoPercurso.x, -7.4f) + deslocamento;
        float y = Finito(centroDoPercurso.y, 3.15f);
        saida.position = new Vector3(x + Finito(deslocamentoSaida.x, 0.18f),
            y + Finito(deslocamentoSaida.y, -0.98f), 0f);
        cano.forcaCuspe = new Vector2(Finito(forcaLancamento.x, 3.5f), Finito(forcaLancamento.y, -2.5f));

        if (imagem == null || arte == null) return;
        imagem.sprite = arte;
        imagem.color = modoTrapaca ? new Color(1f, 0.5f, 0.82f) : Color.white;
        imagem.sortingOrder = ordemNaTela;
        float altura = Positivo(alturaImagem, 1.55f);
        float intensidade = Mathf.Clamp(Finito(intensidadeGesto, 0.07f), 0f, 0.2f) * gesto;
        Vector3 pai = transform.lossyScale;
        Vector3 tamanho = arte.bounds.size;
        float escala = altura / Positivo(tamanho.y, 1f);
        imagem.transform.localScale = new Vector3(
            escala * (1f + intensidade) / Positivo(Mathf.Abs(pai.x), 1f),
            escala * (1f - intensidade) / Positivo(Mathf.Abs(pai.y), 1f), 1f);
        imagem.transform.rotation = Quaternion.identity;
        imagem.transform.position = new Vector3(x, y - intensidade * altura * 0.5f, 0f);
    }

    private void AoLancarBola()
    {
        // A notificação vem depois da criação real: não há gesto se a arena estiver cheia.
        tempoGesto = 0f;
        gesticulando = true;
    }

    // Movimento suave e periódico. Não usa UnityEngine.Random para não alterar
    // os sorteios, a participação dos Pokémon nem a variação de força original.
    internal static float DeslocamentoHorizontal(float distancia, float velocidade, double segundos)
    {
        double largura = Math.Max(0d, Finito(distancia, 0f));
        double rapidez = Math.Max(0d, Finito(velocidade, 0f));
        if (largura <= 0d || rapidez <= 0d || double.IsNaN(segundos) || double.IsInfinity(segundos)) return 0f;
        double periodo = 2d * largura / rapidez;
        double fase = (segundos % periodo) / periodo;
        return (float)(largura * 0.5d * Math.Sin(2d * Math.PI * fase));
    }

    private void OnDisable()
    {
        if (cano != null)
        {
            cano.BolaLancada -= AoLancarBola;
            if (conectado && cano.pontoSaidaDoCano == saida)
            {
                cano.pontoSaidaDoCano = saidaAnterior;
                cano.forcaCuspe = forcaAnterior;
            }
        }
        if (imagem != null) imagem.enabled = false;
        conectado = false;
        gesticulando = false;
        modoTrapaca = false;
    }

    private void OnDestroy()
    {
        if (saida != null) Destroy(saida.gameObject);
        if (imagem != null) Destroy(imagem.gameObject);
    }

    private static float Finito(float valor, float padrao)
    {
        return float.IsNaN(valor) || float.IsInfinity(valor) ? padrao : valor;
    }

    private static float Positivo(float valor, float padrao)
    {
        return valor > 0f && !float.IsInfinity(valor) ? valor : padrao;
    }
}
