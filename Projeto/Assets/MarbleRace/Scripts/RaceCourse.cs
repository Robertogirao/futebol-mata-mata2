using System;
using System.Collections.Generic;

namespace PokemonMarbleRace
{
    [Serializable]
    public struct RacePoint
    {
        public float x;
        public float y;
        public RacePoint(float x, float y) { this.x = x; this.y = y; }
    }

    public enum RaceSurface { Normal, Water, Ice, Lava, Boost, Grass }
    public enum RaceObstacleKind { Spinner, Bumper, MovingGate, Snorlax, PushPad }

    [Serializable]
    public sealed class RaceSegment
    {
        public int index;
        public string label;
        public RaceSurface surface;
        public float startDistance;
        public float endDistance;
    }

    [Serializable]
    public sealed class RaceObstacle
    {
        public int segmentIndex;
        public RaceObstacleKind kind;
        public RacePoint position;
        public float angle;
        // Spinner: degrees per second. Other animated obstacles: cycle multiplier.
        public float speed;
        // Overall bar span or bumper diameter, in world units; never a radius.
        public float size;
        public float phase;
    }

    [Serializable]
    public sealed class RaceShortcut
    {
        public string id;
        public float startDistance;
        public float endDistance;
        public List<RacePoint> points = new List<RacePoint>();
    }

    [Serializable]
    public sealed class RaceCourse
    {
        public int seed;
        public float width;
        public List<RacePoint> points = new List<RacePoint>();
        public List<RaceSegment> segments = new List<RaceSegment>();
        public List<RaceObstacle> obstacles = new List<RaceObstacle>();
        public List<RaceShortcut> shortcuts = new List<RaceShortcut>();
        public float Length { get { return RacePolyline.Length(points); } }
        public RacePoint Sample(float distance) { return RacePolyline.Sample(points, distance); }
        public RacePoint Tangent(float distance) { return RacePolyline.Tangent(points, distance); }
        public float Project(RacePoint position, float previousDistance, float maxAdvance = 3f)
        {
            previousDistance = RacePolyline.Finite(previousDistance)
                ? RacePolyline.Clamp(previousDistance, 0f, Length) : 0f;
            float candidate = RacePolyline.Project(points, position, previousDistance, maxAdvance);
            RacePoint closest = Sample(candidate);
            float limit = width * 0.5f + 0.55f;
            // Do not gradually advance the cursor through a nearby, different row.
            return RacePolyline.DistanceSquared(position, closest) <= limit * limit
                ? candidate : previousDistance;
        }
        public RaceSurface SurfaceAt(float distance)
        {
            RaceSegment segment = SegmentAt(distance);
            return segment == null ? RaceSurface.Normal : segment.surface;
        }
        public string LabelAt(float distance)
        {
            RaceSegment segment = SegmentAt(distance);
            return segment == null ? "RACE TRACK" : segment.label;
        }
        private RaceSegment SegmentAt(float distance)
        {
            if (segments == null || segments.Count == 0) return null;
            distance = RacePolyline.Clamp(distance, 0f, Length);
            for (int i = 0; i < segments.Count; i++)
                if (distance < segments[i].endDistance || i == segments.Count - 1)
                    return segments[i];
            return segments[segments.Count - 1];
        }
    }

