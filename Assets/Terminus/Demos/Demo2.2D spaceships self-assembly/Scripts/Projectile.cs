using UnityEngine;
using System.Collections;

namespace Terminus.Demo2
{
	public class Projectile : MonoBehaviour {

		public ShipAttachableBlock ownerBlock;
		public float damage;
		public float forceTranferred;
		public GameObject hitEffect;
		public float hitEffectDuration = 1;

	}
}