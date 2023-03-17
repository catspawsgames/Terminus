using UnityEngine;
using System.Collections;

namespace Terminus 
{
	[System.Serializable]
	public class RigidbodyInfo2D 
	{
		public float mass;
		public float linearDrag;
		public float angularDrag;
		public float gravityScale;
		public Vector3 centerOfMass;
		public bool isKinematic;
		public RigidbodyInterpolation2D interpolation;
		public RigidbodySleepMode2D sleepingMode;
		public CollisionDetectionMode2D collisionDetection;
		public RigidbodyConstraints2D constraints;
		public Transform parent;
	}
}