    /// <summary>Arc-length helpers, independent of Unity, with local progress projection.</summary>
    public static class RacePolyline
    {
        public static float Length(List<RacePoint> points)
        {
            if (points == null) return 0f;
            float length = 0f;
            for (int i = 1; i < points.Count; i++) length += Distance(points[i - 1], points[i]);
            return length;
        }
        public static RacePoint Sample(List<RacePoint> points, float distance)
        {
            if (points == null || points.Count == 0) return new RacePoint(0f, 0f);
            if (!Finite(distance) || distance <= 0f) return points[0];
            for (int i = 1; i < points.Count; i++)
            {
                float size = Distance(points[i - 1], points[i]);
                if (size < 0.00001f) continue;
                if (distance <= size) return Lerp(points[i - 1], points[i], distance / size);
                distance -= size;
            }
            return points[points.Count - 1];
        }
        public static RacePoint Tangent(List<RacePoint> points, float distance)
        {
            if (points == null || points.Count < 2) return new RacePoint(1f, 0f);
            distance = !Finite(distance) ? 0f : Math.Max(0f, distance);
            RacePoint last = new RacePoint(1f, 0f);
            for (int i = 1; i < points.Count; i++)
            {
                RacePoint delta = Subtract(points[i], points[i - 1]);
                float size = (float)Math.Sqrt(delta.x * delta.x + delta.y * delta.y);
                if (size < 0.00001f) continue;
                last = Scale(delta, 1f / size);
                if (distance < size) return last;
                distance -= size;
            }
            return last;
        }
        public static float Project(List<RacePoint> points, RacePoint position,
            float previous, float maxAdvance = 3f)
        {
            float length = Length(points);
            previous = Finite(previous) ? Clamp(previous, 0f, length) : 0f;
            if (points == null || points.Count < 2 || !Finite(position.x) || !Finite(position.y))
                return previous;
            maxAdvance = Finite(maxAdvance) ? Math.Max(0f, maxAdvance) : 3f;
            float lower = Math.Max(0f, previous - 2f);
            float upper = Math.Min(length, previous + maxAdvance);
            float arc = 0f;
            float bestArc = previous;
            float bestDistance = float.PositiveInfinity;
            for (int i = 1; i < points.Count; i++)
            {
                RacePoint a = points[i - 1], b = points[i];
                float size = Distance(a, b);
                if (size < 0.00001f) continue;
                float end = arc + size;
                if (end >= lower && arc <= upper)
                {
                    RacePoint delta = Subtract(b, a);
                    float t = ((position.x - a.x) * delta.x + (position.y - a.y) * delta.y) / (size * size);
                    float projected = Clamp(arc + t * size, Math.Max(lower, arc), Math.Min(upper, end));
                    RacePoint closest = Lerp(a, b, (projected - arc) / size);
                    float squared = DistanceSquared(position, closest);
                    if (squared < bestDistance - 0.000001f ||
                        (Math.Abs(squared - bestDistance) <= 0.000001f && Math.Abs(projected - previous) < Math.Abs(bestArc - previous)))
                    { bestDistance = squared; bestArc = projected; }
                }
                arc = end;
                if (arc > upper) break;
            }
            return bestArc;
        }
        public static float Distance(RacePoint a, RacePoint b) { return (float)Math.Sqrt(DistanceSquared(a, b)); }
        public static float DistanceSquared(RacePoint a, RacePoint b) { float x = b.x - a.x, y = b.y - a.y; return x * x + y * y; }
        public static RacePoint Lerp(RacePoint a, RacePoint b, float t) { return new RacePoint(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t); }
        public static RacePoint Subtract(RacePoint a, RacePoint b) { return new RacePoint(a.x - b.x, a.y - b.y); }
        public static RacePoint Scale(RacePoint a, float value) { return new RacePoint(a.x * value, a.y * value); }
        public static float Clamp(float value, float min, float max) { return Math.Max(min, Math.Min(max, value)); }
        public static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }

