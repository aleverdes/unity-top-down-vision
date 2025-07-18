using System;
using GrozaGames.TopDownVision.URP.MonoBehaviours;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GrozaGames.TopDownVision.URP.RenderFeatures
{
    public class VisionBlurRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private BlurSettings settings;
        [SerializeField] private Shader shader;

        private Material material;
        private VisionBlurRenderPass blurRenderPass;

        public override void Create()
        {
            if (shader == null)
            {
                return;
            }

            material = new Material(shader);
            blurRenderPass = new VisionBlurRenderPass(material, settings);

            blurRenderPass.renderPassEvent = RenderPassEvent.AfterRendering;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera.gameObject.TryGetComponent(out VisionCamera _))
            {
                renderer.EnqueuePass(blurRenderPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
            {
                Destroy(material);
            }
            else
            {
                DestroyImmediate(material);
            }
#else
            Destroy(material);
#endif
        }
    }

    [Serializable]
    public class BlurSettings
    {
        [Range(0, 0.2f)] public float horizontalBlur;
        [Range(0, 0.2f)] public float verticalBlur;
    }
}