using System.Collections.Generic;
using MarbleBasket3D.Core;
using UnityEngine;

namespace MarbleBasket3D
{
    [DefaultExecutionOrder(-1000)]
    public sealed class ArenaBuilder3D : MonoBehaviour
    {
        public ArenaSettings3D settings;
        public MatchFinishedEvent3D onResult = new MatchFinishedEvent3D();
        public MarbleMatch3D Match { get; private set; }
        private ArenaResources3D resources;
        private Transform world;
        private Material steel, dark, gold, glass, wood, woodAlt, white, red, blue;
        private PhysicsMaterial tracks, ballPhysics;
        public const float DeckHalfWidth = 3.2f, DeckFront = -.65f, DeckBack = 3.45f;
        public const float SideGapStart = .10f, SideGapEnd = 2.75f;
        public const float FrontExitHalfWidth = 1.0f, ChuteExitWidth = 1.05f;
        public const float RotorCenterX = 1.35f, RotorCenterZ = 1.4f;
        public static readonly Vector3 Rim = new Vector3(5.4f, 3.7f, -2.3f);
        public static readonly Vector3 Seat = new Vector3(0, 1.48f, -1.42f);
        public static readonly Vector3 Muzzle = new Vector3(.16f, 1.85f, -1.46f);
        private void Awake()
        {
            if (settings == null) { Debug.LogError("Atribua Arena3D_Settings ao ArenaBuilder3D.", this); enabled = false; return; }
            var scope = GetComponent<SimulationEnvironment3D>(); if (scope == null) scope = gameObject.AddComponent<SimulationEnvironment3D>();
            scope.Acquire(settings);
            try { Build(); } catch (System.Exception e) { var incompleteWorld = transform.Find("GeneratedArena3D"); if (incompleteWorld != null) incompleteWorld.gameObject.SetActive(false); Debug.LogException(e, this); enabled = false; }
        }
        public MarbleMatch3D Build()
        {
            if (settings == null) throw new System.InvalidOperationException("Escolha as configurações da arena.");
            settings.Validate();
            Transform previous = transform.Find("GeneratedArena3D");
            if (previous != null) { previous.gameObject.SetActive(false); if (Application.isPlaying) Destroy(previous.gameObject); else DestroyImmediate(previous.gameObject); }
            world = Child("GeneratedArena3D", transform).transform; resources = world.gameObject.AddComponent<ArenaResources3D>();
            Materials();
            Match = world.gameObject.AddComponent<MarbleMatch3D>();
            var launcher = Child("Shared launcher", world).AddComponent<Launcher3D>();
            var returns = world.gameObject.AddComponent<ReturnSystem3D>();
            var signals = world.gameObject.AddComponent<ArenaSignals3D>();
            var rotors = CreateDuelRotors();
            Match.Configure(settings, launcher, returns, rotors, signals);
            launcher.Configure(Match); returns.Configure(Match); if (Application.isPlaying) signals.Configure(Match);
            Match.Finished += result => onResult.Invoke(result);
            Court(); BuildLauncher(launcher); EntryAndContest(launcher); Basket(); ReturnsVisuals(); LightingAndCamera();
            // Pairwise mirrored slots. Alternate which team is instantiated first in each pair.
            // Physics is released simultaneously; there is no per-team launch or scoring handicap.
            for (int i = 0; i < settings.marblesPerTeam; i++)
                for (int j = 0; j < 2; j++) CreateMarble((TeamId)((i + j) % 2), i + 1);
            world.gameObject.AddComponent<Recovery3D>().match = Match;
            world.gameObject.AddComponent<ArenaHud3D>().match = Match;
            world.gameObject.AddComponent<ArenaDiagnostics3D>().match = Match;
            return Match;
        }
        private void Materials()
        {
            steel = Mat("Brushed metal", new Color(.32f, .39f, .48f), .8f, .65f);
            dark = Mat("Frame", new Color(.025f, .04f, .065f), .65f, .4f);
            gold = Mat("Mechanism gold", new Color(.95f, .57f, .055f), .65f, .6f);
            glass = Mat("Transparent guides", new Color(.5f, .8f, 1, .16f), .15f, .85f, true);
            wood = Mat("Court maple", new Color(.48f, .24f, .08f), 0, .4f);
            woodAlt = Mat("Court alternate", new Color(.58f, .32f, .12f), 0, .4f);
            white = Mat("Court lines", new Color(.92f, .94f, .98f), .1f, .5f);
            red = Mat("Red marbles", settings.redColor, .45f, .95f);
            blue = Mat("Blue marbles", settings.blueColor, .45f, .95f);
            red.EnableKeyword("_EMISSION"); red.SetColor("_EmissionColor", settings.redColor * .25f);
            blue.EnableKeyword("_EMISSION"); blue.SetColor("_EmissionColor", settings.blueColor * .25f);
            tracks = new PhysicsMaterial("Marble tracks") { dynamicFriction = .08f, staticFriction = .1f, bounciness = .15f, frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Average };
            ballPhysics = new PhysicsMaterial("Marbles") { dynamicFriction = .08f, staticFriction = .1f, bounciness = settings.marbleBounce, frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Average };
            resources.Own(tracks); resources.Own(ballPhysics);
        }
        private Material Mat(string name, Color color, float metal, float smooth, bool transparent = false)
        {
            Shader shader = Shader.Find("Standard"); if (shader == null) throw new System.InvalidOperationException("Shader Standard indisponível. Use o pipeline Built-in para esta cena.");
            var m = new Material(shader) { name = name, color = color }; m.SetFloat("_Metallic", metal); m.SetFloat("_Glossiness", smooth);
            if (transparent)
            {
                m.SetFloat("_Mode", 3); m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0); m.EnableKeyword("_ALPHABLEND_ON"); m.renderQueue = 3000;
            }
            resources.Own(m); return m;
        }
        private static GameObject Child(string label, Transform parent)
        {
            var go = new GameObject(label); go.transform.SetParent(parent, false); return go;
        }
        private GameObject Box(string label, Vector3 p, Vector3 size, Material material, bool collision = true, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = label; go.transform.SetParent(parent != null ? parent : world, false);
            go.transform.position = p; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = material;
            var c = go.GetComponent<Collider>(); c.sharedMaterial = tracks;
            if (!collision) { c.enabled = false; if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
            return go;
        }
        private GameObject Rod(string label, Vector3 a, Vector3 b, float radius, Material material, bool collision = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name = label; go.transform.SetParent(world, false);
            go.transform.position = (a + b) * .5f; go.transform.rotation = Quaternion.FromToRotation(Vector3.up, b - a);
            go.transform.localScale = new Vector3(radius * 2, Vector3.Distance(a, b) * .5f, radius * 2); go.GetComponent<Renderer>().sharedMaterial = material;
            var c = go.GetComponent<Collider>(); c.sharedMaterial = tracks;
            if (!collision) { c.enabled = false; if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
            return go;
        }
        private void Track(string label, Vector3 a, Vector3 b, float width)
        {
            Quaternion q = Quaternion.LookRotation((b - a).normalized, Vector3.up);
            Vector3 up = q * Vector3.up, right = q * Vector3.right, center = (a + b) * .5f;
            float length = Vector3.Distance(a, b) + .12f;
            var floor = Box(label + " floor", center, new Vector3(width, .12f, length), glass); floor.transform.rotation = q;
            for (int side = -1; side <= 1; side += 2)
            {
                var wall = Box(label + " guide", center + right * side * width * .5f + up * .27f, new Vector3(.065f, .65f, length), glass); wall.transform.rotation = q;
                Rod(label + " rail", a + right * side * width * .5f + up * .58f, b + right * side * width * .5f + up * .58f, .035f, steel);
            }
        }
        private void SurfacePrism(string label, Vector3[] top, float depth, Material material)
        {
            int n=top.Length;var vertices=new Vector3[n*2];var triangles=new List<int>();
            for(int i=0;i<n;i++){vertices[i]=top[i];vertices[i+n]=top[i]-Vector3.up*depth;}
            for(int i=1;i<n-1;i++)triangles.AddRange(new[]{0,i,i+1,n,n+i+1,n+i});
            for(int i=0;i<n;i++){int j=(i+1)%n;triangles.AddRange(new[]{i,i+n,j+n,i,j+n,j});}
            var mesh=new Mesh{name=label};mesh.vertices=vertices;mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();resources.Own(mesh);
            var go=Child(label,world);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
            var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=mesh;collider.convex=true;collider.sharedMaterial=tracks;
        }
        private void ChuteGuard(string label,Vector3 a,Vector3 b)
        {
            Vector3 direction=b-a;direction.y=0;direction.Normalize();
            Vector3 half=Vector3.Cross(Vector3.up,direction)*.0325f;
            SurfacePrism(label,new[]{a+half+Vector3.up*.55f,b+half+Vector3.up*.55f,b-half+Vector3.up*.55f,a-half+Vector3.up*.55f},.65f,glass);
            Rod(label+" rail",a+Vector3.up*.55f,b+Vector3.up*.55f,.025f,steel);
        }
        private void OpenGravityChute(Vector3 lip)
        {
            Vector3 entry=new Vector3(0,DeckHeight(DeckFront)-.04f,DeckFront);
            Vector3 leftEntry=entry-Vector3.right*FrontExitHalfWidth,rightEntry=entry+Vector3.right*FrontExitHalfWidth;
            Vector3 leftExit=lip-Vector3.right*(ChuteExitWidth*.5f),rightExit=lip+Vector3.right*(ChuteExitWidth*.5f);
            SurfacePrism("Open drop chute floor",new[]{leftEntry,rightEntry,rightExit,leftExit},.12f,glass);
            ChuteGuard("Open drop chute guide",leftEntry,leftExit);ChuteGuard("Open drop chute guide",rightExit,rightEntry);
        }
        private void Court()
        {
            Box("Court foundation", new Vector3(0, -.18f, 0), new Vector3(20, .35f, 12), dark);
            for (int i = 0; i < 36; i++) Box("Maple plank", new Vector3(-9.7f + i * .55f, .015f, 0), new Vector3(.54f, .04f, 11.6f), i % 3 == 0 ? woodAlt : wood, false);
            Box("Playing floor", new Vector3(0, -.015f, 0), new Vector3(19.8f, .08f, 11.7f), wood);
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Court sideline", new Vector3(0, .045f, side * 5.3f), new Vector3(18, .01f, .045f), white, false);
                Box("Court baseline", new Vector3(side * 9, .045f, 0), new Vector3(.045f, .01f, 10.6f), white, false);
            }
            Box("Half court line", new Vector3(0, .05f, 0), new Vector3(.045f, .01f, 10.6f), white, false);
            CircleLine("Center circle", new Vector3(0, .055f, 0), 1.7f, white, .025f);
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Reservoir frame post", new Vector3(side * 6.6f, 4, 3.5f), new Vector3(.28f, 8, .28f), dark, false);
                Rod("Tower diagonal", new Vector3(side * 7.3f, .2f, 3.7f), new Vector3(side * 6, 8.7f, 3.7f), .065f, steel);
                Box("Side drop basin", new Vector3(side * 3.9f, 2.85f, 1.4f), new Vector3(1.5f, .15f, 3.2f), dark, false);
                Zone("Lateral fall", ZoneKind3D.Fall, new Vector3(side * 3.95f, 3.45f, 1.4f), new Vector3(1.5f, 1.15f, 3.25f));
            }
        }
        public float DeckHeight(float z) => 4.45f + Mathf.Tan(settings.deckSlopeDegrees * Mathf.Deg2Rad) * (z - 1.4f);
        private RotatingCross3D CreateDuelRotors()
        {
            var group = Child("Twin platform rotors", world);
            Rigidbody[] bodies = new Rigidbody[2];
            Quaternion tilt = Quaternion.Euler(-settings.deckSlopeDegrees,0,0);
            for(int sideIndex=0;sideIndex<2;sideIndex++)
            {
                int side=sideIndex==0?-1:1;
                var rotor=Child(side<0?"Left three-blade rotor":"Right three-blade rotor",group.transform);
                rotor.transform.position=new Vector3(side*RotorCenterX,DeckHeight(RotorCenterZ)+.20f,RotorCenterZ);
                rotor.transform.rotation=tilt;var rb=rotor.AddComponent<Rigidbody>();rb.isKinematic=true;bodies[sideIndex]=rb;
                float inner=.18f,outer=settings.duelRotorRadius;
                for(int i=0;i<3;i++)
                {
                    float angle=(side<0?i*Mathf.PI*2/3:Mathf.PI-i*Mathf.PI*2/3);
                    Vector3 localDirection=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    var paddle=Box("Duel rotor paddle "+side+" "+i,Vector3.zero,new Vector3(.30f,.20f,outer-inner),gold,true,rotor.transform);
                    paddle.transform.localPosition=localDirection*((inner+outer)*.5f);
                    paddle.transform.localRotation=Quaternion.LookRotation(localDirection);
                }
                // A small cap covers the axle, not the whole playing field.
                var hub=Box("Rotor axle cap",Vector3.zero,new Vector3(.35f,.27f,.35f),steel,false,rotor.transform);
                hub.transform.localPosition=Vector3.zero;hub.transform.localRotation=Quaternion.identity;
            }
            var driver=group.AddComponent<RotatingCross3D>();driver.ConfigurePair(Match,bodies[0],bodies[1]);return driver;
        }
        private void DeckGuard(string label,Vector3 a,Vector3 b)
        {
            float z=(a.z+b.z)*.5f;
            var guard=Box(label,new Vector3((a.x+b.x)*.5f,DeckHeight(z)+.25f,z),new Vector3(.06f,.52f,Vector3.Distance(a,b)),glass);
            guard.transform.rotation=Quaternion.LookRotation(b-a);
            Rod(label+" top rail",new Vector3(a.x,DeckHeight(a.z)+.52f,a.z),new Vector3(b.x,DeckHeight(b.z)+.52f,b.z),.025f,steel);
        }
        private void EntryAndContest(Launcher3D launcher)
        {
            var deckMaterial=Mat("Flat competition deck",new Color(.075f,.10f,.14f),.28f,.38f);
            Vector3 D(float x,float z)=>new Vector3(x,DeckHeight(z),z);
            SurfacePrism("Open duel deck",new[]{D(-DeckHalfWidth,DeckBack),D(DeckHalfWidth,DeckBack),D(DeckHalfWidth,SideGapStart),D(FrontExitHalfWidth,DeckFront),D(-FrontExitHalfWidth,DeckFront),D(-DeckHalfWidth,SideGapStart)},.16f,deckMaterial);
            DeckGuard("Rear deck guard",new Vector3(-DeckHalfWidth,0,DeckBack),new Vector3(DeckHalfWidth,0,DeckBack));
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*DeckHalfWidth;
                DeckGuard("Side rear guard",new Vector3(x,0,SideGapEnd),new Vector3(x,0,DeckBack));
                DeckGuard("Front exit guard",new Vector3(side*FrontExitHalfWidth,0,DeckFront),new Vector3(x,0,SideGapStart));
                Vector3 start=EntryStart(side),end=EntryEnd(side);
                Track(side<0?"Red entry":"Blue entry",start,end,1.35f);
                Vector3 center=Vector3.Lerp(start,end,.075f)+Vector3.up*.45f;
                for(int z=-1;z<=1;z+=2)Box("Reservoir transparent wall",center+Vector3.forward*z*.85f,new Vector3(1.8f,1.15f,.06f),glass);
                Box("Reservoir team accent",center+Vector3.up*.65f,new Vector3(1.9f,.06f,1.8f),side<0?red:blue,false);
                // Passive mirrored deflectors turn the ramp's incoming speed toward the rotors/front.
                Vector3 a=new Vector3(side*1.70f,0,3.25f),b=new Vector3(side*.45f,0,2.50f);
                DeckGuard("Entry deflector",a,b);
            }
            if (launcher.Continuous)
            {
                Vector3 drop = launcher.DropPoint;
                Vector3 forward = Rim - Seat; forward.y = 0; forward.Normalize();
                Vector3 axis = Vector3.Cross(Vector3.up,forward);
                Vector3 lip = drop - axis*.36f;
                OpenGravityChute(lip);
                // Parallel to the wheel plane: these walls remove the chute's axial drift
                // while leaving the rotating paddles and their radial exit unobstructed.
                for (int side=-1;side<=1;side+=2)
                {
                    // The rear wall ends below the inlet. The floor stops before the front
                    // wall, leaving more than a full marble diameter for the downward exit.
                    float height = side<0 ? .90f : 1.30f;
                    float centerY = side<0 ? -.51f : -.25f;
                    var guide = Box("Drop guide side",drop + axis * side * .34f + Vector3.up*centerY,new Vector3(.06f,height,1.05f),glass);
                    guide.transform.rotation = Quaternion.LookRotation(forward);
                }
                Zone("Queue sensor",ZoneKind3D.Queue,new Vector3(.15f,3.50f,-.9f),new Vector3(2.2f,1.9f,1.4f));
                return;
            }
            // Compatibility path for the V3 indexed wheel and the V2 lever.
            Funnel("Front exit funnel",new Vector3(0,0,-.70f),.72f,.245f,DeckHeight(DeckFront)-.02f,2.65f,false,true);
            Track("Single file queue",new Vector3(0,2.16f,1.65f),new Vector3(0,1.20f,-1.48f),.60f);
            // The feed tube opens at its bottom toward the existing queue, so it cannot become a gate.
            for(int side=-1;side<=1;side+=2)
                Box("Front feed side",new Vector3(side*.30f,2.20f,-.70f),new Vector3(.045f,.95f,.60f),glass);
            Box("Front feed rear",new Vector3(0,2.30f,-.39f),new Vector3(.60f,.72f,.045f),glass);
            Box("Front feed front",new Vector3(0,2.40f,-1.01f),new Vector3(.60f,.52f,.045f),glass);
            Zone("Queue sensor",ZoneKind3D.Queue,new Vector3(0,2.0f,.1f),new Vector3(.65f,1.6f,3.8f));
        }
        private static Vector3 EntryStart(int side)=>new Vector3(side*7.5f,8.5f,3.1f);
        private static Vector3 EntryEnd(int side)=>new Vector3(side*1.85f,4.9f,2.75f);
        private void BuildLauncher(Launcher3D launcher)
        {
            bool compact=launcher.Compact;
            Box("Launcher plinth",new Vector3(-.25f,.32f,-1.5f),compact?new Vector3(1.35f,.5f,1.0f):new Vector3(1.7f,.5f,1.35f),dark);
            launcher.seat = Seat; launcher.target = Rim;
            var lever = Child(compact?"Compact cross launcher":"Open lever",world); var body = lever.AddComponent<Rigidbody>();
            body.isKinematic=true;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            body.interpolation=RigidbodyInterpolation.Interpolate;
            launcher.ConfigureLever(body);
            Vector3 pivot=body.position;
            float length=launcher.EffectiveArmLength;
            // Small compound paddles leave the ball visible. There is no opaque wheel or entry gate.
            for(int blade=0;blade<(compact?4:1);blade++)
            {
                Quaternion turn=Quaternion.Euler(-blade*90,0,0);
                Vector3 direction=turn*Vector3.forward;
                var arm=Box("Catapult arm "+blade,Vector3.zero,compact?new Vector3(.16f,.10f,length):new Vector3(.20f,.14f,length),steel,true,lever.transform);
                arm.transform.localPosition=direction*(length*.5f);arm.transform.localRotation=turn;
                var paddle=Box("Catapult paddle "+blade,Vector3.zero,compact?new Vector3(.38f,.10f,.27f):new Vector3(.55f,.10f,.44f),gold,true,lever.transform);
                paddle.transform.localPosition=direction*length;paddle.transform.localRotation=turn;
            }
            var hub=Box("Launcher hub",Vector3.zero,new Vector3(.30f,.30f,.30f),gold,false,lever.transform);
            hub.transform.localPosition=Vector3.zero;hub.transform.localRotation=Quaternion.identity;
            var relay=lever.AddComponent<LeverContact3D>();relay.launcher=launcher;
            Rod("Lever axle",pivot+Vector3.forward*.36f,pivot-Vector3.forward*.36f,.11f,gold);
            for(int side=-1;side<=1;side+=2)
                Box("Lever support",new Vector3(pivot.x,pivot.y*.5f,pivot.z+side*.40f),new Vector3(.17f,pivot.y,.15f),dark,false);
            if (launcher.Continuous)
            {
                Vector3 below = launcher.DropPoint; below.y = .75f;
                Zone("Missed wheel return",ZoneKind3D.Fall,below,new Vector3(2.5f,.65f,1.5f));
                Box("Wheel catch tray",below-Vector3.up*.40f,new Vector3(2.5f,.08f,1.5f),dark,false);
                return;
            }
            // The short open trough gathers the next ball while the lever cycles.
            Box("Open loading floor",new Vector3(0,1.15f,-1.44f),new Vector3(.64f,.10f,.66f),steel);
            for(int side=-1;side<=1;side+=2)
                Box("Open loading side",new Vector3(side*.31f,1.34f,-1.43f),new Vector3(.04f,.28f,.66f),glass);
            Box("Loading lip",new Vector3(0,1.27f,-1.77f),new Vector3(.66f,.14f,.04f),steel);
            Zone("Loading cup sensor",ZoneKind3D.Loading,new Vector3(0,1.55f,-1.45f),new Vector3(.60f,.58f,.5f));
        }
        private void Basket()
        {
            // The board is behind the rim along the horizontal shot direction.
            Vector3 flight = Rim - Muzzle; flight.y = 0; flight.Normalize();
            Vector3 board = Rim + flight * .84f + Vector3.up * .50f;
            var backing = Box("Basket backboard", board, new Vector3(.10f, 1.65f, 1.85f), white); backing.transform.rotation = Quaternion.LookRotation(flight) * Quaternion.Euler(0, 90, 0);
            Rod("Basket post", board + Vector3.down * .2f + flight * .25f, new Vector3(board.x, .15f, board.z) + flight * .25f, .09f, dark);
            var hoop = Child("Rim scoring plane", world); hoop.transform.position = Rim;
            var sensor = hoop.AddComponent<BasketSensor3D>(); sensor.match = Match; sensor.innerRadius = .60f;
            const int count = 32; const float radius = .66f;
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2 / count, b = (i + 1) * Mathf.PI * 2 / count;
                Vector3 p = Rim + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius, q = Rim + new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)) * radius;
                // Capsule colliders approximate the tubular ring; no solid collider across the opening.
                var segment = Child("Physical rim segment", hoop.transform); segment.transform.position = (p + q) * .5f;
                segment.transform.rotation = Quaternion.FromToRotation(Vector3.up, q - p);
                var c = segment.AddComponent<CapsuleCollider>(); c.radius = .055f; c.height = Vector3.Distance(p, q) + .11f; c.sharedMaterial = tracks;
                Rod("Orange rim", p, q, .055f, gold);
            }
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2 / 12;
                Vector3 top = Rim + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .64f;
                Vector3 bottom = Rim + new Vector3(Mathf.Cos(a + .35f) * .45f, -.62f, Mathf.Sin(a + .35f) * .45f);
                Rod("Net weave", top, bottom, .009f, white);
            }
            for (int i = 1; i <= 3; i++) CircleLine("Net ring", Rim + Vector3.down * (.18f * i), .64f - .06f * i, white, .009f);
            Funnel("Basket collection", new Vector3(Rim.x, 0, Rim.z), 1.65f, .48f, 1.25f, .28f, false);
            Zone("Collector outlet", ZoneKind3D.Collector, new Vector3(Rim.x, .18f, Rim.z), new Vector3(1.05f, .55f, 1.05f));
        }
        private void Funnel(string label, Vector3 center, float outer, float inner, float topY, float bottomY, bool sideFalls, bool openRear = false)
        {
            const int segments = 20;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments, b = (i + 1) * Mathf.PI * 2 / segments;
                Vector3 P(float angle, float radius, float y) => center + new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
                Vector3[] vertices = { P(a, outer, topY), P(a, inner, bottomY), P(b, inner, bottomY), P(b, outer, topY), P(a, outer, topY - .10f), P(a, inner, bottomY - .10f), P(b, inner, bottomY - .10f), P(b, outer, topY - .10f) };
                var mesh = new Mesh { name = label + " wedge " + i }; mesh.vertices = vertices;
                mesh.triangles = new[] { 0,1,2,0,2,3,4,6,5,4,7,6,0,4,5,0,5,1,1,5,6,1,6,2,2,6,7,2,7,3,3,7,4,3,4,0 }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); resources.Own(mesh);
                var go = Child(label + " slope " + i, world); go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = glass;
                var collider = go.AddComponent<MeshCollider>(); collider.sharedMesh = mesh; collider.convex = true; collider.sharedMaterial = tracks;
                bool opening = (sideFalls && (i == 0 || i == segments - 1 || i == segments / 2 || i == segments / 2 - 1))
                    || (openRear && Mathf.Sin((a+b)*.5f) > .35f);
                if (!opening)
                {
                    Vector3 mid = P((a+b)*.5f, outer, topY + .23f);
                    var wall = Box(label + " outer guide", mid, new Vector3(outer * .33f, .48f, .055f), glass);
                    wall.transform.rotation = Quaternion.Euler(0, 90 - (a+b)*.5f * Mathf.Rad2Deg, 0);
                    Rod(label + " outer rail", P(a, outer, topY + .48f), P(b, outer, topY + .48f), .025f, steel);
                }
            }
        }
        private void CircleLine(string name, Vector3 center, float radius, Material mat, float thickness)
        {
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2 / 48, b = (i+1)*Mathf.PI*2/48;
                Rod(name, center + new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius, center + new Vector3(Mathf.Cos(b),0,Mathf.Sin(b))*radius, thickness, mat);
            }
        }
        private void ReturnsVisuals()
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 low = new Vector3(side * 8.5f, .7f, -3.7f), high = new Vector3(side * 8.5f, 8.8f, 3.4f);
                Rod("Visible return tube", low, high, .27f, glass);
                for (int i = 0; i <= 6; i++)
                {
                    Vector3 p = Vector3.Lerp(low, high, i / 6f);
                    Rod("Elevator support", p, new Vector3(p.x, .1f, p.z), .035f, dark);
                }
                Rod("Reservoir return connection", high, Home((side < 0 ? TeamId.Red : TeamId.Blue), 1), .27f, glass);
            }
            Rod("Basket return common tube",ReturnSystem3D.CollectorOutlet,ReturnSystem3D.CollectorFork,.24f,glass);
            for(int side=-1;side<=1;side+=2)
            {
                TeamId team=side<0?TeamId.Red:TeamId.Blue;
                Vector3 branch=ReturnSystem3D.CollectorBranch(team);
                Rod("Reservoir return branch",ReturnSystem3D.CollectorFork,branch,.24f,glass);
                Rod("Reservoir branch connection",branch,new Vector3(side*8.5f,.7f,-3.7f),.24f,glass);
                Rod("Return team stripe",ReturnSystem3D.CollectorFork+Vector3.up*.24f,branch+Vector3.up*.24f,.025f,side<0?red:blue);
            }
        }
        private void Zone(string label, ZoneKind3D kind, Vector3 center, Vector3 size)
        {
            var go = Child(label, world); go.transform.position = center;
            var collider = go.AddComponent<BoxCollider>(); collider.isTrigger = true; collider.size = size;
            var zone = go.AddComponent<MarbleZone3D>(); zone.kind = kind; zone.match = Match;
        }
        public static Vector3 Home(TeamId team, int number)
        {
            float side = team == TeamId.Red ? -1 : 1;
            Vector3 start = EntryStart((int)side), end = EntryEnd((int)side);
            float t = .035f + ((number - 1) / 2) * .07f;
            return Vector3.Lerp(start, end, t) + Vector3.up * .38f + Vector3.forward * ((number % 2 == 0) ? .24f : -.24f);
        }
        private void CreateMarble(TeamId team, int number)
        {
            GameObject go = settings.marblePrefab != null ? Instantiate(settings.marblePrefab) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = (team == TeamId.Red ? "Red" : "Blue") + " marble " + number; go.transform.SetParent(world, false);
            if (go.GetComponent<Rigidbody>() == null) go.AddComponent<Rigidbody>();
            if (go.GetComponent<SphereCollider>() == null) go.AddComponent<SphereCollider>();
            var ball = go.GetComponent<Marble3D>(); if (ball == null) ball = go.AddComponent<Marble3D>();
            var renderer = go.GetComponent<Renderer>(); if (renderer != null) renderer.sharedMaterial = team == TeamId.Red ? red : blue;
            ball.Configure(Match, team, number, Home(team, number), ballPhysics); Match.Register(ball);
        }
        private void LightingAndCamera()
        {
            var cameraGo = Child("Main Camera", world); cameraGo.tag = "MainCamera"; cameraGo.transform.position = settings.cameraPosition;
            cameraGo.transform.LookAt(settings.cameraTarget); var camera = cameraGo.AddComponent<Camera>(); camera.fieldOfView = settings.cameraFieldOfView;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.015f, .025f, .045f); camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            cameraGo.AddComponent<AudioListener>(); cameraGo.AddComponent<ArenaCamera3D>();
            var key = Child("Arena key light", world); key.transform.rotation = Quaternion.Euler(48, -35, 0);
            var light = key.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.25f; light.shadows = LightShadows.Soft;
            for (int side = -1; side <= 1; side += 2)
            {
                var rim = Child("Team floodlight", world); rim.transform.position = new Vector3(side * 6, 9, -2); rim.transform.LookAt(new Vector3(0, 4, 1));
                var l = rim.AddComponent<Light>(); l.type = LightType.Spot; l.range = 22; l.spotAngle = 80; l.intensity = 3.5f;
                l.color = side < 0 ? new Color(1, .65f, .65f) : new Color(.65f,.8f,1);
                Box("Floodlight housing", rim.transform.position, new Vector3(.8f,.2f,.3f), dark, false);
            }
        }
    }
}
