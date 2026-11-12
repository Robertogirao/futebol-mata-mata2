using System;using System.Collections.Generic;using System.Reflection;using System.IO;using System.Text.RegularExpressions;
namespace UnityEngine{
[AttributeUsage(AttributeTargets.All)]public sealed class HeaderAttribute:Attribute{public HeaderAttribute(string s){}}
[AttributeUsage(AttributeTargets.All)]public sealed class TooltipAttribute:Attribute{public TooltipAttribute(string s){}}
[AttributeUsage(AttributeTargets.All)]public sealed class MinAttribute:Attribute{public MinAttribute(float f){}}
[AttributeUsage(AttributeTargets.All)]public sealed class RangeAttribute:Attribute{public RangeAttribute(float a,float b){}}
[AttributeUsage(AttributeTargets.All)]public sealed class CreateAssetMenuAttribute:Attribute{public string fileName,menuName;public int order;}
public enum HideFlags{HideAndDontSave}
public class Object{public string name;public HideFlags hideFlags;public static readonly List<Object>objects=new List<Object>();public static T FindAnyObjectByType<T>()where T:class{foreach(var x in objects){var y=x as T;if(y!=null)return y;}return null;}}
public class ScriptableObject:Object{public static T CreateInstance<T>()where T:new(){return new T();}}
public class MonoBehaviour:Object{public GameObject gameObject;}
public class GameObject:Object{public UnityEngine.SceneManagement.Scene scene=new UnityEngine.SceneManagement.Scene{valid=true};}
public class Sprite:Object{}
public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white{get{return new Color(1,1,1);}}}
public class GUIContent{public string text;public GUIContent(string s){text=s;}}
public class GUILayoutOption{}
public static class GUILayout{public static HashSet<string>buttons=new HashSet<string>();public static GUILayoutOption Width(float f){return new GUILayoutOption();}public static void Label(string s,params object[]o){}public static bool Button(string s){return UnityEditor.EditorGUI.disabledCount==0&&buttons.Remove(s);}}
public static class Application{public static bool isPlaying;public static string persistentDataPath;}
public static class Debug{public static readonly List<string>messages=new List<string>();public static void Log(object s,Object c=null){messages.Add(s.ToString());}public static void LogWarning(object s,Object c=null){messages.Add(s.ToString());}}
}
namespace UnityEngine.SceneManagement{public struct Scene{public string path;public bool valid;public bool IsValid(){return valid;}}}
namespace UnityEditor{
[AttributeUsage(AttributeTargets.Class)]public class CustomEditor:Attribute{public CustomEditor(Type t){}}
[AttributeUsage(AttributeTargets.Method)]public class MenuItem:Attribute{public MenuItem(string s){}}
public class Editor{public UnityEngine.Object target;public SerializedObject serializedObject;public virtual void OnInspectorGUI(){}}
public class SceneAsset:UnityEngine.Object{}
public class SerializedObject{private object target;public int applies,updates;public SerializedObject(object o){target=o;}public void Update(){updates++;}public void ApplyModifiedProperties(){applies++;}public SerializedProperty FindProperty(string name){var f=target.GetType().GetField(name);return f==null?null:new SerializedProperty(target,f);}}
public class SerializedProperty{private object target;private FieldInfo field;private int index=-1;public string name{get{return field.Name+(index>=0?"["+index+"]":"");}}public SerializedProperty(object t,FieldInfo f){target=t;field=f;}private System.Collections.IList list{get{return (System.Collections.IList)field.GetValue(target);}}public int intValue{get{return Convert.ToInt32(index>=0?list[index]:field.GetValue(target));}set{Type t=index>=0?field.FieldType.GetGenericArguments()[0]:field.FieldType;object v=t.IsEnum?Enum.ToObject(t,value):(object)value;if(index>=0)list[index]=v;else field.SetValue(target,v);}}public float floatValue{get{return(float)field.GetValue(target);}set{field.SetValue(target,value);}}public int arraySize{get{return list.Count;}set{while(list.Count>value)list.RemoveAt(list.Count-1);Type t=field.FieldType.GetGenericArguments()[0];object previous=list.Count>0?list[list.Count-1]:(t.IsValueType?Activator.CreateInstance(t):null);while(list.Count<value)list.Add(previous);}}public SerializedProperty GetArrayElementAtIndex(int i){return new SerializedProperty(target,field){index=i};}public UnityEngine.Object objectReferenceValue{get{return(UnityEngine.Object)list[index];}set{list[index]=value;}}}
public static class EditorApplication{public static bool isPlayingOrWillChangePlaymode;}
public static class EditorStyles{public static object boldLabel=new object();}
public static class EditorGUI{public static int disabledCount,indentLevel;public sealed class DisabledScope:IDisposable{private bool disabled;public DisabledScope(bool b){disabled=b;if(b)disabledCount++;}public void Dispose(){if(disabled)disabledCount--;}}}
public enum MessageType{Info,Warning,Error}
public static class EditorGUILayout{public static Dictionary<string,int>ints=new Dictionary<string,int>();public static Dictionary<string,float>floats=new Dictionary<string,float>();public static float Slider(string s,float v,float min,float max){float n;if(EditorGUI.disabledCount==0&&floats.TryGetValue(s,out n))v=n;return Math.Max(min,Math.Min(max,v));}public static Dictionary<string,int>masks=new Dictionary<string,int>();public static int MaskField(string s,int v,string[]options){int n;return EditorGUI.disabledCount==0&&masks.TryGetValue(s,out n)?n:v;}public static List<string>properties=new List<string>();public static List<string>labels=new List<string>();public static void LabelField(string s,object style){labels.Add(s);}public static void LabelField(string s,string v){labels.Add(s+":"+v);}public static void PropertyField(SerializedProperty p,UnityEngine.GUIContent c,bool child=false){properties.Add(p.name);}public static int IntField(string s,int v){int n;return EditorGUI.disabledCount==0&&ints.TryGetValue(s,out n)?n:v;}public static void Space(){}public static void BeginHorizontal(){}public static void EndHorizontal(){}public static bool Foldout(bool b,string s,bool toggle){return b;}public static void HelpBox(string s,MessageType t){labels.Add(s);}}
public static class Undo{public static int count;public static void RecordObject(UnityEngine.Object o,string s){count++;}}
public static class EditorUtility{public static int dirties;public static string revealed;public static HashSet<UnityEngine.Object>persistent=new HashSet<UnityEngine.Object>();public static void SetDirty(UnityEngine.Object o){dirties++;}public static bool IsPersistent(UnityEngine.Object o){return persistent.Contains(o);}public static void RevealInFinder(string s){revealed=s;}}
public static class AssetDatabase{public static Dictionary<string,UnityEngine.Object>assets=new Dictionary<string,UnityEngine.Object>();public static void Register(string p,UnityEngine.Object o){assets[p]=o;}public static string[]FindAssets(string q){var a=new List<string>();foreach(var kv in assets)if(kv.Value is PokemonDados)a.Add(kv.Key);return a.ToArray();}public static string GUIDToAssetPath(string s){return s;}public static T LoadAssetAtPath<T>(string p)where T:class{UnityEngine.Object o;return assets.TryGetValue(p,out o)?o as T:null;}}
public static class Selection{public static UnityEngine.GameObject activeGameObject;}
}
namespace UnityEditor.SceneManagement{public enum OpenSceneMode{Single,Additive}public static class EditorSceneManager{public static int dirtyCount,savePrompts,opened;public static string lastScene;public static bool saveAllowed=true;public static void MarkSceneDirty(UnityEngine.SceneManagement.Scene s){dirtyCount++;}public static bool SaveCurrentModifiedScenesIfUserWantsTo(){savePrompts++;return saveAllowed;}public static UnityEngine.SceneManagement.Scene OpenScene(string s,OpenSceneMode m){opened++;lastScene=s;return new UnityEngine.SceneManagement.Scene{path=s,valid=true};}}}
public class JogadorDados{public string nomeJogador;public int numeroCamisa;public UnityEngine.Sprite foto512x512,fotoBola;}
public class TeamData:UnityEngine.ScriptableObject{public string nomeEquipe,sigla;public UnityEngine.Sprite escudoOuBandeira;public int overall;public UnityEngine.Color corPrincipal,corSecundaria;public bool participanteIndividual;public List<JogadorDados>elenco;}
public class TemaCompeticao:UnityEngine.ScriptableObject{}

