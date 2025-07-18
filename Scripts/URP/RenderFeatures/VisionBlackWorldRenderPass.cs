using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace GrozaGames.TopDownVision.URP.RenderFeatures
{
    public class VisionBlackWorldRenderPass : ScriptableRenderPass
    {
        private RenderQueueType _renderQueueType;
        private FilteringSettings _filteringSettings;
        private VisionBlackWorldRenderFeature.CustomCameraSettings _cameraSettings;


        /// <summary>
        /// The override material to use.
        /// </summary>
        public Material OverrideMaterial { get; set; }

        /// <summary>
        /// The pass index to use with the override material.
        /// </summary>
        public int OverrideMaterialPassIndex { get; set; }

        /// <summary>
        /// The override shader to use.
        /// </summary>
        public Shader OverrideShader { get; set; }

        /// <summary>
        /// The pass index to use with the override shader.
        /// </summary>
        public int OverrideShaderPassIndex { get; set; }

        private readonly List<ShaderTagId> _shaderTagIdList = new List<ShaderTagId>();
        private PassData _passData;

        /// <summary>
        /// Sets the write and comparison function for depth.
        /// </summary>
        /// <param name="writeEnabled">Sets whether it should write to depth or not.</param>
        /// <param name="function">The depth comparison function to use.</param>
        public void SetDepthState(bool writeEnabled, CompareFunction function = CompareFunction.Less)
        {
            _mRenderStateBlock.mask |= RenderStateMask.Depth;
            _mRenderStateBlock.depthState = new DepthState(writeEnabled, function);
        }

        /// <summary>
        /// Sets up the stencil settings for the pass.
        /// </summary>
        /// <param name="reference">The stencil reference value.</param>
        /// <param name="compareFunction">The comparison function to use.</param>
        /// <param name="passOp">The stencil operation to use when the stencil test passes.</param>
        /// <param name="failOp">The stencil operation to use when the stencil test fails.</param>
        /// <param name="zFailOp">The stencil operation to use when the stencil test fails because of depth.</param>
        public void SetStencilState(int reference, CompareFunction compareFunction, StencilOp passOp, StencilOp failOp, StencilOp zFailOp)
        {
            var stencilState = StencilState.defaultValue;
            stencilState.enabled = true;
            stencilState.SetCompareFunction(compareFunction);
            stencilState.SetPassOperation(passOp);
            stencilState.SetFailOperation(failOp);
            stencilState.SetZFailOperation(zFailOp);

            _mRenderStateBlock.mask |= RenderStateMask.Stencil;
            _mRenderStateBlock.stencilReference = reference;
            _mRenderStateBlock.stencilState = stencilState;
        }

        private RenderStateBlock _mRenderStateBlock;

        /// <summary>
        /// The constructor for render objects pass.
        /// </summary>
        /// <param name="profilerTag">The profiler tag used with the pass.</param>
        /// <param name="renderPassEvent">Controls when the render pass executes.</param>
        /// <param name="shaderTags">List of shader tags to render with.</param>
        /// <param name="renderQueueType">The queue type for the objects to render.</param>
        /// <param name="layerMask">The layer mask to use for creating filtering settings that control what objects get rendered.</param>
        /// <param name="cameraSettings">The settings for custom cameras values.</param>
        public VisionBlackWorldRenderPass(string profilerTag, RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, int layerMask, VisionBlackWorldRenderFeature.CustomCameraSettings cameraSettings)
        {
            profilingSampler = new ProfilingSampler(profilerTag);
            Init(renderPassEvent, shaderTags, renderQueueType, layerMask, cameraSettings);
        }

        private void Init(RenderPassEvent renderPassEventParam, string[] shaderTags, RenderQueueType renderQueueType, int layerMask, VisionBlackWorldRenderFeature.CustomCameraSettings cameraSettings)
        {
            _passData = new PassData();

            renderPassEvent = renderPassEventParam;
            _renderQueueType = renderQueueType;
            OverrideMaterial = null;
            OverrideMaterialPassIndex = 0;
            OverrideShader = null;
            OverrideShaderPassIndex = 0;
            
            var renderQueueRange = (renderQueueType == RenderQueueType.Transparent)
                ? RenderQueueRange.transparent
                : RenderQueueRange.opaque;
            
            _filteringSettings = new FilteringSettings(renderQueueRange, layerMask);

            if (shaderTags is { Length: > 0 })
            {
                foreach (var tag in shaderTags)
                    _shaderTagIdList.Add(new ShaderTagId(tag));
            }
            else
            {
                _shaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
                _shaderTagIdList.Add(new ShaderTagId("UniversalForward"));
                _shaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
            }

            _mRenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            _cameraSettings = cameraSettings;
        }

        private static void ExecutePass(PassData passData, RasterCommandBuffer cmd, RendererList rendererList, bool isYFlipped)
        {
            var camera = passData.CameraData.camera;

            // In case of camera stacking we need to take the viewport rect from base camera
            var pixelRect = passData.CameraData.pixelRect;
            var cameraAspect = (float)pixelRect.width / (float)pixelRect.height;

            if (passData.CameraSettings.OverrideCamera)
            {
                if (passData.CameraData.xr.enabled)
                {
                    Debug.LogWarning("RenderObjects pass is configured to override camera matrices. While rendering in stereo camera matrices cannot be overridden.");
                }
                else
                {
                    var projectionMatrix = Matrix4x4.Perspective(passData.CameraSettings.CameraFieldOfView, cameraAspect, camera.nearClipPlane, camera.farClipPlane);
                    projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, isYFlipped);

                    var viewMatrix = passData.CameraData.GetViewMatrix();
                    var cameraTranslation = viewMatrix.GetColumn(3);
                    viewMatrix.SetColumn(3, cameraTranslation + passData.CameraSettings.Offset);

                    RenderingUtils.SetViewAndProjectionMatrices(cmd, viewMatrix, projectionMatrix, false);
                }
            }

            cmd.DrawRendererList(rendererList);

            if (passData.CameraSettings.OverrideCamera && passData.CameraSettings.RestoreCamera) 
                RenderingUtils.SetViewAndProjectionMatrices(cmd, passData.CameraData.GetViewMatrix(), GL.GetGPUProjectionMatrix(passData.CameraData.GetProjectionMatrix(0), isYFlipped), false);
        }

        private class PassData
        {
            internal VisionBlackWorldRenderFeature.CustomCameraSettings CameraSettings;
            internal RenderPassEvent RenderPassEvent;

            internal TextureHandle Color;
            internal RendererListHandle RendererListHdl;

            internal UniversalCameraData CameraData;

            // Required for code sharing purpose between RG and non-RG.
            internal RendererList RendererList;
        }

        private void InitPassData(UniversalCameraData cameraData, ref PassData passData)
        {
            passData.CameraSettings = _cameraSettings;
            passData.RenderPassEvent = renderPassEvent;
            passData.CameraData = cameraData;
        }

        private void InitRendererLists(UniversalRenderingData renderingData, UniversalLightData lightData, ref PassData passData, ScriptableRenderContext context, RenderGraph renderGraph)
        {
            var sortingCriteria = (_renderQueueType == RenderQueueType.Transparent)
                ? SortingCriteria.CommonTransparent
                : passData.CameraData.defaultOpaqueSortFlags;
            
            var drawingSettings = RenderingUtils.CreateDrawingSettings(_shaderTagIdList, renderingData, passData.CameraData, lightData, sortingCriteria);
            drawingSettings.overrideMaterial = OverrideMaterial;
            drawingSettings.overrideMaterialPassIndex = OverrideMaterialPassIndex;
            drawingSettings.overrideShader = OverrideShader;
            drawingSettings.overrideShaderPassIndex = OverrideShaderPassIndex;

            RenderingUtils.CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings, _filteringSettings, _mRenderStateBlock, ref passData.RendererListHdl);
        }

        /// <inheritdoc />
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();

            using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler);
            
            var resourceData = frameData.Get<UniversalResourceData>();

            InitPassData(cameraData, ref passData);

            passData.Color = resourceData.activeColorTexture;
            builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
            builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);

            var mainShadowsTexture = resourceData.mainShadowsTexture;
            var additionalShadowsTexture = resourceData.additionalShadowsTexture;

            if (mainShadowsTexture.IsValid())
                builder.UseTexture(mainShadowsTexture, AccessFlags.Read);

            if (additionalShadowsTexture.IsValid())
                builder.UseTexture(additionalShadowsTexture, AccessFlags.Read);

            var dBufferHandles = resourceData.dBuffer;
            foreach (var dBuffer in dBufferHandles)
            {
                if (dBuffer.IsValid())
                    builder.UseTexture(dBuffer, AccessFlags.Read);
            }

            var ssaoTexture = resourceData.ssaoTexture;
            if (ssaoTexture.IsValid())
                builder.UseTexture(ssaoTexture, AccessFlags.Read);

            InitRendererLists(renderingData, lightData, ref passData, default, renderGraph);
            builder.UseRendererList(passData.RendererListHdl);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
            {
                var isYFlipped = data.CameraData.IsRenderTargetProjectionMatrixFlipped(data.Color);
                ExecutePass(data, rgContext.cmd, data.RendererListHdl, isYFlipped);
            });
        }
    }
}