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
        private SerializedObject dadosSons, dadosComemoracao, dadosTema;
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

                bool mataMata = formato.enumValueIndex == 1;
                Titulo(mataMata ? "Configurações do Mata-mata" : "Configurações da Liga");
                Titulo("Participantes");
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
                Campo("roteiroTransmissao", "Textos das aberturas e prévias", "Chamadas da identidade futurista. Ajustes visuais podem ser alterados entre sessões.");
                Campo("duracaoPartida", "Tempo real de jogo (s)", "Duração regulamentar real. O relógio distribui os 90 minutos virtuais neste tempo.");
                Campo("tempoAbertura", mataMata ? "Abertura e prévia dos confrontos (s)" : "Abertura do confronto (s)", "As apresentações usam um mínimo de três segundos.");
                Campo("tempoFotos", "Fotos dos capitães ampliadas (s)");
                Campo("tempoTransicaoFotos", "Retorno das fichas ao jogo (s)");
                Campo("tempoTabela", mataMata ? "Leitura do quadro final (s)" : "Leitura da classificação (s)");
                Campo("intervaloEntrePartidas", "Pausa após a partida (s)", "Aplicada uma única vez. Se houver comemorações pendentes, aguarda o fim delas.");
                EditorGUILayout.HelpBox("Os tempos desta seção são os únicos usados pelo modo escolhido. O jogo real é contado em segundos; as aberturas e prévias têm mínimo de três segundos.", MessageType.Info);

                Titulo("Identidade visual");
                Campo("tema", "Tema da competição");
                Campo("fundoCampo", "Imagem do campo (opcional)", "Vazio: campo original desenhado pelo sistema.");
                Campo("fundoExternoArena", "Fundo externo da arena", "Arraste uma imagem importada como Sprite. Ela aparece atrás do campo e dos gols.");
                Slider("opacidadeFundoExterno", "Opacidade do fundo externo", 0f, 1f);
                Campo("sobreporDecoracaoExterna", "Manter arquibancadas sobre a imagem", "Sem imagem, a decoração original continua visível.");
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

                Titulo("Identificação dos jogadores");
                EditorGUILayout.HelpBox("Os nomes vêm do Elenco de cada ficha de time. O capitão é o representante da foto. " +
                    "Cadastre os demais jogadores para nomear os outros discos; vagas sem jogador usam a sigla e o número do disco.", MessageType.Info);
                Campo("mostrarTagsJogadores", "Mostrar nome nos discos");
                Slider("tamanhoFonteTags", "Tamanho da fonte das tags", 2f, 8f);
                Slider("larguraTags", "Largura das tags", 1f, 4f);
                Campo("mostrarMarcaCapitao", "Destacar o capitão");
                Campo("usarFotoNoDiscoCapitao", "Foto no disco do capitão", "O capitão é o representante da foto na abertura e na ficha da partida.");
                Campo("mostrarScannerFotos", "Mostrar leitura luminosa atrás da foto");
                Slider("velocidadeScannerFotos", "Velocidade da leitura luminosa", 0.1f, 3f);

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

                Titulo("Bola presa nos cantos");
                Campo("reporBolaPresaNoCanto", "Repor a bola presa no canto", "Só interrompe quando a bola permanece no canto sem conseguir sair. Uma batida normal não gera cobrança.");
                Slider("tempoBolaPresaNoCanto", "Tempo sem sair do canto (s)", 1f, 10f);
                Slider("alcanceCantoBolaPresa", "Área de detecção do canto", .8f, 3f);
                Slider("pausaReposicaoBolaPresa", "Pausa antes da cobrança (s)", .2f, 3f);
                EditorGUILayout.HelpBox("A reposição só acontece quando a bola fica presa no canto. Último toque do time defensor: " +
                    "escanteio para o atacante. Último toque do atacante: tiro de meta para o defensor.", MessageType.Info);

                DesenharSonsEComemoracao(projeto);

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

        private void DesenharSonsEComemoracao(ProjetoFutebolDiscos projeto)
        {
            Titulo("Sons e comemoração de gol");
            UniversalUI interfacePartida = projeto.GetComponent<UniversalUI>();
            if (interfacePartida != null)
            {
                SerializedObject dados = PrepararDadosInternos(ref dadosSons, interfacePartida);
                CampoInterno(dados, "somGol", "Som do gol");
                CampoInterno(dados, "somApitoInicial", "Apito de início");
                CampoInterno(dados, "somApitoFinal", "Apito de encerramento");
                CampoInterno(dados, "volumeSons", "Volume dos efeitos");
                CampoInterno(dados, "fonteDeAudio", "Fonte de áudio dos efeitos (opcional)");
                AplicarDadosInternos(dados);
            }
            TemaCompeticao tema = P("tema").objectReferenceValue as TemaCompeticao;
            if (tema != null)
            {
                SerializedObject dados = PrepararDadosInternos(ref dadosTema, tema);
                CampoInterno(dados, "somAbertura", "Som das aberturas e prévias");
                AplicarDadosInternos(dados);
            }
            EditorGUILayout.HelpBox("Gol e apitos usam os três efeitos escolhidos acima. O som das aberturas e prévias " +
                "é separado e pertence ao tema, compartilhado pelas cenas que usam esse tema.", MessageType.None);
            GoalBannerUI banner = projeto.GetComponent<GoalBannerUI>();
            if (banner != null)
            {
                SerializedObject dados = PrepararDadosInternos(ref dadosComemoracao, banner);
                CampoInterno(dados, "tempoDeExibicao", "Exibição do autor do gol (s)");
                CampoInterno(dados, "intervaloEntreBanners", "Intervalo entre comemorações (s)");
                AplicarDadosInternos(dados);
            }
        }

        private static SerializedObject PrepararDadosInternos(ref SerializedObject dados, UnityEngine.Object alvo)
        {
            if (dados == null || dados.targetObject != alvo)
            {
                if (dados != null) AplicarDadosInternos(dados);
                dados = new SerializedObject(alvo);
            }
            // Object pickers may change a retained property between Inspector draws.
            // Apply that edit before Update refreshes the serialized snapshot.
            AplicarDadosInternos(dados);
            dados.Update();
            return dados;
        }

        private static void AplicarDadosInternos(SerializedObject dados)
        {
            if (dados == null || dados.targetObject == null || !dados.ApplyModifiedProperties()) return;
            UnityEngine.Object alvo = dados.targetObject;
            if (PrefabUtility.IsPartOfPrefabInstance(alvo))
                PrefabUtility.RecordPrefabInstancePropertyModifications(alvo);
            EditorUtility.SetDirty(alvo);
            Component componente = alvo as Component;
            if (componente != null && componente.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(componente.gameObject.scene);
        }

        private void OnDisable()
        {
            AplicarDadosInternos(dadosSons);
            AplicarDadosInternos(dadosComemoracao);
            AplicarDadosInternos(dadosTema);
            dadosSons = dadosComemoracao = dadosTema = null;
        }

        private static void CampoInterno(SerializedObject dados, string nome, string rotulo)
        {
            EditorGUILayout.PropertyField(dados.FindProperty(nome), new GUIContent(rotulo), true);
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
        private void DesenharTema(TemaCompeticao tema)
        {
            if (tema == null)
            {
                EditorGUILayout.HelpBox("Escolha um tema para alterar as fontes, os logos e os fundos da transmissão.", MessageType.Info);
                return;
            }
            EditorGUILayout.HelpBox("Estas alterações são feitas no asset do tema e valem para as cenas que usam o mesmo tema.", MessageType.None);
            SerializedObject dados = PrepararDadosInternos(ref dadosTema, tema);
            string[,] campos = {
                { "fundoAbertura", "Fundo das aberturas" }, { "fundoChaveamento", "Fundo do chaveamento" },
                { "logoCompeticao", "Troféu / logo da competição" }, { "logoCanal", "Logo do canal" },
                { "fonteTitulo", "Fonte dos títulos" }, { "fonteTexto", "Fonte dos textos" },
                { "escalaFonteTitulo", "Escala dos títulos" }, { "escalaFonteTexto", "Escala dos textos" },
                { "corPrimaria", "Cor principal" }, { "corSecundaria", "Cor secundária" },
                { "corDestaque", "Cor de destaque" }, { "corTexto", "Cor do texto" },
                { "corPainel", "Cor dos painéis" }
            };
            EditorGUI.indentLevel++;
            for (int i = 0; i < campos.GetLength(0); i++)
                EditorGUILayout.PropertyField(dados.FindProperty(campos[i, 0]), new GUIContent(campos[i, 1]));
            EditorGUI.indentLevel--;
            AplicarDadosInternos(dados);
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
