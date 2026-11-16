using System;
using UnityEngine;

namespace FutebolDeDiscos
{
    // O valor zero preserva fichas e sorteios antigos, sem posição cadastrada.
    public enum PosicaoCapitao { Defesa = 0, Meio = 1, Ataque = 2 }

    public static class PosicoesCapitao
    {
        public static bool Valida(PosicaoCapitao posicao)
        { return posicao == PosicaoCapitao.Defesa || posicao == PosicaoCapitao.Meio || posicao == PosicaoCapitao.Ataque; }

        public static string Nome(PosicaoCapitao posicao)
        { return posicao == PosicaoCapitao.Ataque ? "ATTACK" : posicao == PosicaoCapitao.Meio ? "MIDFIELD" : "DEFENSE"; }

        public static string Sigla(PosicaoCapitao posicao)
        { return posicao == PosicaoCapitao.Ataque ? "FWD" : posicao == PosicaoCapitao.Meio ? "MID" : "DEF"; }
    }

    [CreateAssetMenu(fileName = "NovoCapitao", menuName = "Futebol de Discos/Capitão convidado", order = 20)]
    public sealed class CapitaoConvidado : ScriptableObject
    {
        public string nomeCapitao = "New Captain";
        public Sprite foto;
        [Range(1, 100)]
        [Tooltip("Força individual do capitão. Não altera o overall nem os jogadores do time.")]
        public int overall = 75;
        [Tooltip("Posição de base do capitão e preferência na pressão. Respeita a postura do time e a limitação à própria metade.")]
        public PosicaoCapitao posicao = PosicaoCapitao.Defesa;

        // Match the existing TeamData convention: asset file names identify saved participants.
        public string Identificador { get { return name; } }

        public JogadorDados CriarJogador()
        {
            return new JogadorDados { nomeJogador = nomeCapitao.Trim(), foto512x512 = foto };
        }
    }

    [Serializable]
    public sealed class VinculoCapitaoConvidado
    {
        public TeamData time;
        public CapitaoConvidado capitao;
    }
}
