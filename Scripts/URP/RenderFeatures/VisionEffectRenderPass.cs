using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace GrozaGames.TopDownVision.URP.RenderFeatures
{
    internal class VisionEffectRenderPass : ScriptableRenderPass
    {
        public static readonly int BlitTexture = Shader.PropertyToID("_BlitTexture");
        public static readonly int BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");

        private Material m_Material;
        private int m_PassIndex;
        private bool m_FetchActiveColor;
        private bool m_BindDepthStencilAttachment;
        private RTHandle m_CopiedColor;

        private static MaterialPropertyBlock s_SharedPropertyBlock = new MaterialPropertyBlock();

        public VisionEffectRenderPass(string passName)
        {
            profilingSampler = new ProfilingSampler(passName);
        }

        public void SetupMembers(Material material, int passIndex, bool fetchActiveColor, bool bindDepthStencilAttachment)
        {
            m_Material = material;
            m_PassIndex = passIndex;
            m_FetchActiveColor = fetchActiveColor;
            m_BindDepthStencilAttachment = bindDepthStencilAttachment;
        }

        internal void ReAllocate(RenderTextureDescriptor desc)
        {
            desc.msaaSamples = 1;
            desc.depthStencilFormat = GraphicsFormat.None;
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_CopiedColor, desc, name: "_FullscreenPassColorCopy");
        }

        public void Dispose()
        {
            m_CopiedColor?.Release();
        }

        private static void ExecuteCopyColorPass(RasterCommandBuffer cmd, RTHandle sourceTexture)
        {
            Blitter.BlitTexture(cmd, sourceTexture, new Vector4(1, 1, 0, 0), 0.0f, false);
        }

        private static void ExecuteMainPass(RasterCommandBuffer cmd, RTHandle sourceTexture, Material material, int passIndex)
        {
            s_SharedPropertyBlock.Clear();
            if (sourceTexture != null)
                s_SharedPropertyBlock.SetTexture(BlitTexture, sourceTexture);

            // We need to set the "_BlitScaleBias" uniform for user materials with shaders relying on core Blit.hlsl to work
            s_SharedPropertyBlock.SetVector(BlitScaleBias, new Vector4(1, 1, 0, 0));

            cmd.DrawProcedural(Matrix4x4.identity, material, passIndex, MeshTopology.Triangles, 3, 1, s_SharedPropertyBlock);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (cameraData.camera.cameraType == CameraType.SceneView)
                return;

            TextureHandle source, destination;

            Debug.Assert(resourcesData.cameraColor.IsValid());

            if (m_FetchActiveColor)
            {
                var targetDesc = renderGraph.GetTextureDesc(resourcesData.cameraColor);
                targetDesc.name = "_CameraColorFullScreenPass";
                targetDesc.clearBuffer = false;

                source = resourcesData.activeColorTexture;
                destination = renderGraph.CreateTexture(targetDesc);

                using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("Copy Color Full Screen", out var passData, profilingSampler))
                {
                    passData.inputTexture = source;
                    builder.UseTexture(passData.inputTexture, AccessFlags.Read);

                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                    builder.SetRenderFunc((CopyPassData data, RasterGraphContext rgContext) => { ExecuteCopyColorPass(rgContext.cmd, data.inputTexture); });
                }

                //Swap for next pass;
                source = destination;
            }
            else
            {
                source = TextureHandle.nullHandle;
            }

            destination = resourcesData.activeColorTexture;


            using (var builder = renderGraph.AddRasterRenderPass<MainPassData>(passName, out var passData, profilingSampler))
            {
                passData.material = m_Material;
                passData.passIndex = m_PassIndex;

                passData.inputTexture = source;

                if (passData.inputTexture.IsValid())
                    builder.UseTexture(passData.inputTexture, AccessFlags.Read);

                bool needsColor = (input & ScriptableRenderPassInput.Color) != ScriptableRenderPassInput.None;
                bool needsDepth = (input & ScriptableRenderPassInput.Depth) != ScriptableRenderPassInput.None;
                bool needsMotion = (input & ScriptableRenderPassInput.Motion) != ScriptableRenderPassInput.None;
                bool needsNormal = (input & ScriptableRenderPassInput.Normal) != ScriptableRenderPassInput.None;

                if (needsColor)
                {
                    Debug.Assert(resourcesData.cameraOpaqueTexture.IsValid());
                    builder.UseTexture(resourcesData.cameraOpaqueTexture);
                }

                if (needsDepth)
                {
                    Debug.Assert(resourcesData.cameraDepthTexture.IsValid());
                    builder.UseTexture(resourcesData.cameraDepthTexture);
                }

                if (needsMotion)
                {
                    Debug.Assert(resourcesData.motionVectorColor.IsValid());
                    builder.UseTexture(resourcesData.motionVectorColor);
                    Debug.Assert(resourcesData.motionVectorDepth.IsValid());
                    builder.UseTexture(resourcesData.motionVectorDepth);
                }

                if (needsNormal)
                {
                    Debug.Assert(resourcesData.cameraNormalsTexture.IsValid());
                    builder.UseTexture(resourcesData.cameraNormalsTexture);
                }

                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                if (m_BindDepthStencilAttachment)
                    builder.SetRenderAttachmentDepth(resourcesData.activeDepthTexture, AccessFlags.Write);

                builder.SetRenderFunc((MainPassData data, RasterGraphContext rgContext) => { ExecuteMainPass(rgContext.cmd, data.inputTexture, data.material, data.passIndex); });
            }
        }

        private class CopyPassData
        {
            internal TextureHandle inputTexture;
        }

        private class MainPassData
        {
            internal Material material;
            internal int passIndex;
            internal TextureHandle inputTexture;
        }
    }
}