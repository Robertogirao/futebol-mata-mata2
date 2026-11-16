using System.Collections.Generic;
using MarbleBasket3D.Core;
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
        public float ReferenceSpeed { get; private set; }
        public float LastProgress { get; private set; }
        public int ShotsFired { get; private set; }
        public bool Striking => (striking && Phase == LauncherPhase3D.Preparing)
            || (Phase == LauncherPhase3D.Cooling && match != null
                && (match.Settings.compactLauncher || match.SimulationTime - lastReleaseTime <= Time.fixedDeltaTime * 2));
        public Vector3 seat, muzzle, target;
        public Transform barrel;
        public Rigidbody leverBody;
        public const float ArmLength = 1.05f;
        public float EffectiveArmLength => match != null && match.Settings.compactLauncher ? match.Settings.launcherWheelRadius : ArmLength;
        public int ActiveBladeIndex { get; private set; }
        public const float RestAngle = -74f;
        private const float DrawbackAngle = -86f;
        private MarbleMatch3D match;
        private float phaseStarted, strikeStarted, strikeSpeed, strikeAngle, strikeYaw;
        private bool striking;
        private float lastReleaseTime = -100;
        private Vector3 pivot, horizontal;
        private readonly HashSet<Marble3D> candidates = new HashSet<Marble3D>();

        public void Configure(MarbleMatch3D owner) { match = owner; }
        public void ConfigureLever(Rigidbody body)
        {
            leverBody = body; barrel = body.transform;
            horizontal = target - seat; horizontal.y = 0; horizontal.Normalize();
            float rest = RestAngle * Mathf.Deg2Rad, offset = match.Settings.marbleRadius + .07f;
            ActiveBladeIndex = 0;
            pivot = seat - horizontal * (EffectiveArmLength * Mathf.Cos(rest) - Mathf.Sin(rest) * offset)
                - Vector3.up * (EffectiveArmLength * Mathf.Sin(rest) + Mathf.Cos(rest) * offset);
            leverBody.position = pivot;
            Vector3 release = TipAt(match.Settings.launchAngle - 90, 0);
            Vector3 distance = target - release; distance.y = 0;
            ReferenceSpeed = match.Settings.calibrateLaunchSpeed
                ? (float)LaunchMath3D.RequiredSpeed(distance.magnitude, target.y - release.y, match.Settings.launchAngle, -Physics.gravity.y)
                    + (match.Settings.launchSpeed - 8.65f)
                : match.Settings.launchSpeed;
            muzzle = release; leverBody.rotation = Rotation(RestAngle, 0);
        }
        private Quaternion Rotation(float armAngle, float yaw)
        {
            Vector3 heading = Quaternion.Euler(0, yaw, 0) * horizontal;
            return Quaternion.LookRotation(heading) * Quaternion.Euler(-armAngle, 0, 0);
        }
        public Vector3 TipAt(float armAngle, float yaw)
        {
            Quaternion rotation = Rotation(armAngle, yaw);
            return pivot + rotation * Vector3.forward * EffectiveArmLength + rotation * Vector3.up * (match.Settings.marbleRadius + .07f);
        }
        private Quaternion BodyRotation(float armAngle, float yaw) => Rotation(armAngle - ActiveBladeIndex * 90, yaw);
        private void Pose(float armAngle, float yaw)
        {
            if (leverBody != null) leverBody.MoveRotation(BodyRotation(armAngle, yaw));
        }
        public void Enter(Marble3D ball) { if (ball != null) candidates.Add(ball); }
        public void Exit(Marble3D ball) { if (ball != null) candidates.Remove(ball); }
        public void ResetMechanism()
        {
            Occupant = null; candidates.Clear(); ShotsFired = 0; LastProgress = phaseStarted = 0; striking = false;
            ActiveBladeIndex = 0; Phase = LauncherPhase3D.Open; lastReleaseTime = -100;
            if (leverBody != null) leverBody.rotation = BodyRotation(RestAngle, 0);
        }
        public void StopMechanism() { Phase = LauncherPhase3D.Stopped; striking = false; }
        public bool QueueIsHealthy(float now) => Phase != LauncherPhase3D.Stopped && now - LastProgress < match.Settings.mechanismTimeout;
        private void FixedUpdate()
        {
            if (match == null || !match.Active) return;
            var s = match.Settings; float now = match.SimulationTime;
            float cycle = now * Mathf.PI * 2 / s.powerCycleSeconds;
            CurrentAngle = s.launchAngle + Mathf.Sin(cycle) * s.angleVariation;
            CurrentSpeed = ReferenceSpeed + Mathf.Sin(cycle + 1.1f) * s.speedVariation;
            if (Phase == LauncherPhase3D.Open)
            {
                Pose(RestAngle, 0);
                Marble3D selected = null; float best = float.PositiveInfinity; bool tied = false;
                foreach (var b in candidates)
                {
                    if (b == null || (b.State != MarbleState3D.Contest && b.State != MarbleState3D.Queued)) continue;
                    float d = (b.Body.position - seat).sqrMagnitude;
                    if (d > .75f * .75f) continue;
                    if (Mathf.Abs(d - best) < .000001f) { tied = true; continue; }
                    if (d > best) continue;
                    best = d; selected = b; tied = false;
                }
                if (selected != null && !tied)
                {
                    Occupant = selected; candidates.Remove(selected); selected.Seat(TipAt(RestAngle, 0), now);
                    Phase = LauncherPhase3D.Preparing; phaseStarted = LastProgress = now; striking = false;
                    match.Signals.Loaded(selected);
                }
            }
            else if (Phase == LauncherPhase3D.Preparing)
            {
                if (!striking)
                {
                    float fraction = Mathf.Clamp01((now - phaseStarted) / s.preparationSeconds);
                    float armAngle = s.compactLauncher ? RestAngle : RestAngle + (DrawbackAngle - RestAngle) * fraction;
                    Pose(armAngle, 0);
                    if (Occupant != null) Occupant.Body.position = TipAt(armAngle, 0);
                    if (fraction >= 1)
                    {
                        striking = true; strikeStarted = now; strikeAngle = CurrentAngle;
                        strikeSpeed = CurrentSpeed; strikeYaw = Mathf.Sin(cycle * .73f) * s.launchYawVariation;
                    }
                }
                else
                {
                    // Tangent speed = angular speed * paddle radius. A short powered stroke
                    // produces the same tangent direction that the ball receives at release.
                    float releaseArmAngle = strikeAngle - 90;
                    float startAngle = s.compactLauncher ? RestAngle : DrawbackAngle;
                    float angle = Mathf.Min(releaseArmAngle, startAngle + (now - strikeStarted) * strikeSpeed / EffectiveArmLength * Mathf.Rad2Deg);
                    Pose(angle, strikeYaw);
                    if (Occupant != null) Occupant.Body.position = TipAt(angle, strikeYaw);
                    if (angle >= releaseArmAngle)
                    {
                        muzzle = TipAt(releaseArmAngle, strikeYaw);
                        Vector3 direction = Rotation(releaseArmAngle, strikeYaw) * Vector3.up;
                        if (Occupant != null)
                        {
                            Occupant.Launch(muzzle, direction * strikeSpeed, now); RecordLaunch(Occupant);
                        }
                        Occupant = null; Phase = LauncherPhase3D.Cooling; phaseStarted = LastProgress = lastReleaseTime = now; striking = false;
                    }
                }
                if (Occupant == null && Phase == LauncherPhase3D.Preparing) BeginCooling(now);
            }
            else if (Phase == LauncherPhase3D.Cooling)
            {
                float fraction = Mathf.Clamp01((now - phaseStarted) / s.launchInterval);
                float endAngle = RestAngle + (s.compactLauncher ? 90 : 0);
                Pose((strikeAngle - 90) + (endAngle - strikeAngle + 90) * fraction, strikeYaw * (1 - fraction));
                if (fraction >= 1)
                {
                    // Advance one quarter-turn in the same direction. The following paddle
                    // then occupies the same loading seat without reversing the wheel.
                    if (s.compactLauncher) ActiveBladeIndex = (ActiveBladeIndex + 3) % 4;
                    Phase = LauncherPhase3D.Open; LastProgress = now;
                }
            }
            if (Phase == LauncherPhase3D.Preparing && now - phaseStarted > s.mechanismTimeout)
            {
                if (Occupant != null) match.Returns.ReturnHome(Occupant, "Recuperação do lançador");
                Occupant = null; BeginCooling(now);
            }
        }
        private void BeginCooling(float now) { Phase = LauncherPhase3D.Cooling; striking = false; phaseStarted = LastProgress = now; }
        private void RecordLaunch(Marble3D ball) { ShotsFired++; LastProgress = match.SimulationTime; match.Signals.Launch(ball); }
        // Balls hit accidentally by the moving paddle keep their actual collision velocity.
        // Their new attempt is eligible for scoring as well; no target correction is applied.
        public void RegisterPhysicalHit(Marble3D ball)
        {
            if (match == null || !match.Active || !Striking || ball == null || ball.Match != match) return;
            if (ball.State != MarbleState3D.Contest && ball.State != MarbleState3D.Queued) return;
            ball.MarkPhysicalLaunch(match.SimulationTime); candidates.Remove(ball); RecordLaunch(ball);
        }
    }
}
