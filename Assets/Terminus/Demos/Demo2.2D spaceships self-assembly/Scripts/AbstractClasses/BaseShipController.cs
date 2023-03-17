using UnityEngine;
using System.Collections;

namespace Terminus.Demo2
{
	public abstract class BaseShipController : MonoBehaviour {

		public Ship ship;
		public virtual void AttackedBy(Ship attacker, ShipAttachableBlock block, float damage)
		{
		}

		public virtual void Destroyed()
		{
		}
	}
}