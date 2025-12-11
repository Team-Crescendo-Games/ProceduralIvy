using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace TeamCrescendo.ProceduralIvy
{
    [AddComponentMenu("")]
    public class IvyInfo : MonoBehaviour
    {
        public IvyPreset originalPreset;
        public InfoPool infoPool;

        public void Setup(InfoPool infoPool, IvyPreset originalPreset)
        {
            this.infoPool = infoPool;
            this.originalPreset = originalPreset;
        }

        private void OnValidate()
        {
            if (infoPool == null || infoPool.ivyContainer == null || infoPool.ivyContainer.ivyGO == null)
                return;
            
            Assert.IsTrue(infoPool.ivyContainer.ivyGO == gameObject);
        }
    }
}