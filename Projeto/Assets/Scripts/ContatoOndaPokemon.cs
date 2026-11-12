using System;
using UnityEngine;

/// <summary>Contato contínuo de uma bola com a faixa de uma onda circular.</summary>
public static class ContatoOndaPokemon
{
    /// <summary>
    /// Considera o movimento da bola, do centro e do raio no mesmo intervalo.
    /// Margem é o raio da bola somado à metade da espessura da onda.
    /// A pausa e o controle de uma única aplicação por bola pertencem ao chamador.
    /// </summary>
    public static bool Intercepta(Vector2 bolaAnterior, Vector2 bolaAtual,
        Vector2 centroAnterior, Vector2 centroAtual, float raioAnterior,
        float raioAtual, float margem)
    {
        if (!Finito(bolaAnterior) || !Finito(bolaAtual) ||
            !Finito(centroAnterior) || !Finito(centroAtual) ||
            !Finito(raioAnterior) || !Finito(raioAtual) || !Finito(margem) ||
            raioAnterior < 0f || raioAtual < 0f || margem < 0f)
            return false;

        // Subtrai depois da conversão para double: duas coordenadas float
        // finitas podem ter uma diferença maior que o limite de float.
        double x = (double)bolaAnterior.x - centroAnterior.x;
        double y = (double)bolaAnterior.y - centroAnterior.y;
        double dx = ((double)bolaAtual.x - centroAtual.x) - x;
        double dy = ((double)bolaAtual.y - centroAtual.y) - y;
        double r0 = raioAnterior, r1 = raioAtual;
        double dr = r1 - r0, tolerancia = margem;
        double erro;

        // F(t) = |q0 + t*dq|² - (r0 + t*dr + margem)².
        // Se nem o mínimo entrar no círculo externo, não há contato.
        double minimoExterno = Extremo(x, y, dx, dy, r0 + tolerancia,
            dr, true, out erro);
        if (minimoExterno > erro) return false;

        // O raio interno é max(0, r(t)-margem). Se ele zera em uma ponta,
        // existe ao menos um instante que não está estritamente dentro dele.
        if (Math.Min(r0, r1) <= tolerancia) return true;

        // Se até o máximo ficar dentro do círculo interno, a bola esteve
        // sempre atrás da onda. Não confunde o disco todo com sua borda.
        double maximoInterno = Extremo(x, y, dx, dy, r0 - tolerancia,
            dr, false, out erro);
        return maximoInterno >= -erro;
    }

    private static double Extremo(double x, double y, double dx, double dy,
        double raio, double variacaoRaio, bool minimo, out double erro)
    {
        double a = dx * dx + dy * dy - variacaoRaio * variacaoRaio;
        double b = 2d * (x * dx + y * dy - raio * variacaoRaio);
        double c = x * x + y * y - raio * raio;
        double ponta = a + b + c;
        double resultado = minimo ? Math.Min(c, ponta) : Math.Max(c, ponta);

        // Só um vértice interior pode acrescentar um extremo aos dois limites.
        if ((minimo && a > 0d) || (!minimo && a < 0d))
        {
            double t = -b / (2d * a);
            if (t > 0d && t < 1d)
            {
                double valor = (a * t + b) * t + c;
                resultado = minimo ? Math.Min(resultado, valor) : Math.Max(resultado, valor);
            }
        }

        // Compensa somente o arredondamento das operações em double.
        const double epsilonMaquina = 2.2204460492503131e-16;
        erro = 64d * epsilonMaquina * Math.Max(1d,
            Math.Abs(a) + Math.Abs(b) + Math.Abs(c));
        return resultado;
    }

    private static bool Finito(Vector2 valor)
    {
        return Finito(valor.x) && Finito(valor.y);
    }

    private static bool Finito(float valor)
    {
        return !float.IsNaN(valor) && !float.IsInfinity(valor);
    }
}
