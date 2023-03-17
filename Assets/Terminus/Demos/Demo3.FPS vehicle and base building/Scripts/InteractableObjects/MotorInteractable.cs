using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terminus.Demo3
{
	public class MotorInteractable : InteractableObject 
	{
		protected ControllableMotor_Demo3 motor;

		protected string[] interactions = new string[3];

		void Awake ()
		{
			motor = GetComponent<ControllableMotor_Demo3>();
		}

		public override string[] GetPossibleInteractions ()
		{
			interactions[0] = "Change rotation control (current : " + motor.clockwise.ToString() + "/" + motor.countercc.ToString()+")";
			//interactions[1] = "Change rotation direction (current : " + (motor.reverse ? "normal" : "reverse") + ")";
			interactions[1] = "Change break control (current : " + motor.breaks + ")";
			interactions[2] = motor.autoBreak ? "Disable auto-breaks" : "Enable auto-breaks";
			return interactions;
		}

		public override void Interact (int interactionIndex, PlayerActionController player)
		{
			switch (interactionIndex)
			{
			case 0:
				switch (motor.clockwise)
				{
				case VehicleControls.forward:
					motor.clockwise = VehicleControls.backward;
					motor.countercc = VehicleControls.forward;
					break;
				case VehicleControls.backward:
					motor.clockwise = VehicleControls.left;
					motor.countercc = VehicleControls.right;
					break;
				case VehicleControls.left:
					motor.clockwise = VehicleControls.right;
					motor.countercc = VehicleControls.left;
					break;
				case VehicleControls.right:
					motor.clockwise = VehicleControls.up;
					motor.countercc = VehicleControls.down;
					break;
				case VehicleControls.up:
					motor.clockwise = VehicleControls.down;
					motor.countercc = VehicleControls.up;
					break;
				case VehicleControls.down:
				default:
					motor.clockwise = VehicleControls.forward;
					motor.countercc = VehicleControls.backward;
					break;
				}
				break;
			case 1:
				switch (motor.breaks)
				{
				case VehicleControls.breaks:
					motor.breaks = VehicleControls.action1;
					break;
				case VehicleControls.action1:
					motor.breaks = VehicleControls.action2;
					break;
				case VehicleControls.action2:
					motor.breaks = VehicleControls.action3;
					break;
				case VehicleControls.action3:
					motor.breaks = VehicleControls.action4;
					break;
				case VehicleControls.action4:
				default:
					motor.breaks = VehicleControls.breaks;
					break;				
				}
				break;
			case 2:
				motor.autoBreak = !motor.autoBreak;
				break;
			}
		}

	}
}