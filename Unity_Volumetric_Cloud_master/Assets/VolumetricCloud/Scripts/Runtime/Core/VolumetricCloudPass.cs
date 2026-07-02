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
            private ComputeBuffer _cloudDataBuffer;
            
            //声明一个缓存数组，避免每帧 new
            private CloudVolumeData[] _cachedDataArray = new CloudVolumeData[64];

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
                public int cloudCount;
                public Vector3 globalBoundsMin;
                public Vector3 globalBoundsMax;
                public ComputeBuffer cloudBuffer;
            }
            public override void Dispose()
            {
                _cloudDataBuffer?.Release();
                _cloudDataBuffer = null;
            }
            
            void SetCloudVolumeDataArray(out int outCount, out Vector3 outGlobalMin, out Vector3 outGlobalMax)
            {
                int count = CloudVolume.ActiveVolumes.Count;
                outCount = count;

                // 如果当前云块数量超过了缓存数组的容量，才重新扩容
                if (_cachedDataArray == null || _cachedDataArray.Length < count)
                {
                    _cachedDataArray = new CloudVolumeData[Mathf.NextPowerOfTwo(Mathf.Max(64, count))];
                }

                Vector3 globalMin = Vector3.positiveInfinity;
                Vector3 globalMax = Vector3.negativeInfinity;

                for (int i = 0; i < count; i++)
                {
                    _cachedDataArray[i] = CloudVolume.ActiveVolumes[i].GetCloudData();
                    globalMin = Vector3.Min(globalMin, _cachedDataArray[i].boundsMin);
                    globalMax = Vector3.Max(globalMax, _cachedDataArray[i].boundsMax);
                }
                
                outGlobalMin = globalMin;
                outGlobalMax = globalMax;

                if (_cloudDataBuffer == null || _cloudDataBuffer.count != count)
                {
                    _cloudDataBuffer?.Release();
                    _cloudDataBuffer = new ComputeBuffer(count, 48);
                }
                
                // 核心：使用 SetData 的重载，只上传缓存数组中前 count 个有效数据
                _cloudDataBuffer.SetData(_cachedDataArray, 0, 0, count);
            }
            
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null || CloudVolume.ActiveVolumes.Count == 0) return;
                
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                
                // [修改3]：适配 out 参数接收
                SetCloudVolumeDataArray(out int cloudCount, out Vector3 globalMin, out Vector3 globalMax);
                
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("VolumetricCloudPass", out var passData))
                {
                    passData.material = material;
                    passData.cloudCount = cloudCount;
                    passData.globalBoundsMin = globalMin;
                    passData.globalBoundsMax = globalMax;
                    passData.cloudBuffer = _cloudDataBuffer;

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
                        data.material.SetBuffer("_CloudVolumes", data.cloudBuffer); 
                        data.material.SetInt("_CloudVolumeCount", data.cloudCount);
                        data.material.SetVector("_GlobalBoundsMin", data.globalBoundsMin);
                        data.material.SetVector("_GlobalBoundsMax", data.globalBoundsMax);
                        data.material.SetMatrix("_CameraInvVP", data.invVP);
                        Blitter.BlitTexture(context.cmd, new Vector2(1, 1), data.material, 0);
                    });
                }
            }

            //老版本兼容
            [System.Obsolete]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (material == null || Core.CloudVolume.ActiveVolumes.Count == 0) return;

                CommandBuffer cmd = CommandBufferPool.Get("VolumetricCloudPass");

                //out 参数接收
                SetCloudVolumeDataArray(out int cloudCount, out Vector3 globalMin, out Vector3 globalMax);

                // 2. 计算矩阵
                Matrix4x4 viewMat = renderingData.cameraData.GetViewMatrix();
                Matrix4x4 projMat = renderingData.cameraData.GetProjectionMatrix();
                Matrix4x4 gpuProjMat = GL.GetGPUProjectionMatrix(projMat, false);
                Matrix4x4 invVP = (gpuProjMat * viewMat).inverse;

                // 3. 局部传参 (极其干净，不污染全局)
                material.SetBuffer("_CloudVolumes", _cloudDataBuffer);
                material.SetInt("_CloudVolumeCount", cloudCount);
                material.SetVector("_GlobalBoundsMin", globalMin);
                material.SetVector("_GlobalBoundsMax", globalMax);
                material.SetMatrix("_CameraInvVP", invVP);

                // 4. 执行全屏绘制
                CoreUtils.DrawFullScreen(cmd, material, null, 0);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }
}