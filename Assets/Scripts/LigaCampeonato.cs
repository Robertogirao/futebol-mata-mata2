using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class LigaCampeonato : MonoBehaviour
{
    [Header("Identificação do campeonato")]
    [Tooltip("Use somente letras, números, _ e -. Mantenha durante a temporada.")]
    public string ligaId = "teste";

    public string temporadaId = "temporada_01";

    [Header("Consulta e exportação")]
    [Tooltip("Zero exporta a última rodada salva. Informe outro número para exportar uma rodada antiga.")]
    [Min(0)]
    public int rodadaParaExportar = 0;

    [HideInInspector]
    public string ultimoErro;

    [Serializable]
    public class Clube
    {
        public string id;
        public string nome;
    }

    [Serializable]
    public class Jogo
    {
        public string id;
        public string a;
        public string b;
        public string nomeA;
        public string nomeB;
        public int golsA;
        public int golsB;
        public bool registrado;
    }

    [Serializable]
    public class Linha
    {
        public string id;
        public int POS;
        public string TEAM;
        public int PTS;
        public int P;
        public int W;
        public int D;
        public int L;
        public int GD;
    }

    [Serializable]
    public class Rodada
    {
        public int numero;
        public bool confirmada;
        public List<Jogo> jogos = new List<Jogo>();
        public List<Linha> abertura = new List<Linha>();
        public List<Linha> encerramento = new List<Linha>();
    }

    [Serializable]
    public class Dados
    {
        public int versao = 1;
        public string liga;
        public string temporada;
        public List<Clube> clubes = new List<Clube>();
        public List<Rodada> rodadas = new List<Rodada>();
    }

    private Dados dados;
    private string caminhoCarregado;
    private int rodadaPreparada;

    public string PastaDaTemporada
    {
        get
        {
            Exigir(
                IdentificadorValido(ligaId) &&
                IdentificadorValido(temporadaId),
                "Liga e temporada aceitam somente letras, números, _ e -.");

            return Path.Combine(
                Application.persistentDataPath,
                "Ligas",
                ligaId,
                temporadaId);
        }
    }

    public bool PrepararRodada(
        int numero,
        List<TeamData> todos,
        List<Confronto> confrontos)
    {
        rodadaPreparada = 0;

        return Tentar(() =>
        {
            Carregar();

            Exigir(numero > 0, "A rodada deve ser positiva.");

            Exigir(
                todos != null && todos.Count >= 2,
                "Cadastre os clubes da liga antes de gravar.");

            Exigir(
                confrontos != null && confrontos.Count > 0,
                "Não há confrontos para preparar.");

            var clubes = todos.Select(t =>
            {
                return new Clube
                {
                    id = IdClube(t),
                    nome = NomeClube(t)
                };
            }).ToList();

            Exigir(
                clubes.Select(c => c.id).Distinct().Count()
                    == clubes.Count,
                "Existem assets TeamData com nomes repetidos.");

            var ids = new HashSet<string>(
                clubes.Select(c => c.id));

            var novosJogos = confrontos.Select(c =>
            {
                Exigir(c != null, "Há um confronto vazio.");

                string a = IdClube(c.timeA);
                string b = IdClube(c.timeB);

                Exigir(a != b,
                    "Um clube não pode enfrentar ele mesmo.");

                Exigir(
                    ids.Contains(a) && ids.Contains(b),
                    "Um confronto usa um clube fora da lista da liga.");

                return new Jogo
                {
                    id = IdJogo(numero, a, b),
                    a = a,
                    b = b,
                    nomeA = NomeClube(c.timeA),
                    nomeB = NomeClube(c.timeB)
                };
            }).ToList();

            Exigir(
                novosJogos.Select(j => j.id).Distinct().Count()
                    == novosJogos.Count,
                "Há confrontos duplicados no calendário.");

            if (dados.clubes.Count > 0)
            {
                var antigos = new HashSet<string>(
                    dados.clubes.Select(c => c.id));

                Exigir(
                    antigos.SetEquals(ids),
                    "A lista de clubes mudou. Preserve os nomes dos " +
                    "assets TeamData durante a temporada ou use outra temporada.");
            }

            Rodada existente = dados.rodadas.Find(
                r => r.numero == numero);

            Exigir(
                existente == null || !existente.confirmada,
                "Esta rodada já foi confirmada. Use o menu da Liga " +
                "para exportá-la. Ela não será regravada automaticamente.");

            Exigir(
                !dados.rodadas.Any(r =>
                    !r.confirmada && r.numero != numero),
                "Existe outra rodada em gravação. Confirme-a primeiro.");

            int ultimaConfirmada = dados.rodadas
                .Where(r => r.confirmada)
                .Select(r => r.numero)
                .DefaultIfEmpty(0)
                .Max();

            Exigir(
                numero > ultimaConfirmada,
                "Não é permitido gravar antes de uma rodada já confirmada.");

            if (existente != null)
            {
                Exigir(
                    existente.jogos.Select(j => j.id).SequenceEqual(
                        novosJogos.Select(j => j.id)),
                    "O calendário ou a ordem dos jogos mudou. " +
                    "Restaure o calendário usado no início da gravação.");
            }

            Alterar(() =>
            {
                dados.clubes = clubes;

                if (existente == null)
                {
                    existente = new Rodada
                    {
                        numero = numero,
                        jogos = novosJogos,
                        abertura = Calcular(numero, false)
                    };

                    dados.rodadas.Add(existente);
                }
            });

            Exportar(existente);

            for (int i = 0; i < confrontos.Count; i++)
            {
                Jogo salvo = existente.jogos[i];
                Confronto c = confrontos[i];

                c.concluido = salvo.registrado;
                c.pontosRegistrados = salvo.registrado;
                c.golsA = salvo.registrado ? salvo.golsA : 0;
                c.golsB = salvo.registrado ? salvo.golsB : 0;

                c.vencedor =
                    !salvo.registrado || salvo.golsA == salvo.golsB
                        ? null
                        : salvo.golsA > salvo.golsB
                            ? c.timeA
                            : c.timeB;
            }

            rodadaPreparada = numero;

            Debug.Log(
                "Liga preparada. Arquivos em: " + PastaDaTemporada,
                this);
        });
    }

    public bool RegistrarResultado(
        Confronto confronto,
        int golsA,
        int golsB)
    {
        return Tentar(() =>
        {
            Carregar();

            Rodada rodada = RodadaPreparada();

            Exigir(
                !rodada.confirmada,
                "Esta rodada já está confirmada.");

            Exigir(
                golsA >= 0 && golsB >= 0,
                "O placar não pode ser negativo.");

            Jogo jogo = LocalizarJogo(rodada, confronto);

            Alterar(() =>
            {
                jogo.golsA = golsA;
                jogo.golsB = golsB;
                jogo.registrado = true;

                rodada.encerramento =
                    rodada.jogos.All(j => j.registrado)
                        ? Calcular(rodada.numero, true)
                        : new List<Linha>();
            });

            Exportar(rodada);
        });
    }

    public bool LimparResultado(Confronto confronto)
    {
        return Tentar(() =>
        {
            Carregar();

            Rodada rodada = RodadaPreparada();

            Exigir(
                !rodada.confirmada,
                "Rodadas confirmadas não podem ser regravadas.");

            Jogo jogo = LocalizarJogo(rodada, confronto);

            Alterar(() =>
            {
                jogo.registrado = false;
                jogo.golsA = 0;
                jogo.golsB = 0;
                rodada.encerramento.Clear();
            });

            Exportar(rodada);
        });
    }

    public bool ConfirmarRodada()
    {
        return Tentar(() =>
        {
            Carregar();

            // O seletor de exportação não interfere na confirmação.
            Rodada rodada = rodadaPreparada > 0
                ? RodadaPreparada()
                : (
                    dados.rodadas.FirstOrDefault(r => !r.confirmada)
                    ?? dados.rodadas
                        .OrderByDescending(r => r.numero)
                        .FirstOrDefault()
                );

            Exigir(
                rodada != null,
                "Não há rodada salva para confirmar.");

            Exigir(
                rodada.jogos.Count > 0 &&
                rodada.jogos.All(j => j.registrado),
                "Todos os confrontos precisam ter resultado.");

            if (!rodada.confirmada)
            {
                Alterar(() =>
                {
                    rodada.encerramento =
                        Calcular(rodada.numero, true);

                    rodada.confirmada = true;
                });
            }

            // Repetir a confirmação apenas reexporta os arquivos.
            Exportar(rodada);

            Debug.Log(
                "Rodada " + rodada.numero + " confirmada.",
                this);
        });
    }

    public bool ExportarRodada()
    {
        return Tentar(() =>
        {
            Carregar();
            Exportar(SelecionarRodada());
        });
    }
[ContextMenu("Liga/Cancelar rodada em gravação")]
private void MenuCancelarRodada()
{
    if (Application.isPlaying)
    {
        Debug.LogWarning(
            "Pare o Recorder e saia do Play Mode antes de cancelar.",
            this);
        return;
    }

    Tentar(() =>
    {
        Carregar();

        Rodada rodada = dados.rodadas.Find(
            r => !r.confirmada);

        Exigir(
            rodada != null,
            "Não há rodada em gravação para cancelar.");

        string pasta = PastaDaTemporada;

        string identificador =
            DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") +
            "_" +
            Guid.NewGuid().ToString("N").Substring(0, 6);

        string arquivoCanceladas = Path.Combine(
            pasta,
            "canceladas");

        Directory.CreateDirectory(arquivoCanceladas);

        // Preserva o histórico antes de cancelar.
        File.Copy(
            caminhoCarregado,
            Path.Combine(
                arquivoCanceladas,
                "historico_antes_" + identificador + ".json"));

        int numero = rodada.numero;

        // Remove somente o rascunho.
        Alterar(() =>
        {
            dados.rodadas.Remove(rodada);
        });

        rodadaPreparada = 0;

        string pastaRodada = Path.Combine(
            pasta,
            "rodada_" + numero.ToString("D3"));

        // Guarda os CSVs antigos fora da pasta ativa da rodada.
        if (Directory.Exists(pastaRodada))
        {
            try
            {
                Directory.Move(
                    pastaRodada,
                    Path.Combine(
                        arquivoCanceladas,
                        "rodada_" + numero.ToString("D3") +
                        "_" + identificador));
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    "A rodada foi cancelada, mas os CSVs antigos " +
                    "não puderam ser movidos: " + e.Message,
                    this);
            }
        }

        Debug.Log(
            "Rodada " + numero + " cancelada. " +
            "Rodadas confirmadas foram preservadas. " +
            "Agora configure o novo calendário e a rodada desejada.",
            this);
    });
}
public List<Linha> ObterClassificacao(bool encerramento)
{
    List<Linha> copia = null;

    bool ok = Tentar(() =>
    {
        Carregar();

        Rodada rodada = rodadaPreparada > 0
            ? RodadaPreparada()
            : SelecionarRodada();

        List<Linha> origem = encerramento
            ? rodada.encerramento
            : rodada.abertura;

        Exigir(
            origem != null && origem.Count > 0,
            encerramento
                ? "A classificação final só fica disponível após todos os jogos."
                : "A classificação de abertura ainda não está disponível.");

        copia = origem.Select(t => new Linha
        {
            id = t.id,
            POS = t.POS,
            TEAM = t.TEAM,
            PTS = t.PTS,
            P = t.P,
            W = t.W,
            D = t.D,
            L = t.L,
            GD = t.GD
        }).ToList();
    });

    return ok ? copia : null;
}
public List<Jogo> ObterJogosDaRodada()
{
    List<Jogo> copia = null;

    bool ok = Tentar(() =>
    {
        Carregar();

        Rodada rodada = rodadaPreparada > 0
            ? RodadaPreparada()
            : SelecionarRodada();

        copia = rodada.jogos.Select(j => new Jogo
        {
            id = j.id,
            a = j.a,
            b = j.b,
            nomeA = j.nomeA,
            nomeB = j.nomeB,
            golsA = j.golsA,
            golsB = j.golsB,
            registrado = j.registrado
        }).ToList();
    });

    return ok ? copia : null;
}
    [ContextMenu("Liga/Confirmar rodada")]
    private void MenuConfirmar()
    {
        ConfirmarRodada();
    }

    [ContextMenu("Liga/Exportar CSVs")]
    private void MenuExportar()
    {
        ExportarRodada();
    }

    [ContextMenu("Liga/Abrir pasta da temporada")]
    private void MenuAbrir()
    {
        Tentar(() =>
        {
            string pasta = PastaDaTemporada;
            Directory.CreateDirectory(pasta);

            Application.OpenURL(
                new Uri(
                    pasta + Path.DirectorySeparatorChar
                ).AbsoluteUri);
        });
    }

    private void Carregar()
    {
        string caminho = Path.Combine(
            PastaDaTemporada,
            "temporada.json");

        if (dados != null && caminhoCarregado == caminho)
            return;

        Dados carregados;

        if (File.Exists(caminho))
        {
            string json = File.ReadAllText(
                caminho,
                Encoding.UTF8);

            Exigir(
                !string.IsNullOrWhiteSpace(json),
                "O histórico está vazio. Preserve o arquivo " +
                "e verifique o backup.");

            carregados = JsonUtility.FromJson<Dados>(json);
            Validar(carregados);
        }
        else
        {
            carregados = new Dados
            {
                liga = ligaId,
                temporada = temporadaId
            };
        }

        dados = carregados;
        caminhoCarregado = caminho;
        rodadaPreparada = 0;
    }

    private void Alterar(Action acao)
    {
        string anterior = JsonUtility.ToJson(dados);

        try
        {
            acao();
            Validar(dados);

            EscreverAtomico(
                caminhoCarregado,
                JsonUtility.ToJson(dados, true),
                false,
                true);
        }
        catch
        {
            dados = JsonUtility.FromJson<Dados>(anterior);
            throw;
        }
    }

    private void Validar(Dados d)
    {
        Exigir(
            d != null &&
            d.versao == 1 &&
            d.liga == ligaId &&
            d.temporada == temporadaId,
            "Histórico inválido, de outra liga/temporada " +
            "ou de versão incompatível.");

        Exigir(
            d.clubes != null &&
            d.clubes.Count >= 2 &&
            d.rodadas != null,
            "O histórico contém cadastro incompleto.");

        var ids = new HashSet<string>();

        foreach (Clube c in d.clubes)
        {
            Exigir(
                c != null &&
                !string.IsNullOrWhiteSpace(c.id) &&
                !string.IsNullOrWhiteSpace(c.nome) &&
                ids.Add(c.id),
                "O histórico contém clubes inválidos ou IDs repetidos.");
        }

        var numeros = new HashSet<int>();
        int rascunhos = 0;

        foreach (Rodada r in d.rodadas)
        {
            Exigir(
                r != null &&
                r.numero > 0 &&
                numeros.Add(r.numero) &&
                r.jogos != null &&
                r.jogos.Count > 0 &&
                r.abertura != null &&
                r.encerramento != null,
                "O histórico contém uma rodada inválida.");

            Exigir(
                r.abertura.Count == d.clubes.Count,
                "A classificação de abertura está incompleta.");

            var jogos = new HashSet<string>();

            foreach (Jogo j in r.jogos)
            {
                Exigir(
                    j != null &&
                    ids.Contains(j.a) &&
                    ids.Contains(j.b) &&
                    j.a != j.b &&
                    j.id == IdJogo(r.numero, j.a, j.b) &&
                    jogos.Add(j.id) &&
                    j.golsA >= 0 &&
                    j.golsB >= 0 &&
                    !string.IsNullOrWhiteSpace(j.nomeA) &&
                    !string.IsNullOrWhiteSpace(j.nomeB),
                    "O histórico contém um confronto inválido.");
            }

            bool completa = r.jogos.All(j => j.registrado);

            Exigir(
                !r.confirmada || completa,
                "Uma rodada confirmada contém jogos sem resultado.");

            Exigir(
                r.encerramento.Count ==
                    (completa ? d.clubes.Count : 0),
                "A classificação de encerramento está inconsistente.");

            if (!r.confirmada) rascunhos++;
        }

        Exigir(
            rascunhos <= 1,
            "Há mais de uma rodada em gravação.");

        Rodada draft = d.rodadas.Find(r => !r.confirmada);

        Exigir(
            draft == null ||
            !d.rodadas.Any(r =>
                r.confirmada && r.numero >= draft.numero),
            "A ordem das rodadas do histórico está inconsistente.");
    }

    private List<Linha> Calcular(
        int numero,
        bool incluirAtual)
    {
        var mapa = dados.clubes.ToDictionary(
            c => c.id,
            c => new Linha
            {
                id = c.id,
                TEAM = c.nome
            });

        foreach (Rodada r in dados.rodadas)
        {
            bool incluir =
                r.numero < numero && r.confirmada;

            if (incluirAtual && r.numero == numero)
                incluir = true;

            if (!incluir) continue;

            foreach (Jogo j in r.jogos.Where(j => j.registrado))
            {
                Linha a = mapa[j.a];
                Linha b = mapa[j.b];

                a.P++;
                b.P++;

                a.GD += j.golsA - j.golsB;
                b.GD += j.golsB - j.golsA;

                if (j.golsA > j.golsB)
                {
                    a.W++;
                    a.PTS += 3;
                    b.L++;
                }
                else if (j.golsA < j.golsB)
                {
                    b.W++;
                    b.PTS += 3;
                    a.L++;
                }
                else
                {
                    a.D++;
                    b.D++;
                    a.PTS++;
                    b.PTS++;
                }
            }
        }

        var tabela = mapa.Values
            .OrderByDescending(t => t.PTS)
            .ThenByDescending(t => t.GD)
            .ThenByDescending(t => t.W)
            .ThenBy(
                t => t.TEAM,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(
                t => t.id,
                StringComparer.Ordinal)
            .ToList();

        for (int i = 0; i < tabela.Count; i++)
        {
            bool empatado =
                i > 0 &&
                tabela[i].PTS == tabela[i - 1].PTS &&
                tabela[i].GD == tabela[i - 1].GD &&
                tabela[i].W == tabela[i - 1].W;

            tabela[i].POS = empatado
                ? tabela[i - 1].POS
                : i + 1;
        }

        return tabela;
    }

    private void Exportar(Rodada rodada)
    {
        string pasta = Path.Combine(
            PastaDaTemporada,
            "rodada_" + rodada.numero.ToString("D3"));

        Directory.CreateDirectory(pasta);

        EscreverAtomico(
            Path.Combine(pasta, "classificacao_abertura.csv"),
            CsvTabela(rodada.abertura),
            true,
            false);

        var confrontos = new List<string>
        {
            "RODADA,TIME A,TIME B"
        };

        var resultados = new List<string>
        {
            "RODADA,TIME A,TIME B,GOLS A,GOLS B"
        };

        foreach (Jogo j in rodada.jogos)
        {
            confrontos.Add(
                CsvLinha(
                    rodada.numero,
                    j.nomeA,
                    j.nomeB));

            if (j.registrado)
            {
                resultados.Add(
                    CsvLinha(
                        rodada.numero,
                        j.nomeA,
                        j.nomeB,
                        j.golsA,
                        j.golsB));
            }
        }

        EscreverAtomico(
            Path.Combine(pasta, "confrontos.csv"),
            string.Join("\r\n", confrontos) + "\r\n",
            true,
            false);

        EscreverAtomico(
            Path.Combine(pasta, "resultados.csv"),
            string.Join("\r\n", resultados) + "\r\n",
            true,
            false);

        string final = Path.Combine(
            pasta,
            "classificacao_encerramento.csv");

        if (rodada.encerramento.Count > 0)
        {
            EscreverAtomico(
                final,
                CsvTabela(rodada.encerramento),
                true,
                false);
        }
        else if (File.Exists(final))
        {
            File.Delete(final);
        }
    }

    private static string CsvTabela(List<Linha> tabela)
    {
        return "POS,TEAM,PTS,P,W,D,L,GD\r\n" +
            string.Join(
                "\r\n",
                tabela.Select(t =>
                    CsvLinha(
                        t.POS,
                        t.TEAM,
                        t.PTS,
                        t.P,
                        t.W,
                        t.D,
                        t.L,
                        t.GD))) +
            "\r\n";
    }

    private static string CsvLinha(params object[] campos)
    {
        return string.Join(",", campos.Select(c =>
        {
            string s =
                Convert.ToString(
                    c,
                    CultureInfo.InvariantCulture) ?? "";

            return s.IndexOfAny(
                new[] { ',', '"', '\r', '\n' }) >= 0
                    ? "\"" + s.Replace("\"", "\"\"") + "\""
                    : s;
        }));
    }

    private static void EscreverAtomico(
        string caminho,
        string texto,
        bool bom,
        bool backup)
    {
        Directory.CreateDirectory(
            Path.GetDirectoryName(caminho));

        string temporario = caminho + ".tmp";

        using (var arquivo = new FileStream(
            temporario,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None))
        {
            using (var escritor = new StreamWriter(
                arquivo,
                new UTF8Encoding(bom),
                4096,
                true))
            {
                escritor.Write(texto);
                escritor.Flush();
            }

            arquivo.Flush(true);
        }

        if (File.Exists(caminho))
        {
            File.Replace(
                temporario,
                caminho,
                backup ? caminho + ".bak" : null);
        }
        else
        {
            File.Move(temporario, caminho);
        }
    }

    private Rodada RodadaPreparada()
    {
        Exigir(
            rodadaPreparada > 0,
            "Prepare a rodada pelo TournamentManager primeiro.");

        Rodada r = dados.rodadas.Find(
            x => x.numero == rodadaPreparada);

        Exigir(
            r != null,
            "A rodada preparada não está no histórico.");

        return r;
    }

    private Rodada SelecionarRodada()
    {
        int numero = rodadaParaExportar > 0
            ? rodadaParaExportar
            : dados.rodadas
                .Select(r => r.numero)
                .DefaultIfEmpty(0)
                .Max();

        Rodada r = dados.rodadas.Find(
            x => x.numero == numero);

        Exigir(
            r != null,
            "A rodada selecionada não foi salva.");

        return r;
    }

    private static Jogo LocalizarJogo(
        Rodada rodada,
        Confronto confronto)
    {
        Exigir(confronto != null, "Confronto ausente.");

        string id = IdJogo(
            rodada.numero,
            IdClube(confronto.timeA),
            IdClube(confronto.timeB));

        Jogo jogo = rodada.jogos.Find(j => j.id == id);

        Exigir(
            jogo != null,
            "O confronto não pertence à rodada preparada.");

        return jogo;
    }

    private static string IdClube(TeamData time)
    {
        Exigir(
            time != null &&
            !string.IsNullOrWhiteSpace(time.name),
            "Cada clube precisa de um asset TeamData com nome único.");

        return time.name.Trim();
    }

    private static string NomeClube(TeamData time)
    {
        return string.IsNullOrWhiteSpace(time.nomeEquipe)
            ? IdClube(time)
            : time.nomeEquipe.Trim();
    }

    private static string IdJogo(
        int rodada,
        string a,
        string b)
    {
        return rodada.ToString(
            CultureInfo.InvariantCulture) +
            "|" + Uri.EscapeDataString(a) +
            "|" + Uri.EscapeDataString(b);
    }

    private static bool IdentificadorValido(string valor)
    {
        return !string.IsNullOrWhiteSpace(valor) &&
            valor.Length <= 80 &&
            Regex.IsMatch(
                valor,
                "^[A-Za-z0-9_-]+$");
    }

    private static void Exigir(
        bool condicao,
        string mensagem)
    {
        if (!condicao)
            throw new InvalidOperationException(mensagem);
    }

    private bool Tentar(Action acao)
    {
        ultimoErro = "";

        try
        {
            acao();
            return true;
        }
        catch (Exception e)
        {
            ultimoErro = e.Message;
            Debug.LogError("Liga: " + e.Message, this);
            return false;
        }
    }
}