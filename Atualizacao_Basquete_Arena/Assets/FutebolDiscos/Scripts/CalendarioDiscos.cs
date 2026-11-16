using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FutebolDeDiscos
{
    /// <summary>Round-robin calendar independent of Unity; asset names remain unchanged.</summary>
    public static class CalendarioDiscos
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
            public bool sortear = true;
            public List<string> participantes = new List<string>();
            public List<Rodada> rodadas = new List<Rodada>();
        }

        public static int RodadasPorTurno(int quantidadeTimes)
        {
            if (quantidadeTimes < 2 || quantidadeTimes > 32)
                throw new ArgumentException("Escolha entre 2 e 32 equipes.");
            return quantidadeTimes % 2 == 0 ? quantidadeTimes - 1 : quantidadeTimes;
        }

        public static Calendario Gerar(IEnumerable<string> ids, int quantidadeRodadas, int semente, bool sortear = true)
        {
            var ordem = ids != null ? new List<string>(ids) : new List<string>();
            ValidarParticipantes(ordem);
            if (quantidadeRodadas < 1 || quantidadeRodadas > 100)
                throw new ArgumentException("Escolha de 1 a 100 rodadas.");
            var resultado = new Calendario
            {
                quantidadeRodadas = quantidadeRodadas,
                semente = semente,
                sortear = sortear,
                participantes = new List<string>(ordem)
            };
            if (sortear)
            {
                var random = new Sorteio(semente);
                for (int i = ordem.Count - 1; i > 0; i--)
                {
                    int j = random.Proximo(i + 1);
                    string atual = ordem[i]; ordem[i] = ordem[j]; ordem[j] = atual;
                }
            }
            if (ordem.Count % 2 != 0) ordem.Add(null);
            int rodadasPorTurno = ordem.Count - 1;
            // Restart the circle at each turn so return games reverse exactly
            // the same fixtures rather than a rotated permutation.
            var primeiroTurno = new List<string>(ordem);
            for (int indice = 0; indice < quantidadeRodadas; indice++)
            {
                int turno = indice / rodadasPorTurno;
                int dentroDoTurno = indice % rodadasPorTurno;
                if (dentroDoTurno == 0) ordem = new List<string>(primeiroTurno);
                var rodada = new Rodada { numero = indice + 1 };
                for (int i = 0; i < ordem.Count / 2; i++)
                {
                    string a = ordem[i], b = ordem[ordem.Count - 1 - i];
                    if (a == null || b == null) { rodada.folga = a ?? b; continue; }
                    if (((dentroDoTurno + i) % 2 == 1) ^ (turno % 2 == 1))
                    { string troca = a; a = b; b = troca; }
                    rodada.jogos.Add(new Jogo { a = a, b = b });
                }
                resultado.rodadas.Add(rodada);
                string ultimo = ordem[ordem.Count - 1];
                for (int i = ordem.Count - 1; i > 1; i--) ordem[i] = ordem[i - 1];
                ordem[1] = ultimo;
            }
            return resultado;
        }

        public static void Validar(Calendario calendario)
        {
            if (calendario == null || calendario.versao != 1 || calendario.rodadas == null)
                throw new ArgumentException("Calendário ausente ou de versão incompatível.");
            Calendario referencia = Gerar(calendario.participantes, calendario.quantidadeRodadas,
                calendario.semente, calendario.sortear);
            if (calendario.rodadas.Count != referencia.rodadas.Count)
                throw new ArgumentException("Quantidade de rodadas diferente do calendário salvo.");
            for (int r = 0; r < referencia.rodadas.Count; r++)
            {
                Rodada atual = calendario.rodadas[r], esperado = referencia.rodadas[r];
                if (atual == null || atual.numero != esperado.numero || atual.folga != esperado.folga ||
                    atual.jogos == null || atual.jogos.Count != esperado.jogos.Count)
                    throw new ArgumentException("Rodada inválida no calendário: " + (r + 1) + ".");
                for (int j = 0; j < esperado.jogos.Count; j++)
                    if (atual.jogos[j] == null || atual.jogos[j].a != esperado.jogos[j].a ||
                        atual.jogos[j].b != esperado.jogos[j].b)
                        throw new ArgumentException("Os confrontos da rodada " + (r + 1) + " foram alterados.");
            }
        }

        public static string ParaCsv(Calendario calendario)
        {
            Validar(calendario);
            var csv = new StringBuilder("Rodada,TimeA,TimeB\n");
            foreach (Rodada rodada in calendario.rodadas)
                foreach (Jogo jogo in rodada.jogos)
                    csv.Append(rodada.numero.ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(CampoCsv(jogo.a)).Append(',').Append(CampoCsv(jogo.b)).Append('\n');
            return csv.ToString();
        }

        public static void ValidarParticipantes(IList<string> ids)
        {
            if (ids == null || ids.Count < 2 || ids.Count > 32)
                throw new ArgumentException("Cadastre de 2 a 32 equipes.");
            var unicos = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in ids)
                if (string.IsNullOrWhiteSpace(id) || id.Length > 200 || id != id.Trim() ||
                    id.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0 || !unicos.Add(id))
                    throw new ArgumentException("Há nomes de assets vazios, repetidos ou inválidos nos participantes.");
        }

        private static string CampoCsv(string value)
        {
            return value.IndexOfAny(new[] { ',', ';', '"' }) >= 0
                ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }

        // Explicit PRNG keeps saved calendars reproducible across runtimes.
        private sealed class Sorteio
        {
            private uint estado;
            public Sorteio(int seed) { estado = unchecked((uint)seed) ^ 0x9e3779b9u; if (estado == 0) estado = 0x6d2b79f5u; }
            public int Proximo(int maximo)
            {
                uint x = estado; x ^= x << 13; x ^= x >> 17; x ^= x << 5; estado = x;
                return (int)(x % (uint)maximo);
            }
        }
    }
}
