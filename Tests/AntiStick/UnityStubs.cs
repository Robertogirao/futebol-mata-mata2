using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.All)] public sealed class RequireComponent : Attribute { public RequireComponent(Type t) {} }
    [AttributeUsage(AttributeTargets.All)] public sealed class DisallowMultipleComponent : Attribute {}
    [AttributeUsage(AttributeTargets.All)] public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string t) {} }
    [AttributeUsage(AttributeTargets.All)] public sealed class MinAttribute : Attribute { public MinAttribute(float t) {} }
    [AttributeUsage(AttributeTargets.All)] public sealed class RangeAttribute : Attribute { public RangeAttribute(float a,float b) {} }
    [AttributeUsage(AttributeTargets.All)] public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string t) {} }
    [AttributeUsage(AttributeTargets.All)] public sealed class InspectorNameAttribute : Attribute { public InspectorNameAttribute(string t) {} }
    public class Object
    {
        public bool destroyed;
        public static bool operator ==(Object a,Object b)
        {
            bool an=ReferenceEquals(a,null)||(!ReferenceEquals(a,null)&&a.destroyed);
            bool bn=ReferenceEquals(b,null)||(!ReferenceEquals(b,null)&&b.destroyed);
            return an||bn ? an==bn : ReferenceEquals(a,b);
        }
        public static bool operator !=(Object a,Object b)=>!(a==b);
        public override bool Equals(object value)=>ReferenceEquals(this,value);
        public override int GetHashCode()=>RuntimeHelpers.GetHashCode(this);
    }
    public class GameObject : Object
    {
        public bool activeSelf=true;
        public bool activeInHierarchy => activeSelf && (transform.parent==null||transform.parent.gameObject.activeInHierarchy);
        public Transform transform;
        private readonly List<Component> components=new List<Component>();
        public GameObject() { transform=new Transform { gameObject=this }; components.Add(transform); }
        public T AddComponent<T>() where T:Component,new() { var c=new T { gameObject=this }; components.Add(c); return c; }
        public T GetComponent<T>() where T:class => components.OfType<T>().FirstOrDefault();
        public T GetComponentInParent<T>() where T:class => GetComponent<T>()??transform.parent?.gameObject.GetComponentInParent<T>();
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T:class
        {
            var all=new List<T>();
            if(includeInactive||activeInHierarchy) all.AddRange(components.OfType<T>());
            foreach(var child in transform.children) all.AddRange(child.gameObject.GetComponentsInChildren<T>(includeInactive));
            return all.ToArray();
        }
    }
    public class Component:Object
    {
        public GameObject gameObject;
        public Transform transform=>gameObject.transform;
        public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>();
        public T GetComponentInParent<T>() where T:class=>gameObject.GetComponentInParent<T>();
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T:class=>gameObject.GetComponentsInChildren<T>(includeInactive);
    }
    public class Behaviour:Component { public bool enabled=true; public bool isActiveAndEnabled=>enabled&&gameObject.activeInHierarchy; }
    public class MonoBehaviour:Behaviour {}
    public class Transform:Component
    {
        public Transform parent;
        public readonly List<Transform> children=new List<Transform>();
        public Vector3 localPosition;
        public Vector3 localScale=new Vector3(1,1,1);
        public float localRotation;
        public float rotation => localRotation+(parent?.rotation??0f);
        public Vector3 position { get=>parent==null?localPosition:parent.TransformPoint(localPosition); set=>localPosition=parent==null?value:parent.InverseTransformPoint(value); }
        public Vector3 right { get { float a=rotation*MathF.PI/180;return new Vector3(MathF.Cos(a),MathF.Sin(a),0); } }
        public Vector3 TransformPoint(Vector3 p)
        {
            float a=localRotation*MathF.PI/180,c=MathF.Cos(a),s=MathF.Sin(a);
            var scaled=new Vector3(p.x*localScale.x,p.y*localScale.y,p.z*localScale.z);
            var v=new Vector3(c*scaled.x-s*scaled.y+localPosition.x,s*scaled.x+c*scaled.y+localPosition.y,scaled.z+localPosition.z);
            return parent==null?v:parent.TransformPoint(v);
        }
        public Vector3 InverseTransformPoint(Vector3 p)
        {
            if(parent!=null)p=parent.InverseTransformPoint(p);
            p=p-localPosition;
            float a=-localRotation*MathF.PI/180,c=MathF.Cos(a),s=MathF.Sin(a);
            return new Vector3((c*p.x-s*p.y)/localScale.x,(s*p.x+c*p.y)/localScale.y,p.z/localScale.z);
        }
        public void SetParent(Transform p) { parent=p;p?.children.Add(this); }
    }
    public struct Vector2
    {
        public float x,y;
        public Vector2(float x,float y){this.x=x;this.y=y;}
        public static Vector2 zero=>new Vector2(); public static Vector2 up=>new Vector2(0,1);
        public float sqrMagnitude=>x*x+y*y;
        public float magnitude=>MathF.Sqrt(sqrMagnitude);
        public void Normalize(){float m=magnitude;if(m>0){x/=m;y/=m;}}
        public static float Dot(Vector2 a,Vector2 b)=>a.x*b.x+a.y*b.y;
        public static float Distance(Vector2 a,Vector2 b)=>(a-b).magnitude;
        public static Vector2 operator +(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);
        public static Vector2 operator -(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);
        public static Vector2 operator -(Vector2 a)=>new Vector2(-a.x,-a.y);
        public static Vector2 operator *(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b);
        public static Vector2 operator /(Vector2 a,float b)=>new Vector2(a.x/b,a.y/b);
        public static implicit operator Vector3(Vector2 a)=>new Vector3(a.x,a.y,0);
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;}
        public static implicit operator Vector2(Vector3 a)=>new Vector2(a.x,a.y);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
    }
    public static class Mathf
    {
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Abs(float a)=>Math.Abs(a);
        public static float Clamp(float v,float min,float max)=>Math.Clamp(v,min,max);
    }
    public static class Time { public static float time,unscaledTime;public static float fixedDeltaTime=.02f,timeScale=1f; }
    public enum RigidbodyType2D { Dynamic, Kinematic, Static }
    public enum ForceMode2D { Impulse }
    public class Rigidbody2D:Component
    {
        public RigidbodyType2D bodyType=RigidbodyType2D.Dynamic;
        public float mass=1,angularVelocity;
        public Vector2 linearVelocity;
        public Vector2 position { get=>transform.position;set=>transform.position=value; }
        public int forceCount,wakes;
        public Vector2 lastForce;
        public Vector2 GetPointVelocity(Vector2 p)
        {
            var r=p-position;float w=angularVelocity*MathF.PI/180;
            return linearVelocity+new Vector2(-r.y*w,r.x*w);
        }
        public void WakeUp(){wakes++;}
        public void AddForce(Vector2 force,ForceMode2D mode){forceCount++;lastForce=force;linearVelocity+=force/mass;}
    }
    public struct ColliderDistance2D { public bool isValid,isOverlapped; public float distance; }
    public class Collider2D:Behaviour
    {
        private static int next; public int id=++next;
        public bool isTrigger;
        public Rigidbody2D attachedRigidbody=>GetComponentInParent<Rigidbody2D>();
        public ColliderDistance2D Distance(Collider2D other)=>Physics2D.Distance(this,other);
    }
    public struct ContactPoint2D { public Vector2 normal; }
    public class Collision2D
    {
        public Collider2D collider; public Vector2 normal=new Vector2(1,0); public int contactCount=1;
        public ContactPoint2D GetContact(int n)=>new ContactPoint2D { normal=normal };
    }
    public static class Physics2D
    {
        public static int ignoreTrueCalls,ignoreFalseCalls;
        public static readonly Dictionary<(int,int),bool> ignores=new Dictionary<(int,int),bool>();
        public static readonly Dictionary<(int,int),ColliderDistance2D> distances=new Dictionary<(int,int),ColliderDistance2D>();
        private static (int,int) Key(Collider2D a,Collider2D b)=>a.id<b.id?(a.id,b.id):(b.id,a.id);
        public static bool GetIgnoreCollision(Collider2D a,Collider2D b)=>ignores.TryGetValue(Key(a,b),out bool v)&&v;
        public static void IgnoreCollision(Collider2D a,Collider2D b,bool ignore)
        {
            if(!a.enabled||!b.enabled||!a.gameObject.activeInHierarchy||!b.gameObject.activeInHierarchy)throw new Exception("Ignored pair API requires active colliders");
            ignores[Key(a,b)]=ignore;if(ignore)ignoreTrueCalls++;else ignoreFalseCalls++;
        }
        public static ColliderDistance2D Distance(Collider2D a,Collider2D b)=>distances.TryGetValue(Key(a,b),out var d)?d:new ColliderDistance2D { isValid=true,distance=.01f };
        public static void SetDistance(Collider2D a,Collider2D b,float distance,bool valid=true)=>distances[Key(a,b)]=new ColliderDistance2D { isValid=valid,distance=distance,isOverlapped=distance<0 };
        public static void Disable(Collider2D c){c.enabled=false;foreach(var k in ignores.Keys.ToArray())if(k.Item1==c.id||k.Item2==c.id)ignores[k]=false;}
        public static void Reset(){ignoreTrueCalls=ignoreFalseCalls=0;ignores.Clear();distances.Clear();}
    }
}
public class BallController:UnityEngine.MonoBehaviour { public bool jaMarcouGol; }
public class RotatingKicker:UnityEngine.MonoBehaviour {}
