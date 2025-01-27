using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
	[ExecuteAlways]
	[AddComponentMenu("BXRenderPipeline/Probe Adjustment Volume")]
	public class ProbeAdjustmentVolume : MonoBehaviour, ISerializationCallbackReceiver
	{
		/// <summary>
		/// The type of shape that an adjustment volume can take
		/// </summary>
		public enum Shape
		{
			/// <summary>
			/// A Box Shape
			/// </summary>
			Box,
			/// <summary>
			/// A Sphere Shape
			/// </summary>
			Sphere
		}

		/// <summary>
		/// The shape of the adjustment volume
		/// </summary>
		[Tooltip("Select the shape used for this Probe Adjustment Volume")]
		public Shape shape = Shape.Box;

		/// <summary>
		/// The size for box shape
		/// </summary>
		[Min(0f), Tooltip("Modify the size of this Probe Adjustment Volume. This is unaffected by the GameObject's Transform's Scale property.")]
		public Vector3 size = new Vector3(1f, 1f, 1f);

		/// <summary>
		/// The size for sphere shape.
		/// </summary>
		[Min(0.0f), Tooltip("Modify the radius of this Probe Adjustment Volume. This is unaffected by the GameObject's Transform's Scale property.")]
		public float radius = 1.0f;


		/// <summary>
		/// Returns the extents of the volume
		/// </summary>
		/// <returns>The extents of the ProbeVolume</returns>
		public Vector3 GetExtents()
		{
			return size;
		}

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
