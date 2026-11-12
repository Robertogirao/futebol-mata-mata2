using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PokemonMarbleRace
{
    /// <summary>Arcade steering on a real dynamic 2D body. Track contacts remain physical.</summary>
    public sealed class RaceMarble : MonoBehaviour
    {
        public PokemonDados Pokemon { get; private set; }
        public float Progress { get; private set; }
        public bool Finished { get; private set; }
        public float FinishTime { get; private set; }
        public int Rescues { get; private set; }
        public string Ability { get; private set; }
        public float AbilityRemaining { get; private set; }
        public Vector2 Position { get { return body != null ? body.position : (Vector2)transform.position; } }

        private RaceCourse course;
        private Rigidbody2D body;
        private CircleCollider2D contact;
        private SpriteRenderer portrait;
        private SpriteRenderer rim;
        private TrailRenderer trail;
        private IReadOnlyList<Collider2D> hazards;
        private float baseSpeed, diameter, phase, laneBias, nextAbility;
        private float lastAdvanceTime, bestProgress, holdUntil, slowUntil, slowFactor = 1f;
        private float padUntil, nextPad, ghostUntil;
        private bool racing, powers, ghostActive;
        private int raceSeed;
        private RaceShortcut shortcut;
        private float branchProgress;
        private readonly HashSet<string> passedShortcuts = new HashSet<string>(StringComparer.Ordinal);

        public void Initialize(PokemonDados pokemon, RaceCourse raceCourse, int seed, int slot,
            float size, Sprite ballSprite, Material material, PhysicsMaterial2D physicsMaterial,
            TMP_FontAsset font, bool usePowers, float speed, IReadOnlyList<Collider2D> obstacleContacts)
        {
            Pokemon = pokemon;
            course = raceCourse;
            powers = usePowers;
            raceSeed = seed;
            diameter = size;
            baseSpeed = speed;
            hazards = obstacleContacts;
            int h = StableHash(pokemon.IdEstavel, seed) & int.MaxValue;
            phase = (h % 6283) / 1000f;
            laneBias = ((h / 31) % 1000) / 999f * 2f - 1f;
            nextAbility = 5f + (h % 600) / 100f;
            int columns = Mathf.Clamp(Mathf.FloorToInt((course.width - 0.2f) / (size + 0.08f)), 1, 4);
            float spacing = size + 0.08f;
            Progress = 0.45f + (slot / columns) * spacing;
            float lateral = ((slot % columns) - (columns - 1) * 0.5f) * spacing;
            Vector2 tangent = V(course.Tangent(Progress));
            Vector2 spawn = V(course.Sample(Progress)) + Normal(tangent) * lateral;
            transform.position = new Vector3(spawn.x, spawn.y, 0f);
            transform.localScale = Vector3.one;

            body = gameObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.mass = Has(TipoPokemon.Pedra) ? 1.15f : 1f;
            body.linearDamping = 0.25f;
            body.angularDamping = 0.1f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            contact = gameObject.AddComponent<CircleCollider2D>();
            contact.radius = size * 0.5f;
            contact.sharedMaterial = physicsMaterial;
            float native = Mathf.Max(ballSprite.bounds.size.x, ballSprite.bounds.size.y);
            native = Mathf.Max(0.0001f, native);
            rim = MakeSprite("Marble Rim", ballSprite, material, Color.Lerp(pokemon.cor, Color.black, 0.6f), size * 1.1f / native, 20);
            MakeSprite("Marble", ballSprite, material, pokemon.cor, size / native, 21);
            Sprite photo = pokemon.retratoBola != null ? pokemon.retratoBola : pokemon.retrato;
            float photoSize = Mathf.Max(photo.bounds.size.x, photo.bounds.size.y);
            portrait = MakeSprite("Pokemon Portrait", photo, material, Color.white,
                size * 0.82f / Mathf.Max(0.0001f, photoSize), 23);
            if (font != null)
            {
                var label = new GameObject("Racer Name", typeof(RectTransform));
                label.transform.SetParent(transform, false);
                label.transform.localPosition = new Vector3(0f, -size * 0.72f, -0.02f);
                var text = label.AddComponent<TextMeshPro>();
                text.font = font;
                text.text = pokemon.nomePokemon.ToUpperInvariant();
                text.fontSize = 2.15f;
                text.alignment = TextAlignmentOptions.Center;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Ellipsis;
                text.color = Color.white;
                text.outlineColor = Color.black;
                text.outlineWidth = 0.2f;
                text.rectTransform.sizeDelta = new Vector2(1.4f, 0.3f);
                Renderer labelRenderer = text.GetComponent<Renderer>();
                if (labelRenderer != null) labelRenderer.sortingOrder = 24;
            }
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = 0.22f;
            trail.minVertexDistance = 0.08f;
            trail.startWidth = size * 0.45f;
            trail.endWidth = 0f;
            trail.startColor = new Color(pokemon.cor.r, pokemon.cor.g, pokemon.cor.b, 0.28f);
            trail.endColor = new Color(pokemon.cor.r, pokemon.cor.g, pokemon.cor.b, 0f);
            trail.sortingOrder = 18;
            trail.emitting = false;
            body.simulated = false;
            bestProgress = Progress;
            Ability = "";
        }

        public void SetRacing(bool active)
        {
            racing = active && !Finished;
            if (body == null) return;
            body.linearVelocity = Vector2.zero;
            body.simulated = racing;
            if (trail != null) { trail.emitting = racing; if (!racing) trail.Clear(); }
            if (!racing) SetGhost(false);
        }

        public void Step(float elapsed, float delta, IList<RaceMarble> racers)
        {
            if (!racing || Finished || body == null || delta <= 0f) return;
            AbilityRemaining = Mathf.Max(0f, AbilityRemaining - delta);
            if (AbilityRemaining <= 0f) Ability = "";
            if (ghostActive && elapsed >= ghostUntil) SetGhost(false);
            else if (ghostActive) SetGhost(true); // Reapply after a gate re-enables its collider.
            if (elapsed < holdUntil) { body.linearVelocity = Vector2.zero; return; }
            Vector2 position = body.position;
            UpdateProgress(position);
            if (Progress > bestProgress + 0.18f)
            {
                bestProgress = Progress;
                lastAdvanceTime = elapsed;
            }
            if (Progress >= course.Length - 0.45f && Vector2.Distance(position, V(course.Sample(course.Length))) < 1.4f)
            {
                Finished = true;
                Progress = course.Length;
                FinishTime = elapsed;
                SetRacing(false);
                var c = portrait.color; c.a = 0.65f; portrait.color = c;
                return;
            }
            if (elapsed - lastAdvanceTime > 4.5f)
            {
                Recover(elapsed);
                return;
            }
            if (powers && elapsed >= nextAbility) ActivateAbility(elapsed, racers);
            float factor = SurfaceFactor(course.SurfaceAt(Progress));
            if (AbilityRemaining > 0f)
                factor *= Has(TipoPokemon.Eletrico) ? 1.3f : Has(TipoPokemon.Pedra) || Has(TipoPokemon.Gelo) || Has(TipoPokemon.Fantasma) ? 1.06f : 1.16f;
            if (elapsed < slowUntil) factor *= slowFactor;
            if (elapsed < padUntil) factor *= 1.25f;
            CheckPads(elapsed, position);

            float look = 0.7f + baseSpeed * 0.3f;
            Vector2 target, tangent;
            if (shortcut != null)
            {
                target = V(RacePolyline.Sample(shortcut.points, branchProgress + look));
                tangent = V(RacePolyline.Tangent(shortcut.points, branchProgress + look));
            }
            else
            {
                target = V(course.Sample(Progress + look));
                tangent = V(course.Tangent(Progress + look));
            }
            float maxLane = Mathf.Max(0.05f, course.width * 0.5f - diameter * 0.5f - 0.18f);
            float lane = (Mathf.Sin(elapsed * 0.72f + phase) * 0.58f + laneBias * 0.32f) * maxLane;
            for (int i = 0; i < course.obstacles.Count; i++)
            {
                RaceObstacle obstacle = course.obstacles[i];
                if (obstacle.kind == RaceObstacleKind.Spinner || obstacle.kind == RaceObstacleKind.PushPad) continue;
                Vector2 offset = V(obstacle.position) - position;
                if (offset.magnitude < 1.6f && Vector2.Dot(offset, tangent) > 0f)
                {
                    float side = Vector2.Dot(position - V(obstacle.position), Normal(tangent));
                    lane = (Mathf.Abs(side) < 0.08f ? (laneBias < 0 ? -1f : 1f) : Mathf.Sign(side)) * maxLane;
                    break;
                }
            }
            target += Normal(tangent) * lane;
            Vector2 direction = (target - position).normalized;
            Vector2 desired = direction * (baseSpeed * factor);
            float response = course.SurfaceAt(Progress) == RaceSurface.Ice && !Has(TipoPokemon.Gelo) ? 1.8f : 4.5f;
            body.linearVelocity = Vector2.Lerp(body.linearVelocity, desired, 1f - Mathf.Exp(-response * delta));
            if (rim != null)
                rim.color = AbilityRemaining > 0f ? Color.Lerp(Pokemon.cor, Color.white, 0.5f + 0.25f * Mathf.Sin(elapsed * 9f)) : Color.Lerp(Pokemon.cor, Color.black, 0.6f);
        }

        private void UpdateProgress(Vector2 position)
        {
            if (shortcut == null)
            {
                Progress = course.Project(P(position), Progress);
                for (int i = 0; i < course.shortcuts.Count; i++)
                {
                    RaceShortcut candidate = course.shortcuts[i];
                    if (passedShortcuts.Contains(candidate.id) || Progress < candidate.startDistance - 0.4f) continue;
                    passedShortcuts.Add(candidate.id);
                    bool choose = (StableHash(Pokemon.IdEstavel + candidate.id, raceSeed) & int.MaxValue) % 3 == 0;
                    if (choose && Progress <= candidate.startDistance + 0.7f && Vector2.Distance(position, V(candidate.points[0])) < 1.6f)
                    {
                        shortcut = candidate;
                        branchProgress = 0f;
                    }
                }
            }
            if (shortcut != null)
            {
                float length = RacePolyline.Length(shortcut.points);
                float candidate = RacePolyline.Project(shortcut.points, P(position), branchProgress);
                // A position outside the bypass cannot earn progress by projection alone.
                if (Vector2.Distance(position, V(RacePolyline.Sample(shortcut.points, candidate))) <= course.width * 0.5f + 0.55f)
                    branchProgress = candidate;
                Progress = shortcut.startDistance + (shortcut.endDistance - shortcut.startDistance) * Mathf.Clamp01(branchProgress / length);
                if (branchProgress >= length - 0.35f)
                {
                    Progress = shortcut.endDistance;
                    shortcut = null;
                }
            }
        }

        private void Recover(float elapsed)
        {
            // Resume at the last reached checkpoint, never ahead of earned progress.
            shortcut = null;
            Vector2 center = V(course.Sample(Progress));
            Vector2 normal = Normal(V(course.Tangent(Progress)));
            body.position = center + normal * (laneBias * 0.35f);
            body.linearVelocity = Vector2.zero;
            holdUntil = elapsed + 0.8f;
            lastAdvanceTime = elapsed + 0.8f;
            bestProgress = Progress;
            Rescues++;
            Ability = "RECOVERING";
            AbilityRemaining = 0.8f;
            if (trail != null) trail.Clear();
        }

        private void ActivateAbility(float elapsed, IList<RaceMarble> racers)
        {
            nextAbility = elapsed + 11f + (StableHash(Pokemon.IdEstavel, raceSeed) & 255) / 100f;
            AbilityRemaining = Has(TipoPokemon.Eletrico) ? 2.2f : 1.8f;
            if (Has(TipoPokemon.Eletrico)) Ability = "ELECTRIC TURBO";
            else if (Has(TipoPokemon.Gelo))
            {
                Ability = "ICE TRAIL";
                for (int i = 0; i < racers.Count; i++)
                    if (racers[i] != this && !racers[i].Finished && Vector2.Distance(Position, racers[i].Position) < 1.8f)
                        racers[i].ApplySlow(elapsed, 1.4f, 0.8f);
            }
            else if (Has(TipoPokemon.Fantasma))
            {
                Ability = "GHOST PHASE";
                ghostUntil = elapsed + AbilityRemaining;
                SetGhost(true);
            }
            else if (Has(TipoPokemon.Pedra)) Ability = "ROCK GUARD";
            else Ability = TypeName(Pokemon.tipoPrimario).ToUpperInvariant() + " BOOST";
        }

        private float SurfaceFactor(RaceSurface surface)
        {
            switch (surface)
            {
                case RaceSurface.Water: return Has(TipoPokemon.Agua) ? 1.28f : 0.9f;
                case RaceSurface.Ice: return Has(TipoPokemon.Gelo) ? 1.15f : 0.94f;
                case RaceSurface.Lava: return Has(TipoPokemon.Fogo) ? 1.23f : 0.87f;
                case RaceSurface.Grass: return Has(TipoPokemon.Planta) ? 1.15f : 1f;
                case RaceSurface.Boost: return 1.2f;
                default: return 1f;
            }
        }

        private void CheckPads(float elapsed, Vector2 position)
        {
            if (elapsed < nextPad) return;
            foreach (RaceObstacle obstacle in course.obstacles)
                if (obstacle.kind == RaceObstacleKind.PushPad && Vector2.Distance(position, V(obstacle.position)) < obstacle.size * 0.5f + diameter * 0.5f)
                {
                    padUntil = elapsed + 0.75f;
                    nextPad = elapsed + 2f;
                    Ability = "TRACK BOOST";
                    AbilityRemaining = 0.75f;
                    break;
                }
        }

        private void ApplySlow(float elapsed, float duration, float factor)
        {
            if (Has(TipoPokemon.Pedra) || Has(TipoPokemon.Gelo)) return;
            slowUntil = Mathf.Max(slowUntil, elapsed + duration);
            slowFactor = factor;
        }

        private void SetGhost(bool active)
        {
            ghostActive = active;
            if (contact != null && hazards != null)
                for (int i = 0; i < hazards.Count; i++)
                    if (hazards[i] != null && !hazards[i].isTrigger)
                        Physics2D.IgnoreCollision(contact, hazards[i], active);
            if (portrait != null)
            {
                Color c = portrait.color;
                c.a = active ? 0.55f : 1f;
                portrait.color = c;
            }
        }

        private bool Has(TipoPokemon type)
        {
            return powers && Pokemon != null && (Pokemon.tipoPrimario == type || Pokemon.tipoSecundario == type);
        }

        private SpriteRenderer MakeSprite(string label, Sprite sprite, Material material, Color color, float scale, int order)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        public RaceFinish Result(int place, float elapsed)
        {
            return new RaceFinish { id = Pokemon.IdEstavel, place = place, time = Finished ? FinishTime : elapsed,
                finished = Finished, progress = Mathf.Clamp01(Progress / course.Length), rescues = Rescues };
        }

        public static string TypeName(TipoPokemon type)
        {
            string[] names = { "Mixed", "Normal", "Fire", "Water", "Electric", "Grass", "Ice", "Fighting", "Poison", "Ground", "Flying", "Psychic", "Bug", "Rock", "Ghost", "Dragon", "Dark", "Steel", "Fairy" };
            int index = (int)type;
            return index >= 0 && index < names.Length ? names[index] : "Mixed";
        }

        private static int StableHash(string text, int seed)
        {
            unchecked { int h = seed ^ 216613626; foreach (char c in text) h = (h ^ c) * 16777619; return h; }
        }

        private static RacePoint P(Vector2 p) { return new RacePoint(p.x, p.y); }
        private static Vector2 V(RacePoint p) { return new Vector2(p.x, p.y); }
        private static Vector2 Normal(Vector2 direction) { return new Vector2(-direction.y, direction.x); }
        private void OnDisable() { if (body != null) SetRacing(false); }
    }
}
