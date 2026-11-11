#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Configuração da cena de liga sem criar fichas intermediárias no projeto.</summary>
[CustomEditor(typeof(LigaPokemon))]
public sealed class LigaPokemonEditor : Editor
{
    public const string CaminhoCena = "Assets/Scenes/Ligas/Liga_Pokemon.unity";
    private bool mostrarParticipantes = true;

    public override void OnInspectorGUI()
    {
        LigaPokemon liga = (LigaPokemon)target;
        bool emPlay = EditorApplication.isPlayingOrWillChangePlaymode;
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(emPlay))
        {
            EditorGUILayout.LabelField("Identidade e imagens", EditorStyles.boldLabel);
            Campo("nomeLiga", "Nome da liga");
            Campo("ligaId", "Identificador da liga");
            Campo("temporadaId", "Temporada");
            Campo("tema", "Tema base");
            Campo("fundoArena", "Fundo da arena");
            Campo("fundoClassificacao", "Fundo da classificação");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Participantes", EditorStyles.boldLabel);
            SerializedProperty quantidade = serializedObject.FindProperty("quantidadePokemon");
            int novaQuantidade = Math.Max(2,
                EditorGUILayout.IntField("Quantidade de Pokémon", quantidade.intValue));
            if (!emPlay) quantidade.intValue = novaQuantidade;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Atalhos", GUILayout.Width(55f));
            if (GUILayout.Button("10 Pokémon")) quantidade.intValue = 10;
            if (GUILayout.Button("15 Pokémon")) quantidade.intValue = 15;
            if (GUILayout.Button("20 Pokémon")) quantidade.intValue = 20;
            EditorGUILayout.EndHorizontal();

            SerializedProperty lista = serializedObject.FindProperty("participantes");
            if (!emPlay) AjustarVagas(lista, quantidade.intValue);
            mostrarParticipantes = EditorGUILayout.Foldout(mostrarParticipantes,
                "Pokémon da liga (" + lista.arraySize + ")", true);
            if (mostrarParticipantes)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < lista.arraySize; i++)
                    EditorGUILayout.PropertyField(lista.GetArrayElementAtIndex(i),
                        new GUIContent("Pokémon " + (i + 1)));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rodadas e continuidade", EditorStyles.boldLabel);
            SerializedProperty rodadas = serializedObject.FindProperty("quantidadeRodadas");
            int novasRodadas = Math.Max(1,
                EditorGUILayout.IntField("Quantidade de rodadas", rodadas.intValue));
            if (!emPlay) rodadas.intValue = novasRodadas;
            Campo("sementeCalendario", "Semente do calendário");
            Campo("umaRodadaPorPlay", "Uma rodada por Play");
            Campo("intervaloEntreRodadas", "Intervalo entre rodadas");
            Campo("duracaoPartida", "Duração de cada partida");
            Campo("mostrarAbertura", "Mostrar abertura dos confrontos");

            int porTurno = quantidade.intValue % 2 == 0
                ? quantidade.intValue - 1 : quantidade.intValue;
            EditorGUILayout.HelpBox("Um turno completo tem " + porTurno + " rodadas. " +
                (quantidade.intValue % 2 == 0
                    ? "Todos os Pokémon jogam uma vez em cada rodada. "
                    : "Um Pokémon folga em cada rodada; a folga não vale pontos. ") +
                "Rodadas além de um turno continuam no returno.", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.HelpBox("Vitória: 3 pontos. Empate: 1 ponto. Derrota: 0. " +
            "Cada partida é salva e a classificação é confirmada automaticamente ao fim da rodada. " +
            "Altere participantes, calendário ou temporada fora do Play.", MessageType.Info);

        using (new EditorGUI.DisabledScope(emPlay))
        {
            if (GUILayout.Button("Sortear Pokémon do catálogo")) SortearParticipantes(liga);
            if (GUILayout.Button("Validar liga"))
            {
                if (liga.ValidarConfiguracao()) Debug.Log("Liga Pokémon: configuração válida.", liga);
                else Debug.LogWarning("Liga Pokémon: " + liga.ultimoErro, liga);
            }
            if (GUILayout.Button("Nova temporada")) NovaTemporada(liga);
        }
        if (GUILayout.Button("Abrir pasta da temporada")) AbrirPasta(liga);
        using (new EditorGUI.DisabledScope(!Application.isPlaying ||
            !liga.Preparada || liga.TemporadaConcluida))
        {
            if (GUILayout.Button("Continuar próxima rodada no Play")) liga.ContinuarNoPlay();
        }

        EditorGUILayout.Space();
        if (!string.IsNullOrWhiteSpace(liga.situacao))
            EditorGUILayout.HelpBox(liga.situacao, MessageType.Info);
        if (Application.isPlaying && liga.Preparada)
            EditorGUILayout.LabelField("Rodada atual", liga.RodadaAtual + " / " + liga.quantidadeRodadas);
        if (!string.IsNullOrWhiteSpace(liga.ultimoErro))
            EditorGUILayout.HelpBox(liga.ultimoErro, MessageType.Warning);
    }

