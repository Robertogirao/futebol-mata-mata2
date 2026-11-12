using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PokemonMarbleRace;
using UnityEngine;

public static class Harness
{
    private static int checks;
    private static void Check(bool value, string name)
    {
        checks++;
        if (!value) throw new Exception("FAILED: " + name);
    }
    private static bool Near(float a, float b) { return Math.Abs(a - b) < 0.002f; }
    private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    private static bool Active(Component value) { return !value.destroyed && value.gameObject.activeInHierarchy; }

    private static float DistanceFromBox(Vector2 point, BoxCollider2D box)
    {
        Vector2 delta = point - (Vector2)box.transform.position;
        float angle = -box.transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
        float x = delta.x * Mathf.Cos(angle) - delta.y * Mathf.Sin(angle);
        float y = delta.x * Mathf.Sin(angle) + delta.y * Mathf.Cos(angle);
        float halfX = Math.Abs(box.size.x * box.transform.lossyScale.x) * 0.5f;
        float halfY = Math.Abs(box.size.y * box.transform.lossyScale.y) * 0.5f;
        float dx = Math.Max(0f, Math.Abs(x) - halfX);
        float dy = Math.Max(0f, Math.Abs(y) - halfY);
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    private static void CheckRoadClearance(List<RacePoint> points, BoxCollider2D[] walls, string label)
    {
        float length = RacePolyline.Length(points);
        float minimum = float.MaxValue;
        // The closed grid back wall intentionally begins at distance0.
        for (float d = 0.55f; d < length - 0.1f; d += 0.3f)
        {
            RacePoint point = RacePolyline.Sample(points, d);
            foreach (BoxCollider2D wall in walls)
                minimum = Math.Min(minimum, DistanceFromBox(new Vector2(point.x, point.y), wall));
        }
        Check(minimum > 0.30f, label + " centerline remains passable by default0.55marble (clearance " + minimum + ")");
    }

    private static void ValidateBuiltCourse(RaceTrackView view, RaceCourse course)
    {
        MeshFilter[] filters = view.GetComponentsInChildren<MeshFilter>(true).Where(Active).ToArray();
        Check(filters.Length >= course.segments.Count + 3, "road strokes and all surface sections built");
        bool finiteGeometry = filters.All(f => f.sharedMesh != null && f.sharedMesh.vertices.Length >= 4 &&
            f.sharedMesh.vertices.All(v => Finite(v.x) && Finite(v.y) && Math.Abs(v.x) < 16f && Math.Abs(v.y) < 9f));
        Check(finiteGeometry, "road meshes have finite camera-sized geometry");
        Check(filters.All(f => f.sharedMesh.triangles.All(index => index >= 0 && index < f.sharedMesh.vertices.Length)), "mesh indices reference existing vertices");
        Check(filters.All(f => f.GetComponent<MeshRenderer>().sharedMaterial != null), "road meshes have sprite material");

        BoxCollider2D[] boxes = view.GetComponentsInChildren<BoxCollider2D>(true).Where(Active).ToArray();
        BoxCollider2D[] walls = boxes.Where(c => c.gameObject.name.Contains("Rail") || c.gameObject.name.Contains("Back Wall")).ToArray();
        Check(walls.Length > 220, "physical perimeter colliders generated");
        Check(walls.All(w => Near(w.transform.localScale.y, 0.12f) && w.sharedMaterial != null && !w.isTrigger), "walls are physical with consistent thickness and bounce material");
        CheckRoadClearance(course.points, walls, "main road");
        foreach (RaceShortcut shortcut in course.shortcuts) CheckRoadClearance(shortcut.points, walls, "shortcut" + shortcut.id);

        Collider2D[] all = view.GetComponentsInChildren<Collider2D>(true).Where(Active).ToArray();
        Check(view.ObstacleColliders.Count > 0, "physical obstacle list exported");
        Check(view.ObstacleColliders.All(c => !c.isTrigger && !c.gameObject.name.Contains("Rail") && !c.gameObject.name.Contains("Back Wall")), "ghost obstacle list excludes walls and triggers");
        Check(view.ObstacleColliders.Distinct().Count() == view.ObstacleColliders.Count, "ghost list has no duplicate colliders");
        Check(view.ObstacleColliders.All(c => all.Contains(c)), "exported obstacle references belong to active course");

        SpriteRenderer[] sprites = view.GetComponentsInChildren<SpriteRenderer>(true).Where(Active).ToArray();
        Check(sprites.Count(s => s.gameObject.name.Contains("Checker")) == 32, "start and finish checkerboards generated");
        Check(sprites.Count(s => s.gameObject.name == "Direction Arrow") > 8, "direction arrows generated");
        Check(sprites.All(s => s.sprite != null && s.sharedMaterial != null), "every generated sprite has asset and material");
        Check(sprites.Where(s => s.gameObject.name == "Spinner Hub").All(s => Near(s.bounds.size.x, 0.28f)), "spinner hub drawn in world units");

        RaceObstacleMotion[] motions = view.GetComponentsInChildren<RaceObstacleMotion>(true).Where(Active).ToArray();
        Check(motions.Length == course.obstacles.Count(o => o.kind != RaceObstacleKind.Bumper), "animated object count matches obstacle definitions");
        foreach (RaceObstacleMotion motion in motions)
        {
            string kind = motion.gameObject.name;
            if (kind == "Spinner")
            {
                Rigidbody2D body = motion.GetComponent<Rigidbody2D>();
                Check(body != null && body.bodyType == RigidbodyType2D.Kinematic, "spinner has kinematic body");
                float a = body.rotation;
                view.SetElapsed(1.1f);
                Check(!Near(a, body.rotation), "spinner actually rotates from elapsed course time");
            }
            if (kind == "MovingGate")
            {
                Rigidbody2D body = motion.GetComponent<Rigidbody2D>();
                view.SetElapsed(0f); Vector2 a = body.position;
                view.SetElapsed(1.75f);
                Check(Vector2.Distance(a, body.position) > 0.01f, "gate actually moves using kinematic body");
            }
            if (kind == "Snorlax")
            {
                Collider2D collider = motion.GetComponent<Collider2D>();
                bool opened = false, closed = false;
                for (int i = 0; i < 100; i++)
                {
                    view.SetElapsed(i * 0.25f);
                    opened |= !collider.enabled;
                    closed |= collider.enabled;
                }
                Check(opened && closed, "Snorlax alternates closed and passable collider states");
            }
            if (kind == "PushPad") Check(motion.GetComponent<CircleCollider2D>().isTrigger, "push pad is a trigger, not a blocking wall");
        }
        view.SetElapsed(float.NaN);
        view.SetElapsed(float.PositiveInfinity);
        Check(true, "invalid elapsed values ignored safely");
    }

    public static void Main()
    {
        Application.isPlaying = true;
        GameObject holder = new GameObject("CourseViewTest");
        RaceTrackView view = holder.AddComponent<RaceTrackView>();
        int shortcutCourses = 0;
        var seenKinds = new HashSet<RaceObstacleKind>();
        for (int seed = 0; seed < 6; seed++)
        {
            RaceCourse course = RaceCourseGenerator.Generate(seed);
            foreach (RaceObstacle obstacle in course.obstacles) seenKinds.Add(obstacle.kind);
            view.Build(course, null, null, null);
            ValidateBuiltCourse(view, course);
            shortcutCourses += course.shortcuts.Count > 0 ? 1 : 0;
            List<UnityEngine.Object> resources = ((List<UnityEngine.Object>)typeof(RaceTrackView).GetField("owned", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view)).ToList();
            Check(resources.Count >= 6, "runtime meshes and sprites tracked for disposal");
            view.Clear();
            Check(resources.All(r => r.destroyed), "Clear destroys owned meshes, materials, sprites and textures");
            Check(view.GetComponentsInChildren<Collider2D>(true).Length == 0 && view.ObstacleColliders.Count == 0, "Clear removes collision world and ghost references");
        }
        Check(shortcutCourses > 0, "suite covers real generated branch geometry");
        Check(seenKinds.Count == 5, "suite covers all five obstacle kinds");
        view.Build(null, null, null, null);
        Check(view.GetComponentsInChildren<Collider2D>(true).Length == 0, "empty course safe with no collision obstacles");
        view.Clear();
        TMPro.TMP_FontAsset font = TMPro.TMP_Settings.defaultFontAsset;
        TMPro.TMP_Settings.defaultFontAsset = null;
        view.Build(RaceCourseGenerator.Generate(815), null, null, null);
        Check(view.ObstacleColliders.Count > 0, "missing default font does not suppress track physics");
        view.Clear();
        TMPro.TMP_Settings.defaultFontAsset = font;
        Console.WriteLine("PASS road meshes, physical walls, main/shortcut passage, obstacle lists and elapsed animation");
        Console.WriteLine("PASS resource cleanup, world-unit sizes, missing-font fallback and empty-course safety");
        Console.WriteLine("PASS " + checks + " assertions using real RaceCourse, RaceTrackView and RaceObstacleMotion with Unity contract facade");
        Console.WriteLine("Scope: no Unity Editor, native import, rendering or collision solver was executed.");
    }
}
