using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using VolumetricCloud.Scripts.Runtime.Feature;

namespace VolumetricCloud.Scripts.Runtime.Core
{
        public class VolumetricCloudPass : CustomRenderPass
        {
            public Material material;

            public override void SetSettings(BaseCustomSetting settings)
            {
                var _settings = settings as CustomSettings;
                this.material = _settings.cloudMaterial;
                this.renderPassEvent = _settings.renderPassEvent;
            }
            
            private class PassData
            {
                public Material material;
                public Matrix4x4 invVP; 
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null) return;
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                using (var builder = renderGraph.AddRasterRenderPass<PassData>("VolumetricCloudPass", out var passData))
                {
                    passData.material = material;

                    Matrix4x4 viewMat = cameraData.GetViewMatrix();
                    Matrix4x4 projMat = cameraData.GetProjectionMatrix();
                    Matrix4x4 gpuProjMat = GL.GetGPUProjectionMatrix(projMat, false);
                    passData.invVP = (gpuProjMat * viewMat).inverse;

                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                    
                    if (resourceData.cameraDepthTexture.IsValid())
                    {
                        builder.UseTexture(resourceData.cameraDepthTexture);
                    }
                    
                    builder.AllowGlobalStateModification(true);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        context.cmd.SetGlobalMatrix("_CameraInvVP", data.invVP);
                        Blitter.BlitTexture(context.cmd, new Vector2(1, 1), data.material, 0);
                    });
                }
            }

            [System.Obsolete]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (material == null) return;

                Matrix4x4 viewMat = renderingData.cameraData.GetViewMatrix();
                Matrix4x4 projMat = renderingData.cameraData.GetProjectionMatrix();
                Matrix4x4 gpuProjMat = GL.GetGPUProjectionMatrix(projMat, false);
                Matrix4x4 invVP = (gpuProjMat * viewMat).inverse;

                CommandBuffer cmd = CommandBufferPool.Get("VolumetricCloudPass");

                cmd.SetGlobalMatrix("_CameraInvVP", invVP);
                CoreUtils.DrawFullScreen(cmd, material, null, 0);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }
}