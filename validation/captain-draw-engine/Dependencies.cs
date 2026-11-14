using System;
namespace UnityEngine
{
    public class Object { public string name; }
    public class ScriptableObject : Object { public static T CreateInstance<T>() where T : new() { return new T(); } }
    public class Sprite : Object { }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(); } }
        public float magnitude { get { return (float)Math.Sqrt(x * x + y * y); } }
        public Vector2 normalized { get { float d = magnitude; return d > .000001f ? this / d : zero; } }
        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        public static Vector2 operator *(Vector2 a, float b) { return new Vector2(a.x * b, a.y * b); }
        public static Vector2 operator /(Vector2 a, float b) { return new Vector2(a.x / b, a.y / b); }
    }
    [AttributeUsage(AttributeTargets.Class)] public class CreateAssetMenuAttribute : Attribute
    { public string fileName, menuName; public int order; }
    [AttributeUsage(AttributeTargets.Field)] public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    [AttributeUsage(AttributeTargets.Field)] public class TooltipAttribute : Attribute { public TooltipAttribute(string text) { } }
}
public class TeamData : UnityEngine.ScriptableObject { }
public class JogadorDados { public string nomeJogador; public UnityEngine.Sprite foto512x512; }
