using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;using UnityEngine;using FutebolDeDiscos;
public static class FeatureHarness {
static int checks;static string caseName;
static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(caseName+": "+message+" Errors: "+string.Join(" | ",Debug.Errors.ToArray()));}
static object Call(object obj,string name,params object[] args){var m=obj.GetType().GetMethod(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);if(m==null)throw new Exception("Missing method "+name);try{return m.Invoke(obj,args);}catch(TargetInvocationException e){throw e.InnerException;}}
static void Reset(){foreach(var go in GameObject.All.Where(g=>g.transform.parent==null&&!g.destroyed).ToArray())UnityEngine.Object.Destroy(go);GameObject.All.Clear();Scheduler.Reset();Time.captureDeltaTime=0;Debug.Errors.Clear();Debug.Warnings.Clear();Debug.Messages.Clear();}
static TeamData Team(string id,Color color){var t=ScriptableObject.CreateInstance<TeamData>();t.name=id;t.nomeEquipe=id.ToUpperInvariant();t.sigla=id;t.corPrincipal=color;t.corSecundaria=Color.white;t.elenco.Add(new JogadorDados{nomeJogador=id+" Player",foto512x512=new Sprite()});t.elenco.Add(new JogadorDados{nomeJogador=id+" Second"});return t;}
sealed class Match {public GameObject root;public ProjetoFutebolDiscos config;public PartidaDiscos engine;public CampoDiscos campo;public HudFutebolDiscos hud;public UniversalUI ui;public GoalBannerUI banner;public TeamData a,b;public List<JogadorDados> repsA,repsB;public GolDiscos left,right;
 public Match(){root=new GameObject("RuntimeFixture");config=root.AddComponent<ProjetoFutebolDiscos>();config.tema=ScriptableObject.CreateInstance<TemaCompeticao>();config.tema.fonteTexto=config.tema.fonteTitulo=TMPro.TMP_Settings.defaultFontAsset;ui=root.AddComponent<UniversalUI>();ui.ativarGoldenGoal=false;ui.duracaoRealEmSegundos=45;ui.tempoCongeladoPosJogo=0;banner=root.AddComponent<GoalBannerUI>();banner.tempoDeExibicao=.06f;banner.intervaloEntreBanners=0;campo=root.AddComponent<CampoDiscos>();hud=root.AddComponent<HudFutebolDiscos>();engine=root.AddComponent<PartidaDiscos>();campo.Construir(config);hud.Construir(config,ui,banner);engine.Configurar(config,campo,hud);a=Team("home",Color.blue);b=Team("away",Color.red);repsA=a.ObterTitularesDaPartida();repsB=b.ObterTitularesDaPartida();left=root.GetComponentsInChildren<GolDiscos>().Single(g=>g.esquerda);right=root.GetComponentsInChildren<GolDiscos>().Single(g=>!g.esquerda);}
 public void Prepare(){engine.PrepararPartida(a,b,repsA,repsB,a.corPrincipal,b.corPrincipal);ui.ConfigurarPartida(a,b,null,repsA,repsB);}
 public void Start(){Prepare();ui.IniciarCronometro();engine.IniciarPartida(a,b,float.MaxValue,repsA,repsB,a.corPrincipal,b.corPrincipal);Call(engine,"FixedUpdate");Call(engine,"FixedUpdate");}
 public void ResetAfterGoal(){Time.captureDeltaTime=.25f;for(int i=0;i<6;i++){Call(engine,"Update");Scheduler.Tick(.001f);}Call(engine,"FixedUpdate");Call(engine,"FixedUpdate");Time.captureDeltaTime=0;}
 public void Cross(bool leftGoal){engine.Bola.Corpo.position=new Vector2((leftGoal?-1:1)*(CampoDiscos.MeioComprimento+config.tamanhoBola*.5f+.03f),0);Call(leftGoal?left:right,"OnTriggerEnter2D",engine.Bola.GetComponent<CircleCollider2D>());}
}

