using System;
using UnityEngine;

namespace VolumetricCloud.Scripts.Runtime.Core
{
    [ExecuteAlways]
    public class CloudVolume : MonoBehaviour
    {
        public Vector3 volumeSize = new Vector3(10, 10, 10);
        
        private static readonly int CloudBoundsMin=Shader.PropertyToID("_CloudBoundsMin");
        private static readonly int CloudBoundsMax=Shader.PropertyToID("_CloudBoundsMax");

        private void Update()
        {
            Vector3 halfSize=volumeSize * 0.5f;
            Vector3 minBounds=transform.position-halfSize;
            Vector3 maxBounds=transform.position+halfSize;
            
            Shader.SetGlobalVector(CloudBoundsMin,minBounds);
            Shader.SetGlobalVector(CloudBoundsMax,maxBounds);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position,volumeSize);
        }
    }
}