    private void Campo(string nome, string rotulo)
    {
        SerializedProperty propriedade = serializedObject.FindProperty(nome);
        if (propriedade != null)
            EditorGUILayout.PropertyField(propriedade, new GUIContent(rotulo), true);
    }

    private static void AjustarVagas(SerializedProperty lista, int quantidade)
    {
        int anterior = lista.arraySize;
        if (anterior == quantidade) return;
        lista.arraySize = quantidade;
        // O Unity replica o último elemento ao expandir arrays de referências.
        // Novas vagas devem ficar vazias para o usuário escolher outra espécie.
        for (int i = anterior; i < quantidade; i++)
            lista.GetArrayElementAtIndex(i).objectReferenceValue = null;
    }

    private static void SortearParticipantes(LigaPokemon liga)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        try
        {
            int quantidade = Math.Max(2, liga.quantidadePokemon);
            var catalogo = new List<PokemonDados>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            string[] guids = AssetDatabase.FindAssets("t:PokemonDados");
            Array.Sort(guids, StringComparer.Ordinal);
            foreach (string guid in guids)
            {
                PokemonDados pokemon = AssetDatabase.LoadAssetAtPath<PokemonDados>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (pokemon == null) continue;
                pokemon.ExigirValido();
                if (ids.Add(pokemon.IdEstavel)) catalogo.Add(pokemon);
            }
            if (catalogo.Count < quantidade)
                throw new InvalidOperationException("O catálogo tem " + catalogo.Count +
                    " Pokémon válidos distintos, mas a liga pede " + quantidade + ".");

            var sorteio = new System.Random(Guid.NewGuid().GetHashCode());
            for (int i = catalogo.Count - 1; i > 0; i--)
            {
                int j = sorteio.Next(i + 1);
                PokemonDados atual = catalogo[i]; catalogo[i] = catalogo[j]; catalogo[j] = atual;
            }
            Undo.RecordObject(liga, "Sortear participantes da liga Pokémon");
            liga.quantidadePokemon = quantidade;
            liga.participantes = catalogo.GetRange(0, quantidade);
            liga.ultimoErro = "";
            MarcarAlteracao(liga);
            Debug.Log(quantidade + " Pokémon escolhidos. Salve a cena para manter a lista. " +
                "Para mudar os participantes de uma liga já iniciada, use Nova temporada.", liga);
        }
        catch (Exception erro)
        {
            liga.ultimoErro = erro.Message;
            Debug.LogWarning("Liga Pokémon: " + erro.Message, liga);
        }
    }

    private static void NovaTemporada(LigaPokemon liga)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Undo.RecordObject(liga, "Criar identificador de temporada Pokémon");
        liga.temporadaId = "temporada_" + DateTime.Now.ToString("yyyyMMdd_HHmmss",
            CultureInfo.InvariantCulture) + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        liga.ultimoErro = "";
        MarcarAlteracao(liga);
        Debug.Log("Nova temporada configurada. Os arquivos das temporadas anteriores foram preservados. " +
            "Salve a cena antes de iniciar.", liga);
    }

    private static void AbrirPasta(LigaPokemon liga)
    {
        try
        {
            string pasta = liga.PastaDaTemporada;
            Directory.CreateDirectory(pasta);
            EditorUtility.RevealInFinder(pasta);
        }
        catch (Exception erro) { Debug.LogWarning("Liga Pokémon: " + erro.Message, liga); }
    }

    private static void MarcarAlteracao(LigaPokemon liga)
    {
        EditorUtility.SetDirty(liga);
        if (liga.gameObject.scene.IsValid() && !EditorUtility.IsPersistent(liga))
            EditorSceneManager.MarkSceneDirty(liga.gameObject.scene);
    }

    [MenuItem("Tools/Pokémon/Abrir liga Pokémon")]
    public static void AbrirCena()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Saia do Play antes de abrir a cena da liga Pokémon.");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CaminhoCena) == null)
        {
            Debug.LogWarning("A cena da liga Pokémon não foi encontrada em " + CaminhoCena + ".");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(CaminhoCena, OpenSceneMode.Single);
        LigaPokemon liga = UnityEngine.Object.FindAnyObjectByType<LigaPokemon>();
        if (liga != null) Selection.activeGameObject = liga.gameObject;
    }
}
#endif
