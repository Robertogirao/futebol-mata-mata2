using System;
using System.Collections.Generic;
using System.Reflection;
using PokemonMarbleRace;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class Harness
{
    private static int checks;
    private static void Check(bool value, string label)
    { checks++; if (!value) throw new Exception("FAILED: " + label); }
    private static object Get(object target, string field)
    { return target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target); }
    private static void Call(object target, string method)
    { target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null); }
    private static List<RaceHudEntry> Entrants(int count)
    {
        var result = new List<RaceHudEntry>();
        for (int i = 0; i < count; i++) result.Add(new RaceHudEntry
        {
            id = "pokemon_" + i, name = "RUNNER " + i, portrait = new Sprite(),
            position = i + 1, color = new Color((i + 1) / 16f, 0.7f, 0.8f),
            detail = i % 2 == 0 ? "12.5s" : "48%", points = count - i,
            finished = i % 2 == 0
        });
        return result;
    }

    public static void Main()
    {
        var owner = new GameObject("RaceHUD");
        var hud = owner.AddComponent<RaceHud>();
        hud.onEnableRegistered = false;
        var title = new TMP_FontAsset(); var body = new TMP_FontAsset();
        hud.Build(title, body, "MY MARBLE CUP");
        var root = (RectTransform)Get(hud, "root");
        Check(root != null && root.parent == owner.transform, "owned broadcast canvas built before OnEnable");
        Check(!hud.isActiveAndEnabled, "Build does not manufacture callback registration");
        Check(root.GetComponent<Canvas>().sortingOrder == 4000, "broadcast canvas sorting order");
        Check(root.GetComponent<CanvasScaler>().referenceResolution.x == 1920f, "reference resolution");
        Check(((TextMeshProUGUI)Get(hud, "competitionTitle")).text == "MY MARBLE CUP", "custom championship name");
        Check(((TextMeshProUGUI)Get(hud, "competitionTitle")).font == title, "heading font connected");
        int initialObjects = GameObject.all.Count;
        var replacementTitle = new TMP_FontAsset();
        hud.Build(replacementTitle, body, "UPDATED NAME");
        Check(GameObject.all.Count == initialObjects && (RectTransform)Get(hud, "root") == root, "Build idempotent without extra canvas");
        Check(((TextMeshProUGUI)Get(hud, "competitionTitle")).text == "UPDATED NAME", "Build refreshes name");
        Check(((TextMeshProUGUI)Get(hud, "competitionTitle")).font == replacementTitle, "Build refreshes font");
        hud.SetRaceInfo("RACE LEAGUE", "HEAT 1", 2, 3, 44.1f, "WATER CHANNEL");
        Check(((TextMeshProUGUI)Get(hud, "timer")).text == "00:45", "race clock ceiling");
        Check(((TextMeshProUGUI)Get(hud, "raceTitle")).text == "RACE 2 / 3", "race ordinal and total");
        Check(((TextMeshProUGUI)Get(hud, "raceSubtitle")).text == "HEAT 1", "race label");
        Check(((TextMeshProUGUI)Get(hud, "raceStatus")).text == "WATER CHANNEL", "course status");
        hud.SetRaceInfo(null, null, 0, 0, float.NaN, null);
        Check(((TextMeshProUGUI)Get(hud, "timer")).text == "00:00", "NaN timer sanitized");
        Check(((TextMeshProUGUI)Get(hud, "raceTitle")).text == "RACE 1 / 1", "safe missing count");

        foreach (int n in new [] {8, 16})
        {
            var entrants = Entrants(n);
            hud.SetLeaderboard(entrants);
            var panel = (RectTransform)Get(hud, "livePanel");
            int count = GameObject.all.Count;
            for (int frame = 0; frame < 100; frame++) hud.SetLeaderboard(entrants);
            Check(GameObject.all.Count == count, "leaderboard pool stable over 100 updates / " + n);
            for (int i = 0; i < n; i++)
            {
                var row = (RectTransform)panel.Find("LiveRow_" + i);
                Check(row.gameObject.activeInHierarchy, "runner visible / " + n);
                Check(row.Find("RunnerName").GetComponent<TextMeshProUGUI>().text == entrants[i].name, "name linked to runner / " + n);
                Check(row.Find("Portrait").GetComponent<Image>().sprite == entrants[i].portrait, "portrait linked to runner / " + n);
                Check(row.sizeDelta.x == 205f && -row.anchoredPosition.y + row.sizeDelta.y < 720f, "live rows inside left margin / " + n);
            }
            entrants.Reverse();
            hud.SetLeaderboard(entrants);
            Check(panel.Find("LiveRow_0").Find("Portrait").GetComponent<Image>().sprite == entrants[0].portrait, "live reorder retains matching portrait");
            hud.ShowResults("GRAND PRIX STANDINGS", entrants, "Next race in 7 seconds.");
            var resultRoot = (RectTransform)Get(hud, "resultRowsRoot");
            Check(((RectTransform)Get(hud, "overlay")).gameObject.activeInHierarchy, "result overlay visible");
            Check(!((RectTransform)Get(hud, "messagePanel")).gameObject.activeSelf, "message panel hidden in results");
            int resultObjects = GameObject.all.Count;
            hud.ShowResults("GRAND PRIX STANDINGS", entrants, "Results saved.");
            Check(GameObject.all.Count == resultObjects, "results rows reused / " + n);
            for (int i = 0; i < n; i++)
            {
                var row = (RectTransform)resultRoot.Find("ResultRow_" + i);
                Check(row.Find("RunnerName").GetComponent<TextMeshProUGUI>().text == entrants[i].name, "result runner name / " + n);
                Check(row.Find("Portrait").GetComponent<Image>().sprite == entrants[i].portrait, "result portrait / " + n);
                Check(row.Find("Points").GetComponent<TextMeshProUGUI>().text == entrants[i].points + " PTS", "result points / " + n);
                Check(-row.anchoredPosition.y + row.sizeDelta.y <= 568f, "result row before footer / " + n);
            }
        }

        hud.SetAbility("Squirtle", "water boost", 2.5f);
        Check(((RectTransform)Get(hud, "abilityPanel")).gameObject.activeSelf, "ability panel active");
        Check(((TextMeshProUGUI)Get(hud, "abilityTitle")).text == "WATER BOOST", "ability English uppercase");
        Check(((TextMeshProUGUI)Get(hud, "abilityTime")).text == "2.5s", "ability remaining time");
        hud.SetAbility("Squirtle", "water boost", 0f);
        Check(!((RectTransform)Get(hud, "abilityPanel")).gameObject.activeSelf, "expired ability hidden");
        hud.ShowMessage("3", "Get ready!");
        Check(((TextMeshProUGUI)Get(hud, "messageTitle")).text == "3", "countdown title");
        Check(((TextMeshProUGUI)Get(hud, "messageBody")).text == "Get ready!", "countdown body");
        Check(!((RectTransform)Get(hud, "resultsPanel")).gameObject.activeSelf, "results hidden in message");
        hud.HideOverlay();
        Check(!((RectTransform)Get(hud, "overlay")).gameObject.activeSelf, "overlay can hide for racing");
        hud.SetLeaderboard(null);
        Check(!((RectTransform)Get(hud, "livePanel")).Find("LiveRow_0").gameObject.activeSelf, "empty leaderboard hides previous entrants");
        hud.ShowResults(null, null, null);
        Check(!((RectTransform)Get(hud, "resultRowsRoot")).Find("ResultRow_0").gameObject.activeSelf, "empty results hide previous entrants");
        Call(hud, "OnDisable"); Check(!root.gameObject.activeSelf, "disable hides entire HUD");
        Call(hud, "OnEnable"); Check(root.gameObject.activeSelf, "enable restores entire HUD");
        hud.Clear();
        Check(Get(hud, "root") == null && root.gameObject.destroyed, "Clear destroys owned canvas");
        hud.Build(null, null, null);
        Check(Get(hud, "root") != null, "rebuild after Clear");
        Check(((TextMeshProUGUI)Get(hud, "competitionTitle")).font == TMP_Settings.defaultFontAsset, "default TMP font fallback");
        Console.WriteLine("PASS lifecycle, fonts, pooling, countdown, timing, abilities and 8/16 entrant standings");
        Console.WriteLine("PASS " + checks + " assertions using real RaceHud with Unity/TMP/UI stubs");
    }
}
