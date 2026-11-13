using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using FutebolDeDiscos;
using TMPro;
using UnityEngine;

public static class Harness
{
    private static int checks;
    private static readonly string DirectoryPath = AppDomain.CurrentDomain.BaseDirectory;
    private static void Check(bool condition, string name)
    {
        checks++;
        if (!condition) throw new Exception("FAILED: " + name);
    }
    private static bool Near(float a, float b) { return Math.Abs(a-b) < .002f; }
    private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    private static bool Active(Component value) { return !value.destroyed && value.gameObject.activeInHierarchy; }
    private static List<UnityEngine.Object> Owned(CampoDiscos campo)
    { return ((List<UnityEngine.Object>)typeof(CampoDiscos).GetField("recursos", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(campo)).ToList(); }
    private static float DistanceToBox(Vector2 point, BoxCollider2D box)
    {
        Bounds bounds = box.bounds;
        float dx = Math.Max(0, Math.Abs(point.x-bounds.center.x)-Math.Abs(bounds.size.x)*.5f);
        float dy = Math.Max(0, Math.Abs(point.y-bounds.center.y)-Math.Abs(bounds.size.y)*.5f);
        return (float)Math.Sqrt(dx*dx+dy*dy);
    }
    private static float Clearance(Vector2 point, BoxCollider2D[] boxes, CircleCollider2D[] posts)
    {
        float distance = float.MaxValue;
        foreach (BoxCollider2D box in boxes) distance = Math.Min(distance,DistanceToBox(point,box));
        foreach (CircleCollider2D post in posts) distance = Math.Min(distance, Vector2.Distance(point,post.transform.position)-post.bounds.size.x*.5f);
        return distance;
    }
    private static void ValidateField(CampoDiscos campo, ProjetoFutebolDiscos config)
    {
        Check(Near(CampoDiscos.MeioComprimento,11) && Near(CampoDiscos.MeioLargura,5.2f) && Near(CampoDiscos.MeioGol,1.65f) && Near(CampoDiscos.ProfundidadeGol,1), "field constants match actors and camera");
        Check(campo.SpriteCirculo != null && Near(campo.SpriteCirculo.bounds.size.x,1) && Near(campo.SpriteCirculo.bounds.size.y,1), "actor circle is normalized one world unit");
        Check(campo.MaterialSprites != null && campo.MaterialFisica != null && Near(campo.MaterialFisica.friction,.025f) && Near(campo.MaterialFisica.bounciness,.88f), "owned shared unlit and bouncy physics material available");
        BoxCollider2D[] allBoxes = campo.GetComponentsInChildren<BoxCollider2D>(true).Where(Active).ToArray();
        BoxCollider2D[] walls = allBoxes.Where(x=>!x.isTrigger).ToArray();
        CircleCollider2D[] posts = campo.GetComponentsInChildren<CircleCollider2D>(true).Where(Active).ToArray();
        Check(allBoxes.Length == 14 && walls.Length == 12 && posts.Length == 4, "12 wall boxes plus two sensors and four round posts");
        Check(walls.All(x=>x.sharedMaterial==campo.MaterialFisica && x.attachedRigidbody==null), "walls are static physical colliders sharing field material");
        Check(posts.All(x=>x.sharedMaterial==campo.MaterialFisica && Near(x.bounds.size.x,.22f) && Near(x.bounds.size.y,.22f)), "posts have real normalized round colliders");
        Check(campo.GetComponentsInChildren<Rigidbody2D>(true).Length == 0, "stadium has no moving rigidbody or keeper");
        var goals = campo.GetComponentsInChildren<GolDiscos>(true).Where(Active).ToArray();
        Check(goals.Length == 2 && goals.Count(x=>x.esquerda)==1, "left and right scoring callbacks correctly identified");
        foreach (GolDiscos goal in goals)
        {
            BoxCollider2D sensor = goal.GetComponent<BoxCollider2D>();
            int side = goal.esquerda ? -1 : 1;
            Check(sensor!=null && sensor.isTrigger && Near(sensor.bounds.size.x,.54f) && Near(sensor.bounds.size.y,3.08f), "goal sensor has recessed trigger geometry");
            Check(Near(side*sensor.bounds.center.x-sensor.bounds.size.x*.5f,11.46f) && Near(side*sensor.bounds.center.x+sensor.bounds.size.x*.5f,12), "sensor front is11.46 and back12");
            // These are geometric clearance tests, not a substitute for native Box2D contacts.
            for (int lane=-1; lane<=1; lane++)
                for (float depth=9;depth<11.70f;depth+=.05f)
                    Check(Clearance(new Vector2(side*depth,lane*.9f),walls,posts)>.21f, "white ball passes through goalmouth into recess");
            Check(walls.Any(x=>x.gameObject.name.Contains("Back Net") && side*x.bounds.center.x>12 && DistanceToBox(new Vector2(side*12.07f,0),x)<.001f), "back net physically closes recess");
            foreach (int vertical in new[]{-1,1})
            {
                Check(walls.Any(x=>x.gameObject.name.Contains("Net") && DistanceToBox(new Vector2(side*11.5f,vertical*1.72f),x)<.001f), "goal side net stops recess escape");
                Check(Clearance(new Vector2(side*11.07f,vertical*3.5f),walls,posts)<.001f, "end line solid outside goalmouth");
                Check(Clearance(new Vector2(side*11f,vertical*1.65f),walls,posts)<0, "post closes mouth corner");
            }
        }
        Check(Clearance(Vector2.zero,walls,posts)>5, "halfway line has no physical wall");
        Check(Near(walls.First(x=>x.gameObject.name=="North Boundary").bounds.min.y,5.2f) && Near(walls.First(x=>x.gameObject.name=="South Boundary").bounds.max.y,-5.2f), "touchline walls leave correct22by10.4 playing area");
        // Radial samples from the center must meet a perimeter including both recesses.
        for(int degree=0;degree<360;degree++)
        {
            Vector2 direction = new Vector2(Mathf.Cos(degree*Mathf.Deg2Rad),Mathf.Sin(degree*Mathf.Deg2Rad));
            bool blocked=false;
            for(float distance=.1f;distance<14;distance+=.03f)
                if(Clearance(direction*distance,walls,posts)<=.03f) { blocked=true;break; }
            Check(blocked,"solid perimeter closes all radial escape paths");
        }
        SpriteRenderer[] sprites=campo.GetComponentsInChildren<SpriteRenderer>(true).Where(Active).ToArray();
        Check(sprites.Length>280 && sprites.Count(x=>x.gameObject.name=="Crowd Light")==112,"decorative arena and crowd generated");
        Check(sprites.All(x=>x.sprite!=null && x.sharedMaterial==campo.MaterialSprites),"all sprites reuse one owned unlit material");
        Check(sprites.All(x=>Finite(x.bounds.size.x)&&Finite(x.bounds.size.y)&&Finite(x.transform.position.x)&&Finite(x.transform.position.y)&&x.bounds.size.x<=32.1f&&x.bounds.size.y<=18.1f),"all stadium geometry finite and in world units");
        var surface=sprites.Single(x=>x.gameObject.name=="Playing Surface");
        Check(Near(surface.bounds.size.x,22)&&Near(surface.bounds.size.y,10.4f)&&Near(surface.color.g,config.corGramado.g),"field surface correctly sized and configured");
        Check(sprites.Count(x=>x.gameObject.name.StartsWith("Grass Stripe"))==10,"subtle pitch strips generated");
        var label=campo.GetComponentsInChildren<TextMeshPro>(true).FirstOrDefault(Active);
        Check(label!=null&&label.text=="DISC FOOTBALL"&&label.font==config.tema.fonteTexto&&label.textWrappingMode==TextWrappingModes.NoWrap,"English world label uses selected font");
        var custom=sprites.FirstOrDefault(x=>x.gameObject.name=="Custom Field Image");
        Check(custom!=null&&custom.sprite==config.fundoCampo&&Near(custom.bounds.size.x,22)&&Near(custom.bounds.size.y,10.4f),"external background sprite normalized without mutation");
        SpriteRenderer left=sprites.Single(x=>x.gameObject.name=="Left Goal Celebration"),right=sprites.Single(x=>x.gameObject.name=="Right Goal Celebration");
        Check(Near(left.color.a,0)&&Near(right.color.a,0),"goal glows initially hidden");
        campo.CelebrarGol(true);
        Check(left.color.a>0&&Near(right.color.a,0),"left goal celebrates independently");
        campo.CelebrarGol(false);
        Check(right.color.a>0,"right goal celebrates");
        Time.captureDeltaTime=.1f;Time.unscaledDeltaTime=5;
        Lifecycle.Invoke(campo,"Update");
        Check(left.color.a>0&&right.color.a>0,"recording captureDeltaTime controls cosmetic clock");
        float previous=left.color.a;
        Time.captureDeltaTime=0;Time.unscaledDeltaTime=float.NaN;Lifecycle.Invoke(campo,"Update");
        Check(Near(previous,left.color.a),"invalid cosmetic delta ignored");
        Time.unscaledDeltaTime=1;Lifecycle.Invoke(campo,"Update");
        Check(Near(left.color.a,0)&&Near(right.color.a,0),"goal celebration fades out");
        string geometry="["+string.Join(",",allBoxes.Select(x=>string.Format(CultureInfo.InvariantCulture,"{{\"name\":\"{0}\",\"trigger\":{1},\"x\":{2},\"y\":{3},\"w\":{4},\"h\":{5}}}",x.gameObject.name,x.isTrigger.ToString().ToLowerInvariant(),x.bounds.center.x,x.bounds.center.y,x.bounds.size.x,x.bounds.size.y)).ToArray())+"]";
        File.WriteAllText(Path.Combine(DirectoryPath,"geometry.json"),geometry);
    }
    private static void ValidateExterior(CampoDiscos campo, ProjetoFutebolDiscos config)
    {
        var textures = new List<Texture2D>();
        var pictures = new List<Sprite>();
        var cameraObject = new GameObject("Wide exterior camera");
        var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true;
        camera.orthographicSize = 10f; camera.aspect = 2.5f;
        foreach (int[] dimensions in new[] { new[] { 400, 100 }, new[] { 100, 400 }, new[] { 200, 200 } })
        {
            var texture = new Texture2D(dimensions[0], dimensions[1]); textures.Add(texture);
            var picture = Sprite.Create(texture, new Rect(0, 0, dimensions[0], dimensions[1]), Vector2.one * .5f, 10f); pictures.Add(picture);
            config.fundoExternoArena = picture; config.opacidadeFundoExterno = .42f;
            config.sobreporDecoracaoExterna = false; campo.Construir(config);
            var sprites = campo.GetComponentsInChildren<SpriteRenderer>(true).Where(Active).ToArray();
            var image = sprites.Single(x => x.gameObject.name == "Custom Exterior Image");
            Check(image.sprite == picture && Near(image.color.a, .42f), "custom exterior uses supplied picture and opacity");
            Check(Near(image.transform.localScale.x, image.transform.localScale.y), "exterior cover scale is uniform");
            Check(Near(image.bounds.size.x / image.bounds.size.y, (float)dimensions[0] / dimensions[1]), "portrait landscape and square exterior aspect preserved");
            Check(image.bounds.size.x >= 50f - .001f && image.bounds.size.y >= 20f - .001f, "exterior covers an expanded orthographic viewport");
            Check(!sprites.Any(x => x.gameObject.name == "Crowd Light" || x.gameObject.name == "Stand Panel"), "custom picture visible without procedural stands");
            Check(image.sortingOrder < sprites.Single(x => x.gameObject.name == "Playing Surface").sortingOrder &&
                image.sortingOrder < sprites.Single(x => x.gameObject.name == "Left Goal Floor").sortingOrder &&
                image.sortingOrder < sprites.Single(x => x.gameObject.name == "Right Goal Floor").sortingOrder, "picture remains below pitch and both goals");
            Check(image.GetComponent<Collider2D>() == null && campo.GetComponentsInChildren<BoxCollider2D>(true).Count(Active) == 14, "exterior adds no collisions and preserves goal sensors and walls");
            Check(!Owned(campo).Contains(picture) && !Owned(campo).Contains(texture), "supplied exterior assets never become owned resources");
            config.sobreporDecoracaoExterna = true; campo.Construir(config);
            sprites = campo.GetComponentsInChildren<SpriteRenderer>(true).Where(Active).ToArray();
            Check(sprites.Count(x => x.gameObject.name == "Crowd Light") == 112 && sprites.Any(x => x.gameObject.name == "Custom Exterior Image"), "procedural decoration can overlay custom exterior optionally");
            campo.Limpar(); Check(!picture.destroyed && !texture.destroyed, "rebuild and cleanup preserve exterior sprite and texture");
        }
        config.fundoExternoArena = null; config.sobreporDecoracaoExterna = false; campo.Construir(config);
        Check(campo.GetComponentsInChildren<SpriteRenderer>(true).Where(Active).Count(x => x.gameObject.name == "Crowd Light") == 112, "clearing exterior picture restores original decorations");
        Check(!campo.GetComponentsInChildren<SpriteRenderer>(true).Where(Active).Any(x => x.gameObject.name == "Custom Exterior Image"), "clearing picture removes retired custom exterior");
        UnityEngine.Object.DestroyImmediate(cameraObject);
    }
    public static void Main()
    {
        Application.isPlaying=true;
        var root=new GameObject("Field Test");
        var campo=root.AddComponent<CampoDiscos>();
        var config=root.AddComponent<ProjetoFutebolDiscos>();
        config.tema=ScriptableObject.CreateInstance<TemaCompeticao>();config.tema.fonteTexto=new TMP_FontAsset();
        var externalTexture=new Texture2D(200,100);config.fundoCampo=Sprite.Create(externalTexture,new Rect(0,0,200,100),Vector2.one*.5f,10);
        var externalMaterial=new Material(new Shader{name="ResourceSpriteUnlit"});Resources.Stored["DiscosSprite"]=externalMaterial;
        campo.Construir(config);
        Check(campo.MaterialSprites!=externalMaterial&&campo.MaterialSprites.shader==externalMaterial.shader,"resource material cloned for ownership");
        ValidateField(campo,config);
        var oldResources=Owned(campo);var oldChildren=campo.GetComponentsInChildren<SpriteRenderer>(true).Select(x=>x.gameObject).ToArray();
        Check(oldResources.Count==6&&oldResources.Distinct().Count()==6,"six generated resources tracked exactly once");
        campo.Construir(config);
        Check(oldResources.All(x=>x.destroyed)&&oldChildren.All(x=>x.destroyed),"rebuild disposes old geometry and all owned resources");
        Check(!config.fundoCampo.destroyed&&!externalTexture.destroyed&&!config.tema.fonteTexto.destroyed&&!externalMaterial.destroyed,"rebuild preserves all external assets");
        config.textoNoCampo="  YOUR SQUAD  ";campo.Construir(config);
        var customLabel=campo.GetComponentsInChildren<TextMeshPro>(true).Single(Active);
        Check(customLabel.text=="YOUR SQUAD"&&customLabel.enableAutoSizing&&!customLabel.richText,"custom field branding uses the chosen plain text and fits its bounds");
        config.mostrarTextoNoCampo=false;campo.Construir(config);
        Check(!campo.GetComponentsInChildren<TextMeshPro>(true).Any(Active),"field branding toggle hides a configured signature");
        Check(campo.GetComponentsInChildren<GolDiscos>(true).Count(Active)==2,"hiding field branding preserves both physical goals");
        config.mostrarTextoNoCampo=true;
        foreach(string empty in new[]{"", "  ", null})
        {
            config.textoNoCampo=empty;campo.Construir(config);
            Check(!campo.GetComponentsInChildren<TextMeshPro>(true).Any(Active),"empty field branding never restores fixed DISC FOOTBALL text");
        }
        ValidateExterior(campo, config);
        var resources=Owned(campo);campo.Limpar();
        Check(resources.All(x=>x.destroyed)&&Owned(campo).Count==0&&campo.transform.childCount==0,"cleanup disposes owned resources and hierarchy");
        Check(campo.SpriteCirculo==null&&campo.MaterialSprites==null&&campo.MaterialFisica==null,"cleanup clears public resource API");
        campo.Limpar();Check(true,"cleanup idempotent");
        Resources.Stored.Clear();Shader.Missing.Add("Universal Render Pipeline/2D/Sprite-Unlit-Default");config.tema=null;config.fundoCampo=null;TMP_Settings.defaultFontAsset=null;
        Application.isPlaying=false;campo.Construir(config);
        Check(campo.MaterialSprites.shader.name=="Sprites/Default","built-in shader fallback works");
        Check(campo.GetComponentsInChildren<TextMeshPro>(true).Length==0,"missing font skips only optional world label");
        Check(campo.GetComponentsInChildren<GolDiscos>(true).Length==2,"missing font does not block physics field");
        var lastResources=Owned(campo);UnityEngine.Object.DestroyImmediate(root);
        Check(lastResources.All(x=>x.destroyed),"OnDestroy releases field runtime resources in edit mode");
        Console.WriteLine("PASS "+checks+" field assertions. Geometry, API, clocks and resource lifecycle validated against actual CampoDiscos source. Unity rendering and native contacts not executed.");
        File.WriteAllText(Path.Combine(DirectoryPath,"report.json"),"{\"passed\":true,\"assertions\":"+checks+",\"actualSource\":\"Assets/FutebolDiscos/Scripts/CampoDiscos.cs\",\"nativeUnity\":false,\"limits\":\"Contract facade checks geometry and lifecycle; no rendering or collision solver.\"}");
    }
}
