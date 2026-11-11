using System;

public enum EventoArenaPokemon { Nenhum, SnorlaxRage, RocketFrenzy, SnorlaxGigante }
public enum EtapaEventoPokemon { Esperando, Aviso, Ativo, Concluido }

/// <summary>Agenda independente da física. Usa seu próprio sorteio, sem consumir UnityEngine.Random.</summary>
public sealed class RitmoEventosPokemon
{
    public EventoArenaPokemon Evento { get; private set; }
    public EtapaEventoPokemon Etapa { get; private set; }
    public double Restante { get; private set; }
    public double DuracaoEtapa { get; private set; }
    public int Executados { get; private set; }
    public int ExecutadosClassicos { get; private set; }
    public int ExecutadosGigante { get; private set; }
    public float Progresso { get { return (float)Math.Max(0d, Math.Min(1d, 1d - Restante / Math.Max(0.001d, DuracaoEtapa))); } }

    private readonly Random sorteio;
    private readonly bool snorlax;
    private readonly bool rocket;
    private readonly int maximo;
    private readonly int maximoGigante;
    private readonly double aviso;
    private readonly double duracaoSnorlax;
    private readonly double duracaoRocket;
    private readonly double duracaoGigante;
    private readonly double minimo;
    private readonly double maxIntervalo;
    private EventoArenaPokemon proximoClassico;

    public RitmoEventosPokemon(int semente, bool usarSnorlax, bool usarRocket, int quantidade,
        float esperaMin, float esperaMax, float tempoAviso, float tempoSnorlax, float tempoRocket,
        bool usarGigante = false, int quantidadeGigante = 0, float tempoGigante = 5f)
    {
        sorteio = new Random(semente);
        snorlax = usarSnorlax; rocket = usarRocket;
        maximo = Math.Max(0, Math.Min(10, quantidade));
        maximoGigante = usarGigante ? Math.Max(0, Math.Min(10, quantidadeGigante)) : 0;
        minimo = Seguro(esperaMin, 8d, 2d, 120d);
        maxIntervalo = Math.Max(minimo, Seguro(esperaMax, 14d, 2d, 120d));
        aviso = Seguro(tempoAviso, 1.5d, 0.5d, 5d);
        duracaoSnorlax = Seguro(tempoSnorlax, 1.2d, 0.4d, 5d);
        duracaoRocket = Seguro(tempoRocket, 5d, 1d, 15d);
        duracaoGigante = Seguro(tempoGigante, 5d, 1d, 15d);
        if (TemClassicos)
            proximoClassico = snorlax && rocket
                ? (sorteio.Next(2) == 0 ? EventoArenaPokemon.SnorlaxRage : EventoArenaPokemon.RocketFrenzy)
                : (snorlax ? EventoArenaPokemon.SnorlaxRage : EventoArenaPokemon.RocketFrenzy);
        if (!TemClassicos && maximoGigante == 0)
        {
            Etapa = EtapaEventoPokemon.Concluido;
            return;
        }
        Evento = EscolherProximo(EventoArenaPokemon.Nenhum);
        DefinirEtapa(EtapaEventoPokemon.Esperando, SortearEspera());
    }

    private bool TemClassicos
    {
        get { return (snorlax || rocket) && ExecutadosClassicos < maximo; }
    }

    /// <returns>Verdadeiro apenas no instante de ativação do evento.</returns>
    public bool Avancar(float segundos)
    {
        if (Etapa == EtapaEventoPokemon.Concluido || float.IsNaN(segundos) ||
            float.IsInfinity(segundos) || segundos <= 0f) return false;
        Restante = Math.Max(0d, Restante - segundos);
        if (Restante > 0d) return false;
        // Uma transição por frame: uma queda de FPS não pode pular o aviso visual.
        if (Etapa == EtapaEventoPokemon.Esperando)
            DefinirEtapa(EtapaEventoPokemon.Aviso, aviso);
        else if (Etapa == EtapaEventoPokemon.Aviso)
        {
            Executados++;
            if (Evento == EventoArenaPokemon.SnorlaxGigante) ExecutadosGigante++;
            else
            {
                ExecutadosClassicos++;
                // O gigante não consome a contagem nem muda a alternância dos dois clássicos.
                if (snorlax && rocket)
                    proximoClassico = Evento == EventoArenaPokemon.SnorlaxRage
                        ? EventoArenaPokemon.RocketFrenzy : EventoArenaPokemon.SnorlaxRage;
            }
            DefinirEtapa(EtapaEventoPokemon.Ativo,
                Evento == EventoArenaPokemon.SnorlaxRage ? duracaoSnorlax :
                Evento == EventoArenaPokemon.SnorlaxGigante ? duracaoGigante : duracaoRocket);
            return true;
        }
        else
        {
            Evento = EscolherProximo(Evento);
            if (Evento == EventoArenaPokemon.Nenhum)
                DefinirEtapa(EtapaEventoPokemon.Concluido, 0d);
            else DefinirEtapa(EtapaEventoPokemon.Esperando, SortearEspera());
        }
        return false;
    }

    private EventoArenaPokemon EscolherProximo(EventoArenaPokemon anterior)
    {
        bool temGigante = ExecutadosGigante < maximoGigante;
        if (!TemClassicos) return temGigante ? EventoArenaPokemon.SnorlaxGigante : EventoArenaPokemon.Nenhum;
        // Sem gigante, nenhum sorteio extra é consumido: a agenda clássica permanece idêntica.
        if (!temGigante) return proximoClassico;
        if (anterior == EventoArenaPokemon.SnorlaxGigante) return proximoClassico;
        if (anterior == proximoClassico) return EventoArenaPokemon.SnorlaxGigante;
        int restantesClassicos = maximo - ExecutadosClassicos;
        int restantesGigante = maximoGigante - ExecutadosGigante;
        return sorteio.Next(restantesClassicos + restantesGigante) < restantesGigante
            ? EventoArenaPokemon.SnorlaxGigante : proximoClassico;
    }

    private void DefinirEtapa(EtapaEventoPokemon etapa, double duracao)
    {
        Etapa = etapa; Restante = DuracaoEtapa = duracao;
    }

    private double SortearEspera() { return minimo + sorteio.NextDouble() * (maxIntervalo - minimo); }
    private static double Seguro(float valor, double padrao, double min, double max)
    {
        return float.IsNaN(valor) || float.IsInfinity(valor) ? padrao : Math.Max(min, Math.Min(max, valor));
    }
}
