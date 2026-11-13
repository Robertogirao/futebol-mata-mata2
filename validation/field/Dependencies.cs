using TMPro;
using UnityEngine;
public class TemaCompeticao : ScriptableObject { public TMP_FontAsset fonteTexto; }
namespace FutebolDeDiscos
{
    public sealed class ProjetoFutebolDiscos : MonoBehaviour
    {
        public TemaCompeticao tema;
        public Sprite fundoCampo;
        public Color corGramado = new Color(.035f,.22f,.22f);
        public Color corLinhas = new Color(.25f,.93f,.92f);
    }
    public sealed class GolDiscos : MonoBehaviour { public bool esquerda; }
}
