using System;
using System.Collections.Generic;
using UnityEngine;

namespace VolumetricCloud.Scripts.Runtime.Core
{
    public struct CloudVolumeData
    {
        public Vector3 boundsMin;
        public float density;

        public Vector3 boundsMax;
        public float padding;

        public Vector4 color;
    }

    [ExecuteAlways]
    public class CloudVolume : MonoBehaviour
    {
        //所有的CloudVolume：
        [HideInInspector] public static List<CloudVolume> ActiveVolumes = new List<CloudVolume>();

        private void OnEnable()
        {
            if (!ActiveVolumes.Contains(this)) ActiveVolumes.Add(this);
        }

        private void OnDisable()
        {
            ActiveVolumes.Remove(this);
        }

        [Header("Settings")]
        public Vector3 volumeSize = new Vector3(10, 10, 10);
        public Color cloudColor = new Color(1f, 1f, 1f, 0.5f);
        [Range(0, 2)] 
        public float densityMultipler = 0.1f;
        public float maxOpacityDistance = 5.0f;
        [Header("Editor")] 
        public Color lineColor = Color.cyan;
        

        public CloudVolumeData GetCloudData()
        {
            Vector3 halfSize = volumeSize * 0.5f;
            float baseExtinction = 4.605f / (maxOpacityDistance + 0.0001f);
            var data = new CloudVolumeData()
            {
                boundsMin = transform.position - halfSize,
                density = densityMultipler * baseExtinction,
                boundsMax = transform.position + halfSize,
                padding = 0,
                color = cloudColor,
            };
            return data;
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = lineColor;
            Gizmos.DrawWireCube(transform.position, volumeSize);
        }
    }
}