    /// <summary>Builds a connected modular course. The same seed reconstructs the same track.</summary>
    public static class RaceCourseGenerator
    {
        private static readonly string[] Labels =
        {
            "SPEEDWAY", "PINWHEEL GATES", "BUMPER RUN", "WATER CHANNEL", "ICE CORNER",
            "LAVA LANE", "SLIDING GATES", "SNORLAX CROSSING", "GARDEN RUN", "BOOST STRIP", "ROCKET BYPASS"
        };
        public static RaceCourse Generate(int seed, int blocks = 9, float width = 2.4f)
        {
            Require(blocks >= 3 && blocks <= 24, "Choose between 3 and 24 course blocks.");
            Require(RacePolyline.Finite(width) && width >= 1.8f && width <= 2.6f, "Course width must be between 1.8 and 2.6.");
            var random = new CourseRandom(seed);
            var course = new RaceCourse { seed = seed, width = width };
            var modules = new List<int>();
            for (int i = 0; i < Labels.Length; i++) modules.Add(i);
            for (int i = modules.Count - 1; i > 0; i--) { int j = random.Next(i + 1); int tmp = modules[i]; modules[i] = modules[j]; modules[j] = tmp; }
            int moduleCursor = 0;
            var middleSegments = new List<RaceSegment>();
            course.points.Add(new RacePoint(-10f, 4f));
            for (int row = 0; row < 3; row++)
            {
                int count = blocks / 3 + (row < blocks % 3 ? 1 : 0);
                float beginX = row == 0 ? -10f : row == 1 ? 10f : -8f;
                float endX = row == 0 ? 10f : row == 1 ? -8f : 12f;
                float baseY = 4f - row * 4f;
                for (int block = 0; block < count; block++)
                {
                    int module = modules[moduleCursor++ % modules.Count];
                    float nextX = beginX + (endX - beginX) * (block + 1f) / count;
                    float nextY = block == count - 1 ? baseY : baseY + random.Range(-0.18f, 0.18f);
                    RacePoint start = course.points[course.points.Count - 1];
                    RacePoint end = new RacePoint(nextX, nextY);
                    RacePoint control = new RacePoint((start.x + end.x) * 0.5f, baseY + random.Range(-0.30f, 0.30f));
                    float arcStart = course.Length;
                    for (int step = 1; step <= 6; step++)
                    {
                        float t = step / 6f, u = 1f - t;
                        course.points.Add(new RacePoint(u * u * start.x + 2f * u * t * control.x + t * t * end.x,
                            u * u * start.y + 2f * u * t * control.y + t * t * end.y));
                    }
                    var segment = new RaceSegment { index = course.segments.Count, label = Labels[module],
                        surface = ModuleSurface(module), startDistance = arcStart, endDistance = course.Length };
                    course.segments.Add(segment);
                    if (row == 1) middleSegments.Add(segment);
                    AddObstacles(course, segment, module, random);
                }
                if (row < 2)
                {
                    float arcStart = course.Length;
                    for (int step = 1; step <= 12; step++)
                    {
                        double angle = (90d + (row == 0 ? -1d : 1d) * 180d * step / 12d) * Math.PI / 180d;
                        course.points.Add(new RacePoint((row == 0 ? 10f : -8f) + 2f * (float)Math.Cos(angle),
                            (row == 0 ? 2f : -2f) + 2f * (float)Math.Sin(angle)));
                    }
                    course.segments.Add(new RaceSegment { index = course.segments.Count,
                        label = row == 0 ? "EAST HAIRPIN" : "WEST HAIRPIN", surface = RaceSurface.Normal,
                        startDistance = arcStart, endDistance = course.Length });
                }
            }
            if (random.Next(4) != 0)
            {
                RaceSegment segment = middleSegments[random.Next(middleSegments.Count)];
                float startDistance = segment.startDistance + (segment.endDistance - segment.startDistance) * 0.12f;
                float endDistance = segment.endDistance - (segment.endDistance - segment.startDistance) * 0.12f;
                var shortcut = new RaceShortcut { id = "rocket_bypass_1", startDistance = startDistance, endDistance = endDistance };
                float side = random.Next(2) == 0 ? -1f : 1f;
                shortcut.points.Add(course.Sample(startDistance));
                for (int step = 1; step <= 3; step++)
                {
                    float distance = startDistance + (endDistance - startDistance) * step / 4f;
                    RacePoint center = course.Sample(distance), direction = course.Tangent(distance);
                    float offset = (step == 2 ? 1.05f : 0.82f) * side;
                    shortcut.points.Add(new RacePoint(center.x - direction.y * offset, center.y + direction.x * offset));
                }
                shortcut.points.Add(course.Sample(endDistance));
                course.shortcuts.Add(shortcut);
            }
            Validate(course);
            return course;
        }

