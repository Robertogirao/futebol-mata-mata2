using System;
using System.Collections.Generic;
using System.Text;

/// <summary>Calendário de pontos corridos, sem dependência do Unity.</summary>
public static class LigaPokemonModelo
{
    [Serializable]
    public sealed class Jogo
    {
        public string a;
        public string b;
    }

    [Serializable]
    public sealed class Rodada
    {
        public int numero;
        public string folga = "";
        public List<Jogo> jogos = new List<Jogo>();
    }

    [Serializable]
    public sealed class Calendario
    {
        public int versao = 1;
        public int quantidadeRodadas;
        public int semente;
        public List<string> participantes = new List<string>();
        public List<Rodada> rodadas = new List<Rodada>();
    }

    public static int RodadasPorTurno(int participantes)
    {
        if (participantes < 2) throw new ArgumentException("Escolha pelo menos dois Pokémon.");
        return participantes % 2 == 0 ? participantes - 1 : participantes;
    }

    public static Calendario Gerar(IEnumerable<string> ids, int quantidadeRodadas, int semente)
    {
        var participantes = ids != null ? new List<string>(ids) : new List<string>();
        ConferirParticipantes(participantes);
        if (quantidadeRodadas < 1) throw new ArgumentException("Escolha pelo menos uma rodada.");
        var resultado = new Calendario
        {
            participantes = new List<string>(participantes),
            quantidadeRodadas = quantidadeRodadas,
            semente = semente
        };
        var sorteio = new Random(semente);
        for (int i = participantes.Count - 1; i > 0; i--)
        {
            int j = sorteio.Next(i + 1);
            string atual = participantes[i]; participantes[i] = participantes[j]; participantes[j] = atual;
        }
        if (participantes.Count % 2 != 0) participantes.Add(null);
        int porTurno = participantes.Count - 1;
        for (int r = 0; r < quantidadeRodadas; r++)
        {
            var rodada = new Rodada { numero = r + 1 };
            int turno = r / porTurno;
            for (int i = 0; i < participantes.Count / 2; i++)
            {
                string a = participantes[i], b = participantes[participantes.Count - 1 - i];
                if (a == null || b == null) { rodada.folga = a ?? b; continue; }
                if (((r % porTurno + i) % 2 == 1) ^ (turno % 2 == 1))
                { string troca = a; a = b; b = troca; }
                rodada.jogos.Add(new Jogo { a = a, b = b });
            }
            resultado.rodadas.Add(rodada);
            string ultimo = participantes[participantes.Count - 1];
            for (int i = participantes.Count - 1; i > 1; i--) participantes[i] = participantes[i - 1];
            participantes[1] = ultimo;
        }
        return resultado;
    }

    public static void Validar(Calendario calendario)
    {
        if (calendario == null || calendario.versao != 1 || calendario.rodadas == null)
            throw new ArgumentException("Calendário da liga ausente ou de versão incompatível.");
        Calendario esperado = Gerar(calendario.participantes, calendario.quantidadeRodadas, calendario.semente);
        if (calendario.rodadas.Count != esperado.rodadas.Count)
            throw new ArgumentException("A quantidade de rodadas do calendário está incorreta.");
        for (int i = 0; i < esperado.rodadas.Count; i++)
        {
            Rodada atual = calendario.rodadas[i], referencia = esperado.rodadas[i];
            if (atual == null || atual.numero != referencia.numero || atual.folga != referencia.folga ||
                atual.jogos == null || atual.jogos.Count != referencia.jogos.Count)
                throw new ArgumentException("Rodada inválida no calendário: " + (i + 1) + ".");
            for (int j = 0; j < referencia.jogos.Count; j++)
                if (atual.jogos[j] == null || atual.jogos[j].a != referencia.jogos[j].a ||
                    atual.jogos[j].b != referencia.jogos[j].b)
                    throw new ArgumentException("Os confrontos da rodada " + (i + 1) + " foram alterados.");
        }
    }

    public static string ParaCsv(Calendario calendario)
    {
        Validar(calendario);
        var texto = new StringBuilder("Rodada,TimeA,TimeB\n");
        foreach (Rodada rodada in calendario.rodadas)
            foreach (Jogo jogo in rodada.jogos)
                texto.Append(rodada.numero).Append(',').Append(jogo.a).Append(',').Append(jogo.b).Append('\n');
        return texto.ToString();
    }

    private static void ConferirParticipantes(List<string> participantes)
    {
        if (participantes.Count < 2) throw new ArgumentException("Cadastre pelo menos dois Pokémon.");
        var unicos = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in participantes)
        {
            if (string.IsNullOrWhiteSpace(id) || !unicos.Add(id))
                throw new ArgumentException("Há Pokémon vazios ou repetidos no calendário.");
            foreach (char c in id)
                if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                    throw new ArgumentException("Use IDs estáveis com letras, números, _ e -.");
        }
    }
}
