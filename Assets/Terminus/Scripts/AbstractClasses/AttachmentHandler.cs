using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Terminus 
{
	/// <summary>
	/// Handles changing several component parameters when attached to <see cref="TerminusObject"/> that is being placed by <see cref="Placer"/> or similiar process.
	/// </summary>
	/// <remarks>
	/// This is empty base class, real functionality is contained inside derivative <see cref="StandardAttachmentHandler"/>.
	/// </remarks>
	/// /// <seealso cref="StandardAttachmentHandler"/> 
	[DisallowMultipleComponent]
	public abstract class AttachmentHandler : MonoBehaviour {

		[SerializeField]
		protected bool inAttachingModeP;

		/// <summary>
		/// If true, <see cref="AttachmentHandler.affectedColliders"/> gameObjects layers will be changed to <see cref="AttachmentHandler.newLayer"/> and back when <see cref="TerminusObject"/> is being placed.
		/// </summary>
		public bool overrideColliderLayers = true;
		/// <summary>
		/// If <see cref="AttachmentHandler.overrideColliderLayers"/> set to true, this will be new layer for <see cref="AttachmentHandler.affectedColliders"/> gameObjects when <see cref="TerminusObject"/>  is being placed.
		/// </summary>
		public int newLayer = 2;

		/// <summary>
		/// Returns true if <see cref="TerminusObject"/> is being placed at the moment.
		/// </summary>
		public bool inAttachingMode
		{
			get
			{
				return inAttachingModeP;
			}
		}

		protected TerminusObject owner;

		/// <summary>
		/// Renderers belonging to owner <see cref="TerminusObject"/> that should be manipulated when this component is being placed.
		/// </summary>
		public List<Renderer> affectedRenderers;
		/// <summary>
		/// Colliders belonging to owner <see cref="TerminusObject"/> that should be manipulated when this component is being placed.
		/// </summary>
		public List<Collider> affectedColliders;
		/// <summary>
		/// Colliders2D belonging to owner <see cref="TerminusObject"/> that should be manipulated when this component is being placed.
		/// </summary>
		public List<Collider2D> affectedColliders2D;
		/// <summary>
		/// Rigidbodies belonging to owner <see cref="TerminusObject"/> that should be manipulated when this component is being placed.
		/// </summary>
		public List<Transform> affectedRigidbodies;
		/// <summary>
		/// Rigidbodies2D belonging to owner <see cref="TerminusObject"/> that should be manipulated when this component is being placed.
		/// </summary>
		public List<Transform> affectedRigidbodies2D;

		/// <summary>
		/// Called when <see cref="TerminusObject"/> begins being in attaching mode
		/// </summary>
		public virtual void OnAttachmentStart()
		{

		}

		//Called when object is attached
		/// <summary>
		/// Called when <see cref="TerminusObject"/> stops being in attaching mode
		/// </summary>
		public virtual void OnAttached()
		{

		}

		/// <summary>
		/// Called every update in which this <see cref="TerminusObject"/> placement is valid, returns if it still valid after checks done by this component 
		/// </summary>
		/// <remarks>
		/// Called by <see cref="Placer"/> 
		/// </remarks>
		/// <seealso cref="AttachmentHandler.InvalidPlacementUpdate"/>
		public virtual bool ValidPlacementUpdate()
		{
			return true;
		}

		/// <summary>
		/// Called every update in which this <see cref="TerminusObject"/> placement is invalid
		/// </summary>
		/// /// <remarks>
		/// Called by <see cref="Placer"/> 
		/// </remarks>
		/// <seealso cref="AttachmentHandler.ValidPlacementUpdate"/>
		public virtual void InvalidPlacementUpdate()
		{

		}

		protected virtual void Awake()
		{
			owner = GetComponent<TerminusObject>();
		}
	}
}