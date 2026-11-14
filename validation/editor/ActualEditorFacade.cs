using System;using System.Collections.Generic;using System.Reflection;
namespace UnityEngine {
 public class GUIContent {public string text,tooltip;public GUIContent(string s,string tip=null){text=s;tooltip=tip;}}
 public static class GUILayout {public static HashSet<string> buttons=new HashSet<string>();public static bool Button(string s){return UnityEditor.EditorGUI.disabledCount==0&&buttons.Remove(s);}}
}
namespace UnityEditor {
 [AttributeUsage(AttributeTargets.Class)]public class CustomEditor:Attribute {public CustomEditor(Type t){} }
 [AttributeUsage(AttributeTargets.Method)]public class MenuItem:Attribute {public MenuItem(string s){} public MenuItem(string s,bool validate){} }
 public class Editor {public static int defaultDrawCount;public UnityEngine.Object target;public SerializedObject serializedObject;public virtual void OnInspectorGUI(){} public void DrawDefaultInspector(){defaultDrawCount++;} public void Repaint(){} }
 public class SerializedObject {
  private object target;private readonly Dictionary<string,object> pending=new Dictionary<string,object>();private bool arraysChanged;
  public SerializedObject(object o){target=o;}
  public UnityEngine.Object targetObject{get{return (UnityEngine.Object)target;}}
  public void Update(){pending.Clear();arraysChanged=false;}
  public bool ApplyModifiedProperties(){bool changed=arraysChanged;foreach(var entry in pending){FieldInfo field=target.GetType().GetField(entry.Key);if(!Equals(field.GetValue(target),entry.Value)){field.SetValue(target,entry.Value);changed=true;}}pending.Clear();arraysChanged=false;return changed;}
  internal object Read(FieldInfo field){object value;return pending.TryGetValue(field.Name,out value)?value:field.GetValue(target);}
  internal void Write(FieldInfo field,object value){pending[field.Name]=value;}
  internal void MarkArrayChange(){arraysChanged=true;}
  public SerializedProperty FindProperty(string n){FieldInfo f=target.GetType().GetField(n);if(f==null)throw new Exception("Missing actual property "+n);return new SerializedProperty(this,f);}
 }
 public class SerializedProperty {
  SerializedObject owner;FieldInfo field;int index=-1;public SerializedProperty(SerializedObject o,FieldInfo f){owner=o;field=f;}
  public string name{get{return field.Name;}}private System.Collections.IList list{get{return (System.Collections.IList)owner.Read(field);}}
  public int intValue{get{return Convert.ToInt32(owner.Read(field));}set{owner.Write(field,field.FieldType.IsEnum?Enum.ToObject(field.FieldType,value):(object)value);}}
  public int enumValueIndex{get{return intValue;}set{intValue=value;}}
  public bool boolValue{get{return(bool)owner.Read(field);}set{owner.Write(field,value);}}
  public float floatValue{get{return(float)owner.Read(field);}set{owner.Write(field,value);}}
  public int arraySize{get{return list.Count;}set{if(list.Count!=value)owner.MarkArrayChange();while(list.Count>value)list.RemoveAt(list.Count-1);object prior=list.Count>0?list[list.Count-1]:null;while(list.Count<value)list.Add(prior);}}
  public SerializedProperty GetArrayElementAtIndex(int i){return new SerializedProperty(owner,field){index=i};}
  public UnityEngine.Object objectReferenceValue{get{return (UnityEngine.Object)(index<0?owner.Read(field):list[index]);}set{if(index<0)owner.Write(field,value);else if(!Equals(list[index],value)){list[index]=value;owner.MarkArrayChange();}}}
 }
 public static class EditorApplication {public static bool isPlayingOrWillChangePlaymode;}
 public static class EditorStyles {public static object boldLabel=new object();}
 public static class EditorGUI {public static int disabledCount,indentLevel;public class DisabledScope:IDisposable {bool disabled;public DisabledScope(bool b){disabled=b;if(b)disabledCount++;}public void Dispose(){if(disabled)disabledCount--;}}}
 public enum MessageType{None,Info,Warning,Error}
 public static class EditorGUILayout {
  public static Dictionary<string,bool> bools=new Dictionary<string,bool>();public static Dictionary<string,UnityEngine.Object> objects=new Dictionary<string,UnityEngine.Object>();public static Dictionary<string,int> ints=new Dictionary<string,int>();public static Dictionary<string,float> floats=new Dictionary<string,float>();public static Dictionary<string,int> popups=new Dictionary<string,int>();public static List<string> properties=new List<string>();public static List<string> labels=new List<string>();
  public class HorizontalScope:IDisposable {public void Dispose(){}}
  public static void HelpBox(string s,MessageType t){}public static void Space(float f=0){}public static void LabelField(string s,object o){labels.Add(s);}
  public static void PropertyField(SerializedProperty p,UnityEngine.GUIContent c,bool child=false){properties.Add(p.name);if(EditorGUI.disabledCount==0&&bools.ContainsKey(c.text))p.boolValue=bools[c.text];if(EditorGUI.disabledCount==0&&floats.ContainsKey(c.text))p.floatValue=floats[c.text];if(EditorGUI.disabledCount==0&&objects.ContainsKey(c.text))p.objectReferenceValue=objects[c.text];}
  public static int Popup(string s,int v,string[] opts){return EditorGUI.disabledCount==0&&popups.ContainsKey(s)?popups[s]:v;}
  public static int IntField(string s,int v){return EditorGUI.disabledCount==0&&ints.ContainsKey(s)?ints[s]:v;}
  public static bool Foldout(bool b,string s,bool toggle){return b;}
  public static float Slider(UnityEngine.GUIContent c,float v,float a,float b){return UnityEngine.Mathf.Clamp(EditorGUI.disabledCount==0&&floats.ContainsKey(c.text)?floats[c.text]:v,a,b);}
  public static int IntSlider(UnityEngine.GUIContent c,int v,int a,int b){return UnityEngine.Mathf.Clamp(EditorGUI.disabledCount==0&&ints.ContainsKey(c.text)?ints[c.text]:v,a,b);}
 }
 public static class Undo {public static int count;public static void RecordObject(UnityEngine.Object o,string s){count++;}}
 public static class EditorUtility {public static int dirty;public static HashSet<UnityEngine.Object> dirtyObjects=new HashSet<UnityEngine.Object>();public static string revealed;public static void SetDirty(UnityEngine.Object o){dirty++;dirtyObjects.Add(o);}public static bool IsPersistent(UnityEngine.Object o){return false;}public static void RevealInFinder(string s){revealed=s;}public static bool DisplayDialog(string a,string b,string c){return true;}}
 public static class AssetDatabase {public static Dictionary<string,UnityEngine.Object> assets=new Dictionary<string,UnityEngine.Object>();public static string[] FindAssets(string query){var result=new List<string>();foreach(var entry in assets)if(entry.Value!=null&&(!query.StartsWith("t:")||entry.Value.GetType().Name==query.Substring(2)))result.Add(entry.Key);return result.ToArray();}public static string GUIDToAssetPath(string g){return g;}public static string GetAssetPath(UnityEngine.Object o){return o.name;}public static T LoadAssetAtPath<T>(string p)where T:class{return assets.ContainsKey(p)?assets[p] as T:null;}}
 public enum SelectionMode {DeepAssets}
 public static class PrefabUtility{public static HashSet<UnityEngine.Object> instances=new HashSet<UnityEngine.Object>();public static int records;public static bool IsPartOfPrefabInstance(UnityEngine.Object o){return instances.Contains(o);}public static void RecordPrefabInstancePropertyModifications(UnityEngine.Object o){records++;}}
 public static class Selection {public static UnityEngine.GameObject activeGameObject;public static T[] GetFiltered<T>(SelectionMode m){return new T[0];}}
}
namespace UnityEditor.SceneManagement {public static class EditorSceneManager {public static int dirty,opened,prompts;public static string path;public static bool allowSave=true;public static void MarkSceneDirty(UnityEngine.SceneManagement.Scene s){dirty++;}public static bool IsPreviewSceneObject(UnityEngine.Object o){return false;}public static bool SaveCurrentModifiedScenesIfUserWantsTo(){prompts++;return allowSave;}public static UnityEngine.SceneManagement.Scene OpenScene(string s){opened++;path=s;return new UnityEngine.SceneManagement.Scene();}}}
