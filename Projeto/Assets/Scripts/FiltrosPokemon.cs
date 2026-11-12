using System;
using System.Collections.Generic;

/// <summary>Tipos e gerações selecionados: OU em cada grupo, E entre os grupos.</summary>
public static class FiltrosPokemon
{
    public static List<TipoPokemon> NormalizarTipos(IEnumerable<TipoPokemon> tipos)
    {
        if (tipos == null) throw new ArgumentException("Marque pelo menos um tipo Pokémon.");
        var unicos = new HashSet<TipoPokemon>();
        foreach (TipoPokemon tipo in tipos)
        {
            if ((int)tipo < 1 || (int)tipo > 18 || !Enum.IsDefined(typeof(TipoPokemon), tipo))
                throw new ArgumentException("Marque tipos Pokémon válidos; Nenhum não é um filtro selecionável.");
            unicos.Add(tipo);
        }
        if (unicos.Count == 0) throw new ArgumentException("Marque pelo menos um tipo Pokémon.");
        var resultado = new List<TipoPokemon>(unicos);
        resultado.Sort();
        return resultado;
    }

    public static List<int> NormalizarGeracoes(IEnumerable<int> geracoes)
    {
        if (geracoes == null) throw new ArgumentException("Marque pelo menos uma geração Pokémon.");
        var unicas = new HashSet<int>();
        foreach (int geracao in geracoes)
        {
            if (geracao < 1 || geracao > 9)
                throw new ArgumentException("Marque gerações Pokémon de 1 a 9.");
            unicas.Add(geracao);
        }
        if (unicas.Count == 0) throw new ArgumentException("Marque pelo menos uma geração Pokémon.");
        var resultado = new List<int>(unicas);
        resultado.Sort();
        return resultado;
    }

    public static bool Permite(PokemonDados pokemon, IEnumerable<TipoPokemon> tipos,
        IEnumerable<int> geracoes)
    {
        if (pokemon == null) return false;
        List<TipoPokemon> tiposValidos = NormalizarTipos(tipos);
        List<int> geracoesValidas = NormalizarGeracoes(geracoes);
        return geracoesValidas.Contains(pokemon.Geracao) &&
            (tiposValidos.Contains(pokemon.tipoPrimario) || tiposValidos.Contains(pokemon.tipoSecundario));
    }
}
