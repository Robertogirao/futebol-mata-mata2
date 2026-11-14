using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FutebolDeDiscos
{
    /// <summary>A fixed-step marble simulation. Fresh assignments are made only at the finish line.</summary>
    public sealed class MotorSorteioCapitaes
    {
        public const float Largura = 1080f;
        public const float Altura = 620f;
        public const float PassoFixo = 1f / 120f;
        public const float InicioRetaFinal = 14f;

        public enum TipoObstaculo { Pino, Rampa, Giratorio }

        public sealed class MarbleState
        {
            public CapitaoConvidado Capitao { get; internal set; }
            public Vector2 Posicao { get; internal set; }
            public Vector2 Velocidade { get; internal set; }
            public float Raio { get; internal set; }
            public bool Concluida { get; internal set; }
            public bool Fixa { get; internal set; }
            public TeamData TimeAtribuido { get; internal set; }
            public int Colisoes { get; internal set; }
            public int ColisoesObstaculos { get; internal set; }
            internal float LiberarEm;
        }

        public sealed class ObstacleState
        {
            public TipoObstaculo Tipo { get; internal set; }
            public Vector2 PontoA { get; internal set; }
            public Vector2 PontoB { get; internal set; }
            public Vector2 Centro { get { return (PontoA + PontoB) * .5f; } }
            public float Raio { get; internal set; }
            public bool Ativo { get; internal set; }
            internal Vector2 Eixo;
            internal float MeiaExtensao, Fase, Rotacao;
        }

        private readonly List<MarbleState> marbles = new List<MarbleState>();
        private readonly List<ObstacleState> obstacles = new List<ObstacleState>();
        private readonly List<RegistroCapitaoConvidado> resultados = new List<RegistroCapitaoConvidado>();
        private readonly Dictionary<string, TeamData> destinosFixos = new Dictionary<string, TeamData>(StringComparer.Ordinal);
        private readonly Dictionary<string, RegistroCapitaoConvidado> registrosSalvos = new Dictionary<string, RegistroCapitaoConvidado>(StringComparer.Ordinal);
        private readonly List<TeamData> timesLivres;
        private readonly IReadOnlyList<MarbleState> marblesView;
        private readonly IReadOnlyList<ObstacleState> obstaclesView;
        private readonly IReadOnlyList<RegistroCapitaoConvidado> resultadosView;
        private readonly bool repeticao;
        private int passos, proximoTime;
        private double acumulador;

        public IReadOnlyList<MarbleState> Marbles { get { return marblesView; } }
        public IReadOnlyList<ObstacleState> Obstacles { get { return obstaclesView; } }
        public IReadOnlyList<RegistroCapitaoConvidado> Resultados { get { return resultadosView; } }
        public bool Concluido { get { return resultados.Count == marbles.Count; } }
        public bool Repeticao { get { return repeticao; } }
        public float TempoSimulado { get { return passos / 120f; } }
        public bool RetaFinal { get { return TempoSimulado >= InicioRetaFinal; } }

        public MotorSorteioCapitaes(IList<TeamData> times, IList<CapitaoConvidado> catalogo,
            IList<VinculoCapitaoConvidado> fixos, int seed, IList<RegistroCapitaoConvidado> replay = null)
        {
            if (times == null || times.Any(t => t == null)) throw new ArgumentException("Confira os times do sorteio.");
            RepositorioCapitaesConvidados.ValidarConfiguracao(times.Select(t => t.name), catalogo, fixos);
            var random = new System.Random(seed);
            var porTime = times.ToDictionary(t => t.name, StringComparer.Ordinal);
            var porCapitao = catalogo.ToDictionary(c => c.Identificador, StringComparer.Ordinal);
            var idsFixos = new HashSet<string>(StringComparer.Ordinal);
            foreach (VinculoCapitaoConvidado vinculo in fixos ?? new List<VinculoCapitaoConvidado>())
            {
                destinosFixos.Add(vinculo.capitao.Identificador, porTime[vinculo.time.name]);
                idsFixos.Add(vinculo.capitao.Identificador);
            }
            List<CapitaoConvidado> escolhidos;
            repeticao = replay != null;
            if (repeticao)
            {
                if (replay.Count != times.Count) throw new ArgumentException("O sorteio salvo não contém todos os participantes.");
                var usados = new HashSet<string>(StringComparer.Ordinal);
                foreach (RegistroCapitaoConvidado registro in replay)
                {
                    if (registro == null || string.IsNullOrEmpty(registro.timeId) || string.IsNullOrEmpty(registro.capitaoId) ||
                        !porTime.ContainsKey(registro.timeId) || !porCapitao.ContainsKey(registro.capitaoId) ||
                        !usados.Add(registro.timeId) || registrosSalvos.ContainsKey(registro.capitaoId))
                        throw new ArgumentException("O sorteio salvo contém participantes ausentes ou repetidos.");
                    TeamData fixo;
                    if (destinosFixos.TryGetValue(registro.capitaoId, out fixo) && fixo.name != registro.timeId)
                        throw new ArgumentException("O sorteio salvo difere das associações fixas.");
                    registrosSalvos.Add(registro.capitaoId, CopiarRegistro(registro));
                    destinosFixos[registro.capitaoId] = porTime[registro.timeId];
                }
                if (idsFixos.Any(id => !registrosSalvos.ContainsKey(id)))
                    throw new ArgumentException("O sorteio salvo não contém os capitães fixados.");
                escolhidos = registrosSalvos.Keys.OrderBy(id => id, StringComparer.Ordinal).Select(id => porCapitao[id]).ToList();
            }
            else
            {
                var disponiveis = catalogo.Where(c => !idsFixos.Contains(c.Identificador))
                    .OrderBy(c => c.Identificador, StringComparer.Ordinal).ToList();
                Embaralhar(disponiveis, random);
                escolhidos = idsFixos.OrderBy(id => id, StringComparer.Ordinal).Select(id => porCapitao[id]).ToList();
                escolhidos.AddRange(disponiveis.Take(times.Count - escolhidos.Count));
            }
            Embaralhar(escolhidos, random);
            timesLivres = times.Where(t => !destinosFixos.Values.Contains(t)).OrderBy(t => t.name, StringComparer.Ordinal).ToList();
            Embaralhar(timesLivres, new System.Random(seed ^ 0x6D2B79F5));
            int colunas = Math.Min(8, escolhidos.Count);
            for (int i = 0; i < escolhidos.Count; i++)
            {
                int coluna = i % colunas, linha = i / colunas;
                float x = (coluna + .5f) * Largura / colunas;
                marbles.Add(new MarbleState
                {
                    Capitao = escolhidos[i], Raio = 20f,
                    Posicao = new Vector2(x, 24f + linha * 48f),
                    Velocidade = new Vector2((float)(random.NextDouble() * 90d - 45d), 0f),
                    LiberarEm = .15f + (float)random.NextDouble() * .35f,
                    Fixa = idsFixos.Contains(escolhidos[i].Identificador)
                });
            }
            CriarObstaculos(new System.Random(seed ^ 0x13579BDF));
            marblesView = marbles.AsReadOnly(); obstaclesView = obstacles.AsReadOnly(); resultadosView = resultados.AsReadOnly();
        }

        public void Avancar(float dt)
        {
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt < 0f) throw new ArgumentOutOfRangeException("dt");
            acumulador += dt;
            const double passo = 1d / 120d;
            while (!Concluido && acumulador + .0000001d >= passo)
            {
                acumulador -= passo;
                Passo();
            }
        }

        private void CriarObstaculos(System.Random random)
        {
            for (int linha = 0; linha < 3; linha++)
                for (int coluna = 0; coluna < 7; coluna++)
                {
                    float x = 95f + coluna * 145f + (linha % 2 == 0 ? 0f : 42f);
                    float y = 230f + linha * 116f + (float)random.NextDouble() * 16f;
                    var p = new Vector2(x, y);
                    obstacles.Add(new ObstacleState { Tipo = TipoObstaculo.Pino, PontoA = p, PontoB = p, Raio = 14f, Ativo = true });
                }
            obstacles.Add(new ObstacleState { Tipo = TipoObstaculo.Rampa, PontoA = new Vector2(110f, 302f), PontoB = new Vector2(245f, 350f), Raio = 8f, Ativo = true });
            obstacles.Add(new ObstacleState { Tipo = TipoObstaculo.Rampa, PontoA = new Vector2(835f, 350f), PontoB = new Vector2(970f, 302f), Raio = 8f, Ativo = true });
            foreach (Vector2 centro in new[] { new Vector2(380f, 366f), new Vector2(700f, 492f) })
            {
                var o = new ObstacleState
                {
                    Tipo = TipoObstaculo.Giratorio, Raio = 9f, Ativo = true, Eixo = centro, MeiaExtensao = 77f,
                    Fase = (float)random.NextDouble() * 6.283185f, Rotacao = random.Next(2) == 0 ? -.9f : .9f
                };
                AtualizarGiratorio(o); obstacles.Add(o);
            }
        }

        private void AtualizarGiratorio(ObstacleState obstaculo)
        {
            float angulo = obstaculo.Fase + TempoSimulado * obstaculo.Rotacao;
            var eixo = new Vector2((float)Math.Cos(angulo), (float)Math.Sin(angulo)) * obstaculo.MeiaExtensao;
            obstaculo.PontoA = obstaculo.Eixo - eixo; obstaculo.PontoB = obstaculo.Eixo + eixo;
        }

        private void Passo()
        {
            passos++;
            foreach (ObstacleState o in obstacles)
            {
                o.Ativo = !RetaFinal;
                if (o.Tipo == TipoObstaculo.Giratorio) AtualizarGiratorio(o);
            }
            foreach (MarbleState m in marbles)
            {
                if (m.Concluida || TempoSimulado < m.LiberarEm) continue;
                Vector2 v = m.Velocidade;
                v.y += (RetaFinal ? 300f : 190f) * PassoFixo;
                v.x *= .9994f;
                if (RetaFinal) v.y = Math.Max(200f, v.y);
                m.Velocidade = Limitar(v, 430f);
                m.Posicao += m.Velocidade * PassoFixo;
                foreach (ObstacleState o in obstacles) if (o.Ativo) ColidirObstaculo(m, o);
                Conter(m);
            }
            for (int i = 0; i < marbles.Count; i++)
                for (int j = i + 1; j < marbles.Count; j++) ColidirMarbles(marbles[i], marbles[j]);
            foreach (MarbleState m in marbles)
            {
                if (m.Concluida) continue;
                Conter(m);
                if (m.Posicao.y >= Altura + m.Raio) Finalizar(m);
            }
        }

        private void ColidirObstaculo(MarbleState m, ObstacleState o)
        {
            Vector2 segmento = o.PontoB - o.PontoA;
            float comprimento = Dot(segmento, segmento);
            float t = comprimento < .0001f ? 0f : Math.Max(0f, Math.Min(1f, Dot(m.Posicao - o.PontoA, segmento) / comprimento));
            Vector2 ponto = o.PontoA + segmento * t;
            Vector2 delta = m.Posicao - ponto;
            float distancia = delta.magnitude, raio = m.Raio + o.Raio;
            if (distancia >= raio) return;
            Vector2 normal = distancia > .0001f ? delta / distancia : new Vector2(0f, -1f);
            m.Posicao = ponto + normal * (raio + .02f);
            Vector2 superficie = Vector2.zero;
            if (o.Tipo == TipoObstaculo.Giratorio)
            {
                Vector2 local = ponto - o.Centro;
                superficie = new Vector2(-local.y, local.x) * o.Rotacao;
            }
            Vector2 relativa = m.Velocidade - superficie;
            float entrada = Dot(relativa, normal);
            if (entrada < 0f) m.Velocidade -= normal * (entrada * 1.67f);
            // Deflect an exactly centered vertical impact so no marble balances on a peg.
            if (o.Tipo == TipoObstaculo.Pino && Math.Abs(normal.x) < .06f)
                m.Velocidade += new Vector2(m.Posicao.x < Largura * .5f ? -22f : 22f, 0f);
            m.Colisoes++;
            m.ColisoesObstaculos++;
        }

        private void ColidirMarbles(MarbleState a, MarbleState b)
        {
            if (a.Concluida || b.Concluida || TempoSimulado < a.LiberarEm || TempoSimulado < b.LiberarEm) return;
            Vector2 delta = b.Posicao - a.Posicao;
            float d = delta.magnitude, alvo = a.Raio + b.Raio;
            if (d >= alvo) return;
            Vector2 normal = d > .0001f ? delta / d : new Vector2(1f, 0f);
            Vector2 correcao = normal * ((alvo - d + .02f) * .5f);
            a.Posicao -= correcao; b.Posicao += correcao;
            float entrada = Dot(b.Velocidade - a.Velocidade, normal);
            if (entrada < 0f)
            {
                Vector2 impulso = normal * (entrada * .8f);
                a.Velocidade += impulso; b.Velocidade -= impulso;
            }
            a.Colisoes++; b.Colisoes++;
        }

        private static void Conter(MarbleState m)
        {
            Vector2 p = m.Posicao, v = m.Velocidade;
            if (p.x < m.Raio) { p.x = m.Raio; v.x = Math.Abs(v.x) * .75f; }
            if (p.x > Largura - m.Raio) { p.x = Largura - m.Raio; v.x = -Math.Abs(v.x) * .75f; }
            if (p.y < m.Raio) { p.y = m.Raio; v.y = Math.Abs(v.y) * .5f; }
            m.Posicao = p; m.Velocidade = Limitar(v, 430f);
        }

        private void Finalizar(MarbleState m)
        {
            TeamData time;
            if (!destinosFixos.TryGetValue(m.Capitao.Identificador, out time)) time = timesLivres[proximoTime++];
            m.TimeAtribuido = time; m.Concluida = true;
            m.Velocidade = Vector2.zero;
            RegistroCapitaoConvidado salvo;
            resultados.Add(repeticao && registrosSalvos.TryGetValue(m.Capitao.Identificador, out salvo) ? CopiarRegistro(salvo)
                : new RegistroCapitaoConvidado { timeId = time.name, capitaoId = m.Capitao.Identificador, nomeCapitao = m.Capitao.nomeCapitao.Trim(), overall = m.Capitao.overall });
        }

        private static RegistroCapitaoConvidado CopiarRegistro(RegistroCapitaoConvidado registro)
        { return new RegistroCapitaoConvidado { timeId = registro.timeId, capitaoId = registro.capitaoId, nomeCapitao = registro.nomeCapitao, overall = registro.overall }; }
        private static float Dot(Vector2 a, Vector2 b) { return a.x * b.x + a.y * b.y; }
        private static Vector2 Limitar(Vector2 v, float maximo) { return v.magnitude > maximo ? v.normalized * maximo : v; }
        private static void Embaralhar<T>(IList<T> itens, System.Random random)
        {
            for (int i = itens.Count - 1; i > 0; i--) { int j = random.Next(i + 1); T troca = itens[i]; itens[i] = itens[j]; itens[j] = troca; }
        }
    }
}