static void Set(object obj,string name,object value){var f=obj.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance);if(f==null)throw new Exception("Missing field "+name);f.SetValue(obj,value);}
sealed class FixedRandom:System.Random {readonly double value;public FixedRandom(double d){value=d;}public override double NextDouble(){return value;}}
static void Contact(Match m,DiscoAtleta a){var c=new Collision2D{collider=a.GetComponent<CircleCollider2D>()};Call(m.engine.Bola,"OnCollisionEnter2D",c);}
static void Exit(Match m,DiscoAtleta a){var c=new Collision2D{collider=a.GetComponent<CircleCollider2D>()};Call(m.engine.Bola,"OnCollisionExit2D",c);}
static void Charge(Match m,DiscoAtleta a){for(int i=0;i<PartidaDiscos.ToquesParaEspecial;i++){Contact(m,a);Exit(m,a);}}
static void Contacts(){caseName="contact counting";var m=new Match();m.Start();var a=m.engine.Atletas.First(x=>x.TimeA&&x.Capitao);var b=m.engine.Atletas.First(x=>!x.TimeA&&x.Capitao);var other=m.engine.Atletas.First(x=>x.TimeA&&!x.Capitao);
Contact(m,other);Check(m.engine.CargaCapitao(true)==0,"noncaptain contacts do not charge");
Contact(m,a);m.engine.Bola.RegistrarToque(a);Call(m.engine.Bola,"OnCollisionStay2D",new Collision2D{collider=a.GetComponent<CircleCollider2D>()});Check(m.engine.CargaCapitao(true)==1,"enter, shot, stay on same contact count once");
for(int i=0;i<15;i++)m.engine.Bola.RegistrarToque(a);Check(m.engine.CargaCapitao(true)==1,"sustained contact cannot charge to full");
Exit(m,a);Contact(m,a);Check(m.engine.CargaCapitao(true)==2,"rapid separated contact counts without arbitrary cooldown");Contact(m,b);Check(m.engine.CargaCapitao(false)==1&&m.engine.CargaCapitao(true)==2,"captains charge independently");
m.engine.Bola.Repor(Vector2.zero,Vector2.zero);m.engine.Bola.RegistrarToque(a,registrarContato:false);Check(m.engine.CargaCapitao(true)==2,"setpiece attribution does not charge");Contact(m,a);Check(m.engine.CargaCapitao(true)==3,"real touch after setpiece attribution counts");
Charge(m,a);Check(m.engine.CargaCapitao(true)==PartidaDiscos.ToquesParaEspecial,"charge is bounded by five touches");m.engine.PararPartida();Contact(m,b);Check(m.engine.CargaCapitao(false)==1&&!m.engine.RoletaAtiva(true),"stopped match cannot charge or spin");m.Start();Check(m.engine.CargaCapitao(true)==0&&m.engine.CargaCapitao(false)==0,"new match resets both captain charges");Reset();}
static void Arm(Match m, DiscoAtleta captain, bool hit, bool leaveLastContact = true)
{
    Set(m.engine, "random", new FixedRandom(hit ? 0d : .999999d));
    for (int i = 0; i < PartidaDiscos.ToquesParaEspecial; i++)
    {
        Contact(m, captain);
        if (i < PartidaDiscos.ToquesParaEspecial - 1 || leaveLastContact) Exit(m, captain);
    }
    Call(m.engine, "AtualizarEspecial", .1f);
    Call(m.engine, "AtualizarEspecial", 3.3f);
}
static void Roulette()
{
    caseName = "armed result waits for distinct next touch";
    var m = new Match(); m.Start();
    var a = m.engine.Atletas.First(x => x.TimeA && x.Capitao);
    var b = m.engine.Atletas.First(x => !x.TimeA && x.Capitao);
    Set(m.engine, "random", new FixedRandom(0));
    for (int i = 0; i < 5; i++) { Contact(m, a); if (i < 4) Exit(m, a); }
    Call(m.engine, "AtualizarEspecial", .1f);
    Check(m.engine.RoletaAtiva(true) && !m.engine.RoletaAtiva(false), "full home charge starts only home spin");
    Call(m.engine, "AtualizarEspecial", 2.4f);
    Check(m.ui.GolsA == 0 && m.engine.ProgressoRoleta == 1f && m.engine.EspecialArmado(true), "spin completion at 2.5s arms result without awarding a goal");
    Call(m.engine, "AtualizarEspecial", .8f);
    Check(m.engine.EspecialArmado(true) && m.engine.AcertoArmado(true) && !m.engine.RoletaAtiva(true) && !m.engine.ChuteEspecialEmAndamento, "spin completion only arms HIT");
    Check(!m.banner.TemBannersPendentes, "roulette outcome never shows a goal banner before a shot");
    Check(m.ui.GolsA == 0 && m.ui.GolsB == 0 && m.engine.CargaCapitao(true) == 5 && !m.engine.EmReposicao, "armed result preserves full charge and live gameplay without score");
    for (int i = 0; i < 4; i++) { Call(m.engine.Bola, "OnCollisionStay2D", new Collision2D { collider = a.GetComponent<CircleCollider2D>() }); m.engine.Bola.RegistrarToque(a); }
    Check(m.engine.EspecialArmado(true) && !m.engine.ChuteEspecialEmAndamento, "sustained fifth contact cannot consume armed result");
    Contact(m, b); Exit(m, b);
    Check(m.engine.EspecialArmado(true) && !m.engine.ChuteEspecialEmAndamento, "opponent contact cannot consume home result");
    Vector2 start = m.engine.Bola.Corpo.position;
    Vector3 originalCaptainScale = a.transform.localScale, originalBallScale = m.engine.Bola.transform.localScale;
    Exit(m, a); Contact(m, a);
    Check(m.engine.ChuteEspecialEmAndamento && m.engine.EmReposicao && !m.engine.EspecialArmado(true) && m.engine.CargaCapitao(true) == 0, "next distinct captain touch consumes charge and launches animated HIT");
    Check(m.ui.GolsA == 0 && m.engine.Atletas.All(x => !x.Corpo.simulated) && !m.engine.Bola.Corpo.simulated, "power shot begins with actors frozen and score unchanged");
    Check(!m.banner.TemBannersPendentes, "starting special flight does not celebrate prematurely");
    Check(m.root.GetComponentsInChildren<Transform>().Any(t => t.gameObject.name == "Captain Power Shot Aura"), "HIT creates visible power aura");
    float clock = m.ui.TempoAtual; Time.captureDeltaTime = .25f; Call(m.ui, "Update"); Time.captureDeltaTime = 0f;
    Check(m.ui.TempoAtual == clock, "match clock pauses during visual special shot");
    Call(m.engine, "AtualizarChuteEspecial", .7f);
    Check(Vector2.Distance(start, m.engine.Bola.Corpo.position) > .1f && m.ui.GolsA == 0 && m.engine.ChuteEspecialEmAndamento, "slower HIT remains in flight at old 0.7s finish time");
    Check(m.engine.Bola.transform.localScale.x > originalBallScale.x && a.transform.localScale.x > originalCaptainScale.x, "HIT visibly pulses ball and captain");
    Check(!m.banner.TemBannersPendentes, "midflight special shot still has no goal banner");
    Call(m.engine, "AtualizarChuteEspecial", .7f);
    Check(m.ui.GolsA == 1 && m.ui.GolsB == 0 && !m.engine.ChuteEspecialEmAndamento && m.engine.EmReposicao, "HIT completes at 1.4s and scores once for correct captain team");
    Check(m.banner.TemBannersPendentes && m.banner.textoDados.text.Contains("SPECIAL GOAL!") && m.banner.textoDados.text.Contains(a.NomeExibicao.ToUpperInvariant()), "completed special HIT routes to distinct special goal banner with scorer snapshot");
    Check(a.transform.localScale == originalCaptainScale && m.engine.Bola.transform.localScale == originalBallScale && !m.root.GetComponentsInChildren<Transform>().Any(t => t.gameObject.name == "Captain Power Shot Aura"), "finished HIT restores scales and removes power aura");
    Call(m.engine, "AtualizarChuteEspecial", 10f); Call(m.engine, "AtualizarEspecial", 10f);
    Check(m.ui.GolsA == 1, "late callbacks cannot duplicate special goal");
    m.ResetAfterGoal();
    Arm(m, b, false);
    Check(m.engine.EspecialArmado(false) && !m.engine.AcertoArmado(false) && m.ui.GolsA == 1 && m.ui.GolsB == 0, "away MISS is armed with no immediate goal");
    Contact(m, b);
    Check(m.engine.ChuteEspecialEmAndamento && !m.engine.EspecialArmado(false), "next away captain contact launches MISS animation");
    Call(m.engine, "AtualizarChuteEspecial", .45f);
    Check(m.engine.ChuteEspecialEmAndamento && m.ui.GolsB == 0, "slower MISS remains in flight at old 0.45s finish time");
    Call(m.engine, "AtualizarChuteEspecial", .45f);
    Check(!m.engine.ChuteEspecialEmAndamento && !m.engine.EmReposicao && m.ui.GolsA == 1 && m.ui.GolsB == 0 && m.engine.Bola.Corpo.simulated, "MISS completes at 0.9s and resumes ball without awarding a goal");
    Check(Math.Abs(m.engine.Bola.Corpo.position.y) > CampoDiscos.MeioGol && m.engine.CargaCapitao(false) == 0, "MISS ends outside goal mouth and consumes charge once");
    Reset();
}
static void Cancellation()
{
    caseName = "armed and animated special cancellation";
    var m = new Match(); m.Start(); var a = m.engine.Atletas.First(x => x.TimeA && x.Capitao);
    Arm(m, a, true); m.engine.PararPartida();
    Check(!m.engine.EspecialArmado(true) && !m.engine.RoletaAtiva(true) && !m.engine.ChuteEspecialEmAndamento, "stop discards armed and pending outcomes");
    m.Start(); a = m.engine.Atletas.First(x => x.TimeA && x.Capitao);
    Arm(m, a, true); Contact(m, a);
    var ball = m.engine.Bola; var originalScale = Vector3.one; var trail = ball.GetComponent<TrailRenderer>();
    Call(m.engine, "AtualizarChuteEspecial", .3f); m.engine.PararPartida();
    Check(!m.engine.ChuteEspecialEmAndamento && a.transform.localScale == originalScale && ball.transform.localScale == originalScale, "stop midflight restores captain and ball transforms");
    Check(!ball.Corpo.simulated && m.engine.Atletas.All(x => !x.Corpo.simulated) && ball.Corpo.interpolation == RigidbodyInterpolation2D.Interpolate, "stop midflight freezes bodies and restores interpolation");
    Check(!m.root.GetComponentsInChildren<Transform>().Any(t => t.gameObject.name == "Captain Power Shot Aura") && Math.Abs(trail.time - .19f) < .0001f, "stop midflight removes aura and restores normal trail");
    Call(m.engine, "AtualizarChuteEspecial", 5f);
    Check(m.ui.GolsA == 0 && m.ui.GolsB == 0, "cancelled animation cannot score later");
    m.Start(); a = m.engine.Atletas.First(x => x.TimeA && x.Capitao);
    Arm(m, a, true); Contact(m, a); Call(m.engine, "AtualizarChuteEspecial", .3f);
    m.Start();
    Check(!m.engine.ChuteEspecialEmAndamento && !m.engine.EspecialArmado(true) && m.engine.CargaCapitao(true) == 0 && m.ui.GolsA == 0, "new match clears in-flight shot, armed state and charge");
    Reset();
}

