using UnityEngine;

namespace MarbleBasket3D
{
    [CreateAssetMenu(menuName = "Basquete 3D/Configuração da arena", fileName = "Arena3D_Settings")]
    public sealed class ArenaSettings3D : ScriptableObject
    {
        [Header("Partida")]
        [Min(1)] public float matchSeconds = 90;
        [Min(0)] public float countdownSeconds = 3;
        public bool autoStart = true;
        [Range(1, 6)] public int marblesPerTeam = 4;
        public string redName = "VERMELHO", blueName = "AZUL";
        public Color redColor = new Color(.92f, .055f, .1f), blueColor = new Color(.035f, .25f, 1f);
        [Header("Bolinhas e física")]
        [Range(.12f, .22f)] public float marbleRadius = .18f;
        [Range(.1f, 2)] public float marbleMass = .45f;
        [Range(0, 1)] public float marbleBounce = .42f;
        [Range(.005f, .03f)] public float physicsStep = .01f;
        [Header("Plataforma e dois rotores")]
        [Range(1, 8)] public float deckSlopeDegrees = 6.5f;
        [Range(.8f, 1.10f)] public float duelRotorRadius = 1.05f;
        [Range(10, 120)] public float crossDegreesPerSecond = 105;
        [Range(2, 12)] public float crossReversalSeconds = 5;
        [Header("Lançador — mesmos controles para os dois times")]
        public bool continuousLauncher = true;
        [Tooltip("Centro físico da cruz contínua, independente do encaixe da alavanca antiga.")]
        public Vector3 launcherCenter = new Vector3(1.05f,1.85f,-1.80f);
        [Range(30, 160)] public float launcherRpm = 75;
        [Range(0, 20)] public float launcherRpmVariation = 5;
        [Tooltip("Queda no lado ascendente, atrás do centro em relação à cesta, em frações do raio.")]
        [Range(.35f, 1.3f)] public float launcherFeedOffset = .65f;
        [Tooltip("Ajusta a velocidade uma vez, no impacto. O voo seguinte permanece físico.")]
        public bool impactAssist = true;
        [Tooltip("0,25 limita a correção a 25% da velocidade da pancada. Não é uma chance de cesta.")]
        [Range(0, 1)] public float impactAssistStrength = .25f;
        [Range(0, 1.5f)] public float impactTargetSpread = .70f;
        public bool compactLauncher = true;
        [Range(.5f, 1.05f)] public float launcherWheelRadius = .85f;
        public bool calibrateLaunchSpeed = true;
        [Range(0, 6)] public float launchYawVariation = 2.5f;
        [Range(5, 14)] public float launchSpeed = 8.65f;
        [Range(40, 75)] public float launchAngle = 56;
        [Range(0, 5)] public float angleVariation = 2.8f;
        [Range(0, 1)] public float speedVariation = .25f;
        [Min(.5f)] public float powerCycleSeconds = 3.8f;
        [Range(.12f, 1)] public float preparationSeconds = .20f;
        [Range(.12f, 1)] public float launchInterval = .24f;
        [HideInInspector] public bool waitForShotToLand; // Legacy value ignored: the entry now stays open.
        [Header("Retorno e recuperação")]
        [Range(1, 8)] public float returnSeconds = 3.2f;
        [HideInInspector] public float secondChanceSeconds = 1.7f; // Legacy serialized value; all collected balls now return home.
        [HideInInspector] public bool enableSecondChance;
        [Range(3, 20)] public float stuckSeconds = 7;
        [Range(4, 25)] public float mechanismTimeout = 12;
        [Range(.5f, 8)] public float shotTimeout = 6;
        [Header("Câmera e gravação")]
        public bool hideButtonsDuringPlay;
        public Vector3 cameraPosition = new Vector3(0, 12, -23);
        public Vector3 cameraTarget = new Vector3(0, 4.2f, .5f);
        [Range(25, 60)] public float cameraFieldOfView = 41;
        [Tooltip("A cena 3D usa o renderer Built-in e restaura a configuração anterior ao sair do Play.")]
        public bool useBuiltInRendering = true;
        public GameObject marblePrefab;
        [Header("Áudio opcional — sons simples são gerados quando vazio")]
        public AudioClip collisionSound, launchSound, basketSound, fallSound, finishSound;
        public void Validate()
        {
            foreach (float value in new[] { matchSeconds, countdownSeconds, marbleRadius, marbleMass, marbleBounce, physicsStep, launchSpeed, launchAngle, angleVariation, speedVariation, powerCycleSeconds, preparationSeconds, launchInterval, returnSeconds, secondChanceSeconds, stuckSeconds, mechanismTimeout, shotTimeout, cameraFieldOfView, crossDegreesPerSecond, crossReversalSeconds, launchYawVariation, deckSlopeDegrees, duelRotorRadius, launcherWheelRadius, launcherRpm, launcherRpmVariation, launcherFeedOffset, impactTargetSpread, impactAssistStrength })
                if (!Finite(value)) throw new System.InvalidOperationException("Os controles da arena precisam de valores finitos.");
            if (marbleMass < .1f || marbleMass > 2 || marbleBounce < 0 || marbleBounce > 1 || angleVariation < 0 || angleVariation > 5 || speedVariation < 0 || speedVariation > 1) throw new System.InvalidOperationException("Confira massa, quique e variação do lançador.");
            if (!Finite(matchSeconds) || matchSeconds <= 0 || !Finite(countdownSeconds) || countdownSeconds < 0) throw new System.InvalidOperationException("Confira a duração e a contagem regressiva.");
            if (marblesPerTeam < 1 || marblesPerTeam > 6 || marbleRadius < .12f || marbleRadius > .22f) throw new System.InvalidOperationException("Use 1 a 6 bolinhas por time e raio entre 0,12 e 0,22.");
            if (!Finite(launchSpeed) || launchSpeed < 5 || launchSpeed > 14 || !Finite(launchAngle) || launchAngle < 40 || launchAngle > 75) throw new System.InvalidOperationException("Confira velocidade e ângulo de lançamento.");
            if (powerCycleSeconds < .5f || preparationSeconds < .12f || launchInterval < .1f || physicsStep < .005f || physicsStep > .03f) throw new System.InvalidOperationException("Confira os tempos do mecanismo e da física.");
            if (deckSlopeDegrees < 1 || deckSlopeDegrees > 8 || duelRotorRadius < .8f || duelRotorRadius > 1.1f || launcherWheelRadius < .5f || launcherWheelRadius > 1.05f) throw new System.InvalidOperationException("Confira a inclinação, os rotores e o tamanho do lançador.");
            if (!Finite(launcherCenter.x)||!Finite(launcherCenter.y)||!Finite(launcherCenter.z)||Mathf.Abs(launcherCenter.x)>7||Mathf.Abs(launcherCenter.z)>5||(continuousLauncher && launcherCenter.y<launcherWheelRadius+.80f)||launcherCenter.y>3) throw new System.InvalidOperationException("O centro da cruz deve ficar dentro da arena e com folga acima da base.");
            if(continuousLauncher && launcherCenter.y+launcherWheelRadius+.95f>4.45f+Mathf.Tan(deckSlopeDegrees*Mathf.Deg2Rad)*(-.65f-1.4f)-.14f) throw new System.InvalidOperationException("Baixe o centro da cruz para manter a canaleta em descida.");
            if (launcherRpm < 30 || launcherRpm > 160 || launcherRpmVariation < 0 || launcherRpmVariation > 20 || launcherFeedOffset < .35f || launcherFeedOffset > 1.3f || impactTargetSpread < 0 || impactTargetSpread > 1.5f || impactAssistStrength < 0 || impactAssistStrength > 1) throw new System.InvalidOperationException("Confira a rotação contínua, a queda e a assistência dos arremessos.");
            if (crossDegreesPerSecond < 10 || crossDegreesPerSecond > 120 || crossReversalSeconds < 2 || crossReversalSeconds > 12 || launchYawVariation < 0 || launchYawVariation > 6) throw new System.InvalidOperationException("Confira a rotação dos rotores e a oscilação do lançador.");
            if (returnSeconds < 1 || secondChanceSeconds < .5f || stuckSeconds < 3 || mechanismTimeout < 4 || shotTimeout < .5f) throw new System.InvalidOperationException("Confira os tempos de retorno e recuperação.");
        }
        private static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
    }
}
