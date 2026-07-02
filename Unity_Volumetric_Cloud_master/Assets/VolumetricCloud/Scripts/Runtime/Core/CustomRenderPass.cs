using UnityEngine.Rendering.Universal;

namespace VolumetricCloud.Scripts.Runtime.Core
{
    public abstract class CustomRenderPass : ScriptableRenderPass,IPass,IDispose
    {
        public abstract void SetSettings(BaseCustomSetting settings);
        public abstract void Dispose();
    }
    
    public interface IPass
    {
        public void SetSettings(BaseCustomSetting settings);
    }

    public interface IDispose
    {
        public void Dispose();
    }

    [System.Serializable]
    public abstract class BaseCustomSetting
    {
    }

}