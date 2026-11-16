using System;
using System.Collections.Generic;
using FutebolDeDiscos;
using TMPro;
using UnityEngine;

namespace BasqueteArena
{
    [DisallowMultipleComponent]
    public sealed class PartidaBasquete : MonoBehaviour
    {
        private readonly List<BolaArenaBasquete> bolas = new List<BolaArenaBasquete>();
        private ProjetoFutebolDiscos config;
        private ArenaBasquete arena;
        private CampoDiscos campo;
        private HudFutebolDiscos hud;
        private System.Random random;
        private GameObject atores;
        private AudioSource audioArena;
        private float intervalo;
        private bool ativa;
        private int overallA, overallB, capitaoOverallA, capitaoOverallB;
        private const float Raio = .29f;

        public void Preparar(ProjetoFutebolDiscos cfg, CampoDiscos origem, HudFutebolDiscos transmissao,
            TeamData a, TeamData b, List<JogadorDados> repsA, List<JogadorDados> repsB, Color corA, Color corB)
        {
            Parar();
            if (atores != null) { atores.SetActive(false); Destroy(atores); }
            bolas.Clear(); config = cfg; campo = origem; hud = transmissao;
            arena = campo.GetComponent<ArenaBasquete>();
            if (arena == null) throw new InvalidOperationException("A arena de basquete não foi construída.");
            int hash = cfg.SementeEfetiva;
            foreach (char c in a.name + "|" + b.name) unchecked { hash = hash * 31 + c; }
            if (TournamentManager.Instance != null) hash ^= TournamentManager.Instance.rodadaDesejada;
            random = new System.Random(hash);
            overallA = a.overall; overallB = b.overall;
            if (!cfg.TentarObterOverallCapitao(a, out capitaoOverallA)) capitaoOverallA = overallA;
            if (!cfg.TentarObterOverallCapitao(b, out capitaoOverallB)) capitaoOverallB = overallB;
            atores = new GameObject("Bolas de basquete"); atores.transform.SetParent(transform, false);
            if (audioArena == null) { audioArena = gameObject.AddComponent<AudioSource>(); audioArena.playOnAwake = false; audioArena.spatialBlend = 0; }
            CriarEquipe(a, true, corA, cfg.usarCapitaesConvidados ? cfg.SelecionarRepresentantes(a) : repsA);
            CriarEquipe(b, false, corB, cfg.usarCapitaesConvidados ? cfg.SelecionarRepresentantes(b) : repsB);
            // Every pair collides during the contest, even if the project's layer matrix differs.
            for (int i = 0; i < bolas.Count; i++) for (int j = i + 1; j < bolas.Count; j++)
                Physics2D.IgnoreCollision(bolas[i].colisor, bolas[j].colisor, false);
            if (hud != null) { hud.ConfigurarEquipes(a, b, corA, corB); hud.MostrarEstado("CONQUISTE O LANCADOR • SOBREVIVA • FACA A CESTA"); }
            Parar();
        }
        private void CriarEquipe(TeamData time, bool equipeA, Color cor, List<JogadorDados> representantes)
        {
            int total = Mathf.Clamp(config.TotalDiscosPorTime, 1, 5);
            JogadorDados capitao = representantes != null && representantes.Count > 0 ? representantes[0] : null;
            var elenco = time.MontarEscalacaoDiscos(capitao, total);
            for (int i = 0; i < total; i++)
            {
                var go = new GameObject((equipeA ? "A" : "B") + " - " + (i == 0 ? "Capitao" : "Jogador " + i));
                go.transform.SetParent(atores.transform, false);
                var bola = go.AddComponent<BolaArenaBasquete>(); bola.partida = this;
                bola.timeA = equipeA; bola.capitao = i == 0; bola.jogador = elenco[i]; bola.cor = cor;
                bola.imagem = go.AddComponent<SpriteRenderer>(); bola.imagem.sprite = campo.SpriteCirculo;
                bola.imagem.sharedMaterial = campo.MaterialSprites; bola.imagem.color = cor; bola.imagem.sortingOrder = 10;
                go.transform.localScale = Vector3.one * (Raio * 2f);
                bola.corpo = go.AddComponent<Rigidbody2D>(); bola.corpo.gravityScale = 0;
                bola.corpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                bola.corpo.interpolation = RigidbodyInterpolation2D.Interpolate;
                bola.corpo.constraints = RigidbodyConstraints2D.FreezeRotation;
                bola.colisor = go.AddComponent<CircleCollider2D>(); bola.colisor.radius = .5f; bola.colisor.sharedMaterial = arena.MaterialBolas;
                if (bola.jogador != null && bola.jogador.foto512x512 != null)
                {
                    var foto = new GameObject("Foto"); foto.transform.SetParent(go.transform, false);
                    var r = foto.AddComponent<SpriteRenderer>(); r.sprite = bola.jogador.foto512x512; r.sharedMaterial = campo.MaterialSprites; r.sortingOrder = 11;
                    Vector3 size = r.sprite.bounds.size; foto.transform.localScale = Vector3.one * (.7f / Mathf.Max(size.x, size.y));
                }
                var tag = new GameObject("Identificacao"); tag.transform.SetParent(go.transform, false); tag.transform.localPosition = new Vector3(0, .85f, 0);
                var texto = tag.AddComponent<TextMeshPro>(); texto.fontSize = 2.8f; texto.alignment = TextAlignmentOptions.Center;
                texto.text = (bola.capitao ? "★ " : "") + (bola.jogador != null ? bola.jogador.nomeJogador : time.sigla + " " + (i + 1));
                texto.rectTransform.sizeDelta = new Vector2(4, .8f); texto.GetComponent<MeshRenderer>().sortingOrder = 12;
                bolas.Add(bola); Reaparecer(bola, i);
            }
        }
        public void Iniciar()
        {
            ativa = true; intervalo = 0;
            for (int i = 0; i < bolas.Count; i++) { Reaparecer(bolas[i], i % Mathf.Max(1, bolas.Count / 2)); bolas[i].corpo.simulated = true; }
        }
        public void Parar()
        {
            ativa = false;
            foreach (var b in bolas) if (b != null && b.corpo != null) b.corpo.simulated = false;
            if (audioArena != null) audioArena.Stop();
        }
        private void FixedUpdate()
        {
            if (!ativa) return;
            var ui = UniversalUI.Instance;
            if (ui == null || !ui.isActiveAndEnabled || !ui.partidaEmAndamento) { Parar(); return; }
            float dt = Time.fixedDeltaTime; intervalo = Mathf.Max(0, intervalo - dt);
            foreach (var b in bolas)
            {
                if (b.estado == EstadoBola.Retorno)
                {
                    b.espera -= dt;
                    if (b.espera <= 0) Reaparecer(b, random.Next(5));
                    continue;
                }
                b.idade += dt;
                // Local gravity keeps this mode independent of the football project's global gravity.
                b.corpo.AddForce(Vector2.down * (9.81f * arena.Tema.gravidade * b.corpo.mass));
                Vector2 p = b.corpo.position;
                if (b.estado == EstadoBola.Disputa)
                {
                    if (arena.Tema.elemento == ElementoArena.Agua)
                        b.corpo.AddForce(Vector2.right * (Mathf.Sin(b.idade * 1.5f) * arena.Tema.forcaCorrente));
                    if (Vector2.Distance(p, b.ultimaPosicao) < .015f) b.parado += dt; else b.parado = 0;
                    if (b.parado > arena.Tema.tempoDestravar * .5f)
                    {
                        b.corpo.AddForce(new Vector2((float)random.NextDouble() * 2 - 1, 2) * 1.5f, ForceMode2D.Impulse); b.parado = 0;
                    }
                    if (p.y < -4.4f || p.x < -10 || p.x > 1.2f || b.idade > arena.Tema.tempoDestravar * 3f) Eliminar(b);
                }
                else
                {
                    if (p.y > arena.Aro.y + Raio) b.passouPorCima = true;
                    // Swept top-to-bottom test prevents duplicate, upward, side and high-speed scores.
                    if (b.passouPorCima && b.alturaAnterior >= arena.Aro.y && p.y < arena.Aro.y && b.corpo.linearVelocity.y < 0)
                    {
                        float t = (b.alturaAnterior - arena.Aro.y) / Mathf.Max(.0001f, b.alturaAnterior - p.y);
                        float x = Mathf.Lerp(b.ultimaPosicao.x, p.x, t);
                        if (Mathf.Abs(x - arena.Aro.x) < .65f - Raio - .06f)
                        {
                            int pontos = b.valorCesta;
                            Voltar(b, .75f);
                            ui.RegistrarCesta(b.timeA, pontos);
                            if (GoalBannerUI.Instance != null) GoalBannerUI.Instance.ExibirCesta(b.jogador != null ? b.jogador.nomeJogador : "JOGADOR", b.jogador != null ? b.jogador.foto512x512 : null, pontos);
                            if (hud != null) hud.MostrarEstado("CESTA DE " + pontos + " PONTOS!");
                        }
                    }
                    if (b.estado == EstadoBola.Arremesso && (p.y < -4.6f || p.x > 11 || p.x < 1 || b.idade > 8f)) Voltar(b, arena.Tema.tempoRetorno);
                    b.alturaAnterior = p.y;
                }
                b.ultimaPosicao = p;
            }
        }
        public void Eliminar(BolaArenaBasquete b)
        {
            if (!ativa || b.estado != EstadoBola.Disputa) return;
            // A fading elemental flash is separate from the returned ball.
            var flash = arena.Desenhar("Eliminacao " + arena.Tema.elemento, b.corpo.position, Vector2.one * .85f, arena.Tema.corEliminacao, 15, campo.SpriteCirculo);
            flash.color = arena.Tema.corEliminacao;
            flash.gameObject.AddComponent<EfeitoEliminacaoBasquete>();
            if (arena.Tema.somEliminacao != null) audioArena.PlayOneShot(arena.Tema.somEliminacao, .4f);
            Voltar(b, arena.Tema.tempoRetorno);
        }
        public void TentarLancar(BolaArenaBasquete b)
        {
            if (!ativa || intervalo > 0 || b.estado != EstadoBola.Disputa) return;
            intervalo = Mathf.Max(.2f, arena.Tema.intervaloLancador);
            b.estado = EstadoBola.Arremesso; b.idade = 0; b.passouPorCima = false;
            b.valorCesta = random.NextDouble() < .3 ? 3 : 2;
            var saida = arena.Saida + (b.valorCesta == 3 ? Vector2.left * 1f : Vector2.zero);
            b.corpo.position = saida; b.ultimaPosicao = saida; b.alturaAnterior = saida.y;
            ConfigurarColisoes(b, true);
            float g = 9.81f * arena.Tema.gravidade;
            float altura = arena.Aro.y + 2.2f;
            float vy = Mathf.Sqrt(2 * g * (altura - saida.y));
            float tempo = vy / g + Mathf.Sqrt(2 * (altura - arena.Aro.y) / g);
            int overall = b.timeA ? overallA : overallB;
            if (b.capitao && config.usarCapitaesConvidados) overall = b.timeA ? capitaoOverallA : capitaoOverallB;
            float dispersao = arena.Tema.dispersaoArremesso * (arena.Tema.usarOverall ? Mathf.Lerp(1.3f, .8f, Mathf.Clamp01(overall / 100f)) : 1f);
            float erro = ((float)random.NextDouble() * 2 - 1) * dispersao;
            b.corpo.linearVelocity = new Vector2((arena.Aro.x + erro - saida.x) / tempo, vy);
            if (arena.Tema.somLancamento != null) audioArena.PlayOneShot(arena.Tema.somLancamento, .4f);
            if (hud != null) hud.MostrarEstado((b.capitao ? "CAPITAO" : "JOGADOR") + " • TENTATIVA DE " + b.valorCesta + " PONTOS");
        }
        private void Voltar(BolaArenaBasquete b, float espera)
        {
            b.estado = EstadoBola.Retorno; b.espera = Mathf.Max(.1f, espera);
            b.corpo.linearVelocity = Vector2.zero; b.corpo.simulated = false;
            b.gameObject.SetActive(false);
        }
        private void Reaparecer(BolaArenaBasquete b, int indice)
        {
            b.gameObject.SetActive(true); b.estado = EstadoBola.Disputa; b.idade = b.parado = 0; b.passouPorCima = false;
            // Mirrored spawn slots; no permanent priority for either team.
            float x = -4.5f + (b.timeA ? -1 : 1) * (.45f + (indice % 5) * .65f);
            b.corpo.position = new Vector2(x, 2.7f + (indice % 5) * .35f);
            b.corpo.linearVelocity = new Vector2((b.timeA ? 1 : -1) * .6f, 0);
            b.ultimaPosicao = b.corpo.position; b.corpo.simulated = true;
            ConfigurarColisoes(b, false);
        }
        private void ConfigurarColisoes(BolaArenaBasquete b, bool arremesso)
        {
            foreach (var c in arena.ColisoresDisputa) if (c != null) Physics2D.IgnoreCollision(b.colisor, c, arremesso);
            foreach (var c in arena.ColisoresCesta) if (c != null) Physics2D.IgnoreCollision(b.colisor, c, !arremesso);
            foreach (var outra in bolas) if (outra != b) Physics2D.IgnoreCollision(b.colisor, outra.colisor, arremesso || outra.estado != EstadoBola.Disputa);
        }
        private void OnDisable() { Parar(); }
        private void OnDestroy() { if (atores != null) Destroy(atores); }
    }
    public sealed class EfeitoEliminacaoBasquete : MonoBehaviour
    {
        private float tempo;
        private void Update()
        {
            tempo += Time.deltaTime;
            var r = GetComponent<SpriteRenderer>(); if (r != null) { var c = r.color; c.a = Mathf.Max(0, 1 - tempo * 2); r.color = c; }
            transform.localScale *= 1 + Time.deltaTime;
            if (tempo >= .5f) Destroy(gameObject);
        }
    }
}
