using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Data-only adapters. The harness compiles the real HUD, player cards, banner,
// team assets and presentation theme; tournament logic is validated separately.
public sealed class UniversalUI : MonoBehaviour
{
    public Image escudoTimeA, escudoTimeB, logoCampeonato;
    public TextMeshProUGUI placarTimeA, placarTimeB, textoCronometro;
    public GameObject bannerGol;
    public Image[] fotosTitularesA, fotosTitularesB;
    public TextMeshProUGUI[] nomesTitularesA, nomesTitularesB, numerosTitularesA, numerosTitularesB;
}
public sealed class LigaCampeonato
{
    public sealed class Linha { public string id, TEAM; public int POS, PTS, P, W, D, L, GD; }
    public sealed class Jogo { public string a, b, nomeA, nomeB; public bool registrado; public int golsA, golsB; }
}
namespace FutebolDeDiscos
{
    public enum FormatoDiscos { Liga, MataMata }
    public sealed class ProjetoFutebolDiscos : MonoBehaviour
    {
        public string nomeCampeonato = "WORLD DISC CUP";
        public TemaCompeticao tema;
        public FormatoDiscos formato;
        public int discosPorTime = 3;
        public int TotalDiscosPorTime { get { return discosPorTime; } }
        public string tituloRodape = "DISC FOOTBALL", subtituloRodape = "AUTOMATIC DISC FOOTBALL";
        public bool mostrarTituloRodape = true, mostrarSubtituloRodape = true, mostrarDetalhesFormato = true;
    }
}
