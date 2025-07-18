using GrozaGames.TopDownVision.URP.MonoBehaviours;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GrozaGames.TopDownVision.URP.RenderFeatures
{
    public enum RenderQueueType
    {
        /// <summary>
        /// Use this for opaque objects.
        /// </summary>
        Opaque,

        /// <summary>
        /// Use this for transparent objects.
        /// </summary>
        Transparent,
    }

    /// <summary>
    /// The class for the render objects renderer feature.
    /// </summary>
    public class VisionBlackWorldRenderFeature : ScriptableRendererFeature
    {
        /// <summary>
        /// Settings class used for the render objects renderer feature.
        /// </summary>
        [System.Serializable]
        public class RenderObjectsSettings
        {
            /// <summary>
            /// The profiler tag used with the pass.
            /// </summary>
            public string PassTag = "RenderObjectsFeature";

            /// <summary>
            /// Controls when the render pass executes.
            /// </summary>
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

            /// <summary>
            /// The filter settings for the pass.
            /// </summary>
            public FilterSettings FilterSettings = new FilterSettings();

            /// <summary>
            /// The override material to use.
            /// </summary>
            public Material OverrideMaterial = null;

            /// <summary>
            /// The pass index to use with the override material.
            /// </summary>
            public int OverrideMaterialPassIndex = 0;

            /// <summary>
            /// The override shader to use.
            /// </summary>
            public Shader OverrideShader = null;

            /// <summary>
            /// The pass index to use with the override shader.
            /// </summary>
            public int OverrideShaderPassIndex = 0;

            /// <summary>
            /// Options to select which type of override mode should be used.
            /// </summary>
            public enum OverrideMaterialMode
            {
                /// <summary>
                /// Use this to not override.
                /// </summary>
                None,

                /// <summary>
                /// Use this to use an override material.
                /// </summary>
                Material,

                /// <summary>
                /// Use this to use an override shader.
                /// </summary>
                Shader
            };

            /// <summary>
            /// The selected override mode.
            /// </summary>
            public OverrideMaterialMode OverrideMode = OverrideMaterialMode.Material; //default to Material as this was previously the only option

            /// <summary>
            /// Sets whether it should override depth or not.
            /// </summary>
            public bool OverrideDepthState = false;

            /// <summary>
            /// The depth comparison function to use.
            /// </summary>
            public CompareFunction DepthCompareFunction = CompareFunction.LessEqual;

            /// <summary>
            /// Sets whether it should write to depth or not.
            /// </summary>
            public bool EnableWrite = true;

            /// <summary>
            /// The stencil settings to use.
            /// </summary>
            public StencilStateData StencilSettings = new StencilStateData();

            /// <summary>
            /// The camera settings to use.
            /// </summary>
            public CustomCameraSettings CameraSettings = new CustomCameraSettings();
        }

        /// <summary>
        /// The filter settings used.
        /// </summary>
        [System.Serializable]
        public class FilterSettings
        {
            // TODO: expose opaque, transparent, all ranges as drop down

            /// <summary>
            /// The queue type for the objects to render.
            /// </summary>
            public RenderQueueType RenderQueueType;

            /// <summary>
            /// The layer mask to use.
            /// </summary>
            public LayerMask LayerMask;

            /// <summary>
            /// The passes to render.
            /// </summary>
            public string[] PassNames;

            /// <summary>
            /// The constructor for the filter settings.
            /// </summary>
            public FilterSettings()
            {
                RenderQueueType = RenderQueueType.Opaque;
                LayerMask = 0;
            }
        }

        /// <summary>
        /// The settings for custom cameras values.
        /// </summary>
        [System.Serializable]
        public class CustomCameraSettings
        {
            /// <summary>
            /// Used to mark whether camera values should be changed or not.
            /// </summary>
            public bool OverrideCamera = false;

            /// <summary>
            /// Should the values be reverted after rendering the objects?
            /// </summary>
            public bool RestoreCamera = true;

            /// <summary>
            /// Changes the camera offset.
            /// </summary>
            public Vector4 Offset;

            /// <summary>
            /// Changes the camera field of view.
            /// </summary>
            public float CameraFieldOfView = 60.0f;
        }

        public RenderObjectsSettings Settings = new RenderObjectsSettings();

        private VisionBlackWorldRenderPass _visionBlackWorldRenderPass;

        public override void Create()
        {
            FilterSettings filter = Settings.FilterSettings;

            // Render Objects pass doesn't support events before rendering prepasses.
            // The camera is not setup before this point and all rendering is monoscopic.
            // Events before BeforeRenderingPrepasses should be used for input texture passes (shadow map, LUT, etc) that doesn't depend on the camera.
            // These events are filtering in the UI, but we still should prevent users from changing it from code or
            // by changing the serialized data.
            if (Settings.Event < RenderPassEvent.BeforeRenderingPrePasses)
                Settings.Event = RenderPassEvent.BeforeRenderingPrePasses;

            _visionBlackWorldRenderPass = new VisionBlackWorldRenderPass(Settings.PassTag, Settings.Event, filter.PassNames, filter.RenderQueueType, filter.LayerMask, Settings.CameraSettings);

            switch (Settings.OverrideMode)
            {
                case RenderObjectsSettings.OverrideMaterialMode.None:
                    _visionBlackWorldRenderPass.OverrideMaterial = null;
                    _visionBlackWorldRenderPass.OverrideShader = null;
                    break;
                case RenderObjectsSettings.OverrideMaterialMode.Material:
                    _visionBlackWorldRenderPass.OverrideMaterial = Settings.OverrideMaterial;
                    _visionBlackWorldRenderPass.OverrideMaterialPassIndex = Settings.OverrideMaterialPassIndex;
                    _visionBlackWorldRenderPass.OverrideShader = null;
                    break;
                case RenderObjectsSettings.OverrideMaterialMode.Shader:
                    _visionBlackWorldRenderPass.OverrideMaterial = null;
                    _visionBlackWorldRenderPass.OverrideShader = Settings.OverrideShader;
                    _visionBlackWorldRenderPass.OverrideShaderPassIndex = Settings.OverrideShaderPassIndex;
                    break;
            }

            if (Settings.OverrideDepthState)
                _visionBlackWorldRenderPass.SetDepthState(Settings.EnableWrite, Settings.DepthCompareFunction);

            if (Settings.StencilSettings.overrideStencilState)
                _visionBlackWorldRenderPass.SetStencilState(Settings.StencilSettings.stencilReference,
                    Settings.StencilSettings.stencilCompareFunction, Settings.StencilSettings.passOperation,
                    Settings.StencilSettings.failOperation, Settings.StencilSettings.zFailOperation);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera.TryGetComponent(out VisionCamera _))
                renderer.EnqueuePass(_visionBlackWorldRenderPass);
        }
    }
}
