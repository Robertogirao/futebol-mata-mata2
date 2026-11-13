using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FutebolDeDiscos
{
    /// <summary>Assigns conditional pressing roles once per team, before movement.</summary>
    public static class TaticaDiscos
    {
        public static int LimitePressao(PosturaTatica postura, int quantidadeDiscos)
        {
            int limite;
            switch (postura)
            {
                case PosturaTatica.Defesa: limite = 1; break;
                case PosturaTatica.Balanceado: limite = 2; break;
                case PosturaTatica.Ataque: limite = 3; break;
                default: limite = 0; break;
            }
            return Mathf.Clamp(limite, 0, Mathf.Max(0, quantidadeDiscos));
        }

        public static int QuantidadeDesejada(PosturaTatica postura, int quantidadeDiscos,
            bool restringirMetade, bool timeA, Vector2 posBola, Vector2 velBola)
        {
            if (restringirMetade) return 0;
            int limite = LimitePressao(postura, quantidadeDiscos);
            float progresso = timeA ? posBola.x : -posBola.x;
            float velocidade = timeA ? velBola.x : -velBola.x;
            // Defend the home half, and avoid sending support into a fast counterattack.
            if (limite == 0 || progresso <= .35f || (velocidade < -2.5f && progresso < 4f)) return 0;
            int desejada = progresso >= 5f ? 3 : progresso >= 2.5f ? 2 : 1;
            if (velocidade < -2.5f) desejada--;
            return Mathf.Clamp(desejada, 0, limite);
        }

        public static void Atualizar(IList<DiscoAtleta> atletas, BolaDiscos bola, ProjetoFutebolDiscos cfg)
        {
            if (atletas == null || bola == null || bola.Corpo == null || cfg == null) return;
            AtualizarEquipe(atletas, bola, cfg, true);
            AtualizarEquipe(atletas, bola, cfg, false);
        }

        private static void AtualizarEquipe(IList<DiscoAtleta> atletas, BolaDiscos bola,
            ProjetoFutebolDiscos cfg, bool timeA)
        {
            var equipe = atletas.Where(a => a != null && a.Corpo != null && a.TimeA == timeA).ToList();
            if (equipe.Count == 0) return;
            int limite = cfg.limitarDiscosNaMetade ? 0 : LimitePressao(equipe[0].Equipe.posturaTatica, equipe.Count);
            int desejada = QuantidadeDesejada(equipe[0].Equipe.posturaTatica, equipe.Count,
                cfg.limitarDiscosNaMetade, timeA, bola.Corpo.position, bola.Corpo.linearVelocity);
            float sinal = timeA ? 1f : -1f;
            float raio = cfg.tamanhoDisco * .5f;
            // A returning player retains the crossing slot until fully back home.
            // Never admit a replacement just because its predecessor is retreating.
            var fora = equipe.Where(a => sinal * a.Corpo.position.x + raio > .02f).ToList();
            var escolhidos = fora.OrderBy(a => Prioridade(a, bola)).ThenBy(a => a.Indice)
                .Take(desejada).ToList();
            int novasVagas = Mathf.Min(desejada - escolhidos.Count, Mathf.Max(0, limite - fora.Count));
            escolhidos.AddRange(equipe.Where(a => !fora.Contains(a))
                .OrderBy(a => Prioridade(a, bola)).ThenBy(a => a.Indice).Take(novasVagas));
            foreach (DiscoAtleta atleta in equipe)
                atleta.DefinirPressao(escolhidos.Contains(atleta),
                    !cfg.limitarDiscosNaMetade && fora.Contains(atleta) && !escolhidos.Contains(atleta));
        }

        private static float Prioridade(DiscoAtleta atleta, BolaDiscos bola)
        {
            // Prefer the existing role unless another player is clearly better placed.
            return Vector2.Distance(atleta.Corpo.position, bola.Corpo.position) - (atleta.Pressionando ? 1f : 0f);
        }
    }
}
