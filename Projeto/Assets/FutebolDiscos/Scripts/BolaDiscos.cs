using UnityEngine;

namespace FutebolDeDiscos
{
    public sealed class BolaDiscos : MonoBehaviour
    {
        public Rigidbody2D Corpo { get; private set; }
        public PartidaDiscos Partida { get; private set; }
        public DiscoAtleta UltimoToque { get; private set; }
        private TrailRenderer rastro;
        public void Criar(PartidaDiscos partida, float diametro, Sprite sprite, Material material, PhysicsMaterial2D fisica)
        {
            Partida = partida;
            transform.localScale = Vector3.one;
            Corpo = gameObject.AddComponent<Rigidbody2D>();
            Corpo.gravityScale = 0f; Corpo.mass = .45f; Corpo.linearDamping = .09f;
            Corpo.constraints = RigidbodyConstraints2D.FreezeRotation;
            Corpo.interpolation = RigidbodyInterpolation2D.Interpolate;
            Corpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Corpo.sleepMode = RigidbodySleepMode2D.NeverSleep;
            var col = gameObject.AddComponent<CircleCollider2D>(); col.radius = diametro * .5f; col.sharedMaterial = fisica;
            var image = new GameObject("White Match Ball"); image.transform.SetParent(transform, false);
            float s = diametro / Mathf.Max(.0001f, Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y));
            image.transform.localScale = new Vector3(s, s, 1f);
            var sr = image.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sharedMaterial = material; sr.color = Color.white; sr.sortingOrder = 20;
            rastro = gameObject.AddComponent<TrailRenderer>(); rastro.sharedMaterial = material;
            rastro.time = .19f; rastro.startWidth = diametro * .65f; rastro.endWidth = 0f;
            rastro.startColor = new Color(.8f, 1f, 1f, .4f); rastro.endColor = new Color(.8f, 1f, 1f, 0f);
            rastro.minVertexDistance = .08f; rastro.sortingOrder = 11;
            Corpo.simulated = false;
        }
        public void Repor(Vector2 pos, Vector2 velocidade)
        {
            Corpo.position = pos; Corpo.linearVelocity = velocidade; Corpo.angularVelocity = 0f; UltimoToque = null;
            if (rastro != null) rastro.Clear();
        }
        public void RegistrarToque(DiscoAtleta atleta) { UltimoToque = atleta; }
        public void LimitarVelocidade(float maximo) { Corpo.linearVelocity = Vector2.ClampMagnitude(Corpo.linearVelocity, maximo); }
        private void OnCollisionEnter2D(Collision2D collision)
        {
            DiscoAtleta a = collision.collider.GetComponent<DiscoAtleta>(); if (a != null) RegistrarToque(a);
        }
    }
}
