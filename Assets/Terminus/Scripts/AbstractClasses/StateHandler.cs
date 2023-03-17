using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Terminus 
{
	/// <summary>
	/// Handles changing rigidbodies properties when <see cref="TerminusObject.inAssemblyState"/> is changed.
	/// </summary>
	/// <remarks>
	/// This is empty base class, real functionality is contained inside derivative <see cref="StandardStateHandler"/>.
	/// </remarks>
	/// <seealso cref="StandardStateHandler"/> 
	[DisallowMultipleComponent]
	public abstract class StateHandler : MonoBehaviour {

		[SerializeField]
		protected bool inAssemblyStateP = false;

		/// <summary>
		/// Internal assembly state. Automatically tries to duplicate <see cref="TerminusObject.inAssemblyState"/> 
		/// </summary>
		public bool inAssemblyState
		{
			get
			{
				return inAssemblyStateP;
			}
		}

		protected TerminusObject owner;

		/// <summary>
		/// Rigidbodies belonging to owner <see cref="TerminusObject"/> that should be manipulated when <see cref="TerminusObject.inAssemblyState"/> is changed.
		/// </summary>
		public List<Transform> affectedRigidbodies;
		/// <summary>
		/// Rigidbodies2D belonging to owner <see cref="TerminusObject"/> that should be manipulated when <see cref="TerminusObject.inAssemblyState"/> is changed.
		/// </summary>
		public List<Transform> affectedRigidbodies2D;

		/// <summary>
		/// Called when see cref="TerminusObject.inAssemblyState"/> set to false
		/// </summary>
		public virtual void ExitAssemblyState()
		{
		}

		/// <summary>
		/// Called when see cref="TerminusObject.inAssemblyState"/> set to false
		/// </summary>
		public virtual void EnterAssemblyState()
		{
		}

		protected virtual void Awake()
		{
			owner = GetComponent<TerminusObject>();
		}
	}
}