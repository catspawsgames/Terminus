using UnityEngine;
using System.Collections;

namespace Terminus.Demo1
{
	public class ControllableCannon : ControllablePart {

		public GameObject projectilePrefab;
		public Transform[] projectileGenPositions;
		public AudioSource cannonShotSource;
		public float power = 100;
		public float powerMultiplier = 1000;
		public float rechargeRate = 1;
		public float projectileLifetime = 0;
		public float inaccuracy;

		protected TerminusObject ownerObject;
		protected Rigidbody rbody;
		protected float lastShotTime;
        
		protected override void ControlPressed(int index)
		{
			Shoot();
		}

		protected override void ControlDown(int index)
		{
			Shoot();
		}

		protected void Shoot()
		{
			if (ownerObject.isPlaymodeClone && (Time.time - lastShotTime) > rechargeRate)
			{
				lastShotTime = Time.time;
				for (int i = 0; i < projectileGenPositions.Length; i++)
				{
					GameObject obj = Instantiate(projectilePrefab);
					obj.transform.position = projectileGenPositions[i].position;
					Vector3 inaccuracyVector = Vector3.zero;
					if (inaccuracy > 0)
						inaccuracyVector = projectileGenPositions[i].up * UnityEngine.Random.Range(-inaccuracy,inaccuracy) + projectileGenPositions[i].right * UnityEngine.Random.Range(-inaccuracy,inaccuracy);
					obj.GetComponent<Rigidbody>().AddForceAtPosition((projectileGenPositions[i].forward + inaccuracyVector) * power * powerMultiplier, projectileGenPositions[i].position , ForceMode.Impulse);
					ownerObject.mainOrWeldedRigidbody.AddForceAtPosition(-(projectileGenPositions[i].forward + inaccuracyVector) * power * powerMultiplier , projectileGenPositions[i].position ,ForceMode.Impulse);
					if (projectileLifetime > 0)
						Destroy(obj,projectileLifetime);
				}
				cannonShotSource.Play();
			}
		}

		void Awake()
		{
			ownerObject = GetComponent<TerminusObject>();
			rbody = GetComponent<Rigidbody>();
		}
	}
}