        private static RaceSurface ModuleSurface(int module)
        {
            switch (module)
            {
                case 3: return RaceSurface.Water;
                case 4: return RaceSurface.Ice;
                case 5: return RaceSurface.Lava;
                case 8: return RaceSurface.Grass;
                case 9: return RaceSurface.Boost;
                default: return RaceSurface.Normal;
            }
        }
        private static void AddObstacles(RaceCourse course, RaceSegment segment, int module, CourseRandom random)
        {
            RaceObstacleKind? kind = null;
            switch (module)
            {
                case 1: kind = RaceObstacleKind.Spinner; break;
                case 2: kind = RaceObstacleKind.Bumper; break;
                case 6: kind = RaceObstacleKind.MovingGate; break;
                case 7: kind = RaceObstacleKind.Snorlax; break;
                case 10: kind = RaceObstacleKind.PushPad; break;
            }
            if (!kind.HasValue) return;
            // Keep the launch grid and finish clear; obstacles belong to a block's interior.
            float distance = segment.startDistance + (segment.endDistance - segment.startDistance) * random.Range(0.43f, 0.62f);
            if (distance < 6.5f)
            {
                if (segment.endDistance > 7.5f) distance = 6.7f;
                else return;
            }
            RacePoint position = course.Sample(distance), direction = course.Tangent(distance);
            float lateral = module == 2 ? random.Range(-0.34f, 0.34f) : random.Range(-0.10f, 0.10f);
            position = new RacePoint(position.x - direction.y * lateral, position.y + direction.x * lateral);
            float size = module == 1 ? random.Range(1.12f, 1.38f) : module == 2 ? random.Range(0.45f, 0.65f)
                : module == 6 ? random.Range(0.55f, 0.82f) : module == 7 ? random.Range(1.05f, 1.22f) : random.Range(0.65f, 0.90f);
            size = Math.Min(size, course.width * 0.59f);
            course.obstacles.Add(new RaceObstacle { segmentIndex = segment.index, kind = kind.Value,
                position = position, angle = (float)(Math.Atan2(direction.y, direction.x) * 180d / Math.PI),
                speed = module == 1 ? random.Range(55f, 110f) * (random.Next(2) == 0 ? -1f : 1f) : random.Range(0.7f, 1.4f),
                size = size, phase = random.Range(0f, 6.283185f) });
        }

