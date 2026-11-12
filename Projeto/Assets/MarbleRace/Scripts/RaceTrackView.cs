using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PokemonMarbleRace
{
    /// <summary>Builds the visible course and its real 2D collision geometry.</summary>
    [DisallowMultipleComponent]
    public sealed class RaceTrackView : MonoBehaviour
    {
        private GameObject runtimeRoot;
        private Material spriteMaterial;
        private PhysicsMaterial2D railMaterial;
        private Sprite square;
        private Sprite circle;
        private Sprite arrow;
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private readonly List<RaceObstacleMotion> moving = new List<RaceObstacleMotion>();
        private readonly List<Collider2D> obstacleColliders = new List<Collider2D>();

        public IReadOnlyList<Collider2D> ObstacleColliders { get { return obstacleColliders; } }

        public static Material CreateSpriteMaterial()
        {
            Material source = Resources.Load<Material>("MarbleRaceSprite");
            if (source != null) return new Material(source);
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            if (shader == null) throw new InvalidOperationException("A sprite shader is required to display Marble Race.");
            return new Material(shader);
        }

        public void Build(RaceCourse course, Sprite background, Sprite snorlax, Sprite rocket)
        {
            Clear();
            runtimeRoot = Child("Generated Course", transform);
            spriteMaterial = CreateSpriteMaterial();
            owned.Add(spriteMaterial);
            railMaterial = new PhysicsMaterial2D("Marble Race Rails") { friction = 0.05f, bounciness = 0.78f };
            owned.Add(railMaterial);
            square = MakeSprite(1, delegate(int x, int y) { return Color.white; });
            circle = MakeSprite(64, delegate(int x, int y)
            {
                float dx = (x + 0.5f) / 64f - 0.5f;
                float dy = (y + 0.5f) / 64f - 0.5f;
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                return new Color(1f, 1f, 1f, Mathf.Clamp01((0.5f - radius) * 128f));
            });
            arrow = MakeSprite(32, delegate(int x, int y)
            {
                float px = (x + 0.5f) / 32f;
                float py = Mathf.Abs((y + 0.5f) / 32f - 0.5f);
                bool shaft = px > 0.08f && px < 0.57f && py < 0.105f;
                bool tip = px >= 0.42f && px < 0.95f && py < (0.95f - px) * 0.66f;
                return new Color(1f, 1f, 1f, shaft || tip ? 1f : 0f);
            });

            DecorateArena(background);
            if (course == null || course.points == null || course.points.Count < 2) return;
            float width = Mathf.Clamp(course.width, 1.4f, 4f);
            Stroke("Road Glow", course.points, width + 0.42f, new Color(0.02f, 0.76f, 1f, 0.22f), -45);
            Stroke("Road Border", course.points, width + 0.19f, new Color(0.01f, 0.58f, 0.76f), -44);
            Stroke("Main Road", course.points, width, SurfaceColor(RaceSurface.Normal), -42);

            if (course.shortcuts != null)
                foreach (RaceShortcut shortcut in course.shortcuts)
                {
                    if (shortcut == null || shortcut.points == null || shortcut.points.Count < 2) continue;
                    Stroke("Bypass Glow", shortcut.points, width + 0.38f, new Color(0.09f, 1f, 0.65f, 0.22f), -43);
                    Stroke("Bypass Border", shortcut.points, width + 0.16f, new Color(0.03f, 0.67f, 0.45f), -42);
                    Stroke("Rocket Bypass", shortcut.points, width, new Color(0.15f, 0.35f, 0.28f), -41);
                }

            if (course.segments != null)
                foreach (RaceSegment segment in course.segments)
                {
                    if (segment == null || segment.endDistance <= segment.startDistance) continue;
                    List<RacePoint> section = Slice(course, segment.startDistance, segment.endDistance);
                    Stroke("Surface " + segment.index, section, width - 0.15f, SurfaceColor(segment.surface), -40);
                    RacePoint location = course.Sample((segment.startDistance + segment.endDistance) * 0.5f);
                    Vector2 tangent = V(course.Tangent((segment.startDistance + segment.endDistance) * 0.5f));
                    Vector2 normal = new Vector2(-tangent.y, tangent.x);
                    string label = string.IsNullOrEmpty(segment.label) ? segment.surface.ToString().ToUpperInvariant() : segment.label.ToUpperInvariant();
                    WorldLabel("Zone " + segment.index, label, V(location) + normal * (width * 0.28f), 2.35f, 3.8f, new Color(0.9f, 0.97f, 1f), -4);
                }

            for (float d = 5f; d < course.Length - 3f; d += 4.6f)
            {
                Vector2 tangent = V(course.Tangent(d));
                RendererSprite("Direction Arrow", arrow, runtimeRoot.transform, V(course.Sample(d)), new Vector2(0.66f, 0.4f), Angle(tangent), new Color(1f, 1f, 1f, 0.40f), -12);
            }

            BuildRails(course.points, course, -1);
            if (course.shortcuts != null)
                for (int i = 0; i < course.shortcuts.Count; i++)
                {
                    RaceShortcut shortcut = course.shortcuts[i];
                    if (shortcut == null || shortcut.points == null || shortcut.points.Count < 2) continue;
                    BuildRails(shortcut.points, course, i);
                    float middle = RacePolyline.Length(shortcut.points) * 0.5f;
                    Vector2 point = V(RacePolyline.Sample(shortcut.points, middle));
                    WorldLabel("Bypass Label", "ROCKET BYPASS", point, 2.4f, 3.8f, new Color(0.6f, 1f, 0.77f), -3);
                    if (rocket != null)
                        RendererSprite("Rocket Bypass Sign", rocket, runtimeRoot.transform, point + new Vector2(0f, 0.65f), new Vector2(0.85f, 0.65f), 0f, Color.white, 4, true);
                }

            Vector2 first = V(course.Sample(0f));
            Vector2 firstTangent = V(course.Tangent(0f));
            Vector2 firstNormal = new Vector2(-firstTangent.y, firstTangent.x);
            Wall(first - firstNormal * width * 0.5f, first + firstNormal * width * 0.5f, "Starting Grid Back Wall");
            Checker(course, Mathf.Min(2.8f, course.Length * 0.08f), "START", true);
            Checker(course, Mathf.Max(0f, course.Length - 0.6f), "FINISH", false);

            if (course.obstacles != null)
                foreach (RaceObstacle obstacle in course.obstacles)
                    if (obstacle != null) BuildObstacle(course, obstacle, snorlax);
            SetElapsed(0f);
        }

        public void SetElapsed(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            foreach (RaceObstacleMotion obstacle in moving)
                if (obstacle != null) obstacle.SetElapsed(Mathf.Max(0f, seconds));
        }

        private void DecorateArena(Sprite background)
        {
            RendererSprite("Arena Backdrop", square, runtimeRoot.transform, Vector2.zero, new Vector2(32f, 18.5f), 0f, new Color(0.018f, 0.035f, 0.09f), -100);
            if (background != null)
                RendererSprite("Custom Arena Image", background, runtimeRoot.transform, Vector2.zero, new Vector2(31f, 17.5f), 0f, new Color(1f, 1f, 1f, 0.24f), -98);
            for (int side = -1; side <= 1; side += 2)
            {
                RendererSprite("Stadium Light Strip", square, runtimeRoot.transform, new Vector2(0f, side * 6.6f), new Vector2(29.5f, 0.11f), 0f, new Color(0.02f, 0.63f, 1f, 0.6f), -90);
                for (int i = 0; i < 48; i++)
                {
                    float x = -14.6f + i * 0.62f;
                    Color tint = i % 3 == 0 ? new Color(1f, 0.68f, 0.16f, 0.52f) : new Color(0.12f, 0.42f, 0.75f, 0.55f);
                    RendererSprite("Stadium Crowd", circle, runtimeRoot.transform, new Vector2(x, side * (6.96f + (i % 2) * 0.28f)), new Vector2(0.18f, 0.18f), 0f, tint, -88);
                }
            }
        }

        private void BuildRails(List<RacePoint> path, RaceCourse course, int shortcutIndex)
        {
            if (path == null || path.Count < 2) return;
            float length = RacePolyline.Length(path);
            float step = 0.46f;
            float half = course.width * 0.5f;
            for (float d = 0f; d < length - 0.001f; d += step)
            {
                float next = Mathf.Min(length, d + step);
                Vector2 a = V(RacePolyline.Sample(path, d));
                Vector2 b = V(RacePolyline.Sample(path, next));
                Vector2 tangent = (b - a).normalized;
                if (tangent.sqrMagnitude < 0.01f) continue;
                Vector2 normal = new Vector2(-tangent.y, tangent.x);
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 railA = a + normal * (half + 0.02f) * side;
                    Vector2 railB = b + normal * (half + 0.02f) * side;
                    Vector2 midpoint = (railA + railB) * 0.5f;
                    if (InsideOtherRoad(midpoint, course, shortcutIndex, half + 0.16f)) continue;
                    Wall(railA, railB, shortcutIndex < 0 ? "Course Rail" : "Bypass Rail");
                }
            }
        }

        private static bool InsideOtherRoad(Vector2 point, RaceCourse course, int shortcutIndex, float tolerance)
        {
            if (shortcutIndex >= 0 && DistanceToPath(point, course.points) < tolerance) return true;
            if (course.shortcuts != null)
                for (int i = 0; i < course.shortcuts.Count; i++)
                {
                    if (i == shortcutIndex) continue;
                    RaceShortcut other = course.shortcuts[i];
                    if (other != null && other.points != null && DistanceToPath(point, other.points) < tolerance) return true;
                }
            return false;
        }

        private static float DistanceToPath(Vector2 point, List<RacePoint> path)
        {
            float minimum = float.MaxValue;
            if (path == null) return minimum;
            for (int i = 1; i < path.Count; i++)
            {
                Vector2 a = V(path[i - 1]);
                Vector2 b = V(path[i]);
                Vector2 direction = b - a;
                float ratio = direction.sqrMagnitude > 0.000001f ? Mathf.Clamp01(Vector2.Dot(point - a, direction) / direction.sqrMagnitude) : 0f;
                minimum = Mathf.Min(minimum, Vector2.Distance(point, a + direction * ratio));
            }
            return minimum;
        }

        private void Wall(Vector2 a, Vector2 b, string name)
        {
            float length = Vector2.Distance(a, b);
            if (length < 0.0001f) return;
            SpriteRenderer renderer = RendererSprite(name, square, runtimeRoot.transform, (a + b) * 0.5f, new Vector2(length + 0.065f, 0.12f), Angle(b - a), new Color(0.09f, 0.86f, 1f), -9);
            BoxCollider2D collider = renderer.gameObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.sharedMaterial = railMaterial;
        }

        private void Checker(RaceCourse course, float distance, string title, bool start)
        {
            Vector2 center = V(course.Sample(distance));
            Vector2 tangent = V(course.Tangent(distance));
            Vector2 normal = new Vector2(-tangent.y, tangent.x);
            float tile = course.width / 8f;
            for (int row = 0; row < 2; row++)
                for (int column = 0; column < 8; column++)
                {
                    Vector2 position = center + normal * ((column + 0.5f) * tile - course.width * 0.5f) + tangent * ((row - 0.5f) * tile);
                    RendererSprite(title + " Checker", square, runtimeRoot.transform, position, new Vector2(tile, tile), Angle(tangent), (row + column) % 2 == 0 ? Color.white : new Color(0.025f, 0.035f, 0.07f), -5);
                }
            Vector2 labelPosition = center + new Vector2(0f, start ? 1.65f : -1.65f);
            labelPosition.y = Mathf.Clamp(labelPosition.y, -5.75f, 5.55f);
            WorldLabel(title + " Sign", title, labelPosition, 3.9f, 3.8f, start ? new Color(0.4f, 1f, 0.74f) : new Color(1f, 0.83f, 0.2f), 10);
        }

        private void BuildObstacle(RaceCourse course, RaceObstacle definition, Sprite snorlax)
        {
            GameObject holder = Child(definition.kind.ToString(), runtimeRoot.transform);
            holder.transform.localPosition = new Vector3(definition.position.x, definition.position.y, 0f);
            Vector2 tangent = new Vector2(Mathf.Cos(definition.angle * Mathf.Deg2Rad), Mathf.Sin(definition.angle * Mathf.Deg2Rad));
            Vector2 normal = new Vector2(-tangent.y, tangent.x);
            float size = Mathf.Clamp(definition.size, 0.25f, course.width * 0.65f);
            Collider2D physical = null;
            SpriteRenderer actor = null;
            Rigidbody2D body = null;

            if (definition.kind == RaceObstacleKind.Spinner)
            {
                body = holder.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                Color[] colors = { new Color(1f, 0.33f, 0.2f), new Color(1f, 0.8f, 0.1f), new Color(0.13f, 0.7f, 1f) };
                for (int i = 0; i < 3; i++)
                {
                    SpriteRenderer blade = RendererSprite("Spinner Blade", square, holder.transform, Vector2.zero, new Vector2(size, 0.17f), i * 60f, colors[i], 10);
                    BoxCollider2D collider = blade.gameObject.AddComponent<BoxCollider2D>();
                    collider.size = Vector2.one;
                    collider.sharedMaterial = railMaterial;
                    obstacleColliders.Add(collider);
                }
                RendererSprite("Spinner Hub", circle, holder.transform, Vector2.zero, new Vector2(0.28f, 0.28f), 0f, Color.white, 12);
            }
            else if (definition.kind == RaceObstacleKind.MovingGate)
            {
                body = holder.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                actor = RendererSprite("Sliding Gate", square, holder.transform, Vector2.zero, new Vector2(size, 0.22f), Angle(tangent) + 90f, new Color(1f, 0.37f, 0.15f), 10);
                BoxCollider2D collider = actor.gameObject.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one;
                collider.sharedMaterial = railMaterial;
                physical = collider;
            }
            else if (definition.kind == RaceObstacleKind.Snorlax)
            {
                actor = RendererSprite("Sleeping Snorlax", snorlax != null ? snorlax : circle, holder.transform, Vector2.zero, new Vector2(size, size), 0f, snorlax != null ? Color.white : new Color(0.13f, 0.43f, 0.53f), 14, true);
                CircleCollider2D collider = holder.AddComponent<CircleCollider2D>();
                collider.radius = size * 0.43f;
                collider.sharedMaterial = railMaterial;
                physical = collider;
                body = holder.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
            }
            else if (definition.kind == RaceObstacleKind.PushPad)
            {
                actor = RendererSprite("Boost Pad", circle, holder.transform, Vector2.zero, new Vector2(size, size), 0f, new Color(0.09f, 0.9f, 0.77f, 0.72f), -8);
                RendererSprite("Boost Arrows", arrow, holder.transform, Vector2.zero, new Vector2(size * 0.7f, size * 0.5f), Angle(tangent), Color.white, -6);
                CircleCollider2D collider = holder.AddComponent<CircleCollider2D>();
                collider.radius = size * 0.5f;
                collider.isTrigger = true;
                physical = collider;
            }
            else
            {
                actor = RendererSprite("Bumper", circle, holder.transform, Vector2.zero, new Vector2(size, size), 0f, new Color(1f, 0.27f, 0.17f), 10);
                RendererSprite("Bumper Center", circle, holder.transform, Vector2.zero, new Vector2(size * 0.6f, size * 0.6f), 0f, new Color(1f, 0.86f, 0.44f), 11);
                CircleCollider2D collider = holder.AddComponent<CircleCollider2D>();
                collider.radius = size * 0.5f;
                collider.sharedMaterial = railMaterial;
                physical = collider;
            }

            if (physical != null && !physical.isTrigger) obstacleColliders.Add(physical);

            if (definition.kind == RaceObstacleKind.Spinner || definition.kind == RaceObstacleKind.MovingGate || definition.kind == RaceObstacleKind.Snorlax || definition.kind == RaceObstacleKind.PushPad)
            {
                RaceObstacleMotion animation = holder.AddComponent<RaceObstacleMotion>();
                animation.Configure(definition, body, physical, actor, normal, course.width);
                moving.Add(animation);
            }
        }

        private void Stroke(string name, List<RacePoint> path, float width, Color tint, int order)
        {
            if (path == null || path.Count < 2) return;
            float length = RacePolyline.Length(path);
            if (length <= 0.001f) return;
            int count = Mathf.Max(2, Mathf.CeilToInt(length / 0.32f) + 1);
            Vector3[] vertices = new Vector3[count * 2];
            Vector2[] uv = new Vector2[count * 2];
            Color[] colors = new Color[count * 2];
            int[] triangles = new int[(count - 1) * 6];
            for (int i = 0; i < count; i++)
            {
                float distance = length * i / (count - 1);
                Vector2 point = V(RacePolyline.Sample(path, distance));
                Vector2 tangent = V(RacePolyline.Tangent(path, distance));
                Vector2 normal = new Vector2(-tangent.y, tangent.x) * width * 0.5f;
                vertices[i * 2] = new Vector3(point.x + normal.x, point.y + normal.y, 0f);
                vertices[i * 2 + 1] = new Vector3(point.x - normal.x, point.y - normal.y, 0f);
                uv[i * 2] = new Vector2(0f, distance);
                uv[i * 2 + 1] = new Vector2(1f, distance);
                colors[i * 2] = colors[i * 2 + 1] = tint;
                if (i == count - 1) continue;
                int vertex = i * 2;
                int index = i * 6;
                triangles[index] = vertex;
                triangles[index + 1] = vertex + 2;
                triangles[index + 2] = vertex + 1;
                triangles[index + 3] = vertex + 1;
                triangles[index + 4] = vertex + 2;
                triangles[index + 5] = vertex + 3;
            }
            Mesh mesh = new Mesh { name = name, vertices = vertices, uv = uv, colors = colors, triangles = triangles };
            mesh.RecalculateBounds();
            owned.Add(mesh);
            GameObject holder = Child(name, runtimeRoot.transform);
            holder.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = holder.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = spriteMaterial;
            renderer.sortingOrder = order;
        }

        private Sprite MakeSprite(int size, Func<int, int, Color> pixel)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Marble Race Shape";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++) pixels[y * size + x] = pixel(x, y);
            texture.SetPixels(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            owned.Add(sprite);
            owned.Add(texture);
            return sprite;
        }

        private SpriteRenderer RendererSprite(string name, Sprite sprite, Transform parent, Vector2 position, Vector2 size, float angle, Color tint, int order, bool preserveAspect = false)
        {
            GameObject holder = Child(name, parent);
            holder.transform.localPosition = new Vector3(position.x, position.y, 0f);
            holder.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            SpriteRenderer renderer = holder.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.sortingOrder = order;
            renderer.sharedMaterial = spriteMaterial;
            if (sprite != null)
            {
                Vector3 bounds = sprite.bounds.size;
                float x = size.x / Mathf.Max(0.0001f, bounds.x);
                float y = size.y / Mathf.Max(0.0001f, bounds.y);
                if (preserveAspect) x = y = Mathf.Min(x, y);
                holder.transform.localScale = new Vector3(x, y, 1f);
            }
            return renderer;
        }

        private void WorldLabel(string name, string value, Vector2 position, float fontSize, float width, Color tint, int order)
        {
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null) return;
            GameObject holder = Child(name, runtimeRoot.transform);
            holder.transform.localPosition = new Vector3(position.x, position.y, 0f);
            TextMeshPro text = holder.AddComponent<TextMeshPro>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.color = tint;
            text.rectTransform.sizeDelta = new Vector2(width, 0.55f);
            MeshRenderer renderer = holder.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sortingOrder = order;
        }

        private static List<RacePoint> Slice(RaceCourse course, float from, float to)
        {
            var points = new List<RacePoint>();
            from = Mathf.Clamp(from, 0f, course.Length);
            to = Mathf.Clamp(to, from, course.Length);
            points.Add(course.Sample(from));
            for (float d = from + 0.4f; d < to; d += 0.4f) points.Add(course.Sample(d));
            points.Add(course.Sample(to));
            return points;
        }

        private static Color SurfaceColor(RaceSurface surface)
        {
            switch (surface)
            {
                case RaceSurface.Water: return new Color(0.035f, 0.4f, 0.69f);
                case RaceSurface.Ice: return new Color(0.40f, 0.73f, 0.86f);
                case RaceSurface.Lava: return new Color(0.70f, 0.22f, 0.075f);
                case RaceSurface.Boost: return new Color(0.13f, 0.50f, 0.32f);
                case RaceSurface.Grass: return new Color(0.18f, 0.36f, 0.22f);
                default: return new Color(0.55f, 0.39f, 0.16f);
            }
        }

        private static GameObject Child(string name, Transform parent)
        {
            GameObject holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            return holder;
        }

        private static float Angle(Vector2 direction) { return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg; }
        private static Vector2 V(RacePoint point) { return new Vector2(point.x, point.y); }

        public void Clear()
        {
            moving.Clear();
            obstacleColliders.Clear();
            if (runtimeRoot != null)
            {
                runtimeRoot.SetActive(false);
                Dispose(runtimeRoot);
                runtimeRoot = null;
            }
            foreach (UnityEngine.Object item in owned) Dispose(item);
            owned.Clear();
            spriteMaterial = null;
            railMaterial = null;
            square = circle = arrow = null;
        }

        private static void Dispose(UnityEngine.Object item)
        {
            if (item == null) return;
            if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
        }

        private void OnDestroy() { Clear(); }
    }
}
