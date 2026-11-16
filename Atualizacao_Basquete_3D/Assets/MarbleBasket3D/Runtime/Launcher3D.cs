using System.Collections.Generic;
using UnityEngine;

namespace MarbleBasket3D
{
    public enum LauncherPhase3D { Open, Preparing, Cooling, Stopped }
    [DefaultExecutionOrder(-100)]
    public sealed class Launcher3D : MonoBehaviour
    {
        public LauncherPhase3D Phase { get; private set; } = LauncherPhase3D.Stopped;
        public Marble3D Occupant { get; private set; }
        public float CurrentAngle { get; private set; }
        public float CurrentSpeed { get; private set; }
        public float LastProgress { get; private set; }
        public int ShotsFired { get; private set; }
        public Vector3 seat, muzzle, target;
        public Transform barrel, gateVisual;
        public Collider gate;
        private MarbleMatch3D match;
        private float phaseStarted;
        private readonly HashSet<Marble3D> candidates = new HashSet<Marble3D>();
        public void Configure(MarbleMatch3D owner) { match = owner; }
        public void Enter(Marble3D ball) { if (ball != null) candidates.Add(ball); }
        public void Exit(Marble3D ball) { if (ball != null) candidates.Remove(ball); }
        public void ResetMechanism()
        {
            Occupant = null; candidates.Clear(); ShotsFired = 0; LastProgress = phaseStarted = 0;
            Phase = LauncherPhase3D.Open; SetGate(false);
        }
        public void StopMechanism() { Phase = LauncherPhase3D.Stopped; SetGate(true); }
        public bool QueueIsHealthy(float now) => Phase != LauncherPhase3D.Stopped && now - LastProgress < match.Settings.mechanismTimeout;
        private void FixedUpdate()
        {
            if (match == null || !match.Active) return;
            var s = match.Settings; float now = match.SimulationTime;
            // The visible continuous cycle is independent of team, score, arrival order and identity.
            float cycle = now * Mathf.PI * 2 / s.powerCycleSeconds;
            CurrentAngle = s.launchAngle + Mathf.Sin(cycle) * s.angleVariation;
            CurrentSpeed = s.launchSpeed + Mathf.Sin(cycle + 1.1f) * s.speedVariation;
            Vector3 horizontal = target - muzzle; horizontal.y = 0; horizontal.Normalize();
            Vector3 direction = horizontal * Mathf.Cos(CurrentAngle * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(CurrentAngle * Mathf.Deg2Rad);
            if (barrel != null) barrel.rotation = Quaternion.LookRotation(direction);
            if (Phase == LauncherPhase3D.Open)
            {
                // Physical proximity chooses the front ball; triggers do not award priority by callback/creation order.
                Marble3D selected = null; float best = float.PositiveInfinity; bool tied = false;
                foreach (var b in candidates)
                {
                    if (b == null || (b.State != MarbleState3D.Contest && b.State != MarbleState3D.Queued)) continue;
                    float d = (b.Body.position - seat).sqrMagnitude;
                    if (d > .75f * .75f) continue; // Ignore stale trigger entries after a return/disabled collider.
                    if (Mathf.Abs(d - best) < .000001f) { tied = true; continue; }
                    if (d > best) continue;
                    best = d; selected = b; tied = false;
                }
                if (selected != null && !tied)
                {
                    Occupant = selected; candidates.Remove(selected);
                    selected.Seat(seat, now); Phase = LauncherPhase3D.Preparing; phaseStarted = LastProgress = now; SetGate(true);
                    match.Signals.Loaded(selected);
                }
            }
            else if (Phase == LauncherPhase3D.Preparing)
            {
                float fraction = Mathf.Clamp01((now - phaseStarted) / s.preparationSeconds);
                // Short visible seating motion along the open cup, never a jump across the arena.
                if (Occupant != null) Occupant.Body.position = Vector3.Lerp(seat, muzzle, fraction * .65f);
                if (now - phaseStarted >= s.preparationSeconds)
                {
                    if (Occupant != null)
                    {
                        Occupant.Launch(muzzle, direction * CurrentSpeed, now); ShotsFired++; match.Signals.Launch(Occupant);
                    }
                    Occupant = null; Phase = LauncherPhase3D.Cooling; phaseStarted = LastProgress = now;
                }
            }
            else if (Phase == LauncherPhase3D.Cooling && now - phaseStarted >= s.launchInterval)
            {
                bool airborne = false;
                if (s.waitForShotToLand) foreach (var b in match.Marbles) if (b.State == MarbleState3D.InFlight) { airborne = true; break; }
                if (!airborne) { Phase = LauncherPhase3D.Open; LastProgress = now; SetGate(false); }
            }
            if (Phase == LauncherPhase3D.Preparing && now - phaseStarted > s.mechanismTimeout)
            {
                if (Occupant != null) match.Returns.ReturnHome(Occupant, "Recuperação do lançador");
                Occupant = null; Phase = LauncherPhase3D.Open; SetGate(false); LastProgress = now;
            }
        }
        private void SetGate(bool closed)
        {
            if (gate != null) gate.enabled = closed;
            if (gateVisual != null) gateVisual.localPosition = new Vector3(0, closed ? 0 : .65f, 0);
        }
    }
}