static bool IsHitSector(Color color) { return color.g > .45f && color.r < .2f; }
static bool IsMissSector(Color color) { return color.r > .6f && color.g < .3f; }
static void OddsAndMesh()
{
    caseName = "overall odds and decorated roulette mesh";
    float last = 0f;
    for (int i = 1; i <= 100; i++)
    {
        float chance = PartidaDiscos.ChanceEspecial(i);
        Check(chance >= last && Math.Abs(chance - i / 100f) < .00001f, "chance rises with overall " + i); last = chance;
    }
    Check(PartidaDiscos.ChanceEspecial(-20) == .01f && PartidaDiscos.ChanceEspecial(500) == 1f, "overall endpoints clamp to valid probability");
    var wheel = new GameObject("decorated wheel", typeof(RectTransform)).AddComponent<RoletaCapitaoUI>();
    wheel.rectTransform.sizeDelta = new Vector2(100f, 100f);
    foreach (float chance in new[] { .01f, .25f, .5f, .85f, 1f })
    {
        wheel.DefinirChance(chance); var vh = new UnityEngine.UI.VertexHelper(); Call(wheel, "OnPopulateMesh", vh);
        double hit = 0d, sectors = 0d, decor = 0d;
        for (int i = 0; i < vh.indices.Count; i += 3)
        {
            int n = vh.indices[i]; Vector3 a = vh.vertices[n], b = vh.vertices[vh.indices[i + 1]], c = vh.vertices[vh.indices[i + 2]];
            double area = Math.Abs((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) * .5d;
            Color color = vh.colors[n];
            if (IsHitSector(color)) { hit += area; sectors += area; }
            else if (IsMissSector(color)) sectors += area;
            else decor += area;
        }
        Check(sectors > 0d && Math.Abs(hit / sectors - chance) < .002d, "actual shaded HIT/MISS sector areas match chance while decoration is excluded: " + chance);
        Check(decor > sectors * .1d && vh.vertices.Count > 1000 && vh.vertices.Count < 65536, "wheel includes substantial rings, ticks and hub within safe mesh size");
        Check(vh.colors.All(c => c.a == 1f) && vh.vertices.All(v => v.x >= -.001f && v.x <= 100.001f && v.y >= -.001f && v.y <= 100.001f), "decorated wheel mesh stays opaque and inside its rect");
        Check(vh.vertices.Any(v => Vector2.Distance(v, new Vector2(50f, 50f)) > 43f), "outer decorative rings extend beyond the colored probability sectors");
    }
    Reset();
}
static object Get(object obj,string name){return obj.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance).GetValue(obj);}
static void Cards()
{
    caseName = "external captain cards and visible roulette";
    var m = new Match(); m.config.corBarraCapitao = new Color(.1f, .2f, .8f, .2f); m.Start();
    var layout = m.root.GetComponentsInChildren<LayoutJogadorUnicoUI>().FirstOrDefault();
    Check(layout != null, "existing external portrait layout exists");
    var ficha = Get(layout, "fichaA");
    var carga = (UnityEngine.UI.Image)Get(ficha, "carga");
    var texto = (TMPro.TextMeshProUGUI)Get(ficha, "textoCarga");
    var painel = (RectTransform)Get(ficha, "painelEspecial");
    var especial = (RectTransform)Get(ficha, "especial");
    var resultado = (TMPro.TextMeshProUGUI)Get(ficha, "resultado");
    var ponteiro = (RectTransform)Get(ficha, "ponteiro");
    var wheel = (RoletaCapitaoUI)Get(ficha, "roleta");
    var shimmer = (UnityEngine.UI.Image)Get(ficha, "brilhoCarga");
    var a = m.engine.Atletas.First(x => x.TimeA && x.Capitao);
    Contact(m, a); Exit(m, a); Call(layout, "Update");
    Check(carga.rectTransform.anchorMax.x > 0f && carga.rectTransform.anchorMax.x < .2f, "charge bar interpolates toward contact count");
    for (int i = 0; i < 8; i++) { Scheduler.Tick(.02f); Call(layout, "Update"); }
    Check(carga.gameObject.activeInHierarchy && Math.Abs(carga.rectTransform.anchorMax.x - .2f) < .0001f && texto.text == "1/5 TOUCHES", "external bar settles at correct charge with English counter");
    Check(painel.GetComponent<UnityEngine.UI.Image>().color.a == 1f && painel.anchorMax.y <= .84f, "special panel is opaque and placed lower beside external portrait");
    Check(carga.color.a == 1f && carga.color.b > carga.color.r, "bar uses selected configuration color with visible alpha");
    float shimmerStart = shimmer.rectTransform.anchorMin.x; Color initial = carga.color;
    Scheduler.Tick(.15f); Call(layout, "Update");
    Check(shimmer.gameObject.activeSelf && shimmer.rectTransform.anchorMin.x != shimmerStart && carga.color.r != initial.r, "charge bar shimmer and color pulse animate over time");
    Check(!especial.gameObject.activeSelf, "roulette stays hidden before full charge");
    Set(m.engine, "random", new FixedRandom(0)); Charge(m, a); Call(m.engine, "AtualizarEspecial", .1f); Call(layout, "Update");
    Check(especial.gameObject.activeSelf && resultado.text.Contains("HIT") && resultado.text.Contains("MISS"), "full bar exposes English HIT/MISS odds next to photo");
    Check(wheel is UnityEngine.UI.Image && wheel.GetComponent<CanvasRenderer>() != null && wheel.mainTexture == Texture2D.whiteTexture && wheel.color.a == 1f, "roulette has native Image lifecycle, CanvasRenderer, white texture and opaque tint");
    var vertices = new UnityEngine.UI.VertexHelper(); Call(wheel, "OnPopulateMesh", vertices);
    Check(vertices.vertices.Count > 0 && vertices.colors.All(c => c.a == 1f), "actual wheel mesh supplies visible opaque vertex colors");
    Call(m.engine, "AtualizarEspecial", 2.4f); Call(layout, "Update");
    float angle = (-ponteiro.localRotation.eulerAngles.z) % 360f; float chance = PartidaDiscos.ChanceEspecial(a.OverallEfetivo);
    Check(m.engine.EspecialArmado(true) && angle >= 0f && angle < chance * 360f && resultado.text.Contains("HIT READY") && resultado.text.Contains("NEXT TOUCH") && texto.text == "SPECIAL READY", "visible stopped pointer and ready label match armed HIT exactly at spin completion");
    Call(layout, "Update"); Check(especial.gameObject.activeSelf && m.ui.GolsA == 0, "roulette ready result remains visible without auto scoring");
    Exit(m, a); Contact(m, a); Call(layout, "Update");
    Check(!especial.gameObject.activeSelf && m.engine.ChuteEspecialEmAndamento, "next touch hides roulette for power-shot animation");
    m.engine.PararPartida(); Call(layout, "Update");
    Check(!painel.gameObject.activeSelf && !especial.gameObject.activeSelf, "special panel and wheel hide when match ends");
    Reset();
}

