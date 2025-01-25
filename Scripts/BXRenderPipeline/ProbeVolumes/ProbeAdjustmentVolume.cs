using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
	[ExecuteAlways]
	[AddComponentMenu("BXRenderPipeline/Probe Adjustment Volume")]
	public class ProbeAdjustmentVolume : MonoBehaviour, ISerializationCallbackReceiver
	{
		public void OnAfterDeserialize()
		{
			throw new System.NotImplementedException();
		}

		public void OnBeforeSerialize()
		{
			throw new System.NotImplementedException();
		}

		// Start is called before the first frame update
		void Start()
		{

		}

		// Update is called once per frame
		void Update()
		{

		}
	}
}
