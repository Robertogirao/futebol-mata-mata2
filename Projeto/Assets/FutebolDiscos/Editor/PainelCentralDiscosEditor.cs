#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FutebolDeDiscos
{
    /// <summary>Keep implementation components usable alone, but route this project to one inspector.</summary>
    public static class PainelCentralDiscosEditor
    {
        public static bool MostrarAviso(Component componente)
        {
            if (componente == null) return false;
            ProjetoFutebolDiscos projeto = componente.GetComponent<ProjetoFutebolDiscos>();
            if (projeto == null) return false;
            EditorGUILayout.HelpBox(
                "Os controles deste componente estão reunidos em Projeto Futebol Discos, no mesmo objeto. " +
                "Use Configurações da Liga ou Configurações do Mata-mata no painel principal.", MessageType.Info);
            if (GUILayout.Button("Selecionar painel central")) Selection.activeGameObject = projeto.gameObject;
            return true;
        }
    }

    public abstract class ComponenteInternoDiscosEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (!PainelCentralDiscosEditor.MostrarAviso(target as Component)) DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(TournamentManager))]
    public sealed class TournamentManagerDiscosEditor : ComponenteInternoDiscosEditor { }
    [CustomEditor(typeof(LigaCampeonato))]
    public sealed class LigaCampeonatoDiscosEditor : ComponenteInternoDiscosEditor { }
    [CustomEditor(typeof(UniversalUI))]
    public sealed class UniversalUIDiscosEditor : ComponenteInternoDiscosEditor { }
    [CustomEditor(typeof(AberturaJogoUI))]
    public sealed class AberturaJogoUIDiscosEditor : ComponenteInternoDiscosEditor { }
    [CustomEditor(typeof(ClassificacaoUI))]
    public sealed class ClassificacaoUIDiscosEditor : ComponenteInternoDiscosEditor { }
    [CustomEditor(typeof(ChaveamentoUI))]
    public sealed class ChaveamentoUIDiscosEditor : ComponenteInternoDiscosEditor { }
    [CustomEditor(typeof(GoalBannerUI))]
    public sealed class GoalBannerUIDiscosEditor : ComponenteInternoDiscosEditor { }
}
#endif
