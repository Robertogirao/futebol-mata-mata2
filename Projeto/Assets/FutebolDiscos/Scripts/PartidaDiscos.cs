using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace FutebolDeDiscos
{
    /// <summary>Automatic top-down football: a fixed team of physical discs and one scoring ball.</summary>
    [DisallowMultipleComponent]
    public sealed class PartidaDiscos : MonoBehaviour
    {
        public static PartidaDiscos Instance { get; private set; }
        public bool EmAndamento { get; private set; }
        public bool EmReposicao { get; private set; }
        public IReadOnlyList<DiscoAtleta> Atletas { get { return atletas; } }
        public BolaDiscos Bola { get { return bola; } }
        public ProjetoFutebolDiscos Configuracao { get { return config; } }

        private ProjetoFutebolDiscos config;
        private CampoDiscos campo;
        private HudFutebolDiscos hud;
        private TeamData timeA, timeB;
        private Color corA, corB;
        private GameObject raiz;
        private BolaDiscos bola;
        private readonly List<DiscoAtleta> atletas = new List<DiscoAtleta>();
        private System.Random random;
        private float reposicaoRestante, tempoMovimento, tempoBolaLenta;
        private Vector2 centroBolaLenta;
        private bool saidaA;
        private bool golLiberado, aguardandoConfirmacaoSaida;
        private int passosConfirmacaoSaida;
        private int cantoMonitorado = -1;
        private float tempoCantoPreso;
        private Vector2 ancoraCanto;
        private bool cobrancaBolaPresa;
        private Vector2 posicaoCobranca, velocidadeCobranca;
        private DiscoAtleta cobradorReposicao;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Configurar(ProjetoFutebolDiscos configuracao, CampoDiscos estadio, HudFutebolDiscos transmissao)
        {
            config = configuracao;
            campo = estadio;
            hud = transmissao;
        }

        public void PrepararPartida(TeamData a, TeamData b, List<JogadorDados> repsA, List<JogadorDados> repsB, Color colorA, Color colorB)
        {
            if (config == null || campo == null || campo.SpriteCirculo == null)
                throw new InvalidOperationException("Configure o campo de discos antes de preparar a partida.");
            if (a == null || b == null || ReferenceEquals(a, b))
                throw new InvalidOperationException("Escolha duas equipes diferentes.");
            LimparJogadores();
            timeA = a; timeB = b; corA = colorA; corB = colorB;
            int rodada = TournamentManager.Instance != null ? TournamentManager.Instance.rodadaDesejada : 0;
            random = new System.Random(Hash(a.name + "|" + b.name, config.SementeEfetiva ^ rodada));
            raiz = new GameObject("Match Actors");
            raiz.transform.SetParent(transform, false);
            CriarEquipe(a, true, repsA, colorA);
            CriarEquipe(b, false, repsB, colorB);
            var objetoBola = new GameObject("Match Ball");
            objetoBola.transform.SetParent(raiz.transform, false);
            bola = objetoBola.AddComponent<BolaDiscos>();
            bola.Criar(this, config.tamanhoBola, campo.SpriteCirculo, campo.MaterialSprites, campo.MaterialFisica);
            if (hud != null) { hud.ConfigurarEquipes(a, b, colorA, colorB); hud.MostrarEstado("READY FOR KICKOFF"); }
            Reposicionar(false);
            PararPartida();
        }

        private void CriarEquipe(TeamData equipe, bool equipeA, List<JogadorDados> representantes, Color color)
        {
            var elenco = (equipe.elenco ?? new List<JogadorDados>())
                .Where(j => j != null && !string.IsNullOrWhiteSpace(j.nomeJogador)).Distinct().ToList();
            JogadorDados capitao = representantes != null
                ? representantes.FirstOrDefault(j => j != null && !string.IsNullOrWhiteSpace(j.nomeJogador)) : null;
            if (capitao == null) capitao = elenco.FirstOrDefault();
            var demais = elenco.Where(j => !ReferenceEquals(j, capitao)).ToList();
            for (int i = 0; i < config.discosPorTime; i++)
            {
                var go = new GameObject((equipeA ? "Home Disc " : "Away Disc ") + (i + 1));
                go.transform.SetParent(raiz.transform, false);
                var atleta = go.AddComponent<DiscoAtleta>();
                JogadorDados jogador = i == 0 ? capitao : i - 1 < demais.Count ? demais[i - 1] : null;
                atleta.Criar(this, equipe, equipeA, i, jogador, color, campo.SpriteCirculo, campo.MaterialSprites,
                    campo.MaterialFisica, config.tema != null ? config.tema.fonteTexto : TMP_Settings.defaultFontAsset);
                atletas.Add(atleta);
            }
        }

        public void IniciarPartida(TeamData a, TeamData b, float duracao, List<JogadorDados> repsA, List<JogadorDados> repsB, Color colorA, Color colorB)
        {
            // Tournament duration is owned by UniversalUI, including Golden Goal.
            if (bola == null || !ReferenceEquals(a, timeA) || !ReferenceEquals(b, timeB))
                PrepararPartida(a, b, repsA, repsB, colorA, colorB);
            EmAndamento = true;
            EmReposicao = false;
            tempoMovimento = tempoBolaLenta = 0f;
            saidaA = random.Next(2) == 0;
            Reposicionar(true);
            if (hud != null) hud.MostrarEstado("KICKOFF");
        }

        public void PararPartida()
        {
            EmAndamento = false;
            EmReposicao = false;
            golLiberado = aguardandoConfirmacaoSaida = false;
            cobrancaBolaPresa = false;
            cobradorReposicao = null;
            ReiniciarMonitorCanto();
            Congelar(true);
        }

        private void Update()
        {
            if (!EmAndamento) return;
            UniversalUI ui = UniversalUI.Instance;
            if (ui == null || !ui.isActiveAndEnabled || !ui.partidaEmAndamento) { PararPartida(); return; }
            if (EmReposicao && !aguardandoConfirmacaoSaida)
            {
                reposicaoRestante -= PassoVideo();
                if (reposicaoRestante <= 0f)
                {
                    if (cobrancaBolaPresa) ColocarCobranca(true);
                    else Reposicionar(true);
                }
            }
        }

        private void FixedUpdate()
        {
            if (!EmAndamento || bola == null) return;
            UniversalUI ui = UniversalUI.Instance;
            if (ui == null || !ui.isActiveAndEnabled || !ui.partidaEmAndamento) { PararPartida(); return; }
            if (aguardandoConfirmacaoSaida)
            {
                // A timer cannot rearm a goal. Confirm the physical AND rendered
                // restart poses across a simulation step before accepting another one.
                Vector2 destino = cobrancaBolaPresa ? posicaoCobranca : Vector2.zero;
                float tolerancia = Mathf.Max(0.25f, config.tamanhoBola);
                if (!bola.Corpo.simulated ||
                    Vector2.Distance(bola.Corpo.position, destino) > tolerancia ||
                    Vector2.Distance((Vector2)bola.transform.position, destino) > tolerancia)
                {
                    if (cobrancaBolaPresa) ColocarCobranca(true);
                    else Reposicionar(true);
                    return;
                }
                if (passosConfirmacaoSaida++ == 0) return;
                aguardandoConfirmacaoSaida = false;
                golLiberado = true;
                EmReposicao = false;
                if (cobrancaBolaPresa)
                {
                    bola.RegistrarToque(cobradorReposicao);
                    bola.Corpo.linearVelocity = velocidadeCobranca;
                    bola.LimitarVelocidade(config.velocidadeMaximaBola);
                    cobrancaBolaPresa = false;
                    cobradorReposicao = null;
                }
                if (hud != null) hud.MostrarEstado(ui.EmProrrogacao ? "GOLDEN GOAL" : "BACK IN PLAY");
            }
            if (EmReposicao) return;
            float dt = Time.fixedDeltaTime;
            tempoMovimento += dt;
            TaticaDiscos.Atualizar(atletas, bola, config);
            for (int i = 0; i < atletas.Count; i++) atletas[i].Mover(tempoMovimento, dt, atletas, bola, ui.EmProrrogacao);
            bola.LimitarVelocidade(config.velocidadeMaximaBola);
            bool monitorandoCanto = MonitorarBolaPresaNoCanto(dt);
            if (EmReposicao) return;
            if (Vector2.Distance(bola.Corpo.position, centroBolaLenta) > 0.45f)
            {
                centroBolaLenta = bola.Corpo.position;
                tempoBolaLenta = 0f;
            }
            else tempoBolaLenta += dt;
            // The random rescue impulse must not interrupt the corner's own timer.
            if (monitorandoCanto) tempoBolaLenta = 0f;
            else if (tempoBolaLenta > (ui.EmProrrogacao ? 1.4f : 2.8f))
            {
                // Free a trapped real ball without removing it or awarding a score.
                Vector2 direction = new Vector2(random.Next(2) == 0 ? -1f : 1f, (float)random.NextDouble() - 0.5f).normalized;
                bola.Corpo.AddForce(direction * 2.4f, ForceMode2D.Impulse);
                tempoBolaLenta = 0f;
            }
            Vector2 p = bola.Corpo.position;
            if (Mathf.Abs(p.x) > CampoDiscos.MeioComprimento + CampoDiscos.ProfundidadeGol + 1f || Mathf.Abs(p.y) > CampoDiscos.MeioLargura + 1f)
            {
                bola.Repor(Vector2.zero, new Vector2(random.Next(2) == 0 ? -2f : 2f, 0.7f));
                centroBolaLenta = Vector2.zero;
                tempoBolaLenta = 0f;
                ReiniciarMonitorCanto();
            }
        }

        private bool MonitorarBolaPresaNoCanto(float dt)
        {
            Vector2 p = bola.Corpo.position;
            float alcance = config.alcanceCantoBolaPresa;
            DiscoAtleta ultimo = bola.UltimoToque;
            // A corner needs both boundaries, actual match possession and sustained
            // lack of progress. Speed alone misses a ball jittering between discs.
            if (!config.reporBolaPresaNoCanto || ultimo == null || !atletas.Contains(ultimo) ||
                Mathf.Abs(p.x) < CampoDiscos.MeioComprimento - alcance ||
                Mathf.Abs(p.y) < CampoDiscos.MeioLargura - alcance ||
                Mathf.Abs(p.x) > CampoDiscos.MeioComprimento || Mathf.Abs(p.y) > CampoDiscos.MeioLargura)
            {
                ReiniciarMonitorCanto();
                return false;
            }
            int canto = (p.x < 0f ? 0 : 2) + (p.y < 0f ? 0 : 1);
            if (canto != cantoMonitorado || Vector2.Distance(p, ancoraCanto) > .45f)
            {
                cantoMonitorado = canto;
                ancoraCanto = p;
                tempoCantoPreso = 0f;
            }
            else tempoCantoPreso += dt;
            if (tempoCantoPreso >= config.tempoBolaPresaNoCanto) IniciarCobrancaBolaPresa(p, ultimo);
            return true;
        }

        private void ReiniciarMonitorCanto()
        {
            cantoMonitorado = -1;
            tempoCantoPreso = 0f;
        }

        private void IniciarCobrancaBolaPresa(Vector2 canto, DiscoAtleta ultimo)
        {
            bool defensorA = canto.x < 0f;
            bool escanteio = ultimo.TimeA == defensorA;
            bool cobrancaA = escanteio ? !defensorA : defensorA;
            float horizontal = canto.x < 0f ? -1f : 1f;
            float vertical = canto.y < 0f ? -1f : 1f;
            float margem = Mathf.Max(.38f, config.tamanhoBola * .5f + .16f);
            posicaoCobranca = escanteio
                ? new Vector2(horizontal * (CampoDiscos.MeioComprimento - margem), vertical * (CampoDiscos.MeioLargura - margem))
                : new Vector2(horizontal * (CampoDiscos.MeioComprimento - 1.15f), vertical * .9f);
            Vector2 alvo = escanteio
                ? new Vector2(horizontal * (CampoDiscos.MeioComprimento - 3.2f), -vertical * .6f)
                : new Vector2(0f, -vertical * .4f);
            velocidadeCobranca = (alvo - posicaoCobranca).normalized *
                Mathf.Min(config.velocidadeMaximaBola, Mathf.Max(4f, config.forcaChute * 1.2f));
            cobradorReposicao = null;
            float distancia = float.MaxValue;
            foreach (DiscoAtleta atleta in atletas)
            {
                if (atleta.TimeA != cobrancaA) continue;
                float d = Vector2.Distance(atleta.Corpo.position, canto);
                if (d < distancia) { distancia = d; cobradorReposicao = atleta; }
            }
            cobrancaBolaPresa = true;
            golLiberado = false;
            EmReposicao = true;
            reposicaoRestante = config.pausaReposicaoBolaPresa;
            ColocarCobranca(false);
            TeamData equipe = cobrancaA ? timeA : timeB;
            if (hud != null) hud.MostrarEstado((escanteio ? "CORNER KICK — " : "GOAL KICK — ") + equipe.nomeEquipe.ToUpperInvariant());
        }

        private void ColocarCobranca(bool simular)
        {
            // Automatic kick: release the crowd without moving a player across
            // tactical half-field limits. Keep the same ball and synchronize poses.
            foreach (DiscoAtleta atleta in atletas) atleta.Repor(Formacao(atleta.TimeA, atleta.Indice), simular);
            bola.Repor(posicaoCobranca, Vector2.zero, simular);
            Physics2D.SyncTransforms();
            aguardandoConfirmacaoSaida = simular;
            passosConfirmacaoSaida = 0;
            centroBolaLenta = posicaoCobranca;
            tempoBolaLenta = 0f;
            ReiniciarMonitorCanto();
        }

        public void NotificarGol(bool golEsquerdo)
        {
            UniversalUI ui = UniversalUI.Instance;
            if (!EmAndamento || !golLiberado || EmReposicao || bola == null || ui == null || !ui.isActiveAndEnabled || !ui.partidaEmAndamento) return;
            float crossed = golEsquerdo ? -bola.Corpo.position.x : bola.Corpo.position.x;
            if (crossed < CampoDiscos.MeioComprimento + config.tamanhoBola * 0.5f) return;
            if (Mathf.Abs(bola.Corpo.position.y) + config.tamanhoBola * 0.5f > CampoDiscos.MeioGol) return;
            bool pontoA = !golEsquerdo;
            DiscoAtleta ultimo = bola.UltimoToque;
            TeamData equipe = pontoA ? timeA : timeB;
            string nomeAutor = ultimo != null ? ultimo.NomeExibicao : equipe.nomeEquipe;
            Sprite imagemAutor = ultimo != null ? ultimo.ImagemDoGol : equipe.escudoOuBandeira;
            if (ultimo != null && ultimo.TimeA != pontoA) nomeAutor += " (OG)";
            golLiberado = false;
            EmReposicao = true;
            Congelar(true);
            saidaA = !pontoA;
            reposicaoRestante = config.pausaAposGol;
            campo.CelebrarGol(golEsquerdo);
            if (hud != null) hud.MostrarEstado("GOAL — " + equipe.nomeEquipe.ToUpperInvariant());
            ui.RegistrarGol(pontoA, ui.TempoAtual);
            if (GoalBannerUI.Instance != null)
                GoalBannerUI.Instance.ExibirBannerDeGol(nomeAutor, imagemAutor, pontoA ? corA : corB);
            if (!ui.partidaEmAndamento) PararPartida();
        }

        public void TentarChute(DiscoAtleta atleta)
        {
            if (!EmAndamento || !golLiberado || EmReposicao || bola == null || atleta == null || !atleta.PodeChutar(tempoMovimento)) return;
            bool ataqueA = atleta.TimeA;
            float objetivoX = ataqueA ? CampoDiscos.MeioComprimento + 0.4f : -CampoDiscos.MeioComprimento - 0.4f;
            float mira = ((float)random.NextDouble() * 2f - 1f) * CampoDiscos.MeioGol * 0.86f;
            float habilidade = config.usarForcaDosTimes ? Mathf.Clamp(atleta.Equipe.overall, 1, 100) / 100f : 0.75f;
            mira += ((float)random.NextDouble() * 2f - 1f) * (1f - habilidade) * 1.2f;
            Vector2 direction = (new Vector2(objetivoX, mira) - bola.Corpo.position).normalized;
            bola.RegistrarToque(atleta);
            bola.Corpo.AddForce(direction * config.forcaChute * (0.82f + (float)random.NextDouble() * 0.18f), ForceMode2D.Impulse);
            bola.LimitarVelocidade(config.velocidadeMaximaBola);
        }

        public Vector2 Formacao(bool equipeA, int index)
        {
            float side = equipeA ? -1f : 1f;
            if (config.discosPorTime == 1) return new Vector2(side * 5.5f, 0f);
            if (config.discosPorTime == 2) return new Vector2(side * (index == 0 ? 7f : 3.6f), index == 0 ? -1.8f : 1.8f);
            if (index == 0) return new Vector2(side * 8.5f, 0f);
            float lane = (index - 1f) / Mathf.Max(1f, config.discosPorTime - 2f);
            return new Vector2(side * (index % 2 == 0 ? 3.7f : 5.7f), Mathf.Lerp(-3f, 3f, lane));
        }

        private void Reposicionar(bool simular)
        {
            cobrancaBolaPresa = false;
            cobradorReposicao = null;
            ReiniciarMonitorCanto();
            golLiberado = false;
            aguardandoConfirmacaoSaida = simular;
            passosConfirmacaoSaida = 0;
            foreach (DiscoAtleta atleta in atletas) atleta.Repor(Formacao(atleta.TimeA, atleta.Indice), simular);
            Vector2 launch = new Vector2(saidaA ? -1.6f : 1.6f, (float)random.NextDouble() * 1.2f - 0.6f);
            bola.Repor(Vector2.zero, simular ? launch : Vector2.zero, simular);
            Physics2D.SyncTransforms();
            centroBolaLenta = Vector2.zero;
            tempoBolaLenta = 0f;
        }

        private void Congelar(bool congelar)
        {
            foreach (DiscoAtleta atleta in atletas) if (atleta != null) atleta.Congelar(congelar);
            if (bola != null) { bola.Corpo.linearVelocity = Vector2.zero; bola.Corpo.simulated = !congelar; }
        }

        private void LimparJogadores()
        {
            PararPartida();
            if (raiz != null) { raiz.SetActive(false); Destroy(raiz); }
            raiz = null; bola = null; atletas.Clear();
        }

        private static int Hash(string text, int seed)
        {
            unchecked { int h = seed ^ 216613626; foreach (char c in text) h = (h ^ c) * 16777619; return h; }
        }
        private static float PassoVideo() { return Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime; }
        private void OnDisable() { PararPartida(); }
        private void OnDestroy() { LimparJogadores(); if (Instance == this) Instance = null; }
    }
}
