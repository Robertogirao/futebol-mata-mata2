#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PokemonMarbleRace.EditorTools
{
    /// <summary>Centraliza a configuração da corrida e preserva o histórico das temporadas.</summary>
    [CustomEditor(typeof(MarbleRaceManager))]
    public sealed class MarbleRaceManagerEditor : UnityEditor.Editor
    {
        public const string CaminhoCena = "Assets/Scenes/MarbleRace/MarbleRace_Pokemon.unity";
        private bool mostrarParticipantes = true;
        private static readonly string[] Geracoes =
        {
            "Geração 1", "Geração 2", "Geração 3", "Geração 4", "Geração 5",
            "Geração 6", "Geração 7", "Geração 8", "Geração 9"
        };
        private static readonly string[] Tipos =
        {
            "Normal", "Fogo", "Água", "Elétrico", "Planta", "Gelo", "Lutador", "Venenoso",
            "Terra", "Voador", "Psíquico", "Inseto", "Pedra", "Fantasma", "Dragão", "Sombrio", "Aço", "Fada"
        };

        public override void OnInspectorGUI()
        {
            var corrida = (MarbleRaceManager)target;
            bool emPlay = EditorApplication.isPlayingOrWillChangePlaymode;
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(emPlay))
            {
                EditorGUILayout.LabelField("Campeonato e apresentação", EditorStyles.boldLabel);
                Campo("nomeCampeonato", "Nome do campeonato");
                Campo("campeonatoId", "Identificador do campeonato");
                Campo("temporadaId", "Temporada");
                Campo("tema", "Tema e fontes");
                Campo("fundo", "Fundo da pista (opcional)");
                EditorGUILayout.HelpBox("As fontes do título e dos textos ficam no asset Tema. " +
                    "Vazio no fundo mantém o cenário procedural da corrida.", MessageType.Info);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Formato", EditorStyles.boldLabel);
                Campo("formato", "Formato da competição");
                var formato = (RaceFormat)serializedObject.FindProperty("formato").intValue;
                bool miniGp = formato == RaceFormat.MiniGP;
                using (new EditorGUI.DisabledScope(!miniGp))
                {
                    SerializedProperty provas = serializedObject.FindProperty("quantidadeCorridas");
                    int valor = Math.Max(1, Math.Min(30,
                        EditorGUILayout.IntField("Corridas do Mini GP", provas.intValue)));
                    if (miniGp && !emPlay) provas.intValue = valor;
                }
                EditorGUILayout.HelpBox(DescricaoFormato(formato), MessageType.Info);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Participantes e filtros", EditorStyles.boldLabel);
                Filtro("geracoesPermitidas", "Gerações permitidas", Geracoes);
                Filtro("tiposPermitidos", "Tipos permitidos", Tipos);
                EditorGUILayout.HelpBox("Nenhuma seleção permite todos. O sorteio aceita qualquer geração marcada " +
                    "e qualquer tipo marcado, inclusive o tipo secundário. Os dois grupos são combinados. " +
                    "Os filtros são usados no sorteio do catálogo.", MessageType.Info);

                SerializedProperty quantidade = serializedObject.FindProperty("quantidadeParticipantes");
                int novaQuantidade = Math.Max(2, Math.Min(32,
                    EditorGUILayout.IntField("Quantidade de Pokémon", quantidade.intValue)));
                if (!emPlay) quantidade.intValue = novaQuantidade;
                EditorGUILayout.BeginHorizontal();
                if (formato == RaceFormat.BestOfThree)
                {
                    if (GUILayout.Button("2 Pokémon")) quantidade.intValue = 2;
                }
                else
                {
                    if (GUILayout.Button("8 Pokémon")) quantidade.intValue = 8;
                    if (GUILayout.Button("16 Pokémon")) quantidade.intValue = 16;
                    if (formato == RaceFormat.Knockout && GUILayout.Button("32 Pokémon")) quantidade.intValue = 32;
                }
                EditorGUILayout.EndHorizontal();

                if (!QuantidadePermitida(formato, quantidade.intValue))
                {
                    EditorGUILayout.HelpBox(formato == RaceFormat.BestOfThree
                        ? "Melhor de três precisa de exatamente dois Pokémon. A lista foi preservada; ajuste a quantidade."
                        : "Este formato permite de 2 a 16 Pokémon. A lista foi preservada; ajuste a quantidade.",
                        MessageType.Warning);
                }

                SerializedProperty lista = serializedObject.FindProperty("participantes");
                if (!emPlay) AjustarVagas(lista, quantidade.intValue);
                mostrarParticipantes = EditorGUILayout.Foldout(mostrarParticipantes,
                    "Pokémon da competição (" + lista.arraySize + ")", true);
                if (mostrarParticipantes)
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < lista.arraySize; i++)
                        EditorGUILayout.PropertyField(lista.GetArrayElementAtIndex(i),
                            new GUIContent("Pokémon " + (i + 1)));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Pista, ritmo e continuidade", EditorStyles.boldLabel);
                Campo("semente", "Semente das pistas");
                Campo("duracaoAlvo", "Duração alvo da corrida");
                Campo("duracaoMaxima", "Duração máxima da corrida");
                Campo("duracaoAbertura", "Duração da apresentação");
                Campo("duracaoResultado", "Duração do resultado");
                Campo("intervaloEntreCorridas", "Intervalo entre corridas");
                Campo("umaCorridaPorPlay", "Uma corrida por Play");
                Campo("poderesPorTipo", "Ativar poderes por tipo");
                SerializedProperty tamanho = serializedObject.FindProperty("tamanhoMarble");
                float novoTamanho = EditorGUILayout.Slider("Diâmetro das marbles", tamanho.floatValue, 0.3f, 0.65f);
                if (!emPlay) tamanho.floatValue = novoTamanho;
                EditorGUILayout.HelpBox("Diâmetro de 0,30 a 0,65 unidade, para manter espaço na largada " +
                    "e a leitura da pista com até 16 marbles.", MessageType.Info);
                EditorGUILayout.HelpBox("Semente zero sorteia uma vez ao criar a temporada; as pistas e os resultados " +
                    "ficam salvos para continuar a mesma competição. Para trocar lista, formato ou calendário " +
                    "depois de iniciar, use Nova temporada.", MessageType.Info);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Arte das marbles e eventos", EditorStyles.boldLabel);
                Campo("spriteBola", "Base da marble");
                Campo("spriteSnorlax", "Snorlax da pista");
                Campo("spriteRocket", "Equipe Rocket");
            }

            serializedObject.ApplyModifiedProperties();

            using (new EditorGUI.DisabledScope(emPlay))
            {
                if (GUILayout.Button("Sortear Pokémon do catálogo com os filtros")) SortearParticipantes(corrida);
                if (GUILayout.Button("Validar competição"))
                {
                    if (corrida.ValidarConfiguracao()) Debug.Log("Marble Race: configuração válida.", corrida);
                    else Debug.LogWarning("Marble Race: " + corrida.ultimoErro, corrida);
                }
                if (GUILayout.Button("Nova temporada (preserva as anteriores)")) NovaTemporada(corrida);
            }
            if (GUILayout.Button("Abrir pasta da temporada")) AbrirPasta(corrida);
            using (new EditorGUI.DisabledScope(!Application.isPlaying ||
                !corrida.Preparada || corrida.CampeonatoConcluido))
            {
                if (GUILayout.Button("Continuar próxima corrida no Play")) corrida.ContinuarNoPlay();
            }

            if (!string.IsNullOrWhiteSpace(corrida.situacao))
                EditorGUILayout.HelpBox(corrida.situacao, MessageType.Info);
            if (!string.IsNullOrWhiteSpace(corrida.ultimoErro))
                EditorGUILayout.HelpBox(corrida.ultimoErro, MessageType.Warning);
        }

        private void Campo(string nome, string rotulo)
        {
            SerializedProperty propriedade = serializedObject.FindProperty(nome);
            if (propriedade != null)
                EditorGUILayout.PropertyField(propriedade, new GUIContent(rotulo), true);
        }

        private void Filtro(string nome, string rotulo, string[] opcoes)
        {
            SerializedProperty lista = serializedObject.FindProperty(nome);
            int mascara = 0;
            for (int i = 0; i < lista.arraySize; i++)
            {
                int valor = lista.GetArrayElementAtIndex(i).intValue;
                if (valor >= 1 && valor <= opcoes.Length) mascara |= 1 << (valor - 1);
            }
            int nova = EditorGUILayout.MaskField(rotulo, mascara, opcoes) & ((1 << opcoes.Length) - 1);
            if (nova == mascara) return;
            var valores = new List<int>();
            for (int i = 0; i < opcoes.Length; i++) if ((nova & (1 << i)) != 0) valores.Add(i + 1);
            lista.arraySize = valores.Count;
            for (int i = 0; i < valores.Count; i++) lista.GetArrayElementAtIndex(i).intValue = valores[i];
        }

        private static string DescricaoFormato(RaceFormat formato)
        {
            switch (formato)
            {
                case RaceFormat.SingleRace: return "Corrida única: de 2 a 16 Pokémon disputam uma pista.";
                case RaceFormat.BestOfThree: return "Duelo: dois Pokémon disputam até três provas; duas vitórias decidem a série.";
                case RaceFormat.Knockout: return "Eliminatórias: de 2 a 32 Pokémon, baterias de até oito e avanço da metade superior até a final.";
                default: return "Mini GP: de 2 a 16 Pokémon somam pontos por posição nas corridas escolhidas.";
            }
        }

        private static bool QuantidadePermitida(RaceFormat formato, int quantidade)
        {
            return formato == RaceFormat.BestOfThree ? quantidade == 2
                : quantidade >= 2 && quantidade <= (formato == RaceFormat.Knockout ? 32 : 16);
        }

        private static void AjustarVagas(SerializedProperty lista, int quantidade)
        {
            int anterior = lista.arraySize;
            if (anterior == quantidade) return;
            lista.arraySize = quantidade;
            for (int i = anterior; i < quantidade; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = null;
        }

        private static void SortearParticipantes(MarbleRaceManager corrida)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            try
            {
                int quantidade = corrida.quantidadeParticipantes;
                if (!QuantidadePermitida(corrida.formato, quantidade))
                    throw new InvalidOperationException("Ajuste a quantidade de Pokémon ao formato antes de sortear.");
                var catalogo = new List<PokemonDados>();
                var ids = new HashSet<string>(StringComparer.Ordinal);
                string[] guids = AssetDatabase.FindAssets("t:PokemonDados");
                Array.Sort(guids, StringComparer.Ordinal);
                foreach (string guid in guids)
                {
                    PokemonDados pokemon = AssetDatabase.LoadAssetAtPath<PokemonDados>(AssetDatabase.GUIDToAssetPath(guid));
                    if (pokemon == null) continue;
                    bool geracao = corrida.geracoesPermitidas == null || corrida.geracoesPermitidas.Count == 0 ||
                        corrida.geracoesPermitidas.Contains(pokemon.Geracao);
                    bool tipo = corrida.tiposPermitidos == null || corrida.tiposPermitidos.Count == 0 ||
                        corrida.tiposPermitidos.Exists(pokemon.PertenceAoTipo);
                    if (!geracao || !tipo) continue;
                    pokemon.ExigirValido();
                    if (ids.Add(pokemon.IdEstavel)) catalogo.Add(pokemon);
                }
                if (catalogo.Count < quantidade)
                    throw new InvalidOperationException("Os filtros encontraram " + catalogo.Count +
                        " Pokémon válidos distintos, mas a competição pede " + quantidade + ".");

                var sorteio = new System.Random(Guid.NewGuid().GetHashCode());
                for (int i = catalogo.Count - 1; i > 0; i--)
                {
                    int j = sorteio.Next(i + 1);
                    PokemonDados atual = catalogo[i]; catalogo[i] = catalogo[j]; catalogo[j] = atual;
                }
                Undo.RecordObject(corrida, "Sortear Pokémon do Marble Race");
                corrida.participantes = catalogo.GetRange(0, quantidade);
                corrida.ultimoErro = "";
                MarcarAlteracao(corrida);
                Debug.Log("Pokémon sorteados. Salve a cena. Para mudar participantes de uma temporada iniciada, use Nova temporada.", corrida);
            }
            catch (Exception erro)
            {
                corrida.ultimoErro = erro.Message;
                Debug.LogWarning("Marble Race: " + erro.Message, corrida);
            }
        }

        private static void NovaTemporada(MarbleRaceManager corrida)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Undo.RecordObject(corrida, "Criar temporada do Marble Race");
            corrida.temporadaId = "temporada_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) +
                "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            corrida.ultimoErro = "";
            MarcarAlteracao(corrida);
            Debug.Log("Nova temporada configurada; os históricos anteriores foram preservados. Salve a cena antes do Play.", corrida);
        }

        private static void AbrirPasta(MarbleRaceManager corrida)
        {
            try
            {
                string pasta = corrida.PastaDaTemporada;
                Directory.CreateDirectory(pasta);
                EditorUtility.RevealInFinder(pasta);
            }
            catch (Exception erro) { Debug.LogWarning("Marble Race: " + erro.Message, corrida); }
        }

        private static void MarcarAlteracao(MarbleRaceManager corrida)
        {
            EditorUtility.SetDirty(corrida);
            if (corrida.gameObject.scene.IsValid() && !EditorUtility.IsPersistent(corrida))
                EditorSceneManager.MarkSceneDirty(corrida.gameObject.scene);
        }

        [MenuItem("Tools/Pokémon/Abrir Marble Race")]
        public static void AbrirCena()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Saia do Play antes de abrir a cena do Marble Race.");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CaminhoCena) == null)
            {
                Debug.LogWarning("A cena não foi encontrada em " + CaminhoCena + ".");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(CaminhoCena, OpenSceneMode.Single);
            MarbleRaceManager corrida = UnityEngine.Object.FindAnyObjectByType<MarbleRaceManager>();
            if (corrida != null) Selection.activeGameObject = corrida.gameObject;
        }
    }
}
#endif
