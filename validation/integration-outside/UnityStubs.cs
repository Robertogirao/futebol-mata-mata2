using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
namespace UnityEngine {
[AttributeUsage(AttributeTargets.All)] public class InspectorNameAttribute:Attribute{public InspectorNameAttribute(string n){}}
[AttributeUsage(AttributeTargets.All)] public class TextAreaAttribute:Attribute{public TextAreaAttribute(int a,int b){}}
[AttributeUsage(AttributeTargets.All)] public class HeaderAttribute:Attribute{public HeaderAttribute(string s){}}
[AttributeUsage(AttributeTargets.All)] public class TooltipAttribute:Attribute{public TooltipAttribute(string s){}}
[AttributeUsage(AttributeTargets.All)] public class MinAttribute:Attribute{public MinAttribute(float n){}}
[AttributeUsage(AttributeTargets.All)] public class RangeAttribute:Attribute{public RangeAttribute(float a,float b){}}
[AttributeUsage(AttributeTargets.All)] public class HideInInspector:Attribute{}
[AttributeUsage(AttributeTargets.All)] public class DisallowMultipleComponent:Attribute{}
[AttributeUsage(AttributeTargets.All)] public class ContextMenu:Attribute{public ContextMenu(string n){}}
public class Object { public string name; public bool destroyed; public static void Destroy(Object o){if(o!=null)o.destroyed=true;} public static T FindAnyObjectByType<T>() where T:Object{return null;} public static T[] FindObjectsByType<T>() where T:Object{return new T[0];} public static T[] FindObjectsByType<T>(FindObjectsInactive inactive) where T:Object{return new T[0];} }
public enum FindObjectsInactive{Exclude,Include}
public class GameObject:Object {
 Dictionary<Type,Component> comps=new Dictionary<Type,Component>(); public Transform transform; public int layer; public bool activeSelf=true;
 public GameObject(string n="",params Type[] types){name=n; bool rect=Array.IndexOf(types,typeof(RectTransform))>=0; transform=rect?(Transform)new RectTransform():new Transform();transform.gameObject=this; comps[transform.GetType()]=transform; foreach(var t in types)if(!typeof(Transform).IsAssignableFrom(t))AddComponent(t);}
 public bool activeInHierarchy{get{return activeSelf&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);}}
 public Component AddComponent(Type t){var c=(Component)Activator.CreateInstance(t);c.gameObject=this;comps[t]=c;return c;}
 public T AddComponent<T>()where T:Component,new(){return(T)AddComponent(typeof(T));}
 public T GetComponent<T>()where T:class{foreach(var c in comps.Values)if(c is T)return c as T;return null;}
 public void SetActive(bool active){bool old=activeInHierarchy;activeSelf=active;if(old&&!activeInHierarchy)DisableTree();}
 void DisableTree(){foreach(var c in comps.Values){var m=c.GetType().GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);if(m!=null)m.Invoke(c,null);} foreach(var child in transform.children)child.gameObject.DisableTree();}
}
public class Component:Object {public T GetComponentInChildren<T>(bool inactive=false)where T:class{var own=gameObject.GetComponent<T>();if(own!=null)return own;foreach(var child in transform.children){if(inactive||child.gameObject.activeInHierarchy){var c=child.GetComponentInChildren<T>(inactive);if(c!=null)return c;}}return null;}public GameObject gameObject;public Transform transform{get{return gameObject.transform;}}public T GetComponent<T>()where T:class{return gameObject.GetComponent<T>();}public T GetComponentInParent<T>()where T:class{for(var t=transform;t!=null;t=t.parent){var c=t.gameObject.GetComponent<T>();if(c!=null)return c;}return null;}}
public class Behaviour:Component{public bool enabled=true; public bool isActiveAndEnabled{get{return enabled&&gameObject!=null&&gameObject.activeInHierarchy&&!destroyed;}}}
public class MonoBehaviour:Behaviour{
 public Coroutine StartCoroutine(IEnumerator e){return Scheduler.Start(e);} public void StopCoroutine(Coroutine c){if(c!=null)c.stopped=true;}public void StopAllCoroutines(){}public void CancelInvoke(string s){}public void Invoke(string s,float f){}
}
public class Coroutine{public Stack<IEnumerator> stack=new Stack<IEnumerator>();public bool stopped;public Coroutine(IEnumerator e){stack.Push(e);}}
public static class Scheduler{
 static List<Coroutine> routines=new List<Coroutine>();public static double Seconds;
 public static void Reset(){routines.Clear();Seconds=0;Time.timeScale=1;Time.captureDeltaTime=0;Time.unscaledDeltaTime=0.1f;Time.deltaTime=.1f;}
 public static Coroutine Start(IEnumerator e){var c=new Coroutine(e);routines.Add(c);Advance(c);return c;}
 static void Advance(Coroutine c){while(!c.stopped&&c.stack.Count>0){var e=c.stack.Peek();if(!e.MoveNext()){c.stack.Pop();continue;}var child=e.Current as IEnumerator;if(child!=null){c.stack.Push(child);continue;}return;}}
 public static void Tick(float dt=.1f){Time.unscaledDeltaTime=dt;Time.deltaTime=dt*Time.timeScale;Seconds+=dt;foreach(var c in routines.ToArray())Advance(c);routines.RemoveAll(c=>c.stopped||c.stack.Count==0);}
}
public class Transform:Component{public Transform parent;public List<Transform>children=new List<Transform>();public Vector3 localScale=Vector3.one;public void SetParent(Transform p,bool world){parent=p;if(p!=null)p.children.Add(this);}public void SetAsFirstSibling(){}public Transform Find(string n){return children.Find(c=>c.gameObject.name==n);}}
public class RectTransform:Transform{public Vector2 anchorMin,anchorMax,offsetMin,offsetMax;}
public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero{get{return new Vector2();}}public static Vector2 one{get{return new Vector2(1,1);}}public static Vector2 operator+(Vector2 a,Vector2 b){return new Vector2(a.x+b.x,a.y+b.y);}public static Vector2 operator-(Vector2 a,Vector2 b){return new Vector2(a.x-b.x,a.y-b.y);}public static Vector2 operator*(Vector2 a,float b){return new Vector2(a.x*b,a.y*b);}public static Vector2 Lerp(Vector2 a,Vector2 b,float t){return a+(b-a)*Mathf.Clamp01(t);}}
public struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 one{get{return new Vector3(1,1,1);}}public static Vector3 Scale(Vector3 a,Vector3 b){return new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);}public static Vector3 Lerp(Vector3 a,Vector3 b,float t){t=Mathf.Clamp01(t);return new Vector3(a.x+(b.x-a.x)*t,a.y+(b.y-a.y)*t,a.z+(b.z-a.z)*t);}}
public struct Vector4{public static Vector4 zero{get{return new Vector4();}}}
public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white{get{return new Color(1,1,1);}}public static Color yellow{get{return new Color(1,1,0);}}public float maxColorComponent{get{return Math.Max(r,Math.Max(g,b));}}public static Color Lerp(Color a,Color b,float t){return new Color(a.r+(b.r-a.r)*t,a.g+(b.g-a.g)*t,a.b+(b.b-a.b)*t,a.a+(b.a-a.a)*t);}}
public static class Mathf{public static float Clamp(float v,float min,float max){return Math.Max(min,Math.Min(max,v));}public static float Max(float a,float b){return Math.Max(a,b);}public static int Max(int a,int b){return Math.Max(a,b);}public static float Clamp01(float n){return Math.Max(0,Math.Min(1,n));}public static float SmoothStep(float a,float b,float n){n=Clamp01(n);return a+(b-a)*(n*n*(3-2*n));}public static int FloorToInt(float n){return(int)Math.Floor(n);}}
public static class Time{public static float timeScale=1,deltaTime=.1f,unscaledDeltaTime=.1f,captureDeltaTime;}
public static class Debug{public static List<string> errors=new List<string>();public static void Log(object s,Object o=null){}public static void LogWarning(object s,Object o=null){}public static void LogError(object s,Object o=null){errors.Add(s.ToString());}}
public class Canvas:Behaviour{}
public class Sprite:Object{}public class TextAsset:Object{public string text;}public class Rigidbody2D:Component{}
public class AudioClip:Object{}public class AudioSource:Behaviour{public bool playOnAwake,loop;public float spatialBlend;public int plays;public void PlayOneShot(AudioClip clip,float v){plays++;}}
public class WaitForSecondsRealtime{public WaitForSecondsRealtime(float s){}}
public static class Application{public static bool isPlaying=true;public static string persistentDataPath;public static void OpenURL(string s){}}
}
namespace UnityEngine.UI{public class Image:UnityEngine.Behaviour{public UnityEngine.Sprite sprite;public UnityEngine.Color color;public bool raycastTarget,preserveAspect;}}
namespace TMPro{public class TMP_FontAsset:UnityEngine.Object{}public static class TMP_Settings{public static TMP_FontAsset defaultFontAsset=new TMP_FontAsset();}public enum FontStyles{Normal,Bold}public enum TextAlignmentOptions{Center,MidlineLeft}public enum TextOverflowModes{Ellipsis}public enum TextWrappingModes{Normal}public class TextMeshProUGUI:UnityEngine.Behaviour{public TMP_FontAsset font;public float fontSize,fontSizeMin,fontSizeMax,characterSpacing;public bool enableAutoSizing,richText,raycastTarget;public FontStyles fontStyle;public TextAlignmentOptions alignment;public TextOverflowModes overflowMode;public TextWrappingModes textWrappingMode;public UnityEngine.Color color;public UnityEngine.Vector4 margin;public string text;}}

namespace UnityEngine{public static class JsonUtility{
public static string ToJson(object obj,bool pretty=false){var s=new System.Xml.Serialization.XmlSerializer(obj.GetType());using(var w=new System.IO.StringWriter()){s.Serialize(w,obj);return w.ToString();}}
public static T FromJson<T>(string text){var s=new System.Xml.Serialization.XmlSerializer(typeof(T));using(var r=new System.IO.StringReader(text))return(T)s.Deserialize(r);}
}}
