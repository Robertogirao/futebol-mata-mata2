using System;

namespace MarbleBasket3D.Core
{
    public enum TeamId { Red = 0, Blue = 1 }
    public enum MatchPhase { Ready, Countdown, Regulation, SuddenDeath, Paused, Finished }
    [Serializable]
    public sealed class MatchResult3D
    {
        public int redScore, blueScore;
        public TeamId winner;
        public bool suddenDeath;
        public double elapsedSeconds;
        public int matchNumber;
    }

    // Pure rules: no physics, Unity time, frame order or random scoring.
    public sealed class MatchRules3D
    {
        public MatchPhase Phase { get; private set; } = MatchPhase.Ready;
        public int Red { get; private set; }
        public int Blue { get; private set; }
        public int MatchNumber { get; private set; }
        public double Remaining { get; private set; }
        public double Elapsed { get; private set; }
        public double Countdown { get; private set; }
        public MatchResult3D Result { get; private set; }
        public bool Active => Phase == MatchPhase.Regulation || Phase == MatchPhase.SuddenDeath;
        private MatchPhase pausedPhase;
        private double duration;
        public void Reset(double seconds, double countdown)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (double.IsNaN(countdown) || double.IsInfinity(countdown) || countdown < 0) throw new ArgumentOutOfRangeException(nameof(countdown));
            MatchNumber++; duration = seconds; Remaining = seconds; Countdown = countdown;
            Elapsed = 0; Red = Blue = 0; Result = null; Phase = MatchPhase.Ready;
        }
        public void Start() { if (Phase == MatchPhase.Ready) Phase = Countdown > 0 ? MatchPhase.Countdown : MatchPhase.Regulation; }
        public void Tick(double dt)
        {
            if (double.IsNaN(dt) || double.IsInfinity(dt) || dt < 0) throw new ArgumentOutOfRangeException(nameof(dt));
            if (Phase == MatchPhase.Countdown)
            {
                Countdown = Math.Max(0, Countdown - dt);
                if (Countdown <= 0) Phase = MatchPhase.Regulation;
                return; // Release happens on a fixed-step boundary, with a full match clock.
            }
            if (!Active) return;
            Elapsed += dt;
            if (Phase == MatchPhase.SuddenDeath) return;
            Remaining = Math.Max(0, Remaining - dt);
            if (Remaining > 0.0000001) return;
            Remaining = 0;
            if (Red == Blue) Phase = MatchPhase.SuddenDeath;
            else Finish(false);
        }
        public bool Score(TeamId team)
        {
            if (!Active || (team != TeamId.Red && team != TeamId.Blue)) return false;
            if (team == TeamId.Red) Red += 2; else Blue += 2;
            if (Phase == MatchPhase.SuddenDeath) Finish(true);
            return true;
        }
        public void Pause() { if (Phase == MatchPhase.Countdown || Active) { pausedPhase = Phase; Phase = MatchPhase.Paused; } }
        public void Resume() { if (Phase == MatchPhase.Paused) Phase = pausedPhase; }
        private void Finish(bool overtime)
        {
            Phase = MatchPhase.Finished;
            Result = new MatchResult3D { redScore = Red, blueScore = Blue, winner = Red > Blue ? TeamId.Red : TeamId.Blue,
                suddenDeath = overtime, elapsedSeconds = Elapsed, matchNumber = MatchNumber };
        }
    }
}
