using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace VolumetricCloud.Scripts.Runtime.Feature
{
    public class VolumetricCloudFeature : ScriptableRendererFeature
    {
        public CustomSettings settings = new CustomSettings();
        private VolumetricCloudPass m_ScriptablePass;

        public override void Create()
        {
            m_ScriptablePass = new VolumetricCloudPass();
            m_ScriptablePass.material = settings.cloudMaterial;
            m_ScriptablePass.renderPassEvent = settings.renderPassEvent;
        }
        
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.cloudMaterial != null &&
                (renderingData.cameraData.cameraType == CameraType.Game || renderingData.cameraData.cameraType == CameraType.SceneView))
            {
                renderer.EnqueuePass(m_ScriptablePass);
            }
        }
        
        [System.Serializable]
        public class CustomSettings
        {
            public Material cloudMaterial;
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        }
        
        class VolumetricCloudPass : ScriptableRenderPass
        {
            public Material material;

            private class PassData
            {
                public Material material;
                public Matrix4x4 invVP; // 存储当前相机的逆矩阵
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null) return;
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                using (var builder = renderGraph.AddRasterRenderPass<PassData>("VolumetricCloudPass", out var passData))
                {
                    passData.material = material;
                    
                    //计算好当前相机的矩阵，存入 passData
                    Matrix4x4 viewMat = cameraData.GetViewMatrix();
                    Matrix4x4 projMat = cameraData.GetProjectionMatrix();
                    passData.invVP = (projMat * viewMat).inverse;

                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                    builder.AllowGlobalStateModification(true);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        context.cmd.SetGlobalMatrix("_CameraInvVP", data.invVP);
                        Blitter.BlitTexture(context.cmd, new Vector2(1, 1), data.material, 0);
                    });
                }
            }

            // 兼容非 RenderGraph 的老管线
            [System.Obsolete]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (material == null) return;
                
                Matrix4x4 viewMat = renderingData.cameraData.GetViewMatrix();
                Matrix4x4 projMat = renderingData.cameraData.GetProjectionMatrix();
                Matrix4x4 invVP = (projMat * viewMat).inverse;

                CommandBuffer cmd = CommandBufferPool.Get("VolumetricCloudPass");

                cmd.SetGlobalMatrix("_CameraInvVP", invVP);
                CoreUtils.DrawFullScreen(cmd, material, null, 0);
                
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }
    }
}