using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FutebolDeDiscos
{
    /// <summary>Original broadcast layout. Existing score, clock and goal queue remain authoritative.</summary>
    [DisallowMultipleComponent]
    public sealed class HudFutebolDiscos : MonoBehaviour
    {
        private RectTransform raiz;
        private TMP_FontAsset fonteTitulo, fonteTexto;
        private TextMeshProUGUI nomeA, nomeB, estado;
        private Image faixaA, faixaB, escudoA, escudoB;
        private UniversalUI interfacePartida;
        private GoalBannerUI bannerPartida;
        private Color destaque;

        public void Construir(ProjetoFutebolDiscos config, UniversalUI ui, GoalBannerUI banner)
        {
            if (ui == null || banner == null)
                throw new System.InvalidOperationException("Atribua UniversalUI e GoalBannerUI ao HUD de discos.");
            Limpar();
            interfacePartida = ui;
            bannerPartida = banner;
            TemaCompeticao tema = config != null ? config.tema : null;
            TMP_FontAsset padrao = TMP_Settings.defaultFontAsset;
            if (padrao == null) padrao = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            fonteTitulo = tema != null && tema.fonteTitulo != null ? tema.fonteTitulo : tema != null && tema.fonteTexto != null ? tema.fonteTexto : padrao;
            fonteTexto = tema != null && tema.fonteTexto != null ? tema.fonteTexto : fonteTitulo;
            if (fonteTitulo == null || fonteTexto == null)
                throw new System.InvalidOperationException("Atribua uma fonte TMP ao tema da competição.");
            destaque = tema != null ? Visivel(tema.corDestaque) : new Color(0.18f, 0.94f, 0.88f);

            // UniversalUI resolves this child canvas before creating its representative cards.
            GameObject objetoCanvas = new GameObject("DiscosBroadcast", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            objetoCanvas.transform.SetParent(ui.transform, false);
            objetoCanvas.layer = 5;
            raiz = (RectTransform)objetoCanvas.transform;
            Canvas canvas = objetoCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            // Standings (300), match opening (2000) and bracket (2100)
            // cover the live scoreboard during outside-field presentations.
            canvas.sortingOrder = 100;
            CanvasScaler scaler = objetoCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Color escuro = new Color(0.018f, 0.07f, 0.085f, 0.97f);
            Color painel = new Color(0.025f, 0.105f, 0.12f, 0.97f);
            RectTransform cabecalho = Caixa(raiz, "Scoreboard", 24f, 18f, 1872f, 128f, escuro);
            Caixa(cabecalho, "Top Accent", 0f, 0f, 1872f, 4f, destaque);
            Caixa(cabecalho, "Bottom Accent", 0f, 125f, 1872f, 3f, new Color(destaque.r, destaque.g, destaque.b, 0.55f));
            Texto(cabecalho, "Championship", config != null ? config.nomeCampeonato : "DISK FOOTBALL CHAMPIONSHIP",
                180f, 11f, 1512f, 33f, 26f, Color.white, true, TextAlignmentOptions.Center);
            Image logoCompeticao = Imagem(cabecalho, "Competition Logo", 20f, 19f, 73f, 73f);
            Image logoCanal = Imagem(cabecalho, "Channel Logo", 1779f, 19f, 73f, 73f);
            AtribuirImagem(logoCompeticao, tema != null ? tema.logoCompeticao : null);
            AtribuirImagem(logoCanal, tema != null ? tema.logoCanal : null);

            RectTransform placaA = Caixa(cabecalho, "Home Team", 156f, 51f, 590f, 66f, painel);
            RectTransform placaB = Caixa(cabecalho, "Away Team", 1126f, 51f, 590f, 66f, painel);
            faixaA = Caixa(placaA, "Home Color", 0f, 0f, 590f, 4f, new Color(0.15f, 0.7f, 1f)).GetComponent<Image>();
            faixaB = Caixa(placaB, "Away Color", 0f, 0f, 590f, 4f, new Color(1f, 0.65f, 0.12f)).GetComponent<Image>();
            escudoA = Imagem(placaA, "Home Crest", 12f, 9f, 47f, 47f);
            escudoB = Imagem(placaB, "Away Crest", 531f, 9f, 47f, 47f);
            nomeA = Texto(placaA, "Home Name", "HOME TEAM", 72f, 9f, 375f, 47f, 31f, Color.white, true, TextAlignmentOptions.Left);
            nomeB = Texto(placaB, "Away Name", "AWAY TEAM", 143f, 9f, 375f, 47f, 31f, Color.white, true, TextAlignmentOptions.Right);
            ui.placarTimeA = Texto(placaA, "Home Score", "0", 474f, 3f, 104f, 60f, 52f, Color.white, true, TextAlignmentOptions.Center);
            ui.placarTimeB = Texto(placaB, "Away Score", "0", 12f, 3f, 104f, 60f, 52f, Color.white, true, TextAlignmentOptions.Center);
            RectTransform relogio = Caixa(cabecalho, "Match Clock", 802f, 47f, 268f, 72f, new Color(0.025f, 0.15f, 0.16f, 1f));
            Caixa(relogio, "Clock Underline", 0f, 69f, 268f, 3f, destaque);
            ui.textoCronometro = Texto(relogio, "Clock", "00:00", 8f, 1f, 252f, 65f, 53f, Color.white, true, TextAlignmentOptions.Center);
            ui.textoCronometro.richText = true;
            ui.escudoTimeA = escudoA;
            ui.escudoTimeB = escudoB;
            ui.logoCampeonato = logoCompeticao;
            // The original UI clears old bindings and mounts its own compact player cards.
            ui.fotosTitularesA = new Image[0]; ui.fotosTitularesB = new Image[0];
            ui.nomesTitularesA = new TextMeshProUGUI[0]; ui.nomesTitularesB = new TextMeshProUGUI[0];
            ui.numerosTitularesA = new TextMeshProUGUI[0]; ui.numerosTitularesB = new TextMeshProUGUI[0];

            estado = Texto(raiz, "Match State", "READY FOR KICKOFF", 350f, 151f, 1220f, 28f,
                18f, destaque, false, TextAlignmentOptions.Center);
            string tituloRodape = config == null ? "DISC FOOTBALL" : TextoOpcional(config.tituloRodape);
            if ((config == null || config.mostrarTituloRodape) && tituloRodape.Length > 0)
                Texto(raiz, "Broadcast Signature", tituloRodape, 530f, 976f, 860f, 40f,
                    31f, Color.white, true, TextAlignmentOptions.Center);
            string formato = config != null && config.formato == FormatoDiscos.MataMata ? "KNOCKOUT" : "LEAGUE";
            int discos = config != null ? Mathf.Max(1, config.TotalDiscosPorTime) : 3;
            string linhaRodape = config == null || config.mostrarDetalhesFormato ? formato + "  ·  " + discos + " v " + discos : "";
            string subtitulo = config == null ? "AUTOMATIC DISC FOOTBALL" :
                config.mostrarSubtituloRodape ? TextoOpcional(config.subtituloRodape) : "";
            if (subtitulo.Length > 0) linhaRodape += (linhaRodape.Length > 0 ? "  ·  " : "") + subtitulo;
            if (linhaRodape.Length > 0)
                Texto(raiz, "Broadcast Format", linhaRodape, 530f, 1021f, 860f, 26f, 17f,
                    new Color(0.62f, 0.81f, 0.82f), false, TextAlignmentOptions.Center);

            RectTransform avisoGol = Caixa(raiz, "Goal Celebration", 640f, 198f, 640f, 124f, escuro);
            Caixa(avisoGol, "Goal Accent", 0f, 0f, 7f, 124f, new Color(1f, 0.79f, 0.18f));
            banner.painelBanner = avisoGol.gameObject;
            banner.fotoJogador = Imagem(avisoGol, "Scorer Photo", 24f, 12f, 98f, 98f);
            banner.textoDados = Texto(avisoGol, "Goal Text", "GOAL!", 140f, 12f, 476f, 98f,
                33f, Color.white, true, TextAlignmentOptions.Center);
            banner.textoDados.richText = true;
            ui.bannerGol = avisoGol.gameObject;
            avisoGol.gameObject.SetActive(false);
        }

        public void ConfigurarEquipes(TeamData a, TeamData b, Color corA, Color corB)
        {
            if (raiz == null) return;
            nomeA.text = NomeEquipe(a, "HOME TEAM");
            nomeB.text = NomeEquipe(b, "AWAY TEAM");
            faixaA.color = Visivel(corA);
            faixaB.color = Visivel(corB);
            AtribuirImagem(escudoA, a != null ? a.escudoOuBandeira : null);
            AtribuirImagem(escudoB, b != null ? b.escudoOuBandeira : null);
            MostrarEstado("READY FOR KICKOFF");
        }

        public void MostrarEstado(string texto)
        {
            if (estado != null) estado.text = string.IsNullOrWhiteSpace(texto) ? "" : texto.Trim();
        }

        public void Limpar()
        {
            if (bannerPartida != null) bannerPartida.LimparFila();
            if (raiz != null)
            {
                raiz.gameObject.SetActive(false);
                // Destroy is deferred in Play mode. Detach now so UniversalUI never
                // resolves a retired canvas during a rebuild in the same frame.
                raiz.SetParent(null, false);
                if (Application.isPlaying) Destroy(raiz.gameObject); else DestroyImmediate(raiz.gameObject);
            }
            if (interfacePartida != null)
            {
                interfacePartida.escudoTimeA = interfacePartida.escudoTimeB = interfacePartida.logoCampeonato = null;
                interfacePartida.placarTimeA = interfacePartida.placarTimeB = interfacePartida.textoCronometro = null;
                interfacePartida.bannerGol = null;
            }
            if (bannerPartida != null)
            {
                bannerPartida.painelBanner = null;
                bannerPartida.fotoJogador = null;
                bannerPartida.textoDados = null;
            }
            raiz = null;
            interfacePartida = null;
            bannerPartida = null;
            nomeA = nomeB = estado = null;
            faixaA = faixaB = escudoA = escudoB = null;
        }

        private static string NomeEquipe(TeamData time, string padrao)
        {
            return time != null && !string.IsNullOrWhiteSpace(time.nomeEquipe) ? time.nomeEquipe.Trim().ToUpperInvariant() : padrao;
        }

        private static string TextoOpcional(string texto)
        {
            return string.IsNullOrWhiteSpace(texto) ? "" : texto.Trim();
        }

        private static Color Visivel(Color cor)
        {
            cor.a = 1f;
            return cor.maxColorComponent < 0.2f ? Color.Lerp(cor, Color.white, 0.45f) : cor;
        }

        private static void AtribuirImagem(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
            image.color = Color.white;
        }

        private static RectTransform Retangulo(Transform pai, string nome, float x, float y, float largura, float altura)
        {
            GameObject objeto = new GameObject(nome, typeof(RectTransform));
            objeto.layer = 5;
            RectTransform rect = (RectTransform)objeto.transform;
            rect.SetParent(pai, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(largura, altura);
            return rect;
        }

        private static RectTransform Caixa(Transform pai, string nome, float x, float y, float largura, float altura, Color cor)
        {
            RectTransform rect = Retangulo(pai, nome, x, y, largura, altura);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = cor;
            image.raycastTarget = false;
            return rect;
        }

        private static Image Imagem(Transform pai, string nome, float x, float y, float largura, float altura)
        {
            Image image = Retangulo(pai, nome, x, y, largura, altura).gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private TextMeshProUGUI Texto(Transform pai, string nome, string valor, float x, float y, float largura, float altura,
            float tamanho, Color cor, bool titulo, TextAlignmentOptions alinhamento)
        {
            TextMeshProUGUI text = Retangulo(pai, nome, x, y, largura, altura).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = titulo ? fonteTitulo : fonteTexto;
            text.text = valor;
            text.fontSize = tamanho;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Min(tamanho, 13f);
            text.fontSizeMax = tamanho;
            text.fontStyle = titulo ? FontStyles.Bold : FontStyles.Normal;
            text.alignment = alinhamento;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.richText = false;
            text.color = cor;
            text.raycastTarget = false;
            text.margin = Vector4.zero;
            return text;
        }

        private void OnDestroy() { Limpar(); }
    }
}
