using System;
using System.Collections.Generic;
using MarbleBasket3D.Core;
using UnityEngine;

namespace MarbleBasket3D
{
    public enum ArenaEventKind3D { Release, Contact, Loaded, Launch, Basket, Fall, SecondChance, Finish }
    public struct ArenaEvent3D
    {
        public ArenaEventKind3D kind;
        public Marble3D marble;
        public string message;
    }
    public sealed class ArenaSignals3D : MonoBehaviour
    {
        public event Action<ArenaEvent3D> Emitted;
        public string Message { get; private set; } = "PRONTO PARA COMEÇAR";
        public Marble3D Featured { get; private set; }
        public float Volume = .5f;
        private MarbleMatch3D match;
        private AudioSource audioSource;
        private AudioClip contact, launch, basket, fall, finish;
        private readonly List<AudioClip> generated = new List<AudioClip>();
        private readonly List<GameObject> effects = new List<GameObject>();
        private float nextContact;
        public void Configure(MarbleMatch3D owner)
        {
            match = owner; audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false; audioSource.spatialBlend = 0;
            var s = match.Settings;
            contact = s.collisionSound != null ? s.collisionSound : Tone("Contact", 900, .055f, 1);
            launch = s.launchSound != null ? s.launchSound : Tone("Launch", 180, .18f, 2.5f);
            basket = s.basketSound != null ? s.basketSound : Tone("Basket", 660, .32f, 1.5f);
            fall = s.fallSound != null ? s.fallSound : Tone("Fall", 220, .20f, .5f);
            finish = s.finishSound != null ? s.finishSound : Tone("Finish", 440, .55f, 1.1f);
        }
        private AudioClip Tone(string label, float hz, float seconds, float sweep)
        {
            int count = Mathf.CeilToInt(22050 * seconds); var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / 22050, p = t / seconds;
                samples[i] = Mathf.Sin(2 * Mathf.PI * hz * (t + (sweep - 1) * t * p * .5f)) * Mathf.Sin(Mathf.PI * p) * Mathf.Exp(-p * 3) * .35f;
            }
            AudioClip clip = AudioClip.Create(label + " procedural", count, 1, 22050, false); clip.SetData(samples, 0); generated.Add(clip); return clip;
        }
        private void Play(AudioClip clip, float volume = 1) { if (audioSource != null && clip != null) audioSource.PlayOneShot(clip, Volume * volume); }
        private void Emit(ArenaEventKind3D kind, Marble3D b, string message)
        {
            Message = message; Featured = b; Emitted?.Invoke(new ArenaEvent3D { kind = kind, marble = b, message = message });
        }
        private string Team(Marble3D b) => b.Team == TeamId.Red ? match.Settings.redName : match.Settings.blueName;
        public void ReleaseTeams() { Emit(ArenaEventKind3D.Release, null, "DISPUTA ABERTA"); }
        public void Contact(Marble3D b, float strength)
        {
            if (match.SimulationTime < nextContact) return;
            nextContact = match.SimulationTime + .06f; Play(contact, strength * .5f);
            Emitted?.Invoke(new ArenaEvent3D { kind = ArenaEventKind3D.Contact, marble = b });
        }
        public void Loaded(Marble3D b) { Emit(ArenaEventKind3D.Loaded, b, Team(b) + " · BOLA " + b.Number + " NO LANÇADOR"); }
        public void Launch(Marble3D b) { Emit(ArenaEventKind3D.Launch, b, Team(b) + " ARREMESSOU"); Play(launch); Flash(b.Body.position, new Color(1, .7f, .2f), .4f); }
        public void Basket(Marble3D b) { Emit(ArenaEventKind3D.Basket, b, "CESTA! " + Team(b) + " +2"); Play(basket); Flash(new Vector3(5.4f, 3.7f, -2.3f), new Color(1, .8f, .25f), .9f); }
        public void Fall(Marble3D b, string reason)
        {
            Emit(ArenaEventKind3D.Fall, b, Team(b) + " · " + reason);
            if (reason == "Queda lateral" || reason == "Fora da arena") { Play(fall, .6f); Flash(b.Body.position, new Color(.35f, .7f, 1), .5f); }
        }
        public void SecondChance(Marble3D b) { Emit(ArenaEventKind3D.SecondChance, b, Team(b) + " · SEGUNDA CHANCE PELA FILA"); }
        public void Finish(MatchResult3D result) { Emit(ArenaEventKind3D.Finish, null, "VENCEU: " + (result.winner == TeamId.Red ? match.Settings.redName : match.Settings.blueName)); Play(finish); }
        private void Flash(Vector3 position, Color color, float seconds)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = "Short arena effect";
            go.transform.SetParent(transform, false); go.transform.position = position; go.transform.localScale = Vector3.one * .15f;
            var collider = go.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            var material = new Material(Shader.Find("Standard")); material.color = color; material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 1.2f);
            go.GetComponent<Renderer>().sharedMaterial = material;
            var effect = go.AddComponent<ArenaFlash3D>(); effect.Setup(seconds, material); effects.Add(go);
        }
        public void ClearEffects()
        {
            foreach (var go in effects) if (go != null) Destroy(go); effects.Clear();
            if (audioSource != null) audioSource.Stop(); Message = "PRONTO PARA COMEÇAR"; Featured = null; nextContact = 0;
        }
        private void OnDestroy() { foreach (var clip in generated) if (clip != null) Destroy(clip); }
    }
}
