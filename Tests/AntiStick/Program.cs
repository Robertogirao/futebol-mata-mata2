using System;
using System.Reflection;
using UnityEngine;

static class Program
{
    static int checks;
    const float Dt=.02f;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static void Near(float actual,float expected,string message,float tolerance=.001f)=>Check(Math.Abs(actual-expected)<tolerance,message+": "+actual+" vs "+expected);
    static void Invoke(object o,string name,params object[] p)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,p);
    static T Read<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    sealed class Rig
    {
        public readonly GameObject ballGo=new GameObject(),helixGo=new GameObject();
        public readonly Rigidbody2D rb,hrb;
        public readonly Collider2D ball,helix;
        public readonly BallController player;
        public readonly RotatingKicker kicker;
        public readonly DesprenderBolaDaHelice release;
        public Vector3 local=new Vector3(.05f,1.55f,0);
        public Rig(bool compound=false)
        {
            Physics2D.Reset();Time.time=Time.unscaledTime=0;Time.fixedDeltaTime=Dt;Time.timeScale=1;
            rb=ballGo.AddComponent<Rigidbody2D>();player=ballGo.AddComponent<BallController>();ball=ballGo.AddComponent<Collider2D>();
            hrb=helixGo.AddComponent<Rigidbody2D>();hrb.bodyType=RigidbodyType2D.Kinematic;hrb.angularVelocity=250;
            kicker=helixGo.AddComponent<RotatingKicker>();helix=helixGo.AddComponent<Collider2D>();
            helixGo.transform.position=new Vector3(-6.56f,-1.09f,0);helixGo.transform.localScale=new Vector3(1.8375f,1.82f,1);
            if(compound){Child(ballGo).AddComponent<Collider2D>();Child(helixGo).AddComponent<Collider2D>();}
            release=ballGo.AddComponent<DesprenderBolaDaHelice>();Invoke(release,"Awake");Pose();
        }
        public static GameObject Child(GameObject parent){var go=new GameObject();go.transform.SetParent(parent.transform);return go;}
        public void Pose(){rb.position=helixGo.transform.TransformPoint(local);rb.linearVelocity=hrb.GetPointVelocity(rb.position);}
        public Collision2D Contact(Collider2D c=null)=>new Collision2D { collider=c??helix,normal=new Vector2(1,0) };
        public void Enter()=>Invoke(release,"OnCollisionEnter2D",Contact());
        public void Step(bool callback=true,bool exit=false,float distance=.01f,bool pose=true)
        {
            Time.unscaledTime+=Dt;Time.time+=Dt*Time.timeScale;
            helixGo.transform.localRotation+=250*Dt*Time.timeScale;
            if(pose)Pose();
            foreach(var b in ballGo.GetComponentsInChildren<Collider2D>(true))
                foreach(var h in helixGo.GetComponentsInChildren<Collider2D>(true))Physics2D.SetDistance(b,h,distance);
            if(callback)Invoke(release,"OnCollisionStay2D",Contact());
            if(exit)Invoke(release,"OnCollisionExit2D",Contact());
            Invoke(release,"Update");Invoke(release,"FixedUpdate");
        }
        public void Trap(){Enter();for(int i=0;i<75&&Physics2D.ignoreTrueCalls==0;i++)Step(i%5==0,i%5==1);Check(Physics2D.ignoreTrueCalls>0,"co-rotation should trigger a lease");}
    }
    static void Main()
    {
        // Regression: moving a long way in world space, and never keeping a
        // continuous collision for 1.2 seconds, still triggers a local-frame watch.
        {
            var r=new Rig();r.Enter();var initial=r.rb.position;
            for(int i=0;i<30;i++)r.Step(i%5==0,i%5==1);
            Check(Vector2.Distance(initial,r.rb.position)>1,"orbital world displacement is large");
            Check(Physics2D.ignoreTrueCalls==0,"short contact must not release");
            for(int i=0;i<45&&Physics2D.ignoreTrueCalls==0;i++)r.Step(i%5==0,i%5==1);
            Check(Physics2D.GetIgnoreCollision(r.ball,r.helix),"interrupted contact still releases");
            Check(r.rb.forceCount==1,"one controlled release impulse");
            var outward=r.rb.position-r.hrb.position;outward.Normalize();
            Check(Vector2.Dot(r.rb.linearVelocity-r.hrb.GetPointVelocity(r.rb.position),outward)>3.99f,"minimum outward relative speed");
            Near(Vector2.Distance(r.rb.position,r.helixGo.transform.TransformPoint(r.local)),0,"never teleport the ball");
        }
        {
            var r=new Rig();r.Enter();
            for(int i=0;i<100;i++){r.local.x+=.012f;r.Step();}
            Check(Physics2D.ignoreTrueCalls==0,"rolling relative to kicker resets the observation");
            Check(r.rb.forceCount==0,"normal relative progress has no rescue force");
        }
        {
            var r=new Rig();r.Enter();
            for(int i=0;i<165&&Physics2D.ignoreTrueCalls==0;i++)
            {
                r.local.x=(i%4<2)?.15f:-.15f;r.Step();
            }
            Check(Physics2D.ignoreTrueCalls>0,"maximum dwell releases a boot that keeps jittering beyond the local-progress radius");
            Check(Time.time>=2.99f&&Time.time<3.3f,"dwell failsafe waits its full three-second threshold");
        }
        {
            var r=new Rig();r.Enter();for(int i=0;i<10;i++)r.Step();
            for(int i=0;i<40;i++)r.Step(false,i==0,1f);
            Check(Read<RotatingKicker>(r.release,"heliceObservada")==null,"leaving proximity past grace clears observation");
            Check(Physics2D.ignoreTrueCalls==0,"ordinary hit and departure not released");
        }
        {
            var r=new Rig();r.Enter();for(int i=0;i<55;i++)r.Step();
            for(int i=0;i<7;i++)r.Step(false,i==0,.09f);
            Check(Physics2D.ignoreTrueCalls==0,"must not rescue a currently separated ball during grace");
            r.Step(true,false,.01f);
            Check(Physics2D.GetIgnoreCollision(r.ball,r.helix),"short gap preserves local-frame elapsed time");
        }
        {
            var r=new Rig();r.Enter();for(int i=0;i<30;i++)r.Step();
            float observed=Read<float>(r.release,"tempoDeContato");Time.timeScale=0;
            for(int i=0;i<80;i++)r.Step(false);
            Near(Read<float>(r.release,"tempoDeContato"),observed,"paused physics does not accumulate stuck time");
            Check(Physics2D.ignoreTrueCalls==0,"paused ball receives no release");
        }
        foreach(var type in new[]{RigidbodyType2D.Static,RigidbodyType2D.Kinematic})
        {
            var r=new Rig();r.rb.bodyType=type;r.Enter();for(int i=0;i<80;i++)r.Step();
            Check(r.rb.forceCount==0&&Physics2D.ignoreTrueCalls==0,"non-dynamic body ignored");
        }
        {
            var r=new Rig();r.player.jaMarcouGol=true;r.Enter();for(int i=0;i<80;i++)r.Step();
            Check(r.rb.forceCount==0&&Physics2D.ignoreTrueCalls==0,"scored ball is not altered");
        }
        {
            var r=new Rig();var wall=new GameObject().AddComponent<Collider2D>();
            Invoke(r.release,"OnCollisionEnter2D",r.Contact(wall));for(int i=0;i<80;i++)r.Step(false);
            Check(r.rb.forceCount==0&&Physics2D.ignoreTrueCalls==0,"walls are not observed as kickers");
        }
        {
            var r=new Rig();r.Trap();Time.timeScale=0;
            Time.unscaledTime+=.10f;Invoke(r.release,"Update");
            Check(Physics2D.GetIgnoreCollision(r.ball,r.helix),"lease is still active before its deadline");
            Time.unscaledTime+=.20f;Invoke(r.release,"Update");
            Check(!Physics2D.GetIgnoreCollision(r.ball,r.helix),"unscaled timeout restores collision even when paused");
            Check(Physics2D.ignoreFalseCalls==1,"lease restored once");
            Invoke(r.release,"Update");Check(Physics2D.ignoreFalseCalls==1,"expired leases not repeatedly restored");
        }
        foreach(var callback in new[]{"OnDisable","OnDestroy"})
        {
            var r=new Rig();r.Trap();Time.timeScale=0;Invoke(r.release,callback);
            Check(!Physics2D.GetIgnoreCollision(r.ball,r.helix),callback+" restores its collision lease during a freeze");
        }
        {
            var r=new Rig();r.Trap();Invoke(r.release,"OnApplicationPause",true);
            Check(!Physics2D.GetIgnoreCollision(r.ball,r.helix),"application pause restores lease");
        }
        {
            var r=new Rig();r.Trap();r.player.jaMarcouGol=true;Invoke(r.release,"Update");
            Check(!Physics2D.GetIgnoreCollision(r.ball,r.helix),"scored ball releases its ignore lease immediately");
        }
        {
            var r=new Rig();r.Trap();Physics2D.Disable(r.helix);Invoke(r.release,"Update");
            Check(!Physics2D.GetIgnoreCollision(r.ball,r.helix),"disabled colliders have no retained lease");
            r.helix.enabled=true;Invoke(r.release,"OnDisable");
            Check(!Physics2D.GetIgnoreCollision(r.ball,r.helix),"reenabled collider stays collidable");
        }
        {
            var r=new Rig();r.Trap();r.helix.destroyed=true;Invoke(r.release,"Update");Invoke(r.release,"OnDestroy");
            Check(true,"destroyed collider handles are safe to forget");
        }
        {
            var r=new Rig(true);
            var balls=r.ballGo.GetComponentsInChildren<Collider2D>(true);
            var boots=r.helixGo.GetComponentsInChildren<Collider2D>(true);
            var wall=new GameObject().AddComponent<Collider2D>();
            var foreignBody=Rig.Child(r.ballGo);foreignBody.AddComponent<Rigidbody2D>();var foreignBall=foreignBody.AddComponent<Collider2D>();
            var nested=Rig.Child(r.helixGo);nested.AddComponent<Rigidbody2D>();nested.AddComponent<RotatingKicker>();var foreignKicker=nested.AddComponent<Collider2D>();
            var trigger=Rig.Child(r.helixGo).AddComponent<Collider2D>();trigger.isTrigger=true;
            Invoke(r.release,"Awake");
            Physics2D.IgnoreCollision(balls[1],boots[1],true);int before=Physics2D.ignoreTrueCalls;
            r.Enter();for(int i=0;i<75&&Physics2D.ignoreTrueCalls==before;i++)r.Step();
            Check(Physics2D.ignoreTrueCalls-before==3,"compound colliders acquire exactly the three not-already-ignored pairs");
            foreach(var b in balls)foreach(var h in boots)Check(Physics2D.GetIgnoreCollision(b,h),"all ball/kicker collider pairs covered");
            Check(!Physics2D.GetIgnoreCollision(r.ball,wall),"wall remains collidable during escape");
            Check(!Physics2D.GetIgnoreCollision(foreignBall,r.helix),"collider attached to another ball body is excluded");
            Check(!Physics2D.GetIgnoreCollision(r.ball,foreignKicker),"nested independent kicker body is excluded");
            Check(!Physics2D.GetIgnoreCollision(r.ball,trigger),"trigger colliders stay untouched");
            Invoke(r.release,"OnDisable");
            Check(Physics2D.GetIgnoreCollision(balls[1],boots[1]),"an externally preignored pair is preserved");
            Check(!Physics2D.GetIgnoreCollision(balls[0],boots[0]),"owned compound ignore restored");
            Check(!Physics2D.GetIgnoreCollision(balls[0],boots[1]),"second owned compound ignore restored");
            Check(!Physics2D.GetIgnoreCollision(balls[1],boots[0]),"third owned compound ignore restored");
        }
        {
            var r=new Rig();r.Enter();
            for(int i=0;i<75&&!Physics2D.GetIgnoreCollision(r.ball,r.helix);i++)
            {
                Time.unscaledTime+=Dt;Time.time+=Dt;
                r.Pose();var radial=r.rb.position-r.hrb.position;radial.Normalize();
                r.rb.linearVelocity=r.hrb.GetPointVelocity(r.rb.position)+radial*8;
                Invoke(r.release,"OnCollisionStay2D",r.Contact());Invoke(r.release,"FixedUpdate");
            }
            Check(Physics2D.GetIgnoreCollision(r.ball,r.helix),"escape lease opens even if outward speed was already sufficient");
            Check(r.rb.forceCount==0,"sufficient relative exit velocity is not boosted");
        }
        foreach(float side in new[]{-.08f,.08f})
        {
            var r=new Rig();r.local=new Vector3(side,.4f);r.Pose();r.Trap();
            Vector2 lateral=r.helixGo.transform.right;
            Check(Vector2.Dot(r.rb.lastForce,lateral)*side>0,"shaft release is sideways toward nearest side");
        }
        foreach(float mass in new[]{.2f,1f,3f})
        {
            var r=new Rig();r.rb.mass=mass;r.Trap();var radial=r.rb.position-r.hrb.position;radial.Normalize();
            Near(Vector2.Dot(r.rb.linearVelocity-r.hrb.GetPointVelocity(r.rb.position),radial),4,"mass-independent exit speed");
        }
        {
            var r=new Rig();r.release.tempoSemColisao=99;r.Trap();Time.unscaledTime+=.76f;Invoke(r.release,"Update");
            Check(!Physics2D.GetIgnoreCollision(r.ball,r.helix),"configured ignore interval is capped");
        }
        {
            var r=new Rig();r.helix.enabled=false;r.Enter();for(int i=0;i<80;i++)r.Step();
            Check(Physics2D.ignoreTrueCalls==0,"inactive kicker collider is never ignored");
        }
        {
            var r=new Rig();r.Enter();
            for(int i=0;i<80;i++)
            {
                Time.time+=Dt;Time.unscaledTime+=Dt;r.Pose();
                Physics2D.SetDistance(r.ball,r.helix,.001f,false);
                Invoke(r.release,"OnCollisionStay2D",r.Contact());Invoke(r.release,"FixedUpdate");
            }
            Check(Physics2D.ignoreTrueCalls==0,"invalid geometry distance never triggers release");
        }
        Console.WriteLine("PASS "+checks+" assertions: actual release source compiled with deterministic Unity API/state stubs (not Unity physics).");
    }
}
