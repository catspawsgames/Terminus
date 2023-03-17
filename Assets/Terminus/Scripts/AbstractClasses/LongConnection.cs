using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Terminus 
{
	/// <summary>
	/// Helper component that handles special type of <see cref="TerminusObject"/>s such as struts.
	/// </summary>
	/// <remarks>
	/// Long connection TerminusObject is different in its placement procedure since it requres attaching 2 <see cref="TerminusObject.connectors"/> instead of one.
	/// It also modifies itself depending on positions of its connectors.
	/// This is base class, real functionality is contained in derivatives <see cref="LongConnectionMesh"/> and <see cref="LongConnectionLRend"/>.
	/// </remarks>
	/// <seealso cref="LongConnectionMesh"/>
	/// <seealso cref="LongConnectionLRend"/>
	public abstract class LongConnection : MonoBehaviour {

		/// <summary>
		/// Use calculations in 2D space.
		/// </summary>
		public bool use2D;

		/// <summary>
		/// Offset from <see cref="Connector"/> with index = 0 from <see cref="TerminusObject.connectors"/> for generating geometry.
		/// </summary>
		public Vector3 offset1;
		/// <summary>
		/// Offset from <see cref="Connector"/> with index = 1 from <see cref="TerminusObject.connectors"/> for generating geometry.
		/// </summary>
		public Vector3 offset2;

		/// <summary>
		/// If set to true, geometry of long connection will be recalculated in real-time. If set to false, it will be calculated on the moment of attachment.
		/// </summary>
		/// <seealso cref="LongConnection.Recalculate"/>
		public bool realtimeRecalculate;

		protected TerminusObject owner;

		/// <summary>
		/// Called by <see cref="Port"/> after its been attached.
		/// </summary>
		public virtual void LongConnectionAfterAttachment(AttachmentInfo attInfo)
		{
			if (owner == null)
			{
				Awake();
				Start();
			}
			Recalculate();
		}

		/// <summary>
		/// Can be called manually to recalculate long connection geometry.
		/// </summary>
		public virtual void Recalculate()
		{
		}

		protected virtual void Awake()
		{
			owner = GetComponent<TerminusObject>();
		}

		protected virtual void Start()
		{
		}		
	}
}