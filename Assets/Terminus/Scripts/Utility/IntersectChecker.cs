using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Terminus 
{
	/// <summary>
	/// Helper component placed by <see cref="StandardAttachmentHandler"/> to detect when <see cref="AttachmentHandler.affectedColliders"/> intersect with other colliders.
	/// </summary>
	public class IntersectChecker : MonoBehaviour {

		protected List<Collider> colliders;

		void OnTriggerEnter(Collider coll)
		{
			if (!colliders.Contains(coll))
				colliders.Add(coll);
		}

		void OnTriggerExit(Collider coll)
		{
			colliders.Remove(coll);
		}

		/// <summary>
		/// Returns true if colliders intersect with other colliders.
		/// </summary>
		public bool Intersects()
		{
			return colliders.Count > 0;
		}


		void OnEnable ()
		{
			colliders = new List<Collider>();
		}

	}
}