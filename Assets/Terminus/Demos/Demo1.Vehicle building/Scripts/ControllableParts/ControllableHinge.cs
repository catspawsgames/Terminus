using UnityEngine;
using System.Collections;

namespace Terminus.Demo1
{
	public class ControllableHinge : ControllablePart {

		//Inputs: 0 - clockwise, 1 - counter-cc, 2 - align

		public HingeJoint joint;
		public float power = 10;
		public float powerMultiplier = 1000;
		public float limitAngle;
		public bool autoAlign;

		protected void InputChanged()
		{
			JointSpring spring = joint.spring;
			JointLimits limits = joint.limits;
			limits.min = - limitAngle;
			limits.max = limitAngle;
			if (controls[0].pressed && !controls[1].pressed)
			{
				spring.spring = power * powerMultiplier;
				spring.targetPosition = limits.max;
			}
			else if (!controls[0].pressed && controls[1].pressed)
			{
				spring.spring = power * powerMultiplier;
				spring.targetPosition = limits.min;
			}
			else if (autoAlign || controls[2].pressed)
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


		protected override void ControlDown(int index)
		{
			InputChanged();
		}

		protected override void ControlUp(int index)
		{
			InputChanged();
		}	
	}
}