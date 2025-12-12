using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace TeamCrescendo.ProceduralIvy
{
    [AddComponentMenu("")]
    public class IvyInfo : MonoBehaviour
    {
        public InfoPool infoPool;

        public void Setup(InfoPool infoPool)
        {
            this.infoPool = infoPool;
        }

        private void OnValidate()
        {
            if (infoPool == null || infoPool.ivyContainer == null || infoPool.ivyContainer.ivyGO == null)
                return;
            
            Assert.IsTrue(infoPool.ivyContainer.ivyGO == gameObject);
        }
    }
}