using System.Collections.Generic;
using UnityEngine;

namespace FutebolDeDiscos
{
    /// <summary>Pure normal-scene formation planner shared by the match and its Inspector preview.</summary>
    public static class FormacaoElencoDiscos
    {
        public static Vector2[] Calcular(TeamData equipe, IList<JogadorDados> escalacao, bool timeA)
        {
            int total = escalacao != null ? escalacao.Count : 0;
            var bases = new Vector2[total];
            if (total == 0) return bases;
            float lado = timeA ? -1f : 1f;
            PosicaoCapitaoDiscos posicaoCapitao = equipe != null ? equipe.posicaoCapitaoDiscos : PosicaoCapitaoDiscos.Defesa;
            if (posicaoCapitao != PosicaoCapitaoDiscos.Meio && posicaoCapitao != PosicaoCapitaoDiscos.Ataque)
                posicaoCapitao = PosicaoCapitaoDiscos.Defesa;

            PosicaoJogadorDiscos individualCapitao = PosicaoIndividual(escalacao[0]);
            if (individualCapitao != PosicaoJogadorDiscos.Automatica)
                posicaoCapitao = individualCapitao == PosicaoJogadorDiscos.Ataque ? PosicaoCapitaoDiscos.Ataque
                    : individualCapitao == PosicaoJogadorDiscos.Meio ? PosicaoCapitaoDiscos.Meio : PosicaoCapitaoDiscos.Defesa;

            bool definida = false;
            for (int i = 0; i < total; i++)
            {
                bases[i] = BaseLegada(lado, total, i, posicaoCapitao);
                if (PosicaoIndividual(escalacao[i]) != PosicaoJogadorDiscos.Automatica) definida = true;
            }
            // New zero-valued fields preserve every existing normal-scene formation.
            if (!definida || total == 1) return bases;

            var funcoes = new PosicaoJogadorDiscos[total];
            for (int i = 0; i < total; i++)
            {
                funcoes[i] = PosicaoIndividual(escalacao[i]);
                if (funcoes[i] == PosicaoJogadorDiscos.Automatica)
                {
                    // Automatic teammates inherit the old slot AFTER the captain's
                    // own role is applied, keeping another disc in the defensive slot.
                    float profundidade = Mathf.Abs(bases[i].x);
                    funcoes[i] = profundidade >= 6.5f ? PosicaoJogadorDiscos.Defesa
                        : profundidade >= 4.7f ? PosicaoJogadorDiscos.Meio : PosicaoJogadorDiscos.Ataque;
                }
            }
            for (int i = 0; i < total; i++)
            {
                int quantidade = 0, ordem = 0;
                for (int j = 0; j < total; j++)
                    if (funcoes[j] == funcoes[i])
                    {
                        quantidade++;
                        if (j < i) ordem++;
                    }
                float profundidade = funcoes[i] == PosicaoJogadorDiscos.Defesa ? 8.5f
                    : funcoes[i] == PosicaoJogadorDiscos.Meio ? 5.7f : 3.7f;
                float faixa = quantidade <= 1 ? 0f : Mathf.Lerp(-3f, 3f, (float)ordem / (quantidade - 1));
                bases[i] = new Vector2(lado * profundidade, faixa);
            }
            return bases;
        }

        private static PosicaoJogadorDiscos PosicaoIndividual(JogadorDados jogador)
        {
            if (jogador == null || string.IsNullOrWhiteSpace(jogador.nomeJogador)) return PosicaoJogadorDiscos.Automatica;
            PosicaoJogadorDiscos posicao = jogador.posicaoDiscos;
            return posicao == PosicaoJogadorDiscos.Defesa || posicao == PosicaoJogadorDiscos.Meio || posicao == PosicaoJogadorDiscos.Ataque
                ? posicao : PosicaoJogadorDiscos.Automatica;
        }

        private static Vector2 BaseLegada(float lado, int total, int indice, PosicaoCapitaoDiscos posicao)
        {
            if (total == 1)
            {
                float profundidade = posicao == PosicaoCapitaoDiscos.Ataque ? 2.8f : posicao == PosicaoCapitaoDiscos.Meio ? 4.8f : 5.5f;
                return new Vector2(lado * profundidade, 0f);
            }
            bool capitao = indice == 0;
            if (posicao != PosicaoCapitaoDiscos.Defesa)
            {
                int vaga = 1;
                float alvo = posicao == PosicaoCapitaoDiscos.Ataque ? 3.7f : 5.7f;
                float melhor = float.MaxValue;
                for (int i = 1; i < total; i++)
                {
                    float distancia = Mathf.Abs(Mathf.Abs(VagaOriginal(lado, total, i).x) - alvo);
                    if (distancia < melhor) { melhor = distancia; vaga = i; }
                }
                if (indice == 0) indice = vaga;
                else if (indice == vaga) indice = 0;
            }
            Vector2 destino = VagaOriginal(lado, total, indice);
            if (total == 2 && capitao && posicao == PosicaoCapitaoDiscos.Meio) destino.x = lado * 5.2f;
            return destino;
        }

        private static Vector2 VagaOriginal(float lado, int total, int indice)
        {
            if (total == 2) return new Vector2(lado * (indice == 0 ? 7f : 3.6f), indice == 0 ? -1.8f : 1.8f);
            if (indice == 0) return new Vector2(lado * 8.5f, 0f);
            float faixa = (indice - 1f) / Mathf.Max(1f, total - 2f);
            return new Vector2(lado * (indice % 2 == 0 ? 3.7f : 5.7f), Mathf.Lerp(-3f, 3f, faixa));
        }
    }
}
