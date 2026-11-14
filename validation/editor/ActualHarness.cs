using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FutebolDeDiscos;
using UnityEditor;
using UnityEngine;

public static class ActualHarness
{
    static int checks;
    static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    static TeamData Team(int id) { return new TeamData { name = "Team_" + id, nomeEquipe = "Team " + id, elenco = new List<JogadorDados> { new JogadorDados { nomeJogador = "Player " + id } } }; }
    static ProjetoFutebolDiscos Manager()
    {
        var go = new GameObject("FutebolDiscos_Manager");
        go.SetActive(false);
        var manager = go.AddComponent<ProjetoFutebolDiscos>();
        manager.participantes = Enumerable.Range(1, 8).Select(Team).ToList();
        go.AddComponent<UniversalUI>();
        go.AddComponent<GoalBannerUI>();
        return manager;
    }
    static ProjetoFutebolDiscosEditor EditorFor(ProjetoFutebolDiscos manager)
    {
        return new ProjetoFutebolDiscosEditor { target = manager, serializedObject = new SerializedObject(manager) };
    }
    static void CheckSinglePanel(ProjetoFutebolDiscos manager, ProjetoFutebolDiscosEditor editor)
    {
        var ui = manager.GetComponent<UniversalUI>();
        var banner = manager.GetComponent<GoalBannerUI>();
        banner.tempoDeExibicao = 1.75f;
        banner.intervaloEntreBanners = .37f;
        ui.volumeSons = .42f;
        var sound = new AudioClip(); ui.somGol = sound;
        manager.duracaoPartida = 57f; manager.tempoAbertura = 8f; manager.tempoFotos = 6f;
        manager.formato = FormatoDiscos.Liga;
        EditorGUILayout.properties.Clear(); EditorGUILayout.labels.Clear();
        editor.OnInspectorGUI();
        Check(EditorGUILayout.labels.Contains("Configurações da Liga") && !EditorGUILayout.labels.Contains("Configurações do Mata-mata"), "league settings have a separate selected-mode section");
        Check(EditorGUILayout.properties.Contains("quantidadeRodadas") && !EditorGUILayout.properties.Contains("permitirFolgas"), "league panel shows rounds and omits knockout-only byes");
        foreach (string field in new[] { "duracaoPartida", "tempoAbertura", "tempoFotos", "tempoTransicaoFotos", "tempoTabela", "intervaloEntrePartidas", "tempoDeExibicao", "intervaloEntreBanners", "volumeSons", "somGol", "somApitoInicial", "somApitoFinal", "fundoExternoArena", "mostrarScannerFotos" })
            Check(EditorGUILayout.properties.Count(p => p == field) == 1, "central property is displayed exactly once: " + field);
        Check(!EditorGUILayout.properties.Contains("duracaoRealEmSegundos") && !EditorGUILayout.properties.Contains("tempoCongeladoPosJogo"), "runtime aliases do not duplicate the real-game or interval controls");
        Check(manager.duracaoPartida == 57f && manager.tempoAbertura == 8f && manager.tempoFotos == 6f, "existing serialized root times are preserved");
        Check(banner.tempoDeExibicao == 1.75f && banner.intervaloEntreBanners == .37f && ui.volumeSons == .42f && ui.somGol == sound, "existing internal sound/banner values are preserved in central view");
        EditorGUILayout.floats["Exibição do autor do gol (s)"] = 2.25f;
        EditorGUILayout.floats["Volume dos efeitos"] = .63f;
        editor.OnInspectorGUI(); EditorGUILayout.floats.Clear();
        Check(banner.tempoDeExibicao == 2.25f && ui.volumeSons == .63f, "central controls edit the existing component backing values");
        manager.formato = FormatoDiscos.MataMata; manager.quantidadeTimes = 8;
        EditorGUILayout.properties.Clear(); EditorGUILayout.labels.Clear();
        editor.OnInspectorGUI();
        Check(EditorGUILayout.labels.Contains("Configurações do Mata-mata") && !EditorGUILayout.labels.Contains("Configurações da Liga"), "knockout settings have a separate selected-mode section");
        Check(!EditorGUILayout.properties.Contains("quantidadeRodadas") && EditorGUILayout.properties.Contains("permitirFolgas"), "knockout panel shows byes and omits league-only rounds");
        manager.formato = FormatoDiscos.Liga;
        manager.duracaoPartida = 45f; manager.tempoAbertura = 5f; manager.tempoFotos = 4f;

        Type[] components = { typeof(TournamentManager), typeof(LigaCampeonato), typeof(UniversalUI), typeof(AberturaJogoUI), typeof(ClassificacaoUI), typeof(ChaveamentoUI), typeof(GoalBannerUI), typeof(MataMataCampeonato) };
        Type[] editors = { typeof(TournamentManagerDiscosEditor), typeof(LigaCampeonatoDiscosEditor), typeof(UniversalUIDiscosEditor), typeof(AberturaJogoUIDiscosEditor), typeof(ClassificacaoUIDiscosEditor), typeof(ChaveamentoUIDiscosEditor), typeof(GoalBannerUIDiscosEditor), typeof(MataMataEditor) };
        for (int i = 0; i < components.Length; i++)
        {
            var component = manager.gameObject.GetComponent(components[i]) ?? manager.gameObject.AddComponent(components[i]);
            var legacyEditor = (Editor)Activator.CreateInstance(editors[i]); legacyEditor.target = component;
            int before = Editor.defaultDrawCount;
            EditorGUILayout.properties.Clear(); legacyEditor.OnInspectorGUI();
            Check(Editor.defaultDrawCount == before && EditorGUILayout.properties.Count == 0, "internal inspector has no duplicate controls: " + components[i].Name);
            Check(((Behaviour)component).enabled, "inspector routing never disables component: " + components[i].Name);
            var standalone = new GameObject("Standalone " + components[i].Name); standalone.SetActive(false);
            legacyEditor.target = standalone.AddComponent(components[i]);
            legacyEditor.OnInspectorGUI();
            Check(Editor.defaultDrawCount == before + 1, "standalone component retains normal inspector: " + components[i].Name);
            UnityEngine.Object.Destroy(standalone);
        }
        GUILayout.buttons.Add("Selecionar painel central");
        Check(PainelCentralDiscosEditor.MostrarAviso(ui) && Selection.activeGameObject == manager.gameObject, "legacy redirect selects central manager without mutating settings");
        EditorGUILayout.properties.Clear(); EditorGUILayout.labels.Clear();
    }
    static void CheckCornerControls(ProjetoFutebolDiscos manager, ProjetoFutebolDiscosEditor editor)
    {
        Check(manager.reporBolaPresaNoCanto && manager.tempoBolaPresaNoCanto == 3f &&
            manager.alcanceCantoBolaPresa == 1.6f && manager.pausaReposicaoBolaPresa == .8f,
            "corner recovery defaults are enabled with a three-second wait");
        var dataMethod = typeof(ProjetoFutebolDiscos).GetMethod("CriarDadosConfiguracao", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (FormatoDiscos mode in new[] { FormatoDiscos.Liga, FormatoDiscos.MataMata })
        {
            manager.formato = mode;
            string savedConfiguration = JsonUtility.ToJson(dataMethod.Invoke(manager, null));
            EditorGUILayout.labels.Clear(); EditorGUILayout.properties.Clear();
            EditorGUILayout.floats["Tempo sem sair do canto (s)"] = 4.25f;
            EditorGUILayout.floats["Área de detecção do canto"] = 2.1f;
            EditorGUILayout.floats["Pausa antes da cobrança (s)"] = 1.25f;
            editor.OnInspectorGUI(); EditorGUILayout.floats.Clear();
            Check(EditorGUILayout.labels.Count(label => label == "Bola presa nos cantos") == 1 &&
                EditorGUILayout.properties.Count(property => property == "reporBolaPresaNoCanto") == 1,
                "one central corner-recovery group in mode " + mode);
            Check(manager.tempoBolaPresaNoCanto == 4.25f && manager.alcanceCantoBolaPresa == 2.1f && manager.pausaReposicaoBolaPresa == 1.25f,
                "central sliders edit actual recovery settings in mode " + mode);
            manager.reporBolaPresaNoCanto = false;
            Check(manager.ValidarConfiguracao(), "valid recovery controls can be disabled without invalidating " + mode);
            Check(savedConfiguration == JsonUtility.ToJson(dataMethod.Invoke(manager, null)),
                "changing recovery controls does not change the saved-season configuration in mode " + mode);
        }
        manager.formato = FormatoDiscos.Liga;
        foreach (string field in new[] { "tempoBolaPresaNoCanto", "alcanceCantoBolaPresa", "pausaReposicaoBolaPresa" })
        {
            FieldInfo setting = typeof(ProjetoFutebolDiscos).GetField(field);
            float original = (float)setting.GetValue(manager);
            foreach (float invalid in new[] { 0f, -1f, 11f, float.NaN, float.PositiveInfinity })
            {
                setting.SetValue(manager, invalid);
                Check(!manager.ValidarConfiguracao() && manager.ultimoErro.Contains("bola presa"), "validator rejects invalid recovery setting " + field + " = " + invalid);
            }
            setting.SetValue(manager, original);
        }
        manager.reporBolaPresaNoCanto = true; manager.tempoBolaPresaNoCanto = 1f;
        manager.alcanceCantoBolaPresa = .8f; manager.pausaReposicaoBolaPresa = .2f;
        Check(manager.ValidarConfiguracao(), "minimum recovery slider values validate");
        manager.tempoBolaPresaNoCanto = 10f; manager.alcanceCantoBolaPresa = 3f; manager.pausaReposicaoBolaPresa = 3f;
        Check(manager.ValidarConfiguracao(), "maximum recovery slider values validate");
        manager.tempoBolaPresaNoCanto = 3f; manager.alcanceCantoBolaPresa = 1.6f; manager.pausaReposicaoBolaPresa = .8f;
        EditorGUILayout.labels.Clear(); EditorGUILayout.properties.Clear();
    }
    static void CheckSoundSelection()
    {
        foreach (FormatoDiscos mode in new[] { FormatoDiscos.Liga, FormatoDiscos.MataMata })
        {
            var manager = Manager(); manager.formato = mode;
            manager.tema = ScriptableObject.CreateInstance<TemaCompeticao>();
            var ui = manager.GetComponent<UniversalUI>();
            var source = manager.gameObject.AddComponent<AudioSource>();
            var old = new AudioClip { name = "Old clip" };
            var goal = new AudioClip { name = "Selected goal" };
            var start = new AudioClip { name = "Selected kickoff" };
            var finish = new AudioClip { name = "Selected final whistle" };
            var opening = new AudioClip { name = "Selected opening" };
            source.clip = old; ui.somGol = ui.somApitoInicial = ui.somApitoFinal = old;
            var editor = EditorFor(manager);
            typeof(ProjetoFutebolDiscosEditor).GetField("mostrarTema", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(editor, true);
            EditorGUILayout.objects["Som do gol"] = goal;
            EditorGUILayout.objects["Apito de início"] = start;
            EditorGUILayout.objects["Apito de encerramento"] = finish;
            EditorGUILayout.objects["Fonte de áudio dos efeitos (opcional)"] = source;
            EditorGUILayout.objects["Som das aberturas e prévias"] = opening;
            EditorGUILayout.floats["Volume dos efeitos"] = .57f;
            PrefabUtility.instances.Add(ui);
            int prefabChanges = PrefabUtility.records, sceneChanges = UnityEditor.SceneManagement.EditorSceneManager.dirty;
            EditorGUILayout.properties.Clear(); editor.OnInspectorGUI();
            EditorGUILayout.objects.Clear(); EditorGUILayout.floats.Clear();
            Check(ui.somGol == goal && ui.somApitoInicial == start && ui.somApitoFinal == finish && ui.fonteDeAudio == source && ui.volumeSons == .57f,
                "central object selectors assign the actual three event clips and source in " + mode);
            Check(manager.tema.somAbertura == opening && EditorGUILayout.properties.Count(p => p == "somAbertura") == 1,
                "opening clip is separately editable once, even when theme foldout is open, in " + mode);
            Check(EditorUtility.dirtyObjects.Contains(ui) && EditorUtility.dirtyObjects.Contains(manager.tema) &&
                PrefabUtility.records > prefabChanges && UnityEditor.SceneManagement.EditorSceneManager.dirty > sceneChanges,
                "audio edits mark their component/asset, scene and prefab override for persistence in " + mode);

            FieldInfo cacheField = typeof(ProjetoFutebolDiscosEditor).GetField("dadosSons", BindingFlags.NonPublic | BindingFlags.Instance);
            var cached = (SerializedObject)cacheField.GetValue(editor);
            var delayedGoal = new AudioClip { name = "Retained property selection" };
            cached.FindProperty("somGol").objectReferenceValue = delayedGoal;
            Check(ui.somGol == goal, "facade models a queued serialized selection before Apply in " + mode);
            editor.OnInspectorGUI();
            Check(ReferenceEquals(cached, cacheField.GetValue(editor)) && ui.somGol == delayedGoal,
                "retained audio property edits are applied before the next Inspector refresh in " + mode);
            var delayedFinal = new AudioClip { name = "Selected before closing Inspector" };
            cached.FindProperty("somApitoFinal").objectReferenceValue = delayedFinal;
            typeof(ProjetoFutebolDiscosEditor).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(editor, null);
            editor = EditorFor(manager); editor.OnInspectorGUI();
            Check(ui.somGol == delayedGoal && ui.somApitoInicial == start && ui.somApitoFinal == delayedFinal && manager.tema.somAbertura == opening,
                "closing and rebuilding the Inspector retains all separate audio assignments in " + mode);

            ui.ativarGoldenGoal = false; ui.IniciarCronometro(); ui.RegistrarGol(true, 0f);
            Time.captureDeltaTime = 46f;
            typeof(UniversalUI).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
            Time.captureDeltaTime = 0f;
            Check(source.playedClips.SequenceEqual(new[] { start, delayedGoal, delayedFinal }),
                "real kickoff, goal and final events dispatch the selected clips instead of the old clip in " + mode);
            Check(source.playedVolumes.All(volume => Math.Abs(volume - .57f) < .001f),
                "every real event applies the selected central effect volume in " + mode);
            Check(!source.playedClips.Contains(old) && !source.playedClips.Contains(opening),
                "match effects do not substitute a legacy or opening clip in " + mode);
            ui.CancelarPartida();
            EditorGUILayout.objects["Som do gol"] = null;
            editor.OnInspectorGUI(); EditorGUILayout.objects.Clear();
            ui.IniciarCronometro(); int played = source.playedClips.Count; ui.RegistrarGol(false, 0f);
            Check(ui.somGol == null && source.playedClips.Count == played,
                "clearing a central goal clip means silence without an old-clip fallback in " + mode);
            ui.CancelarPartida(); PrefabUtility.instances.Remove(ui);
            UnityEngine.Object.Destroy(manager.gameObject);
        }
        EditorGUILayout.properties.Clear(); EditorGUILayout.labels.Clear();
    }
    public static void Main()
    {
        Application.isPlaying = false;
        Application.persistentDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "actual-sandbox");
        if (Directory.Exists(Application.persistentDataPath)) Directory.Delete(Application.persistentDataPath, true);
        var manager = Manager();
        var editor = EditorFor(manager);
        CheckSinglePanel(manager, editor);
        CheckCornerControls(manager, editor);
        CheckSoundSelection();
        Check(manager.ValidarConfiguracao(), "actual root validates scene default count/rounds/physics: " + manager.ultimoErro);
        editor.OnInspectorGUI();
        Check(manager.quantidadeTimes == 8 && manager.participantes.Count == 8, "actual default remains eight");
        Check(manager.tamanhoDisco == 1f && manager.tamanhoBola == .42f, "actual safe sizes unchanged by GUI");
        foreach (string field in new[] { "nomeCampeonato", "competicaoId", "temporadaId", "tema", "fundoCampo", "corGramado", "corLinhas", "quantidadeRodadas", "sortearConfrontos", "semente", "umaRodadaOuFasePorPlay", "duracaoPartida", "tempoAbertura", "tempoFotos", "tempoTransicaoFotos", "tempoTabela", "intervaloEntrePartidas", "limitarDiscosNaMetade", "usarForcaDosTimes" })
            Check(EditorGUILayout.properties.Contains(field), "actual serialized root field wired: " + field);
        EditorGUILayout.ints["Quantidade de times"] = 15;
        editor.OnInspectorGUI();
        EditorGUILayout.ints.Clear();
        Check(manager.participantes.Count == 15 && manager.participantes.Skip(8).All(team => team == null), "actual expanded list never duplicates prior Unity list slot");
        for (int i = 8; i < 15; i++) manager.participantes[i] = Team(i + 1);
        GUILayout.buttons.Add("Usar um turno completo");
        editor.OnInspectorGUI();
        Check(manager.quantidadeRodadas == 15 && manager.ValidarConfiguracao(), "actual odd-count league accepts full turn");
        EditorGUILayout.popups["Formato"] = 1;
        editor.OnInspectorGUI();
        EditorGUILayout.popups.Clear();
        Check(manager.formato == FormatoDiscos.MataMata && manager.quantidadeTimes == 16 && manager.participantes.Count == 16, "actual format switch expands to legal bracket");
        Check(!manager.ValidarConfiguracao(), "actual validator rejects unfilled knockout without byes");
        manager.permitirFolgas = true;
        Check(manager.ValidarConfiguracao(), "actual validator accepts fifteen teams in sixteen-slot bracket with byes");
        manager.tema = new TemaCompeticao();
        typeof(ProjetoFutebolDiscosEditor).GetField("mostrarTema", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(editor, true);
        editor.OnInspectorGUI();
        Check(EditorGUILayout.properties.Contains("fonteTitulo") && EditorGUILayout.properties.Contains("fonteTexto") && EditorGUILayout.properties.Contains("logoCompeticao") && EditorGUILayout.properties.Contains("logoCanal"), "actual theme fields are usable");
        string oldFolder = manager.PastaDaTemporada;
        Directory.CreateDirectory(oldFolder);
        File.WriteAllText(Path.Combine(oldFolder, "saved.json"), "SAVED");
        manager.semente = 25;
        GUILayout.buttons.Add("Nova temporada (preserva as anteriores)");
        editor.OnInspectorGUI();
        Check(manager.temporadaId != "temporada_01" && manager.semente == 0, "actual fresh season creates new ID and automatic seed");
        Check(File.ReadAllText(Path.Combine(oldFolder, "saved.json")) == "SAVED", "actual prior saved season preserved");
        Check(!Directory.Exists(manager.PastaDaTemporada), "actual editor action never creates progress");
        Application.isPlaying = true;
        EditorApplication.isPlayingOrWillChangePlaymode = true;
        manager.quantidadeTimes = 0;
        manager.tamanhoDisco = 10;
        manager.tamanhoBola = 0;
        TeamData[] oldRoster = manager.participantes.ToArray();
        string oldSeason = manager.temporadaId;
        GUILayout.buttons.Add("8 times");
        GUILayout.buttons.Add("Nova temporada (preserva as anteriores)");
        editor.OnInspectorGUI();
        Check(manager.quantidadeTimes == 0 && manager.tamanhoDisco == 10 && manager.tamanhoBola == 0, "actual Play config is immutable even when invalid");
        Check(oldRoster.SequenceEqual(manager.participantes) && manager.temporadaId == oldSeason, "actual Play list and season immutable");
        Application.isPlaying = false;
        EditorApplication.isPlayingOrWillChangePlaymode = false;
        GUILayout.buttons.Clear();
        GameObject.All.Clear();
        Selection.activeGameObject = null;
        var knockoutGo = new GameObject("Inactive bracket");
        knockoutGo.SetActive(false);
        var knockout = knockoutGo.AddComponent<MataMataCampeonato>();
        var locate = typeof(MataMataEditor).GetMethod("LocalizarCampeonato", BindingFlags.Static | BindingFlags.NonPublic);
        Check(ReferenceEquals(locate.Invoke(null, null), knockout), "modern FindObjectsByType Include locates inactive tournament");
        var secondGo = new GameObject("Other bracket");
        secondGo.SetActive(false);
        secondGo.AddComponent<MataMataCampeonato>();
        Check(locate.Invoke(null, null) == null, "inactive multiple-controller ambiguity remains guarded");
        Selection.activeGameObject = knockoutGo;
        Check(ReferenceEquals(locate.Invoke(null, null), knockout), "selected controller resolves ambiguity");
        Check(EditorGUI.disabledCount == 0, "GUI scopes balanced");
        Console.WriteLine("PASS " + checks + " assertions using real central editor, MataMataEditor, central runtime, all tournament/storage/field/HUD/physics source; Unity APIs provided by facade.");
        Console.WriteLine("No Unity import, rendering or physics solver execution.");
    }
}
