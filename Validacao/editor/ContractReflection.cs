using System;
using System.IO;
using System.Reflection;

public static class ContractReflection
{
    static int checks;
    static string Shape(Type t){if(!t.IsGenericType)return t.FullName;return t.GetGenericTypeDefinition().FullName+"<"+string.Join(",",Array.ConvertAll(t.GetGenericArguments(),Shape))+">";}
    static void Check(bool value,string text){checks++;if(!value)throw new Exception(text);}
    public static void Main()
    {
        var actual=Assembly.LoadFrom("/workspace/work/pokemon-marble-race/validation/integration/AllRuntime.dll");
        var test=Assembly.LoadFrom("/workspace/work/pokemon-marble-race/validation/editor/Harness.exe");
        Type manager=actual.GetType("PokemonMarbleRace.MarbleRaceManager",true);
        Type stub=test.GetType("PokemonMarbleRace.MarbleRaceManager",true);
        foreach(string name in new[]{"nomeCampeonato","campeonatoId","temporadaId","tema","fundo","spriteBola","spriteSnorlax","spriteRocket","formato","quantidadeParticipantes","quantidadeCorridas","semente","participantes","geracoesPermitidas","tiposPermitidos","duracaoAlvo","duracaoMaxima","duracaoAbertura","duracaoResultado","intervaloEntreCorridas","umaCorridaPorPlay","poderesPorTipo","tamanhoMarble","situacao","ultimoErro"})
        {
            var a=manager.GetField(name,BindingFlags.Public|BindingFlags.Instance);
            var b=stub.GetField(name,BindingFlags.Public|BindingFlags.Instance);
            Check(a!=null&&b!=null,"field exists "+name);
            Check(Shape(a.FieldType)==Shape(b.FieldType),"field type "+name);
        }
        foreach(string name in new[]{"PastaDaTemporada","Preparada","CampeonatoConcluido"})
        {
            var a=manager.GetProperty(name,BindingFlags.Public|BindingFlags.Instance);
            Check(a!=null&&a.GetGetMethod()!=null,"public readable property "+name);
            Check(a.PropertyType== (name=="PastaDaTemporada"?typeof(string):typeof(bool)),"property type "+name);
        }
        foreach(string name in new[]{"ValidarConfiguracao","ContinuarNoPlay"})
        {
            var a=manager.GetMethod(name,BindingFlags.Public|BindingFlags.Instance);
            Check(a!=null&&a.GetParameters().Length==0,"public parameterless method "+name);
            Check(a.ReturnType==(name=="ValidarConfiguracao"?typeof(bool):typeof(void)),"return type "+name);
        }
        Type format=actual.GetType("PokemonMarbleRace.RaceFormat",true);
        string[] names={"SingleRace","MiniGP","BestOfThree","Knockout"};
        for(int i=0;i<names.Length;i++)Check(Convert.ToInt32(Enum.Parse(format,names[i]))==i,"enum serialized value "+names[i]);
        Console.WriteLine("PASS "+checks+" checks: editor contract matches actual compiled MarbleRaceManager public fields, properties, methods and format values.");
        Console.WriteLine("Scope: reflection over real source compiled with runtime Unity stubs, not Unity Editor execution.");
    }
}
