using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PokemonMarbleRace
{
    [Serializable]
    public sealed class RaceHudEntry
    {
        public string id;
        public string name;
        public string detail;
        public Sprite portrait;
        public Color color = Color.white;
        public int position;
        public int points;
        public bool finished;
    }

    /// <summary>Broadcast graphics for the race. Build is explicit, independent of Awake order.</summary>
    [DisallowMultipleComponent]
    public sealed class RaceHud : MonoBehaviour
    {
        private static readonly Color Ink = new Color(0.018f, 0.035f, 0.09f, 0.97f);
        private static readonly Color Panel = new Color(0.025f, 0.065f, 0.15f, 0.96f);
        private static readonly Color Cyan = new Color(0.08f, 0.87f, 1f, 1f);
        private static readonly Color Gold = new Color(1f, 0.8f, 0.13f, 1f);
        private static readonly Color Muted = new Color(0.58f, 0.74f, 0.85f, 1f);

        private sealed class LiveRow
        {
            public RectTransform root;
            public Image plate, accent, portrait;
            public TextMeshProUGUI rank, name, detail, points;
        }

        private sealed class ResultRow
        {
            public RectTransform root;
            public Image plate, accent, portrait;
            public TextMeshProUGUI rank, name, detail, points;
        }

        private readonly List<LiveRow> liveRows = new List<LiveRow>();
        private readonly List<ResultRow> resultRows = new List<ResultRow>();
        private RectTransform root;
        private RectTransform livePanel;
        private RectTransform overlay;
        private RectTransform messagePanel;
        private RectTransform resultsPanel;
        private RectTransform resultRowsRoot;
        private RectTransform abilityPanel;
        private Image abilityAccent;
        private Image timerPanel;
        private TextMeshProUGUI competitionTitle, raceTitle, raceSubtitle, timer, raceStatus;
        private TextMeshProUGUI abilityTitle, abilityRunner, abilityTime;
        private TextMeshProUGUI messageTitle, messageBody;
        private TextMeshProUGUI resultsTitle, resultsFooter;
        private TMP_FontAsset headingFont, regularFont;

        public void Build(TMP_FontAsset titleFont, TMP_FontAsset bodyFont, string competitionName)
        {
            TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
            if (fallback == null)
                fallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            headingFont = titleFont != null ? titleFont : bodyFont != null ? bodyFont : fallback;
            regularFont = bodyFont != null ? bodyFont : headingFont;
            if (root != null)
            {
                competitionTitle.text = Safe(competitionName, "POKÉMON MARBLE RACE");
                ApplyFonts();
                return;
            }

            GameObject canvasObject = new GameObject("MarbleRace.Broadcast",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.layer = 5;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 4000;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            root = (RectTransform)canvasObject.transform;

            CreateHeader(Safe(competitionName, "POKÉMON MARBLE RACE"));
            livePanel = Rect(root, "LivePositions", 20f, 166f, 205f, 720f);
            Label(livePanel, "LivePositionsTitle", "LIVE POSITIONS", 0f, 0f, 205f, 28f,
                19f, Cyan, true);
            Label(root, "BroadcastSignature", "POKÉMON MARBLE RACE", 24f, 1005f, 305f, 28f,
                19f, Cyan, true);
            Label(root, "BroadcastTagline", "RACE · ADAPT · WIN", 24f, 1036f, 305f, 22f,
                14f, Muted, false);
            CreateAbilityPanel();
            CreateOverlays();
        }

        public void SetRaceInfo(string competitionName, string raceLabel, int raceNumber,
            int totalRaces, float secondsRemaining, string status)
        {
            EnsureBuilt();
            competitionTitle.text = Safe(competitionName, "POKÉMON MARBLE RACE");
            raceTitle.text = "RACE " + Mathf.Max(1, raceNumber) + " / " + Mathf.Max(1, totalRaces);
            raceSubtitle.text = Safe(raceLabel, "GRAND PRIX");
            timer.text = FormatTime(secondsRemaining);
            bool urgent = Finite(secondsRemaining) && secondsRemaining > 0f && secondsRemaining <= 10f;
            timer.color = urgent ? Gold : Color.white;
            timerPanel.color = urgent ? new Color(0.25f, 0.09f, 0.025f, 0.98f) : Panel;
            raceStatus.text = Safe(status, "RACE TO THE FINISH");
        }

        public void SetLeaderboard(List<RaceHudEntry> entries)
        {
            EnsureBuilt();
            int count = entries != null ? entries.Count : 0;
            // Sixteen names remain readable in the course's left broadcast margin.
            // Additional entrants are available in the final standings overlay.
            int visible = Math.Min(16, count);
            while (liveRows.Count < visible) liveRows.Add(CreateLiveRow(liveRows.Count));
            float pitch = visible <= 8 ? 68f : 39f;
            float height = visible <= 8 ? 62f : 35f;
            for (int i = 0; i < liveRows.Count; i++)
            {
                LiveRow row = liveRows[i];
                bool show = i < visible;
                row.root.gameObject.SetActive(show);
                if (!show) continue;
                RaceHudEntry entry = entries[i];
                Place(row.root, 0f, 38f + i * pitch, 205f, height);
                ConfigureLiveGeometry(row, height);
                bool leading = i == 0;
                Color runnerColor = entry != null ? VisibleColor(entry.color) : Cyan;
                row.plate.color = leading ? new Color(0.17f, 0.12f, 0.025f, 0.97f) : Panel;
                row.accent.color = runnerColor;
                row.rank.color = leading ? Gold : Color.white;
                row.rank.text = Position(entry, i).ToString(CultureInfo.InvariantCulture);
                row.name.text = Safe(entry != null ? entry.name : null, "POKÉMON");
                row.detail.text = Safe(entry != null ? entry.detail : null,
                    entry != null && entry.finished ? "FINISHED" : "RACING");
                row.detail.color = entry != null && entry.finished ? Gold : Muted;
                row.points.text = entry != null && entry.points > 0
                    ? entry.points.ToString(CultureInfo.InvariantCulture) + " PTS" : "";
                SetPortrait(row.portrait, entry != null ? entry.portrait : null);
            }
        }

        public void SetAbility(string runnerName, string ability, float secondsRemaining)
        {
            EnsureBuilt();
            bool show = !string.IsNullOrWhiteSpace(ability) && Finite(secondsRemaining) && secondsRemaining > 0f;
            abilityPanel.gameObject.SetActive(show);
            if (!show) return;
            abilityTitle.text = ability.Trim().ToUpperInvariant();
            abilityRunner.text = Safe(runnerName, "POKÉMON");
            abilityTime.text = secondsRemaining.ToString("0.0", CultureInfo.InvariantCulture) + "s";
            abilityAccent.color = AbilityColor(ability);
            abilityTitle.color = abilityAccent.color;
        }

        public void ShowMessage(string title, string body)
        {
            EnsureBuilt();
            resultsPanel.gameObject.SetActive(false);
            messagePanel.gameObject.SetActive(true);
            messageTitle.text = Safe(title, "GET READY");
            messageBody.text = Safe(body, "The next race is about to begin.");
            overlay.gameObject.SetActive(true);
        }

        public void HideOverlay()
        {
            if (overlay != null) overlay.gameObject.SetActive(false);
        }

        public void ShowResults(string title, List<RaceHudEntry> entries, string footer)
        {
            EnsureBuilt();
            messagePanel.gameObject.SetActive(false);
            resultsPanel.gameObject.SetActive(true);
            resultsTitle.text = Safe(title, "RACE RESULTS");
            resultsFooter.text = Safe(footer, "Results saved.");
            int count = entries != null ? entries.Count : 0;
            while (resultRows.Count < count) resultRows.Add(CreateResultRow(resultRows.Count));
            float pitch = count <= 8 ? 66f : Mathf.Min(52f, 568f / Mathf.Max(1, count));
            float height = Mathf.Max(16f, pitch - 4f);
            for (int i = 0; i < resultRows.Count; i++)
            {
                ResultRow row = resultRows[i];
                bool show = i < count;
                row.root.gameObject.SetActive(show);
                if (!show) continue;
                RaceHudEntry entry = entries[i];
                Place(row.root, 0f, i * pitch, 1120f, height);
                ConfigureResultGeometry(row, height);
                Color runnerColor = entry != null ? VisibleColor(entry.color) : Cyan;
                row.plate.color = i == 0 ? new Color(0.22f, 0.14f, 0.025f, 0.99f) : Panel;
                row.accent.color = runnerColor;
                row.rank.color = i == 0 ? Gold : Color.white;
                row.rank.text = Position(entry, i).ToString(CultureInfo.InvariantCulture);
                row.name.text = Safe(entry != null ? entry.name : null, "POKÉMON");
                row.points.text = (entry != null ? entry.points : 0).ToString(CultureInfo.InvariantCulture) + " PTS";
                row.detail.text = Safe(entry != null ? entry.detail : null,
                    entry != null && entry.finished ? "FINISHED" : "TIME LIMIT");
                SetPortrait(row.portrait, entry != null ? entry.portrait : null);
            }
            overlay.gameObject.SetActive(true);
        }

        public void Clear()
        {
            if (root != null)
            {
                root.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(root.gameObject);
                else DestroyImmediate(root.gameObject);
            }
            root = null;
            livePanel = overlay = messagePanel = resultsPanel = resultRowsRoot = abilityPanel = null;
            liveRows.Clear();
            resultRows.Clear();
        }

        private void EnsureBuilt()
        {
            if (root == null) Build(null, null, "POKÉMON MARBLE RACE");
        }

        private void CreateHeader(string competitionName)
        {
            RectTransform header = Box(root, "Header", 20f, 18f, 1880f, 114f, Ink);
            Box(header, "HeaderAccent", 0f, 110f, 1880f, 4f, Cyan);
            competitionTitle = Label(header, "CompetitionName", competitionName,
                24f, 13f, 1125f, 57f, 43f, Color.white, true);
            Label(header, "RaceBrand", "MARBLE RACE", 27f, 75f, 1100f, 25f, 21f, Cyan, true);

            RectTransform raceBox = Box(header, "RaceCard", 1190f, 10f, 408f, 92f, Panel);
            raceTitle = Label(raceBox, "RaceNumber", "RACE 1 / 3", 12f, 9f, 384f, 43f,
                36f, Gold, true, TextAlignmentOptions.Center);
            raceSubtitle = Label(raceBox, "RaceLabel", "GRAND PRIX", 12f, 56f, 384f, 26f,
                18f, Muted, false, TextAlignmentOptions.Center);

            RectTransform clockBox = Box(header, "ClockCard", 1622f, 10f, 244f, 92f, Panel);
            timerPanel = clockBox.GetComponent<Image>();
            timer = Label(clockBox, "RaceClock", "00:45", 7f, 4f, 230f, 80f,
                61f, Color.white, true, TextAlignmentOptions.Center);
            raceStatus = Label(root, "RaceStatus", "RACE TO THE FINISH", 380f, 140f,
                1490f, 31f, 22f, Color.white, true, TextAlignmentOptions.Center);
        }

        private void CreateAbilityPanel()
        {
            abilityPanel = Box(root, "ActiveAbility", 360f, 982f, 1535f, 72f, Ink);
            abilityAccent = Box(abilityPanel, "AbilityAccent", 0f, 0f, 6f, 72f, Cyan).GetComponent<Image>();
            abilityRunner = Label(abilityPanel, "AbilityRunner", "POKÉMON", 25f, 12f, 370f, 45f,
                26f, Color.white, true);
            abilityTitle = Label(abilityPanel, "AbilityName", "ELECTRIC TURBO", 420f, 12f, 880f, 45f,
                29f, Cyan, true);
            abilityTime = Label(abilityPanel, "AbilityTime", "0.0s", 1320f, 12f, 190f, 45f,
                29f, Gold, true, TextAlignmentOptions.Right);
            abilityPanel.gameObject.SetActive(false);
        }

        private void CreateOverlays()
        {
            overlay = Rect(root, "Overlay", 0f, 0f, 1920f, 1080f);
            Box(overlay, "Dimmer", 0f, 0f, 1920f, 1080f, new Color(0.005f, 0.015f, 0.045f, 0.87f));
            messagePanel = Box(overlay, "MessagePanel", 320f, 300f, 1280f, 460f, Ink);
            Box(messagePanel, "MessageTopAccent", 0f, 0f, 1280f, 5f, Cyan);
            Box(messagePanel, "MessageBottomAccent", 0f, 455f, 1280f, 5f, Gold);
            messageTitle = Label(messagePanel, "MessageTitle", "GET READY", 45f, 40f,
                1190f, 105f, 68f, Gold, true, TextAlignmentOptions.Center);
            messageBody = Label(messagePanel, "MessageBody", "The next race is about to begin.",
                70f, 165f, 1140f, 240f, 29f, Color.white, false, TextAlignmentOptions.Center);
            messageBody.textWrappingMode = TextWrappingModes.Normal;

            resultsPanel = Box(overlay, "ResultsPanel", 340f, 105f, 1240f, 870f, Ink);
            Box(resultsPanel, "ResultsTopAccent", 0f, 0f, 1240f, 5f, Cyan);
            resultsTitle = Label(resultsPanel, "ResultsTitle", "RACE RESULTS", 60f, 26f,
                1120f, 75f, 50f, Gold, true, TextAlignmentOptions.Center);
            Label(resultsPanel, "PositionHeader", "POS", 73f, 115f, 65f, 27f, 18f, Muted, true);
            Label(resultsPanel, "PokemonHeader", "POKÉMON", 210f, 115f, 520f, 27f, 18f, Muted, true);
            Label(resultsPanel, "PointsHeader", "POINTS", 760f, 115f, 160f, 27f,
                18f, Muted, true, TextAlignmentOptions.Center);
            Label(resultsPanel, "ResultHeader", "RESULT", 950f, 115f, 215f, 27f,
                18f, Muted, true, TextAlignmentOptions.Right);
            resultRowsRoot = Rect(resultsPanel, "ResultRows", 60f, 155f, 1120f, 568f);
            Box(resultsPanel, "ResultsFooterDivider", 60f, 752f, 1120f, 2f, Cyan);
            resultsFooter = Label(resultsPanel, "ResultsFooter", "Results saved.", 60f, 778f,
                1120f, 55f, 25f, Color.white, false, TextAlignmentOptions.Center);
            resultsFooter.textWrappingMode = TextWrappingModes.Normal;
            overlay.gameObject.SetActive(false);
        }

        private LiveRow CreateLiveRow(int index)
        {
            var row = new LiveRow();
            row.root = Box(livePanel, "LiveRow_" + index, 0f, 38f, 205f, 62f, Panel);
            row.plate = row.root.GetComponent<Image>();
            row.accent = Box(row.root, "RunnerAccent", 0f, 0f, 3f, 62f, Cyan).GetComponent<Image>();
            row.rank = Label(row.root, "Position", "1", 6f, 5f, 25f, 36f, 25f, Color.white,
                true, TextAlignmentOptions.Center);
            row.portrait = Picture(row.root, "Portrait", 33f, 5f, 43f, 43f);
            row.name = Label(row.root, "RunnerName", "POKÉMON", 81f, 5f, 119f, 28f, 21f, Color.white, true);
            row.detail = Label(row.root, "RunnerDetail", "RACING", 81f, 35f, 119f, 20f, 13f, Muted, false);
            row.points = Label(row.root, "Points", "", 3f, 45f, 73f, 15f,
                10f, Muted, false, TextAlignmentOptions.Center);
            return row;
        }

        private static void ConfigureLiveGeometry(LiveRow row, float height)
        {
            bool compact = height < 50f;
            Place(row.accent.rectTransform, 0f, 0f, 3f, height);
            Place(row.rank.rectTransform, 6f, compact ? 2f : 5f, 25f, compact ? 30f : 36f);
            Place(row.portrait.rectTransform, 33f, compact ? 3f : 5f, compact ? 29f : 43f, compact ? 29f : 43f);
            Place(row.name.rectTransform, compact ? 68f : 81f, compact ? 1f : 5f,
                compact ? 132f : 119f, compact ? 20f : 28f);
            Place(row.detail.rectTransform, compact ? 68f : 81f, compact ? 21f : 35f,
                compact ? 132f : 119f, compact ? 13f : 20f);
            row.name.fontSize = compact ? 16f : 21f;
            row.name.fontSizeMax = row.name.fontSize;
            row.name.fontSizeMin = compact ? 11f : 14f;
            row.detail.fontSize = compact ? 10f : 13f;
            row.detail.fontSizeMax = row.detail.fontSize;
            row.detail.fontSizeMin = compact ? 9f : 11f;
            row.points.gameObject.SetActive(!compact);
        }

        private ResultRow CreateResultRow(int index)
        {
            var row = new ResultRow();
            row.root = Box(resultRowsRoot, "ResultRow_" + index, 0f, 0f, 1120f, 62f, Panel);
            row.plate = row.root.GetComponent<Image>();
            row.accent = Box(row.root, "RunnerAccent", 0f, 0f, 5f, 62f, Cyan).GetComponent<Image>();
            row.rank = Label(row.root, "Position", "1", 12f, 4f, 65f, 54f,
                31f, Color.white, true, TextAlignmentOptions.Center);
            row.portrait = Picture(row.root, "Portrait", 87f, 5f, 52f, 52f);
            row.name = Label(row.root, "RunnerName", "POKÉMON", 150f, 4f, 520f, 54f, 29f, Color.white, true);
            row.points = Label(row.root, "Points", "0 PTS", 690f, 4f, 170f, 54f,
                27f, Gold, true, TextAlignmentOptions.Center);
            row.detail = Label(row.root, "Result", "FINISHED", 880f, 4f, 225f, 54f,
                24f, Muted, false, TextAlignmentOptions.Right);
            return row;
        }

        private static void ConfigureResultGeometry(ResultRow row, float height)
        {
            float portraitSize = Mathf.Max(12f, height - 8f);
            float fontSize = Mathf.Clamp(height * 0.52f, 12f, 29f);
            Place(row.accent.rectTransform, 0f, 0f, 5f, height);
            Place(row.rank.rectTransform, 12f, 2f, 65f, height - 4f);
            Place(row.portrait.rectTransform, 87f, 4f, portraitSize, portraitSize);
            Place(row.name.rectTransform, 150f, 2f, 520f, height - 4f);
            Place(row.points.rectTransform, 690f, 2f, 170f, height - 4f);
            Place(row.detail.rectTransform, 880f, 2f, 225f, height - 4f);
            SetFontSize(row.rank, fontSize);
            SetFontSize(row.name, fontSize);
            SetFontSize(row.points, fontSize * 0.9f);
            SetFontSize(row.detail, fontSize * 0.82f);
        }

        private static void SetFontSize(TextMeshProUGUI label, float size)
        {
            label.fontSize = size;
            label.fontSizeMax = size;
            label.fontSizeMin = Mathf.Max(9f, size * 0.7f);
        }

        private void ApplyFonts()
        {
            foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                text.font = text.fontStyle == FontStyles.Bold ? headingFont : regularFont;
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            GameObject item = new GameObject(name, typeof(RectTransform));
            item.layer = 5;
            item.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)item.transform;
            Place(rect, x, y, width, height);
            return rect;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(Mathf.Max(0f, width), Mathf.Max(0f, height));
        }

        private static RectTransform Box(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            RectTransform rect = Rect(parent, name, x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static Image Picture(Transform parent, string name, float x, float y, float width, float height)
        {
            RectTransform rect = Rect(parent, name, x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            image.enabled = false;
            return image;
        }

        private TextMeshProUGUI Label(Transform parent, string name, string value,
            float x, float y, float width, float height, float size, Color color,
            bool bold, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            RectTransform rect = Rect(parent, name, x, y, width, height);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = bold ? headingFont : regularFont;
            text.text = value;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMax = size;
            text.fontSizeMin = Mathf.Max(10f, size * 0.63f);
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.alignment = alignment;
            text.color = color;
            text.richText = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.margin = Vector4.zero;
            return text;
        }

        private static void SetPortrait(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private static int Position(RaceHudEntry entry, int index)
        {
            return entry != null && entry.position > 0 ? entry.position : index + 1;
        }

        private static Color VisibleColor(Color value)
        {
            if (!Finite(value.r) || !Finite(value.g) || !Finite(value.b) || value.r + value.g + value.b < 0.15f)
                return Cyan;
            return new Color(Mathf.Clamp01(value.r), Mathf.Clamp01(value.g), Mathf.Clamp01(value.b), 1f);
        }

        private static Color AbilityColor(string ability)
        {
            string upper = ability.ToUpperInvariant();
            if (upper.Contains("WATER")) return new Color(0.12f, 0.65f, 1f);
            if (upper.Contains("ICE")) return new Color(0.6f, 0.94f, 1f);
            if (upper.Contains("FIRE") || upper.Contains("LAVA")) return new Color(1f, 0.38f, 0.12f);
            if (upper.Contains("GHOST")) return new Color(0.8f, 0.45f, 1f);
            return Gold;
        }

        private static string Safe(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static string FormatTime(float seconds)
        {
            float safe = Finite(seconds) ? Mathf.Clamp(seconds, 0f, 359999f) : 0f;
            int value = Mathf.CeilToInt(safe);
            return (value / 60).ToString("00", CultureInfo.InvariantCulture) + ":" +
                (value % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void OnEnable()
        {
            if (root != null) root.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            if (root != null) root.gameObject.SetActive(false);
        }

        private void OnDestroy() { Clear(); }
    }
}
