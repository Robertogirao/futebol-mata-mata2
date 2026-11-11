#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LancadorEquipeRocketMenu
{
    [MenuItem("Tools/Pokémon/Configurar lançador da Equipe Rocket")]
    public static void Configurar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Saia do Play para salvar os ajustes do lançador.");
            return;
        }
        LigaPokemon liga = Object.FindFirstObjectByType<LigaPokemon>();
        if (liga == null)
        {
            Debug.LogWarning("Abra primeiro uma cena da liga Pokémon.");
            return;
        }
        TournamentManager motor = liga.GetComponent<TournamentManager>();
        AutoBallPipe cano = motor != null ? motor.canoAutomatizado : null;
        if (cano == null) cano = Object.FindFirstObjectByType<AutoBallPipe>();
        if (cano == null)
        {
            Debug.LogWarning("A cena não contém AutoBallPipe.");
            return;
        }
        LancadorEquipeRocket lancador = cano.GetComponent<LancadorEquipeRocket>();
        if (lancador == null) lancador = Undo.AddComponent<LancadorEquipeRocket>(cano.gameObject);
        Selection.activeGameObject = cano.gameObject;
        EditorSceneManager.MarkSceneDirty(cano.gameObject.scene);
        Debug.Log("Ajuste distância, velocidade, tamanho da arte e saída em Lancador Equipe Rocket. Salve a cena com Ctrl+S. Os ajustes ficam salvos nesta cena da liga.", lancador);
    }
}
#endif
