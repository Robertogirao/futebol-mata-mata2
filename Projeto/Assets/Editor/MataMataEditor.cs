#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(MataMataCampeonato))]
public sealed class MataMataEditor : Editor
{
    private const string MenuCompletar =
        "Tools/Canal/Mata-mata/Completar participantes com fichas selecionadas";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MataMataCampeonato campeonato = (MataMataCampeonato)target;
        int vagas = Math.Max(0, (int)campeonato.tamanhoChave);
        int preenchidas = ContarPreenchidas(campeonato, vagas);
        int faltantes = vagas - preenchidas;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "Participantes da chave",
            preenchidas + " / " + vagas);

        string resumo;
        MessageType tipo;

        if (faltantes == 0)
        {
            resumo = "Todas as vagas estão preenchidas. A validação confere " +
                "também fichas repetidas e os demais dados do torneio.";
            tipo = MessageType.Info;
        }
        else if (campeonato.permitirFolgas)
        {
            resumo = "Faltam " + faltantes + " fichas. As vagas vazias serão " +
                "folgas (BYE), pois Permitir Folgas está habilitado.";
            tipo = MessageType.Info;
        }
        else
        {
            resumo = "Faltam " + faltantes + " fichas para uma chave de " +
                vagas + " participantes. Preencha essas vagas antes de " +
                "iniciar o torneio.";
            tipo = MessageType.Warning;
        }

        resumo += " Apenas as primeiras " + vagas +
            " posições da lista são usadas nesta chave.";
        EditorGUILayout.HelpBox(resumo, tipo);

        if (!string.IsNullOrEmpty(campeonato.ultimoErro))
            EditorGUILayout.HelpBox(campeonato.ultimoErro, MessageType.Warning);

        if (GUILayout.Button("Validar configuração"))
        {
            if (campeonato.ValidarConfiguracao())
                Debug.Log("Mata-mata: configuração válida.", campeonato);
            else
                Debug.LogError("Mata-mata: " + campeonato.ultimoErro, campeonato);

            Repaint();
        }

        if (GUILayout.Button("Abrir pasta do torneio"))
            AbrirPasta(campeonato);
    }

    private static int ContarPreenchidas(MataMataCampeonato campeonato, int vagas)
    {
        int quantidade = 0;
        if (campeonato.participantes == null)
            return quantidade;

        int limite = Math.Min(vagas, campeonato.participantes.Count);
        for (int i = 0; i < limite; i++)
        {
            if (campeonato.participantes[i] != null)
                quantidade++;
        }

        return quantidade;
    }

    private static void AbrirPasta(MataMataCampeonato campeonato)
    {
        try
        {
            string pasta = campeonato.PastaTorneio;
            if (string.IsNullOrEmpty(pasta) || !Directory.Exists(pasta))
            {
                Debug.LogWarning(
                    "Mata-mata: a pasta do torneio ainda não existe. " +
                    "Ela ficará disponível após salvar o progresso.",
                    campeonato);
                return;
            }

            EditorUtility.RevealInFinder(pasta);
        }
        catch (Exception erro)
        {
            Debug.LogWarning(
                "Mata-mata: não foi possível abrir a pasta. " + erro.Message,
                campeonato);
        }
    }

    [MenuItem(MenuCompletar)]
    private static void CompletarParticipantes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Mata-mata: saia do Play Mode para editar os participantes.");
            return;
        }

        MataMataCampeonato campeonato = LocalizarCampeonato();
        if (campeonato == null)
            return;

        TeamData[] selecionadas = Selection.GetFiltered<TeamData>(SelectionMode.DeepAssets);
        if (selecionadas.Length == 0)
        {
            Debug.LogWarning(
                "Mata-mata: selecione fichas TeamData ou uma pasta que contenha " +
                "essas fichas no painel Project.",
                campeonato);
            return;
        }

        // O nome define a ordem; o caminho desempata nomes iguais de forma estável.
        Array.Sort(selecionadas, CompararFichas);

        int vagas = Math.Max(0, (int)campeonato.tamanhoChave);
        int preenchidas = ContarPreenchidas(campeonato, vagas);
        if (preenchidas == vagas)
        {
            Debug.Log("Mata-mata: todas as vagas já estão preenchidas.", campeonato);
            return;
        }

        HashSet<TeamData> usadas = new HashSet<TeamData>();
        if (campeonato.participantes != null)
        {
            int limite = Math.Min(vagas, campeonato.participantes.Count);
            for (int i = 0; i < limite; i++)
            {
                if (campeonato.participantes[i] != null)
                    usadas.Add(campeonato.participantes[i]);
            }
        }

        List<TeamData> disponiveis = new List<TeamData>();
        foreach (TeamData ficha in selecionadas)
        {
            if (ficha != null && usadas.Add(ficha))
                disponiveis.Add(ficha);
        }

        Undo.RecordObject(campeonato, "Completar participantes do mata-mata");

        bool alterou = false;
        if (campeonato.participantes == null)
        {
            campeonato.participantes = new List<TeamData>();
            alterou = true;
        }

        while (campeonato.participantes.Count < vagas)
        {
            campeonato.participantes.Add(null);
            alterou = true;
        }

        int adicionadas = 0;
        for (int i = 0; i < vagas && adicionadas < disponiveis.Count; i++)
        {
            if (campeonato.participantes[i] != null)
                continue;

            campeonato.participantes[i] = disponiveis[adicionadas++];
            alterou = true;
        }

        if (alterou)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(campeonato))
                PrefabUtility.RecordPrefabInstancePropertyModifications(campeonato);

            EditorUtility.SetDirty(campeonato);
            EditorSceneManager.MarkSceneDirty(campeonato.gameObject.scene);
        }

        Debug.Log(
            "Mata-mata: " + adicionadas + " fichas adicionadas. " +
            ContarPreenchidas(campeonato, vagas) + " / " + vagas +
            " vagas preenchidas. As posições já preenchidas foram preservadas.",
            campeonato);
    }

    [MenuItem(MenuCompletar, true)]
    private static bool PodeCompletarParticipantes()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static MataMataCampeonato LocalizarCampeonato()
    {
        if (Selection.activeGameObject != null)
        {
            MataMataCampeonato selecionado =
                Selection.activeGameObject.GetComponent<MataMataCampeonato>();

            if (EstaNaCena(selecionado))
                return selecionado;
        }

        MataMataCampeonato encontrado = null;
        MataMataCampeonato[] campeonatos =
            UnityEngine.Object.FindObjectsByType<MataMataCampeonato>(
                FindObjectsInactive.Include);

        foreach (MataMataCampeonato campeonato in campeonatos)
        {
            if (!EstaNaCena(campeonato))
                continue;

            if (encontrado != null)
            {
                Debug.LogWarning(
                    "Mata-mata: há mais de um controlador nas cenas abertas. " +
                    "Selecione o GameObject do torneio desejado antes de completar as fichas.");
                return null;
            }

            encontrado = campeonato;
        }

        if (encontrado == null)
            Debug.LogWarning("Mata-mata: nenhum controlador foi encontrado nas cenas abertas.");

        return encontrado;
    }

    private static bool EstaNaCena(MataMataCampeonato campeonato)
    {
        return campeonato != null &&
            !EditorUtility.IsPersistent(campeonato) &&
            campeonato.gameObject.scene.IsValid() &&
            campeonato.gameObject.scene.isLoaded &&
            !EditorSceneManager.IsPreviewSceneObject(campeonato);
    }

    private static int CompararFichas(TeamData a, TeamData b)
    {
        if (a == b)
            return 0;
        if (a == null)
            return 1;
        if (b == null)
            return -1;

        int nomes = string.Compare(a.name, b.name, StringComparison.Ordinal);
        return nomes != 0 ? nomes : string.Compare(
            AssetDatabase.GetAssetPath(a),
            AssetDatabase.GetAssetPath(b),
            StringComparison.Ordinal);
    }
}
#endif
