using UnityEngine;
using System.Collections.Generic;

public enum PosturaTatica
{
    Ataque,
    Balanceado,
    Defesa
}

[System.Serializable]
public class JogadorDados
{
    [Header("Identificação")]
    public string nomeJogador = "Player Name";
    [HideInInspector]
    public int numeroCamisa = 10;
    public Sprite foto512x512;

    // Campos antigos preservados por compatibilidade.
    // O novo lançador não usa habilidade individual.
    [HideInInspector]
    [Range(1, 5)]
    public int estrelasSkill = 3;

    public float ObterMultiplicadorForca(float variacaoMaxima)
    {
        return 1f + (Mathf.Clamp(estrelasSkill, 1, 5) - 3)
            * Mathf.Clamp(variacaoMaxima, 0f, 0.5f) / 2f;
    }
}

[CreateAssetMenu(
    fileName = "NovoTime",
    menuName = "Tournament Engine/Nova Ficha de Equipe",
    order = 1)]
public class TeamData : ScriptableObject
{
    [Header("Dados Básicos da Equipe")]
    public string nomeEquipe = "Team Name";
    public string sigla = "BRA";
    public Sprite escudoOuBandeira;

    [Header("Força do Time")]
    [Range(1, 100)]
    [Tooltip("Comparado ao overall adversário para definir a participação nos lançamentos.")]
    public int overall = 75;

    [Header("Cores Padrão")]
    [InspectorName("Principal 1")]
    public Color corPrincipal = Color.blue;

    [InspectorName("Principal 2 (alternativa)")]
    public Color corSecundaria = Color.white;

    [Header("Tática e IA")]
    public PosturaTatica posturaTatica = PosturaTatica.Balanceado;

    [Header("Elenco Completo")]
    public List<JogadorDados> elenco = new List<JogadorDados>();

    [Header("Jogador representante da partida")]
    [Tooltip("Escolhe um único jogador aleatório do elenco para esta partida.")]
    public bool sortearTitularesAleatorios = false;
    [Tooltip("Usa o primeiro índice configurado abaixo quando o sorteio está desligado.")]
    public bool usarSelecaoManual = false;

    [Tooltip("Índice do representante no elenco: 0, 1, 2... Apenas o primeiro elemento é usado. O campo preserva as fichas antigas.")]
    public int[] indicesTitulares = new int[1] { 0 };

    public static float CalcularChanceLancamentoTimeA(
        TeamData timeA,
        TeamData timeB,
        float vantagemPor20Pontos)
    {
        if (timeA == null || timeB == null)
            return 0.5f;

        int valorA = Mathf.Clamp(timeA.overall, 1, 100);
        int valorB = Mathf.Clamp(timeB.overall, 1, 100);

        float intensidade =
            Mathf.Clamp(vantagemPor20Pontos, 0f, 0.25f);

        float chance =
            0.5f + (valorA - valorB) / 20f * intensidade;

        return Mathf.Clamp(chance, 0.25f, 0.75f);
    }

    public static void EscolherCoresParaPartida(
        TeamData timeA,
        TeamData timeB,
        float limiar,
        out Color corA,
        out Color corB)
    {
        corA = timeA != null ? timeA.corPrincipal : Color.blue;
        corB = timeB != null ? timeB.corPrincipal : Color.red;

        if (timeA == null || timeB == null) return;

        limiar = Mathf.Max(0f, limiar);

        float distanciaPrincipal = DistanciaEntreCores(corA, corB);

        if (distanciaPrincipal >= limiar) return;

        float distanciaAlternativaB = DistanciaEntreCores(
            timeA.corPrincipal,
            timeB.corSecundaria);

        if (distanciaAlternativaB >= limiar)
        {
            corB = timeB.corSecundaria;
            return;
        }

        float distanciaAlternativaA = DistanciaEntreCores(
            timeA.corSecundaria,
            timeB.corPrincipal);

        if (distanciaAlternativaA >= limiar)
        {
            corA = timeA.corSecundaria;
            return;
        }

        float distanciaAmbas = DistanciaEntreCores(
            timeA.corSecundaria,
            timeB.corSecundaria);

        if (distanciaAmbas >= limiar)
        {
            corA = timeA.corSecundaria;
            corB = timeB.corSecundaria;
            return;
        }

        float melhor = distanciaPrincipal;

        if (distanciaAlternativaB > melhor)
        {
            melhor = distanciaAlternativaB;
            corB = timeB.corSecundaria;
        }

        if (distanciaAlternativaA > melhor)
        {
            melhor = distanciaAlternativaA;
            corA = timeA.corSecundaria;
            corB = timeB.corPrincipal;
        }

        if (distanciaAmbas > melhor)
        {
            corA = timeA.corSecundaria;
            corB = timeB.corSecundaria;
        }

        Debug.LogWarning(
            "As duas opções de cor dos times ainda são próximas. " +
            "Foi escolhido o maior contraste disponível; " +
            "cadastre uma alternativa mais distinta.");
    }

    public static float DistanciaEntreCores(Color a, Color b)
    {
        return Vector3.Distance(
            ConverterParaLab(a),
            ConverterParaLab(b));
    }

    private static Vector3 ConverterParaLab(Color cor)
    {
        float r = CanalLinear(cor.r);
        float g = CanalLinear(cor.g);
        float b = CanalLinear(cor.b);

        float x = CurvaLab(
            (0.4124564f * r +
             0.3575761f * g +
             0.1804375f * b) / 0.95047f);

        float y = CurvaLab(
            0.2126729f * r +
            0.7151522f * g +
            0.0721750f * b);

        float z = CurvaLab(
            (0.0193339f * r +
             0.1191920f * g +
             0.9503041f * b) / 1.08883f);

        return new Vector3(
            116f * y - 16f,
            500f * (x - y),
            200f * (y - z));
    }

    private static float CanalLinear(float canal)
    {
        canal = Mathf.Clamp01(canal);

        return canal <= 0.04045f
            ? canal / 12.92f
            : Mathf.Pow((canal + 0.055f) / 1.055f, 2.4f);
    }

    private static float CurvaLab(float valor)
    {
        return valor > 0.008856f
            ? Mathf.Pow(valor, 1f / 3f)
            : 7.787037f * valor + 16f / 116f;
    }

    /// <summary>
    /// Seleciona uma vez o representante usado pela interface e pelo lançador.
    /// O elenco completo continua disponível para escolher outros jogadores.
    /// </summary>
    public List<JogadorDados> ObterTitularesDaPartida()
    {
        var titulares = new List<JogadorDados>(1);
        var disponiveis = new List<JogadorDados>();

        if (elenco != null)
        {
            foreach (JogadorDados jogador in elenco)
            {
                if (jogador != null)
                    disponiveis.Add(jogador);
            }
        }

        if (disponiveis.Count == 0) return titulares;

        JogadorDados escolhido = disponiveis[0];

        if (sortearTitularesAleatorios)
        {
            escolhido = disponiveis[Random.Range(0, disponiveis.Count)];
        }
        else if (usarSelecaoManual &&
                 indicesTitulares != null &&
                 indicesTitulares.Length > 0)
        {
            int indice = indicesTitulares[0];

            if (indice >= 0 &&
                indice < elenco.Count &&
                elenco[indice] != null)
            {
                escolhido = elenco[indice];
            }
        }

        titulares.Add(escolhido);
        return titulares;
    }

    // Compatibilidade com chamadas externas das versões anteriores.
    // A nova regra retorna somente o representante da partida.
    public List<JogadorDados> ObterOsTresTitulares()
    {
        return ObterTitularesDaPartida();
    }
}
