using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FutebolDeDiscos
{
    /// <summary>Broadcast view of the marble draw. The simulation owns every assignment.</summary>
    [DisallowMultipleComponent]
    public sealed class SorteioCapitaesUI : MonoBehaviour
    {
        private const float PistaX = 74f, PistaY = 244f;
        public bool Ativo { get { return canvas != null && canvas.gameObject.activeInHierarchy; } }
        public bool Concluido { get; private set; }
        public int QuantidadeRevelada { get; private set; }

        private sealed class MarbleView
        {
            public MotorSorteioCapitaes.MarbleState estado;
            public RectTransform area;
            public TextMeshProUGUI nome;
            public bool revelada, naChegada;
        }
        private sealed class TeamView
        {
            public TeamData equipe;
            public RectTransform area;
            public Image fundo, retrato, acento;
            public TextMeshProUGUI nomeCapitao;
            public Vector2 destino;
            public float tamanhoFoto, brilho;
        }
        private sealed class ObstacleView
        {
            public MotorSorteioCapitaes.ObstacleState estado;
            public RectTransform area;
            public Image imagem;
        }
        private sealed class Flight
        {
            public MarbleView marble;
            public TeamView equipe;
            public Vector2 inicio;
            public float tempo, duracao, distanciaHorizontal, distanciaVertical, distanciaFinal;
        }

        private Canvas canvas;
        private RectTransform raiz, pista;
        private TMP_FontAsset fonteTitulo, fonteTexto;
        private TextMeshProUGUI contador, fase, anuncio, rodape;
        private Image progresso, luzGate;
        private Color acento, painel;
        private MotorSorteioCapitaes motor;
        private int versao;
        private float tempoVisual;
        private Texture2D texturaCirculo;
        private Sprite circulo;
        private readonly List<MarbleView> marbles = new List<MarbleView>();
        private readonly List<ObstacleView> obstaculos = new List<ObstacleView>();
        private readonly Dictionary<string, MarbleView> marblePorId = new Dictionary<string, MarbleView>(StringComparer.Ordinal);
        private readonly Dictionary<string, TeamView> equipePorId = new Dictionary<string, TeamView>(StringComparer.Ordinal);
        private readonly List<Flight> voos = new List<Flight>();
        private AudioClip musicaSorteio;
        private float volumeMusicaSorteio = .35f, fadeInMusicaSorteio = 1.5f, fadeOutMusicaSorteio = 1.5f;
        private AudioTransicao audioSorteio;

        public void ConfigurarMusica(AudioClip clip, float volume, float fadeIn, float fadeOut)
        {
            musicaSorteio = clip;
            volumeMusicaSorteio = Seguro(volume, 0f, 1f, .35f);
            fadeInMusicaSorteio = Seguro(fadeIn, 0f, 10f, 1.5f);
            fadeOutMusicaSorteio = Seguro(fadeOut, 0f, 10f, 1.5f);
        }

        private void TocarMusica()
        {
            if (musicaSorteio == null || volumeMusicaSorteio <= 0f) return;
            audioSorteio = gameObject.AddComponent<AudioTransicao>();
            audioSorteio.Tocar(musicaSorteio, volumeMusicaSorteio, true, fadeInMusicaSorteio);
        }

        private void PararMusica()
        {
            if (audioSorteio == null) return;
            audioSorteio.Parar();
            Destroy(audioSorteio);
            audioSorteio = null;
        }

        public IEnumerator Exibir(MotorSorteioCapitaes simulacao, IList<TeamData> times,
            IList<CapitaoConvidado> catalogo, TemaCompeticao tema, float tempoApresentacao,
            float velocidadeSimulacao, float intervaloRevelacoes, bool replay = false)
        {
            Cancelar();
            if (simulacao == null) throw new ArgumentNullException("simulacao");
            if (times == null || times.Count < 2 || times.Count > 32 || simulacao.Marbles.Count != times.Count)
                throw new ArgumentException("Confira os participantes da apresentação do sorteio.");
            motor = simulacao;
            int atual = versao;
            Construir(times, tema, replay);
            AtualizarVisual(0f);
            TocarMusica();
            float duracao = Seguro(tempoApresentacao, 0f, 60f, 2f);
            float decorrido = 0f;
            while (atual == versao && decorrido < duracao)
            {
                float dt = PassoVideo();
                decorrido += dt;
                AtualizarVisual(dt);
                yield return null;
            }
            if (atual != versao) yield break;
            fase.text = replay ? "REPLAYING SAVED ASSIGNMENTS" : "LET THE MARBLES DECIDE";
            rodape.text = replay ? "SAVED DRAW  /  THE ORIGINAL PAIRINGS ARE PRESERVED" : "ARRIVAL GATE  /  ONE CAPTAIN FOR EVERY CLUB";
            float esperaRevelacao = 0f;
            float intervalo = Seguro(intervaloRevelacoes, .05f, 5f, .35f);
            float velocidade = Seguro(velocidadeSimulacao, .25f, 4f, 1f);
            while (atual == versao && (!motor.Concluido || QuantidadeRevelada < times.Count || voos.Count != 0))
            {
                float dt = PassoVideo();
                motor.Avancar(dt * velocidade);
                esperaRevelacao -= dt;
                if (QuantidadeRevelada < motor.Resultados.Count && esperaRevelacao <= 0f)
                {
                    Revelar(motor.Resultados[QuantidadeRevelada]);
                    QuantidadeRevelada++;
                    esperaRevelacao = intervalo;
                }
                AtualizarVisual(dt);
                yield return null;
            }
            if (atual != versao) yield break;
            Concluido = true;
            contador.text = times.Count + " / " + times.Count;
            fase.text = "ALL CLUBS HAVE A CAPTAIN";
            rodape.text = "DRAFT COMPLETE  /  THE CHAMPIONSHIP STARTS NEXT";
            anuncio.text = "THE LINEUP IS SET";
            progresso.fillAmount = 1f;
        }

        /// <summary>The caller saves the complete result before holding this final board.</summary>
        public IEnumerator ExibirResultadoFinal(float segundos)
        {
            if (!Ativo || !Concluido) yield break;
            int atual = versao;
            float tempo = 0f, duracao = Seguro(segundos, 0f, 60f, 3f);
            bool saidaIniciada = false;
            float inicioSaida = Mathf.Max(0f, duracao - fadeOutMusicaSorteio);
            while (atual == versao && tempo < duracao)
            {
                if (!saidaIniciada && tempo >= inicioSaida)
                {
                    if (audioSorteio != null) audioSorteio.Encerrar(fadeOutMusicaSorteio);
                    saidaIniciada = true;
                }
                float dt = PassoVideo();
                tempo += dt;
                AtualizarVisual(dt);
                yield return null;
            }
            if (atual != versao) yield break;
            if (!saidaIniciada && audioSorteio != null) audioSorteio.Encerrar(fadeOutMusicaSorteio);
            // O áudio usa tempo real. Captura de vídeo ou um quadro final curto
            // não podem cortar o fade nem deixar a música vazar para a abertura.
            while (atual == versao && audioSorteio != null && audioSorteio.EstaTocando)
            {
                AtualizarVisual(PassoVideo());
                yield return null;
            }
            if (atual == versao) Ocultar();
        }

        public void Ocultar()
        {
            PararMusica();
            if (canvas != null) canvas.gameObject.SetActive(false);
        }

        public void Cancelar()
        {
            versao++;
            Concluido = false;
            QuantidadeRevelada = 0;
            StopAllCoroutines();
            PararMusica();
            if (canvas != null)
            {
                canvas.gameObject.SetActive(false);
                Destroy(canvas.gameObject);
            }
            canvas = null; raiz = pista = null; motor = null;
            marbles.Clear(); obstaculos.Clear(); marblePorId.Clear(); equipePorId.Clear(); voos.Clear();
        }

        private void Construir(IList<TeamData> times, TemaCompeticao tema, bool replay)
        {
            fonteTitulo = tema != null && tema.fonteTitulo != null ? tema.fonteTitulo : TMP_Settings.defaultFontAsset;
            fonteTexto = tema != null && tema.fonteTexto != null ? tema.fonteTexto : fonteTitulo;
            if (fonteTitulo == null || fonteTexto == null)
                throw new InvalidOperationException("Atribua uma fonte TMP ao tema do sorteio.");
            acento = tema != null ? tema.corDestaque : new Color(.12f, .95f, 1f);
            acento.a = 1f;
            if (acento.maxColorComponent < .25f) acento = new Color(.12f, .95f, 1f);
            painel = tema != null ? tema.corPainel : new Color(.025f, .055f, .11f);
            painel.a = 1f;
            tempoVisual = 0f;
            var go = new GameObject("CaptainDraft.Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            go.layer = 5;
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 2600;
            raiz = (RectTransform)go.transform;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            var tela = Caixa(raiz, "Opaque Draft Backdrop", 0f, 0f, 1920f, 1080f, new Color(.008f, .016f, .04f, 1f));
            if (tema != null && tema.fundoAbertura != null)
            {
                Image fundo = Caixa(tela, "Theme Backdrop", 0f, 0f, 1920f, 1080f, new Color(1f, 1f, 1f, .22f)).GetComponent<Image>();
                fundo.sprite = tema.fundoAbertura;
            }
            for (int i = 1; i < 20; i++) Caixa(raiz, "Draft Grid Vertical", i * 96f, 0f, 1f, 1080f, Alpha(acento, .045f));
            for (int i = 1; i < 12; i++) Caixa(raiz, "Draft Grid Horizontal", 0f, i * 90f, 1920f, 1f, Alpha(acento, .045f));
            Caixa(raiz, "Header Accent", 40f, 28f, 1840f, 3f, acento);
            Texto(raiz, "Draft Title", replay ? "SAVED DRAFT" : "CAPTAIN DRAFT", 190f, 48f, 1540f, 60f, 51f, fonteTitulo, Color.white, TextAlignmentOptions.Center);
            fase = Texto(raiz, "Draft Phase", "MARBLES READY  /  CLUBS WAITING", 250f, 111f, 1420f, 30f, 22f, fonteTexto, acento, TextAlignmentOptions.Center);
            if (tema != null)
            {
                Retrato(raiz, "Competition Logo", 54f, 47f, 88f, tema.logoCompeticao, false);
                Retrato(raiz, "Channel Logo", 1778f, 47f, 88f, tema.logoCanal, false);
            }
            Caixa(raiz, "Marble Arena Frame", 40f, 168f, 1140f, 780f, Alpha(acento, .5f));
            Caixa(raiz, "Marble Arena Plate", 43f, 171f, 1134f, 774f, Color.Lerp(painel, Color.black, .32f));
            Texto(raiz, "Arena Label", "THE MARBLE CHAMBER", 74f, 188f, 730f, 32f, 24f, fonteTitulo, Color.white, TextAlignmentOptions.Left);
            Texto(raiz, "Marble Count", times.Count + " CAPTAINS", 850f, 188f, 304f, 32f, 21f, fonteTexto, acento, TextAlignmentOptions.Right);
            pista = Caixa(raiz, "Obstacle Stage", PistaX, PistaY, MotorSorteioCapitaes.Largura, MotorSorteioCapitaes.Altura, new Color(.012f, .031f, .063f));
            for (int i = 1; i < 9; i++) Caixa(pista, "Arena Lane", i * 120f, 0f, 1f, MotorSorteioCapitaes.Altura, Alpha(acento, .07f));
            Caixa(pista, "Starting Line", 0f, 185f, 1080f, 1f, Alpha(acento, .21f));
            Caixa(raiz, "Arrival Gate Housing", PistaX, PistaY + 620f, 1080f, 58f, new Color(.015f, .05f, .075f));
            luzGate = Caixa(raiz, "Arrival Gate Glow", PistaX, PistaY + 620f, 1080f, 4f, acento).GetComponent<Image>();
            for (int i = 0; i < 36; i++)
                if (i % 2 == 0) Caixa(raiz, "Arrival Checker", PistaX + i * 30f, PistaY + 626f, 30f, 9f, Alpha(Color.white, .55f));
            rodape = Texto(raiz, "Arena Footer", "ALL CAPTAINS ENTER THE DRAW TOGETHER", PistaX + 20f, PistaY + 679f, 1040f, 20f, 16f, fonteTexto, acento, TextAlignmentOptions.Center);
            CriarEquipes(times);
            foreach (MotorSorteioCapitaes.ObstacleState obstaculo in motor.Obstacles)
            {
                bool pino = obstaculo.Tipo == MotorSorteioCapitaes.TipoObstaculo.Pino;
                RectTransform area = Caixa(pista, "Draw Obstacle " + obstaculo.Tipo, 0f, 0f, 1f, 1f, pino ? acento : new Color(1f, .67f, .18f));
                area.pivot = new Vector2(.5f, .5f);
                Image imagem = area.GetComponent<Image>();
                if (pino) imagem.sprite = ObterCirculo();
                obstaculos.Add(new ObstacleView { estado = obstaculo, area = area, imagem = imagem });
            }
            foreach (MotorSorteioCapitaes.MarbleState estado in motor.Marbles)
            {
                float diametro = estado.Raio * 2f;
                RectTransform area = Caixa(pista, "Captain Marble " + estado.Capitao.Identificador, 0f, 0f, diametro, diametro, CorMarble(marbles.Count));
                area.pivot = new Vector2(.5f, .5f);
                area.GetComponent<Image>().sprite = ObterCirculo();
                Image sombra = Caixa(area, "Inner Marble", diametro * .08f, diametro * .08f, diametro * .84f, diametro * .84f, new Color(.015f, .025f, .055f)).GetComponent<Image>();
                sombra.sprite = ObterCirculo();
                if (estado.Capitao.foto != null)
                    Retrato(area, "Captain Photo", diametro * .1f, diametro * .1f, diametro * .8f, estado.Capitao.foto, true);
                else Texto(area, "Captain Initial", Inicial(estado.Capitao.nomeCapitao), diametro * .12f, diametro * .06f, diametro * .76f, diametro * .8f, diametro * .46f, fonteTitulo, Color.white, TextAlignmentOptions.Center);
                Image reflexo = Caixa(area, "Marble Shine", diametro * .2f, diametro * .12f, diametro * .23f, diametro * .1f, Alpha(Color.white, .52f)).GetComponent<Image>();
                reflexo.sprite = ObterCirculo();
                TextMeshProUGUI nome = Texto(area, "Marble Name", (estado.Fixa ? "LOCKED · " : "") + estado.Capitao.nomeCapitao,
                    -47f + diametro * .5f, diametro + 2f, 94f, 20f, 12f, fonteTexto, Color.white, TextAlignmentOptions.Center);
                var view = new MarbleView { estado = estado, area = area, nome = nome };
                marbles.Add(view); marblePorId.Add(estado.Capitao.Identificador, view);
            }
            Caixa(raiz, "Announcement Plate", 40f, 976f, 1840f, 70f, painel);
            Caixa(raiz, "Announcement Accent", 40f, 976f, 5f, 70f, acento);
            anuncio = Texto(raiz, "Latest Assignment", replay ? "YOUR SAVED CAPTAINS RETURN" : "A NEW CLUB. A NEW CAPTAIN.", 74f, 988f, 1770f, 46f, 29f, fonteTitulo, Color.white, TextAlignmentOptions.Center);
            Canvas.ForceUpdateCanvases();
        }

        private void CriarEquipes(IList<TeamData> times)
        {
            Caixa(raiz, "Clubs Frame", 1210f, 168f, 670f, 780f, Alpha(acento, .35f));
            Caixa(raiz, "Clubs Plate", 1213f, 171f, 664f, 774f, painel);
            Texto(raiz, "Clubs Label", "THE CLUBS", 1230f, 186f, 380f, 32f, 25f, fonteTitulo, Color.white, TextAlignmentOptions.Left);
            contador = Texto(raiz, "Assignment Counter", "0 / " + times.Count, 1640f, 186f, 216f, 32f, 24f, fonteTitulo, acento, TextAlignmentOptions.Right);
            progresso = Caixa(raiz, "Draft Progress", 1230f, 227f, 630f, 3f, acento).GetComponent<Image>();
            progresso.type = Image.Type.Filled; progresso.fillMethod = Image.FillMethod.Horizontal; progresso.fillAmount = 0f;
            int colunas = times.Count > 12 ? 2 : 1;
            int linhas = (times.Count + colunas - 1) / colunas;
            float intervaloY = Mathf.Min(92f, 694f / linhas);
            float largura = colunas == 1 ? 630f : 309f;
            float altura = intervaloY - 6f;
            float inicioY = 244f + (694f - intervaloY * linhas) * .5f;
            for (int i = 0; i < times.Count; i++)
            {
                int coluna = i / linhas, linha = i % linhas;
                float x = 1230f + coluna * 321f, y = inicioY + linha * intervaloY;
                float foto = Mathf.Min(54f, altura - 10f);
                TeamData time = times[i];
                RectTransform card = Caixa(raiz, "Draw Team " + time.name, x, y, largura, altura, new Color(.018f, .038f, .071f));
                Image barra = Caixa(card, "Team Accent", 0f, 0f, 3f, altura, Alpha(acento, .25f)).GetComponent<Image>();
                Retrato(card, "Team Crest", 8f, (altura - foto) * .5f, foto, time.escudoOuBandeira, false);
                Image retrato = Retrato(card, "Captain Portrait", largura - foto - 8f, (altura - foto) * .5f, foto, null, true);
                float textoX = foto + 16f, textoLargura = largura - foto * 2f - 30f;
                bool compacto = altura < 60f;
                float textoFonte = compacto ? 14f : colunas == 1 ? 23f : 19f;
                // Dense boards still need room for actual TMP line metrics,
                // rather than two rectangles shorter than the font itself.
                float alturaLinha = compacto ? (altura - 4f) * .5f : altura * .35f;
                var nomeTime = Texto(card, "Team Name", Nome(time.nomeEquipe), textoX, compacto ? 1f : altura * .15f,
                    textoLargura, alturaLinha, textoFonte, fonteTitulo, Color.white, TextAlignmentOptions.Left);
                var nomeCapitao = Texto(card, "Captain Name", "AWAITING CAPTAIN", textoX, compacto ? altura * .5f : altura * .54f,
                    textoLargura, compacto ? alturaLinha : altura * .3f, textoFonte * .83f, fonteTexto, Alpha(Color.white, .46f), TextAlignmentOptions.Left);
                if (compacto) { nomeTime.fontSizeMin = 9f; nomeCapitao.fontSizeMin = 9f; }
                equipePorId.Add(time.name, new TeamView
                {
                    equipe = time, area = card, fundo = card.GetComponent<Image>(), retrato = retrato, acento = barra,
                    nomeCapitao = nomeCapitao, tamanhoFoto = foto,
                    destino = new Vector2(x + largura - foto * .5f - 8f, y + altura * .5f)
                });
            }
        }

        private void Revelar(RegistroCapitaoConvidado registro)
        {
            MarbleView marble = marblePorId[registro.capitaoId];
            TeamView equipe = equipePorId[registro.timeId];
            marble.revelada = true;
            if (!marble.naChegada)
            {
                // A just-finished marble may not have had a rendered queue frame.
                // Its assignment animation starts below the finish, never inside
                // the obstacle chamber at the previous physics frame's position.
                marble.area.anchoredPosition = new Vector2(marble.estado.Posicao.x, -650f);
                marble.area.localScale = Vector3.one * .55f;
                marble.naChegada = true;
            }
            Vector2 inicio = new Vector2(PistaX + marble.area.anchoredPosition.x, PistaY - marble.area.anchoredPosition.y);
            marble.area.SetParent(raiz, false);
            marble.area.anchorMin = marble.area.anchorMax = new Vector2(0f, 1f);
            marble.area.anchoredPosition = new Vector2(inicio.x, -inicio.y);
            marble.nome.enabled = false;
            marble.area.SetAsLastSibling();
            equipe.nomeCapitao.text = marble.estado.Capitao.nomeCapitao;
            equipe.nomeCapitao.color = acento;
            equipe.brilho = 1f;
            float horizontal = Mathf.Abs(1195f - inicio.x);
            float vertical = Mathf.Abs(equipe.destino.y - inicio.y);
            float final = Mathf.Abs(equipe.destino.x - 1195f);
            voos.Add(new Flight { marble = marble, equipe = equipe, inicio = inicio,
                distanciaHorizontal = horizontal, distanciaVertical = vertical, distanciaFinal = final,
                duracao = Mathf.Clamp((horizontal + vertical + final) / 1600f, .65f, 1.5f) });
            anuncio.text = Nome(registro.nomeCapitao) + "  ·  OVR " + registro.overall + "  ·  " +
                PosicoesCapitao.Nome(registro.posicao).ToUpperInvariant() + "  →  " +
                Nome(equipe.equipe.nomeEquipe) + (marble.estado.Fixa ? "  /  LOCKED" : "");
            contador.text = (QuantidadeRevelada + 1) + " / " + equipePorId.Count;
            progresso.fillAmount = (QuantidadeRevelada + 1f) / equipePorId.Count;
        }

        private void AtualizarVisual(float dt)
        {
            tempoVisual += dt;
            foreach (ObstacleView view in obstaculos)
            {
                var estado = view.estado;
                view.area.gameObject.SetActive(estado.Ativo);
                if (estado.Tipo == MotorSorteioCapitaes.TipoObstaculo.Pino)
                {
                    view.area.anchoredPosition = new Vector2(estado.Centro.x, -estado.Centro.y);
                    view.area.sizeDelta = Vector2.one * estado.Raio * 2f;
                }
                else
                {
                    Vector2 delta = estado.PontoB - estado.PontoA;
                    Vector2 centro = (estado.PontoA + estado.PontoB) * .5f;
                    view.area.anchoredPosition = new Vector2(centro.x, -centro.y);
                    view.area.sizeDelta = new Vector2(delta.magnitude, estado.Raio * 2f);
                    view.area.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                }
            }
            int espera = 0;
            foreach (MarbleView view in marbles)
            {
                if (view.revelada) continue;
                Vector2 p = view.estado.Posicao;
                if (view.estado.Concluida)
                {
                    // The arrival queue is outside the collision chamber. Keeping
                    // completed portraits among live pins made them appear to pass
                    // through obstacles while awaiting their assignment animation.
                    p = new Vector2(18f + espera * 33.5f, 650f);
                    view.naChegada = true;
                    view.area.localScale = Vector3.one * .55f;
                    espera++;
                }
                view.area.anchoredPosition = new Vector2(p.x, -p.y);
                bool nomeLivre = !view.estado.Concluida;
                // In a crowded starting rack the portraits remain clear; floating
                // names appear only once nearby marbles leave enough room.
                foreach (MarbleView outro in marbles)
                {
                    if (outro == view || outro.revelada || outro.estado.Concluida) continue;
                    Vector2 distancia = outro.estado.Posicao - p;
                    if (Mathf.Abs(distancia.x) < 102f && Mathf.Abs(distancia.y) < 64f) { nomeLivre = false; break; }
                }
                view.nome.enabled = nomeLivre;
            }
            for (int i = voos.Count - 1; i >= 0; i--)
            {
                Flight voo = voos[i];
                voo.tempo += dt;
                float t = Mathf.Clamp01(voo.tempo / voo.duracao);
                float suave = t * t * (3f - 2f * t);
                float percurso = suave * (voo.distanciaHorizontal + voo.distanciaVertical + voo.distanciaFinal);
                Vector2 canto = new Vector2(1195f, voo.inicio.y);
                Vector2 coluna = new Vector2(1195f, voo.equipe.destino.y);
                Vector2 p;
                if (percurso < voo.distanciaHorizontal)
                    p = Vector2.Lerp(voo.inicio, canto, percurso / Mathf.Max(.001f, voo.distanciaHorizontal));
                else if (percurso < voo.distanciaHorizontal + voo.distanciaVertical)
                    p = Vector2.Lerp(canto, coluna, (percurso - voo.distanciaHorizontal) / Mathf.Max(.001f, voo.distanciaVertical));
                else
                    p = Vector2.Lerp(coluna, voo.equipe.destino, (percurso - voo.distanciaHorizontal - voo.distanciaVertical) / Mathf.Max(.001f, voo.distanciaFinal));
                voo.marble.area.anchoredPosition = new Vector2(p.x, -p.y);
                float escala = Mathf.Lerp(.55f, voo.equipe.tamanhoFoto / (voo.marble.estado.Raio * 2f), suave);
                voo.marble.area.localScale = Vector3.one * escala;
                if (t >= 1f)
                {
                    voo.equipe.retrato.sprite = voo.marble.estado.Capitao.foto != null ? voo.marble.estado.Capitao.foto : voo.equipe.equipe.escudoOuBandeira;
                    voo.equipe.retrato.enabled = voo.equipe.retrato.sprite != null;
                    voo.marble.area.gameObject.SetActive(false);
                    voos.RemoveAt(i);
                }
            }
            foreach (TeamView equipe in equipePorId.Values)
            {
                equipe.brilho = Mathf.Max(0f, equipe.brilho - dt * .65f);
                equipe.fundo.color = Color.Lerp(new Color(.018f, .038f, .071f), Color.Lerp(painel, acento, .32f), equipe.brilho);
                equipe.acento.color = Alpha(acento, .25f + equipe.brilho * .75f);
            }
            if (luzGate != null) luzGate.color = Alpha(acento, .5f + .25f * (Mathf.Sin(tempoVisual * 2f) + 1f));
        }

        private Image Retrato(Transform pai, string nome, float x, float y, float tamanho, Sprite imagem, bool redondo)
        {
            RectTransform area = Caixa(pai, nome + " Frame", x, y, tamanho, tamanho, new Color(.025f, .06f, .1f));
            if (redondo)
            {
                area.GetComponent<Image>().sprite = ObterCirculo();
                area.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            }
            Image foto = Caixa(area, nome, 0f, 0f, tamanho, tamanho, Color.white).GetComponent<Image>();
            foto.sprite = imagem;
            foto.preserveAspect = true;
            foto.enabled = imagem != null;
            return foto;
        }

        private static RectTransform Caixa(Transform pai, string nome, float x, float y, float largura, float altura, Color cor)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, false);
            var area = (RectTransform)go.transform;
            area.anchorMin = area.anchorMax = new Vector2(0f, 1f);
            area.pivot = new Vector2(0f, 1f);
            area.anchoredPosition = new Vector2(x, -y);
            area.sizeDelta = new Vector2(largura, altura);
            Image imagem = go.AddComponent<Image>();
            imagem.color = cor;
            imagem.raycastTarget = false;
            return area;
        }

        private static TextMeshProUGUI Texto(Transform pai, string nome, string valor, float x, float y, float largura, float altura,
            float tamanho, TMP_FontAsset fonte, Color cor, TextAlignmentOptions alinhamento)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, false);
            var area = (RectTransform)go.transform;
            area.anchorMin = area.anchorMax = new Vector2(0f, 1f);
            area.pivot = new Vector2(0f, 1f);
            area.anchoredPosition = new Vector2(x, -y);
            area.sizeDelta = new Vector2(largura, altura);
            var texto = go.AddComponent<TextMeshProUGUI>();
            texto.font = fonte;
            texto.text = valor;
            texto.fontSize = tamanho;
            texto.enableAutoSizing = true;
            texto.fontSizeMax = tamanho;
            texto.fontSizeMin = Mathf.Min(tamanho, 11f);
            texto.fontStyle = FontStyles.Bold;
            texto.alignment = alinhamento;
            texto.color = cor;
            texto.richText = false;
            texto.textWrappingMode = TextWrappingModes.NoWrap;
            texto.overflowMode = TextOverflowModes.Ellipsis;
            texto.raycastTarget = false;
            return texto;
        }

        private Sprite ObterCirculo()
        {
            if (circulo != null) return circulo;
            const int lado = 96;
            var pixels = new Color32[lado * lado];
            for (int y = 0; y < lado; y++) for (int x = 0; x < lado; x++)
            {
                float dx = (x + .5f) / lado - .5f, dy = (y + .5f) / lado - .5f;
                pixels[y * lado + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((.5f - Mathf.Sqrt(dx * dx + dy * dy)) * lado * 2f));
            }
            texturaCirculo = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            texturaCirculo.name = "CaptainDraft.Circle";
            texturaCirculo.hideFlags = HideFlags.DontSave;
            texturaCirculo.wrapMode = TextureWrapMode.Clamp;
            texturaCirculo.filterMode = FilterMode.Bilinear;
            texturaCirculo.SetPixels32(pixels);
            texturaCirculo.Apply(false, true);
            circulo = Sprite.Create(texturaCirculo, new Rect(0f, 0f, lado, lado), new Vector2(.5f, .5f));
            circulo.hideFlags = HideFlags.DontSave;
            return circulo;
        }

        private static Color CorMarble(int indice)
        {
            switch (indice % 6)
            {
                case 0: return new Color(.18f, .95f, 1f);
                case 1: return new Color(1f, .7f, .18f);
                case 2: return new Color(.8f, .4f, 1f);
                case 3: return new Color(.4f, 1f, .6f);
                case 4: return new Color(1f, .35f, .5f);
                default: return new Color(.5f, .65f, 1f);
            }
        }
        private static string Inicial(string nome) { return string.IsNullOrWhiteSpace(nome) ? "?" : nome.Trim().Substring(0, 1).ToUpperInvariant(); }
        private static string Nome(string nome) { return string.IsNullOrWhiteSpace(nome) ? "" : nome.Replace("\r", " ").Replace("\n", " ").Trim().ToUpperInvariant(); }
        private static Color Alpha(Color cor, float alpha) { cor.a = alpha; return cor; }
        private static float Seguro(float valor, float min, float max, float padrao)
        { return float.IsNaN(valor) || float.IsInfinity(valor) ? padrao : Mathf.Clamp(valor, min, max); }
        private static float PassoVideo()
        {
            float dt = Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;
            return float.IsNaN(dt) || float.IsInfinity(dt) ? 0f : Mathf.Max(0f, dt);
        }
        private void OnDisable() { Cancelar(); }
        private void OnDestroy()
        {
            Cancelar();
            if (circulo != null) Destroy(circulo);
            if (texturaCirculo != null) Destroy(texturaCirculo);
            circulo = null; texturaCirculo = null;
        }
    }
}
