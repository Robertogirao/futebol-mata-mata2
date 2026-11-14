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
            public int Destravamentos { get; internal set; }
            internal float LiberarEm;
            internal float MaiorY, TempoSemProgresso;
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
                    if (!PosicoesCapitao.Valida(registro.posicao) ||
                        registro.posicao != porCapitao[registro.capitaoId].posicao ||
                        registro.overall != porCapitao[registro.capitaoId].overall)
                        throw new ArgumentException("Overall ou posição de um capitão diferem do sorteio salvo. Restaure a ficha ou use Nova temporada.");
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
                    MaiorY = 24f + linha * 48f,
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
            // Keep clear passages throughout a rotor's entire sweep. The old
            // ramps and rotors overlapped neighbouring pins and trapped discs.
            for (int linha = 0; linha < 2; linha++)
                for (int coluna = 0; coluna < (linha == 0 ? 7 : 6); coluna++)
                {
                    float x = linha == 0 ? 90f + coluna * 150f : 140f + coluna * 160f;
                    float y = (linha == 0 ? 240f : 505f) + (float)random.NextDouble() * 8f;
                    var p = new Vector2(x, y);
                    obstacles.Add(new ObstacleState { Tipo = TipoObstaculo.Pino, PontoA = p, PontoB = p, Raio = 14f, Ativo = true });
                }
            obstacles.Add(new ObstacleState { Tipo = TipoObstaculo.Rampa, PontoA = new Vector2(95f, 340f), PontoB = new Vector2(225f, 385f), Raio = 8f, Ativo = true });
            obstacles.Add(new ObstacleState { Tipo = TipoObstaculo.Rampa, PontoA = new Vector2(855f, 385f), PontoB = new Vector2(985f, 340f), Raio = 8f, Ativo = true });
            foreach (Vector2 centro in new[] { new Vector2(390f, 375f), new Vector2(690f, 375f) })
            {
                var o = new ObstacleState
                {
                    Tipo = TipoObstaculo.Giratorio, Raio = 9f, Ativo = true, Eixo = centro, MeiaExtensao = 55f,
                    Fase = (float)random.NextDouble() * 6.283185f, Rotacao = random.Next(2) == 0 ? -.9f : .9f
                };
                AtualizarGiratorio(o); obstacles.Add(o);
            }
        }

        private void AtualizarGiratorio(ObstacleState obstaculo, float? tempo = null)
        {
            float angulo = obstaculo.Fase + (tempo ?? TempoSimulado) * obstaculo.Rotacao;
            var eixo = new Vector2((float)Math.Cos(angulo), (float)Math.Sin(angulo)) * obstaculo.MeiaExtensao;
            obstaculo.PontoA = obstaculo.Eixo - eixo; obstaculo.PontoB = obstaculo.Eixo + eixo;
        }

        private void Passo()
        {
            passos++;
            const int subpassos = 4;
            float dt = PassoFixo / subpassos;
            for (int subpasso = 0; subpasso < subpassos; subpasso++)
            {
                float tempo = TempoSimulado - PassoFixo + (subpasso + 1) * dt;
                foreach (ObstacleState o in obstacles)
                    if (o.Tipo == TipoObstaculo.Giratorio) AtualizarGiratorio(o, tempo);
                foreach (MarbleState m in marbles)
                {
                    if (m.Concluida || tempo < m.LiberarEm) continue;
                    ResolverObstaculos(m);
                    Vector2 v = m.Velocidade;
                    v.y += (RetaFinal ? 230f : 190f) * dt;
                    v.x *= .99985f;
                    m.Velocidade = Limitar(v, 430f);
                    MoverSemAtravessar(m, m.Posicao + m.Velocidade * dt);
                    Conter(m);
                }
                // Pairs can push a marble back into a pin. Reconcile both kinds
                // of contact repeatedly, giving solid obstacles final priority.
                for (int iteracao = 0; iteracao < 6; iteracao++)
                {
                    bool contato = false;
                    for (int i = 0; i < marbles.Count; i++)
                        for (int j = i + 1; j < marbles.Count; j++) contato |= ColidirMarbles(marbles[i], marbles[j]);
                    foreach (MarbleState m in marbles)
                        if (!m.Concluida && tempo >= m.LiberarEm) { contato |= ResolverObstaculos(m); Conter(m); }
                    if (!contato) break;
                }
            }
            foreach (MarbleState m in marbles)
            {
                if (m.Concluida) continue;
                if (m.Posicao.y >= Altura + m.Raio) Finalizar(m);
                else if (TempoSimulado >= m.LiberarEm)
                {
                    if (m.Posicao.y > m.MaiorY + 12f) { m.MaiorY = m.Posicao.y; m.TempoSemProgresso = 0f; }
                    else m.TempoSemProgresso += PassoFixo;
                    if (m.TempoSemProgresso >= 1.2f)
                    {
                        // Free a balanced/crowded marble by velocity, never by
                        // teleporting it or turning collision off.
                        float lado = m.Posicao.x < Largura * .5f ? 1f : -1f;
                        m.Velocidade = new Vector2(lado * 125f, Math.Max(45f, m.Velocidade.y));
                        m.TempoSemProgresso = 0f; m.Destravamentos++;
                    }
                }
            }
        }

        private bool ResolverObstaculos(MarbleState m)
        {
            bool contato = false;
            foreach (ObstacleState o in obstacles) if (o.Ativo) contato |= ColidirObstaculo(m, o);
            return contato;
        }

        private void MoverSemAtravessar(MarbleState m, Vector2 destino)
        {
            Vector2 origem = m.Posicao, delta = destino - origem;
            float primeiro = 1f;
            Vector2 normal = Vector2.zero;
            ObstacleState atingido = null;
            foreach (ObstacleState o in obstacles)
            {
                if (!o.Ativo) continue;
                float t; Vector2 n;
                if (IntersecaoCapsula(origem, delta, o.PontoA, o.PontoB, m.Raio + o.Raio + .02f, out t, out n) && t < primeiro)
                { primeiro = t; normal = n; atingido = o; }
            }
            m.Posicao = atingido == null ? destino : origem + delta * Math.Max(0f, primeiro - .00001f);
            if (atingido != null) ResponderContato(m, atingido, normal, m.Posicao - normal * m.Raio);
        }

        private static bool IntersecaoCirculo(Vector2 origem, Vector2 delta, Vector2 centro, float raio, out float t, out Vector2 normal)
        {
            t = 2f; normal = Vector2.zero;
            float a = Dot(delta, delta);
            if (a < .0000001f) return false;
            Vector2 relativo = origem - centro;
            float b = Dot(relativo, delta), c = Dot(relativo, relativo) - raio * raio;
            if (b >= 0f || c < -.01f) return false;
            float discriminante = b * b - a * c;
            if (discriminante < 0f) return false;
            float contato = (-b - (float)Math.Sqrt(discriminante)) / a;
            if (contato < -.0001f || contato > 1f) return false;
            t = Math.Max(0f, contato);
            normal = (origem + delta * t - centro).normalized;
            return true;
        }

        private static bool IntersecaoCapsula(Vector2 origem, Vector2 delta, Vector2 a, Vector2 b, float raio, out float t, out Vector2 normal)
        {
            t = 2f; normal = Vector2.zero;
            Vector2 segmento = b - a;
            float comprimento = segmento.magnitude;
            if (comprimento < .0001f) return IntersecaoCirculo(origem, delta, a, raio, out t, out normal);
            float candidato; Vector2 n;
            if (IntersecaoCirculo(origem, delta, a, raio, out candidato, out n)) { t = candidato; normal = n; }
            if (IntersecaoCirculo(origem, delta, b, raio, out candidato, out n) && candidato < t) { t = candidato; normal = n; }
            if (comprimento > .0001f)
            {
                Vector2 eixo = segmento / comprimento, perpendicular = new Vector2(-eixo.y, eixo.x);
                float distancia = Dot(origem - a, perpendicular), movimento = Dot(delta, perpendicular);
                if (Math.Abs(movimento) > .00001f)
                    for (int lado = -1; lado <= 1; lado += 2)
                    {
                        if (movimento * lado >= 0f) continue;
                        candidato = (lado * raio - distancia) / movimento;
                        if (candidato < -.0001f || candidato > 1f || candidato >= t) continue;
                        float posicao = Dot(origem + delta * candidato - a, eixo);
                        if (posicao < 0f || posicao > comprimento) continue;
                        t = Math.Max(0f, candidato); normal = perpendicular * lado;
                    }
            }
            return t <= 1f;
        }

        private bool ColidirObstaculo(MarbleState m, ObstacleState o)
        {
            Vector2 segmento = o.PontoB - o.PontoA;
            float comprimento = Dot(segmento, segmento);
            float t = comprimento < .0001f ? 0f : Math.Max(0f, Math.Min(1f, Dot(m.Posicao - o.PontoA, segmento) / comprimento));
            Vector2 ponto = o.PontoA + segmento * t;
            Vector2 delta = m.Posicao - ponto;
            float distancia = delta.magnitude, raio = m.Raio + o.Raio;
            if (distancia >= raio) return false;
            Vector2 normal = distancia > .0001f ? delta / distancia : new Vector2(0f, -1f);
            m.Posicao = ponto + normal * (raio + .02f);
            ResponderContato(m, o, normal, ponto);
            return true;
        }

        private static void ResponderContato(MarbleState m, ObstacleState o, Vector2 normal, Vector2 ponto)
        {
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

        private bool ColidirMarbles(MarbleState a, MarbleState b)
        {
            if (a.Concluida || b.Concluida || TempoSimulado < a.LiberarEm || TempoSimulado < b.LiberarEm) return false;
            Vector2 delta = b.Posicao - a.Posicao;
            float d = delta.magnitude, alvo = a.Raio + b.Raio;
            if (d >= alvo) return false;
            Vector2 normal = d > .0001f ? delta / d : new Vector2(1f, 0f);
            Vector2 correcao = normal * ((alvo - d + .02f) * .5f);
            MoverSemAtravessar(a, a.Posicao - correcao);
            MoverSemAtravessar(b, b.Posicao + correcao);
            float entrada = Dot(b.Velocidade - a.Velocidade, normal);
            if (entrada < 0f)
            {
                Vector2 impulso = normal * (entrada * .8f);
                a.Velocidade += impulso; b.Velocidade -= impulso;
            }
            ResolverObstaculos(a); ResolverObstaculos(b);
            a.Colisoes++; b.Colisoes++;
            return true;
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
                : new RegistroCapitaoConvidado { timeId = time.name, capitaoId = m.Capitao.Identificador,
                    nomeCapitao = m.Capitao.nomeCapitao.Trim(), overall = m.Capitao.overall, posicao = m.Capitao.posicao });
        }

        private static RegistroCapitaoConvidado CopiarRegistro(RegistroCapitaoConvidado registro)
        { return new RegistroCapitaoConvidado { timeId = registro.timeId, capitaoId = registro.capitaoId,
            nomeCapitao = registro.nomeCapitao, overall = registro.overall, posicao = registro.posicao }; }
        private static float Dot(Vector2 a, Vector2 b) { return a.x * b.x + a.y * b.y; }
        private static Vector2 Limitar(Vector2 v, float maximo) { return v.magnitude > maximo ? v.normalized * maximo : v; }
        private static void Embaralhar<T>(IList<T> itens, System.Random random)
        {
            for (int i = itens.Count - 1; i > 0; i--) { int j = random.Next(i + 1); T troca = itens[i]; itens[i] = itens[j]; itens[j] = troca; }
        }
    }
}
