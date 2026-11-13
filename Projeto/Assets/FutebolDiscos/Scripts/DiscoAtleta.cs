using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FutebolDeDiscos
{
    public sealed class DiscoAtleta : MonoBehaviour
    {
        public TeamData Equipe { get; private set; }
        public bool TimeA { get; private set; }
        public int Indice { get; private set; }
        public JogadorDados Jogador { get; private set; }
        public Rigidbody2D Corpo { get; private set; }
        private PartidaDiscos partida;
        private float proximoChute;

        public void Criar(PartidaDiscos match, TeamData equipe, bool timeA, int indice, JogadorDados jogador,
            Color cor, Sprite circulo, Material material, PhysicsMaterial2D fisica, TMP_FontAsset fonte)
        {
            partida = match; Equipe = equipe; TimeA = timeA; Indice = indice; Jogador = jogador;
            transform.localScale = Vector3.one;
            float d = partida.Configuracao.tamanhoDisco;
            Corpo = gameObject.AddComponent<Rigidbody2D>();
            Corpo.gravityScale = 0f; Corpo.mass = 1.8f; Corpo.linearDamping = 0.6f;
            Corpo.constraints = RigidbodyConstraints2D.FreezeRotation;
            Corpo.interpolation = RigidbodyInterpolation2D.Interpolate;
            Corpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Corpo.sleepMode = RigidbodySleepMode2D.NeverSleep;
            var collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = d * 0.5f; collider.sharedMaterial = fisica;
            Imagem("Disc Shadow", circulo, material, new Color(0f, 0f, 0f, .65f), d * 1.1f, 12);
            Imagem("Team Ring", circulo, material, cor, d, 13);
            Imagem("Inner Plate", circulo, material, new Color(.025f, .055f, .09f), d * .83f, 14);
            if (equipe.escudoOuBandeira != null)
            {
                Sprite logo = equipe.escudoOuBandeira;
                Imagem("Team Identity", logo, material, Color.white, d * .68f, 15);
            }
            else if (fonte != null)
            {
                var label = new GameObject("Disc Number", typeof(RectTransform));
                label.transform.SetParent(transform, false);
                var text = label.AddComponent<TextMeshPro>();
                text.font = fonte; text.text = (indice + 1).ToString(); text.fontSize = 5f;
                text.color = Color.white; text.alignment = TextAlignmentOptions.Center;
                text.rectTransform.sizeDelta = new Vector2(d, d);
            }
            Corpo.simulated = false;
        }

        private void Imagem(string nome, Sprite sprite, Material material, Color cor, float tamanho, int ordem)
        {
            var go = new GameObject(nome); go.transform.SetParent(transform, false);
            var render = go.AddComponent<SpriteRenderer>();
            render.sprite = sprite; render.sharedMaterial = material; render.color = cor; render.sortingOrder = ordem;
            float lado = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            float s = tamanho / Mathf.Max(.0001f, lado); go.transform.localScale = new Vector3(s, s, 1f);
        }

        public void Mover(float tempo, float dt, IList<DiscoAtleta> atletas, BolaDiscos bola, bool prorrogacao)
        {
            if (Corpo == null || !Corpo.simulated) return;
            ProjetoFutebolDiscos cfg = partida.Configuracao;
            Vector2 pos = Corpo.position;
            Vector2 alvo = partida.Formacao(TimeA, Indice);
            float fase = Indice * 2.13f + (TimeA ? .5f : 2.8f);
            alvo += new Vector2(Mathf.Sin(tempo * .8f + fase) * 1.25f, Mathf.Cos(tempo * .63f + fase) * 1.3f);
            bool bolaNaMetade = TimeA ? bola.Corpo.position.x <= .7f : bola.Corpo.position.x >= -.7f;
            int maisPerto = 0;
            float distancia = Vector2.Distance(pos, bola.Corpo.position);
            foreach (DiscoAtleta outro in atletas)
                if (outro != this && outro.TimeA == TimeA && Vector2.Distance(outro.Corpo.position, bola.Corpo.position) < distancia)
                    maisPerto++;
            if (bolaNaMetade && maisPerto < (cfg.discosPorTime > 2 ? 2 : 1))
                alvo = bola.Corpo.position - new Vector2(TimeA ? .45f : -.45f, 0f);
            else if (!cfg.limitarDiscosNaMetade && maisPerto == 0)
                alvo = bola.Corpo.position - new Vector2(TimeA ? .5f : -.5f, 0f);

            float raio = cfg.tamanhoDisco * .5f;
            float limite = CampoDiscos.MeioComprimento - raio - .15f;
            float minX = -limite, maxX = limite;
            if (cfg.limitarDiscosNaMetade) { if (TimeA) maxX = -raio; else minX = raio; }
            alvo.x = Mathf.Clamp(alvo.x, minX, maxX);
            alvo.y = Mathf.Clamp(alvo.y, -CampoDiscos.MeioLargura + raio + .1f, CampoDiscos.MeioLargura - raio - .1f);
            Vector2 corrigida = new Vector2(Mathf.Clamp(pos.x, minX, maxX),
                Mathf.Clamp(pos.y, -CampoDiscos.MeioLargura + raio, CampoDiscos.MeioLargura - raio));
            if (Vector2.Distance(pos, corrigida) > .01f) { Corpo.position = corrigida; pos = corrigida; }
            float habilidade = cfg.usarForcaDosTimes ? .88f + Mathf.Clamp(Equipe.overall, 1, 100) / 100f * .24f : 1f;
            float velocidade = cfg.velocidadeDiscos * habilidade * (prorrogacao ? 1.12f : 1f);
            Vector2 diferenca = alvo - pos;
            Vector2 desejada = diferenca.magnitude > .18f ? diferenca.normalized * velocidade : Vector2.zero;
            Corpo.AddForce((desejada - Corpo.linearVelocity) * Corpo.mass * 4.3f);
            Corpo.linearVelocity = Vector2.ClampMagnitude(Corpo.linearVelocity, velocidade * 1.5f);
        }

        public bool PodeChutar(float tempo)
        {
            if (tempo < proximoChute) return false;
            proximoChute = tempo + .42f;
            return true;
        }
        public void Repor(Vector2 pos, bool simular)
        {
            RigidbodyInterpolation2D interpolacao = Corpo.interpolation;
            Corpo.interpolation = RigidbodyInterpolation2D.None;
            Corpo.simulated = false;
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
            Corpo.position = pos;
            Corpo.simulated = simular;
            Corpo.position = pos;
            Corpo.linearVelocity = Vector2.zero;
            Corpo.angularVelocity = 0f;
            Corpo.interpolation = interpolacao;
            if (simular) Corpo.WakeUp();
            proximoChute = 0f;
        }
        public void Congelar(bool parar) { if (Corpo != null) { Corpo.linearVelocity = Vector2.zero; Corpo.angularVelocity = 0f; Corpo.simulated = !parar; } }
        private void OnCollisionEnter2D(Collision2D collision) { Contato(collision); }
        private void OnCollisionStay2D(Collision2D collision) { Contato(collision); }
        private void Contato(Collision2D collision)
        {
            if (collision.collider.GetComponent<BolaDiscos>() != null) partida.TentarChute(this);
        }
    }
}
