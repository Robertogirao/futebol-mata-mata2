using System;
using System.IO;
using UnityEngine;
namespace MarbleBasket3D
{
    [Serializable]
    public sealed class ArenaReport3D
    {
        public string phase;
        public int expectedMarbles, registeredMarbles, returning, redShots, blueShots, baskets, redScore, blueScore, recoveries;
        public float elapsedSimulation;
    }
    public sealed class ArenaDiagnostics3D : MonoBehaviour
    {
        public MarbleMatch3D match;
        public int RedShots { get; private set; }
        public int BlueShots { get; private set; }
        public int Baskets { get; private set; }
        private int epoch;
        private void Start() { if (match != null) match.Signals.Emitted += Track; }
        private void Track(ArenaEvent3D e)
        {
            CheckEpoch();
            if (e.kind == ArenaEventKind3D.Launch) { if (e.marble.Team == Core.TeamId.Red) RedShots++; else BlueShots++; }
            if (e.kind == ArenaEventKind3D.Basket) Baskets++;
        }
        public void ResetCounters() { epoch = match.Rules.MatchNumber; RedShots = BlueShots = Baskets = 0; }
        private void CheckEpoch()
        {
            if (match != null && epoch != match.Rules.MatchNumber) { epoch = match.Rules.MatchNumber; RedShots = BlueShots = Baskets = 0; }
        }
        public ArenaReport3D Capture()
        {
            CheckEpoch();
            return new ArenaReport3D { phase = match.Rules.Phase.ToString(), expectedMarbles = match.Settings.marblesPerTeam * 2,
                registeredMarbles = match.Marbles.Count, returning = match.Returns.ReturningCount, redShots = RedShots, blueShots = BlueShots,
                baskets = Baskets, redScore = match.Rules.Red, blueScore = match.Rules.Blue, elapsedSimulation = match.SimulationTime,
                recoveries = match.GetComponent<Recovery3D>() != null ? match.GetComponent<Recovery3D>().Recoveries : 0 };
        }
        [ContextMenu("Salvar diagnóstico da partida")]
        public void SaveReport()
        {
            if (!Application.isPlaying || match == null) return;
            string path = Path.Combine(Application.persistentDataPath, "MarbleBasket3D-last-report.json");
            File.WriteAllText(path, JsonUtility.ToJson(Capture(), true)); Debug.Log("Diagnóstico salvo em: " + path, this);
        }
        private void OnDestroy() { if (match != null && match.Signals != null) match.Signals.Emitted -= Track; }
    }
}