namespace PokemonMarbleRace{
public enum RaceFormat{SingleRace,MiniGP,BestOfThree,Knockout}
public class MarbleRaceManager:UnityEngine.MonoBehaviour{
public string nomeCampeonato="Race",campeonatoId="pokemon_marble_race",temporadaId="temporada_01";
public TemaCompeticao tema;public UnityEngine.Sprite fundo,spriteBola,spriteSnorlax,spriteRocket;
public RaceFormat formato=RaceFormat.MiniGP;public int quantidadeParticipantes=8,quantidadeCorridas=3,semente;
public List<PokemonDados>participantes=new List<PokemonDados>();public List<int>geracoesPermitidas=new List<int>();public List<TipoPokemon>tiposPermitidos=new List<TipoPokemon>();
public float duracaoAlvo=45,duracaoMaxima=60,duracaoAbertura=4,duracaoResultado=7,intervaloEntreCorridas=2,tamanhoMarble=.55f;
public bool umaCorridaPorPlay,poderesPorTipo=true;public string situacao="Ready",ultimoErro;public bool Preparada,CampeonatoConcluido;
public int validations,continuations;public bool valid=true;public bool ValidarConfiguracao(){validations++;return valid;}public void ContinuarNoPlay(){continuations++;}
public string PastaDaTemporada{get{if(!Regex.IsMatch(campeonatoId,@"\A[A-Za-z0-9_-]+\z")||!Regex.IsMatch(temporadaId,@"\A[A-Za-z0-9_-]+\z"))throw new Exception("Identificador inválido");return Path.Combine(UnityEngine.Application.persistentDataPath,"MarbleRace",campeonatoId,temporadaId);}}}
}
