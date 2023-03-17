using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Terminus 
{
	/// <summary>
	/// Handles changing <see cref="StateHandler.affectedRigidbodies"/> and <see cref="StateHandler.affectedRigidbodies2D"/> isKinematic properties when <see cref="TerminusObject.inAssemblyState"/> is changed.
	/// </summary>
	/// <seealso cref="StateHandler"/> 
	[DisallowMultipleComponent]
	public class StandardStateHandler : StateHandler {
		public override void ExitAssemblyState()
		{
			if (Application.isPlaying && inAssemblyState)
			{				
				inAssemblyStateP = false;
				for (int i = 0; i < affectedRigidbodies.Count; i++)
				{
					//affectedRigidbodies[i].isKinematic = owner.GetSupposedComponentState(affectedRigidbodies[i]).flag;
					Rigidbody rbody = affectedRigidbodies[i].GetComponent<Rigidbody>();
					if (rbody != null)
						rbody.isKinematic = owner.GetSupposedComponentState(affectedRigidbodies[i]).flag;
				}
				for (int i = 0; i < affectedRigidbodies2D.Count; i++)
				{
					//affectedRigidbodies2D[i].isKinematic = owner.GetSupposedComponentState(affectedRigidbodies2D[i]).flag;
					Rigidbody2D rbody2d = affectedRigidbodies2D[i].GetComponent<Rigidbody2D>();
					if (rbody2d != null)
						rbody2d.isKinematic = owner.GetSupposedComponentState(affectedRigidbodies[i]).flag;
				}
			}
		}
		
		public override void EnterAssemblyState()
		{
			if (Application.isPlaying && !inAssemblyState)
			{
				inAssemblyStateP = true;
				for (int i = 0; i < affectedRigidbodies.Count; i++)
				{
					Rigidbody rbody = affectedRigidbodies[i].GetComponent<Rigidbody>();
					if (rbody != null)
						rbody.isKinematic = true;
				}
				for (int i = 0; i < affectedRigidbodies2D.Count; i++)
				{
					Rigidbody2D rbody2d = affectedRigidbodies2D[i].GetComponent<Rigidbody2D>();
					if (rbody2d != null)
						rbody2d.isKinematic = true;
				}
			}
		}
	}
}