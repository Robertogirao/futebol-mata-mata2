using UnityEngine;

namespace PokemonMarbleRace
{
    /// <summary>Course-controlled obstacle motion. It never changes the race clock.</summary>
    [DisallowMultipleComponent]
    public sealed class RaceObstacleMotion : MonoBehaviour
    {
        private RaceObstacle definition;
        private Rigidbody2D body;
        private Collider2D physical;
        private SpriteRenderer actor;
        private Vector2 origin;
        private Vector2 normal;
        private float width;

        public void Configure(RaceObstacle obstacle, Rigidbody2D rigidbody, Collider2D collider, SpriteRenderer renderer, Vector2 perpendicular, float roadWidth)
        {
            definition = obstacle;
            body = rigidbody;
            physical = collider;
            actor = renderer;
            origin = new Vector2(obstacle.position.x, obstacle.position.y);
            normal = perpendicular.sqrMagnitude > 0.01f ? perpendicular.normalized : Vector2.up;
            width = Mathf.Max(1f, roadWidth);
        }

        public void SetElapsed(float seconds)
        {
            if (definition == null) return;
            float phase = seconds * Mathf.Max(0.1f, Mathf.Abs(definition.speed)) + definition.phase;
            if (definition.kind == RaceObstacleKind.Spinner)
            {
                float angle = definition.angle + seconds * definition.speed;
                if (body != null) body.MoveRotation(angle);
                else transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            else if (definition.kind == RaceObstacleKind.MovingGate)
            {
                Vector2 position = origin + normal * (Mathf.Sin(phase) * width * 0.26f);
                if (body != null) body.MovePosition(position);
                else transform.localPosition = new Vector3(position.x, position.y, 0f);
            }
            else if (definition.kind == RaceObstacleKind.Snorlax)
            {
                bool open = Mathf.Sin(phase) > 0.18f;
                Vector2 position = origin + normal * (open ? width * 0.78f : 0f);
                // Teleport this intermittent gate while its collision is off.
                // MovePosition over this distance would impart an artificial
                // high kinematic velocity when the gate closes.
                if (physical != null) physical.enabled = false;
                if (body != null) body.position = position;
                else transform.localPosition = new Vector3(position.x, position.y, 0f);
                if (physical != null) physical.enabled = !open;
                if (actor != null) actor.color = new Color(1f, 1f, 1f, open ? 0.46f : 1f);
            }
            else if (definition.kind == RaceObstacleKind.PushPad && actor != null)
            {
                actor.color = new Color(0.09f, 0.9f, 0.77f, 0.65f + Mathf.Sin(phase * 3f) * 0.16f);
            }
        }
    }
}
