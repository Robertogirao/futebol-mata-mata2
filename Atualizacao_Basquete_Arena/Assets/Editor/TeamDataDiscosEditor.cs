#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FutebolDeDiscos;

/// <summary>Expõe a seleção já existente do capitão e a formação das cenas normais.</summary>
[CustomEditor(typeof(TeamData))]
public sealed class TeamDataDiscosEditor : Editor
{
    private static readonly string[] Modos = { "Primeiro do elenco", "Escolher jogador", "Sortear por partida" };
    private int discosNaPrevia = 5;
    private bool mostrarPrevia = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            EditorGUILayout.HelpBox("Saia do Play para alterar capitão e posições. A prévia abaixo é apenas ilustrativa; " +
                "não modifica a formação da partida em andamento.", MessageType.None);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            DrawPropertiesExcluding(serializedObject, "m_Script", "sortearTitularesAleatorios", "usarSelecaoManual", "indicesTitulares");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Capitão das cenas normais", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("O representante escolhido abaixo é o capitão: recebe a foto da ficha e ocupa o disco C. " +
                "Os demais jogadores entram na ordem do Elenco. Esta escolha não altera os capitães convidados da pasta Capitaes.", MessageType.Info);

            SerializedProperty sorteio = serializedObject.FindProperty("sortearTitularesAleatorios");
            SerializedProperty manual = serializedObject.FindProperty("usarSelecaoManual");
            SerializedProperty indices = serializedObject.FindProperty("indicesTitulares");
            int modo = sorteio.boolValue ? 2 : manual.boolValue ? 1 : 0;
            EditorGUI.BeginChangeCheck();
            int novoModo = EditorGUILayout.Popup("Escolha do capitão (cenas normais)", modo, Modos);
            if (EditorGUI.EndChangeCheck())
            {
                sorteio.boolValue = novoModo == 2;
                manual.boolValue = novoModo == 1;
                modo = novoModo;
            }
            if (modo == 1) DesenharEscolhaManual(indices);
            else if (modo == 2)
                EditorGUILayout.HelpBox("Um capitão é sorteado entre os jogadores com nome antes de cada partida. " +
                    "A prévia usa o primeiro jogador válido apenas como exemplo e não realiza sorteios.", MessageType.None);
            else
                EditorGUILayout.HelpBox("O primeiro jogador com nome no Elenco será o capitão. Reordene a lista ou escolha um jogador pelo nome.", MessageType.None);

            EditorGUILayout.HelpBox("Em cada jogador do Elenco, escolha a posição nas cenas normais: Automática, Defesa, Meio ou Ataque. " +
                "A posição individual do capitão tem prioridade; a Posição do capitão do time é usada quando ele está em Automática. " +
                "As funções definem a base da formação, não prendem os discos: a IA continua reagindo à bola e às regras do time.", MessageType.Info);
        }
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        mostrarPrevia = EditorGUILayout.Foldout(mostrarPrevia, "Prévia da formação nas cenas normais", true);
        if (mostrarPrevia) DesenharPrevia((TeamData)target);
    }

    private void DesenharEscolhaManual(SerializedProperty indices)
    {
        SerializedProperty elenco = serializedObject.FindProperty("elenco");
        var indicesValidos = new List<int>();
        var nomes = new List<string> { "Selecione um jogador" };
        for (int i = 0; i < elenco.arraySize; i++)
        {
            SerializedProperty jogador = elenco.GetArrayElementAtIndex(i);
            SerializedProperty nome = jogador.FindPropertyRelative("nomeJogador");
            if (nome == null || string.IsNullOrWhiteSpace(nome.stringValue)) continue;
            indicesValidos.Add(i);
            nomes.Add(nome.stringValue.Trim() + "  (índice " + i + ")");
        }
        if (indicesValidos.Count == 0)
        {
            EditorGUILayout.HelpBox("Cadastre pelo menos um jogador com nome no Elenco para escolher o capitão destas cenas.", MessageType.Warning);
            return;
        }
        int indiceSalvo = indices.arraySize > 0 ? indices.GetArrayElementAtIndex(0).intValue : -1;
        int escolha = indicesValidos.IndexOf(indiceSalvo) + 1;
        EditorGUI.BeginChangeCheck();
        int novaEscolha = EditorGUILayout.Popup("Jogador capitão", escolha, nomes.ToArray());
        if (EditorGUI.EndChangeCheck() && novaEscolha > 0)
        {
            if (indices.arraySize == 0) indices.arraySize = 1;
            indices.GetArrayElementAtIndex(0).intValue = indicesValidos[novaEscolha - 1];
            escolha = novaEscolha;
        }
        if (escolha == 0)
            EditorGUILayout.HelpBox("O índice salvo não aponta para um jogador com nome. A prévia e o jogo usam o primeiro jogador válido " +
                "até você escolher outro. Abrir este Inspector não altera a configuração salva.", MessageType.Warning);
        EditorGUILayout.HelpBox("A escolha manual usa o índice original do Elenco. Se reordenar ou remover jogadores, " +
            "reconfira o campo Jogador capitão. Não é necessário reordenar a lista para escolher as posições.", MessageType.None);
    }

    private void DesenharPrevia(TeamData equipe)
    {
        EditorGUILayout.HelpBox("Quantidade da prévia é apenas uma simulação deste Inspector. Para colocar cinco em campo, ajuste " +
            "Discos por time = 5 no painel Projeto Futebol Discos da cena normal. Cadastrar cinco nomes não muda a quantidade da cena.", MessageType.None);
        discosNaPrevia = EditorGUILayout.IntSlider("Discos na prévia (não altera a cena)", discosNaPrevia, 1, 5);
        JogadorDados capitao = CapitaoDaPrevia(equipe);
        if (capitao == null)
        {
            EditorGUILayout.HelpBox("Preencha um nome no Elenco para visualizar a formação das cenas normais.", MessageType.None);
            return;
        }
        List<JogadorDados> escalacao = equipe.MontarEscalacaoDiscos(capitao, discosNaPrevia);
        Vector2[] bases = FormacaoElencoDiscos.Calcular(equipe, escalacao, true);
        if (bases == null || bases.Length != escalacao.Count) return;

        Rect area = GUILayoutUtility.GetRect(180f, 240f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(area, new Color(.035f, .12f, .13f, 1f));
        Rect campo = new Rect(area.x + 13f, area.y + 26f, Mathf.Max(1f, area.width - 26f), area.height - 48f);
        Color linhas = new Color(.34f, .76f, .71f, 1f);
        EditorGUI.DrawRect(new Rect(campo.x, campo.y, campo.width, 1f), linhas);
        EditorGUI.DrawRect(new Rect(campo.x, campo.yMax - 1f, campo.width, 1f), linhas);
        EditorGUI.DrawRect(new Rect(campo.x, campo.y, 1f, campo.height), linhas);
        EditorGUI.DrawRect(new Rect(campo.xMax - 1f, campo.y, 1f, campo.height), linhas);
        var legenda = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
        legenda.normal.textColor = new Color(.74f, .87f, .86f, 1f);
        GUI.Label(new Rect(campo.x, area.y + 4f, campo.width * .5f, 18f), "OWN GOAL", legenda);
        GUI.Label(new Rect(campo.center.x, area.y + 4f, campo.width * .5f, 18f), "HALFWAY", legenda);
        var numero = new GUIStyle(EditorStyles.boldLabel);
        numero.alignment = TextAnchor.MiddleCenter;
        numero.normal.textColor = Color.black;
        Color corAnterior = Handles.color;
        Handles.BeginGUI();
        for (int i = 0; i < bases.Length; i++)
        {
            float x = campo.x + Mathf.Clamp01((bases[i].x + 11f) / 11f) * campo.width;
            float y = campo.center.y - bases[i].y / 10.4f * campo.height;
            Handles.color = i == 0 ? new Color(1f, .78f, .20f, 1f) : new Color(.31f, .86f, .75f, 1f);
            Handles.DrawSolidDisc(new Vector3(x, y, 0f), Vector3.forward, 10f);
            GUI.Label(new Rect(x - 10f, y - 10f, 20f, 20f), i == 0 ? "C" : (i + 1).ToString(), numero);
        }
        Handles.color = corAnterior;
        Handles.EndGUI();
        GUI.Label(new Rect(campo.x, area.yMax - 20f, campo.width, 18f), "Formation base · movement remains automatic", legenda);

        for (int i = 0; i < escalacao.Count; i++)
        {
            JogadorDados jogador = escalacao[i];
            string nome = jogador != null && !string.IsNullOrWhiteSpace(jogador.nomeJogador)
                ? jogador.nomeJogador.Trim() : (string.IsNullOrWhiteSpace(equipe.sigla) ? equipe.nomeEquipe : equipe.sigla) + " " + (i + 1);
            string funcao = jogador == null ? "AUTO" : Funcao(jogador.posicaoDiscos);
            if (i == 0 && funcao == "AUTO")
                funcao += " (" + FuncaoDoCapitao(equipe.posicaoCapitaoDiscos) + ")";
            EditorGUILayout.LabelField(i == 0 ? "C — " + nome : (i + 1) + " — " + nome, funcao);
        }
        if (equipe.sortearTitularesAleatorios)
            EditorGUILayout.HelpBox("Exemplo com o primeiro jogador válido como C. O capitão real será sorteado uma vez antes de cada partida.", MessageType.None);
    }

    private static JogadorDados CapitaoDaPrevia(TeamData equipe)
    {
        if (equipe == null || equipe.elenco == null) return null;
        if (!equipe.sortearTitularesAleatorios && equipe.usarSelecaoManual && equipe.indicesTitulares != null && equipe.indicesTitulares.Length > 0)
        {
            int indice = equipe.indicesTitulares[0];
            if (indice >= 0 && indice < equipe.elenco.Count && Valido(equipe.elenco[indice])) return equipe.elenco[indice];
        }
        foreach (JogadorDados jogador in equipe.elenco) if (Valido(jogador)) return jogador;
        return null;
    }

    private static bool Valido(JogadorDados jogador)
    { return jogador != null && !string.IsNullOrWhiteSpace(jogador.nomeJogador); }

    private static string Funcao(PosicaoJogadorDiscos posicao)
    {
        switch (posicao)
        {
            case PosicaoJogadorDiscos.Defesa: return "DEF";
            case PosicaoJogadorDiscos.Meio: return "MID";
            case PosicaoJogadorDiscos.Ataque: return "FWD";
            default: return "AUTO";
        }
    }

    private static string FuncaoDoCapitao(PosicaoCapitaoDiscos posicao)
    { return posicao == PosicaoCapitaoDiscos.Ataque ? "FWD" : posicao == PosicaoCapitaoDiscos.Meio ? "MID" : "DEF"; }
}
#endif
