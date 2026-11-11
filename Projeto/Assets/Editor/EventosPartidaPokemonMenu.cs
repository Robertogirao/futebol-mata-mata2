#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EventosPartidaPokemonMenu
{
    [MenuItem("Tools/Pokémon/Configurar eventos da partida")]
    public static void Configurar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Saia do Play para salvar os ajustes dos eventos.");
            return;
        }
        LigaPokemon liga = Object.FindFirstObjectByType<LigaPokemon>();
        if (liga == null)
        {
            Debug.LogWarning("Abra uma cena da liga Pokémon.");
            return;
        }
        EventosPartidaPokemon eventos = liga.GetComponent<EventosPartidaPokemon>();
        if (eventos == null) eventos = Undo.AddComponent<EventosPartidaPokemon>(liga.gameObject);
        Selection.activeGameObject = liga.gameObject;
        EditorSceneManager.MarkSceneDirty(liga.gameObject.scene);
        Debug.Log("Configure os eventos e salve a cena com Ctrl+S. Eventos Por Partida controla Snorlax Rage e Rocket Frenzy; Eventos Gigante Por Partida controla o terceiro especial separadamente. Ajuste também duração e tamanho do gigante. Os especiais têm aviso prévio e não se sobrepõem.", eventos);
    }
}
#endif
