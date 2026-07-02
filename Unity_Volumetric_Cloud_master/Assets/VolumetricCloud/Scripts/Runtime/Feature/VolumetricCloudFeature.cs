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
        private Material m_RuntimeMaterial;

        public override void Create()
        {
            if (m_RuntimeMaterial != null)
            {
                CoreUtils.Destroy(m_RuntimeMaterial);
            }
            if (settings.cloudMaterial == null)
            {
                return;
            }
            m_RuntimeMaterial = Instantiate(settings.cloudMaterial);
            Debug.Log("Created");
            m_ScriptablePass = new VolumetricCloudPass();
            var c = new CustomSettings()
            {
                cloudMaterial = m_RuntimeMaterial,
                renderPassEvent = this.settings.renderPassEvent
            };
            m_ScriptablePass.SetSettings(c);
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            m_ScriptablePass?.Dispose();
            if (m_RuntimeMaterial != null)
            {
                CoreUtils.Destroy(m_RuntimeMaterial);
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