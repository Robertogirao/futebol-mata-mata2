using System;
using System.Collections.Generic;

/// <summary>
/// Chave de eliminação simples sem dependência de Unity. Os IDs de participantes
/// e a ordem das sementes são persistidos; somente o chamador decide embaralhá-los.
/// </summary>
public static class MataMataModelo
{
    [Serializable]
    public sealed class Estado
    {
        public int versao = 1;
        public int vagas;
        public List<string> participantes = new List<string>();
        public List<string> sementes = new List<string>();
        public List<Fase> fases = new List<Fase>();
        public string campeao = "";
    }

    [Serializable]
    public sealed class Fase
    {
        public int indice;
        public int vagas;
        public List<Partida> partidas = new List<Partida>();
    }

    [Serializable]
    public sealed class Partida
    {
        public string id = "";
        public int fase;
        public int indice;
        public string a = "";
        public string b = "";
        public string vencedor = "";
        public int golsA;
        public int golsB;
        public bool resolvidaA;
        public bool resolvidaB;
        public bool concluida;
        public bool bye;
    }

    /// <summary>
    /// Cria uma chave de 2, 4, 8, 16 ou 32 vagas. O primeiro ID é a semente 1.
    /// Sementes ausentes são BYEs; as primeiras sementes ficam em lados opostos.
    /// </summary>
    public static Estado Criar(List<string> ids, int vagas)
    {
        VerificarVagas(vagas);
        VerificarIds(ids, vagas, "participantes");

        var estado = new Estado
        {
            vagas = vagas,
            participantes = new List<string>(ids),
            sementes = new List<string>(ids)
        };

        List<int> ordem = OrdemSementes(vagas);
        int numeroFase = 0;

        for (int tamanho = vagas; tamanho >= 2; tamanho /= 2)
        {
            var fase = new Fase { indice = numeroFase, vagas = tamanho };

            for (int i = 0; i < tamanho / 2; i++)
            {
                var partida = new Partida
                {
                    id = "f" + numeroFase + "-p" + i,
                    fase = numeroFase,
                    indice = i
                };

                if (numeroFase == 0)
                {
                    partida.a = IdDaSemente(ids, ordem[i * 2]);
                    partida.b = IdDaSemente(ids, ordem[i * 2 + 1]);
                    partida.resolvidaA = true;
                    partida.resolvidaB = true;
                }

                fase.partidas.Add(partida);
            }

            estado.fases.Add(fase);
            numeroFase++;
        }

        Propagar(estado);
        return estado;
    }

    /// <summary>
    /// Verifica inclusive os resultados, BYEs e a origem de cada participante.
    /// Não altera o estado. Lança ArgumentException se o arquivo estiver corrompido.
    /// </summary>
    public static bool Validar(Estado estado)
    {
        if (estado == null) throw Invalido("Estado ausente.");
        if (estado.versao != 1) throw Invalido("Versão da chave não suportada.");
        VerificarVagas(estado.vagas);
        HashSet<string> participantes = VerificarIds(
            estado.participantes, estado.vagas, "participantes");
        HashSet<string> sementes = VerificarIds(
            estado.sementes, estado.vagas, "sementes");

        if (!participantes.SetEquals(sementes))
            throw Invalido("As sementes não correspondem aos participantes.");

        Estado esperado = Criar(estado.sementes, estado.vagas);

        if (estado.fases == null || estado.fases.Count != esperado.fases.Count)
            throw Invalido("Quantidade de fases inválida.");

        // Reproduzir resultados sobre uma chave nova impede que um vencedor
        // adulterado ou um slot ainda desconhecido seja aceito como BYE.
        for (int f = 0; f < esperado.fases.Count; f++)
        {
            Fase atual = estado.fases[f];
            Fase referencia = esperado.fases[f];

            if (atual == null || atual.partidas == null ||
                atual.partidas.Count != referencia.partidas.Count)
                throw Invalido("Quantidade de partidas inválida na fase " + f + ".");

            for (int p = 0; p < referencia.partidas.Count; p++)
            {
                Partida informada = atual.partidas[p];
                Partida original = referencia.partidas[p];

                if (informada == null) throw Invalido("Partida ausente.");

                if (informada.concluida && !informada.bye)
                {
                    if (!PodeJogar(original))
                        throw Invalido("Resultado sem os dois participantes definidos.");

                    VerificarPlacar(informada.golsA, informada.golsB);
                    AplicarResultado(esperado, original, informada.golsA, informada.golsB);
                }
            }
        }

        if (!Igual(estado.campeao, esperado.campeao))
            throw Invalido("Campeão incompatível com a final.");

        for (int f = 0; f < esperado.fases.Count; f++)
        {
            Fase atual = estado.fases[f];
            Fase referencia = esperado.fases[f];

            if (atual.indice != referencia.indice || atual.vagas != referencia.vagas)
                throw Invalido("Identificação da fase inválida.");

            for (int p = 0; p < referencia.partidas.Count; p++)
                CompararPartidas(atual.partidas[p], referencia.partidas[p]);
        }

        return true;
    }

