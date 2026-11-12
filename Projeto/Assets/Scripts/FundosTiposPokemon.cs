using System;
using System.Collections.Generic;

/// <summary>Arte procedural original. Pixels RGBA, origem inferior esquerda para Texture2D.</summary>
public static class FundosTiposPokemon
{
    public const int Largura = 1920;
    public const int Altura = 1080;
    private static readonly int[] Cores =
    {
        0x22D9FF, 0xE0BE8A, 0xFF742B, 0x20CFFF, 0xFFE340, 0x69E783,
        0xA5ECFF, 0xFF765E, 0xD78BFF, 0xDCA76A, 0xB5D5FF, 0xFF8BCC,
        0xB8E653, 0xD6C2A0, 0xBC9CFF, 0x9B8AFF, 0xAF9CCA, 0xB1DDE6, 0xFFB6E2
    };

    public static int CorDestaque(TipoPokemon tipo)
    {
        int n = (int)tipo;
        if (n < 0 || n >= Cores.Length) throw new ArgumentException("Tipo inválido para o fundo.");
        return Cores[n];
    }

    public static string NomeTipo(TipoPokemon tipo)
    {
        switch (tipo)
        {
            case TipoPokemon.Nenhum: return "MISTO";
            case TipoPokemon.Agua: return "ÁGUA";
            case TipoPokemon.Eletrico: return "ELÉTRICO";
            case TipoPokemon.Psiquico: return "PSÍQUICO";
            case TipoPokemon.Dragao: return "DRAGÃO";
            case TipoPokemon.Aco: return "AÇO";
            default: return tipo.ToString().ToUpperInvariant();
        }
    }

    public static byte[] Gerar(TipoPokemon tipo, bool arena)
    {
        return GerarComCor(tipo, arena, CorDestaque(tipo));
    }

    public static byte[] GerarComCor(TipoPokemon tipo, bool arena, int cor)
    {
        CorDestaque(tipo);
        if (cor < 0 || cor > 0xFFFFFF) throw new ArgumentException("Cor RGB inválida para o fundo.");
        var tela = new Tela();
        tela.Gradiente(cor);
        if (arena)
        {
            // Piso e teto acompanham a arena ortográfica da base.
            tela.Retangulo(0, 885, 1920, 1080, 0x030A17, 210);
            for (int x = -1400; x <= 3300; x += 250)
                tela.Linha(960, 830, x, 1080, 2, cor, 45);
            for (int y = 925; y < 1080; y += 64) tela.Linha(0, y, 1920, y, 2, cor, 55);
            tela.Painel(450, 52, 1020, 226, cor, 68);
            tela.Painel(46, 100, 306, 166, cor, 65);
            tela.Painel(1568, 100, 306, 166, cor, 65);
            tela.Motivo(tipo, 199, 185, 62, cor, 205);
            tela.Motivo(tipo, 1721, 185, 62, cor, 205);
            tela.Painel(0, 282, 1920, 610, cor, 55);
            int baseR = 4 + ((cor >> 16) & 255) * 9 / 100;
            int baseG = 6 + ((cor >> 8) & 255) * 9 / 100;
            int baseB = 12 + (cor & 255) * 9 / 100;
            tela.Retangulo(20, 308, 1900, 873, (baseR << 16) | (baseG << 8) | baseB, 216);
            tela.Grade(24, 320, 1896, 866, cor, 11);
            tela.Motivo(tipo, 960, 590, 177, cor, 12);
            tela.Neon(28, 305, 1892, 305, cor);
            tela.Neon(28, 880, 1892, 880, cor);
            tela.Neon(18, 326, 18, 855, cor);
            tela.Neon(1902, 326, 1902, 855, cor);
            for (int x = 60; x < 1900; x += 310)
            {
                tela.Forma(x, 291, 1, new float[] { 0,-5, 114,-5, 105,5, -9,5 }, cor, 190);
                tela.Forma(x, 895, 1, new float[] { 0,-4, 114,-4, 105,4, -9,4 }, cor, 190);
            }
        }
        else
        {
            tela.Grade(40, 30, 1880, 1040, cor, 15);
            tela.Painel(26, 24, 1868, 1032, cor, 25);
            tela.Retangulo(240, 188, 1680, 960, 0x020817, 135);
            tela.Motivo(tipo, 150, 567, 106, cor, 50);
            tela.Motivo(tipo, 1770, 567, 106, cor, 50);
            tela.Motivo(tipo, 960, 610, 280, cor, 7);
            tela.Neon(70, 165, 1850, 165, cor);
            tela.Neon(70, 992, 1850, 992, cor);
            tela.Linha(40, 102, 280, 20, 20, cor, 35);
            tela.Linha(1640, 1060, 1880, 978, 20, cor, 35);
        }
        return tela.Pixels;
    }

