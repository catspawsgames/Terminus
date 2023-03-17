using UnityEngine;
using System.Collections;
using Terminus;

namespace Terminus.Demo2
{
	public class ShipThruster : MonoBehaviour {

		public bool active;
		public float power = 10;
		public ParticleSystem particles;

		public ShipAttachableBlock ownerBlock;

		void Update()
		{
			if (particles != null && particles.emission.enabled != active)
			{
				ParticleSystem.EmissionModule emission = particles.emission;
				emission.enabled = active;
			}
			if (ownerBlock.currentShip == null)
				active = false;
		}

		void FixedUpdate()
		{
			if (active && isActiveAndEnabled && ownerBlock.rbody != null)
			{
				if (ownerBlock.currentShip != null && ownerBlock.currentShip.cruise)
					ownerBlock.rbody.AddForceAtPosition(transform.up * power / 3,Utils.XY(transform.position));
				else
					ownerBlock.rbody.AddForceAtPosition(transform.up * power,Utils.XY(transform.position));				
			}
		}
	}
}