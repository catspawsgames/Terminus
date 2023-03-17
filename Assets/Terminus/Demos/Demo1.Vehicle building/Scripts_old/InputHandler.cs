using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Terminus.Demo1
{
	/// <summary>
	/// Handles pre-defined keystrokes input processing for Terminus Demo1. <see cref="ControllablePart"/> and its derivatives handles input for vehicle parts.
	/// </summary>
	public class InputHandler : MonoBehaviour {

		/// <summary>
		/// Active UI handler on current scene.
		/// </summary>
		public BuilderUIHandler UIHandler;
		/// <summary>
		/// Ignore user input if UI is focused on one of these InputFields.
		/// </summary>
		public List<InputField> disableControlsOnFocus;
		public KeyCode cyclePortKey = KeyCode.Tab;
		public KeyCode rotatePortKey = KeyCode.BackQuote;
		public KeyCode hideUIKey = KeyCode.F12;
		public KeyCode makeJointsBreakableOnObjectKey = KeyCode.F11;

	
		void MakeJointsBreakableForObject(TerminusObject terminusObject)
		{
			List<AttachmentInfo> attachments = terminusObject.treeRoot.treeListDown;


			for (int i = 0; i < attachments.Count; i++)
			{
				if (attachments[i].joint != null && attachments[i].joint is Joint)
				{
					((Joint)attachments[i].joint).breakForce = UIHandler.jointBreakForce;
					((Joint)attachments[i].joint).breakTorque = UIHandler.jointBreakTorque;
				}
			}
		}

		void Awake () 
		{
			if (UIHandler == null)
				UIHandler = GetComponent<BuilderUIHandler>();
		}

		void Update () 
		{
			for (int i = 0; i < disableControlsOnFocus.Count; i++)
			{
				if (disableControlsOnFocus[i].isFocused)
					return;
			}

			if (Input.GetKeyDown(KeyCode.Alpha1))		
				UIHandler.OnBuildKeyPressed(0);
			if (Input.GetKeyDown(KeyCode.Alpha2))		
				UIHandler.OnBuildKeyPressed(1);
			if (Input.GetKeyDown(KeyCode.Alpha3))		
				UIHandler.OnBuildKeyPressed(2);
			if (Input.GetKeyDown(KeyCode.Alpha4))		
				UIHandler.OnBuildKeyPressed(3);
			if (Input.GetKeyDown(KeyCode.Alpha5))		
				UIHandler.OnBuildKeyPressed(4);
			if (Input.GetKeyDown(KeyCode.Alpha6))		
				UIHandler.OnBuildKeyPressed(5);
			if (Input.GetKeyDown(KeyCode.Alpha7))		
				UIHandler.OnBuildKeyPressed(6);
			if (Input.GetKeyDown(KeyCode.Alpha8))		
				UIHandler.OnBuildKeyPressed(7);
			if (Input.GetKeyDown(KeyCode.Alpha9))		
				UIHandler.OnBuildKeyPressed(8);
			if (Input.GetKeyDown(KeyCode.Alpha0))		
				UIHandler.OnBuildKeyPressed(9);

			if (Input.GetKeyDown(KeyCode.F1))
			    UIHandler.OnTabKeyPressed(0);
			if (Input.GetKeyDown(KeyCode.F2))
				UIHandler.OnTabKeyPressed(1);
			if (Input.GetKeyDown(KeyCode.F3))
				UIHandler.OnTabKeyPressed(2);
			if (Input.GetKeyDown(KeyCode.F4))
				UIHandler.OnTabKeyPressed(3);
			if (Input.GetKeyDown(KeyCode.F5))
				UIHandler.OnTabKeyPressed(4);
			if (Input.GetKeyDown(KeyCode.F6))
				UIHandler.OnTabKeyPressed(5);



			if (Input.GetKeyDown(cyclePortKey))
				UIHandler.placer.CyclePortOnActiveObject();
			if (Input.GetKeyDown(rotatePortKey))
				UIHandler.placer.RotateActiveObject(1);

			if (Input.GetKeyDown(hideUIKey))
				UIHandler.mainCanvas.enabled = !UIHandler.mainCanvas.enabled;

			if (Input.GetKeyDown(makeJointsBreakableOnObjectKey))
				MakeJointsBreakableForObject(UIHandler.currentObject);

		}
	}
}