    /// <summary>Retorna a primeira partida jogável, por fase e posição.</summary>
    public static Partida Proxima(Estado estado)
    {
        Validar(estado);

        for (int f = 0; f < estado.fases.Count; f++)
            for (int p = 0; p < estado.fases[f].partidas.Count; p++)
                if (PodeJogar(estado.fases[f].partidas[p]))
                    return estado.fases[f].partidas[p];

        return null;
    }

    /// <summary>
    /// Registra uma partida pronta e propaga seu vencedor. Repetir o mesmo
    /// resultado é permitido; alterar um resultado já concluído é rejeitado.
    /// </summary>
    public static void Registrar(Estado estado, string partidaId, int golsA, int golsB)
    {
        Validar(estado);
        VerificarPlacar(golsA, golsB);

        Partida partida = null;

        for (int f = 0; f < estado.fases.Count; f++)
            for (int p = 0; p < estado.fases[f].partidas.Count; p++)
                if (Igual(estado.fases[f].partidas[p].id, partidaId))
                    partida = estado.fases[f].partidas[p];

        if (partida == null) throw Invalido("Partida não encontrada.");

        if (partida.concluida)
        {
            if (!partida.bye && partida.golsA == golsA && partida.golsB == golsB)
                return;

            throw new InvalidOperationException("Não é possível alterar uma partida concluída.");
        }

        if (!PodeJogar(partida))
            throw new InvalidOperationException("A partida ainda não possui dois participantes definidos.");

        AplicarResultado(estado, partida, golsA, golsB);
    }

    /// <summary>
    /// Retorna uma cópia com uma partida real reaberta. Descendentes perdem seus
    /// resultados; partidas independentes e a ordem das sementes são preservadas.
    /// O estado original não é modificado.
    /// </summary>
    public static Estado Reabrir(Estado estado, string partidaId)
    {
        Validar(estado);
        Partida alvo = null;

        for (int f = 0; f < estado.fases.Count; f++)
            for (int p = 0; p < estado.fases[f].partidas.Count; p++)
                if (Igual(estado.fases[f].partidas[p].id, partidaId))
                    alvo = estado.fases[f].partidas[p];

        if (alvo == null) throw Invalido("Partida não encontrada.");
        if (!alvo.concluida || alvo.bye)
            throw new InvalidOperationException("Escolha uma partida real já concluída.");

        Estado novo = Criar(estado.sementes, estado.vagas);
        novo.participantes = new List<string>(estado.participantes);

        for (int f = 0; f < estado.fases.Count; f++)
        {
            for (int p = 0; p < estado.fases[f].partidas.Count; p++)
            {
                Partida antiga = estado.fases[f].partidas[p];
                if (!antiga.concluida || antiga.bye || Igual(antiga.id, alvo.id)) continue;

                Partida candidata = novo.fases[f].partidas[p];
                if (PodeJogar(candidata) && Igual(candidata.a, antiga.a) && Igual(candidata.b, antiga.b))
                    AplicarResultado(novo, candidata, antiga.golsA, antiga.golsB);
            }
        }

        return novo;
    }

    public static bool Finalizado(Estado estado)
    {
        Validar(estado);
        return estado.campeao.Length > 0;
    }

    public static string NomeFase(int vagas)
    {
        switch (vagas)
        {
            case 32: return "ROUND OF 32";
            case 16: return "ROUND OF 16";
            case 8: return "QUARTER-FINALS";
            case 4: return "SEMI-FINALS";
            case 2: return "FINAL";
            default: throw Invalido("Tamanho de fase inválido.");
        }
    }

    private static void AplicarResultado(Estado estado, Partida partida, int golsA, int golsB)
    {
        partida.golsA = golsA;
        partida.golsB = golsB;
        partida.vencedor = golsA > golsB ? partida.a : partida.b;
        partida.concluida = true;
        Propagar(estado);
    }

