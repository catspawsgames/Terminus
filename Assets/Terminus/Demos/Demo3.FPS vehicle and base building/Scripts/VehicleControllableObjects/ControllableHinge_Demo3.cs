using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terminus.Demo3
{
	public class ControllableHinge_Demo3 : VehicleControllableObject {

		public VehicleControls clockwise = VehicleControls.left;
		public VehicleControls countercc = VehicleControls.right;
		public VehicleControls align = VehicleControls.action1;
		public HingeJoint joint;
		public float power = 5;
		public float powerMultiplier = 1000;
		public float limitAngle;
		public bool autoAlign;

		public override void InputChanged()
		{
			JointSpring spring = joint.spring;
			JointLimits limits = joint.limits;
			limits.min = - limitAngle;
			limits.max = limitAngle;
			if (activeController.GetControlState(clockwise) && !activeController.GetControlState(countercc))
			{
				spring.spring = power * powerMultiplier;
				spring.targetPosition = limits.max;
			}
			else if (!activeController.GetControlState(clockwise) && activeController.GetControlState(countercc))
			{
				spring.spring = power * powerMultiplier;
				spring.targetPosition = limits.min;
			}
			else if (autoAlign || activeController.GetControlState(align))
			{
				spring.spring = power * powerMultiplier;
				spring.targetPosition = 0;
			}
			else
			{
				spring.spring = 0;
				spring.targetPosition = 0;
			}
			joint.limits = limits;
			joint.useLimits = true;
			joint.spring = spring;
			joint.useSpring = true;
		}
	}
}