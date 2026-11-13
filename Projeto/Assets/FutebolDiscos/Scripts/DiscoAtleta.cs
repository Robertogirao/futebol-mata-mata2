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
        public bool Pressionando { get; private set; }
        public bool Recuando { get; private set; }
        public bool Capitao { get { return Indice == 0; } }
        public string NomeExibicao
        {
            get
            {
                if (Jogador != null && !string.IsNullOrWhiteSpace(Jogador.nomeJogador)) return Jogador.nomeJogador.Trim();
                string equipe = Equipe != null && !string.IsNullOrWhiteSpace(Equipe.sigla) ? Equipe.sigla.Trim()
                    : Equipe != null && !string.IsNullOrWhiteSpace(Equipe.nomeEquipe) ? Equipe.nomeEquipe.Trim() : "PLAYER";
                return equipe + " " + (Indice + 1);
            }
        }
        public Sprite ImagemDoGol
        {
            get
            {
                return Capitao && Jogador != null && Jogador.foto512x512 != null ? Jogador.foto512x512
                    : Equipe != null ? Equipe.escudoOuBandeira : null;
            }
        }
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
            bool destacar = Capitao && partida.Configuracao.mostrarMarcaCapitao;
            Color ouro = new Color(1f, .81f, .2f);
            if (destacar) Imagem("Captain Ring", circulo, material, ouro, d * 1.16f, 12);
            Imagem("Team Ring", circulo, material, cor, d, 13);
            Imagem("Inner Plate", circulo, material, new Color(.025f, .055f, .09f), d * .83f, 14);
            Sprite identidade = Capitao && partida.Configuracao.usarFotoNoDiscoCapitao ? ImagemDoGol : equipe.escudoOuBandeira;
            if (identidade != null)
            {
                Imagem("Team Identity", identidade, material, Color.white, d * .68f, 15);
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
            if (fonte != null)
            {
                if (destacar)
                {
                    var badge = new GameObject("Captain Badge"); badge.transform.SetParent(transform, false);
                    badge.transform.localPosition = new Vector3(d * .36f, d * .36f, 0f);
                    var image = badge.AddComponent<SpriteRenderer>(); image.sprite = circulo;
                    image.sharedMaterial = material; image.color = ouro; image.sortingOrder = 24;
                    float s = d * .34f / Mathf.Max(.0001f, Mathf.Max(circulo.bounds.size.x, circulo.bounds.size.y));
                    badge.transform.localScale = new Vector3(s, s, 1f);
                    TextoMundo("Captain C", "C", new Vector2(d * .36f, d * .36f), d * .3f, d * .3f,
                        d * 3.2f, Color.black, fonte, 25);
                }
                if (partida.Configuracao.mostrarTagsJogadores)
                {
                    float largura = Mathf.Clamp(partida.Configuracao.larguraTags, 1f, 4f);
                    float y = -d * .5f - .31f;
                    var fundo = new GameObject("Player Tag Background"); fundo.transform.SetParent(transform, false);
                    fundo.transform.localPosition = new Vector3(0f, y, 0f);
                    var image = fundo.AddComponent<SpriteRenderer>(); image.sprite = circulo; image.sharedMaterial = material;
                    image.color = new Color(.005f, .02f, .03f, .88f); image.sortingOrder = 21;
                    fundo.transform.localScale = new Vector3(largura, .46f, 1f);
                    TextoMundo("Player Name Tag", NomeExibicao, new Vector2(0f, y), largura * .92f, .43f,
                        Mathf.Clamp(partida.Configuracao.tamanhoFonteTags, 2f, 8f), Color.white, fonte, 22);
                }
            }
            Corpo.simulated = false;
        }

        private void TextoMundo(string nome, string valor, Vector2 pos, float largura, float altura,
            float tamanho, Color cor, TMP_FontAsset fonte, int ordem)
        {
            var go = new GameObject(nome, typeof(RectTransform)); go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            var text = go.AddComponent<TextMeshPro>(); text.font = fonte; text.text = valor;
            text.fontSize = tamanho; text.fontStyle = FontStyles.Bold; text.color = cor;
            text.enableAutoSizing = true; text.fontSizeMin = Mathf.Min(tamanho, 1.5f); text.fontSizeMax = tamanho;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis;
            text.richText = false; text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(largura, altura);
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null) renderer = go.AddComponent<MeshRenderer>();
            renderer.sortingOrder = ordem;
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
            if (Pressionando && !cfg.limitarDiscosNaMetade)
            {
                int ordemPressao = 0;
                foreach (DiscoAtleta outro in atletas)
                {
                    if (outro == this || outro.TimeA != TimeA || !outro.Pressionando) continue;
                    float distanciaOutro = Vector2.Distance(outro.Corpo.position, bola.Corpo.position);
                    if (distanciaOutro < distancia - .001f ||
                        (Mathf.Abs(distanciaOutro - distancia) <= .001f && outro.Indice < Indice)) ordemPressao++;
                }
                float sinal = TimeA ? 1f : -1f;
                if (ordemPressao == 0)
                    alvo = bola.Corpo.position - new Vector2(sinal * .45f, 0f);
                else
                {
                    // Support in different lanes rather than stacking every disc on the ball.
                    float progresso = Mathf.Max(1f, sinal * bola.Corpo.position.x - ordemPressao * 1.8f);
                    alvo = new Vector2(sinal * progresso, bola.Corpo.position.y + (ordemPressao == 1 ? 1.6f : -1.6f));
                }
            }
            else if (!Recuando && bolaNaMetade && maisPerto < (cfg.discosPorTime > 2 ? 2 : 1))
                alvo = bola.Corpo.position - new Vector2(TimeA ? .45f : -.45f, 0f);

            float raio = cfg.tamanhoDisco * .5f;
            float limite = CampoDiscos.MeioComprimento - raio - .15f;
            float minX = -limite, maxX = limite;
            bool sairDaMetade = !cfg.limitarDiscosNaMetade && (Pressionando || Recuando);
            if (!sairDaMetade) { if (TimeA) maxX = -raio; else minX = raio; }
            float alvoMinX = minX, alvoMaxX = maxX;
            if (Recuando || !Pressionando)
            {
                if (TimeA) alvoMaxX = Mathf.Min(alvoMaxX, -raio - .15f);
                else alvoMinX = Mathf.Max(alvoMinX, raio + .15f);
            }
            alvo.x = Mathf.Clamp(alvo.x, alvoMinX, alvoMaxX);
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
            if (!sairDaMetade)
            {
                Vector2 v = Corpo.linearVelocity;
                if ((TimeA && pos.x + v.x * dt > maxX) || (!TimeA && pos.x + v.x * dt < minX))
                { v.x = 0f; Corpo.linearVelocity = v; }
            }
        }

        internal void DefinirPressao(bool pressionando, bool recuando)
        {
            Pressionando = pressionando;
            Recuando = recuando;
        }

        public bool PodeChutar(float tempo)
        {
            if (tempo < proximoChute) return false;
            proximoChute = tempo + .42f;
            return true;
        }
        public void Repor(Vector2 pos, bool simular)
        {
            DefinirPressao(false, false);
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
