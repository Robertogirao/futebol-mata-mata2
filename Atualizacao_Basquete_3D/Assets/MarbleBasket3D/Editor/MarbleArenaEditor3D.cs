#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MarbleBasket3D.Editor
{
    public static class MarbleArenaEditor3D
    {
        private const string Root = "Assets/MarbleBasket3D";
        private const string ScenePath = Root + "/Scenes/MarbleBasket3D.unity";
        [MenuItem("Tools/Basquete 3D/Abrir cena de simulação")]
        public static void Open()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath); Selection.activeObject = Object.FindFirstObjectByType<ArenaBuilder3D>();
        }
        [MenuItem("Tools/Basquete 3D/Gerar arena visível no Editor")]
        public static void BuildVisible()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var builder = Object.FindFirstObjectByType<ArenaBuilder3D>();
            if (builder == null) throw new System.InvalidOperationException("Abra a cena MarbleBasket3D fornecida no pacote.");
            builder.Build(); PersistGeneratedResources(builder.transform);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = builder.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
        }
        [MenuItem("Tools/Basquete 3D/Selecionar configurações")]
        public static void Settings() { Selection.activeObject = AssetDatabase.LoadAssetAtPath<ArenaSettings3D>(Root + "/Settings/Arena3D_Settings.asset"); }
        private static void PersistGeneratedResources(Transform parent)
        {
            string folder = Root + "/Generated"; Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            foreach (var renderer in parent.GetComponentsInChildren<Renderer>())
                foreach (var material in renderer.sharedMaterials) Persist(material, folder);
            foreach (var filter in parent.GetComponentsInChildren<MeshFilter>()) Persist(filter.sharedMesh, folder);
            foreach (var collider in parent.GetComponentsInChildren<Collider>()) Persist(collider.sharedMaterial, folder);
            AssetDatabase.SaveAssets();
        }
        private static void Persist(Object obj, string folder)
        {
            if (obj == null || EditorUtility.IsPersistent(obj)) return;
            string name = obj.name;
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + name + ".asset");
            AssetDatabase.CreateAsset(obj, path);
        }
    }
    [CustomEditor(typeof(ArenaBuilder3D))]
    public sealed class ArenaBuilderInspector3D : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("A cena constrói o circuito ao entrar em Play. Para visualizar antes, use Tools > Basquete 3D > Gerar arena visível no Editor. O evento On Result é preservado no objeto raiz.", MessageType.Info);
            if (!Application.isPlaying && GUILayout.Button("Selecionar configurações")) MarbleArenaEditor3D.Settings();
        }
    }
}
#endif