    private sealed class Tela
    {
        public readonly byte[] Pixels = new byte[Largura * Altura * 4];

        public void Gradiente(int cor)
        {
            int r = (cor >> 16) & 255, g = (cor >> 8) & 255, b = cor & 255;
            for (int y = 0; y < Altura; y++)
            {
                float fy = (y - 460f) / Altura;
                for (int x = 0; x < Largura; x++)
                {
                    float fx = (x - 960f) / Largura;
                    float luz = Math.Max(0f, 1f - fx * fx * 2.5f - fy * fy * 1.8f);
                    int i = ((Altura - 1 - y) * Largura + x) * 4;
                    Pixels[i] = (byte)(3 + r * (0.025f + luz * 0.055f));
                    Pixels[i + 1] = (byte)(6 + g * (0.035f + luz * 0.075f));
                    Pixels[i + 2] = (byte)(14 + b * (0.04f + luz * 0.10f));
                    Pixels[i + 3] = 255;
                }
            }
        }

        private void Pixel(int x, int y, int cor, int alfa)
        {
            if (x < 0 || y < 0 || x >= Largura || y >= Altura || alfa <= 0) return;
            int i = ((Altura - 1 - y) * Largura + x) * 4;
            int inverso = 255 - alfa;
            Pixels[i] = (byte)((Pixels[i] * inverso + ((cor >> 16) & 255) * alfa) / 255);
            Pixels[i + 1] = (byte)((Pixels[i + 1] * inverso + ((cor >> 8) & 255) * alfa) / 255);
            Pixels[i + 2] = (byte)((Pixels[i + 2] * inverso + (cor & 255) * alfa) / 255);
        }

        public void Retangulo(int x1, int y1, int x2, int y2, int cor, int alfa)
        {
            for (int y = Math.Max(0, y1); y < Math.Min(Altura, y2); y++)
                for (int x = Math.Max(0, x1); x < Math.Min(Largura, x2); x++) Pixel(x, y, cor, alfa);
        }

        public void Linha(float x1, float y1, float x2, float y2, float espessura, int cor, int alfa)
        {
            float dx = x2 - x1, dy = y2 - y1;
            float comprimento = dx * dx + dy * dy;
            float raio = espessura / 2f;
            int minX = Math.Max(0, (int)(Math.Min(x1, x2) - raio - 1));
            int maxX = Math.Min(Largura - 1, (int)(Math.Max(x1, x2) + raio + 1));
            int minY = Math.Max(0, (int)(Math.Min(y1, y2) - raio - 1));
            int maxY = Math.Min(Altura - 1, (int)(Math.Max(y1, y2) + raio + 1));
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    float t = comprimento > 0f ? Math.Max(0f, Math.Min(1f, ((x - x1) * dx + (y - y1) * dy) / comprimento)) : 0f;
                    float px = x - x1 - t * dx, py = y - y1 - t * dy;
                    if (px * px + py * py <= raio * raio) Pixel(x, y, cor, alfa);
                }
        }

        public void Neon(float x1, float y1, float x2, float y2, int cor)
        {
            Linha(x1, y1, x2, y2, 24, cor, 12);
            Linha(x1, y1, x2, y2, 10, cor, 36);
            Linha(x1, y1, x2, y2, 3, cor, 235);
            Linha(x1, y1, x2, y2, 1, 0xFFFFFF, 150);
        }

        public void Forma(float cx, float cy, float escala, float[] pontos, int cor, int alfa)
        {
            int n = pontos.Length / 2;
            float min = Altura, max = 0;
            for (int i = 0; i < n; i++) { float y = cy + pontos[i * 2 + 1] * escala; min = Math.Min(min, y); max = Math.Max(max, y); }
            var cruzamentos = new List<float>(n);
            for (int y = Math.Max(0, (int)min); y <= Math.Min(Altura - 1, (int)max); y++)
            {
                cruzamentos.Clear();
                for (int i = 0, j = n - 1; i < n; j = i++)
                {
                    float yi = cy + pontos[i * 2 + 1] * escala, yj = cy + pontos[j * 2 + 1] * escala;
                    if ((yi > y) == (yj > y)) continue;
                    float xi = cx + pontos[i * 2] * escala, xj = cx + pontos[j * 2] * escala;
                    cruzamentos.Add(xi + (y - yi) * (xj - xi) / (yj - yi));
                }
                cruzamentos.Sort();
                for (int i = 0; i + 1 < cruzamentos.Count; i += 2)
                    for (int x = Math.Max(0, (int)cruzamentos[i]); x <= Math.Min(Largura - 1, (int)cruzamentos[i + 1]); x++) Pixel(x, y, cor, alfa);
            }
        }

