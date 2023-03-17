using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terminus.Demo3
{
	public class ForcefieldJack : InteractableObject {

		public bool engaged = true;
		public Port jackPort;

		private string[] engagedInteraction = new string[1] {"disengage"};
		private string[] disengagedInteraction = new string[1] {"engage"};

		public override string[] GetPossibleInteractions ()
		{
			if (engaged)
				return engagedInteraction;
			else
				return disengagedInteraction;
		}

		public override void Interact (int interactionIndex, PlayerActionController player)
		{			
			if (interactionIndex == 0)
			{
				if (engaged)
				{
					engaged = false;
					if (jackPort.attachmentInfo.attachmentType != AttachmentInfo.Types.none)
						jackPort.Detach();
					jackPort.active = false;
					jackPort.gameObject.SetActive(false);
				}
				else
				{
					engaged = true;
					jackPort.gameObject.SetActive(true);
					jackPort.active = true;
				}
			}
		}
	}
}