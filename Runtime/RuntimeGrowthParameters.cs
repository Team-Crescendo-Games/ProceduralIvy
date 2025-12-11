using UnityEngine;

namespace Dynamite3D.RealIvy
{
	[System.Serializable]
	public class RuntimeGrowthParameters
	{
		public float growthSpeed;
		public float lifetime;
		public bool speedOverLifetimeEnabled;
		public AnimationCurve speedOverLifetimeCurve;
		public float delay;
		public bool startGrowthOnAwake;


		public RuntimeGrowthParameters()
		{
			this.growthSpeed = 25f;
			this.lifetime = 5f;
			this.speedOverLifetimeEnabled = false;
			this.speedOverLifetimeCurve = new AnimationCurve(new Keyframe[4] { new Keyframe(0f, 0f), new Keyframe(0.2f, 1f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0f) });
			this.delay = 0f;
			this.startGrowthOnAwake = true;
		}
	}
}