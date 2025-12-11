using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dynamite3D.RealIvy
{
	public class IvyCaster : MonoBehaviour
	{
		public IvyPreset[] ivyPresets;
		public List<IvyController> ivys;
		public IvyController prefabIvyController;

		public void CastIvyByPresetName(string presetName, Vector3 position, Quaternion rotation)
		{
			IvyPreset ivyPreset = GetPresetByName(presetName);
			CastIvy(ivyPreset, position, rotation);
		}

		public void CastIvy(IvyPreset ivyPreset, Vector3 position, Quaternion rotation)
		{
			IvyController selectedIvy = GetFreeIvy();
			if(selectedIvy == null)
			{
				IvyController ivyControllerInstance = Instantiate<IvyController>(prefabIvyController);
				ivyControllerInstance.transform.parent = transform;

				selectedIvy = ivyControllerInstance;
				ivys.Add(ivyControllerInstance);
			}

			selectedIvy.transform.position = position;
            selectedIvy.transform.rotation = rotation;
            selectedIvy.transform.Rotate(Vector3.right, -90f);

			selectedIvy.ivyParameters = ivyPreset.ivyParameters;

			selectedIvy.gameObject.SetActive(true);
			selectedIvy.StartGrowth();
		}

		public void CastRandomIvy(Vector3 position, Quaternion rotation)
		{
			int rndIdx = UnityEngine.Random.Range(0, ivyPresets.Length);
			IvyPreset selectedPreset = ivyPresets[rndIdx];

			CastIvy(selectedPreset, position, rotation);
		}

		private IvyController GetFreeIvy()
		{
			IvyController res = null;

			for(int i = 0; i < ivys.Count; i++)
			{
				if (!ivys[i].gameObject.activeSelf)
				{
					res = ivys[i];
					break;
				}
			}

			return res;
		}

		private IvyPreset GetPresetByName(string presetName)
		{
			IvyPreset res = null;

			for(int i = 0; i < ivyPresets.Length; i++)
			{
				if(ivyPresets[i].name == presetName)
				{
					res = ivyPresets[i];
					break;
				}
			}

			return res;
		}
	}
}