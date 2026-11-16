#if UNITY_EDITOR
using System;
using FutebolDeDiscos;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BasqueteArena.Editor
{
    public sealed class ConfiguradorBasquete : EditorWindow
    {
        private Vector2 scroll;
        [MenuItem("Tools/Basquete/Painel da arena")]
        public static void Abrir() { GetWindow<ConfiguradorBasquete>("Basquete"); }
        [MenuItem("Tools/Basquete/Abrir Liga NBA")]
        private static void Liga() { AbrirCena("Assets/Scenes/Basquete/Liga_NBA.unity"); }
        [MenuItem("Tools/Basquete/Abrir Mata-mata NBA")]
        private static void Copa() { AbrirCena("Assets/Scenes/Basquete/MataMata_NBA.unity"); }
        [MenuItem("Tools/Basquete/Abrir Liga NBA com capitaes")]
        private static void LigaCapitaes() { AbrirCena("Assets/Scenes/Basquete/Liga_NBA_Capitaes.unity"); }
        [MenuItem("Tools/Basquete/Abrir Mata-mata NBA com capitaes")]
        private static void CopaCapitaes() { AbrirCena("Assets/Scenes/Basquete/MataMata_NBA_Capitaes.unity"); }
        private static void AbrirCena(string path)
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(path); Abrir();
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("NBA — Arena de sobrevivencia", EditorStyles.boldLabel);
            var projeto = FindFirstObjectByType<ProjetoFutebolDiscos>();
            if (projeto == null || !projeto.modoBasquete)
            {
                EditorGUILayout.HelpBox("Abra uma cena de basquete pelo menu Tools/Basquete. As cenas de futebol continuam independentes.", MessageType.Info); return;
            }
            if (EditorApplication.isPlaying) { EditorGUILayout.HelpBox("Pare o Play para configurar o campeonato.", MessageType.Info); return; }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            var s = new SerializedObject(projeto); s.Update();
            foreach (string campo in new[] { "nomeCampeonato", "competicaoId", "temporadaId", "formato", "tema", "temaBasquete", "quantidadeTimes", "quantidadeRodadas", "participantes", "usarCapitaesConvidados", "capitaoExtra", "discosPorTime", "catalogoCapitaes", "capitaesFixos", "duracaoPartida", "tempoAbertura", "tempoFotos", "tempoTabela" })
                EditorGUILayout.PropertyField(s.FindProperty(campo), true);
            if (s.ApplyModifiedProperties()) EditorSceneManager.MarkSceneDirty(projeto.gameObject.scene);
            EditorGUILayout.HelpBox("Com capitao extra: 4 bolas + capitao. Sem capitao extra: use 5 bolas. Cada equipe fica limitada a 5. Use nova temporada ao mudar participantes ou regras.", MessageType.Info);
            if (projeto.temaBasquete != null)
            {
                EditorGUILayout.Space(); EditorGUILayout.LabelField("Arena deste campeonato", EditorStyles.boldLabel);
                var arena = new SerializedObject(projeto.temaBasquete); arena.Update();
                var prop = arena.GetIterator(); bool filhos = true;
                while (prop.NextVisible(filhos)) { filhos = false; if (prop.name != "m_Script") EditorGUILayout.PropertyField(prop, true); }
                arena.ApplyModifiedProperties();
                if (GUILayout.Button("Duplicar arena para este campeonato"))
                {
                    string path = AssetDatabase.GenerateUniqueAssetPath("Assets/BasqueteArena/Temas/ArenaPersonalizada.asset");
                    AssetDatabase.CreateAsset(Instantiate(projeto.temaBasquete), path);
                    Undo.RecordObject(projeto, "Arena do campeonato"); projeto.temaBasquete = AssetDatabase.LoadAssetAtPath<TemaArenaBasquete>(path);
                    EditorUtility.SetDirty(projeto); EditorSceneManager.MarkSceneDirty(projeto.gameObject.scene); AssetDatabase.SaveAssets();
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
