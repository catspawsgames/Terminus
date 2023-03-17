using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terminus.Demo3
{
	public class HingeInteractable : InteractableObject {

		protected ControllableHinge_Demo3 hinge;

		protected string[] interactions = new string[2];

		public override string[] GetPossibleInteractions ()
		{
			interactions[0] = "Change control (current : " + hinge.clockwise.ToString() + "/" + hinge.countercc.ToString()+")";
			interactions[1] = hinge.autoAlign ? "Disable auto-restore" : "Enable auto-restore";
			return interactions;
		}

		public override void Interact (int interactionIndex, PlayerActionController player)
		{
			switch (interactionIndex)
			{
			case 0:
				switch (hinge.clockwise)
				{
				case VehicleControls.forward:
					hinge.clockwise = VehicleControls.backward;
					hinge.countercc = VehicleControls.forward;
					break;
				case VehicleControls.backward:
				default:
					hinge.clockwise = VehicleControls.right;
					hinge.countercc = VehicleControls.left;
					break;
				case VehicleControls.right:
					hinge.clockwise = VehicleControls.left;
					hinge.countercc = VehicleControls.right;
					break;
				case VehicleControls.left:
					hinge.clockwise = VehicleControls.up;
					hinge.countercc = VehicleControls.down;
					break;
				case VehicleControls.up:
					hinge.clockwise = VehicleControls.down;
					hinge.countercc = VehicleControls.up;
					break;
				case VehicleControls.down:
					hinge.clockwise = VehicleControls.forward;
					hinge.countercc = VehicleControls.backward;
					break;				
				}
				break;
			case 1:
				hinge.autoAlign = !hinge.autoAlign;
				break;
			}
		}

		void Awake ()
		{
			hinge = GetComponent<ControllableHinge_Demo3>();
		}
	}
}