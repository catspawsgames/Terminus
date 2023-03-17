using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terminus.Demo3
{
	public class ControllableMotor_Demo3 : VehicleControllableObject 
	{
		public VehicleControls clockwise = VehicleControls.forward;
		public VehicleControls countercc = VehicleControls.backward;
		public VehicleControls breaks = VehicleControls.breaks;
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

		public override void InputChanged()
		{
			JointMotor motor = joint.motor;
			motor.freeSpin = false;
			if (activeController.GetControlState(clockwise) && !activeController.GetControlState(countercc))
			{
				motor.force = power * powerMultiplier;
				motor.targetVelocity = reverse ? -maxSpeed * speedMultiplier : maxSpeed * speedMultiplier;
			}
			else if (!activeController.GetControlState(clockwise) && activeController.GetControlState(countercc))
			{
				motor.force = power * powerMultiplier;
				motor.targetVelocity = !reverse ? -maxSpeed * speedMultiplier : maxSpeed * speedMultiplier;
			}
			else if (autoBreak || activeController.GetControlState(breaks))
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
			

		void Update()
		{			
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