        public void Painel(int x, int y, int w, int h, int cor, int alfa)
        {
            Forma(x, y, 1, new float[] { 24,0, w-24,0, w,24, w,h-24, w-24,h, 24,h, 0,h-24, 0,24 }, 0x010815, 185);
            Linha(x + 24, y, x + w - 24, y, 5, cor, alfa);
            Linha(x + 24, y + h, x + w - 24, y + h, 5, cor, alfa);
            Linha(x, y + 24, x, y + h - 24, 4, cor, alfa);
            Linha(x + w, y + 24, x + w, y + h - 24, 4, cor, alfa);
            Linha(x, y + 24, x + 24, y, 5, cor, alfa * 2);
            Linha(x + w - 24, y + h, x + w, y + h - 24, 5, cor, alfa * 2);
        }

        public void Grade(int x1, int y1, int x2, int y2, int cor, int alfa)
        {
            for (int y = y1; y < y2 - 42; y += 84)
                for (int x = x1 + ((y - y1) / 84 % 2) * 72; x < x2 - 94; x += 144)
                {
                    float[] p = { 24,0, 72,0, 96,42, 72,84, 24,84, 0,42 };
                    for (int i = 0; i < 6; i++)
                    { int j = (i + 1) % 6; Linha(x + p[i * 2], y + p[i * 2 + 1], x + p[j * 2], y + p[j * 2 + 1], 1, cor, alfa); }
                }
        }

        private void Circulo(float cx, float cy, float raio, int cor, int alfa, float borda = 0f)
        {
            float interno = Math.Max(0, raio - borda);
            for (int y = Math.Max(0, (int)(cy - raio)); y <= Math.Min(Altura - 1, (int)(cy + raio)); y++)
                for (int x = Math.Max(0, (int)(cx - raio)); x <= Math.Min(Largura - 1, (int)(cx + raio)); x++)
                {
                    float d = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                    if (d <= raio * raio && (borda <= 0 || d >= interno * interno)) Pixel(x, y, cor, alfa);
                }
        }

