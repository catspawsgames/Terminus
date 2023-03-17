using UnityEngine;
using System.Collections;
using Terminus;

namespace Terminus.Demo1
{   
	public class ControllableMotor : ControllablePart {

		public AnimationCurve pitchCurve;
		public AnimationCurve volumeCurve;
		public AudioSource audioSource;
		public HingeJoint joint;
		public float maxSpeed = 50;
		public float power = 50;
		public float powerMultiplier = 100;
		public float speedMultiplier = 100;
		public float maxVelocityForEffectsPurposes = 1000;        
		public bool reverse;        
		public bool autoBreak;

		protected void InputChanged()
		{
			JointMotor motor = joint.motor;
			motor.freeSpin = false;
			if (controls[0].pressed && !controls[1].pressed)
			{
				motor.force = power * powerMultiplier;
				motor.targetVelocity = reverse ? -maxSpeed * speedMultiplier : maxSpeed * speedMultiplier;
			}
			else if (!controls[0].pressed && controls[1].pressed)
			{
				motor.force = power * powerMultiplier;
				motor.targetVelocity = !reverse ? -maxSpeed * speedMultiplier : maxSpeed * speedMultiplier;
			}
			else if (autoBreak || controls[2].pressed)
			{
				motor.force = power * powerMultiplier;
				motor.targetVelocity = 0;
			}
			else
			{
				motor.force = 0;
				motor.targetVelocity = 0;
			}
			joint.motor = motor;
			joint.useMotor = true;
		}


		protected override void ControlDown(int index)
		{
			InputChanged();
		}

		protected override void ControlUp(int index)
		{
			InputChanged();
		}

		protected override void Update()
		{
			base.Update();
			float velocity = Mathf.Abs(joint.velocity);
			if (velocity < 1)
				audioSource.mute = true;
			else
			{
				audioSource.mute = false;
				audioSource.volume = volumeCurve.Evaluate(velocity/maxVelocityForEffectsPurposes);
				audioSource.pitch = pitchCurve.Evaluate(velocity/maxVelocityForEffectsPurposes);
			}
		}
	}
}