using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PokemonMarbleRace;
using UnityEngine;
using TMPro;

public static class MarbleHarness
{
    private static int assertions;
    private static void Check(bool value, string message)
    {
        assertions++;
        if (!value) throw new Exception("FAILED: " + message);
    }
    private static void Near(float expected, float actual, float tolerance, string message)
    { Check(Math.Abs(expected - actual) <= tolerance, message + ": expected " + expected + ", actual " + actual); }
    private static PokemonDados Pokemon(int number, TipoPokemon type)
    {
        var pokemon = ScriptableObject.CreateInstance<PokemonDados>();
        pokemon.numeroPokedex = number;
        pokemon.nomePokemon = "RACER " + number;
        pokemon.tipoPrimario = type;
        pokemon.retrato = new Sprite { bounds = new Bounds(Vector3.zero, new Vector3(1.2f, 2.4f, 0)) };
        pokemon.cor = new Color(.7f, .6f, .2f);
        return pokemon;
    }
    private static RaceMarble Runner(PokemonDados pokemon, RaceCourse course, int slot, bool powers, float size = .55f, IReadOnlyList<Collider2D> hazards = null)
    {
        var go = new GameObject("Marble " + pokemon.IdEstavel);
        var runner = go.AddComponent<RaceMarble>();
        runner.Initialize(pokemon, course, course.seed, slot, size,
            new Sprite { bounds = new Bounds(Vector3.zero, new Vector3(4.86f, 4.86f, 0)) },
            new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")),
            new PhysicsMaterial2D("Harness") { friction = .05f, bounciness = .7f },
            TMP_Settings.defaultFontAsset, powers, course.Length / 45f,
            hazards ?? new List<Collider2D>());
        return runner;
    }
    private static void Reset()
    {
        foreach (GameObject go in GameObject.All.ToArray()) if (!go.destroyed) UnityEngine.Object.Destroy(go);
        GameObject.All.Clear(); Scheduler.Reset(); Debug.Errors.Clear();
    }
    private static void DimensionsAndGrid()
    {
        Reset();
        RaceCourse course = RaceCourseGenerator.Generate(59);
        var racers = new List<RaceMarble>();
        for (int i = 0; i < 16; i++) racers.Add(Runner(Pokemon(i + 1, TipoPokemon.Normal), course, i, true));
        foreach (RaceMarble racer in racers)
        {
            Near(1, racer.transform.localScale.x, .00001f, "physical object root is not scaled by texture bounds");
            Near(.55f, racer.GetComponent<CircleCollider2D>().bounds.size.x, .00001f, "physical diameter matches configured size");
            SpriteRenderer body = racer.transform.Find("Marble").GetComponent<SpriteRenderer>();
            SpriteRenderer portrait = racer.transform.Find("Pokemon Portrait").GetComponent<SpriteRenderer>();
            SpriteRenderer rim = racer.transform.Find("Marble Rim").GetComponent<SpriteRenderer>();
            Near(.55f, body.bounds.size.x, .00001f, "large native texture normalized to marble size");
            Near(.55f * .82f, Mathf.Max(portrait.bounds.size.x, portrait.bounds.size.y), .00001f, "portrait fits inside body");
            Near(.55f * 1.1f, rim.bounds.size.x, .00001f, "rim limited to ten percent decoration");
            Check(racer.Progress < 4.5f, "default16 grid stays before first obstacle");
            RacePoint center = course.Sample(racer.Progress);
            float clearance = Vector2.Distance(racer.Position, new Vector2(center.x, center.y)) + .275f;
            Check(clearance < course.width * .5f, "grid fits inside rails");
            Check(!racer.GetComponent<Rigidbody2D>().simulated, "grid is frozen until the race starts");
        }
        for (int i = 0; i < racers.Count; i++) for (int j = i + 1; j < racers.Count; j++)
            Check(Vector2.Distance(racers[i].Position, racers[j].Position) >= .55f, "grid circles do not overlap");
        var stopped = racers[0]; stopped.SetRacing(true); stopped.Step(1, .02f, racers);
        Check(stopped.GetComponent<Rigidbody2D>().linearVelocity.magnitude > 0, "racing starts motor");
        stopped.SetRacing(false);
        Check(!stopped.GetComponent<Rigidbody2D>().simulated && stopped.GetComponent<Rigidbody2D>().linearVelocity.magnitude == 0, "stopping disables body and velocity");
        Console.WriteLine("PASS actual RaceMarble size normalization and non-overlapping16 grid");
    }
    private static void ProjectionAndRecovery()
    {
        Reset(); RaceCourse course = RaceCourseGenerator.Generate(761);
        var runner = Runner(Pokemon(1, TipoPokemon.Normal), course, 0, false);
        var racers = new List<RaceMarble> { runner };
        runner.SetRacing(true);
        RacePoint finish = course.Sample(course.Length);
        runner.GetComponent<Rigidbody2D>().position = new Vector2(finish.x, finish.y);
        runner.Step(.1f, .02f, racers);
        Check(!runner.Finished && runner.Progress < 5f, "teleport beside finish cannot skip course checkpoints");
        RacePoint start = course.Sample(.45f);
        runner.GetComponent<Rigidbody2D>().position = new Vector2(start.x, start.y);
        float before = runner.Progress;
        runner.Step(5f, .02f, racers);
        Check(runner.Rescues == 1, "blocked marble recovers after bounded delay");
        Check(runner.Progress <= before + .001f, "recovery gives no unearned progress");
        Check(runner.Ability == "RECOVERING", "recovery is signaled");
        runner.Step(5.2f, .02f, racers);
        Near(0, runner.GetComponent<Rigidbody2D>().linearVelocity.magnitude, .00001f, "recovery penalty holds motor");
        runner.Step(6f, .02f, racers);
        Check(runner.GetComponent<Rigidbody2D>().linearVelocity.magnitude > 0, "motor resumes after recovery penalty");
        RaceFinish result = runner.Result(1, 60f);
        Check(!result.finished && result.time == 60f && result.progress >= 0 && result.progress <= 1, "timeout result has bounded progress and race time");
        Console.WriteLine("PASS checkpoint ordering, stuck recovery and bounded timeout result");
    }
    private static void MotorCourseSweep()
    {
        int finishers = 0, total = 0;
        foreach (int seed in new[] { 1, 19, 66, 704, 1918, 2026 })
        {
            Reset(); RaceCourse course = RaceCourseGenerator.Generate(seed);
            var racers = new List<RaceMarble>();
            for (int i = 0; i < 8; i++) racers.Add(Runner(Pokemon(i + 1, (TipoPokemon)(i + 1)), course, i, true));
            foreach (RaceMarble racer in racers) racer.SetRacing(true);
            float elapsed = 0f;
            while (elapsed < 60f && racers.Any(r => !r.Finished))
            {
                elapsed += .02f;
                foreach (RaceMarble racer in racers) racer.Step(elapsed, .02f, racers);
                Physics2D.Simulate(.02f);
            }
            int count = racers.Count(r => r.Finished);
            finishers += count; total += racers.Count;
            Check(count >= 6, "steering reaches finish for most racers on seed " + seed + " (facade has no collisions): " + count);
            foreach (RaceMarble racer in racers)
            {
                Check(!float.IsNaN(racer.Progress) && !float.IsInfinity(racer.Progress), "progress remains finite");
                Check(racer.Progress >= 0 && racer.Progress <= course.Length + .001f, "progress stays in course domain");
                if (racer.Finished)
                {
                    Check(racer.FinishTime > 0 && racer.FinishTime <= 60.1f, "actual steering assigns a valid finish time");
                    Check(!racer.GetComponent<Rigidbody2D>().simulated, "finisher does not block other marbles");
                }
            }
            Console.WriteLine("PASS motor seed " + seed + ": " + count + "/8 finishers, elapsed " + elapsed.ToString("F2"));
        }
        Console.WriteLine("Motor facade sweep: " + finishers + "/" + total + " finishers. This integrates velocity only, without rail/obstacle/other-marble collisions.");
    }
    private static void BypassRejectsOffTrackProgress()
    {
        Reset();
        RaceCourse course = null;
        for (int seed = 1; seed < 20; seed++)
        {
            var candidate = RaceCourseGenerator.Generate(seed);
            if (candidate.shortcuts.Count > 0) { course = candidate; break; }
        }
        Check(course != null, "test has a real generated bypass");
        var runner = Runner(Pokemon(25, TipoPokemon.Normal), course, 0, false);
        var shortcut = course.shortcuts[0];
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(RaceMarble).GetField("shortcut", flags).SetValue(runner, shortcut);
        typeof(RaceMarble).GetField("branchProgress", flags).SetValue(runner, 0.4f);
        float mapped = shortcut.startDistance + (shortcut.endDistance - shortcut.startDistance) * .4f / RacePolyline.Length(shortcut.points);
        typeof(RaceMarble).GetField("<Progress>k__BackingField", flags).SetValue(runner, mapped);
        runner.GetComponent<Rigidbody2D>().position = new Vector2(40f, 40f);
        runner.SetRacing(true);
        var racers = new List<RaceMarble> { runner };
        for (int step = 1; step <= 80; step++) runner.Step(step * .02f, .02f, racers);
        Near(mapped, runner.Progress, .00001f, "off-track bypass position cannot earn progress over repeated frames");
        Near(.4f, (float)typeof(RaceMarble).GetField("branchProgress", flags).GetValue(runner), .00001f, "bypass cursor stays at earned position");
        Check(!runner.Finished, "off-track bypass does not finish race");
        Console.WriteLine("PASS off-track bypass projection cannot advance the race cursor");
    }
    public static void Main()
    {
        DimensionsAndGrid(); ProjectionAndRecovery(); MotorCourseSweep(); BypassRejectsOffTrackProgress();
        Console.WriteLine("PASS " + assertions + " assertions on real RaceMarble/course code with explicit Unity-contract facade");
    }
}
