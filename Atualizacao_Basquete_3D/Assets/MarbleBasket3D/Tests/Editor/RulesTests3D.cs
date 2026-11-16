using MarbleBasket3D.Core;
using NUnit.Framework;

namespace MarbleBasket3D.Tests
{
    public sealed class RulesTests3D
    {
        [Test] public void BuzzerStopsScoringAndOvertimeEndsOnNextBasket()
        {
            var rules = new MatchRules3D(); rules.Reset(1,0); rules.Start(); rules.Score(TeamId.Red); rules.Tick(1);
            Assert.That(rules.Result.winner, Is.EqualTo(TeamId.Red)); Assert.That(rules.Score(TeamId.Blue), Is.False);
            rules.Reset(1,0); rules.Start(); rules.Tick(1); Assert.That(rules.Phase, Is.EqualTo(MatchPhase.SuddenDeath));
            rules.Score(TeamId.Blue); Assert.That(rules.Result.suddenDeath, Is.True); Assert.That(rules.Result.winner, Is.EqualTo(TeamId.Blue));
        }
        [Test] public void BasketNeedsNewLaunchAndDownwardPassInsideOpening()
        {
            var shot = new ShotValidation3D(); Assert.That(shot.Sample(0,4,0,0,3,0,.5,.18), Is.False);
            shot.Launch(); shot.Sample(0,2,0,0,3,0,.5,.18); Assert.That(shot.Sample(0,4,0,0,3,0,.5,.18), Is.False);
            Assert.That(shot.Sample(0,2,0,0,3,0,.5,.18), Is.True); Assert.That(shot.Sample(0,1,0,0,3,0,.5,.18), Is.False);
            shot.Launch(); shot.Sample(.6,4,0,0,3,0,.5,.18); Assert.That(shot.Sample(.6,2,0,0,3,0,.5,.18), Is.False);
        }
        [Test] public void PauseAndRestartClearAllRules()
        {
            var r = new MatchRules3D(); r.Reset(90,3); r.Start(); r.Tick(3); r.Score(TeamId.Red); r.Pause(); r.Tick(90);
            Assert.That(r.Remaining, Is.EqualTo(90)); Assert.That(r.Score(TeamId.Blue), Is.False);
            r.Reset(90,3); Assert.That(r.Red + r.Blue, Is.Zero); Assert.That(r.Result, Is.Null); Assert.That(r.Phase, Is.EqualTo(MatchPhase.Ready));
        }
    }
}
