using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terminus.Demo3
{
	public class ControllableCannon_Demo3 : VehicleControllableObject {

		public VehicleControls shoot;

		protected PlayerGun gun;

		public override void InputChanged()
		{
			if (activeController.GetControlState(shoot))
				gun.Fire();
		}

		void Awake()
		{
			gun = GetComponent<PlayerGun>();
		}
	}
}