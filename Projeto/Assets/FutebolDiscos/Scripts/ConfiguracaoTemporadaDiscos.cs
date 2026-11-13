using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FutebolDeDiscos
{
    /// <summary>Only identity, schedule and game rules belong here; visual settings remain editable.</summary>
    [Serializable]
    public sealed class DadosConfiguracaoDiscos
    {
        public string competicaoId = "futebol_discos";
        public string temporadaId = "temporada_01";
        public int formato;
        public int quantidadeTimes = 8;
        public int quantidadeRodadas = 7;
        public int semente;
        public int distribuicao;
        public List<string> ids = new List<string>();
        public bool sortearConfrontos = true;
        public bool permitirFolgas;
        public int discosPorTime = 3;
        public float duracaoPartida = 45f;
        public float velocidadeDiscos = 3f;
        public float velocidadeMaximaBola = 15f;
        public float forcaChute = 7f;
        public float tamanhoDisco = 1f;
        public float tamanhoBola = 0.42f;
        public float pausaAposGol = 1.1f;
        public bool limitarDiscosNaMetade = true;
        public bool usarForcaDosTimes = true;
    }

    [Serializable]
    public sealed class TemporadaDiscos
    {
        public int versao = 1;
        public DadosConfiguracaoDiscos configuracao;
        public int sementeEfetiva;
        // Kept as an empty object for knockout: Unity's inline serializer need
        // not preserve null nested classes for the metadata to round-trip.
        public CalendarioDiscos.Calendario calendarioLiga = new CalendarioDiscos.Calendario();
        public string salvoEmUtc;
    }

    public interface ISerializadorTemporadaDiscos
    {
        string ParaJson(TemporadaDiscos dados);
        TemporadaDiscos DeJson(string json);
    }

    /// <summary>Writes season identity once, before either tournament engine starts.</summary>
    public sealed class RepositorioTemporadaDiscos
    {
        private readonly ISerializadorTemporadaDiscos serializador;
        public string Pasta { get; private set; }
        public string CaminhoArquivo { get { return Path.Combine(Pasta, "configuracao_discos.json"); } }

        public RepositorioTemporadaDiscos(string pasta, ISerializadorTemporadaDiscos serializador)
        {
            if (string.IsNullOrWhiteSpace(pasta)) throw new ArgumentException("Informe a pasta da temporada.");
            if (serializador == null) throw new ArgumentNullException("serializador");
            Pasta = Path.GetFullPath(pasta);
            this.serializador = serializador;
        }

        public TemporadaDiscos CarregarOuCriar(DadosConfiguracaoDiscos configuracao, bool possuiResultadosExistentes = false)
        {
            ValidarConfiguracao(configuracao);
            // A missing metadata file must not assign a new seed over old results.
            if (!File.Exists(CaminhoArquivo) && possuiResultadosExistentes)
                throw new InvalidOperationException("Há resultados salvos sem a configuração da temporada. Preserve os arquivos e use outra temporada.");
            Directory.CreateDirectory(Pasta);
            using (var trava = new FileStream(Path.Combine(Pasta, "configuracao_discos.lock"), FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None))
            {
                if (File.Exists(CaminhoArquivo))
                {
                    TemporadaDiscos salva;
                    try { salva = serializador.DeJson(File.ReadAllText(CaminhoArquivo, Encoding.UTF8)); }
                    catch (Exception error)
                    { throw new InvalidDataException("A configuração salva não pôde ser lida. O arquivo foi preservado.", error); }
                    ValidarTemporada(salva, configuracao);
                    return salva;
                }
                if (possuiResultadosExistentes)
                    throw new InvalidOperationException("Há resultados salvos sem a configuração da temporada. Use outra temporada.");
                int seed = configuracao.semente != 0 ? configuracao.semente : Guid.NewGuid().GetHashCode() & int.MaxValue;
                if (configuracao.semente == 0 && seed == 0) seed = 1;
                var nova = new TemporadaDiscos
                {
                    configuracao = CopiarConfiguracao(configuracao),
                    sementeEfetiva = seed,
                    salvoEmUtc = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
                    calendarioLiga = configuracao.formato == 0
                        ? CalendarioDiscos.Gerar(configuracao.ids, configuracao.quantidadeRodadas, seed, configuracao.sortearConfrontos)
                        : new CalendarioDiscos.Calendario { quantidadeRodadas = 0, semente = seed, sortear = configuracao.sortearConfrontos }
                };
                ValidarTemporada(nova, configuracao);
                string json = serializador.ParaJson(nova);
                if (string.IsNullOrWhiteSpace(json)) throw new InvalidDataException("O serializador não produziu os dados da temporada.");
                // Check the actual serializer before committing the identity.
                TemporadaDiscos recarregada = serializador.DeJson(json);
                ValidarTemporada(recarregada, configuracao);
                if (recarregada.sementeEfetiva != nova.sementeEfetiva)
                    throw new InvalidDataException("O serializador alterou a semente da temporada.");
                GravarNovoArquivo(json);
                return nova;
            }
        }

        public static void ValidarConfiguracao(DadosConfiguracaoDiscos configuracao)
        {
            if (configuracao == null) throw new ArgumentException("A configuração da temporada está vazia.");
            Exigir(IdValido(configuracao.competicaoId) && IdValido(configuracao.temporadaId),
                "Identificadores aceitam de 1 a 80 letras, números, _ e -.");
            Exigir(configuracao.formato == 0 || configuracao.formato == 1, "Formato de campeonato inválido.");
            CalendarioDiscos.ValidarParticipantes(configuracao.ids);
            Exigir(configuracao.quantidadeTimes >= 2 && configuracao.quantidadeTimes <= 32,
                "Escolha de 2 a 32 vagas.");
            Exigir(configuracao.quantidadeRodadas >= 1 && configuracao.quantidadeRodadas <= 100,
                "Escolha de 1 a 100 rodadas.");
            Exigir(configuracao.distribuicao == 0 || configuracao.distribuicao == 1, "Distribuição de confrontos inválida.");
            if (configuracao.formato == 0)
                Exigir(configuracao.ids.Count == configuracao.quantidadeTimes, "Preencha todas as vagas da liga.");
            else
            {
                Exigir(new[] { 2, 4, 8, 16, 32 }.Contains(configuracao.quantidadeTimes), "Escolha uma chave de 2, 4, 8, 16 ou 32 vagas.");
                Exigir(configuracao.ids.Count <= configuracao.quantidadeTimes &&
                    (configuracao.permitirFolgas || configuracao.ids.Count == configuracao.quantidadeTimes),
                    "Complete a chave ou permita folgas.");
            }
            Exigir(configuracao.discosPorTime >= 1 && configuracao.discosPorTime <= 5, "Escolha de 1 a 5 discos por equipe.");
            Exigir(Positivo(configuracao.duracaoPartida) && configuracao.duracaoPartida <= 600f,
                "A duração da partida deve ser positiva e de até 600 segundos.");
            Exigir(Positivo(configuracao.velocidadeDiscos) && Positivo(configuracao.velocidadeMaximaBola) && Positivo(configuracao.forcaChute),
                "Confira as velocidades e a força dos chutes.");
            Exigir(Positivo(configuracao.tamanhoDisco) && Positivo(configuracao.tamanhoBola),
                "Confira os tamanhos dos discos e da bola.");
            Exigir(Finito(configuracao.pausaAposGol) && configuracao.pausaAposGol >= 0f,
                "A pausa após gol deve ser positiva ou zero.");
        }

        public static void ValidarTemporada(TemporadaDiscos temporada, DadosConfiguracaoDiscos esperada)
        {
            ValidarConfiguracao(esperada);
            Exigir(temporada != null && temporada.versao == 1 && temporada.configuracao != null,
                "Configuração salva ausente ou incompatível. O arquivo foi preservado.");
            ValidarConfiguracao(temporada.configuracao);
            Exigir(MesmaConfiguracao(temporada.configuracao, esperada),
                "A configuração difere da temporada salva. Restaure os ajustes ou use Nova temporada.");
            Exigir(esperada.semente == 0 || temporada.sementeEfetiva == esperada.semente,
                "A semente salva não corresponde à configuração da temporada.");
            Exigir(esperada.semente != 0 || temporada.sementeEfetiva > 0,
                "A semente automática salva está ausente ou inválida.");
            if (esperada.formato == 0)
            {
                CalendarioDiscos.Validar(temporada.calendarioLiga);
                Exigir(temporada.calendarioLiga.semente == temporada.sementeEfetiva &&
                    temporada.calendarioLiga.quantidadeRodadas == esperada.quantidadeRodadas &&
                    temporada.calendarioLiga.sortear == esperada.sortearConfrontos &&
                    temporada.calendarioLiga.participantes.SequenceEqual(esperada.ids),
                    "O calendário salvo não corresponde aos participantes, rodadas ou semente da temporada.");
            }
            else
                Exigir(temporada.calendarioLiga == null ||
                    (temporada.calendarioLiga.quantidadeRodadas == 0 &&
                     temporada.calendarioLiga.versao == 1 &&
                     temporada.calendarioLiga.semente == temporada.sementeEfetiva &&
                     temporada.calendarioLiga.sortear == esperada.sortearConfrontos &&
                     (temporada.calendarioLiga.participantes == null || temporada.calendarioLiga.participantes.Count == 0) &&
                     (temporada.calendarioLiga.rodadas == null || temporada.calendarioLiga.rodadas.Count == 0)),
                    "Uma temporada de mata-mata não pode conter calendário de liga.");
        }

        public static bool MesmaConfiguracao(DadosConfiguracaoDiscos a, DadosConfiguracaoDiscos b)
        {
            return a != null && b != null && a.competicaoId == b.competicaoId && a.temporadaId == b.temporadaId &&
                a.formato == b.formato && a.quantidadeTimes == b.quantidadeTimes && a.quantidadeRodadas == b.quantidadeRodadas &&
                a.semente == b.semente && a.distribuicao == b.distribuicao && a.sortearConfrontos == b.sortearConfrontos &&
                a.permitirFolgas == b.permitirFolgas && a.discosPorTime == b.discosPorTime && a.duracaoPartida == b.duracaoPartida &&
                a.velocidadeDiscos == b.velocidadeDiscos && a.velocidadeMaximaBola == b.velocidadeMaximaBola &&
                a.forcaChute == b.forcaChute && a.tamanhoDisco == b.tamanhoDisco && a.tamanhoBola == b.tamanhoBola &&
                a.pausaAposGol == b.pausaAposGol && a.limitarDiscosNaMetade == b.limitarDiscosNaMetade &&
                a.usarForcaDosTimes == b.usarForcaDosTimes && a.ids != null && b.ids != null && a.ids.SequenceEqual(b.ids);
        }

        private static DadosConfiguracaoDiscos CopiarConfiguracao(DadosConfiguracaoDiscos c)
        {
            return new DadosConfiguracaoDiscos
            {
                competicaoId = c.competicaoId, temporadaId = c.temporadaId, formato = c.formato,
                quantidadeTimes = c.quantidadeTimes, quantidadeRodadas = c.quantidadeRodadas, semente = c.semente,
                distribuicao = c.distribuicao, ids = new List<string>(c.ids), sortearConfrontos = c.sortearConfrontos,
                permitirFolgas = c.permitirFolgas, discosPorTime = c.discosPorTime, duracaoPartida = c.duracaoPartida,
                velocidadeDiscos = c.velocidadeDiscos, velocidadeMaximaBola = c.velocidadeMaximaBola,
                forcaChute = c.forcaChute, tamanhoDisco = c.tamanhoDisco, tamanhoBola = c.tamanhoBola,
                pausaAposGol = c.pausaAposGol, limitarDiscosNaMetade = c.limitarDiscosNaMetade,
                usarForcaDosTimes = c.usarForcaDosTimes
            };
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

        private static bool IdValido(string text) { return !string.IsNullOrEmpty(text) && Regex.IsMatch(text, @"\A[A-Za-z0-9_-]{1,80}\z"); }
        private static bool Finito(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static bool Positivo(float value) { return Finito(value) && value > 0f; }
        private static void Exigir(bool value, string message) { if (!value) throw new InvalidDataException(message); }
    }
}
