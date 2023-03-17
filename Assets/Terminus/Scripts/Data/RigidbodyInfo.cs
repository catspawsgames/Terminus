using UnityEngine;
using System.Collections;

namespace Terminus 
{
	[System.Serializable]
	public class RigidbodyInfo 
	{
		public float mass;
		public float drag;
		public float angularDrag;
		public Vector3 centerOfMass;
		public bool isKinematic;
		public bool useGravity;
		public RigidbodyInterpolation interpolation;
		public CollisionDetectionMode collisionDetection;
		public RigidbodyConstraints constraints;
		public Transform parent;
	}
}