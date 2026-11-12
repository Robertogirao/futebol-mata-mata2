using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PokemonMarbleRace;

public static class Harness
{
    private static int assertions;
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
    public static void Main()
    {
        TestGeometryHelpers();
        TestGeneratedCourses();
        TestProjection();
        TestValidation();
        Console.WriteLine("PASS " + assertions + " assertions using real Unity-independent course model and generator");
    }
    private static void Check(bool value, string label)
    {
        assertions++;
        if (!value) throw new Exception("FAILED: " + label);
    }
    private static void Close(float actual, float expected, float tolerance, string label)
    { Check(Math.Abs(actual - expected) <= tolerance, label + " got " + actual + ", expected " + expected); }
    private static void PointClose(RacePoint actual, RacePoint expected, string label)
    { Close(actual.x, expected.x, .002f, label + " x"); Close(actual.y, expected.y, .002f, label + " y"); }
    private static void Throws(Action action, string label)
    {
        bool threw = false;
        try { action(); } catch (InvalidOperationException) { threw = true; }
        Check(threw, label);
    }
    private static string Canonical(RaceCourse course)
    {
        var b = new StringBuilder();
        b.Append(course.seed).Append(':').Append(course.width.ToString("R", Culture));
        foreach (var p in course.points) b.Append('|').Append(p.x.ToString("R", Culture)).Append(',').Append(p.y.ToString("R", Culture));
        foreach (var s in course.segments) b.Append('|').Append(s.index).Append(',').Append(s.label).Append(',').Append(s.surface).Append(',').Append(s.startDistance.ToString("R", Culture)).Append(',').Append(s.endDistance.ToString("R", Culture));
        foreach (var o in course.obstacles) b.Append('|').Append(o.segmentIndex).Append(',').Append(o.kind).Append(',').Append(o.position.x.ToString("R", Culture)).Append(',').Append(o.position.y.ToString("R", Culture)).Append(',').Append(o.angle.ToString("R", Culture)).Append(',').Append(o.speed.ToString("R", Culture)).Append(',').Append(o.size.ToString("R", Culture)).Append(',').Append(o.phase.ToString("R", Culture));
        foreach (var s in course.shortcuts)
        {
            b.Append('|').Append(s.id).Append(',').Append(s.startDistance.ToString("R", Culture)).Append(',').Append(s.endDistance.ToString("R", Culture));
            foreach (var p in s.points) b.Append(',').Append(p.x.ToString("R", Culture)).Append(',').Append(p.y.ToString("R", Culture));
        }
        return b.ToString();
    }
    private static void TestGeometryHelpers()
    {
        var points = new List<RacePoint> { new RacePoint(0, 0), new RacePoint(3, 0), new RacePoint(3, 4) };
        Close(RacePolyline.Length(points), 7f, .0001f, "arc length");
        PointClose(RacePolyline.Sample(points, 2f), new RacePoint(2, 0), "sample first segment");
        PointClose(RacePolyline.Sample(points, 5f), new RacePoint(3, 2), "sample second segment");
        PointClose(RacePolyline.Sample(points, -8f), new RacePoint(0, 0), "sample clamps start");
        PointClose(RacePolyline.Sample(points, 100f), new RacePoint(3, 4), "sample clamps finish");
        PointClose(RacePolyline.Tangent(points, 2f), new RacePoint(1, 0), "horizontal tangent");
        PointClose(RacePolyline.Tangent(points, 3f), new RacePoint(0, 1), "corner uses next tangent");
        PointClose(RacePolyline.Tangent(points, 100f), new RacePoint(0, 1), "finish tangent");
        Close(RacePolyline.Project(points, new RacePoint(3.5f, 2f), 4f), 5f, .0001f, "normal projection");
        Close(RacePolyline.Project(points, new RacePoint(3, 4), 0f, 1f), 1f, .0001f, "projection respects forward cap");
        Close(RacePolyline.Project(points, new RacePoint(0, 0), 7f), 5f, .0001f, "projection respects backward cap");
        Close(RacePolyline.Project(points, new RacePoint(float.NaN, 0), 3f), 3f, .0001f, "invalid position preserves cursor");
        Close(RacePolyline.Length(null), 0f, .0001f, "null geometry length");
        PointClose(RacePolyline.Sample(new List<RacePoint>(), 4), new RacePoint(), "empty geometry sample");
        PointClose(RacePolyline.Tangent(new List<RacePoint>(), 4), new RacePoint(1, 0), "empty geometry tangent");
        var duplicate = new List<RacePoint> { new RacePoint(), new RacePoint(), new RacePoint(1, 0) };
        Close(RacePolyline.Length(duplicate), 1f, .0001f, "helpers tolerate duplicate zero-length segment");
        PointClose(RacePolyline.Sample(duplicate, .5f), new RacePoint(.5f, 0), "sample skips zero segment");
        Console.WriteLine("PASS arc-length, samples, normalized tangents and bounded projection");
    }
    private static void TestGeneratedCourses()
    {
        var signatures = new HashSet<string>();
        var surfaces = new HashSet<RaceSurface>();
        var obstacleKinds = new HashSet<RaceObstacleKind>();
        int shortcuts = 0, courses = 0;
        foreach (int blocks in new[] { 3, 6, 9, 12, 24 })
        foreach (float width in new[] { 1.8f, 2.4f, 2.6f })
        for (int seed = -25; seed < 25; seed++)
        {
            RaceCourse course = RaceCourseGenerator.Generate(seed, blocks, width);
            RaceCourseGenerator.Validate(course);
            courses++;
            Check(Canonical(course) == Canonical(RaceCourseGenerator.Generate(seed, blocks, width)), "same seed exactly reproduces geometry, order, obstacles and bypass");
            Close(course.width, width, .0001f, "configured width retained");
            Check(course.segments.Count == blocks + 2, "exact block count plus two U turns");
            Check(course.Length >= 69f && course.Length <= 77f, "playable length for target 40-50 second race");
            PointClose(course.Sample(0f), new RacePoint(-10f, 4f), "course start");
            PointClose(course.Sample(course.Length), new RacePoint(12f, -4f), "course finish");
            foreach (RacePoint p in course.points)
            {
                Check(p.x >= -10.001f && p.x <= 12.001f && p.y >= -4.301f && p.y <= 4.301f, "every center point inside camera course area");
            }
            float end = 0f;
            foreach (var segment in course.segments)
            {
                Close(segment.startDistance, end, .002f, "connected segment intervals");
                float center = (segment.startDistance + segment.endDistance) * .5f;
                Check(course.SurfaceAt(center) == segment.surface && course.LabelAt(center) == segment.label, "surface and label lookup by arc distance");
                surfaces.Add(segment.surface);
                end = segment.endDistance;
            }
            Close(end, course.Length, .002f, "all blocks cover finish");
            foreach (var obstacle in course.obstacles)
            {
                obstacleKinds.Add(obstacle.kind);
                RaceSegment segment = course.segments[obstacle.segmentIndex];
                float distance = RacePolyline.Project(course.points, obstacle.position, (segment.startDistance + segment.endDistance) * .5f,
                    segment.endDistance - segment.startDistance);
                Check(distance >= 6.49f, "first 6.5 units clear for maximum start grid");
                Check(obstacle.size <= width * .6f, "obstacles leave clearance and never permanently fill road");
                Check(distance <= course.Length - .5f, "finish remains clear");
            }
            foreach (var shortcut in course.shortcuts)
            {
                shortcuts++;
                PointClose(shortcut.points[0], course.Sample(shortcut.startDistance), "bypass entrance joins centerline");
                PointClose(shortcut.points[shortcut.points.Count - 1], course.Sample(shortcut.endDistance), "bypass exit joins centerline");
                Check(shortcut.endDistance > shortcut.startDistance && shortcut.endDistance < course.Length, "bypass has ordered mapped progress");
                Check(RacePolyline.Length(shortcut.points) > 1f, "bypass contains a real drivable route");
            }
            if (blocks == 9 && Math.Abs(width - 2.4f) < .01f) signatures.Add(Canonical(course));
        }
        Check(signatures.Count == 50, "fifty tested seeds create distinct full courses");
        Check(surfaces.Count == 6 && obstacleKinds.Count == 5, "all terrain and obstacle modules occur");
        Check(shortcuts > 0 && shortcuts < courses, "bypass presence varies automatically");
        foreach (int seed in new[] { int.MinValue, int.MaxValue, 0, -1, 2026 })
            Check(Canonical(RaceCourseGenerator.Generate(seed)) == Canonical(RaceCourseGenerator.Generate(seed)), "boundary seeds repeat safely");
        Console.WriteLine("PASS " + courses + " generated courses, seed variety, safe grid, all surfaces/obstacles and connected bypasses");
    }
    private static void TestProjection()
    {
        var course = RaceCourseGenerator.Generate(12345);
        float cursor = 0f;
        for (float arc = 0; arc <= course.Length; arc += .18f)
        {
            cursor = course.Project(course.Sample(arc), cursor, .8f);
            Close(cursor, arc, .003f, "continuous progress follows main track without jumps");
            RacePoint tangent = course.Tangent(arc);
            Close((float)Math.Sqrt(tangent.x * tangent.x + tangent.y * tangent.y), 1f, .0001f, "tangent normalized across bends");
        }
        float fixedCursor = 3f;
        for (int i = 0; i < 500; i++) fixedCursor = course.Project(new RacePoint(-7f, 0f), fixedCursor);
        Close(fixedCursor, 3f, .0001f, "marble on different row cannot repeatedly walk projection cursor to finish");
        Close(course.Project(new RacePoint(70, -40), 13f), 13f, .0001f, "off-track position cannot increase progress");
        Close(course.Project(new RacePoint(float.PositiveInfinity, 4), 8f), 8f, .0001f, "infinite coordinate preserves prior progress");
        Close(course.Project(new RacePoint(float.NaN, 4), float.NaN), 0f, .0001f, "invalid cursor and position reset safely to start");
        Check(course.SurfaceAt(-10f) == course.segments[0].surface, "surface clamps before start");
        Check(course.SurfaceAt(1000f) == course.segments[course.segments.Count - 1].surface, "surface clamps after finish");
        Console.WriteLine("PASS continuous progress, wrong-row rejection and non-finite input handling");
    }
    private static void TestValidation()
    {
        Throws(() => RaceCourseGenerator.Generate(1, 2), "too few blocks rejected");
        Throws(() => RaceCourseGenerator.Generate(1, 25), "too many blocks rejected");
        Throws(() => RaceCourseGenerator.Generate(1, 9, float.NaN), "non-finite width rejected");
        Throws(() => RaceCourseGenerator.Generate(1, 9, 5f), "unsafe road width rejected");
        Throws(() => RaceCourseGenerator.Validate(null), "missing course rejected");
        var course = RaceCourseGenerator.Generate(5);course.points[2] = new RacePoint(float.NaN, 0);
        Throws(() => RaceCourseGenerator.Validate(course), "corrupt point rejected");
        course = RaceCourseGenerator.Generate(5);course.points[0] = new RacePoint(-9, 4);
        Throws(() => RaceCourseGenerator.Validate(course), "disconnected start rejected");
        course = RaceCourseGenerator.Generate(5);course.segments[1].startDistance += 1;
        Throws(() => RaceCourseGenerator.Validate(course), "block gap rejected");
        course = RaceCourseGenerator.Generate(5);course.segments[0].index = 7;
        Throws(() => RaceCourseGenerator.Validate(course), "wrong block index rejected");
        course = RaceCourseGenerator.Generate(5);course.obstacles[0].size = 9f;
        Throws(() => RaceCourseGenerator.Validate(course), "full-width blocker rejected");
        course = RaceCourseGenerator.Generate(5);course.obstacles[0].position = new RacePoint(100, 100);
        Throws(() => RaceCourseGenerator.Validate(course), "off-track obstacle rejected");
        for (int seed = 0; ; seed++)
        {
            course = RaceCourseGenerator.Generate(seed);
            if (course.shortcuts.Count == 0) continue;
            course.shortcuts[0].points[0] = new RacePoint(5, 2);
            Throws(() => RaceCourseGenerator.Validate(course), "disconnected bypass rejected");
            break;
        }
        Console.WriteLine("PASS invalid settings and corrupted geometry/metadata rejected");
    }
}
