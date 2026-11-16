using System;
using System.Collections.Generic;
using MarbleBasket3D.Core;
using UnityEngine;
using UnityEngine.Events;

namespace MarbleBasket3D
{
    [Serializable] public sealed class MatchFinishedEvent3D : UnityEvent<MatchResult3D> { }
    [DefaultExecutionOrder(-200)]
    public sealed class MarbleMatch3D : MonoBehaviour
    {
        public ArenaSettings3D Settings { get; private set; }
        public IReadOnlyList<Marble3D> Marbles => marbles;
        public MatchRules3D Rules { get; } = new MatchRules3D();
        public bool Active => Rules.Active;
        public float SimulationTime { get; private set; }
        public Launcher3D Launcher { get; private set; }
        public ReturnSystem3D Returns { get; private set; }
        public ArenaSignals3D Signals { get; private set; }
        public MatchFinishedEvent3D onFinished = new MatchFinishedEvent3D();
        public event Action<MatchResult3D> Finished;
        private readonly List<Marble3D> marbles = new List<Marble3D>();
        private RotatingCross3D obstacle;
        private bool configured, released, resultSent;
        private readonly Dictionary<Marble3D, int> awardedAttempts = new Dictionary<Marble3D, int>();
        private float pausedScale = 1;
        public void Configure(ArenaSettings3D settings, Launcher3D launcher, ReturnSystem3D returns, RotatingCross3D beam, ArenaSignals3D signals)
        {
            Settings = settings; Launcher = launcher; Returns = returns; obstacle = beam; Signals = signals;
            Settings.Validate(); configured = true;
        }
        public void Register(Marble3D marble)
        {
            if (marble == null || marble.Match != this || marbles.Contains(marble) || marbles.Exists(b => b.Team == marble.Team && b.Number == marble.Number) || marbles.Count >= Settings.marblesPerTeam * 2) throw new InvalidOperationException("Bolinha duplicada ou ausente.");
            marbles.Add(marble);
        }
        private void Start() { if (configured) { ResetMatch(); if (Settings.autoStart) StartMatch(); } }
        public void StartMatch()
        {
            if (!configured) return;
            if (Rules.Phase == MatchPhase.Finished) ResetMatch();
            Rules.Start();
        }
        public void RestartMatch() { if (!configured) return; ResetMatch(); StartMatch(); }
        public void ResetMatch()
        {
            RestorePause(); Rules.Reset(Settings.matchSeconds, Settings.countdownSeconds);
            SimulationTime = 0; released = resultSent = false; awardedAttempts.Clear();
            Launcher.ResetMechanism(); Returns.ResetSystem(); obstacle.ResetMechanism(); Signals.ClearEffects();
            foreach (var ball in marbles) if (ball != null) ball.ResetHome();
            foreach (var sensor in GetComponentsInChildren<BasketSensor3D>()) sensor.ResetSensor(Rules.MatchNumber);
            var recovery = GetComponent<Recovery3D>(); if (recovery != null) recovery.ResetCounters(Rules.MatchNumber);
            var diagnostics = GetComponent<ArenaDiagnostics3D>(); if (diagnostics != null) diagnostics.ResetCounters();
        }
        public void TogglePause()
        {
            if (Rules.Phase == MatchPhase.Paused) { Rules.Resume(); RestorePause(); }
            else if (Rules.Phase == MatchPhase.Countdown || Rules.Active)
            {
                pausedScale = Time.timeScale > 0 ? Time.timeScale : 1; Rules.Pause(); Time.timeScale = 0;
            }
        }
        private void RestorePause() { if (Time.timeScale == 0) Time.timeScale = pausedScale; }
        private void FixedUpdate()
        {
            if (!configured || Rules.Phase == MatchPhase.Paused || Rules.Phase == MatchPhase.Finished || Rules.Phase == MatchPhase.Ready) return;
            SimulationTime += Time.fixedDeltaTime;
            Rules.Tick(Time.fixedDeltaTime);
            if (Rules.Active && !released)
            {
                released = true; obstacle.Release();
                // Every body is enabled on this same fixed step before any physics simulation.
                foreach (var ball in marbles) if (ball != null) ball.Release(MarbleState3D.Contest, SimulationTime);
                foreach (var ball in marbles) if (ball != null) ball.Body.WakeUp();
                Signals.ReleaseTeams();
            }
            DispatchResult();
        }
        public bool TryAward(Marble3D ball) => TryAwardBatch(new[] { ball }) > 0;
        public int TryAwardBatch(IReadOnlyList<Marble3D> simultaneous)
        {
            if (!Active) return 0;
            var accepted = new List<Marble3D>(); var unique = new HashSet<Marble3D>();
            int red = 0, blue = 0;
            foreach (var ball in simultaneous)
            {
                if (ball == null || ball.Match != this || !marbles.Contains(ball) || ball.State != MarbleState3D.InFlight || !ball.Shot.Scored || !unique.Add(ball)) continue;
                if (awardedAttempts.TryGetValue(ball, out int attempt) && attempt >= ball.Shot.Attempt) continue;
                accepted.Add(ball); if (ball.Team == TeamId.Red) red++; else blue++;
            }
            if (!Rules.ScoreBatch(red, blue)) return 0;
            foreach (var ball in accepted) awardedAttempts[ball] = ball.Shot.Attempt;
            foreach (var ball in accepted) Signals.Basket(ball);
            DispatchResult(); return accepted.Count;
        }
        private void DispatchResult()
        {
            if (Rules.Result == null || resultSent) return;
            MatchResult3D snapshot = Rules.Result;
            resultSent = true; Launcher.StopMechanism(); Returns.StopSystem(); obstacle.Freeze();
            foreach (var ball in marbles) if (ball != null) ball.SetKinematic(true);
            Signals.Finish(snapshot);
            // A finished snapshot is emitted once; a restart creates a new result object.
            onFinished.Invoke(snapshot); Finished?.Invoke(snapshot);
        }
        private void OnDisable() { if (Rules.Phase == MatchPhase.Paused) RestorePause(); }
    }
}