static bool SameColor(Color a, Color b)
{
    return Math.Abs(a.r - b.r) < .0001f && Math.Abs(a.g - b.g) < .0001f && Math.Abs(a.b - b.b) < .0001f && Math.Abs(a.a - b.a) < .0001f;
}
static void WaitingPulse()
{
    caseName = "slow opaque background pulse while waiting for next touch";
    foreach (bool hit in new[] { true, false })
    {
        var m = new Match(); m.Start();
        var a = m.engine.Atletas.First(x => x.TimeA && x.Capitao);
        var layout = m.root.GetComponentsInChildren<LayoutJogadorUnicoUI>().First();
        var ficha = Get(layout, "fichaA");
        var background = (UnityEngine.UI.Image)Get(ficha, "fundoEspecial");
        Call(layout, "Update"); Color normal = background.color;
        Time.captureDeltaTime = 1.3f; Call(layout, "Update");
        Check(SameColor(normal, background.color), "uncharged panel background does not pulse");
        Set(m.engine, "random", new FixedRandom(hit ? 0d : .999999d)); Charge(m, a); Call(m.engine, "AtualizarEspecial", .1f);
        Call(layout, "Update");
        Check(m.engine.RoletaAtiva(true) && SameColor(normal, background.color), "spinning panel background does not pulse before a result is armed");
        Call(m.engine, "AtualizarEspecial", 2.4f);
        Set(ficha, "tempoEsperaEspecial", 0f); Time.captureDeltaTime = 0f; Time.unscaledDeltaTime = 0f; Call(layout, "Update");
        Color baseColor = background.color;
        Check(m.engine.EspecialArmado(true) && SameColor(normal, baseColor), "armed result starts pulse at base background luminance");
        Time.captureDeltaTime = .65f; Call(layout, "Update"); Color rising = background.color;
        Time.captureDeltaTime = .65f; Call(layout, "Update"); Color peak = background.color;
        Check(peak.r > rising.r && rising.r > baseColor.r && peak.g > rising.g && rising.g > baseColor.g && peak.b > rising.b && rising.b > baseColor.b, "background brightens gradually to peak after 1.3 seconds while HIT or MISS waits");
        Check(baseColor.a == 1f && rising.a == 1f && peak.a == 1f, "pulse changes luminance without fading panel alpha");
        Time.captureDeltaTime = 1.3f; Call(layout, "Update");
        Check(SameColor(baseColor, background.color), "2.6 second pulse cycle returns to opaque base color");
        Contact(m, a); Call(layout, "Update");
        Check(m.engine.ChuteEspecialEmAndamento && SameColor(normal, background.color) && (float)Get(ficha, "tempoEsperaEspecial") == 0f, "next touch stops background pulse and resets its local waiting timer");
        m.engine.PararPartida(); Call(layout, "Update");
        Check((float)Get(ficha, "tempoEsperaEspecial") == 0f, "hidden panel keeps waiting pulse timer reset");
        Time.captureDeltaTime = 0f; Time.unscaledDeltaTime = .02f; Reset();
    }
}
static void WidePanelsAndFooter()
{
    caseName = "wide horizontal mirrored panels and reserved footer gap";
    var m = new Match(); m.Start();
    var layout = m.root.GetComponentsInChildren<LayoutJogadorUnicoUI>().First();
    var fichaA = Get(layout, "fichaA"); var fichaB = Get(layout, "fichaB");
    var cardA = (RectTransform)Get(fichaA, "retangulo"); var cardB = (RectTransform)Get(fichaB, "retangulo");
    var panelA = (RectTransform)Get(fichaA, "painelEspecial"); var panelB = (RectTransform)Get(fichaB, "painelEspecial");
    float cardAWidth = (cardA.anchorMax.x - cardA.anchorMin.x) * 1920f;
    float cardBWidth = (cardB.anchorMax.x - cardB.anchorMin.x) * 1920f;
    float aLeft = cardA.anchorMin.x * 1920f + panelA.anchorMin.x * cardAWidth;
    float aRight = cardA.anchorMin.x * 1920f + panelA.anchorMax.x * cardAWidth;
    float bLeft = cardB.anchorMin.x * 1920f + panelB.anchorMin.x * cardBWidth;
    float bRight = cardB.anchorMin.x * 1920f + panelB.anchorMax.x * cardBWidth;
    float aBottom = (cardA.anchorMin.y + panelA.anchorMin.y * (cardA.anchorMax.y - cardA.anchorMin.y)) * 1080f;
    float aTop = (cardA.anchorMin.y + panelA.anchorMax.y * (cardA.anchorMax.y - cardA.anchorMin.y)) * 1080f;
    float bBottom = (cardB.anchorMin.y + panelB.anchorMin.y * (cardB.anchorMax.y - cardB.anchorMin.y)) * 1080f;
    float bTop = (cardB.anchorMin.y + panelB.anchorMax.y * (cardB.anchorMax.y - cardB.anchorMin.y)) * 1080f;
    Check(aRight - aLeft >= 450f && bRight - bLeft >= 450f, "both special panels are substantially wider in 1920px layout");
    Check(aRight - aLeft > (aTop - aBottom) * 2.5f && bRight - bLeft > (bTop - bBottom) * 2.5f, "special panels extend horizontally");
    Check(Math.Abs(aLeft - (1920f - bRight)) < .1f && Math.Abs(aRight - (1920f - bLeft)) < .1f, "left and right expanded panels mirror across screen center");
    Check(aLeft >= cardA.anchorMax.x * 1920f && bRight <= cardB.anchorMin.x * 1920f, "expanded panels clear their external portrait cards");
    Check(aRight <= 800f && bLeft >= 1120f, "expanded panels leave central x800..1120 footer region unobstructed");
    Check(aLeft >= 0f && bRight <= 1920f && aBottom >= 0f && bBottom >= 0f, "panels remain inside broadcast viewport");
    Check(Math.Abs(aBottom - bBottom) < .1f && Math.Abs(aTop - bTop) < .1f && aTop < (8.3f - 5.2f) / (8.3f * 2f) * 1080f, "mirrored panels retain low vertical placement below arena");
    var wheelA = (RoletaCapitaoUI)Get(fichaA, "roleta"); var resultA = (TMPro.TextMeshProUGUI)Get(fichaA, "resultado");
    Check(resultA.transform.parent == ((RectTransform)Get(fichaA, "especial")) && wheelA.rectTransform.anchorMax.x - wheelA.rectTransform.anchorMin.x < .5f, "wider panel separates wheel region from result text");
    Reset();
}
static void CustomFooterOnly()
{
    caseName = "footer contains only explicitly enabled configured text";
    var m = new Match();
    foreach (bool legacyDetails in new[] { false, true })
    foreach (FormatoDiscos format in new[] { FormatoDiscos.Liga, FormatoDiscos.MataMata })
    foreach (bool titleEnabled in new[] { false, true })
    foreach (bool subtitleEnabled in new[] { false, true })
    {
        m.config.mostrarDetalhesFormato = legacyDetails; m.config.formato = format; m.config.discosPorTime = 5;
        m.config.tituloRodape = "My Custom Title"; m.config.subtituloRodape = "My custom subtitle";
        m.config.mostrarTituloRodape = titleEnabled; m.config.mostrarSubtituloRodape = subtitleEnabled;
        m.hud.Construir(m.config, m.ui, m.banner);
        var text = m.root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Where(t => t.gameObject.name == "Broadcast Signature" || t.gameObject.name == "Broadcast Format").ToArray();
        Check(text.Length == (titleEnabled ? 1 : 0) + (subtitleEnabled ? 1 : 0), "footer flags exclusively control configured text visibility");
        Check(text.All(t => t.text == (t.gameObject.name == "Broadcast Signature" ? "My Custom Title" : "My custom subtitle")), "footer preserves exact custom text for both legacy format-flag values");
        Check(text.All(t => !t.text.Contains("KNOCKOUT") && !t.text.Contains("LEAGUE") && !t.text.Contains(" v ")), "no automatic format or player count is added");
        Check(text.All(t => t.rectTransform.anchoredPosition.x >= 800f && t.rectTransform.sizeDelta.x <= 320f && t.rectTransform.anchoredPosition.x + t.rectTransform.sizeDelta.x <= 1120f), "configured footer fits reserved central gap between wide panels");
    }
    m.config.mostrarTituloRodape = m.config.mostrarSubtituloRodape = true;
    foreach (bool legacyDetails in new[] { false, true })
    foreach (string blank in new[] { "", "  ", null })
    {
        m.config.mostrarDetalhesFormato = legacyDetails; m.config.tituloRodape = m.config.subtituloRodape = blank;
        m.hud.Construir(m.config, m.ui, m.banner);
        Check(!m.root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Any(t => t.gameObject.name == "Broadcast Signature" || t.gameObject.name == "Broadcast Format"), "blank footer fields remain absent even if legacy format-details flag is true");
    }
    m.hud.Construir(null, m.ui, m.banner);
    Check(!m.root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Any(t => t.gameObject.name == "Broadcast Signature" || t.gameObject.name == "Broadcast Format"), "missing config does not create automatic footer fallback");
    Reset();
}
static void TickUntil(Func<bool> condition, string message, int limit = 400)
{
    for (int i = 0; !condition() && i < limit; i++) Scheduler.Tick(.05f);
    Check(condition(), message);
}
static void MixedGoalBanners()
{
    caseName = "normal-special-normal banner queue snapshots and style restoration";
    var m = new Match(); m.Start();
    m.banner.tempoDeExibicao = .4f; m.banner.intervaloEntreBanners = .05f;
    var panel = (RectTransform)m.banner.painelBanner.transform;
    panel.anchoredPosition += new Vector2(12f, -8f); panel.localScale = new Vector3(.93f, 1.04f, 1f);
    m.banner.fotoJogador.transform.localScale = new Vector3(.9f, 1.05f, 1f);
    var image = panel.GetComponent<UnityEngine.UI.Image>(); image.color = new Color(.04f, .11f, .18f, .92f);
    m.banner.fotoJogador.color = new Color(.83f, .91f, .98f, .96f); m.banner.textoDados.color = new Color(.95f, .94f, .93f, .97f);
    var group = panel.gameObject.AddComponent<CanvasGroup>(); group.alpha = .86f;
    Vector2 originalPosition = panel.anchoredPosition; Vector3 originalScale = panel.localScale, originalPhotoScale = m.banner.fotoJogador.transform.localScale;
    Color originalBackground = image.color, originalPhoto = m.banner.fotoJogador.color, originalText = m.banner.textoDados.color;
    var firstSprite = new Sprite { name = "normal first portrait" }; var specialSprite = new Sprite { name = "special second portrait" }; var lastSprite = new Sprite { name = "normal last portrait" };
    var first = new JogadorDados { nomeJogador = "First Normal", foto512x512 = firstSprite };
    var last = new JogadorDados { nomeJogador = "Last Normal", foto512x512 = lastSprite };
    Time.captureDeltaTime = .05f; Time.timeScale = 0f;
    m.banner.ExibirBannerDeGol(first, Color.blue);
    m.banner.ExibirBannerDeGolEspecial("Special Second", specialSprite, Color.red);
    m.banner.ExibirBannerDeGol(last, Color.green);
    first.nomeJogador = "Changed First"; first.foto512x512 = null; last.nomeJogador = "Changed Last"; last.foto512x512 = null;
    Check(m.banner.textoDados.text.Contains("FIRST NORMAL") && !m.banner.textoDados.text.Contains("SPECIAL GOAL") && m.banner.fotoJogador.sprite == firstSprite, "first normal goal keeps its snapshotted name and portrait");
    TickUntil(() => m.banner.painelBanner.activeSelf && m.banner.textoDados.text.Contains("SPECIAL GOAL!"), "queued special goal starts after preceding normal goal");
    Check(m.banner.textoDados.text.Contains("SPECIAL SECOND") && m.banner.fotoJogador.sprite == specialSprite, "special queue item carries distinct metadata and identity snapshot");
    Check(panel.gameObject.GetComponentsInChildren<Transform>(true).Any(t => t.gameObject.name == "Special_Goal_FX" && t.gameObject.activeInHierarchy), "special item owns an active distinct gold celebration overlay");
    Scheduler.Tick(.05f); Scheduler.Tick(.05f);
    Check(!SameColor(image.color, originalBackground) || panel.localScale != originalScale || Vector2.Distance(panel.anchoredPosition, originalPosition) > .1f, "special celebration visibly animates its panel style");
    TickUntil(() => m.banner.painelBanner.activeSelf && m.banner.textoDados.text.Contains("LAST NORMAL"), "normal goal queued after special starts normally");
    Check(!m.banner.textoDados.text.Contains("SPECIAL GOAL") && m.banner.fotoJogador.sprite == lastSprite, "last normal item does not inherit special status and keeps identity snapshot");
    Check(!panel.gameObject.GetComponentsInChildren<Transform>(true).Any(t => t.gameObject.name == "Special_Goal_FX" && t.gameObject.activeInHierarchy), "normal item after special has no active special overlay");
    Check(Vector2.Distance(panel.anchoredPosition, originalPosition) < .0001f && panel.localScale == originalScale && m.banner.fotoJogador.transform.localScale == originalPhotoScale, "normal after special restores panel/photo geometry");
    Check(SameColor(image.color, originalBackground) && SameColor(m.banner.fotoJogador.color, originalPhoto) && SameColor(m.banner.textoDados.color, originalText) && Math.Abs(group.alpha - .86f) < .0001f, "normal after special restores background, photo, text and opacity");
    TickUntil(() => !m.banner.TemBannersPendentes, "mixed queue completes while timeScale is zero");
    Check(!m.banner.painelBanner.activeSelf && panel.localScale == originalScale && SameColor(image.color, originalBackground), "completed mixed queue hides cleanly without special style leak");
    Time.captureDeltaTime = 0f; Time.timeScale = 1f; Reset();
}
static void BannerCancellationAndTiming()
{
    caseName = "special banner cancellation, short timing, disable and HUD rebuild";
    foreach (int rate in new[] { 0, 30, 60 })
    {
        var m = new Match(); m.Start();
        var panel = (RectTransform)m.banner.painelBanner.transform; Vector2 originalPosition = panel.anchoredPosition; Vector3 originalScale = panel.localScale;
        Color originalBackground = panel.GetComponent<UnityEngine.UI.Image>().color;
        m.banner.tempoDeExibicao = .1f; m.banner.intervaloEntreBanners = 0f;
        Time.captureDeltaTime = rate == 0 ? 0f : 1f / rate; Time.timeScale = 0f;
        m.banner.ExibirBannerDeGolEspecial("Short Special", null, Color.blue);
        Check(m.banner.TemBannersPendentes && m.banner.textoDados.text.Contains("SPECIAL GOAL!") && !m.banner.fotoJogador.enabled, "short special supports missing portrait and starts with English title");
        var duplicateOwner = new GameObject("duplicate goal banner owner"); duplicateOwner.SetActive(false);
        var duplicate = duplicateOwner.AddComponent<GoalBannerUI>(); duplicate.painelBanner = m.banner.painelBanner; duplicateOwner.SetActive(true);
        Check(duplicateOwner.destroyed && GoalBannerUI.Instance == m.banner && m.banner.painelBanner.activeSelf, "duplicate singleton destruction cannot hide the active owner's special banner");
        int expected = (int)Math.Ceiling(.1d / (rate == 0 ? .05d : 1d / rate));
        for (int i = 0; i < expected - 1; i++) Scheduler.Tick(.05f);
        Check(m.banner.painelBanner.activeSelf, "special duration does not end early at capture rate " + rate);
        for (int i = 0; i < 4; i++) Scheduler.Tick(.05f);
        Check(!m.banner.TemBannersPendentes && !m.banner.painelBanner.activeSelf && panel.localScale == originalScale && SameColor(panel.GetComponent<UnityEngine.UI.Image>().color, originalBackground), "short special finishes and restores base style at capture rate " + rate);
        m.banner.tempoDeExibicao = 1f;
        m.banner.ExibirBannerDeGolEspecial("Cancel Me", new Sprite(), Color.red); Scheduler.Tick(.05f);
        m.banner.ExibirBannerDeGol("Queued Normal", null, Color.blue); m.banner.LimparFila();
        Check(!m.banner.TemBannersPendentes && !panel.gameObject.activeSelf && panel.localScale == originalScale && Vector2.Distance(panel.anchoredPosition, originalPosition) < .0001f, "clearing special queue restores transforms and hides panel");
        Check(!panel.gameObject.GetComponentsInChildren<Transform>(true).Any(t => t.gameObject.name == "Special_Goal_FX"), "clearing queue destroys owned special overlay");
        m.banner.ExibirBannerDeGolEspecial("Disable Me", new Sprite(), Color.red); Scheduler.Tick(.05f); m.banner.enabled = false;
        Check(!m.banner.TemBannersPendentes && !panel.gameObject.activeSelf && SameColor(panel.GetComponent<UnityEngine.UI.Image>().color, originalBackground), "disabling component cancels special and restores normal colors");
        m.banner.enabled = true;
        m.banner.ExibirBannerDeGolEspecial("Rebuild Me", new Sprite(), Color.red); Scheduler.Tick(.05f);
        var oldPanel = m.banner.painelBanner; m.hud.Construir(m.config, m.ui, m.banner);
        Check(oldPanel.destroyed && !m.banner.TemBannersPendentes && m.banner.painelBanner != oldPanel && !m.banner.painelBanner.activeSelf, "HUD rebuild disposes active special panel and its queued work");
        m.banner.ExibirBannerDeGol("Normal After Rebuild", null, Color.blue);
        Check(m.banner.textoDados.text.Contains("NORMAL AFTER REBUILD") && !m.banner.textoDados.text.Contains("SPECIAL GOAL") && ((RectTransform)m.banner.painelBanner.transform).localScale == Vector3.one, "new HUD starts subsequent normal goal with unmodified style");
        m.banner.LimparFila(); Time.captureDeltaTime = 0f; Time.timeScale = 1f; Reset();
    }
    var ordinary = new Match(); ordinary.Start(); var captain = ordinary.engine.Atletas.First(x => x.Capitao && x.TimeA);
    ordinary.engine.Bola.RegistrarToque(captain); ordinary.Cross(false);
    Check(ordinary.ui.GolsA == 1 && ordinary.banner.textoDados.text.Contains("GOAL!") && !ordinary.banner.textoDados.text.Contains("SPECIAL GOAL!"), "ordinary captain goal continues to use normal goal banner");
    Reset();
}
public static void Main(){string dir=Path.Combine(Path.GetTempPath(),"disc-feature-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);Application.persistentDataPath=dir;try{Reset();Contacts();Roulette();Cancellation();OddsAndMesh();Cards();WaitingPulse();WidePanelsAndFooter();CustomFooterOnly();MixedGoalBanners();BannerCancellationAndTiming();Console.WriteLine("PASS "+checks+" feature assertions: contact dedupe, rapid recontacts, setpiece attribution, captain-only independent charge, armed HIT/MISS, next-distinct-touch launch, visible ball flight, one score, cancellation/reset lifecycle, overall odds, native renderer/mesh and English external panel, slower HIT/MISS and opaque waiting-only background pulse, wider mirrored panels/custom-only footer, decorated wheel sectors and distinct queued special-goal celebration lifecycle.");Console.WriteLine("Compiled actual runtime sources against the Unity/TMP facade. This does not simulate Unity native collision dispatch or GPU rendering.");}finally{Reset();Directory.Delete(dir,true);}}
}
