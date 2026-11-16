using System;
using System.Globalization;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace BasqueteArena
{
    public enum ElementoArena { Fogo, Gelo, Agua }
    [Serializable]
    public sealed class ObstaculoArena
    {
        public Vector2 posicao;
        public Vector2 tamanho = new Vector2(3f, .2f);
        public float angulo;
        public float grausPorSegundo;
        public bool elimina;
    }

    [CreateAssetMenu(menuName = "Basquete/Arena do campeonato", fileName = "ArenaBasquete")]
    public sealed class TemaArenaBasquete : ScriptableObject
    {
        public ElementoArena elemento;
        public Sprite fundoCenario, fundoQuadra, imagemCesta, imagemLancador;
        public AudioClip somEliminacao, somLancamento;
        public Color corFundo = new Color(.055f, .075f, .13f);
        public Color corPlataformas = new Color(.35f, .65f, .9f);
        public Color corEliminacao = new Color(1f, .25f, .05f);
        [Range(.5f, 6f)] public float tempoRetorno = 2f;
        [Range(.1f, 1f)] public float gravidade = .75f;
        [Range(0f, 1f)] public float atrito = .12f;
        [Range(0f, 1f)] public float quique = .3f;
        [Range(0f, 8f)] public float forcaCorrente = 1.5f;
        [Range(0f, 2f)] public float intervaloLancador = .65f;
        [Range(0f, .7f)] public float dispersaoArremesso = .32f;
        [Range(3f, 20f)] public float tempoDestravar = 8f;
        public bool usarOverall = true;
        public List<ObstaculoArena> obstaculos = new List<ObstaculoArena>();

        public void Validar()
        {
            if ((int)elemento < 0 || (int)elemento > 2) throw new InvalidOperationException("Elemento da arena inválido.");
            Faixa(tempoRetorno, .5f, 6f); Faixa(gravidade, .1f, 1f); Faixa(atrito, 0, 1); Faixa(quique, 0, 1);
            Faixa(forcaCorrente, 0, 8); Faixa(intervaloLancador, 0, 2); Faixa(dispersaoArremesso, 0, .7f); Faixa(tempoDestravar, 3, 20);
            foreach (var o in obstaculos)
            {
                if (o == null) continue;
                Faixa(o.posicao.x, -9.5f, .5f); Faixa(o.posicao.y, -4, 4);
                Faixa(o.tamanho.x, .1f, 9); Faixa(o.tamanho.y, .1f, 8);
                Faixa(o.angulo, -360, 360); Faixa(o.grausPorSegundo, -360, 360);
            }
        }
        private static void Faixa(float valor, float min, float max)
        {
            if (float.IsNaN(valor) || float.IsInfinity(valor) || valor < min || valor > max)
                throw new InvalidOperationException("Confira os limites dos controles e dos obstáculos da arena de basquete.");
        }
        public string Assinatura()
        {
            var s = new StringBuilder("arena-v1|");
            s.Append((int)elemento).Append('|').Append(usarOverall).Append('|');
            foreach (float v in new[] { tempoRetorno, gravidade, atrito, quique, forcaCorrente, intervaloLancador, dispersaoArremesso, tempoDestravar })
                s.Append(v.ToString("R", CultureInfo.InvariantCulture)).Append('|');
            foreach (var o in obstaculos)
            {
                if (o == null) continue;
                foreach (float v in new[] { o.posicao.x, o.posicao.y, o.tamanho.x, o.tamanho.y, o.angulo, o.grausPorSegundo })
                    s.Append(v.ToString("R", CultureInfo.InvariantCulture)).Append('|');
                s.Append(o.elimina).Append('|');
            }
            return s.ToString();
        }
    }
}
