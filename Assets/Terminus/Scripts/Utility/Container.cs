using UnityEngine;
using System.Collections;

namespace Terminus 
{
	/// <summary>
	/// Component that's placed on <see cref="TerminusObject.container"/> gameObject.
	/// Containers mainly used to place all interconnected <see cref="TerminusObject"/>s under one transform in Unity hierarchy when these TerminusObjects are attached only via physics joints.
	/// This allows manipulation of position and rotation of these TerminusObjects as a whole on transform level without involving physics engine.
	/// </summary>
	public class Container : MonoBehaviour {

		/// <summary>
		/// Roor <see cref="TerminusObject"/> this container were generated for.
		/// </summary>
		public TerminusObject terminusObject;

		/// <summary>
		/// Set to true when container is being destroyed.
		/// </summary>
		public bool destroyFlag;
	}
}