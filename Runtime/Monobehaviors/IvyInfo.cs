using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace TeamCrescendo.ProceduralIvy
{
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
            Assert.IsTrue(infoPool.ivyContainer.ivyGO == gameObject);
        }
    }
}