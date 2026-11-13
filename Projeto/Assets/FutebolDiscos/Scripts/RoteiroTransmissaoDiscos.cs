using System;
using UnityEngine;

namespace FutebolDeDiscos
{
    [Serializable]
    public sealed class RoteiroTransmissaoDiscos
    {
        [InspectorName("Título da prévia")] public string tituloPrevia = "ARENA";
        [InspectorName("Título do confronto")] public string tituloDuelo = "CHALLENGERS";
        [InspectorName("Chamada para começar")] public string chamadaJogo = "START";
        [InspectorName("Separador dos times")] public string separador = "VERSUS";
        [InspectorName("Título do chaveamento")] public string tituloChaveamento = "TOURNAMENT GRID";
        [InspectorName("Fase concluída")] public string faseConcluida = "STAGE COMPLETE";
        [InspectorName("Legenda do quadro")] public string legendaQuadro = "PATH TO GLORY";
        [InspectorName("Chamada da vitória")] public string chamadaVitoria = "ADVANCE TO THE NEXT SECTOR";
        [InspectorName("Chamada da final")] public string chamadaFinal = "THE FINAL CHALLENGE";
        [InspectorName("Título do campeão")] public string tituloCampeao = "ARENA CHAMPION";
        [InspectorName("Legenda do campeão")] public string legendaCampeao = "COMPETITION CONQUERED";
        [InspectorName("Título da classificação")] public string tituloClassificacao = "LEAGUE STATUS";
        [InspectorName("Agenda da rodada")] public string agenda = "NEXT ENCOUNTERS";
        [InspectorName("Resultados da rodada")] public string resultados = "MATCH DATA";
        [InspectorName("Rótulo da rodada")] public string rodada = "ROUND";
        [InspectorName("Rótulo da partida")] public string partida = "ENCOUNTER";

        public static string Texto(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? "" : valor.Replace('\r', ' ').Replace('\n', ' ').Trim();
        }

        public static string Numero(string rotulo, int numero)
        {
            string texto = Texto(rotulo);
            return (texto.Length > 0 ? texto + " " : "") + Mathf.Max(1, numero);
        }
    }
}
