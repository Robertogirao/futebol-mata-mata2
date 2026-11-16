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
        [Header("Lançador — mesmos controles para os dois times")]
        [Range(5, 14)] public float launchSpeed = 8.65f;
        [Range(40, 75)] public float launchAngle = 56;
        [Range(0, 5)] public float angleVariation = 2.8f;
        [Range(0, 1)] public float speedVariation = .38f;
        [Min(.5f)] public float powerCycleSeconds = 3.8f;
        [Range(.3f, 3)] public float preparationSeconds = .85f;
        [Range(.1f, 3)] public float launchInterval = .55f;
        public bool waitForShotToLand = true;
        [Header("Retorno e recuperação")]
        [Range(1, 8)] public float returnSeconds = 3.2f;
        [Range(.5f, 5)] public float secondChanceSeconds = 1.7f;
        public bool enableSecondChance = true;
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
            foreach (float value in new[] { matchSeconds, countdownSeconds, marbleRadius, marbleMass, marbleBounce, physicsStep, launchSpeed, launchAngle, angleVariation, speedVariation, powerCycleSeconds, preparationSeconds, launchInterval, returnSeconds, secondChanceSeconds, stuckSeconds, mechanismTimeout, shotTimeout, cameraFieldOfView })
                if (!Finite(value)) throw new System.InvalidOperationException("Os controles da arena precisam de valores finitos.");
            if (marbleMass < .1f || marbleMass > 2 || marbleBounce < 0 || marbleBounce > 1 || angleVariation < 0 || angleVariation > 5 || speedVariation < 0 || speedVariation > 1) throw new System.InvalidOperationException("Confira massa, quique e variação do lançador.");
            if (!Finite(matchSeconds) || matchSeconds <= 0 || !Finite(countdownSeconds) || countdownSeconds < 0) throw new System.InvalidOperationException("Confira a duração e a contagem regressiva.");
            if (marblesPerTeam < 1 || marblesPerTeam > 6 || marbleRadius < .12f || marbleRadius > .22f) throw new System.InvalidOperationException("Use 1 a 6 bolinhas por time e raio entre 0,12 e 0,22.");
            if (!Finite(launchSpeed) || launchSpeed < 5 || launchSpeed > 14 || !Finite(launchAngle) || launchAngle < 40 || launchAngle > 75) throw new System.InvalidOperationException("Confira velocidade e ângulo de lançamento.");
            if (powerCycleSeconds < .5f || preparationSeconds < .3f || launchInterval < .1f || physicsStep < .005f || physicsStep > .03f) throw new System.InvalidOperationException("Confira os tempos do mecanismo e da física.");
            if (returnSeconds < 1 || secondChanceSeconds < .5f || stuckSeconds < 3 || mechanismTimeout < 4 || shotTimeout < .5f) throw new System.InvalidOperationException("Confira os tempos de retorno e recuperação.");
        }
        private static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
    }
}
