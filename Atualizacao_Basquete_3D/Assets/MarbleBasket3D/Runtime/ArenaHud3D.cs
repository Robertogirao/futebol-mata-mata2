using MarbleBasket3D.Core;
using UnityEngine;
namespace MarbleBasket3D
{
    public sealed class ArenaHud3D : MonoBehaviour
    {
        public MarbleMatch3D match;
        private GUIStyle team, score, small, message, button;
        private Texture2D panel, accent;
        private ArenaDiagnostics3D diagnostics;
        private static Texture2D Pixel(Color color)
        {
            var t = new Texture2D(1, 1); t.SetPixel(0, 0, color); t.Apply(); return t;
        }
        private void OnGUI()
        {
            if (match == null || match.Settings == null) return;
            float w = Screen.width, h = Screen.height;
            float v = Mathf.Min(w / 1920f, h / 1080f), ox = (w - 1920 * v) * .5f, oy = (h - 1080 * v) * .5f;
            if (panel == null) { panel = Pixel(new Color(.025f, .04f, .065f, .94f)); accent = Pixel(new Color(.95f, .7f, .22f)); }
            if (team == null)
            {
                team = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                score = new GUIStyle(team); small = new GUIStyle(team); message = new GUIStyle(team); button = new GUIStyle(GUI.skin.button);
            }
            team.fontSize = Mathf.Max(12, Mathf.RoundToInt(28 * v)); score.fontSize = Mathf.Max(18, Mathf.RoundToInt(48 * v));
            small.fontSize = Mathf.Max(11, Mathf.RoundToInt(18 * v)); message.fontSize = Mathf.Max(12, Mathf.RoundToInt(24 * v)); button.fontSize = Mathf.Max(11, Mathf.RoundToInt(19 * v));
            Rect R(float x, float y, float rw, float rh) => new Rect(ox + x * v, oy + y * v, rw * v, rh * v);
            GUI.color = Color.white; GUI.DrawTexture(R(250, 20, 1420, 132), panel); GUI.DrawTexture(R(250, 150, 1420, 3), accent);
            GUI.color = match.Settings.redColor; GUI.DrawTexture(R(270, 40, 6, 65), Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(R(285, 40, 480, 60), match.Settings.redName, team); GUI.Label(R(760, 33, 110, 70), match.Rules.Red.ToString("00"), score);
            GUI.Label(R(1050, 33, 110, 70), match.Rules.Blue.ToString("00"), score); GUI.Label(R(1170, 40, 465, 60), match.Settings.blueName, team);
            GUI.color = match.Settings.blueColor; GUI.DrawTexture(R(1644, 40, 6, 65), Texture2D.whiteTexture); GUI.color = Color.white;
            if(diagnostics==null)diagnostics=match.GetComponent<ArenaDiagnostics3D>();
            if(diagnostics!=null)
            {
                string Count(TeamId id) => "BOLAS "+diagnostics.CountLive(id)+"/"+match.Settings.marblesPerTeam+" · "+diagnostics.CountLive(id,true)+" NO RETORNO";
                GUI.Label(R(290,103,480,30),Count(TeamId.Red),small);
                GUI.Label(R(1170,103,465,30),Count(TeamId.Blue),small);
                GUI.Label(R(790,103,340,30),"ARREMESSOS "+diagnostics.RedShots+" / "+diagnostics.BlueShots,small);
            }
            string clock = match.Rules.Phase == MatchPhase.SuddenDeath ? "PRORROGAÇÃO" : Mathf.CeilToInt((float)match.Rules.Remaining).ToString("00") + "s";
            GUI.Label(R(865, 40, 190, 55), clock, small);
            string phase = match.Rules.Phase == MatchPhase.Countdown ? "COMEÇA EM " + Mathf.CeilToInt((float)match.Rules.Countdown) : match.Rules.Phase == MatchPhase.Paused ? "PAUSADO" : match.Signals.Message;
            GUI.DrawTexture(R(440, 980, 1040, 55), panel); GUI.Label(R(450, 983, 1020, 48), phase, message);
            if (match.Active)
            {
                float cyclePower = Mathf.Clamp01((match.Launcher.CurrentSpeed - match.Launcher.ReferenceSpeed + match.Settings.speedVariation) / Mathf.Max(.01f, match.Settings.speedVariation * 2));
                GUI.DrawTexture(R(1540, 924, 250, 9), panel); GUI.DrawTexture(R(1540, 924, 250 * cyclePower, 9), accent);
                GUI.Label(R(1530, 884, 270, 30), "CICLO DE POTÊNCIA", small);
            }
            if (!match.Settings.hideButtonsDuringPlay || match.Rules.Phase == MatchPhase.Ready || match.Rules.Phase == MatchPhase.Finished || match.Rules.Phase == MatchPhase.Paused)
            {
                if (GUI.Button(R(680, 1040, 175, 35), "Iniciar", button)) match.StartMatch();
                if (GUI.Button(R(872, 1040, 175, 35), match.Rules.Phase == MatchPhase.Paused ? "Continuar" : "Pausar", button)) match.TogglePause();
                if (GUI.Button(R(1064, 1040, 175, 35), "Reiniciar", button)) match.RestartMatch();
            }
        }
        private void OnDestroy() { if (panel != null) Destroy(panel); if (accent != null) Destroy(accent); }
    }
}
