using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FutebolDeDiscos
{
    [Serializable]
    public sealed class RegistroCapitaoConvidado
    {
        public string timeId, capitaoId, nomeCapitao;
        public int overall;
    }

    [Serializable]
    public sealed class RegistroVinculoManualCapitao
    {
        public string timeId, capitaoId;
    }

    [Serializable]
    public sealed class DadosCapitaesConvidados
    {
        public int versao = 1;
        public int semente;
        public bool capitaoExtra = true;
        public List<string> times = new List<string>();
        public List<RegistroCapitaoConvidado> atribuicoes = new List<RegistroCapitaoConvidado>();
        public List<RegistroVinculoManualCapitao> vinculosManuais = new List<RegistroVinculoManualCapitao>();
        public string salvoEmUtc;
    }

    public interface ISerializadorCapitaesConvidados
    {
        string ParaJson(DadosCapitaesConvidados dados);
        DadosCapitaesConvidados DeJson(string json);
    }

    /// <summary>Persist guest captains separately so old seasons keep their original metadata schema.</summary>
    public sealed class RepositorioCapitaesConvidados
    {
        private readonly ISerializadorCapitaesConvidados serializador;
        public string Pasta { get; private set; }
        public string CaminhoArquivo { get { return Path.Combine(Pasta, "capitaes_convidados.json"); } }

        public RepositorioCapitaesConvidados(string pasta, ISerializadorCapitaesConvidados serializador)
        {
            if (string.IsNullOrWhiteSpace(pasta)) throw new ArgumentException("Informe a pasta da temporada dos capitães.");
            if (serializador == null) throw new ArgumentNullException("serializador");
            Pasta = Path.GetFullPath(pasta);
            this.serializador = serializador;
        }

        public DadosCapitaesConvidados CarregarOuCriar(IEnumerable<string> times, IList<CapitaoConvidado> catalogo,
            IList<VinculoCapitaoConvidado> manuais, int semente, bool possuiResultadosExistentes = false, bool capitaoExtra = true)
        {
            List<string> ids = times != null ? times.ToList() : null;
            ValidarConfiguracao(ids, catalogo, manuais);
            if (!File.Exists(CaminhoArquivo) && possuiResultadosExistentes)
                throw new InvalidOperationException("Há resultados sem o sorteio salvo dos capitães. Preserve os arquivos e use Nova temporada.");
            Directory.CreateDirectory(Pasta);
            using (var trava = new FileStream(Path.Combine(Pasta, "capitaes_convidados.lock"), FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None))
            {
                if (File.Exists(CaminhoArquivo))
                {
                    DadosCapitaesConvidados salva;
                    try { salva = serializador.DeJson(File.ReadAllText(CaminhoArquivo, Encoding.UTF8)); }
                    catch (Exception erro)
                    { throw new InvalidDataException("O sorteio salvo dos capitães não pôde ser lido. O arquivo foi preservado.", erro); }
                    ValidarDados(salva, ids, catalogo, manuais, semente, capitaoExtra);
                    return salva;
                }
                if (possuiResultadosExistentes)
                    throw new InvalidOperationException("Há resultados sem o sorteio salvo dos capitães. Use Nova temporada.");
                DadosCapitaesConvidados nova = CriarSorteio(ids, catalogo, manuais, semente, capitaoExtra);
                ValidarDados(nova, ids, catalogo, manuais, semente, capitaoExtra);
                string json = serializador.ParaJson(nova);
                if (string.IsNullOrWhiteSpace(json)) throw new InvalidDataException("O serializador não produziu o sorteio dos capitães.");
                DadosCapitaesConvidados recarregada = serializador.DeJson(json);
                ValidarDados(recarregada, ids, catalogo, manuais, semente, capitaoExtra);
                Exigir(MesmoSorteio(nova, recarregada), "O serializador alterou os capitães atribuídos aos times.");
                GravarNovoArquivo(json);
                return nova;
            }
        }

        public static void ValidarConfiguracao(IEnumerable<string> times, IList<CapitaoConvidado> catalogo,
            IList<VinculoCapitaoConvidado> manuais)
        {
            List<string> ids = times != null ? times.ToList() : null;
            Exigir(ids != null && ids.Count >= 2 && ids.Count <= 32 && ids.All(IdValido) &&
                ids.Distinct(StringComparer.Ordinal).Count() == ids.Count, "Confira os times da competição de capitães.");
            Exigir(catalogo != null && catalogo.Count >= ids.Count, "Cadastre pelo menos um capitão diferente para cada time.");
            var unicos = new HashSet<string>(StringComparer.Ordinal);
            foreach (CapitaoConvidado capitao in catalogo)
                Exigir(capitao != null && IdValido(capitao.Identificador) && unicos.Add(capitao.Identificador) &&
                    !string.IsNullOrWhiteSpace(capitao.nomeCapitao) && capitao.overall >= 1 && capitao.overall <= 100,
                    "Confira os nomes, identificadores e overall dos capitães. Os nomes dos assets devem ser únicos.");
            var timesManuais = new HashSet<string>(StringComparer.Ordinal);
            var capitaesManuais = new HashSet<string>(StringComparer.Ordinal);
            foreach (VinculoCapitaoConvidado vinculo in manuais ?? new List<VinculoCapitaoConvidado>())
                Exigir(vinculo != null && vinculo.time != null && vinculo.capitao != null &&
                    ids.Contains(vinculo.time.name, StringComparer.Ordinal) && catalogo.Contains(vinculo.capitao) &&
                    timesManuais.Add(vinculo.time.name) && capitaesManuais.Add(vinculo.capitao.Identificador),
                    "Confira os vínculos manuais: use times participantes e capitães do catálogo, sem repetições.");
        }

        public static void ValidarDados(DadosCapitaesConvidados dados, IEnumerable<string> times,
            IList<CapitaoConvidado> catalogo, IList<VinculoCapitaoConvidado> manuais, int semente, bool capitaoExtra = true)
        {
            List<string> ids = times != null ? times.ToList() : null;
            ValidarConfiguracao(ids, catalogo, manuais);
            Exigir(dados != null && dados.versao == 1 && dados.semente == semente && dados.capitaoExtra == capitaoExtra &&
                dados.times != null && dados.times.SequenceEqual(ids) && dados.atribuicoes != null &&
                dados.atribuicoes.Count == ids.Count && dados.vinculosManuais != null,
                "O sorteio salvo não corresponde à temporada dos capitães. Preserve os arquivos e use Nova temporada.");
            var porId = catalogo.ToDictionary(c => c.Identificador, StringComparer.Ordinal);
            var usados = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < ids.Count; i++)
            {
                RegistroCapitaoConvidado registro = dados.atribuicoes[i];
                CapitaoConvidado capitao;
                Exigir(registro != null && registro.timeId == ids[i] && IdValido(registro.capitaoId) &&
                    usados.Add(registro.capitaoId) && porId.TryGetValue(registro.capitaoId, out capitao) &&
                    !string.IsNullOrWhiteSpace(registro.nomeCapitao) && registro.overall >= 1 && registro.overall <= 100,
                    "O sorteio salvo contém capitães ausentes, repetidos ou inválidos. O arquivo foi preservado.");
                Exigir(registro.overall == porId[registro.capitaoId].overall,
                    "O overall de um capitão mudou durante a temporada. Restaure o valor ou use Nova temporada.");
            }
            List<RegistroVinculoManualCapitao> esperados = ManuaisOrdenados(manuais);
            Exigir(dados.vinculosManuais.Count == esperados.Count, "Os vínculos manuais diferem do sorteio salvo. Use Nova temporada.");
            for (int i = 0; i < esperados.Count; i++)
            {
                RegistroVinculoManualCapitao salvo = dados.vinculosManuais[i], esperado = esperados[i];
                Exigir(salvo != null && salvo.timeId == esperado.timeId && salvo.capitaoId == esperado.capitaoId &&
                    dados.atribuicoes.Any(v => v.timeId == salvo.timeId && v.capitaoId == salvo.capitaoId),
                    "Os vínculos manuais diferem do sorteio salvo. Restaure os vínculos ou use Nova temporada.");
            }
        }

        private static DadosCapitaesConvidados CriarSorteio(List<string> ids, IList<CapitaoConvidado> catalogo,
            IList<VinculoCapitaoConvidado> manuais, int semente, bool capitaoExtra)
        {
            List<RegistroVinculoManualCapitao> fixos = ManuaisOrdenados(manuais);
            var ocupados = new HashSet<string>(fixos.Select(v => v.capitaoId), StringComparer.Ordinal);
            var restantes = catalogo.Where(c => !ocupados.Contains(c.Identificador))
                .OrderBy(c => c.Identificador, StringComparer.Ordinal).ToList();
            var random = new Random(semente);
            for (int i = restantes.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                CapitaoConvidado troca = restantes[i]; restantes[i] = restantes[j]; restantes[j] = troca;
            }
            var porId = catalogo.ToDictionary(c => c.Identificador, StringComparer.Ordinal);
            var resultado = new DadosCapitaesConvidados
            {
                semente = semente, capitaoExtra = capitaoExtra, times = new List<string>(ids), vinculosManuais = fixos,
                salvoEmUtc = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture)
            };
            int proximo = 0;
            foreach (string id in ids)
            {
                RegistroVinculoManualCapitao manual = fixos.FirstOrDefault(v => v.timeId == id);
                CapitaoConvidado capitao = manual != null ? porId[manual.capitaoId] : restantes[proximo++];
                resultado.atribuicoes.Add(new RegistroCapitaoConvidado
                {
                    timeId = id, capitaoId = capitao.Identificador, nomeCapitao = capitao.nomeCapitao.Trim(), overall = capitao.overall
                });
            }
            return resultado;
        }

        private static List<RegistroVinculoManualCapitao> ManuaisOrdenados(IList<VinculoCapitaoConvidado> manuais)
        {
            return (manuais ?? new List<VinculoCapitaoConvidado>()).Select(v => new RegistroVinculoManualCapitao
                { timeId = v.time.name, capitaoId = v.capitao.Identificador }).OrderBy(v => v.timeId, StringComparer.Ordinal).ToList();
        }

        private static bool MesmoSorteio(DadosCapitaesConvidados a, DadosCapitaesConvidados b)
        {
            return a.atribuicoes.Select(v => v.timeId + "\0" + v.capitaoId + "\0" + v.nomeCapitao + "\0" + v.overall)
                .SequenceEqual(b.atribuicoes.Select(v => v.timeId + "\0" + v.capitaoId + "\0" + v.nomeCapitao + "\0" + v.overall));
        }

        private void GravarNovoArquivo(string json)
        {
            string temporario = CaminhoArquivo + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporario, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true))
                    { writer.Write(json); writer.Flush(); }
                    stream.Flush(true);
                }
                File.Move(temporario, CaminhoArquivo);
            }
            finally { if (File.Exists(temporario)) File.Delete(temporario); }
        }

        private static bool IdValido(string valor)
        {
            return !string.IsNullOrWhiteSpace(valor) && valor.Length <= 200 && valor == valor.Trim() &&
                valor.IndexOfAny(new[] { '\r', '\n', '\0' }) < 0;
        }
        private static void Exigir(bool condicao, string mensagem)
        { if (!condicao) throw new InvalidDataException(mensagem); }
    }
}
