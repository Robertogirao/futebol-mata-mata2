using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FundoIlustradoPokemon
{
    public TipoPokemon tipo;
    public Sprite arena;
    [Tooltip("Opcional. Se vazio, a apresentação usa a mesma imagem da arena.")]
    public Sprite apresentacao;
}

[CreateAssetMenu(fileName = "Biblioteca_Fundos", menuName = "Pokemon/Biblioteca de fundos ilustrados", order = 3)]
public sealed class BibliotecaFundosPokemon : ScriptableObject
{
    private static readonly string[] NomesTipos = { "MIXED", "NORMAL", "FIRE", "WATER", "ELECTRIC", "GRASS", "ICE", "FIGHTING", "POISON", "GROUND", "FLYING", "PSYCHIC", "BUG", "ROCK", "GHOST", "DRAGON", "DARK", "STEEL", "FAIRY" };

    public Sprite fundoMisto;
    public Sprite apresentacaoMista;
    public List<FundoIlustradoPokemon> fundos = new List<FundoIlustradoPokemon>();

    public void ConferirBiblioteca()
    {
        var tipos = new HashSet<TipoPokemon>();
        if (fundoMisto == null) throw new InvalidOperationException("Atribua o fundo do torneio misto na biblioteca.");
        if (fundos == null) throw new InvalidOperationException("A biblioteca de fundos está vazia.");
        foreach (FundoIlustradoPokemon entrada in fundos)
        {
            if (entrada == null || (int)entrada.tipo < 1 || (int)entrada.tipo > 18 || !tipos.Add(entrada.tipo))
                throw new InvalidOperationException("A biblioteca contém um tipo inválido ou repetido.");
            if (entrada.arena == null) throw new InvalidOperationException("Falta a ilustração do tipo " + entrada.tipo + ".");
        }
        if (tipos.Count != 18) throw new InvalidOperationException("Cadastre os fundos dos 18 tipos na biblioteca.");
    }

    public Sprite Obter(TipoPokemon tipo, bool apresentacao = false)
    {
        if (!Enum.IsDefined(typeof(TipoPokemon), tipo)) throw new ArgumentException("Tipo de fundo inválido.");
        if (tipo == TipoPokemon.Nenhum)
        {
            Sprite misto = apresentacao && apresentacaoMista != null ? apresentacaoMista : fundoMisto;
            if (misto != null) return misto;
        }
        else if (fundos != null)
            foreach (FundoIlustradoPokemon entrada in fundos)
                if (entrada != null && entrada.tipo == tipo && entrada.arena != null)
                    return apresentacao && entrada.apresentacao != null ? entrada.apresentacao : entrada.arena;
        throw new InvalidOperationException("Não há fundo ilustrado salvo para " + tipo + ".");
    }

    public void AplicarAoTema(TemaCompeticao tema, bool tituloEmIngles = true)
    {
        if (tema == null) throw new ArgumentNullException(nameof(tema));
        tema.ConferirFiltrosPokemon();
        TipoPokemon tipo = tema.TipoVisualPokemon;
        Sprite arena = Obter(tipo);
        Sprite apresentacao = Obter(tipo, true);
        string titulo = tituloEmIngles ? TituloEmIngles(tema) : tema.nomeCompeticao;
        tema.campo = arena;
        tema.fundoAbertura = apresentacao;
        tema.fundoChaveamento = apresentacao;
        if (tituloEmIngles) tema.nomeCompeticao = titulo;
    }

    public static string TituloEmIngles(TipoPokemon tipo, int geracao = 0)
    {
        int indice = (int)tipo;
        if (indice < 0 || indice >= NomesTipos.Length) throw new ArgumentException("Tipo de título inválido.");
        if (geracao < 0 || geracao > 9) throw new ArgumentException("Geração de título inválida.");
        if (geracao > 0)
            return "GEN " + geracao + (tipo == TipoPokemon.Nenhum ? " CUP" : " " + NomesTipos[indice] + " CUP");
        return NomesTipos[indice] + " CUP";
    }

    public static string TituloEmIngles(TemaCompeticao tema)
    {
        if (tema == null) throw new ArgumentNullException(nameof(tema));
        tema.ConferirFiltrosPokemon();
        if (!tema.usarFiltrosMultiplosPokemon)
            return TituloEmIngles(tema.TipoVisualPokemon, tema.GeracaoVisualPokemon);

        List<TipoPokemon> tipos = FiltrosPokemon.NormalizarTipos(tema.tiposPokemonPermitidos);
        List<int> geracoes = FiltrosPokemon.NormalizarGeracoes(tema.geracoesPokemonPermitidas);
        var partes = new List<string>();
        if (geracoes.Count <= 3)
            partes.Add("GEN " + string.Join("+", geracoes.ConvertAll(g => g.ToString()).ToArray()));
        else if (geracoes.Count < 9)
            partes.Add("MULTI-GEN");

        if (tipos.Count <= 3)
            partes.Add(string.Join("/", tipos.ConvertAll(t => NomesTipos[(int)t]).ToArray()));
        else if (tipos.Count < 18)
            partes.Add("MULTI-TYPE");

        return partes.Count == 0 ? "MIXED CUP" : string.Join(" ", partes.ToArray()) + " CUP";
    }
}
