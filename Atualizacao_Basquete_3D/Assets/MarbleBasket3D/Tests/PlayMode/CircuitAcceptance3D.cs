using System.Collections;
using System.Collections.Generic;
using MarbleBasket3D.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MarbleBasket3D.Tests
{
    public sealed class CircuitAcceptance3D
    {
        private GameObject root;
        private ArenaSettings3D settings;
        private MarbleMatch3D match;
        [UnitySetUp] public IEnumerator SetUp()
        {
            settings = ScriptableObject.CreateInstance<ArenaSettings3D>(); settings.autoStart = false; settings.countdownSeconds = .1f;
            root = new GameObject("Circuit acceptance test"); root.SetActive(false);
            var builder = root.AddComponent<ArenaBuilder3D>(); builder.settings = settings;
            root.SetActive(true); yield return null; match = builder.Match;
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            if (root != null) Object.Destroy(root); if (settings != null) Object.Destroy(settings); yield return null;
        }
        [UnityTest, Timeout(240000)] public IEnumerator FullAutomaticMatchKeepsEightMarblesAndGivesBothTeamsShots()
        {
            int finishes = 0; match.Finished += result => finishes++; match.StartMatch();
            float deadline = Time.realtimeSinceStartup + 185;
            var identities = new HashSet<int>(); foreach (var b in match.Marbles) identities.Add(b.GetInstanceID());
            while (match.Rules.Phase != MatchPhase.Finished && Time.realtimeSinceStartup < deadline)
            {
                Assert.That(match.Marbles.Count, Is.EqualTo(8));
                foreach (var b in match.Marbles) { Assert.That(b, Is.Not.Null); Assert.That(identities.Contains(b.GetInstanceID()), Is.True); }
                yield return new WaitForSeconds(.25f);
            }
            ArenaReport3D report = match.GetComponent<ArenaDiagnostics3D>().Capture();
            Debug.Log(JsonUtility.ToJson(report, true));
            Assert.That(match.Rules.Phase, Is.EqualTo(MatchPhase.Finished), "Ajuste a dinâmica se não houver resultado em até 185 s reais.");
            Assert.That(finishes, Is.EqualTo(1)); Assert.That(report.redShots, Is.GreaterThan(0)); Assert.That(report.blueShots, Is.GreaterThan(0));
            Assert.That(report.baskets, Is.GreaterThan(0)); Assert.That(identities.Count, Is.EqualTo(8));
            match.RestartMatch(); Assert.That(match.Rules.Result, Is.Null); Assert.That(match.Rules.Red + match.Rules.Blue, Is.Zero);
            Assert.That(match.Launcher.ShotsFired, Is.Zero); Assert.That(match.Returns.ReturningCount, Is.Zero);
        }
        [UnityTest] public IEnumerator V4BuildHasTwinRotorsContinuousWheelAndEightMarbles()
        {
            var diagnostics=match.GetComponent<ArenaDiagnostics3D>();
            Assert.That(diagnostics.CountLive(TeamId.Red),Is.EqualTo(4));Assert.That(diagnostics.CountLive(TeamId.Blue),Is.EqualTo(4));
            Assert.That(root.GetComponentsInChildren<RotatingCross3D>().Length,Is.EqualTo(1));
            var rotors=root.GetComponentInChildren<RotatingCross3D>();
            Assert.That(rotors.body,Is.Not.Null);Assert.That(rotors.oppositeBody,Is.Not.Null);
            Assert.That(rotors.body.GetComponentsInChildren<BoxCollider>().Length,Is.EqualTo(3));
            Assert.That(rotors.oppositeBody.GetComponentsInChildren<BoxCollider>().Length,Is.EqualTo(3));
            int paddles=0;foreach(var child in match.Launcher.leverBody.GetComponentsInChildren<Transform>())
                if(child.name.StartsWith("Catapult paddle"))paddles++;
            Assert.That(paddles,Is.EqualTo(4));Assert.That(settings.launcherWheelRadius,Is.LessThan(.81f));
            Assert.That(match.Launcher.Continuous,Is.True);Assert.That(settings.impactAssistStrength,Is.EqualTo(.25f));
            foreach(var collider in root.GetComponentsInChildren<Collider>()) Assert.That(collider.gameObject.name,Does.Not.Contain("Queue gate"));
            Assert.That(settings.preparationSeconds,Is.LessThan(.4f));
            LogAssert.NoUnexpectedReceived();yield return null;
        }
        [UnityTest] public IEnumerator ContinuousWheelDoesNotWaitForAnOccupantAndPausesWithMatch()
        {
            match.StartMatch();while(!match.Active)yield return new WaitForFixedUpdate();
            float before=match.Launcher.WheelAngle;
            for(int i=0;i<20;i++)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(match.Launcher.Occupant,Is.Null);
                foreach(var b in match.Marbles)Assert.That(b.State,Is.Not.EqualTo(MarbleState3D.Seated));
            }
            Assert.That(match.Launcher.WheelAngle,Is.Not.EqualTo(before));
            Assert.That(match.Launcher.Phase,Is.EqualTo(LauncherPhase3D.Spinning));
            match.TogglePause();before=match.Launcher.WheelAngle;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(match.Launcher.WheelAngle,Is.EqualTo(before));match.TogglePause();
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            Assert.That(match.Launcher.WheelAngle,Is.Not.EqualTo(before));
        }
        [UnityTest] public IEnumerator PauseAndOutOfBoundsRecoveryPreserveIdentity()
        {
            match.StartMatch(); while (!match.Active) yield return new WaitForFixedUpdate();
            var b = match.Marbles[0]; int identity = b.GetInstanceID();
            match.TogglePause(); Vector3 paused = b.Body.position; yield return new WaitForSecondsRealtime(.15f);
            Assert.That(Vector3.Distance(paused, b.Body.position), Is.LessThan(.001f)); match.TogglePause();
            b.Body.position = new Vector3(30,2,0); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.That(b.State, Is.EqualTo(MarbleState3D.Returning));
            yield return new WaitForSeconds(settings.returnSeconds + .1f);
            Assert.That(b.GetInstanceID(), Is.EqualTo(identity)); Assert.That(match.Marbles.Count, Is.EqualTo(8)); Assert.That(b.State, Is.Not.EqualTo(MarbleState3D.Returning));
        }
    }
}
