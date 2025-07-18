using System;
using GrozaGames.TopDownVision.URP.MonoBehaviours;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GrozaGames.TopDownVision.URP.RenderFeatures
{
    public class VisionEffectRenderFeature : ScriptableRendererFeature
    {
        /// <summary>
        /// An injection point for the full screen pass. This is similar to the RenderPassEvent enum but limited to only supported events.
        /// </summary>
        public enum InjectionPoint
        {
            /// <summary>
            /// Inject a full screen pass before transparents are rendered.
            /// </summary>
            BeforeRenderingTransparents = RenderPassEvent.BeforeRenderingTransparents,

            /// <summary>
            /// Inject a full screen pass before post processing is rendered.
            /// </summary>
            BeforeRenderingPostProcessing = RenderPassEvent.BeforeRenderingPostProcessing,

            /// <summary>
            /// Inject a full screen pass after post processing is rendered.
            /// </summary>
            AfterRenderingPostProcessing = RenderPassEvent.AfterRenderingPostProcessing
        }

        /// <summary>
        /// Specifies at which injection point the pass will be rendered.
        /// </summary>
        public InjectionPoint injectionPoint = InjectionPoint.AfterRenderingPostProcessing;

        /// <summary>
        /// Specifies whether the assigned material will need to use the current screen contents as an input texture.
        /// Disable this to optimize away an extra color copy pass when you know that the assigned material will only need
        /// to write on top of or hardware blend with the contents of the active color target.
        /// </summary>
        public bool fetchColorBuffer = true;

        /// <summary>
        /// A mask of URP textures that the assigned material will need access to. Requesting unused requirements can degrade
        /// performance unnecessarily as URP might need to run additional rendering passes to generate them.
        /// </summary>
        public ScriptableRenderPassInput requirements = ScriptableRenderPassInput.None;

        /// <summary>
        /// The material used to render the full screen pass (typically based on the Fullscreen Shader Graph target).
        /// </summary>
        public Material passMaterial;

        /// <summary>
        /// The shader pass index that should be used when rendering the assigned material.
        /// </summary>
        public int passIndex = 0;

        /// <summary>
        /// Specifies if the active camera's depth-stencil buffer should be bound when rendering the full screen pass.
        /// Disabling this will ensure that the material's depth and stencil commands will have no effect (this could also have a slight performance benefit).
        /// </summary>
        public bool bindDepthStencilAttachment = false;

        private VisionEffectRenderPass _pass;

        /// <inheritdoc/>
        public override void Create()
        {
            _pass = new VisionEffectRenderPass(name);
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Preview
                || renderingData.cameraData.cameraType == CameraType.SceneView
                || renderingData.cameraData.cameraType == CameraType.Reflection
                || UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
                return;

            if (!renderingData.cameraData.camera.TryGetComponent(out VisionTargetCamera _))
                return;
            
            if (passMaterial == null)
            {
                Debug.LogWarningFormat("The full screen feature \"{0}\" will not execute - no material is assigned. Please make sure a material is assigned for this feature on the renderer asset.", name);
                return;
            }

            if (passIndex < 0 || passIndex >= passMaterial.passCount)
            {
                Debug.LogWarningFormat("The full screen feature \"{0}\" will not execute - the pass index is out of bounds for the material.", name);
                return;
            }

            _pass.renderPassEvent = (RenderPassEvent)injectionPoint;
            _pass.ConfigureInput(requirements);
            _pass.SetupMembers(passMaterial, passIndex, fetchColorBuffer, bindDepthStencilAttachment);

            _pass.requiresIntermediateTexture = fetchColorBuffer;

            renderer.EnqueuePass(_pass);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            _pass.Dispose();
        }
    }
}