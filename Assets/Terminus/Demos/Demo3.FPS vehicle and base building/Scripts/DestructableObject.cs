using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terminus.Demo3
{
	/// <summary>
	/// Class for handling objects that can be destroyed. Can be used on multi-collider objects by placing this component on each child collider and designating main DestructableObject in redirectObject field.
	/// </summary>
	public class DestructableObject : MonoBehaviour {

		/// <summary>
		/// Current hit points of this object.
		/// </summary>
		[Tooltip("Current hit points of this object.")]
		public float currentHP = 100;
		[Tooltip("TerminusObject attached to this destructable object. Leave null to auto-detect.")]
		/// <summary>
		/// <see cref="TerminusObject"/> attached to this destructable object. Leave null to auto-detect.
		/// </summary>
		public TerminusObject terminusObj;
		/// <summary>
		/// If this component is placed on child collider, place parent DestructableObject here.
		/// </summary>
		[Tooltip("If this component is placed on child collider, place parent DestructableObject here.")]
		public DestructableObject redirectObject;
		/// <summary>
		/// This prefab will be instantiated in case of object destruction.
		/// </summary>
		[Tooltip("This prefab will be instantiated in case of object destruction.")]
		public GameObject remainsPrefab;

		/// <summary>
		/// Handles damage by this object.
		/// </summary>
		/// <param name="damage">Amount taken from <see cref="DestructableObject.currentHP"/> .</param>
		/// <param name="redirected">Used by internal redirect mechanism to intercept redirect loops.</param>
		public void TakeDamage(float damage, bool redirected = false)
		{		
			if (redirectObject != null)
			{
				if (redirected)
				{
					Debug.LogError("Destructable object redirect loop");
					return;
				}
				redirectObject.TakeDamage(damage,true);
				return;
			}

			currentHP -= damage;
			if (currentHP <= 0)
			{				
				//TerminusObject.DestroyObject(gameObject,0.01f);
				GameObject.Destroy(gameObject,0.05f);

				if (remainsPrefab != null)
					GameObject.Instantiate(remainsPrefab,transform.position,transform.rotation);
			}
		}

		void Awake()
		{
			if (terminusObj == null && redirectObject == null)
				terminusObj = GetComponent<TerminusObject>();
		}
	}
}