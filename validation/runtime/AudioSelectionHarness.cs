using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FutebolDeDiscos;
using UnityEngine;

// Verifies dispatch to the actual selected AudioClip references. The facade does
// not decode audio, render a waveform, or model a native Unity audio device.
public static class AudioSelectionHarness
{
    private static int checks;
    private static string scenario;

    private static void Check(bool value, string message)
    {
        checks++;
        if (!value) throw new Exception(scenario + ": " + message);
    }

    private static void Call(object value, string name)
    {
        MethodInfo method = value.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null) throw new Exception("Missing " + name);
        try { method.Invoke(value, null); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }

    private static void Clean()
    {
        foreach (GameObject item in GameObject.All.Where(g => !g.destroyed && g.transform.parent == null).ToArray())
            UnityEngine.Object.Destroy(item);
        GameObject.All.Clear(); Scheduler.Reset();
        Debug.Errors.Clear(); Debug.Warnings.Clear(); Debug.Messages.Clear();
        Time.captureDeltaTime = 0f; Time.timeScale = 1f;
    }

    private static AudioClip Clip(string name) { return new AudioClip { name = name }; }
    private static TeamData Team(string name)
    {
        return new TeamData { name = name, nomeEquipe = name,
            elenco = new List<JogadorDados> { new JogadorDados { nomeJogador = name + " player" } } };
    }

    private sealed class Match
    {
        public readonly GameObject Root;
        public readonly ProjetoFutebolDiscos Config;
        public readonly UniversalUI Ui;
        public readonly AudioSource Ambient, Explicit;
        public readonly AudioClip Old = Clip("Old source or opening clip");
        public readonly AudioClip Start = Clip("New selected start whistle");
        public readonly AudioClip Goal = Clip("New selected goal effect");
        public readonly AudioClip End = Clip("New selected final whistle");
        public readonly TeamData Home = Team("Home"), Away = Team("Away");

        public Match(bool knockout = false, bool explicitSource = false, bool silent = false)
        {
            Root = new GameObject("Audio choice regression fixture"); Root.SetActive(false);
            Ambient = Root.AddComponent<AudioSource>();
            Ambient.clip = Old; Ambient.playOnAwake = Ambient.loop = true;
            Ambient.spatialBlend = .7f; Ambient.Play();
            Config = Root.AddComponent<ProjetoFutebolDiscos>();
            Config.formato = knockout ? FormatoDiscos.MataMata : FormatoDiscos.Liga;
            Config.tema = ScriptableObject.CreateInstance<TemaCompeticao>();
            Config.tema.somAbertura = Old;
            Ui = Root.AddComponent<UniversalUI>();
            if (explicitSource)
            {
                Explicit = Root.AddComponent<AudioSource>();
                Explicit.clip = Old; Explicit.playOnAwake = Explicit.loop = true;
                Explicit.spatialBlend = .8f; Explicit.Play(); Ui.fonteDeAudio = Explicit;
            }
            Ui.somGol = silent ? null : Goal;
            Ui.somApitoInicial = silent ? null : Start;
            Ui.somApitoFinal = silent ? null : End;
            Ui.volumeSons = .37f; Ui.ativarGoldenGoal = knockout;
            Ui.duracaoRealEmSegundos = .2f; Ui.tempoCongeladoPosJogo = 0f;
            Root.SetActive(true);
            Prepare();
        }

        public void Prepare()
        {
            Ui.CancelarPartida();
            Ui.ConfigurarPartida(Home, Away, null, Home.ObterTitularesDaPartida(), Away.ObterTitularesDaPartida());
        }
        public void StartMatch() { Ui.IniciarCronometro(); }
        public void FinishClock()
        {
            Time.captureDeltaTime = .3f; Call(Ui, "Update"); Time.captureDeltaTime = 0f;
        }
    }

