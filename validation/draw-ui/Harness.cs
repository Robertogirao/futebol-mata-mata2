using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FutebolDeDiscos;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class DrawUiHarness
{
    private static int checks;
    private static void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
    private static object Field(object obj,string name) { return obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(obj); }
    private static void Call(object obj,string name) { obj.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,null); }
    private static void Clean()
    {
        foreach(GameObject go in GameObject.All.Where(g=>!g.destroyed&&g.transform.parent==null).ToArray()) UnityEngine.Object.Destroy(go);
        GameObject.All.Clear(); Scheduler.Reset(); Debug.Errors.Clear(); Time.captureDeltaTime=0f;Time.unscaledDeltaTime=.05f;Time.timeScale=1f;
    }
    private static List<TeamData> Teams(int count)
    {
        return Enumerable.Range(0,count).Select(i=>new TeamData {name="club_"+i,nomeEquipe="Club "+i,escudoOuBandeira=new Sprite{name="crest_"+i}}).ToList();
    }
    private static List<CapitaoConvidado> Captains(int count)
    {
        return Enumerable.Range(0,count).Select(i=>new CapitaoConvidado {name="hero_"+i,nomeCapitao="Hero "+i,foto=new Sprite{name="face_"+i},overall=70}).ToList();
    }
    private static TextMeshProUGUI Text(SorteioCapitaesUI ui,string name)
    { return ui.GetComponentsInChildren<TextMeshProUGUI>(true).Single(t=>t.gameObject.name==name); }
    private static void CountsAndIdentity()
    {
        foreach(int count in new[]{2,10,16,20,32})
        {
            Clean();var times=Teams(count);var caps=Captains(count+3);
            var fixes=new List<VinculoCapitaoConvidado>{new VinculoCapitaoConvidado{time=times[0],capitao=caps[0]}};
            var motor=new MotorSorteioCapitaes(times,caps,fixes,348923);
            var go=new GameObject("UI owner");var ui=go.AddComponent<SorteioCapitaesUI>();
            var tema=new TemaCompeticao{fonteTitulo=TMP_Settings.defaultFontAsset,fonteTexto=TMP_Settings.defaultFontAsset};
            IEnumerator e=ui.Exibir(motor,times,caps,tema,.4f,1f,.05f);
            Time.captureDeltaTime=.05f; Time.unscaledDeltaTime=0f; Time.timeScale=0f;
            Check(e.MoveNext(),"warmup creates a frame");
            Check(ui.Ativo&&!ui.Concluido&&motor.Resultados.Count==0,"opening shows all marbles before the simulation assigns any club");
            var objects=ui.GetComponentsInChildren<RectTransform>(true);
            Check(objects.Count(t=>t.gameObject.name.StartsWith("Captain Marble "))==count,"all selected captain marbles rendered at count "+count);
            Check(objects.Count(t=>t.gameObject.name.StartsWith("Draw Team "))==count,"one visible card per actual club at count "+count);
            if(count==32)
            {
                var cards=objects.Where(t=>t.gameObject.name.StartsWith("Draw Team ")).ToArray();
                Check(cards.All(card=>card.GetComponentsInChildren<TextMeshProUGUI>(true).Where(t=>t.gameObject.name=="Team Name"||t.gameObject.name=="Captain Name").All(t=>t.rectTransform.sizeDelta.y>=16f)),"32-club board reserves readable line heights for both labels");
                Check(cards.All(card=>{var texts=card.GetComponentsInChildren<TextMeshProUGUI>(true);var club=texts.Single(t=>t.gameObject.name=="Team Name").rectTransform;var cap=texts.Single(t=>t.gameObject.name=="Captain Name").rectTransform;return -club.anchoredPosition.y+club.sizeDelta.y<-cap.anchoredPosition.y;}),"club and captain text rows do not overlap on the dense board");
                Check(cards.All(card=>{var label=card.GetComponentsInChildren<TextMeshProUGUI>(true).Single(t=>t.gameObject.name=="Captain Name").rectTransform;return -label.anchoredPosition.y+label.sizeDelta.y<=card.sizeDelta.y-1f;}),"captain text remains inside all 32 cards");
            }
            Canvas canvas=ui.GetComponentsInChildren<Canvas>(true).Single();
            Check(canvas.sortingOrder==2600&&canvas.overrideSorting,"draft overlays both bracket and match presentations");
            var scaler=canvas.GetComponent<CanvasScaler>();
            Check(scaler.referenceResolution.x==1920&&scaler.referenceResolution.y==1080,"broadcast uses full HD reference");
            foreach(var marble in motor.Marbles)
            {
                var area=objects.Single(t=>t.gameObject.name=="Captain Marble "+marble.Capitao.Identificador);
                Check(area.anchoredPosition.x>=marble.Raio&&area.anchoredPosition.x<=1080-marble.Raio,"initial portrait stays horizontally visible");
                Check(-area.anchoredPosition.y>=marble.Raio&&-area.anchoredPosition.y<=620-marble.Raio,"initial portrait stays vertically visible");
                Check(area.GetComponentsInChildren<Mask>(true).Length==1,"captain portraits have circular masks");
                Check(area.GetComponentsInChildren<Image>(true).Any(im=>im.sprite==marble.Capitao.foto),"correct captain portrait reaches the marble");
            }
            int frames=0;while(e.MoveNext()){if(++frames>3000)throw new Exception("UI did not finish");}
            Check(ui.Concluido&&ui.Ativo&&ui.QuantidadeRevelada==count,"complete board remains on-screen so caller can persist before summary");
            Check(Text(ui,"Assignment Counter").text==count+" / "+count,"counter reaches all clubs");
            Check(Text(ui,"Draft Phase").text=="ALL CLUBS HAVE A CAPTAIN","completion is announced in English");
            Check(motor.Resultados.Count==count,"all reveals correspond to real simulator arrivals");
            foreach(var registro in motor.Resultados)
            {
                var card=objects.Single(t=>t.gameObject.name=="Draw Team "+registro.timeId);
                var cap=caps.Single(c=>c.Identificador==registro.capitaoId);
                Check(card.GetComponentsInChildren<TextMeshProUGUI>(true).Single(t=>t.gameObject.name=="Captain Name").text==cap.nomeCapitao,"card preserves the simulator's exact assignment");
                Check(card.GetComponentsInChildren<Image>(true).Single(t=>t.gameObject.name=="Captain Portrait").sprite==cap.foto,"revealed card uses assigned portrait");
            }
            Sprite circle=(Sprite)Field(ui,"circulo"); Texture2D texture=(Texture2D)Field(ui,"texturaCirculo");
            Check(circle!=null&&texture!=null&&!circle.destroyed&&!texture.destroyed,"one owned circle resource pair stays valid across animation");
            IEnumerator hold=ui.ExibirResultadoFinal(.2f);while(hold.MoveNext()){}
            Check(!ui.Ativo&&ui.Concluido,"hiding final board preserves completion gate for tournament startup");
            Check(!caps.Any(c=>c.foto.destroyed)&&!times.Any(t=>t.escudoOuBandeira.destroyed),"external portraits and club crests remain owned by their assets");
            UnityEngine.Object.Destroy(go);
            Check(circle.destroyed&&texture.destroyed,"destroying view releases generated circle resources");
        }
    }
    private static void ReplayAndCancellation()
    {
        Clean();var teams=Teams(10);var caps=Captains(12);var fixeds=new List<VinculoCapitaoConvidado>();
        var first=new MotorSorteioCapitaes(teams,caps,fixeds,93);while(!first.Concluido)first.Avancar(.05f);
        var saved=first.Resultados.ToList();string fingerprint=string.Join("|",saved.Select(r=>r.timeId+"="+r.capitaoId).ToArray());
        var motor=new MotorSorteioCapitaes(teams,caps,fixeds,93,saved);var go=new GameObject("Replay owner");var ui=go.AddComponent<SorteioCapitaesUI>();
        IEnumerator e=ui.Exibir(motor,teams,caps,null,.2f,1f,.05f,true);
        Time.captureDeltaTime=0f;Time.unscaledDeltaTime=.05f;
        Check(e.MoveNext()&&Text(ui,"Draft Title").text=="SAVED DRAFT","saved draws are explicitly identified");
        while(e.MoveNext()){}
        Check(ui.Concluido,"unscaled fallback can finish the presentation");
        Check(fingerprint==string.Join("|",saved.Select(r=>r.timeId+"="+r.capitaoId).ToArray()),"view never mutates persisted assignments");
        Sprite circle=(Sprite)Field(ui,"circulo");
        e=ui.Exibir(new MotorSorteioCapitaes(teams,caps,fixeds,9),teams,caps,null,1f,1f,.1f);
        Check(e.MoveNext()&&!ui.Concluido,"new presentation clears the previous completion flag");
        Check(ReferenceEquals(circle,Field(ui,"circulo")),"circle sprite reused instead of allocated again per draw");
        ui.Cancelar();Check(!e.MoveNext()&&!ui.Ativo&&!ui.Concluido,"cancel interrupts warmup and hides the canvas without completing");
        e=ui.Exibir(new MotorSorteioCapitaes(teams,caps,fixeds,9),teams,caps,null,0f,1f,.1f);
        Check(e.MoveNext(),"rebuild after cancellation can start again");
        Call(ui,"OnDisable");Check(!e.MoveNext()&&!ui.Ativo&&!ui.Concluido,"owner disable cancels active simulation presentation");
        Check(!teams.Any(t=>t.escudoOuBandeira.destroyed)&&!caps.Any(c=>c.foto.destroyed),"cancellation leaves supplied assets intact");
        Check(Debug.Errors.Count==0,"UI emits no runtime errors");Clean();
    }
    public static void Main()
    {
        try{CountsAndIdentity();ReplayAndCancellation();Console.WriteLine("PASS "+checks+" captain-draw UI layout, assignment, clock and cancellation assertions using actual sources.");Console.WriteLine("Unity UI/TMP API facade; native rendering was not executed.");}
        finally{Clean();}
    }
}
