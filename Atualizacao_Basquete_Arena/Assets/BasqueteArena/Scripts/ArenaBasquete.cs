using System.Collections.Generic;
using FutebolDeDiscos;
using TMPro;
using UnityEngine;

namespace BasqueteArena
{
    public sealed class ArenaBasquete : MonoBehaviour
    {
        public TemaArenaBasquete Tema { get; private set; }
        public readonly Vector2 Saida = new Vector2(2.6f, -2.4f);
        public readonly Vector2 Aro = new Vector2(6.4f, 1.7f);
        public readonly List<Collider2D> ColisoresDisputa = new List<Collider2D>();
        public readonly List<Collider2D> ColisoresCesta = new List<Collider2D>();
        public PhysicsMaterial2D MaterialBolas { get; private set; }
        private CampoDiscos campo;
        private Sprite quadrado;
        private TemaArenaBasquete temaTemporario;
        private readonly List<Rigidbody2D> rotores = new List<Rigidbody2D>();
        private readonly List<float> velocidades = new List<float>();
        private readonly List<Object> recursos = new List<Object>();
        public void Construir(ProjetoFutebolDiscos config, CampoDiscos origem)
        {
            campo = origem;
            Tema = config.temaBasquete;
            if (Tema == null) { temaTemporario = ScriptableObject.CreateInstance<TemaArenaBasquete>(); Tema = temaTemporario; }
            MaterialBolas = new PhysicsMaterial2D("Basquete") { friction = Tema.atrito, bounciness = Tema.quique };
            recursos.Add(MaterialBolas);
            var tex = new Texture2D(1, 1); tex.SetPixel(0, 0, Color.white); tex.Apply(); recursos.Add(tex);
            quadrado = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1); recursos.Add(quadrado);
            Desenhar("Cenario", Vector2.zero, new Vector2(32, 18), Tema.corFundo, -100, Tema.fundoCenario != null ? Tema.fundoCenario : config.fundoExternoArena);
            Desenhar("Quadra", new Vector2(0, -.2f), new Vector2(21, 9), new Color(.12f, .16f, .23f), -90, Tema.fundoQuadra != null ? Tema.fundoQuadra : config.fundoCampo);
            Texto("DISPUTA", new Vector2(-4, 4.1f), 2.5f, Color.white);
            Texto("ARREMESSO", new Vector2(6, 4.1f), 2.5f, Color.white);
            Plataforma("Parede esquerda", new Vector2(-9.5f, 0), new Vector2(.2f, 8), 0);
            Plataforma("Parede direita", new Vector2(.5f, 0), new Vector2(.2f, 8), 0);
            Plataforma("Rampa esquerda", new Vector2(-7.1f, .6f), new Vector2(3.4f, .2f), -18);
            Plataforma("Rampa direita", new Vector2(-1.9f, .6f), new Vector2(3.4f, .2f), 18);
            Plataforma("Funil esquerdo", new Vector2(-6.4f, -2.5f), new Vector2(3.1f, .2f), -25);
            Plataforma("Funil direito", new Vector2(-2.6f, -2.5f), new Vector2(3.1f, .2f), 25);
            var rotor = Plataforma("Barra giratoria", new Vector2(-4.5f, -.4f), new Vector2(3.1f, .22f), 0);
            Girar(rotor, 65f);
            Zona("Eliminacao esquerda", new Vector2(-8.65f, -3.7f), new Vector2(1.6f, .4f), true);
            Zona("Eliminacao direita", new Vector2(-.35f, -3.7f), new Vector2(1.6f, .4f), true);
            Zona("Entrada do lancador", new Vector2(-4.5f, -3.25f), new Vector2(1.2f, .5f), false);
            Desenhar("Lancador", Saida, new Vector2(1.3f, .6f), new Color(1f, .65f, .12f), 4, Tema.imagemLancador);
            Texto("LANCADOR", Saida + Vector2.down * .7f, 1.7f, Color.white);
            // Separate rim ends leave a real opening; the backboard can cause rebounds.
            Cesta("Aro esquerdo", Aro + Vector2.left * .65f, new Vector2(.12f, .14f));
            Cesta("Aro direito", Aro + Vector2.right * .65f, new Vector2(.12f, .14f));
            Cesta("Tabela", Aro + new Vector2(.95f, .7f), new Vector2(.15f, 1.8f));
            Desenhar("Rede", Aro + Vector2.down * .4f, new Vector2(1.25f, .65f), new Color(1, 1, 1, .2f), -5, Tema.imagemCesta);
            Texto("CESTA", Aro + Vector2.down * 1.1f, 2f, Color.white);
            foreach (var o in Tema.obstaculos)
            {
                if (o == null) continue;
                var go = o.elimina ? Zona("Eliminacao personalizada", o.posicao, o.tamanho, true) : Plataforma("Obstaculo personalizado", o.posicao, o.tamanho, o.angulo);
                go.transform.localRotation = Quaternion.Euler(0, 0, o.angulo);
                if (Mathf.Abs(o.grausPorSegundo) > .01f) Girar(go, o.grausPorSegundo);
            }
        }
        public SpriteRenderer Desenhar(string nome, Vector2 pos, Vector2 tamanho, Color cor, int ordem = 0, Sprite sprite = null)
        {
            var go = new GameObject(nome); go.transform.SetParent(transform, false); go.transform.localPosition = pos;
            var r = go.AddComponent<SpriteRenderer>(); r.sprite = sprite != null ? sprite : quadrado;
            r.sharedMaterial = campo.MaterialSprites; r.color = sprite != null ? Color.white : cor; r.sortingOrder = ordem;
            var b = r.sprite.bounds.size;
            go.transform.localScale = new Vector3(tamanho.x / Mathf.Max(.001f, b.x), tamanho.y / Mathf.Max(.001f, b.y), 1);
            return r;
        }
        private GameObject Plataforma(string nome, Vector2 pos, Vector2 tam, float angulo)
        {
            var r = Desenhar(nome, pos, tam, Tema.corPlataformas); r.transform.localRotation = Quaternion.Euler(0, 0, angulo);
            var c = r.gameObject.AddComponent<BoxCollider2D>(); c.sharedMaterial = MaterialBolas;
            ColisoresDisputa.Add(c); return r.gameObject;
        }
        private void Cesta(string nome, Vector2 pos, Vector2 tam)
        {
            var r = Desenhar(nome, pos, tam, new Color(1, .45f, .1f), 5);
            var c = r.gameObject.AddComponent<BoxCollider2D>(); c.sharedMaterial = MaterialBolas; ColisoresCesta.Add(c);
        }
        private GameObject Zona(string nome, Vector2 pos, Vector2 tam, bool elimina)
        {
            var r = Desenhar(nome, pos, tam, elimina ? Tema.corEliminacao : Color.green, 2);
            var c = r.gameObject.AddComponent<BoxCollider2D>(); c.isTrigger = true;
            var z = r.gameObject.AddComponent<ZonaBasquete>(); z.elimina = elimina;
            ColisoresDisputa.Add(c); return r.gameObject;
        }
        private void Girar(GameObject go, float velocidade)
        {
            var rb = go.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic;
            rotores.Add(rb); velocidades.Add(velocidade);
        }
        private void FixedUpdate()
        {
            if (UniversalUI.Instance == null || !UniversalUI.Instance.partidaEmAndamento) return;
            for (int i = 0; i < rotores.Count; i++) rotores[i].MoveRotation(rotores[i].rotation + velocidades[i] * Time.fixedDeltaTime);
        }
        private void Texto(string texto, Vector2 pos, float tamanho, Color cor)
        {
            var go = new GameObject(texto); go.transform.SetParent(transform, false); go.transform.localPosition = pos;
            var t = go.AddComponent<TextMeshPro>(); t.text = texto; t.fontSize = tamanho; t.alignment = TextAlignmentOptions.Center;
            t.color = cor; t.rectTransform.sizeDelta = new Vector2(7, 1); t.GetComponent<MeshRenderer>().sortingOrder = 20;
        }
        private void OnDestroy()
        {
            foreach (var r in recursos) if (r != null) Destroy(r);
            if (temaTemporario != null) Destroy(temaTemporario);
        }
    }
}