    private static void NewSelectedEffectsAndExclusiveSource()
    {
        foreach (bool knockout in new[] { false, true })
        {
            scenario = "selected effects and exclusive source, knockout=" + knockout;
            var match = new Match(knockout);
            AudioSource source = match.Ui.fonteDeAudio;
            Check(source != null && source != match.Ambient, "an empty source field creates exclusive effects rather than reusing the first source");
            Check(source.clip == null && !source.playOnAwake && !source.loop && source.spatialBlend == 0f, "effects source has no embedded old clip, autoplay, loop or spatial attenuation");
            Check(match.Ambient.clip == match.Old && match.Ambient.isPlaying && match.Ambient.loop && match.Ambient.stopCalls == 0,
                "independent existing audio is neither stopped nor reconfigured");
            match.StartMatch();
            Check(source.oneShots.Count == 1 && source.oneShots[0] == match.Start, "kickoff dispatches the selected start whistle");
            int preparations = source.stopCalls;
            match.Ui.RegistrarGol(true, 0f);
            Check(source.oneShots.Count == 2 && source.oneShots[1] == match.Goal, "goal dispatches selected new goal effect");
            Check(source.stopCalls == preparations, "a subsequent effect does not restart or stop the prepared source");
            match.FinishClock();
            Check(!match.Ui.partidaEmAndamento && source.oneShots.Count == 3 && source.oneShots[2] == match.End,
                "regulation end dispatches selected final whistle");
            Check(source.oneShots.All(clip => clip != match.Old), "old embedded source/opening audio never becomes a match effect fallback");
            Check(source.oneShotVolumes.All(volume => Math.Abs(volume - .37f) < .0001f), "each event uses selected effects volume");
            Check(match.Ui.somGol == match.Goal && match.Ui.somApitoInicial == match.Start && match.Ui.somApitoFinal == match.End,
                "match configuration and completion preserve selected serialized references");
            Check(match.Ambient.oneShots.Count == 0, "gameplay never dispatches through unrelated existing audio");
            Clean();
        }
    }

    private static void ExplicitSourceAndReconfiguration()
    {
        scenario = "explicit effects source with an old clip already playing";
        var match = new Match(explicitSource: true);
        AudioSource source = match.Explicit;
        Check(match.Ui.fonteDeAudio == source, "explicit source assignment is honored");
        Check(source.stopCalls >= 1 && !source.isPlaying && source.clip == null,
            "preparing explicit effects stops the old playback and clears its embedded clip");
        Check(!source.playOnAwake && !source.loop && source.spatialBlend == 0f, "explicit source is configured for one-shot two-dimensional effects");
        match.StartMatch();
        Check(source.oneShots.Single() == match.Start, "explicit source dispatches selected kickoff clip");

        var replacement = match.Root.AddComponent<AudioSource>(); replacement.clip = match.Old;
        replacement.playOnAwake = replacement.loop = true; replacement.spatialBlend = 1f; replacement.Play();
        match.Ui.fonteDeAudio = replacement;
        match.Ui.RegistrarGol(true, 0f);
        Check(replacement.oneShots.Single() == match.Goal, "changing the assigned source is honored on the next effect");
        Check(replacement.stopCalls == 1 && replacement.clip == null && !replacement.loop && !replacement.playOnAwake && replacement.spatialBlend == 0f,
            "newly assigned source is fully prepared before dispatch instead of keeping its old playback");
        Check(source.oneShots.Count == 1, "changed assignment does not continue using the cached old effects source");

        UnityEngine.Object.Destroy(replacement);
        match.Ui.RegistrarGol(false, 0f);
        AudioSource recreated = match.Ui.fonteDeAudio;
        Check(recreated != null && recreated != replacement && recreated != match.Ambient,
            "destroyed source is replaced with a new isolated effects source");
        Check(recreated.oneShots.Single() == match.Goal, "the effect is dispatched after recreating the destroyed source");
        match.Ui.fonteDeAudio = null;
        match.Ui.RegistrarGol(true, 0f);
        Check(match.Ui.fonteDeAudio != null && match.Ui.fonteDeAudio != match.Ambient && match.Ui.fonteDeAudio.oneShots.Last() == match.Goal,
            "clearing a source assignment also returns to an isolated automatic source");
        Check(match.Ambient.clip == match.Old && match.Ambient.isPlaying && match.Ambient.stopCalls == 0, "source replacement never mutates independent audio");
        Clean();
    }