        public static void Validate(RaceCourse course)
        {
            Require(course != null, "Course is missing.");
            Require(RacePolyline.Finite(course.width) && course.width >= 1.8f && course.width <= 2.6f, "Invalid course width.");
            Require(course.points != null && course.points.Count >= 8 && course.points.Count <= 1024, "Invalid centerline.");
            CheckPoints(course.points, true);
            float length = course.Length;
            Require(length > 55f && length < 95f, "Course length is outside the playable range.");
            Require(RacePolyline.Distance(course.points[0], new RacePoint(-10f, 4f)) < 0.001f &&
                RacePolyline.Distance(course.points[course.points.Count - 1], new RacePoint(12f, -4f)) < 0.001f,
                "Start or finish is disconnected.");
            Require(course.segments != null && course.segments.Count >= 5, "Course blocks are missing.");
            float previous = 0f;
            for (int i = 0; i < course.segments.Count; i++)
            {
                RaceSegment segment = course.segments[i];
                Require(segment != null && segment.index == i && !string.IsNullOrWhiteSpace(segment.label) &&
                    Enum.IsDefined(typeof(RaceSurface), segment.surface), "Invalid course block.");
                Require(RacePolyline.Finite(segment.startDistance) && RacePolyline.Finite(segment.endDistance) &&
                    Math.Abs(segment.startDistance - previous) < 0.002f && segment.endDistance > segment.startDistance,
                    "Course blocks have a gap or overlap.");
                previous = segment.endDistance;
            }
            Require(Math.Abs(previous - length) < 0.002f, "Blocks do not cover the course.");
            Require(course.obstacles != null && course.shortcuts != null, "Course elements are missing.");
            foreach (RaceObstacle obstacle in course.obstacles)
            {
                Require(obstacle != null && obstacle.segmentIndex >= 0 && obstacle.segmentIndex < course.segments.Count &&
                    Enum.IsDefined(typeof(RaceObstacleKind), obstacle.kind), "Invalid obstacle.");
                Require(FinitePoint(obstacle.position) && RacePolyline.Finite(obstacle.size) && obstacle.size > 0.1f &&
                    obstacle.size <= course.width * 0.65f && RacePolyline.Finite(obstacle.angle) &&
                    RacePolyline.Finite(obstacle.speed) && RacePolyline.Finite(obstacle.phase), "Invalid obstacle dimensions.");
                RaceSegment segment = course.segments[obstacle.segmentIndex];
                float cursor = RacePolyline.Project(course.points, obstacle.position, (segment.startDistance + segment.endDistance) * 0.5f,
                    segment.endDistance - segment.startDistance);
                Require(RacePolyline.Distance(obstacle.position, course.Sample(cursor)) < course.width * 0.5f,
                    "Obstacle is outside its track.");
            }
            Require(course.shortcuts.Count <= 1, "Too many bypass paths.");
            foreach (RaceShortcut shortcut in course.shortcuts)
            {
                Require(shortcut != null && !string.IsNullOrWhiteSpace(shortcut.id) &&
                    RacePolyline.Finite(shortcut.startDistance) && RacePolyline.Finite(shortcut.endDistance) &&
                    shortcut.startDistance >= 0f && shortcut.endDistance <= length && shortcut.endDistance > shortcut.startDistance,
                    "Invalid bypass progress.");
                Require(shortcut.points != null && shortcut.points.Count >= 3 && shortcut.points.Count <= 32, "Bypass path is missing.");
                CheckPoints(shortcut.points, true);
                Require(RacePolyline.Distance(shortcut.points[0], course.Sample(shortcut.startDistance)) < 0.002f &&
                    RacePolyline.Distance(shortcut.points[shortcut.points.Count - 1], course.Sample(shortcut.endDistance)) < 0.002f,
                    "Bypass entrance or exit is disconnected.");
            }
        }
        private static void CheckPoints(List<RacePoint> points, bool bounded)
        {
            for (int i = 0; i < points.Count; i++)
            {
                Require(FinitePoint(points[i]) && (!bounded || (points[i].x >= -10.001f && points[i].x <= 12.001f &&
                    points[i].y >= -4.301f && points[i].y <= 4.301f)), "Track point is outside the arena.");
                if (i > 0) Require(RacePolyline.Distance(points[i - 1], points[i]) > 0.00001f, "Track contains a duplicate point.");
            }
        }
        private static bool FinitePoint(RacePoint point) { return RacePolyline.Finite(point.x) && RacePolyline.Finite(point.y); }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        // Explicit seeded PRNG: reproducible across Unity/Mono runtime implementations.
        private sealed class CourseRandom
        {
            private uint state;
            public CourseRandom(int seed) { state = unchecked((uint)seed) ^ 0x9e3779b9U; if (state == 0U) state = 0x6d2b79f5U; }
            private uint NextValue() { uint value = state; value ^= value << 13; value ^= value >> 17; value ^= value << 5; state = value; return value; }
            public int Next(int maximum) { return (int)(NextValue() % (uint)maximum); }
            public float Range(float minimum, float maximum) { return minimum + (maximum - minimum) * ((NextValue() >> 8) / 16777216f); }
        }
    }
}
