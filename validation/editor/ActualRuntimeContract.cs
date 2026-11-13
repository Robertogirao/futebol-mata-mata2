// Contract facade for C# compilation and lifecycle tests. Not Unity, rendering, or a collision solver.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.All)] public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string value) { } }
    [AttributeUsage(AttributeTargets.All)] public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string value) { } }
    [AttributeUsage(AttributeTargets.All)] public sealed class MinAttribute : Attribute { public MinAttribute(float value) { } }
    [AttributeUsage(AttributeTargets.All)] public sealed class RangeAttribute : Attribute { public RangeAttribute(float min, float max) { } }
    [AttributeUsage(AttributeTargets.All)] public sealed class InspectorNameAttribute : Attribute { public InspectorNameAttribute(string value) { } }
    [AttributeUsage(AttributeTargets.All)] public sealed class HideInInspector : Attribute { }
    [AttributeUsage(AttributeTargets.All)] public sealed class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.All)] public sealed class TextAreaAttribute : Attribute { public TextAreaAttribute(int min = 3, int max = 3) { } }
    [AttributeUsage(AttributeTargets.All)] public sealed class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.All)] public sealed class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int order) { } }
    [AttributeUsage(AttributeTargets.All)] public sealed class RequireComponent : Attribute { public RequireComponent(Type first, Type second = null, Type third = null) { } }
    [AttributeUsage(AttributeTargets.All)] public sealed class ContextMenu : Attribute { public ContextMenu(string value) { } }
    [AttributeUsage(AttributeTargets.All)] public sealed class CreateAssetMenuAttribute : Attribute { public string fileName, menuName; public int order; }
    public enum HideFlags { None = 0, DontSave=52, HideAndDontSave = 61 }
    public enum FindObjectsInactive { Exclude, Include }

    public class Object
    {
        public string name;
        public bool destroyed;
        static bool IsNull(Object value) { return ReferenceEquals(value,null)||value.destroyed; }
        public static bool operator ==(Object a,Object b) { if(IsNull(a))return IsNull(b);if(IsNull(b))return false;return ReferenceEquals(a,b); }
        public static bool operator !=(Object a,Object b) { return !(a==b); }
        public override bool Equals(object other) { return this==(other as Object); }
        public override int GetHashCode() { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this); }
        public HideFlags hideFlags;
        public static implicit operator bool(Object value) { return value != null && !value.destroyed; }
        public static void Destroy(Object value) { Lifecycle.Destroy(value); }
        public static void DestroyImmediate(Object value) { Lifecycle.Destroy(value); }
        public static T Instantiate<T>(T source) where T : Object
        {
            if (source == null) return null;
            if (source is GameObject) throw new NotSupportedException("The facade does not clone GameObjects.");
            T copy = (T)Activator.CreateInstance(source.GetType());
            foreach (FieldInfo field in source.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)) field.SetValue(copy, field.GetValue(source));
            return copy;
        }
        public static T FindAnyObjectByType<T>() where T : Object { return FindObjectsByType<T>().FirstOrDefault(); }
        public static T[] FindObjectsByType<T>() where T : Object { return FindObjectsByType<T>(FindObjectsInactive.Exclude); }
        public static T[] FindObjectsByType<T>(FindObjectsInactive inactive) where T : Object
        {
            return GameObject.All.Where(g => !g.destroyed && (inactive == FindObjectsInactive.Include || g.activeInHierarchy))
                .SelectMany(g => g.Components).OfType<T>().Where(c => !c.destroyed).ToArray();
        }
    }
    public class TextAsset : Object { public string text; public TextAsset(string text) { this.text = text; } }
    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() { return new T(); }
        public static ScriptableObject CreateInstance(Type type) { return (ScriptableObject)Activator.CreateInstance(type); }
    }
    public static class Lifecycle
    {
        public static void Invoke(Component component, string method)
        {
            MethodInfo target = component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (target == null) return;
            try { target.Invoke(component, null); }
            catch (TargetInvocationException error) { throw error.InnerException; }
        }
        public static void Enable(Component component)
        {
            Behaviour behaviour = component as Behaviour;
            if (behaviour == null) return;
            if (!behaviour.awakeCalled) { behaviour.awakeCalled = true; Invoke(component, "Awake"); }
            if (behaviour.enabled && component.gameObject.activeInHierarchy && !behaviour.onEnableRegistered)
            { behaviour.onEnableRegistered = true; Invoke(component, "OnEnable"); }
        }
        public static void Disable(Component component)
        {
            Behaviour behaviour = component as Behaviour;
            if (behaviour == null || !behaviour.onEnableRegistered) return;
            behaviour.onEnableRegistered = false;
            Scheduler.StopOwner(component as MonoBehaviour);
            Invoke(component, "OnDisable");
        }
        public static void Destroy(Object value)
        {
            if (value == null || value.destroyed) return;
            GameObject gameObject = value as GameObject;
            if (gameObject != null)
            {
                foreach (Transform child in gameObject.transform.Children.ToArray()) Destroy(child.gameObject);
                foreach (Component component in gameObject.Components.ToArray()) { Disable(component); Invoke(component, "OnDestroy"); component.destroyed = true; }
                if (gameObject.transform.parent != null) gameObject.transform.parent.Children.Remove(gameObject.transform);
            }
            else
            {
                Component component = value as Component;
                if (component != null) { Disable(component); Invoke(component, "OnDestroy"); }
            }
            value.destroyed = true;
        }
    }
    public class GameObject : Object
    {
        public static readonly List<GameObject> All = new List<GameObject>();
        private readonly List<Component> components = new List<Component>();
        public IEnumerable<Component> Components { get { return components; } }
        public Transform transform;
        public int layer;
        public string tag;
        public bool activeSelf = true;
        public bool activeInHierarchy { get { return !destroyed && activeSelf && (transform.parent == null || transform.parent.gameObject.activeInHierarchy); } }
        public UnityEngine.SceneManagement.Scene scene;
        public GameObject(string name = "", params Type[] types)
        {
            this.name = name;
            All.Add(this);
            bool rect = types.Any(t => typeof(RectTransform).IsAssignableFrom(t));
            transform = rect ? new RectTransform() : new Transform();
            transform.gameObject = this;
            components.Add(transform);
            foreach (Type type in types) if (!typeof(Transform).IsAssignableFrom(type)) AddComponent(type);
        }
        public T AddComponent<T>() where T : Component, new() { return (T)AddComponent(typeof(T)); }
        public Component AddComponent(Type type)
        {
            if (typeof(Transform).IsAssignableFrom(type)) return transform;
            if (typeof(TMPro.TMP_Text).IsAssignableFrom(type) && !(transform is RectTransform)) UpgradeRectTransform();
            Component component = (Component)Activator.CreateInstance(type);
            component.gameObject = this;
            components.Add(component);
            if (type == typeof(TMPro.TextMeshPro) && GetComponent<Renderer>() == null) AddComponent<MeshRenderer>();
            if (activeInHierarchy) Lifecycle.Enable(component);
            return component;
        }
        private void UpgradeRectTransform()
        {
            Transform old = transform;
            RectTransform replacement = new RectTransform { gameObject = this, localPosition = old.localPosition, localScale = old.localScale, localRotation = old.localRotation };
            if (old.parent != null) { replacement.SetParent(old.parent, false); old.parent.Children.Remove(old); }
            foreach (Transform child in old.Children.ToArray()) child.SetParent(replacement, false);
            components.Remove(old); components.Add(replacement); transform = replacement;
        }
        public T GetComponent<T>() where T : class { return components.OfType<T>().Where(c=>!(c is Object)||!((Object)(object)c).destroyed).FirstOrDefault(); }
        public Component GetComponent(Type type) { return components.FirstOrDefault(c => type.IsAssignableFrom(c.GetType())); }
        public T[] GetComponents<T>() where T : Component { return components.OfType<T>().Where(c => !c.destroyed).ToArray(); }
        public T[] GetComponentsInChildren<T>(bool includeInactive = false) where T : Component
        {
            var result = new List<T>();
            if (includeInactive || activeInHierarchy) result.AddRange(GetComponents<T>());
            foreach (Transform child in transform.Children) result.AddRange(child.gameObject.GetComponentsInChildren<T>(includeInactive));
            return result.ToArray();
        }
        public bool TryGetComponent<T>(out T component) where T : class { component = GetComponent<T>(); return component != null; }
        public void SetActive(bool value)
        {
            bool previous = activeInHierarchy;
            activeSelf = value;
            if (previous == activeInHierarchy) return;
            foreach (Component component in components.ToArray()) if (activeInHierarchy) Lifecycle.Enable(component); else Lifecycle.Disable(component);
            foreach (Transform child in transform.Children.ToArray()) child.gameObject.RefreshActivation();
        }
        private void RefreshActivation()
        {
            foreach (Component component in components.ToArray()) if (activeInHierarchy) Lifecycle.Enable(component); else Lifecycle.Disable(component);
            foreach (Transform child in transform.Children.ToArray()) child.gameObject.RefreshActivation();
        }
        public static GameObject Find(string name) { return All.FirstOrDefault(g => !g.destroyed && g.name == name); }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform { get { return gameObject.transform; } }
        public T GetComponent<T>() where T : class { return gameObject.GetComponent<T>(); }
        public T[] GetComponents<T>() where T : Component { return gameObject.GetComponents<T>(); }
        public T[] GetComponentsInChildren<T>(bool includeInactive = false) where T : Component { return gameObject.GetComponentsInChildren<T>(includeInactive); }
        public T GetComponentInChildren<T>(bool includeInactive = false) where T : Component { return GetComponentsInChildren<T>(includeInactive).FirstOrDefault(); }
        public T GetComponentInParent<T>() where T : class { for (Transform t = transform; t != null; t = t.parent) { T c = t.gameObject.GetComponent<T>(); if (c != null) return c; } return null; }
        public bool TryGetComponent<T>(out T component) where T : class { return gameObject.TryGetComponent(out component); }
    }
    public class Behaviour : Component
    {
        private bool enabledValue = true;
        public bool awakeCalled, onEnableRegistered;
        public bool enabled { get { return enabledValue; } set { if (enabledValue == value) return; enabledValue = value; if (gameObject != null) { if (value && gameObject.activeInHierarchy) Lifecycle.Enable(this); else Lifecycle.Disable(this); } } }
        public bool isActiveAndEnabled { get { return enabled && onEnableRegistered && gameObject != null && gameObject.activeInHierarchy && !destroyed; } }
    }
    public class MonoBehaviour : Behaviour
    {
        public void CancelInvoke(string method) { Scheduler.CancelInvoke(this,method); }
        public void Invoke(string method, float seconds) { Scheduler.ScheduleInvoke(this,method,seconds); }
        public Coroutine StartCoroutine(IEnumerator sequence) { return Scheduler.Start(this, sequence); }
        public void StopCoroutine(Coroutine sequence) { if (sequence != null) sequence.stopped = true; }
        public void StopAllCoroutines() { Scheduler.StopOwner(this); }
    }
    public class Coroutine
    {
        public MonoBehaviour owner; public Stack<IEnumerator> stack = new Stack<IEnumerator>(); public bool stopped;
        public double waitUntil; public bool waitRealtime; public Coroutine dependency;
        public bool Done { get { return stopped || stack.Count == 0; } }
    }
    public static class Scheduler
    {
        public static readonly List<Coroutine> Routines = new List<Coroutine>();
        sealed class Invocation { public MonoBehaviour owner; public string method; public double due; }
        static readonly List<Invocation> invocations = new List<Invocation>();
        public static void ScheduleInvoke(MonoBehaviour owner,string method,float seconds){invocations.Add(new Invocation{owner=owner,method=method,due=ScaledSeconds+seconds});}
        public static void CancelInvoke(MonoBehaviour owner,string method){invocations.RemoveAll(i=>i.owner==owner&&i.method==method);}
        public static double Seconds, ScaledSeconds;
        public static void Reset() { Routines.Clear(); invocations.Clear(); Seconds = ScaledSeconds = 0; Time.timeScale = 1; Time.deltaTime = Time.unscaledDeltaTime = Time.fixedDeltaTime = .02f; }
        public static Coroutine Start(MonoBehaviour owner, IEnumerator sequence)
        {
            Coroutine coroutine = new Coroutine { owner = owner }; coroutine.stack.Push(sequence); Routines.Add(coroutine); Advance(coroutine); return coroutine;
        }
        public static void StopOwner(MonoBehaviour owner) { if (owner != null) { foreach (Coroutine c in Routines.Where(c => c.owner == owner)) c.stopped = true; invocations.RemoveAll(i=>i.owner==owner); } }
        private static void Advance(Coroutine coroutine)
        {
            while (!coroutine.Done)
            {
                IEnumerator sequence = coroutine.stack.Peek();
                if (!sequence.MoveNext()) { coroutine.stack.Pop(); continue; }
                IEnumerator nested = sequence.Current as IEnumerator;
                if (nested != null) { coroutine.stack.Push(nested); continue; }
                WaitForSecondsRealtime realtime = sequence.Current as WaitForSecondsRealtime;
                WaitForSeconds scaled = sequence.Current as WaitForSeconds;
                if (realtime != null) { coroutine.waitUntil = Seconds + realtime.waitTime; coroutine.waitRealtime = true; }
                else if (scaled != null) { coroutine.waitUntil = ScaledSeconds + scaled.seconds; coroutine.waitRealtime = false; }
                else coroutine.waitUntil = 0;
                coroutine.dependency = sequence.Current as Coroutine;
                return;
            }
        }
        public static void Tick(float seconds = .02f)
        {
            Time.unscaledDeltaTime = seconds; Time.deltaTime = seconds * Time.timeScale; Seconds += seconds; ScaledSeconds += Time.deltaTime;
            foreach (Coroutine c in Routines.ToArray())
            {
                if (c.Done || (c.dependency != null && !c.dependency.Done)) continue;
                if ((c.waitRealtime ? Seconds : ScaledSeconds) + 1e-8 < c.waitUntil) continue;
                c.dependency = null; Advance(c);
            }
            Routines.RemoveAll(c => c.Done);
            foreach(var i in invocations.Where(i=>i.due<=ScaledSeconds+1e-8).ToArray()){invocations.Remove(i);if(i.owner!=null&&i.owner.isActiveAndEnabled)Lifecycle.Invoke(i.owner,i.method);}
        }
    }
    public class WaitForSeconds { public float seconds; public WaitForSeconds(float seconds) { this.seconds = seconds; } }
    public class WaitForSecondsRealtime { public float waitTime; public WaitForSecondsRealtime(float seconds) { waitTime = seconds; } }
    public class Transform : Component, IEnumerable
    {
        public readonly List<Transform> Children = new List<Transform>();
        private Transform parentValue;
        public Transform parent { get { return parentValue; } set { SetParent(value, true); } }
        public Vector3 localPosition, localScale = Vector3.one;
        public Quaternion localRotation = Quaternion.identity;
        public Vector3 position { get { return parentValue == null ? localPosition : parentValue.TransformPoint(localPosition); } set { localPosition = parentValue == null ? value : parentValue.InverseTransformPoint(value); } }
        public Quaternion rotation { get { return parentValue == null ? localRotation : Quaternion.Euler(0, 0, parentValue.rotation.eulerAngles.z + localRotation.eulerAngles.z); } set { localRotation = parentValue == null ? value : Quaternion.Euler(0, 0, value.eulerAngles.z - parentValue.rotation.eulerAngles.z); } }
        public Vector3 eulerAngles { get { return rotation.eulerAngles; } set { rotation = Quaternion.Euler(value); } }
        public Vector3 localEulerAngles { get { return localRotation.eulerAngles; } set { localRotation = Quaternion.Euler(value); } }
        public Vector3 lossyScale { get { return parentValue == null ? localScale : Vector3.Scale(parentValue.lossyScale, localScale); } }
        public Vector3 right { get { return rotation * Vector3.right; } }
        public Vector3 up { get { return rotation * Vector3.up; } }
        public int childCount { get { return Children.Count; } }
        public void SetParent(Transform next, bool worldPositionStays = true)
        {
            Vector3 previous = position; Quaternion previousRotation = rotation;
            if (parentValue != null) parentValue.Children.Remove(this); parentValue = next;
            if (next != null && !next.Children.Contains(this)) next.Children.Add(this);
            if (worldPositionStays) { position = previous; rotation = previousRotation; }
        }
        public bool IsChildOf(Transform t) { for(Transform p=this;p!=null;p=p.parent)if(p==t)return true;return false; }
        public Transform GetChild(int index) { return Children[index]; }
        public Transform Find(string name) { return Children.FirstOrDefault(c => c.gameObject.name == name); }
        public void SetAsFirstSibling() { if (parentValue != null) { parentValue.Children.Remove(this); parentValue.Children.Insert(0, this); } }
        public void SetAsLastSibling() { if (parentValue != null) { parentValue.Children.Remove(this); parentValue.Children.Add(this); } }
        public Vector3 TransformPoint(Vector3 point) { return position + rotation * Vector3.Scale(point, lossyScale); }
        public Vector3 InverseTransformPoint(Vector3 point) { Vector3 p = Quaternion.Euler(0, 0, -rotation.eulerAngles.z) * (point - position); Vector3 s = lossyScale; return new Vector3(p.x / s.x, p.y / s.y, p.z / s.z); }
        public void Rotate(float x, float y, float z) { localRotation = Quaternion.Euler(0, 0, localRotation.eulerAngles.z + z); }
        public IEnumerator GetEnumerator() { return Children.GetEnumerator(); }
    }
    public class RectTransform : Transform
    {
        public Vector2 pivot, sizeDelta, anchoredPosition, anchorMin, anchorMax, offsetMin, offsetMax;
        public Rect rect { get { return new Rect(0, 0, sizeDelta.x == 0 ? 1920 : sizeDelta.x, sizeDelta.y == 0 ? 1080 : sizeDelta.y); } }
    }
    public struct Vector2
    {
        public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(); } } public static Vector2 one { get { return new Vector2(1, 1); } }
        public static Vector2 up { get { return new Vector2(0, 1); } } public static Vector2 right { get { return new Vector2(1, 0); } }
        public static Vector2 down { get { return new Vector2(0, -1); } } public static Vector2 left { get { return new Vector2(-1, 0); } }
        public float sqrMagnitude { get { return x * x + y * y; } } public float magnitude { get { return (float)Math.Sqrt(sqrMagnitude); } }
        public Vector2 normalized { get { return magnitude > 1e-8 ? this / magnitude : zero; } }
        public void Normalize() { this = normalized; }
        public static implicit operator Vector3(Vector2 value) { return new Vector3(value.x, value.y, 0); }
        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); } public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        public static Vector2 operator -(Vector2 a) { return new Vector2(-a.x, -a.y); } public static Vector2 operator *(Vector2 a, float b) { return new Vector2(a.x * b, a.y * b); } public static Vector2 operator *(float b, Vector2 a) { return a * b; } public static Vector2 operator /(Vector2 a, float b) { return new Vector2(a.x / b, a.y / b); }
        public static float Distance(Vector2 a, Vector2 b) { return (a - b).magnitude; } public static float Dot(Vector2 a, Vector2 b) { return a.x * b.x + a.y * b.y; }
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) { return a + (b - a) * Mathf.Clamp01(t); }
        public static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDistanceDelta) { Vector2 delta = target - current; return delta.magnitude <= maxDistanceDelta ? target : current + delta.normalized * maxDistanceDelta; }
        public static Vector2 ClampMagnitude(Vector2 value, float max) { return value.magnitude > max ? value.normalized * max : value; }
        public override string ToString() { return string.Format("({0},{1})", x, y); }
    }
    public struct Vector3
    {
        public static bool operator ==(Vector3 a,Vector3 b){return (a-b).sqrMagnitude<1e-10f;} public static bool operator !=(Vector3 a,Vector3 b){return !(a==b);} public override bool Equals(object o){return o is Vector3 && this==(Vector3)o;} public override int GetHashCode(){return x.GetHashCode()^y.GetHashCode()^z.GetHashCode();}
        public float x, y, z; public Vector3(float x, float y, float z = 0) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero { get { return new Vector3(); } } public static Vector3 one { get { return new Vector3(1, 1, 1); } }
        public static Vector3 right { get { return new Vector3(1, 0, 0); } } public static Vector3 up { get { return new Vector3(0, 1, 0); } }
        public float sqrMagnitude { get { return x * x + y * y + z * z; } } public float magnitude { get { return (float)Math.Sqrt(sqrMagnitude); } } public Vector3 normalized { get { return magnitude > 1e-8 ? this / magnitude : zero; } }
        public static implicit operator Vector2(Vector3 value) { return new Vector2(value.x, value.y); }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); } public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); } public static Vector3 operator -(Vector3 a) { return new Vector3(-a.x, -a.y, -a.z); }
        public static Vector3 operator *(Vector3 a, float b) { return new Vector3(a.x * b, a.y * b, a.z * b); } public static Vector3 operator *(float b, Vector3 a) { return a * b; } public static Vector3 operator /(Vector3 a, float b) { return new Vector3(a.x / b, a.y / b, a.z / b); }
        public static float Distance(Vector3 a, Vector3 b) { return (a - b).magnitude; } public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { return a + (b - a) * Mathf.Clamp01(t); }
        public static Vector3 Scale(Vector3 a, Vector3 b) { return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z); }
    }
    public struct Vector4 { public float x, y, z, w; public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; } public static Vector4 zero { get { return new Vector4(); } } }
    public struct Quaternion
    {
        private Vector3 euler;
        public Vector3 eulerAngles { get { return euler; } }
        public static Quaternion identity { get { return Euler(0, 0, 0); } }
        public static Quaternion Euler(float x, float y, float z) { return new Quaternion { euler = new Vector3(x, y, z) }; }
        public static Quaternion Euler(Vector3 values) { return Euler(values.x, values.y, values.z); }
        public static Quaternion AngleAxis(float angle, Vector3 axis) { return Euler(axis.x * angle, axis.y * angle, axis.z * angle); }
        public static Vector3 operator *(Quaternion rotation, Vector3 value) { double angle = rotation.euler.z * Math.PI / 180; return new Vector3((float)(value.x * Math.Cos(angle) - value.y * Math.Sin(angle)), (float)(value.x * Math.Sin(angle) + value.y * Math.Cos(angle)), value.z); }
    }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height) { this.x = x; this.y = y; this.width = width; this.height = height; }
    }
    public struct Bounds
    {
        public Vector3 center, size; public Bounds(Vector3 center, Vector3 size) { this.center = center; this.size = size; }
        public Vector3 extents { get { return size * .5f; } } public Vector3 min { get { return center - extents; } } public Vector3 max { get { return center + extents; } }
    }
    public struct Color32 { public byte r,g,b,a; public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;} public static implicit operator Color32(Color c) { return new Color32((byte)(c.r*255),(byte)(c.g*255),(byte)(c.b*255),(byte)(c.a*255)); } }
    public struct Color
    {
        public float maxColorComponent { get { return Math.Max(r,Math.Max(g,b)); } }
        public float r, g, b, a; public Color(float r, float g, float b, float a = 1) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white { get { return new Color(1, 1, 1); } } public static Color black { get { return new Color(0, 0, 0); } } public static Color clear { get { return new Color(0, 0, 0, 0); } }
        public static Color blue { get { return new Color(0, 0, 1); } } public static Color red { get { return new Color(1, 0, 0); } } public static Color yellow { get { return new Color(1, 1, 0); } }
        public static Color cyan { get { return new Color(0, 1, 1); } } public static Color green { get { return new Color(0, 1, 0); } } public static Color magenta { get { return new Color(1, 0, 1); } }
        public static Color Lerp(Color a, Color b, float t) { t = Mathf.Clamp01(t); return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t); }
        public static Color operator *(Color value, float factor) { return new Color(value.r * factor, value.g * factor, value.b * factor, value.a * factor); }
        public static Color operator *(Color a, Color b) { return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a); }
    }
    public static class Mathf
    {
        public const float PI = (float)Math.PI, Deg2Rad = PI / 180, Rad2Deg = 180 / PI, Infinity = float.PositiveInfinity, Epsilon = float.Epsilon;
        public static float Abs(float value) { return Math.Abs(value); } public static int Abs(int value) { return Math.Abs(value); }
        public static float Min(float a, float b) { return Math.Min(a, b); } public static int Min(int a, int b) { return Math.Min(a, b); }
        public static float Max(float a, float b) { return Math.Max(a, b); } public static int Max(int a, int b) { return Math.Max(a, b); }
        public static float Clamp(float value, float min, float max) { return Math.Max(min, Math.Min(max, value)); } public static int Clamp(int value, int min, int max) { return Math.Max(min, Math.Min(max, value)); }
        public static float Clamp01(float value) { return Clamp(value, 0, 1); } public static float Lerp(float a, float b, float t) { return a + (b - a) * Clamp01(t); } public static float LerpUnclamped(float a, float b, float t) { return a + (b - a) * t; }
        public static int FloorToInt(float value) { return (int)Math.Floor(value); } public static int CeilToInt(float value) { return (int)Math.Ceiling(value); } public static int RoundToInt(float value) { return (int)Math.Round(value); }
        public static float Floor(float value) { return (float)Math.Floor(value); } public static float Ceil(float value) { return (float)Math.Ceiling(value); }
        public static float Sqrt(float value) { return (float)Math.Sqrt(value); } public static float Pow(float value, float power) { return (float)Math.Pow(value, power); }
        public static float Exp(float value) { return (float)Math.Exp(value); }
        public static float Sin(float value) { return (float)Math.Sin(value); } public static float Cos(float value) { return (float)Math.Cos(value); } public static float Atan2(float y, float x) { return (float)Math.Atan2(y, x); }
        public static float Sign(float value) { return Math.Sign(value); } public static float Repeat(float value, float length) { return value - (float)Math.Floor(value / length) * length; }
        public static float MoveTowards(float value, float target, float maxDelta) { return Abs(target - value) <= maxDelta ? target : value + Sign(target - value) * maxDelta; }
        public static float SmoothStep(float a, float b, float t) { t = Clamp01(t); return a + (b - a) * (t * t * (3 - 2 * t)); }
        public static bool Approximately(float a, float b) { return Abs(a - b) <= Max(1e-6f * Max(Abs(a), Abs(b)), 1e-7f); }
    }
    public static class Random
    {
        private static System.Random random = new System.Random(195);
        public static void InitState(int seed) { random = new System.Random(seed); }
        public static int Range(int min, int max) { return random.Next(min, max); }
        public static float Range(float min, float max) { return min + (float)random.NextDouble() * (max - min); }
        public static float value { get { return (float)random.NextDouble(); } }
        public static Vector2 insideUnitCircle { get { float a = Range(0, Mathf.PI * 2), r = (float)Math.Sqrt(value); return new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r); } }
    }
    public static class Time
    {
        public static float timeScale = 1, deltaTime = .02f, fixedDeltaTime = .02f, unscaledDeltaTime = .02f, captureDeltaTime;
        public static float time { get { return (float)Scheduler.ScaledSeconds; } } public static float unscaledTime { get { return (float)Scheduler.Seconds; } } public static float realtimeSinceStartup { get { return (float)Scheduler.Seconds; } }
    }
    public static class Application { public static bool isPlaying = true; public static string persistentDataPath; public static void OpenURL(string value) { } }
    public static class Debug
    {
        public static readonly List<string> Errors = new List<string>(), Warnings = new List<string>(), Messages = new List<string>();
        public static void Log(object value, Object context = null) { Messages.Add(Convert.ToString(value)); }
        public static void LogWarning(object value, Object context = null) { Warnings.Add(Convert.ToString(value)); }
        public static void LogError(object value, Object context = null) { Errors.Add(Convert.ToString(value)); }
    }
    public class Camera : Behaviour { public static Camera main { get { return Object.FindAnyObjectByType<Camera>(); } } public bool orthographic; public float orthographicSize = 8.3f; public Color backgroundColor; public CameraClearFlags clearFlags; public float aspect = 16f / 9; }
    public enum CameraClearFlags { SolidColor }
    public class Texture : Object { }
    public enum TextureFormat { RGBA32, ARGB32 }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureWrapMode { Repeat, Clamp }
    public class Texture2D : Texture
    {
        public int width, height; public FilterMode filterMode; public TextureWrapMode wrapMode;
        public Texture2D(int width, int height) { this.width = width; this.height = height; }
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) : this(width, height) { }
        public void SetPixels32(Color32[] colors) {}
        public void SetPixel(int x, int y, Color color) { } public void SetPixels(Color[] colors) { } public void Apply(bool updateMipmaps = true, bool makeNoLongerReadable = false) { }
        public static Texture2D whiteTexture = new Texture2D(1, 1);
    }
    public enum SpriteMeshType { FullRect, Tight }
    public class Sprite : Object
    {
        public Bounds bounds = new Bounds(Vector3.zero, Vector3.one); public Rect rect = new Rect(0, 0, 100, 100); public float pixelsPerUnit = 100; public Texture2D texture;
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit = 100, uint extrude = 0, SpriteMeshType meshType = SpriteMeshType.Tight)
        { return new Sprite { texture = texture, rect = rect, pixelsPerUnit = pixelsPerUnit, bounds = new Bounds(Vector3.zero, new Vector3(rect.width / pixelsPerUnit, rect.height / pixelsPerUnit, 0)) }; }
    }
    public class Shader : Object { public static Shader Find(string name) { return new Shader { name = name }; } }
    public class Material : Object
    {
        public Shader shader; public Color color; public Texture mainTexture;
        public Material(Shader shader) { this.shader = shader; }
        public Material(Material source) { shader = source.shader; color = source.color; mainTexture = source.mainTexture; }
        public void SetColor(string property, Color value) { color = value; }
    }
    public class Renderer : Behaviour
    {
        public int sortingLayerID, sortingOrder; public Material sharedMaterial; public Material material { get { return sharedMaterial; } set { sharedMaterial = value; } }
        public virtual Bounds bounds { get { return new Bounds(transform.position, transform.lossyScale); } }
    }
    public class SpriteRenderer : Renderer
    {
        public Sprite sprite; public Color color = Color.white; public bool flipX, flipY;
        public override Bounds bounds { get { return new Bounds(transform.position, sprite == null ? Vector3.zero : Vector3.Scale(sprite.bounds.size, transform.lossyScale)); } }
    }
    public class Mesh : Object
    {
        public Vector3[] vertices; public Vector2[] uv; public int[] triangles; public Color[] colors;
        public void RecalculateNormals() { } public void RecalculateBounds() { }
        public void Clear() { vertices = new Vector3[0]; triangles = new int[0]; }
        public void SetVertices(List<Vector3> values) { vertices = values.ToArray(); } public void SetTriangles(List<int> values, int submesh) { triangles = values.ToArray(); }
    }
    public class MeshFilter : Component { public Mesh sharedMesh; public Mesh mesh { get { return sharedMesh; } set { sharedMesh = value; } } }
    public class MeshRenderer : Renderer { }
    public class LineRenderer : Renderer { public int positionCount; public float startWidth, endWidth, widthMultiplier; public Color startColor, endColor; public bool useWorldSpace, loop; public void SetPositions(Vector3[] values) { positionCount = values.Length; } public void SetPosition(int index, Vector3 value) { } }
    public class TrailRenderer : Renderer { public float time, minVertexDistance, startWidth, endWidth, widthMultiplier; public Color startColor, endColor; public bool emitting; public void Clear() { } }
    public class PhysicsMaterial2D : Object { public float friction, bounciness; public PhysicsMaterial2D() { } public PhysicsMaterial2D(string name) { this.name = name; } }
    public enum RigidbodyType2D { Dynamic, Kinematic, Static }
    public enum RigidbodyInterpolation2D { None, Interpolate, Extrapolate }
    public enum RigidbodySleepMode2D { NeverSleep, StartAwake, StartAsleep }
    public enum CollisionDetectionMode2D { Discrete, Continuous }
    [Flags] public enum RigidbodyConstraints2D { None = 0, FreezePositionX = 1, FreezePositionY = 2, FreezeRotation = 4, FreezeAll = 7 }
    public enum ForceMode2D { Force, Impulse }
    public class Rigidbody2D : Component
    {
        public Vector2 linearVelocity; public float angularVelocity, gravityScale, linearDamping, angularDamping, mass = 1;
        public bool simulated = true; public RigidbodyType2D bodyType; public RigidbodyInterpolation2D interpolation; public RigidbodySleepMode2D sleepMode; public CollisionDetectionMode2D collisionDetectionMode; public RigidbodyConstraints2D constraints; public PhysicsMaterial2D sharedMaterial;
        public Vector2 position { get { return transform.position; } set { transform.position = new Vector3(value.x, value.y, transform.position.z); } }
        public float rotation { get { return transform.rotation.eulerAngles.z; } set { transform.rotation = Quaternion.Euler(0, 0, value); } }
        public void MovePosition(Vector2 position) { this.position = position; } public void MoveRotation(float rotation) { this.rotation = rotation; }
        public void AddForce(Vector2 force, ForceMode2D mode = ForceMode2D.Force) { linearVelocity += force / mass * (mode == ForceMode2D.Impulse ? 1 : Time.fixedDeltaTime); }
        public void WakeUp() { } public bool IsSleeping() { return false; }
    }
    public class Collider2D : Behaviour
    {
        public bool isTrigger; public Vector2 offset; public PhysicsMaterial2D sharedMaterial;
        public Rigidbody2D attachedRigidbody { get { return GetComponentInParent<Rigidbody2D>(); } }
        public virtual Bounds bounds { get { return new Bounds(transform.position + (Vector3)offset, transform.lossyScale); } }
        public Vector2 ClosestPoint(Vector2 point) { return transform.position; }
    }
    public class CircleCollider2D : Collider2D { public float radius = .5f; public override Bounds bounds { get { return new Bounds(transform.position + (Vector3)offset, Vector3.Scale(new Vector3(radius * 2, radius * 2, 0), transform.lossyScale)); } } }
    public class BoxCollider2D : Collider2D { public Vector2 size = Vector2.one; public float edgeRadius; public override Bounds bounds { get { return new Bounds(transform.position + (Vector3)offset, Vector3.Scale(size, transform.lossyScale)); } } }
    public class EdgeCollider2D : Collider2D { public Vector2[] points; public float edgeRadius; }
    public class PolygonCollider2D : Collider2D { public Vector2[] points; public int pathCount; public void SetPath(int index, Vector2[] points) { this.points = points; } }
    public class Collision2D { public Collider2D collider; public Rigidbody2D rigidbody; public Vector2 relativeVelocity; public GameObject gameObject { get { return collider.gameObject; } } }
    public struct RaycastHit2D { public Collider2D collider; public Vector2 normal, point; public float distance; public static implicit operator bool(RaycastHit2D value) { return value.collider != null; } }
    public static class Physics2D
    {
        public static Vector2 gravity;
        public static void IgnoreCollision(Collider2D first, Collider2D second, bool ignore = true) { }
        public static void IgnoreLayerCollision(int first, int second, bool ignore = true) { }
        public static void SyncTransforms() { }
        public static void Simulate(float dt)
        {
            foreach (Rigidbody2D body in Object.FindObjectsByType<Rigidbody2D>()) if (body.simulated && body.bodyType == RigidbodyType2D.Dynamic)
            { body.position += body.linearVelocity * dt; body.rotation += body.angularVelocity * dt; }
        }
    }
    public static class Resources { public static T Load<T>(string path) where T : Object { return null; } }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public class Canvas : Behaviour { public bool overrideSorting; public RenderMode renderMode; public int sortingOrder; public float scaleFactor = 1; public static void ForceUpdateCanvases() { } }
    public class CanvasGroup : Behaviour { public float alpha = 1; public bool interactable, blocksRaycasts; }
    public class AudioClip : Object { }
    public class AudioListener : Behaviour { }
    public class AudioSource : Behaviour { public bool playOnAwake, loop, ignoreListenerPause; public float spatialBlend, volume = 1; public AudioClip clip; public void PlayOneShot(AudioClip clip, float volumeScale = 1) { } public void Play() { } public void Stop() { } }
    public static class JsonUtility
    {
        public static string ToJson(object value, bool prettyPrint = false)
        { var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(value.GetType()); using (var stream = new System.IO.MemoryStream()) { serializer.WriteObject(stream, value); return System.Text.Encoding.UTF8.GetString(stream.ToArray()); } }
        public static T FromJson<T>(string json)
        { var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T)); using (var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json))) return (T)serializer.ReadObject(stream); }
    }
}
namespace UnityEngine.SceneManagement { public struct Scene { public bool isLoaded {get{return true;}} public bool IsValid() { return true; } public UnityEngine.GameObject[] GetRootGameObjects() { return UnityEngine.GameObject.All.Where(g => g.transform.parent == null && !g.destroyed).ToArray(); } } }
namespace UnityEngine.UI
{
    public class Button : UnityEngine.Behaviour { public ClickEvent onClick = new ClickEvent(); public class ClickEvent { public void RemoveAllListeners() { } public void AddListener(Action callback) { } } }
    public class GraphicRaycaster : UnityEngine.Behaviour { }
    public class Graphic : UnityEngine.Behaviour { public UnityEngine.Color color; public bool raycastTarget; public UnityEngine.RectTransform rectTransform { get { return transform as UnityEngine.RectTransform; } } }
    public class Image : Graphic { public UnityEngine.Sprite sprite; public bool preserveAspect; public enum Type { Simple, Sliced, Filled } public enum FillMethod { Horizontal, Vertical, Radial90, Radial180, Radial360 } public Type type; public FillMethod fillMethod; public float fillAmount; public int fillOrigin; }
    public class RawImage : Graphic { public UnityEngine.Texture texture; }
    public class CanvasScaler : UnityEngine.Behaviour { public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize } public enum ScreenMatchMode { MatchWidthOrHeight, Expand, Shrink } public ScaleMode uiScaleMode; public ScreenMatchMode screenMatchMode; public UnityEngine.Vector2 referenceResolution; public float matchWidthOrHeight; }
}
namespace TMPro
{
    public class TMP_FontAsset : UnityEngine.ScriptableObject { }
    public static class TMP_Settings { public static TMP_FontAsset defaultFontAsset = new TMP_FontAsset(); }
    public enum FontStyles { Normal, Bold, Italic }
    public enum TextAlignmentOptions { Center, Left, Right, TopLeft, Top, TopRight, MidlineLeft, Midline, MidlineRight, BottomLeft, Bottom, BottomRight }
    public enum TextOverflowModes { Overflow, Ellipsis, Truncate, Masking }
    public enum TextWrappingModes { Normal, NoWrap }
    public class TMP_Text : UnityEngine.Behaviour
    {
        public string text; public TMP_FontAsset font; public float fontSize, fontSizeMin, fontSizeMax, characterSpacing, outlineWidth;
        public int maxVisibleLines; public bool enableAutoSizing, richText, raycastTarget; public FontStyles fontStyle;
        public TextAlignmentOptions alignment; public TextOverflowModes overflowMode; public TextWrappingModes textWrappingMode;
        public UnityEngine.Color color, outlineColor; public UnityEngine.Vector4 margin;
        public UnityEngine.RectTransform rectTransform { get { return transform as UnityEngine.RectTransform; } }
        public void SetText(string value) { text = value; }
    }
    public class TextMeshProUGUI : TMP_Text { }
    public class TextMeshPro : TMP_Text { public int sortingOrder; }
}
