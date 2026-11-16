using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
namespace MarbleBasket3D
{
    [Serializable] public sealed class MarbleReport3D
    {
        public string id, state;
        public bool active, rendererEnabled;
        public Vector3 position;
        public int attempt;
        public float peakHeight, rimPlaneOffset;
    }
    [Serializable] public sealed class ArenaReport3D
    {
        public string phase;
        public int expectedMarbles, registeredMarbles, liveRed, liveBlue, returning, redShots, blueShots, baskets, redScore, blueScore, recoveries;
        public float elapsedSimulation;
        public List<MarbleReport3D> marbles = new List<MarbleReport3D>();
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
            if (match != null && epoch != match.Rules.MatchNumber) ResetCounters();
        }
        public int CountLive(Core.TeamId team, bool onlyReturning = false)
        {
            int count=0;
            if(match==null)return count;
            foreach(var ball in match.Marbles)
                if(ball!=null && ball.Body!=null && ball.gameObject.activeSelf && ball.Team==team && (!onlyReturning || ball.State==MarbleState3D.Returning)) count++;
            return count;
        }
        public ArenaReport3D Capture()
        {
            CheckEpoch();
            var report = new ArenaReport3D { phase=match.Rules.Phase.ToString(),expectedMarbles=match.Settings.marblesPerTeam*2,
                registeredMarbles=match.Marbles.Count, liveRed=CountLive(Core.TeamId.Red),liveBlue=CountLive(Core.TeamId.Blue),returning=match.Returns.ReturningCount,
                redShots=RedShots,blueShots=BlueShots,baskets=Baskets,redScore=match.Rules.Red,blueScore=match.Rules.Blue,elapsedSimulation=match.SimulationTime,
                recoveries=match.GetComponent<Recovery3D>()!=null?match.GetComponent<Recovery3D>().Recoveries:0 };
            foreach(var b in match.Marbles)
            {
                if(b==null || b.Body==null){report.marbles.Add(new MarbleReport3D{id="ausente",active=false});continue;}
                var renderer=b.GetComponent<Renderer>();
                report.marbles.Add(new MarbleReport3D{id=(b.Team==Core.TeamId.Red?"V":"A")+b.Number,state=b.State.ToString(),active=b.gameObject.activeSelf,
                    rendererEnabled=renderer!=null && renderer.enabled,position=b.Body.position,attempt=b.Shot.Attempt,
                    peakHeight=(float)b.Shot.PeakHeight,rimPlaneOffset=(float)b.Shot.RimPlaneOffset});
            }
            return report;
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
