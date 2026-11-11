using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public enum TipoPokemon
{
    Nenhum = 0,
    Normal = 1,
    Fogo = 2,
    Agua = 3,
    Eletrico = 4,
    Planta = 5,
    Gelo = 6,
    Lutador = 7,
    Venenoso = 8,
    Terra = 9,
    Voador = 10,
    Psiquico = 11,
    Inseto = 12,
    Pedra = 13,
    Fantasma = 14,
    Dragao = 15,
    Sombrio = 16,
    Aco = 17,
    Fada = 18
}

[CreateAssetMenu(fileName = "NovoPokemon", menuName = "Pokemon/Novo participante", order = 1)]
public sealed class PokemonDados : ScriptableObject
{
    [Header("Identificação")]
    [Min(1)] public int numeroPokedex = 25;
    public string nomePokemon = "Pikachu";
    public TipoPokemon tipoPrimario = TipoPokemon.Eletrico;
    public TipoPokemon tipoSecundario = TipoPokemon.Nenhum;

    [Header("Imagens")]
    public Sprite retrato;
    [Tooltip("Opcional. Um recorte de rosto pode ser usado na bola. Se vazio, usa Retrato.")]
    public Sprite retratoBola;

    [Header("Cores do participante")]
    public Color cor = new Color(1f, 0.82f, 0.08f, 1f);
    public Color alternativa = Color.white;

    [Header("Participação nos lançamentos")]
    [Tooltip("Com força de times ligada no lançador, compara este valor ao adversário. Não cria vantagem por tipo.")]
    [Range(1, 100)] public int overall = 75;

    public string IdEstavel
    {
        get { return "pokemon_" + numeroPokedex.ToString("D4", CultureInfo.InvariantCulture); }
    }

    // Calculada pela espécie: as fichas existentes não precisam ser recadastradas.
    public int Geracao { get { return GeracaoDaEspecie(numeroPokedex); } }

    public static int GeracaoDaEspecie(int numero)
    {
        if (numero < 1 || numero > 1025) return 0;
        if (numero <= 151) return 1;
        if (numero <= 251) return 2;
        if (numero <= 386) return 3;
        if (numero <= 493) return 4;
        if (numero <= 649) return 5;
        if (numero <= 721) return 6;
        if (numero <= 809) return 7;
        if (numero <= 905) return 8;
        return 9;
    }

    public bool PertenceAGeracao(int geracao)
    {
        return geracao == 0 || (geracao >= 1 && geracao <= 9 && Geracao == geracao);
    }

    public bool PertenceAoTipo(TipoPokemon tipo)
    {
        return tipo == TipoPokemon.Nenhum || tipoPrimario == tipo || tipoSecundario == tipo;
    }

    public void ExigirValido()
    {
        if (numeroPokedex <= 0)
            throw new InvalidOperationException("Informe um número da Pokédex positivo.");
        if (string.IsNullOrWhiteSpace(nomePokemon))
            throw new InvalidOperationException("Informe o nome do Pokémon " + numeroPokedex + ".");
        if (retrato == null)
            throw new InvalidOperationException("Atribua o retrato de " + nomePokemon + ".");
        if (!Enum.IsDefined(typeof(TipoPokemon), tipoPrimario) || tipoPrimario == TipoPokemon.Nenhum ||
            !Enum.IsDefined(typeof(TipoPokemon), tipoSecundario))
            throw new InvalidOperationException("Confira os tipos de " + nomePokemon + ".");
        if (overall < 1 || overall > 100)
            throw new InvalidOperationException("A participação de " + nomePokemon + " deve ficar entre 1 e 100.");
    }

    /// <summary>Creates one temporary adapter for the existing physics/match engine.</summary>
    public TeamData CriarFichaDeJogo()
    {
        ExigirValido();
        TeamData ficha = CreateInstance<TeamData>();
        ficha.hideFlags = HideFlags.HideAndDontSave;
        ficha.name = IdEstavel;
        ficha.nomeEquipe = nomePokemon.Trim();
        ficha.sigla = nomePokemon.Trim().ToUpperInvariant();
        ficha.escudoOuBandeira = retrato;
        ficha.overall = overall;
        ficha.corPrincipal = cor;
        ficha.corSecundaria = alternativa;
        ficha.participanteIndividual = true;
        ficha.elenco = new List<JogadorDados>
        {
            new JogadorDados
            {
                nomeJogador = nomePokemon.Trim(),
                numeroCamisa = numeroPokedex,
                foto512x512 = retrato,
                fotoBola = retratoBola != null ? retratoBola : retrato
            }
        };
        return ficha;
    }

}
