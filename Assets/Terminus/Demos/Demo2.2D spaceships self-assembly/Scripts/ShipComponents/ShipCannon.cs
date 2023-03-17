using UnityEngine;
using System.Collections;

namespace Terminus.Demo2
{
	public class ShipCannon : MonoBehaviour {

		public GameObject projectile;
		public float projectileLifetime = 1;
		public Transform projectileInitPosition;
		public float force = 50;
		public float rechargeRate = 0.1f;
		public float inaccuracy;
		public float recoil = 50;
		public AudioClip audioClip;
		public Vector2 pitchLimits;


		[System.NonSerialized]
		public ShipAttachableBlock ownerBlock;
		protected float lastFireTime;
		protected AudioSource audioSource;

		


		public void Fire()
		{
			if ((Time.time - lastFireTime) >= rechargeRate)
			{
				if (audioSource != null)
				{
					audioSource.pitch = UnityEngine.Random.Range(pitchLimits.x,pitchLimits.y);
					audioSource.PlayOneShot(audioClip);
				}
				GameObject obj = Instantiate(projectile);
				obj.GetComponent<Projectile>().ownerBlock = ownerBlock;
				obj.transform.position = projectileInitPosition.position;
				obj.transform.rotation = projectileInitPosition.rotation;

				Vector2 inaccuracyVector = Vector2.zero;

				if (inaccuracy > 0)
					inaccuracyVector = new Vector2(UnityEngine.Random.Range(-inaccuracy,inaccuracy),0);

				Rigidbody2D objRbody = obj.GetComponent<Rigidbody2D>();
				objRbody.velocity = ownerBlock.rbody.GetPointVelocity(projectileInitPosition.position);
				Vector2 direction = (Utils.XY(obj.transform.up) + inaccuracyVector);
				objRbody.AddForce(force * direction,ForceMode2D.Impulse);
				if (recoil > 0)
					ownerBlock.rbody.AddForce(-recoil * direction,ForceMode2D.Impulse);

				Destroy(obj,projectileLifetime);

				lastFireTime = Time.time;
			}
		}

		void Awake()
		{
			audioSource = GetComponent<AudioSource>();
		}
	}
}
