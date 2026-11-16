using UnityEngine;
using UnityEngine.Rendering;
namespace MarbleBasket3D
{
    // Runtime scope: the 2D project's settings are restored when this scene stops/unloads.
    public sealed class SimulationEnvironment3D : MonoBehaviour
    {
        private RenderPipelineAsset graphics, quality;
        private float step, originalScale;
        private Vector3 gravity;
        private bool acquired;
        public void Acquire(ArenaSettings3D settings)
        {
            if (acquired) return;
            graphics = GraphicsSettings.defaultRenderPipeline; quality = QualitySettings.renderPipeline;
            step = Time.fixedDeltaTime; originalScale = Time.timeScale; gravity = Physics.gravity; acquired = true;
            if (settings.useBuiltInRendering) { QualitySettings.renderPipeline = null; GraphicsSettings.defaultRenderPipeline = null; }
            Time.fixedDeltaTime = settings.physicsStep; Physics.gravity = new Vector3(0, -9.81f, 0);
        }
        private void OnDestroy()
        {
            if (!acquired) return;
            Time.fixedDeltaTime = step; Time.timeScale = originalScale; Physics.gravity = gravity;
            GraphicsSettings.defaultRenderPipeline = graphics; QualitySettings.renderPipeline = quality;
        }
    }
}
