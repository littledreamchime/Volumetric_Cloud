using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using VolumetricCloud.Scripts.Runtime.Core;

namespace VolumetricCloud.Scripts.Runtime.Feature
{
    public class VolumetricCloudFeature : ScriptableRendererFeature
    {
        public CustomSettings settings = new CustomSettings();
        private CustomRenderPass m_ScriptablePass;

        public override void Create()
        {
            m_ScriptablePass = new VolumetricCloudPass();
            m_ScriptablePass.SetSettings(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.cloudMaterial != null &&
                (renderingData.cameraData.cameraType == CameraType.Game ||
                 renderingData.cameraData.cameraType == CameraType.SceneView))
            {
                renderer.EnqueuePass(m_ScriptablePass);
            }
        }

    }
    [System.Serializable]
    public class CustomSettings : BaseCustomSetting
    {
        public Material cloudMaterial;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }
}