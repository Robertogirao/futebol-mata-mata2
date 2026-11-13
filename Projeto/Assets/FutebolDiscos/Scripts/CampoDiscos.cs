using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FutebolDeDiscos
{
    /// <summary>Original top-down disc-football stadium and its real 2D boundaries.</summary>
    [DisallowMultipleComponent]
    public sealed class CampoDiscos : MonoBehaviour
    {
        public const float MeioComprimento = 11f;
        public const float MeioLargura = 5.2f;
        public const float MeioGol = 1.65f;
        public const float ProfundidadeGol = 1.0f;
        private const float EspessuraParede = 0.14f;

        public Material MaterialSprites { get; private set; }
        public Sprite SpriteCirculo { get; private set; }
        public PhysicsMaterial2D MaterialFisica { get; private set; }

        private GameObject raiz;
        private Sprite quadrado;
        private readonly List<UnityEngine.Object> recursos = new List<UnityEngine.Object>();
        private SpriteRenderer brilhoEsquerdo, brilhoDireito;
        private float tempoBrilhoEsquerdo, tempoBrilhoDireito;
        private TMP_FontAsset fonte;
        private Color corLinha;

        public void Construir(ProjetoFutebolDiscos config)
        {
            if (config == null) throw new ArgumentNullException("config");
            Limpar();
            raiz = Filho("Disc Football Stadium", transform);
            MaterialSprites = CriarMaterial();
            recursos.Add(MaterialSprites);
            MaterialFisica = new PhysicsMaterial2D("Disc Football Boundaries") { friction = 0.025f, bounciness = 0.88f };
            recursos.Add(MaterialFisica);
            quadrado = CriarSprite(1, delegate(int x, int y) { return Color.white; });
            SpriteCirculo = CriarSprite(96, delegate(int x, int y)
            {
                float dx = (x + 0.5f) / 96f - 0.5f;
                float dy = (y + 0.5f) / 96f - 0.5f;
                return new Color(1f, 1f, 1f, Mathf.Clamp01((0.5f - Mathf.Sqrt(dx * dx + dy * dy)) * 192f));
            });
            fonte = config.tema != null && config.tema.fonteTexto != null ? config.tema.fonteTexto : TMP_Settings.defaultFontAsset;
            corLinha = config.corLinhas;
            corLinha.a = 1f;
            Color gramado = config.corGramado;
            gramado.a = 1f;

            DesenharArena();
            Retangulo("Field Shadow", Vector2.zero, new Vector2(22.85f, 11.25f), new Color(0.005f, 0.02f, 0.035f, 0.85f), -80);
            Retangulo("Field Glow", Vector2.zero, new Vector2(22.5f, 10.9f), new Color(corLinha.r, corLinha.g, corLinha.b, 0.22f), -78);
            Retangulo("Playing Surface", Vector2.zero, new Vector2(MeioComprimento * 2f, MeioLargura * 2f), gramado, -75);
            for (int i = 0; i < 10; i++)
            {
                float width = MeioComprimento * 2f / 10f;
                float x = -MeioComprimento + width * (i + 0.5f);
                Color tint = i % 2 == 0 ? Color.Lerp(gramado, Color.white, 0.045f) : Color.Lerp(gramado, Color.black, 0.055f);
                Retangulo("Grass Stripe " + i, new Vector2(x, 0f), new Vector2(width, MeioLargura * 2f), tint, -74);
            }
            if (config.fundoCampo != null)
                DesenharSprite("Custom Field Image", config.fundoCampo, Vector2.zero,
                    new Vector2(MeioComprimento * 2f, MeioLargura * 2f), new Color(1f, 1f, 1f, 0.35f), -73, false);

            DesenharMarcacoes();
            CriarLimites();
            CriarGol(true);
            CriarGol(false);
        }

        private static Material CriarMaterial()
        {
            Material modelo = Resources.Load<Material>("DiscosSprite");
            if (modelo != null) return new Material(modelo);
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            if (shader == null) throw new InvalidOperationException("A sprite shader is required to display the stadium.");
            return new Material(shader);
        }

        private void DesenharArena()
        {
            Retangulo("Stadium Backdrop", Vector2.zero, new Vector2(32f, 18f), new Color(0.013f, 0.025f, 0.055f), -100);
            for (int side = -1; side <= 1; side += 2)
            {
                Retangulo("Stand Panel", new Vector2(0f, side * 6.15f), new Vector2(26f, 0.82f), new Color(0.025f, 0.07f, 0.12f), -96);
                Retangulo("Stand Light", new Vector2(0f, side * 5.66f), new Vector2(25.6f, 0.065f), new Color(0.15f, 0.75f, 0.89f, 0.7f), -90);
                for (int i = 0; i < 56; i++)
                {
                    float x = -12.65f + i * 0.46f;
                    Color color = i % 3 == 0 ? new Color(1f, 0.70f, 0.23f, 0.74f) : new Color(0.18f, 0.51f, 0.70f, 0.65f);
                    DesenharSprite("Crowd Light", SpriteCirculo, new Vector2(x, side * (6.04f + (i % 2) * 0.25f)), new Vector2(0.14f, 0.14f), color, -92, false);
                }
                Retangulo("Side Light", new Vector2(side * 13.4f, 0f), new Vector2(0.065f, 10.5f), new Color(0.18f, 0.70f, 0.85f, 0.5f), -90);
            }
        }

        private void DesenharMarcacoes()
        {
            Contorno("Touchline", -MeioComprimento, -MeioLargura, MeioComprimento, MeioLargura, 0.055f, corLinha, -30);
            Linha("Halfway Line", new Vector2(0f, -MeioLargura), new Vector2(0f, MeioLargura), 0.045f, corLinha, -29);
            Anel("Center Circle", Vector2.zero, 1.48f, 0.045f, corLinha, -29);
            DesenharSprite("Center Spot", SpriteCirculo, Vector2.zero, new Vector2(0.13f, 0.13f), corLinha, -28, false);
            for (int side = -1; side <= 1; side += 2)
            {
                float end = side * MeioComprimento;
                float inner = side * (MeioComprimento - 3.4f);
                Linha("Penalty Box Inner", new Vector2(inner, -3.5f), new Vector2(inner, 3.5f), 0.045f, corLinha, -29);
                Linha("Penalty Box Top", new Vector2(inner, 3.5f), new Vector2(end, 3.5f), 0.045f, corLinha, -29);
                Linha("Penalty Box Bottom", new Vector2(inner, -3.5f), new Vector2(end, -3.5f), 0.045f, corLinha, -29);
                float six = side * (MeioComprimento - 1.35f);
                Linha("Goal Box Inner", new Vector2(six, -2.25f), new Vector2(six, 2.25f), 0.04f, corLinha, -29);
                Linha("Goal Box Top", new Vector2(six, 2.25f), new Vector2(end, 2.25f), 0.04f, corLinha, -29);
                Linha("Goal Box Bottom", new Vector2(six, -2.25f), new Vector2(end, -2.25f), 0.04f, corLinha, -29);
                DesenharSprite("Penalty Spot", SpriteCirculo, new Vector2(side * 8.45f, 0f), new Vector2(0.11f, 0.11f), corLinha, -28, false);
            }
            Color brand = new Color(corLinha.r, corLinha.g, corLinha.b, 0.11f);
            TextoMundo("Field Signature", "DISC FOOTBALL", new Vector2(0f, -2.55f), 4.3f, 10f, brand, -35);
        }

        private void CriarLimites()
        {
            Parede("North Boundary", new Vector2(0f, MeioLargura + EspessuraParede * 0.5f), new Vector2(MeioComprimento * 2f + EspessuraParede * 2f, EspessuraParede));
            Parede("South Boundary", new Vector2(0f, -MeioLargura - EspessuraParede * 0.5f), new Vector2(MeioComprimento * 2f + EspessuraParede * 2f, EspessuraParede));
            float height = MeioLargura - MeioGol;
            for (int side = -1; side <= 1; side += 2)
                for (int vertical = -1; vertical <= 1; vertical += 2)
                    Parede("End Boundary", new Vector2(side * (MeioComprimento + EspessuraParede * 0.5f), vertical * (MeioGol + height * 0.5f)), new Vector2(EspessuraParede, height));
        }

        private void CriarGol(bool esquerda)
        {
            int side = esquerda ? -1 : 1;
            string label = esquerda ? "Left Goal" : "Right Goal";
            float centerX = side * (MeioComprimento + ProfundidadeGol * 0.5f);
            Retangulo(label + " Floor", new Vector2(centerX, 0f), new Vector2(ProfundidadeGol, MeioGol * 2f), new Color(0.025f, 0.10f, 0.16f), -70);
            SpriteRenderer glow = Retangulo(label + " Celebration", new Vector2(centerX, 0f), new Vector2(ProfundidadeGol + 0.45f, MeioGol * 2f + 0.45f), new Color(1f, 0.74f, 0.16f, 0f), -60);
            if (esquerda) brilhoEsquerdo = glow; else brilhoDireito = glow;

            for (float y = -MeioGol + 0.15f; y < MeioGol; y += 0.22f)
                Linha(label + " Net Horizontal", new Vector2(side * MeioComprimento, y), new Vector2(side * (MeioComprimento + ProfundidadeGol), y), 0.012f, new Color(0.52f, 0.79f, 0.84f, 0.24f), -21);
            for (float depth = 0.15f; depth < ProfundidadeGol; depth += 0.19f)
                Linha(label + " Net Vertical", new Vector2(side * (MeioComprimento + depth), -MeioGol), new Vector2(side * (MeioComprimento + depth), MeioGol), 0.012f, new Color(0.52f, 0.79f, 0.84f, 0.24f), -21);
            Parede(label + " Back Net", new Vector2(side * (MeioComprimento + ProfundidadeGol + EspessuraParede * 0.5f), 0f), new Vector2(EspessuraParede, MeioGol * 2f + EspessuraParede * 2f));
            Parede(label + " Upper Net", new Vector2(centerX, MeioGol + EspessuraParede * 0.5f), new Vector2(ProfundidadeGol, EspessuraParede));
            Parede(label + " Lower Net", new Vector2(centerX, -MeioGol - EspessuraParede * 0.5f), new Vector2(ProfundidadeGol, EspessuraParede));

            for (int vertical = -1; vertical <= 1; vertical += 2)
            {
                SpriteRenderer post = DesenharSprite(label + " Post", SpriteCirculo, new Vector2(side * MeioComprimento, vertical * MeioGol), new Vector2(0.22f, 0.22f), Color.white, 5, false);
                CircleCollider2D collider = post.gameObject.AddComponent<CircleCollider2D>();
                collider.radius = 0.5f;
                collider.sharedMaterial = MaterialFisica;
            }
            GameObject trigger = Filho(label + " Sensor", raiz.transform);
            // Keep the sensor behind the line; scoring also verifies the whole ball crossed it.
            trigger.transform.localPosition = new Vector3(side * (MeioComprimento + 0.73f), 0f, 0f);
            BoxCollider2D sensor = trigger.AddComponent<BoxCollider2D>();
            sensor.size = new Vector2(0.54f, MeioGol * 2f - 0.22f);
            sensor.isTrigger = true;
            trigger.AddComponent<GolDiscos>().esquerda = esquerda;
        }

        public void CelebrarGol(bool esquerda)
        {
            if (esquerda) tempoBrilhoEsquerdo = 0.9f;
            else tempoBrilhoDireito = 0.9f;
            AtualizarBrilho(0f);
        }

        private void Update()
        {
            float delta = Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;
            if (!float.IsNaN(delta) && !float.IsInfinity(delta)) AtualizarBrilho(Mathf.Max(0f, delta));
        }

        private void AtualizarBrilho(float delta)
        {
            tempoBrilhoEsquerdo = Mathf.Max(0f, tempoBrilhoEsquerdo - delta);
            tempoBrilhoDireito = Mathf.Max(0f, tempoBrilhoDireito - delta);
            AplicarBrilho(brilhoEsquerdo, tempoBrilhoEsquerdo);
            AplicarBrilho(brilhoDireito, tempoBrilhoDireito);
        }

        private static void AplicarBrilho(SpriteRenderer renderer, float time)
        {
            if (renderer == null) return;
            float alpha = time <= 0f ? 0f : (0.2f + 0.55f * Mathf.Clamp01(time / 0.9f)) * (0.7f + 0.3f * Mathf.Sin(time * 22f));
            renderer.color = new Color(1f, 0.74f, 0.16f, alpha);
        }

        private SpriteRenderer Parede(string nome, Vector2 centro, Vector2 tamanho)
        {
            SpriteRenderer renderer = Retangulo(nome, centro, tamanho, new Color(0.12f, 0.68f, 0.76f), -20);
            BoxCollider2D collider = renderer.gameObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.sharedMaterial = MaterialFisica;
            return renderer;
        }

        private void Contorno(string nome, float left, float bottom, float right, float top, float thickness, Color color, int order)
        {
            Linha(nome, new Vector2(left, bottom), new Vector2(right, bottom), thickness, color, order);
            Linha(nome, new Vector2(right, bottom), new Vector2(right, top), thickness, color, order);
            Linha(nome, new Vector2(right, top), new Vector2(left, top), thickness, color, order);
            Linha(nome, new Vector2(left, top), new Vector2(left, bottom), thickness, color, order);
        }

        private void Anel(string nome, Vector2 center, float radius, float thickness, Color color, int order)
        {
            const int segments = 96;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments, b = (i + 1) * Mathf.PI * 2f / segments;
                Linha(nome, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                    center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, thickness, color, order);
            }
        }

        private SpriteRenderer Linha(string nome, Vector2 a, Vector2 b, float width, Color color, int order)
        {
            Vector2 direction = b - a;
            SpriteRenderer result = Retangulo(nome, (a + b) * 0.5f, new Vector2(direction.magnitude, width), color, order);
            result.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            return result;
        }

        private SpriteRenderer Retangulo(string nome, Vector2 center, Vector2 size, Color color, int order)
        {
            return DesenharSprite(nome, quadrado, center, size, color, order, false);
        }

        private SpriteRenderer DesenharSprite(string nome, Sprite sprite, Vector2 center, Vector2 size, Color color, int order, bool preserveAspect)
        {
            var go = Filho(nome, raiz.transform);
            go.transform.localPosition = new Vector3(center.x, center.y, 0f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = MaterialSprites;
            renderer.sortingOrder = order;
            renderer.color = color;
            if (sprite != null)
            {
                float x = size.x / Mathf.Max(0.0001f, sprite.bounds.size.x);
                float y = size.y / Mathf.Max(0.0001f, sprite.bounds.size.y);
                if (preserveAspect) x = y = Mathf.Min(x, y);
                go.transform.localScale = new Vector3(x, y, 1f);
            }
            return renderer;
        }

        private Sprite CriarSprite(int tamanho, Func<int, int, Color> pixel)
        {
            var texture = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, false);
            texture.name = "Disc Football Shape";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] colors = new Color[tamanho * tamanho];
            for (int y = 0; y < tamanho; y++) for (int x = 0; x < tamanho; x++) colors[y * tamanho + x] = pixel(x, y);
            texture.SetPixels(colors);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, tamanho, tamanho), new Vector2(0.5f, 0.5f), tamanho);
            recursos.Add(sprite); recursos.Add(texture);
            return sprite;
        }

        private void TextoMundo(string nome, string text, Vector2 position, float fontSize, float width, Color color, int order)
        {
            if (fonte == null) return;
            GameObject go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(raiz.transform, false);
            go.transform.localPosition = new Vector3(position.x, position.y, 0f);
            TextMeshPro label = go.AddComponent<TextMeshPro>();
            label.font = fonte; label.text = text; label.fontSize = fontSize;
            label.color = color; label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.rectTransform.sizeDelta = new Vector2(width, 0.6f);
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sortingOrder = order;
        }

        private static GameObject Filho(string nome, Transform pai)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            return go;
        }

        public void Limpar()
        {
            if (raiz != null) { raiz.SetActive(false); Destruir(raiz); raiz = null; }
            foreach (UnityEngine.Object recurso in recursos) Destruir(recurso);
            recursos.Clear();
            MaterialSprites = null; MaterialFisica = null; SpriteCirculo = quadrado = null;
            brilhoEsquerdo = brilhoDireito = null;
            tempoBrilhoEsquerdo = tempoBrilhoDireito = 0f;
        }

        private static void Destruir(UnityEngine.Object item)
        {
            if (item == null) return;
            if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
        }

        private void OnDestroy() { Limpar(); }
    }
}
