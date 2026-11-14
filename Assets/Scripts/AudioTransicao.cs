using UnityEngine;

/// <summary>Fonte 2D independente com transições em tempo real, inclusive no pós-jogo pausado.</summary>
public sealed class AudioTransicao : MonoBehaviour
{
    private AudioSource fonte;
    private float inicio, destino, duracao, decorrido, espera;
    private bool transicao;

    public bool EstaTocando { get { return fonte != null && fonte.isPlaying; } }

    public void Tocar(AudioClip clip, float volume, bool repetir, float fadeIn = 0f)
    {
        Parar();
        if (clip == null) return;
        if (fonte == null) fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.spatialBlend = 0f;
        fonte.ignoreListenerPause = true;
        fonte.clip = clip;
        fonte.loop = repetir;
        fonte.volume = fadeIn > 0f ? 0f : Mathf.Clamp01(volume);
        fonte.Play();
        if (fadeIn > 0f) Transicionar(volume, fadeIn, 0f);
    }

    public void Encerrar(float fadeOut, float continuarPor = 0f)
    {
        if (fonte == null || !fonte.isPlaying) return;
        Transicionar(0f, fadeOut, continuarPor);
    }

    private void Transicionar(float volume, float segundos, float atraso)
    {
        inicio = fonte.volume;
        destino = Mathf.Clamp01(volume);
        duracao = Mathf.Max(0.001f, segundos);
        espera = Mathf.Max(0f, atraso);
        decorrido = 0f;
        transicao = true;
    }

    private void Update()
    {
        if (!transicao || fonte == null) return;
        float passo = Time.unscaledDeltaTime;
        if (espera > 0f)
        {
            float consumido = Mathf.Min(espera, passo);
            espera -= consumido;
            passo -= consumido;
        }
        decorrido += passo;
        fonte.volume = Mathf.SmoothStep(inicio, destino, Mathf.Clamp01(decorrido / duracao));
        if (decorrido < duracao) return;
        transicao = false;
        if (destino == 0f) fonte.Stop();
    }

    public void Parar()
    {
        transicao = false;
        if (fonte != null) fonte.Stop();
    }

    private void OnDisable() { Parar(); }
    private void OnDestroy() { if (fonte != null) Destroy(fonte); }
}
