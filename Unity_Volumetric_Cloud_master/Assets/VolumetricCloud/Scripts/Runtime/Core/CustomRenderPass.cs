using UnityEngine.Rendering.Universal;

namespace VolumetricCloud.Scripts.Runtime.Core
{
    public abstract class CustomRenderPass : ScriptableRenderPass,IPass
    {
        public abstract void SetSettings(BaseCustomSetting settings);
    }
    
    public interface IPass
    {
        public void SetSettings(BaseCustomSetting settings);
    }

    [System.Serializable]
    public abstract class BaseCustomSetting
    {
    }

}