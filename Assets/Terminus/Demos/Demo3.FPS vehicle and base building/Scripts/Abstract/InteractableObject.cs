using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terminus.Demo3
{
	public abstract class InteractableObject : MonoBehaviour 
	{
		public float interactionDistance = 1.0f;

		public virtual string[] GetPossibleInteractions()
		{
			return new string[0];
		}

		public abstract void Interact(int interactionIndex, PlayerActionController player);
	}
}