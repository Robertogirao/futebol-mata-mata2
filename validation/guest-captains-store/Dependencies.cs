using System;
namespace UnityEngine
{
    public class Object { public string name; }
    public class ScriptableObject : Object { public static T CreateInstance<T>() where T : new() { return new T(); } }
    public class Sprite : Object { }
    [AttributeUsage(AttributeTargets.Class)] public class CreateAssetMenuAttribute : Attribute
    { public string fileName, menuName; public int order; }
    [AttributeUsage(AttributeTargets.Field)] public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    [AttributeUsage(AttributeTargets.Field)] public class TooltipAttribute : Attribute { public TooltipAttribute(string text) { } }
}
public class TeamData : UnityEngine.ScriptableObject { }
public class JogadorDados { public string nomeJogador; public UnityEngine.Sprite foto512x512; }
