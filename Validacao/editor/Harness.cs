using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using PokemonMarbleRace;
using PokemonMarbleRace.EditorTools;

public static class Harness
{
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static object Call(string method,params object[] args){return typeof(MarbleRaceManagerEditor).GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);}
    static PokemonDados P(int n){return new PokemonDados{numeroPokedex=n,nomePokemon="Pokemon "+n,retrato=new Sprite(),tipoPrimario=TipoPokemon.Fogo,overall=75};}
    static MarbleRaceManager M(){return new MarbleRaceManager{gameObject=new GameObject{name="MarbleRace_Manager"}};}
    static MarbleRaceManagerEditor E(MarbleRaceManager m){return new MarbleRaceManagerEditor{target=m,serializedObject=new SerializedObject(m)};}
    static void Reset(){EditorApplication.isPlayingOrWillChangePlaymode=false;Application.isPlaying=false;EditorGUI.disabledCount=0;Undo.count=0;EditorUtility.dirties=0;EditorUtility.revealed=null;EditorUtility.persistent.Clear();EditorSceneManager.dirtyCount=0;EditorSceneManager.opened=0;EditorSceneManager.savePrompts=0;EditorSceneManager.saveAllowed=true;AssetDatabase.assets.Clear();UnityEngine.Object.objects.Clear();EditorGUILayout.ints.Clear();EditorGUILayout.masks.Clear();EditorGUILayout.floats.Clear();EditorGUILayout.properties.Clear();EditorGUILayout.labels.Clear();GUILayout.buttons.Clear();Debug.messages.Clear();}
    static void Gui()
    {
        Reset();var m=M();var a=P(1);var b=P(2);m.participantes.Add(a);m.participantes.Add(b);var e=E(m);e.OnInspectorGUI();
        Check(m.participantes.Count==8,"default roster eight");Check(m.participantes[0]==a&&m.participantes[1]==b,"existing roster intact");
        for(int i=2;i<8;i++)Check(m.participantes[i]==null,"new vacancies are null");
        foreach(string f in new[]{"nomeCampeonato","campeonatoId","temporadaId","tema","fundo","spriteBola","spriteSnorlax","spriteRocket","formato","semente","duracaoAlvo","duracaoMaxima","duracaoAbertura","duracaoResultado","intervaloEntreCorridas","umaCorridaPorPlay","poderesPorTipo"})Check(EditorGUILayout.properties.Count(x=>x==f)==1,"config field once "+f);
        Check(m.tamanhoMarble==.55f,"default marble diameter preserved");EditorGUILayout.floats["Diâmetro das marbles"]=.1f;e.OnInspectorGUI();Check(m.tamanhoMarble==.3f,"marble diameter minimum .3");EditorGUILayout.floats["Diâmetro das marbles"]=.9f;e.OnInspectorGUI();Check(m.tamanhoMarble==.65f,"marble diameter maximum .65");EditorGUILayout.floats.Clear();
        EditorGUILayout.masks["Gerações permitidas"]=(1<<0)|(1<<2);EditorGUILayout.masks["Tipos permitidos"]=(1<<1)|(1<<2);e.OnInspectorGUI();
        Check(m.geracoesPermitidas.SequenceEqual(new[]{1,3}),"generation multiselect");Check(m.tiposPermitidos.SequenceEqual(new[]{TipoPokemon.Fogo,TipoPokemon.Agua}),"type multiselect enum serialization");
        EditorGUILayout.masks.Clear();m.formato=RaceFormat.BestOfThree;e.OnInspectorGUI();Check(m.quantidadeParticipantes==8&&m.participantes.Count==8,"format switch never silently shrinks roster");Check(EditorGUILayout.labels.Any(x=>x.Contains("exatamente dois")),"duel count explained");
        GUILayout.buttons.Add("2 Pokémon");e.OnInspectorGUI();Check(m.quantidadeParticipantes==2&&m.participantes.SequenceEqual(new[]{a,b}),"explicit two preset preserves first choices");
        m.formato=RaceFormat.Knockout;GUILayout.buttons.Add("32 Pokémon");e.OnInspectorGUI();Check(m.participantes.Count==32,"knockout preset32");for(int i=2;i<32;i++)Check(m.participantes[i]==null,"expanded knockout slots empty");
        m.formato=RaceFormat.MiniGP;e.OnInspectorGUI();Check(m.quantidadeParticipantes==32&&m.participantes.Count==32,"oversized roster kept when switching format");GUILayout.buttons.Add("16 Pokémon");e.OnInspectorGUI();Check(m.quantidadeParticipantes==16&&m.participantes.Count==16,"explicit GP16 preset");
        EditorGUILayout.ints["Corridas do Mini GP"]=0;e.OnInspectorGUI();Check(m.quantidadeCorridas==1,"GP min one");EditorGUILayout.ints["Corridas do Mini GP"]=100;e.OnInspectorGUI();Check(m.quantidadeCorridas==30,"GP max30");EditorGUILayout.ints.Clear();
        m.formato=RaceFormat.SingleRace;m.quantidadeCorridas=3;EditorGUILayout.ints["Corridas do Mini GP"]=9;e.OnInspectorGUI();Check(m.quantidadeCorridas==3,"nonGP race count not edited");EditorGUILayout.ints.Clear();
        m.formato=RaceFormat.MiniGP;m.quantidadeParticipantes=0;m.quantidadeCorridas=0;m.tamanhoMarble=.9f;var roster=m.participantes.ToArray();Application.isPlaying=true;EditorApplication.isPlayingOrWillChangePlaymode=true;GUILayout.buttons.Add("8 Pokémon");EditorGUILayout.masks["Gerações permitidas"]=0;e.OnInspectorGUI();Check(m.quantidadeParticipantes==0&&m.quantidadeCorridas==0,"Play never normalizes config");Check(m.tamanhoMarble==.9f,"Play does not normalize marble size");Check(roster.SequenceEqual(m.participantes),"Play never resizes roster");Check(m.geracoesPermitidas.SequenceEqual(new[]{1,3}),"Play never edits filters");Check(EditorGUI.disabledCount==0,"GUI disabled scopes balanced");
        Console.WriteLine("PASS central controls, all format presets, multiselect and immutable Play configuration");
    }
    static void Catalog()
    {
        Reset();for(int n=1;n<=400;n++){var p=P(n);p.tipoPrimario=n%3==0?TipoPokemon.Fogo:n%3==1?TipoPokemon.Agua:TipoPokemon.Eletrico;p.tipoSecundario=n%5==0?TipoPokemon.Fogo:TipoPokemon.Nenhum;AssetDatabase.Register("Pokemon_"+n.ToString("D4")+".asset",p);}AssetDatabase.Register("duplicate.asset",P(1));
        var m=M();m.geracoesPermitidas.AddRange(new[]{1,2});m.tiposPermitidos.AddRange(new[]{TipoPokemon.Fogo,TipoPokemon.Agua});
        foreach(int count in new[]{8,16}){m.quantidadeParticipantes=count;Call("SortearParticipantes",m);Check(m.participantes.Count==count,"draw count "+count);Check(m.participantes.Select(p=>p.IdEstavel).Distinct().Count()==count,"distinct roster "+count);foreach(var p in m.participantes)Check((p.Geracao==1||p.Geracao==2)&&(p.PertenceAoTipo(TipoPokemon.Fogo)||p.PertenceAoTipo(TipoPokemon.Agua)),"OR within filters and AND across groups");}
        Check(AssetDatabase.assets.Count==401,"catalog assets not created/deleted");Check(Undo.count==2&&EditorUtility.dirties==2&&EditorSceneManager.dirtyCount==2,"sorteio Undo and scene dirty");
        var old=m.participantes;m.geracoesPermitidas=new List<int>{9};Call("SortearParticipantes",m);Check(ReferenceEquals(old,m.participantes)&&m.ultimoErro.Contains("0"),"insufficient filtered catalog preserves roster");Check(Undo.count==2,"failed draw no mutation/Undo");
        m.formato=RaceFormat.BestOfThree;m.quantidadeParticipantes=8;Call("SortearParticipantes",m);Check(ReferenceEquals(old,m.participantes)&&m.ultimoErro.Contains("formato"),"invalid format rejects before draw");
        AssetDatabase.assets.Clear();var secondaryA=P(25);secondaryA.tipoPrimario=TipoPokemon.Eletrico;secondaryA.tipoSecundario=TipoPokemon.Fogo;var secondaryB=P(39);secondaryB.tipoPrimario=TipoPokemon.Normal;secondaryB.tipoSecundario=TipoPokemon.Fogo;AssetDatabase.Register("a",secondaryA);AssetDatabase.Register("b",secondaryB);AssetDatabase.Register("c",P(300));m.quantidadeParticipantes=2;m.geracoesPermitidas=new List<int>{1};m.tiposPermitidos=new List<TipoPokemon>{TipoPokemon.Fogo};Call("SortearParticipantes",m);Check(m.participantes.Count==2&&m.participantes.Contains(secondaryA)&&m.participantes.Contains(secondaryB),"secondary type counts in filter");
        old=m.participantes;AssetDatabase.Register("invalid",new PokemonDados{numeroPokedex=1,nomePokemon="No portrait",tipoPrimario=TipoPokemon.Fogo});Call("SortearParticipantes",m);Check(ReferenceEquals(old,m.participantes)&&m.ultimoErro.Contains("retrato"),"invalid candidate rejects without changing roster");
        int undo=Undo.count;EditorApplication.isPlayingOrWillChangePlaymode=true;Call("SortearParticipantes",m);Check(ReferenceEquals(old,m.participantes)&&Undo.count==undo,"draw locked during Play");
        Console.WriteLine("PASS filtered distinct draws, secondary types, invalid counts/cards and failure immutability");
    }
    static void SeasonAndMenu()
    {
        Reset();var m=M();m.participantes.Add(P(1));var roster=m.participantes;string previous=m.temporadaId;string folder=m.PastaDaTemporada;Directory.CreateDirectory(folder);string marker=Path.Combine(folder,"season.json");File.WriteAllText(marker,"SAVED RACE");Call("NovaTemporada",m);string first=m.temporadaId;Check(first!=previous&&Regex.IsMatch(first,@"\A[A-Za-z0-9_-]{1,80}\z"),"safe season ID");Check(File.ReadAllText(marker)=="SAVED RACE","previous results preserved");Check(!Directory.Exists(m.PastaDaTemporada),"new season action does not write progress");Call("NovaTemporada",m);Check(m.temporadaId!=first,"new ID same second unique");Check(ReferenceEquals(roster,m.participantes)&&m.quantidadeCorridas==3,"new season only ID");Check(Undo.count==2&&EditorSceneManager.dirtyCount==2,"new season Undo+scene dirty");
        EditorApplication.isPlayingOrWillChangePlaymode=true;first=m.temporadaId;Call("NovaTemporada",m);Check(first==m.temporadaId&&Undo.count==2,"new season blocked in Play");EditorApplication.isPlayingOrWillChangePlaymode=false;Call("AbrirPasta",m);Check(Directory.Exists(m.PastaDaTemporada)&&EditorUtility.revealed==m.PastaDaTemporada,"correct season folder");m.temporadaId="../bad";EditorUtility.revealed=null;Call("AbrirPasta",m);Check(EditorUtility.revealed==null,"invalid path not opened");
        Reset();m=M();UnityEngine.Object.objects.Add(m);AssetDatabase.Register(MarbleRaceManagerEditor.CaminhoCena,new SceneAsset());EditorSceneManager.saveAllowed=false;MarbleRaceManagerEditor.AbrirCena();Check(EditorSceneManager.savePrompts==1&&EditorSceneManager.opened==0,"unsaved cancellation respected");EditorSceneManager.saveAllowed=true;MarbleRaceManagerEditor.AbrirCena();Check(EditorSceneManager.lastScene==MarbleRaceManagerEditor.CaminhoCena&&Selection.activeGameObject==m.gameObject,"actual race scene opens and config selected");EditorApplication.isPlayingOrWillChangePlaymode=true;MarbleRaceManagerEditor.AbrirCena();Check(EditorSceneManager.opened==1&&EditorSceneManager.savePrompts==2,"scene menu Play lock");
        Reset();m=M();var e=E(m);GUILayout.buttons.Add("Validar competição");e.OnInspectorGUI();Check(m.validations==1,"validator button routed");Application.isPlaying=true;EditorApplication.isPlayingOrWillChangePlaymode=true;m.Preparada=true;GUILayout.buttons.Add("Continuar próxima corrida no Play");e.OnInspectorGUI();Check(m.continuations==1,"manual continue routed in Play");m.CampeonatoConcluido=true;GUILayout.buttons.Add("Continuar próxima corrida no Play");e.OnInspectorGUI();Check(m.continuations==1,"completed competition cannot continue");
        Console.WriteLine("PASS isolated seasons, saved-file preservation, scene menu and continuation actions");
    }
    public static void Main(){Application.persistentDataPath="/workspace/work/pokemon-marble-race/validation/editor/sandbox";if(Directory.Exists(Application.persistentDataPath))Directory.Delete(Application.persistentDataPath,true);Gui();Catalog();SeasonAndMenu();Console.WriteLine("PASS "+checks+" assertions using real MarbleRaceManagerEditor/PokemonDados with UnityEditor/manager contract stubs");}
}
