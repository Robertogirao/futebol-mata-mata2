#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FutebolDeDiscos
{
    [CustomEditor(typeof(ProjetoFutebolDiscos))]
    public sealed class ProjetoFutebolDiscosEditor : UnityEditor.Editor
    {
        private bool mostrarTema;
        private bool mostrarParticipantes = true;
        private static readonly int[] Vagas = { 2, 4, 8, 16, 32 };
        private static readonly string[] NomesVagas = { "2 times", "4 times", "8 times", "16 times", "32 times" };
        private static bool ConfiguracaoBloqueada { get { return EditorApplication.isPlayingOrWillChangePlaymode; } }

        public override void OnInspectorGUI()
        {
            var projeto = (ProjetoFutebolDiscos)target;
            serializedObject.Update();
            EditorGUILayout.HelpBox(
                "Configure a competição neste painel. As partidas são automáticas e os resultados são salvos " +
                "durante a temporada. Para um novo sorteio, crie uma nova temporada.", MessageType.Info);

            using (new EditorGUI.DisabledScope(ConfiguracaoBloqueada))
            {
                Titulo("Competição e temporada");
                Campo("nomeCampeonato", "Nome na transmissão");
                Campo("competicaoId", "Identificador da competição", "Letras, números, _ e -. Identifica a pasta dos resultados.");
                Campo("temporadaId", "Identificador da temporada", "Mantenha este identificador para retomar os resultados salvos.");

                SerializedProperty formato = P("formato");
                int novoFormato = EditorGUILayout.Popup("Formato", formato.enumValueIndex,
                    new[] { "Liga (pontos corridos)", "Mata-mata" });
                if (!ConfiguracaoBloqueada && novoFormato != formato.enumValueIndex)
                {
                    formato.enumValueIndex = novoFormato;
                    if (novoFormato == 1 && !Vagas.Contains(P("quantidadeTimes").intValue))
                        AlterarQuantidade(ProximasVagas(P("quantidadeTimes").intValue));
                }

                Titulo("Identidade visual");
                Campo("tema", "Tema da competição");
                Campo("fundoCampo", "Imagem do campo (opcional)", "Vazio: campo original desenhado pelo sistema.");
                mostrarTema = EditorGUILayout.Foldout(mostrarTema, "Fontes, imagens e cores da transmissão", true);
                if (mostrarTema) DesenharTema(P("tema").objectReferenceValue as TemaCompeticao);
                Campo("corGramado", "Cor do gramado");
                Campo("corLinhas", "Cor das linhas");

                Titulo("Textos da arena");
                Campo("mostrarTextoNoCampo", "Mostrar texto no campo");
                Campo("textoNoCampo", "Texto no campo", "Deixe vazio para ocultar a assinatura no gramado.");
                Campo("mostrarTituloRodape", "Mostrar título do rodapé");
                Campo("tituloRodape", "Título do rodapé", "Texto maior abaixo do campo. Vazio: oculto.");
                Campo("mostrarSubtituloRodape", "Mostrar subtítulo do rodapé");
                Campo("subtituloRodape", "Subtítulo do rodapé", "Substitui Automatic Disc Football. Vazio: oculto.");
                Campo("mostrarDetalhesFormato", "Mostrar formato e quantidade de discos", "Controla os detalhes Liga/Mata-mata e 3 v 3 separadamente do subtítulo.");

                Titulo("Participantes");
                bool mataMata = formato.enumValueIndex == 1;
                int quantidade = P("quantidadeTimes").intValue;
                int novaQuantidade;
                if (mataMata)
                {
                    int indice = Array.IndexOf(Vagas, quantidade);
                    novaQuantidade = Vagas[EditorGUILayout.Popup("Tamanho da chave", Math.Max(0, indice), NomesVagas)];
                }
                else
                    novaQuantidade = Mathf.Clamp(EditorGUILayout.IntField("Quantidade de times", quantidade), 2, 32);
                if (novaQuantidade != quantidade) AlterarQuantidade(novaQuantidade);
                using (new EditorGUILayout.HorizontalScope())
                {
                    foreach (int n in new[] { 8, 16, 32 })
                        if (GUILayout.Button(n + " times")) AlterarQuantidade(n);
                }
                if (P("participantes").arraySize != P("quantidadeTimes").intValue)
                {
                    EditorGUILayout.HelpBox("A lista deve ter o mesmo número de campos da competição.", MessageType.Warning);
                    if (GUILayout.Button("Ajustar os campos da lista")) AlterarQuantidade(P("quantidadeTimes").intValue);
                }
                if (GUILayout.Button("Sortear times do catálogo")) SortearParticipantes();
                mostrarParticipantes = EditorGUILayout.Foldout(mostrarParticipantes, "Times selecionados", true);
                if (mostrarParticipantes)
                {
                    EditorGUI.indentLevel++;
                    SerializedProperty lista = P("participantes");
                    for (int i = 0; i < lista.arraySize; i++)
                        EditorGUILayout.PropertyField(lista.GetArrayElementAtIndex(i), new GUIContent("Time " + (i + 1)));
                    EditorGUI.indentLevel--;
                }

                Titulo(mataMata ? "Chaveamento" : "Calendário");
                if (mataMata)
                {
                    Campo("permitirFolgas", "Permitir folgas", "Permite vagas vazias na lista. São necessários pelo menos dois times.");
                    SerializedProperty distribuicao = P("distribuicao");
                    int novaDistribuicao = EditorGUILayout.Popup("Distribuição", distribuicao.enumValueIndex,
                        new[] { "1 x 2, 3 x 4...", "Cabeças de chave" });
                    if (!ConfiguracaoBloqueada) distribuicao.enumValueIndex = novaDistribuicao;
                }
                else
                {
                    Campo("quantidadeRodadas", "Quantidade de rodadas");
                    int n = P("quantidadeTimes").intValue;
                    int turno = n % 2 == 0 ? n - 1 : n;
                    EditorGUILayout.HelpBox("Um turno completo tem " + turno + " rodadas. Rodadas extras continuam em novos turnos.", MessageType.None);
                    if (GUILayout.Button("Usar um turno completo")) P("quantidadeRodadas").intValue = turno;
                }
                Campo("sortearConfrontos", "Embaralhar os confrontos");
                Campo("semente", "Semente do sorteio", "0: sorteio novo ao criar uma temporada. Um número positivo repete o sorteio. Retomar a mesma temporada conserva o sorteio salvo.");
                EditorGUILayout.HelpBox("Play e Stop retomam a mesma temporada. Use Nova temporada para sortear outra sem apagar os resultados anteriores.", MessageType.Info);
                Campo("umaRodadaOuFasePorPlay", mataMata ? "Uma fase por Play" : "Uma rodada por Play");

                Titulo("Tempos da transmissão (segundos)");
                Campo("duracaoPartida", "Duração da partida");
                Campo("tempoAbertura", "Abertura dos confrontos");
                Campo("tempoFotos", "Fotos ampliadas no centro");
                Campo("tempoTransicaoFotos", "Retorno das fotos aos cantos");
                Campo("tempoTabela", mataMata ? "Leitura do chaveamento" : "Leitura da classificação");
                Campo("intervaloEntrePartidas", "Intervalo entre partidas");

                Titulo("Identificação dos jogadores");
                EditorGUILayout.HelpBox("Os nomes vêm do Elenco de cada ficha de time. O capitão é o representante da foto. " +
                    "Cadastre os demais jogadores para nomear os outros discos; vagas sem jogador usam a sigla e o número do disco.", MessageType.Info);
                Campo("mostrarTagsJogadores", "Mostrar nome nos discos");
                Slider("tamanhoFonteTags", "Tamanho da fonte das tags", 2f, 8f);
                Slider("larguraTags", "Largura das tags", 1f, 4f);
                Campo("mostrarMarcaCapitao", "Destacar o capitão");
                Campo("usarFotoNoDiscoCapitao", "Foto no disco do capitão", "O capitão é o representante da foto na abertura e na ficha da partida.");

                Titulo("Discos e bola");
                IntSlider("discosPorTime", "Discos por time", 1, 5);
                Slider("tamanhoDisco", "Diâmetro dos discos", 0.6f, 1.4f);
                Slider("tamanhoBola", "Diâmetro da bola", 0.25f, 0.65f);
                Slider("velocidadeDiscos", "Velocidade dos discos", 1f, 8f);
                Slider("velocidadeMaximaBola", "Velocidade máxima da bola", 4f, 25f);
                Slider("forcaChute", "Força do chute", 1f, 15f);
                Slider("pausaAposGol", "Pausa depois do gol", 0.2f, 5f);
                Campo("limitarDiscosNaMetade", "Manter cada time em sua metade");
                EditorGUILayout.HelpBox("Para liberar pressão no campo adversário, desmarque a opção acima e escolha a postura " +
                    "na ficha de cada time: ultra defensivo (0), defensivo (até 1), balanceado (até 2) ou ataque (até 3). " +
                    "A subida depende da posição e da direção da bola.", MessageType.Info);
                Campo("usarForcaDosTimes", "Considerar o overall dos times");
                Slider("limiarCores", "Diferença mínima entre as cores", 0f, 100f);

                serializedObject.ApplyModifiedProperties();
                Titulo("Preparar a gravação");
                if (GUILayout.Button("Validar configuração"))
                {
                    bool valida = projeto.ValidarConfiguracao();
                    EditorUtility.DisplayDialog("Futebol de Discos", valida ? "Configuração válida." : projeto.ultimoErro, "OK");
                }
                if (GUILayout.Button("Nova temporada (preserva as anteriores)"))
                {
                    Undo.RecordObject(projeto, "Nova temporada de Futebol de Discos");
                    projeto.temporadaId = CriarIdentificadorTemporada();
                    projeto.semente = 0;
                    EditorUtility.SetDirty(projeto);
                    if (projeto.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(projeto.gameObject.scene);
                    serializedObject.Update();
                }
            }

            serializedObject.ApplyModifiedProperties();
            if (GUILayout.Button("Abrir pasta desta temporada")) AbrirPasta(projeto);
            if (Application.isPlaying)
            {
                if (!string.IsNullOrWhiteSpace(projeto.situacao))
                    EditorGUILayout.HelpBox(projeto.situacao, MessageType.Info);
                using (new EditorGUI.DisabledScope(!projeto.Preparado || projeto.CompeticaoConcluida))
                    if (GUILayout.Button("Continuar no Play")) projeto.ContinuarNoPlay();
                EditorGUILayout.HelpBox("Pare o Play para alterar a configuração.", MessageType.None);
            }
            if (!string.IsNullOrWhiteSpace(projeto.ultimoErro))
                EditorGUILayout.HelpBox(projeto.ultimoErro, MessageType.Error);
        }

        private SerializedProperty P(string nome) { return serializedObject.FindProperty(nome); }
        private void Campo(string nome, string rotulo, string dica = null)
        {
            EditorGUILayout.PropertyField(P(nome), new GUIContent(rotulo, dica), true);
        }
        private static void Titulo(string titulo)
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField(titulo, EditorStyles.boldLabel);
        }
        private void Slider(string nome, string rotulo, float minimo, float maximo)
        {
            SerializedProperty p = P(nome);
            float valor = EditorGUILayout.Slider(new GUIContent(rotulo), p.floatValue, minimo, maximo);
            if (!ConfiguracaoBloqueada) p.floatValue = valor;
        }
        private void IntSlider(string nome, string rotulo, int minimo, int maximo)
        {
            SerializedProperty p = P(nome);
            int valor = EditorGUILayout.IntSlider(new GUIContent(rotulo), p.intValue, minimo, maximo);
            if (!ConfiguracaoBloqueada) p.intValue = valor;
        }
        private static int ProximasVagas(int n)
        {
            foreach (int vagas in Vagas) if (vagas >= n) return vagas;
            return 32;
        }
        private void AlterarQuantidade(int quantidade)
        {
            if (ConfiguracaoBloqueada) return;
            SerializedProperty lista = P("participantes");
            int anterior = lista.arraySize;
            lista.arraySize = quantidade;
            for (int i = anterior; i < quantidade; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = null;
            P("quantidadeTimes").intValue = quantidade;
        }
        private void SortearParticipantes()
        {
            if (ConfiguracaoBloqueada) return;
            List<TeamData> catalogo = AssetDatabase.FindAssets("t:TeamData")
                .Select(guid => AssetDatabase.LoadAssetAtPath<TeamData>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(t => t != null && t.elenco != null && t.elenco.Any(j => j != null && !string.IsNullOrWhiteSpace(j.nomeJogador)))
                .GroupBy(t => t.name, StringComparer.Ordinal).Select(g => g.First()).ToList();
            int quantidade = P("quantidadeTimes").intValue;
            if (catalogo.Count < quantidade)
            {
                EditorUtility.DisplayDialog("Catálogo insuficiente", "Há somente " + catalogo.Count + " fichas válidas. Reduza a quantidade ou cadastre mais times.", "OK");
                return;
            }
            var aleatorio = new System.Random(Guid.NewGuid().GetHashCode());
            for (int i = catalogo.Count - 1; i > 0; i--)
            {
                int j = aleatorio.Next(i + 1);
                TeamData tmp = catalogo[i]; catalogo[i] = catalogo[j]; catalogo[j] = tmp;
            }
            AlterarQuantidade(quantidade);
            for (int i = 0; i < quantidade; i++) P("participantes").GetArrayElementAtIndex(i).objectReferenceValue = catalogo[i];
        }
        internal static string CriarIdentificadorTemporada()
        {
            return "temporada_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }
        private static void AbrirPasta(ProjetoFutebolDiscos projeto)
        {
            try
            {
                string pasta = projeto.PastaDaTemporada;
                if (Directory.Exists(pasta)) EditorUtility.RevealInFinder(pasta);
                else EditorUtility.DisplayDialog("Temporada", "A pasta será criada no primeiro Play:\n" + pasta, "OK");
            }
            catch (Exception erro) { EditorUtility.DisplayDialog("Temporada", erro.Message, "OK"); }
        }
        private static void DesenharTema(TemaCompeticao tema)
        {
            if (tema == null)
            {
                EditorGUILayout.HelpBox("Escolha um tema para alterar as fontes, os logos e os fundos da transmissão.", MessageType.Info);
                return;
            }
            EditorGUILayout.HelpBox("Estas alterações são feitas no asset do tema e valem para as cenas que usam o mesmo tema.", MessageType.None);
            var dados = new SerializedObject(tema);
            dados.Update();
            string[,] campos = {
                { "fundoAbertura", "Fundo das aberturas" }, { "fundoChaveamento", "Fundo do chaveamento" },
                { "logoCompeticao", "Troféu / logo da competição" }, { "logoCanal", "Logo do canal" },
                { "fonteTitulo", "Fonte dos títulos" }, { "fonteTexto", "Fonte dos textos" },
                { "escalaFonteTitulo", "Escala dos títulos" }, { "escalaFonteTexto", "Escala dos textos" },
                { "corPrimaria", "Cor principal" }, { "corSecundaria", "Cor secundária" },
                { "corDestaque", "Cor de destaque" }, { "corTexto", "Cor do texto" },
                { "corPainel", "Cor dos painéis" }, { "somAbertura", "Som de abertura" }
            };
            EditorGUI.indentLevel++;
            for (int i = 0; i < campos.GetLength(0); i++)
                EditorGUILayout.PropertyField(dados.FindProperty(campos[i, 0]), new GUIContent(campos[i, 1]));
            EditorGUI.indentLevel--;
            dados.ApplyModifiedProperties();
        }

        [MenuItem("Tools/Futebol de Discos/Abrir Liga")]
        private static void AbrirLiga() { AbrirCena("Assets/Scenes/FutebolDiscos/Liga_Discos.unity"); }
        [MenuItem("Tools/Futebol de Discos/Abrir Mata-mata")]
        private static void AbrirMataMata() { AbrirCena("Assets/Scenes/FutebolDiscos/MataMata_Discos.unity"); }
        private static void AbrirCena(string caminho)
        {
            if (ConfiguracaoBloqueada || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var cena = EditorSceneManager.OpenScene(caminho);
            foreach (var raiz in cena.GetRootGameObjects())
                if (raiz.GetComponent<ProjetoFutebolDiscos>() != null) { Selection.activeGameObject = raiz; break; }
        }
    }
}
#endif