    private static void Propagar(Estado estado)
    {
        for (int f = 0; f < estado.fases.Count; f++)
        {
            for (int p = 0; p < estado.fases[f].partidas.Count; p++)
            {
                Partida partida = estado.fases[f].partidas[p];

                if (f > 0)
                {
                    Partida origemA = estado.fases[f - 1].partidas[p * 2];
                    Partida origemB = estado.fases[f - 1].partidas[p * 2 + 1];
                    partida.resolvidaA = origemA.concluida;
                    partida.resolvidaB = origemB.concluida;
                    partida.a = origemA.concluida ? origemA.vencedor : "";
                    partida.b = origemB.concluida ? origemB.vencedor : "";
                }

                if (!partida.concluida && partida.resolvidaA && partida.resolvidaB &&
                    (partida.a.Length == 0 || partida.b.Length == 0))
                {
                    partida.bye = true;
                    partida.concluida = true;
                    partida.vencedor = partida.a.Length > 0 ? partida.a : partida.b;
                }
            }
        }

        Partida final = estado.fases[estado.fases.Count - 1].partidas[0];
        estado.campeao = final.concluida ? final.vencedor : "";
    }

    private static bool PodeJogar(Partida partida)
    {
        return !partida.concluida && partida.resolvidaA && partida.resolvidaB &&
            partida.a.Length > 0 && partida.b.Length > 0;
    }

    /// <summary>Posições das sementes na chave, numeradas a partir de 1.</summary>
    public static List<int> OrdemSementes(int vagas)
    {
        VerificarVagas(vagas);
        var ordem = new List<int> { 1, 2 };

        for (int tamanho = 4; tamanho <= vagas; tamanho *= 2)
        {
            var proxima = new List<int>(tamanho);

            for (int i = 0; i < ordem.Count; i++)
            {
                proxima.Add(ordem[i]);
                proxima.Add(tamanho + 1 - ordem[i]);
            }

            ordem = proxima;
        }

        return ordem;
    }

    private static string IdDaSemente(List<string> ids, int semente)
    {
        return semente <= ids.Count ? ids[semente - 1] : "";
    }

    private static HashSet<string> VerificarIds(List<string> ids, int vagas, string nome)
    {
        if (ids == null || ids.Count < 2 || ids.Count > vagas)
            throw Invalido("Quantidade de " + nome + " deve estar entre 2 e " + vagas + ".");

        var encontrados = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < ids.Count; i++)
        {
            string id = ids[i];

            if (string.IsNullOrWhiteSpace(id) || !Igual(id, id.Trim()))
                throw Invalido("ID de participante vazio ou com espaços nas extremidades.");

            if (!encontrados.Add(id)) throw Invalido("Participante duplicado: " + id + ".");
        }

        return encontrados;
    }

    private static void VerificarVagas(int vagas)
    {
        if (vagas < 2 || vagas > 32 || (vagas & (vagas - 1)) != 0)
            throw Invalido("A chave deve ter 2, 4, 8, 16 ou 32 vagas.");
    }

    private static void VerificarPlacar(int golsA, int golsB)
    {
        if (golsA < 0 || golsB < 0)
            throw new ArgumentOutOfRangeException("gols", "O placar não pode ser negativo.");

        if (golsA == golsB) throw Invalido("Uma partida eliminatória não pode terminar empatada.");
    }

    private static void CompararPartidas(Partida atual, Partida esperada)
    {
        if (!Igual(atual.id, esperada.id) || atual.fase != esperada.fase ||
            atual.indice != esperada.indice || !Igual(atual.a, esperada.a) ||
            !Igual(atual.b, esperada.b) || !Igual(atual.vencedor, esperada.vencedor) ||
            atual.golsA != esperada.golsA || atual.golsB != esperada.golsB ||
            atual.resolvidaA != esperada.resolvidaA || atual.resolvidaB != esperada.resolvidaB ||
            atual.concluida != esperada.concluida || atual.bye != esperada.bye)
            throw Invalido("Partida incompatível com a chave: " + esperada.id + ".");
    }

    private static bool Igual(string a, string b)
    {
        return string.Equals(a, b, StringComparison.Ordinal);
    }

    private static ArgumentException Invalido(string mensagem)
    {
        return new ArgumentException(mensagem);
    }
}
