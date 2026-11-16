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
            var seesaw = CreateSeesaw();
            Match.Configure(settings, launcher, returns, seesaw, signals);
            launcher.Configure(Match); returns.Configure(Match); if (Application.isPlaying) signals.Configure(Match);
            Match.Finished += result => onResult.Invoke(result);
            Court(); EntryAndContest(); BuildLauncher(launcher); Basket(); ReturnsVisuals(); LightingAndCamera();
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
                Box("Fall catcher", new Vector3(side * 3.4f, 1.05f, 1.5f), new Vector3(1.4f, .15f, 2), dark, false);
                Zone("Lateral fall", ZoneKind3D.Fall, new Vector3(side * 3.4f, 1.8f, 1.5f), new Vector3(1.35f, 1.5f, 2.3f));
            }
        }
        private Seesaw3D CreateSeesaw()
        {
            Vector3 pivotPosition = new Vector3(0, 4.75f, 1.5f);
            var pedestal = Box("Seesaw support", pivotPosition - Vector3.up * .5f, new Vector3(.4f, 1, .4f), gold, false);
            var pivot = Child("Seesaw fixed pivot", world); pivot.transform.position = pivotPosition; var fixedBody = pivot.AddComponent<Rigidbody>(); fixedBody.isKinematic = true;
            var beam = Box("Physical seesaw", pivotPosition, new Vector3(4.4f, .14f, 1.05f), gold);
            beam.transform.rotation = Quaternion.Euler(7, 0, 0);
            var rb = beam.AddComponent<Rigidbody>(); rb.mass = 1.8f; rb.angularDamping = .25f; rb.centerOfMass = new Vector3(0, -.06f, 0); rb.isKinematic = true;
            var hinge = beam.AddComponent<HingeJoint>(); hinge.connectedBody = fixedBody; hinge.anchor = Vector3.zero;
            hinge.autoConfigureConnectedAnchor = false; hinge.connectedAnchor = Vector3.zero; hinge.axis = Vector3.forward;
            hinge.useLimits = true; hinge.limits = new JointLimits { min = -18, max = 18 };
            var mechanism = beam.AddComponent<Seesaw3D>(); mechanism.Configure(rb); return mechanism;
        }
        private void EntryAndContest()
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 start = new Vector3(side * 7.5f, 8.5f, 3.1f), end = new Vector3(side * 1.65f, 4.9f, 1.5f);
                Track(side < 0 ? "Red entry" : "Blue entry", start, end, 1.35f);
                // Transparent reservoir shell around the upper part of the same continuous ramp.
                Vector3 center = Vector3.Lerp(start, end, .075f) + Vector3.up * .45f;
                for (int z = -1; z <= 1; z += 2) Box("Reservoir transparent wall", center + Vector3.forward * z * .85f, new Vector3(1.8f, 1.15f, .06f), glass);
                Box("Reservoir team accent", center + Vector3.up * .65f, new Vector3(1.9f, .06f, 1.8f), side < 0 ? red : blue, false);
            }
            Funnel("Contest funnel", new Vector3(0, 0, 1.5f), 2.75f, .245f, 4.35f, 2.65f, true);
            Track("Single file queue", new Vector3(0, 2.16f, 1.65f), new Vector3(0, 1.20f, -1.48f), .60f);
            Zone("Queue sensor", ZoneKind3D.Queue, new Vector3(0, 2.0f, .1f), new Vector3(.65f, 1.6f, 3.8f));
        }
        private void BuildLauncher(Launcher3D launcher)
        {
            Vector3 basePosition = new Vector3(0, .65f, -1.5f);
            Box("Launcher plinth", basePosition - Vector3.up * .3f, new Vector3(1.4f, .4f, 1.3f), dark);
            Box("Launcher cradle", basePosition + Vector3.up * .08f, new Vector3(.7f, .55f, .8f), gold, false);
            launcher.seat = Seat; launcher.muzzle = Muzzle; launcher.target = Rim;
            Transform barrel = Child("Oscillating barrel", world).transform; barrel.position = Seat - Vector3.up * .13f;
            Vector3 direction = (Rim - Muzzle); direction.y = 0; direction = direction.normalized * Mathf.Cos(settings.launchAngle * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(settings.launchAngle * Mathf.Deg2Rad);
            barrel.rotation = Quaternion.LookRotation(direction); launcher.barrel = barrel;
            var tube = GameObject.CreatePrimitive(PrimitiveType.Cylinder); tube.name = "Barrel visual"; tube.transform.SetParent(barrel, false);
            tube.transform.localRotation = Quaternion.Euler(90, 0, 0); tube.transform.localPosition = new Vector3(0, 0, .12f); tube.transform.localScale = new Vector3(.43f, .28f, .43f);
            tube.GetComponent<Renderer>().sharedMaterial = steel; var tc = tube.GetComponent<Collider>(); tc.enabled = false; if (Application.isPlaying) Destroy(tc); else DestroyImmediate(tc);
            // Open seating cup: a floor and two low sides, with an unobstructed upward firing path.
            Box("Cup floor", new Vector3(0, 1.15f, -1.44f), new Vector3(.58f, .1f, .58f), steel);
            for (int s = -1; s <= 1; s += 2) Box("Cup guide", new Vector3(s * .28f, 1.38f, -1.45f), new Vector3(.05f, .36f, .56f), glass);
            Box("Cup back", new Vector3(0, 1.37f, -1.72f), new Vector3(.6f, .35f, .06f), steel);
            var gate = Child("Queue gate", world); gate.transform.position = new Vector3(0, 1.83f, -1.06f);
            var collider = gate.AddComponent<BoxCollider>(); collider.size = new Vector3(.57f, .60f, .07f); launcher.gate = collider;
            var gateMesh = Box("Gate visual", gate.transform.position, new Vector3(.57f, .60f, .07f), gold, false, gate.transform); launcher.gateVisual = gateMesh.transform;
            Zone("Loading cup sensor", ZoneKind3D.Loading, new Vector3(0, 1.55f, -1.45f), new Vector3(.60f, .58f, .5f));
        }
        private void Basket()
        {
            // The board is behind the rim along the horizontal shot direction.
            Vector3 flight = Rim - Muzzle; flight.y = 0; flight.Normalize();
            Vector3 board = Rim + flight * .84f + Vector3.up * .50f;
            var backing = Box("Basket backboard", board, new Vector3(.10f, 1.65f, 1.85f), white); backing.transform.rotation = Quaternion.LookRotation(flight) * Quaternion.Euler(0, 90, 0);
            Rod("Basket post", board + Vector3.down * .2f + flight * .25f, new Vector3(board.x, .15f, board.z) + flight * .25f, .09f, dark);
            var hoop = Child("Rim scoring plane", world); hoop.transform.position = Rim;
            var sensor = hoop.AddComponent<BasketSensor3D>(); sensor.match = Match; sensor.innerRadius = .495f;
            const int count = 32; const float radius = .55f;
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
                Vector3 top = Rim + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .53f;
                Vector3 bottom = Rim + new Vector3(Mathf.Cos(a + .35f) * .35f, -.62f, Mathf.Sin(a + .35f) * .35f);
                Rod("Net weave", top, bottom, .009f, white);
            }
            for (int i = 1; i <= 3; i++) CircleLine("Net ring", Rim + Vector3.down * (.18f * i), .53f - .05f * i, white, .009f);
            Funnel("Basket collection", new Vector3(Rim.x, 0, Rim.z), 1.65f, .28f, 1.25f, .28f, false);
            Zone("Collector outlet", ZoneKind3D.Collector, new Vector3(Rim.x, .18f, Rim.z), new Vector3(.64f, .48f, .64f));
        }
        private void Funnel(string label, Vector3 center, float outer, float inner, float topY, float bottomY, bool sideFalls)
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
                bool opening = sideFalls && (i == 0 || i == segments - 1 || i == segments / 2 || i == segments / 2 - 1);
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
            Vector3[] chance = { new Vector3(5.4f, .25f, -2.3f), new Vector3(4.9f, .65f, -3.6f), new Vector3(1.1f,.65f,-3.6f), new Vector3(1.1f,2.7f,1.7f), new Vector3(0,2.45f,1.65f) };
            for (int i = 1; i < chance.Length; i++) Rod("Second chance return tube", chance[i-1], chance[i], .24f, glass);
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
            Vector3 start = new Vector3(side * 7.5f, 8.5f, 3.1f), end = new Vector3(side * 1.65f, 4.9f, 1.5f);
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
