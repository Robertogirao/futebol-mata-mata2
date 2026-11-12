using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

public sealed class CalendarioCsvLinha
{
    public int numeroLinha;
    public int rodada;
    public string timeA;
    public string timeB;
}

public static class CalendarioCsvParser
{
    public static List<CalendarioCsvLinha> Ler(
        string conteudo,
        Action<int, string> avisar)
    {
        var resultado = new List<CalendarioCsvLinha>();

        if (string.IsNullOrWhiteSpace(conteudo))
        {
            Avisar(avisar, 1, "Calendário vazio.");
            return resultado;
        }

        char separador = DetectarSeparador(conteudo);

        using (var leitor = new StringReader(conteudo))
        {
            int numero = 0;
            bool primeiraLinhaComConteudo = true;
            string linha;

            while ((linha = leitor.ReadLine()) != null)
            {
                numero++;
                linha = linha.TrimStart('\uFEFF');

                if (string.IsNullOrWhiteSpace(linha)) continue;

                List<string> colunas;
                string erro;

                if (!Separar(
                    linha, separador, out colunas, out erro))
                {
                    Avisar(
                        avisar,
                        numero,
                        erro + " Jogo não carregado; corrija esta linha.");

                    primeiraLinhaComConteudo = false;
                    continue;
                }

                string primeira = Limpar(colunas[0]);

                if (primeiraLinhaComConteudo &&
                    (string.Equals(
                        primeira,
                        "rodada",
                        StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(
                        primeira,
                        "round",
                        StringComparison.OrdinalIgnoreCase)))
                {
                    if (colunas.Count < 3)
                    {
                        Avisar(
                            avisar,
                            numero,
                            "Cabeçalho incompleto. Use Rodada,TimeA,TimeB ou Rodada;TimeA;TimeB.");
                    }

                    primeiraLinhaComConteudo = false;
                    continue;
                }

                primeiraLinhaComConteudo = false;

                if (colunas.Count < 3)
                {
                    Avisar(
                        avisar,
                        numero,
                        $"Esperadas 3 colunas; encontradas {colunas.Count}. Use o mesmo separador em todo o arquivo.");

                    continue;
                }

                if (colunas.Count > 3)
                {
                    Avisar(
                        avisar,
                        numero,
                        $"Há {colunas.Count} colunas; apenas Rodada, TimeA e TimeB serão usadas. Coloque nomes com separador entre aspas duplas.");
                }

                int rodada;

                if (!int.TryParse(
                        primeira,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out rodada) ||
                    rodada < 1)
                {
                    Avisar(
                        avisar,
                        numero,
                        $"Rodada inválida '{primeira}'; informe um inteiro positivo na primeira coluna.");

                    continue;
                }

                string timeA = Limpar(colunas[1]);
                string timeB = Limpar(colunas[2]);

                if (timeA.Length == 0 || timeB.Length == 0)
                {
                    Avisar(
                        avisar,
                        numero,
                        "Um dos times está vazio. Jogo não carregado.");

                    continue;
                }

                resultado.Add(new CalendarioCsvLinha
                {
                    numeroLinha = numero,
                    rodada = rodada,
                    timeA = timeA,
                    timeB = timeB
                });
            }
        }

        return resultado;
    }

    private static char DetectarSeparador(string conteudo)
    {
        using (var leitor = new StringReader(conteudo))
        {
            string linha;

            while ((linha = leitor.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(
                    linha.TrimStart('\uFEFF')))
                {
                    continue;
                }

                int virgulas = 0;
                int pontosEVirgulas = 0;
                bool aspas = false;

                for (int i = 0; i < linha.Length; i++)
                {
                    if (linha[i] == '"')
                    {
                        if (aspas &&
                            i + 1 < linha.Length &&
                            linha[i + 1] == '"')
                        {
                            i++;
                        }
                        else
                        {
                            aspas = !aspas;
                        }
                    }
                    else if (!aspas)
                    {
                        if (linha[i] == ',') virgulas++;
                        if (linha[i] == ';') pontosEVirgulas++;
                    }
                }

                return pontosEVirgulas > virgulas ? ';' : ',';
            }
        }

        return ',';
    }

    private static bool Separar(
        string linha,
        char separador,
        out List<string> colunas,
        out string erro)
    {
        colunas = new List<string>();
        erro = null;

        var campo = new StringBuilder();

        bool dentroAspas = false;
        bool fechouAspas = false;

        for (int i = 0; i < linha.Length; i++)
        {
            char c = linha[i];

            if (dentroAspas)
            {
                if (c == '"')
                {
                    if (i + 1 < linha.Length &&
                        linha[i + 1] == '"')
                    {
                        campo.Append('"');
                        i++;
                    }
                    else
                    {
                        dentroAspas = false;
                        fechouAspas = true;
                    }
                }
                else
                {
                    campo.Append(c);
                }
            }
            else if (c == separador)
            {
                colunas.Add(campo.ToString().Trim());
                campo.Length = 0;
                fechouAspas = false;
            }
            else if (c == '"')
            {
                if (fechouAspas ||
                    !string.IsNullOrWhiteSpace(campo.ToString()))
                {
                    erro =
                        "Aspas em posição inválida; envolva o campo inteiro em aspas e duplique aspas internas.";

                    return false;
                }

                campo.Length = 0;
                dentroAspas = true;
            }
            else if (fechouAspas && !char.IsWhiteSpace(c))
            {
                erro =
                    "Texto após o fechamento das aspas, antes do separador.";

                return false;
            }
            else
            {
                campo.Append(c);
            }
        }

        if (dentroAspas)
        {
            erro =
                "Aspas duplas sem fechamento. Cada jogo deve ocupar uma única linha, sem quebras dentro do nome.";

            return false;
        }

        colunas.Add(campo.ToString().Trim());

        return true;
    }

    private static string Limpar(string valor)
    {
        string resultado =
            valor.Trim().TrimStart('\uFEFF').Trim();

        // Aceita o formato legado 'nome', preservando apóstrofos internos.
        if (resultado.Length >= 2 &&
            resultado[0] == '\'' &&
            resultado[resultado.Length - 1] == '\'')
        {
            resultado =
                resultado.Substring(1, resultado.Length - 2).Trim();
        }

        return resultado;
    }

    private static void Avisar(
        Action<int, string> avisar,
        int linha,
        string mensagem)
    {
        if (avisar != null) avisar(linha, mensagem);
    }
}