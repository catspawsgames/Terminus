using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Terminus.Demo2
{
	public class ShipAttachableBlock : MonoBehaviour, IOnAfterAttachment, IOnBeforeDetachment, IOnAfterDetachment {

		public SpriteRenderer spriteRend;
		public float maxHealth;
		public float health;
		public float regen = 0;
		public GameObject deathEffect;
		public AudioClip deathAudioClip;
		public float deathEffectDuration = 1;
		public Color color;
		public Gradient healthColorGradient;
		public bool cockpit;
		public List<ShipThruster> thrusters;
		public List<ShipCannon> cannons;

		[System.NonSerialized]
		public Ship currentShip;
		[System.NonSerialized]
		public TerminusObject selfTO;
		public Rigidbody2D rbody;

		protected float detachForcePerMass = 10f;

		public void DetachPart()
		{

			AttachmentInfo parent = selfTO.parent;
			TerminusObject[] children = selfTO.children.Select(rec => rec.otherConnector.owner).ToArray();
			for (int i = 0; i < children.Length; i++)
			{
				children[i].GetComponent<ShipAttachableBlock>().DetachPart();
			}
			if (!cockpit && parent.attachmentType != AttachmentInfo.Types.none)
			{
				Vector3 detachVect = parent.selfConnector.globalRotation * -Vector3.forward;
				Vector3 detachPos =  parent.selfConnector.globalPosition;
				Ship ship = currentShip;
				selfTO.DetachFromParent();
				selfTO.mode = TerminusObject.Modes.being_attached;
				Rigidbody2D rbody = selfTO.GetComponent<Rigidbody2D>();
				rbody.transform.position += detachVect * 0.2f;
				rbody.velocity = ship.GetComponent<ShipAttachableBlock>().rbody.GetPointVelocity(rbody.transform.position);
				rbody.AddForceAtPosition(Utils.XY(detachVect) * detachForcePerMass * rbody.mass, Utils.XY(detachPos),ForceMode2D.Impulse);
			}
		}

		public void Damage(float hitpoints)
		{
			health -= hitpoints;
			spriteRend.color = Color.Lerp(color,healthColorGradient.Evaluate((maxHealth-health)/maxHealth),(maxHealth-health)/maxHealth);
			if (health <= 0)
			{
				if (deathEffect != null)
				{
					GameObject deathObj = (GameObject)Instantiate(deathEffect);
					deathObj.transform.position = transform.position;
					Destroy(deathObj,deathEffectDuration);
				}
				if (deathAudioClip != null)
					AudioSource.PlayClipAtPoint(deathAudioClip,transform.position);
				DetachPart();
				if (cockpit && currentShip != null)
					currentShip.Destroyed();
				Destroy(gameObject);
			}
		}

		void Awake()
		{
			selfTO = GetComponent<TerminusObject>();
			if (rbody == null)
				rbody = GetComponent<Rigidbody2D>();
			currentShip = selfTO.treeRoot.GetComponent<Ship>();
			for (int i = 0; i < cannons.Count; i++)
				cannons[i].ownerBlock = this;
			for (int i = 0; i < thrusters.Count; i++)
				thrusters[i].ownerBlock = this;
		}

		void Start()
		{
			Damage(0);
		}

		public void OnAfterAttachment(AttachmentInfo attInfo)
		{
			if (!cockpit && attInfo.selfIsInitiator)
			{
				currentShip = attInfo.selfConnector.owner.treeRoot.GetComponent<Ship>();
				rbody = currentShip.GetComponent<Rigidbody2D>();
				if (cannons != null)
					currentShip.cannons.AddRange(cannons);
				if (thrusters != null)
					currentShip.thrusters.AddRange(thrusters);
				currentShip.RecalculateThrusterLists();
			}
		}

        public void OnBeforeDetachment(AttachmentInfo attInfo)
		{
			if (!cockpit && attInfo.selfIsInitiator && currentShip != null)
			{
				for (int i = 0; i < cannons.Count; i++)
					currentShip.cannons.Remove(cannons[i]);
				for (int i = 0; i < thrusters.Count; i++)
					currentShip.thrusters.Remove(thrusters[i]);
				currentShip.RecalculateThrusterLists();
			}
		}

        public void OnAfterDetachment(AttachmentInfo attInfo)
		{
			if (!cockpit && attInfo.selfIsInitiator)
			{
				currentShip = null;
				rbody = GetComponent<Rigidbody2D>();
			}
		}

		void Update()
		{
			if (regen > 0 && health != maxHealth)
			{
				health = Mathf.Clamp(health + regen * Time.deltaTime,0,maxHealth);
				Damage(0);
			}
		}


		void OnTriggerEnter2D(Collider2D projCollider)
		{
			Projectile projectile = projCollider.GetComponent<Projectile>();
			if (projectile != null)
			{
				if (projectile.forceTranferred > 0)
					selfTO.treeRoot.GetComponent<Rigidbody2D>().AddForceAtPosition(Utils.XY(projectile.transform.up * projectile.forceTranferred),
					                                                               Utils.XY((transform.position + projCollider.transform.position)/2),
					                                                               ForceMode2D.Impulse);
				if (projectile.hitEffect != null)
				{
					GameObject hitObj = (GameObject)Instantiate(projectile.hitEffect);
					hitObj.transform.position = (transform.position + projCollider.transform.position)/2;
					Destroy(hitObj,projectile.hitEffectDuration);
				}
				if (currentShip != null)
					currentShip.AttackedBy(projectile.ownerBlock.currentShip,this,projectile.damage);
				Damage(projectile.damage);
				Destroy(projectile.gameObject);
			}
		}
	}
}