using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Terminus.Demo3
{
	public class  RedirectedInteractableObject : InteractableObject {

		public InteractableObject redirectToObject;

		public override string[] GetPossibleInteractions ()
		{
			return base.GetPossibleInteractions ();
		}

		public override void Interact (int interactionIndex, PlayerActionController player)
		{
			Debug.LogError("Attempt at interaction with redirection interactable object. Something went wrong.");
		}
	}
}
