using System;
using UnityEngine;

namespace FutebolDeDiscos
{
    [CreateAssetMenu(fileName = "NovoCapitao", menuName = "Futebol de Discos/Capitão convidado", order = 20)]
    public sealed class CapitaoConvidado : ScriptableObject
    {
        public string nomeCapitao = "Novo Capitão";
        public Sprite foto;
        [Range(1, 100)]
        [Tooltip("Força individual do capitão. Não altera o overall nem os jogadores do time.")]
        public int overall = 75;

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