    private static void ChangedSelectionsNextMatchAndNullEffects()
    {
        scenario = "changed selections survive the next match";
        var match = new Match(); match.StartMatch();
        AudioSource source = match.Ui.fonteDeAudio;
        AudioClip nextStart = Clip("Second match start"), nextGoal = Clip("Second match goal"), nextEnd = Clip("Second match final");
        match.Ui.somGol = nextGoal; match.Ui.somApitoInicial = nextStart; match.Ui.somApitoFinal = nextEnd;
        match.Prepare(); match.StartMatch(); match.Ui.RegistrarGol(true, 0f); match.FinishClock();
        Check(source.oneShots.Skip(1).SequenceEqual(new[] { nextStart, nextGoal, nextEnd }),
            "a new match uses all three changed selections rather than retaining the previous set");
        Check(match.Ui.somGol == nextGoal && match.Ui.somApitoInicial == nextStart && match.Ui.somApitoFinal == nextEnd,
            "new match preparation does not overwrite changed audio references");
        Clean();

        scenario = "empty clip fields intentionally mute events";
        match = new Match(silent: true); source = match.Ui.fonteDeAudio;
        match.StartMatch(); match.Ui.RegistrarGol(true, 0f); match.FinishClock();
        Check(source.oneShots.Count == 0, "empty start, goal and final clips play no sound and do not fall back to old clips");
        Check(match.Ambient.oneShots.Count == 0 && match.Ambient.clip == match.Old && match.Ambient.stopCalls == 0,
            "muted effects do not steal existing ambient audio");
        Clean();
    }

    private static void GoldenGoalOrderingAndVolume()
    {
        scenario = "golden goal uses chosen goal and final effects exactly once";
        var match = new Match(knockout: true); match.StartMatch();
        AudioSource source = match.Ui.fonteDeAudio;
        match.FinishClock();
        Check(match.Ui.EmProrrogacao && match.Ui.partidaEmAndamento && source.oneShots.SequenceEqual(new[] { match.Start }),
            "entering golden goal does not play a premature final whistle");
        match.Ui.RegistrarGol(false, .4f);
        Check(source.oneShots.SequenceEqual(new[] { match.Start, match.Goal, match.End }) && !match.Ui.partidaEmAndamento,
            "golden goal dispatches selected goal then final whistle and ends normally");
        match.Ui.RegistrarGol(false, .4f); match.Ui.IniciarCronometro();
        Check(source.oneShots.Count == 3, "late callbacks or a pending restart do not duplicate effects after completion");
        Clean();

        scenario = "effect volume is bounded";
        match = new Match(); source = match.Ui.fonteDeAudio;
        match.Ui.volumeSons = 1.6f; match.StartMatch();
        match.Ui.volumeSons = -.4f; match.Ui.RegistrarGol(true, 0f);
        Check(source.oneShotVolumes.SequenceEqual(new[] { 1f, 0f }), "one-shot volume uses the configured value clamped to the supported range");
        Clean();
    }

    private static void CancellationAndComponentLifecycle()
    {
        scenario = "cancellation and disabling stop only match effects";
        var match = new Match(); match.StartMatch();
        AudioSource effects = match.Ui.fonteDeAudio;
        int priorStops = effects.stopCalls;
        match.Ui.CancelarPartida();
        Check(!effects.isPlaying && effects.stopCalls > priorStops && !match.Ui.partidaEmAndamento,
            "cancelling the match stops already playing gameplay effects");
        Check(match.Ambient.isPlaying && match.Ambient.stopCalls == 0,
            "match cancellation preserves independently playing background audio");
        match.Prepare(); match.StartMatch();
        Check(effects.oneShots.Count == 2 && effects.oneShots.Last() == match.Start,
            "next start after cancellation can reuse its exclusive source with the current selection");
        match.Ui.enabled = false;
        Check(!effects.isPlaying && match.Ambient.isPlaying,
            "disabling the component stops its effects without stopping background audio");
        UnityEngine.Object.Destroy(match.Ui);
        Check(effects == null, "destroying the UI disposes the automatically owned effects source");
        Check(match.Ambient != null && match.Ambient.isPlaying && match.Ambient.stopCalls == 0,
            "destroying the UI never disposes or stops an independent source");
        Clean();
    }

    public static void Main()
    {
        string folder = Path.Combine(Path.GetTempPath(), "disc-audio-selection-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder); Application.persistentDataPath = folder;
        try
        {
            Clean(); NewSelectedEffectsAndExclusiveSource(); ExplicitSourceAndReconfiguration();
            ChangedSelectionsNextMatchAndNullEffects(); GoldenGoalOrderingAndVolume(); CancellationAndComponentLifecycle();
            Console.WriteLine("PASS " + checks + " selected-audio dispatch and exclusive-source regression assertions using actual game sources.");
            Console.WriteLine("AudioClip identity and source lifecycle were observed through a facade; native Unity audio output was not executed.");
        }
        finally { Clean(); Directory.Delete(folder, true); }
    }
}