        public void Motivo(TipoPokemon tipo, float x, float y, float s, int cor, int alfa)
        {
            switch (tipo)
            {
                case TipoPokemon.Fogo:
                    Forma(x,y,s,new float[] { 0,-1, .2f,-.4f, .5f,-.7f, .7f,.1f, .5f,.7f, 0,1, -.6f,.6f, -.7f,0, -.3f,-.5f, -.25f,.15f },cor,alfa); break;
                case TipoPokemon.Agua:
                    Forma(x,y,s,new float[] { 0,-1, .3f,-.45f, .65f,0, .7f,.45f, .4f,.8f, 0,.95f, -.4f,.8f, -.7f,.45f, -.65f,0, -.3f,-.45f },cor,alfa);
                    Linha(x-.45f*s,y+.3f*s,x+.35f*s,y+.3f*s,s*.07f,0xFFFFFF,alfa/2); break;
                case TipoPokemon.Eletrico:
                    Forma(x,y,s,new float[] { .1f,-1, -.65f,.1f, -.05f,.1f, -.35f,1, .7f,-.25f, .1f,-.25f, .45f,-1 },cor,alfa); break;
                case TipoPokemon.Planta:
                    Forma(x,y,s,new float[] { -.7f,.6f, -.8f,-.2f, -.4f,-.8f, .1f,-.95f, .7f,-1, .7f,-.3f, .4f,.4f, -.1f,.7f },cor,alfa);
                    Linha(x-.7f*s,y+.95f*s,x+.45f*s,y-.7f*s,s*.065f,cor,alfa); break;
                case TipoPokemon.Gelo:
                    for(int i=0;i<6;i++) { double a=i*Math.PI/3; float dx=(float)Math.Cos(a),dy=(float)Math.Sin(a); Linha(x,y,x+dx*s,y+dy*s,s*.09f,cor,alfa); Linha(x+dx*s*.65f,y+dy*s*.65f,x+(dx*.75f-dy*.2f)*s,y+(dy*.75f+dx*.2f)*s,s*.06f,cor,alfa); } break;
                case TipoPokemon.Lutador:
                    Forma(x,y,s,new float[] { -.6f,-.6f, -.2f,-.85f, .5f,-.7f, .75f,-.3f, .6f,.35f, .25f,.45f, .25f,.95f, -.4f,.95f, -.5f,.2f, -.8f,-.1f },cor,alfa); break;
                case TipoPokemon.Venenoso:
                    Circulo(x,y-s*.35f,s*.47f,cor,alfa); Circulo(x-s*.48f,y+s*.2f,s*.4f,cor,alfa); Circulo(x+s*.48f,y+s*.2f,s*.4f,cor,alfa);
                    Linha(x,y+s*.35f,x,y+s*.9f,s*.16f,cor,alfa); break;
                case TipoPokemon.Terra:
                    for(int i=0;i<3;i++) Linha(x-s*(1-i*.2f),y+s*(-.6f+i*.6f),x+s*(1-i*.2f),y+s*(-.6f+i*.6f),s*.18f,cor,alfa); break;
                case TipoPokemon.Voador:
                    Forma(x,y,s,new float[] { -1,-.75f, -.55f,.1f, -.15f,.6f, .05f,.85f, .3f,.25f, .95f,-.7f, .3f,-.3f, .2f,-.75f, -.15f,-.1f },cor,alfa); break;
                case TipoPokemon.Psiquico:
                    float px=x,py=y;
                    for(int i=1;i<=120;i++) { double a=i*.12; float r=s*i/125f; float nx=x+(float)Math.Cos(a)*r,ny=y+(float)Math.Sin(a)*r; Linha(px,py,nx,ny,s*.085f,cor,alfa); px=nx;py=ny; } break;
                case TipoPokemon.Inseto:
                    Circulo(x,y+s*.12f,s*.48f,cor,alfa); Circulo(x,y-s*.5f,s*.3f,cor,alfa);
                    for(int i=0;i<3;i++) { float yy=y+s*(-.35f+i*.4f); Linha(x-s*.3f,yy,x-s*.85f,yy-s*.25f,s*.09f,cor,alfa); Linha(x+s*.3f,yy,x+s*.85f,yy-s*.25f,s*.09f,cor,alfa); } break;
                case TipoPokemon.Pedra:
                    Forma(x,y,s,new float[] { -.9f,.55f, -.65f,-.4f, 0,-.85f, .75f,-.4f, .9f,.6f, .2f,.95f },cor,alfa);
                    Linha(x-s*.6f,y-s*.3f,x+s*.5f,y+s*.4f,s*.06f,0x020918,alfa); break;
                case TipoPokemon.Fantasma:
                    Circulo(x,y-s*.2f,s*.65f,cor,alfa);
                    Forma(x,y,s,new float[] { -.65f,-.2f, .65f,-.2f, .85f,.95f, .3f,.65f, 0,.95f, -.3f,.65f, -.85f,.95f },cor,alfa);
                    Circulo(x-s*.23f,y-s*.12f,s*.1f,0x020918,alfa); Circulo(x+s*.23f,y-s*.12f,s*.1f,0x020918,alfa); break;
                case TipoPokemon.Dragao:
                    Forma(x,y,s,new float[] { -1,-.8f, -.15f,-.25f, .2f,-.95f, .4f,-.3f, .9f,-.6f, .55f,.15f, .1f,.2f, .45f,.8f, -.15f,.95f, -.4f,.3f, -.9f,.3f, -.6f,-.05f },cor,alfa); break;
                case TipoPokemon.Sombrio:
                    Circulo(x,y,s*.87f,cor,alfa); Circulo(x+s*.4f,y-s*.3f,s*.8f,0x020918,alfa); break;
                case TipoPokemon.Aco:
                    Forma(x,y,s,new float[] { -.5f,-.86f, .5f,-.86f, 1,0, .5f,.86f, -.5f,.86f, -1,0 },cor,alfa);
                    Circulo(x,y,s*.45f,0x020918,alfa); break;
                case TipoPokemon.Fada:
                    Forma(x,y,s,new float[] { 0,-1, .25f,-.25f, 1,0, .25f,.25f, 0,1, -.25f,.25f, -1,0, -.25f,-.25f },cor,alfa);
                    Circulo(x+s*.8f,y-s*.8f,s*.1f,cor,alfa); break;
                default:
                    Circulo(x,y,s*.85f,cor,alfa,s*.11f); Circulo(x,y,s*.3f,cor,alfa,s*.08f);
                    Linha(x-s*.82f,y,x-s*.3f,y,s*.09f,cor,alfa); Linha(x+s*.3f,y,x+s*.82f,y,s*.09f,cor,alfa); break;
            }
        }
    }
}
