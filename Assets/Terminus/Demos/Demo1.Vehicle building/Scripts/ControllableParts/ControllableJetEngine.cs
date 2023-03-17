using UnityEngine;
using System.Collections;

namespace Terminus.Demo1
{
	public class ControllableJetEngine : ControllablePart {

		public TerminusObject owner;
        public AnimationCurve pitchCurve;
		public AnimationCurve volumeCurve;
		public AudioSource audioSource;
        public float maxPower = 10;
        public float powerChange = 50;
		public float powerMultiplier = 1000;
        public float maxPowerForEffectsPurposes = 50;
        public AnimationCurve emissionCurve;
        public ParticleSystem particles;

        protected float currentPower = 0;
		
        protected void FixedUpdate()
        {
			owner.mainOrWeldedRigidbody.AddForceAtPosition(transform.forward * currentPower * powerMultiplier,transform.position, ForceMode.Force);
        }

		protected override void Update()
		{
			base.Update();
            currentPower = Mathf.Clamp(controls[0].pressed ? currentPower + powerChange * Time.deltaTime : currentPower - powerChange * Time.deltaTime, 0, maxPower);
			ParticleSystem.MinMaxCurve rate = particles.emission.rateOverTime;
			rate.constant  = emissionCurve.Evaluate(currentPower / maxPowerForEffectsPurposes);
            if (currentPower == 0)
				audioSource.mute = true;
			else
			{
				audioSource.mute = false;
				audioSource.volume = volumeCurve.Evaluate(currentPower / maxPowerForEffectsPurposes);
				audioSource.pitch = pitchCurve.Evaluate(currentPower / maxPowerForEffectsPurposes);
			}
		}

		void Awake ()
		{
			owner = GetComponent<TerminusObject>();
		}
	}
}