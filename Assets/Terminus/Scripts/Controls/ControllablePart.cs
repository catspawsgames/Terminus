using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Terminus
{
	/// <summary>
	/// Allows to control attachable part(<see cref="TerminusObject"/>) at Termins Demo1. Also works with <see cref="BuilderUIHandler"/> to allow player to rebind controls. This is abstract class, real functionality are contained in child classes.
	/// </summary>
	[DisallowMultipleComponent]
	public abstract class ControllablePart : MonoBehaviour {

		/// <summary>
		/// List of possible control inputs.
		/// </summary>
		public List<ControlMonitor> controls;

        /// <summary>
        /// A class for a single controllable function inside a part
        /// </summary>
		[System.Serializable]
		public class ControlMonitor
		{
			/// <summary>
			/// Name for displaying parameter in UI.
			/// </summary>            
			public string name;
			/// <summary>
			/// Keycode to monitor.
			/// </summary>
			public KeyCode key;
			/// <summary>
			/// Alternative keycode to monitor.
			/// </summary>
			public KeyCode altKey;
            /// <summary>
            /// Should <see cref="ControllablePart.ControlMonitor.ControlUp"/> event be fired when <see cref="ControllablePart.ControlMonitor.key"/> or <see cref="ControllablePart.ControlMonitor.altKey"/> are lifted up?
            /// </summary>
            public bool monitorUp;
            /// <summary>
            /// Should <<see cref="ControllablePart.ControlMonitor.ControlDown"/> event be fired when <see cref="ControllablePart.ControlMonitor.key"/> or <see cref="ControllablePart.ControlMonitor.altKey"/> are pressed down?
            /// </summary>
            public bool monitorDown;
            /// <summary>
            /// Should <see cref="ControllablePart.ControlMonitor.ControlPressed"/> event be fired every frame when <see cref="ControllablePart.ControlMonitor.key"/> or <see cref="ControllablePart.ControlMonitor.altKey"/> are pressed?
            /// </summary>
            public bool monitorPressed;
			/// <summary>
			/// True when control is down, false otherwise. Works independently from <see cref="ControllablePart.ControlMonitor.monitorPressed"/>.
			/// </summary>
			public bool pressed;
		}

		/// <summary>
		/// Pressed down control. Can be called to simulate control input without key being actually pressed.
		/// </summary>
		/// <param name="index">Index of control from see cref="ControllablePart.ControlMonitor"/>.</param>
		public void KeyDown(int index)
		{
			controls[index].pressed = true;
			if (controls[index].monitorDown)
				ControlDown(index);
		}

		/// <summary>
		/// Lifts control up. Can be called to simulate control input without key being actually pressed.
		/// </summary>
		/// <param name="index">Index of control from see cref="ControllablePart.ControlMonitor"/>.</param>
		public void KeyUp(int index)
		{
			controls[index].pressed = false;
			if (controls[index].monitorUp)
				ControlUp(index);
		}

		protected virtual void ControlDown(int index)
		{

		}

		protected virtual void ControlUp(int index)
		{

		}

		protected virtual void ControlPressed(int index)
		{

		}

		protected virtual void Update()
		{
			for (int i = 0; i < controls.Count; i++)
			{
				if ((Input.GetKeyDown(controls[i].key) || Input.GetKeyDown(controls[i].altKey)))
				{
					controls[i].pressed = true;
					if (controls[i].monitorDown)
						ControlDown(i);
				}

				if ((Input.GetKeyUp(controls[i].key) || Input.GetKeyUp(controls[i].altKey)))
				{
					if (!Input.GetKey(controls[i].key) && !Input.GetKey(controls[i].altKey))
					{
						controls[i].pressed = false;
						if (controls[i].monitorUp)
							ControlUp(i);
					}
				}

				if (controls[i].monitorPressed && (Input.GetKey(controls[i].key) || Input.GetKey(controls[i].altKey)))
				{
					controls[i].pressed = true;
					ControlPressed(i);
				}
			}
		}
	}
}