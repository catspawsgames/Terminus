using UnityEngine;
using System.Collections;

namespace Terminus 
{
	/// <summary>
	/// Contains information about connector-to-connector attachments. All hierarchy information inside Terminus are stored through trees of AttachmentInfos. AttachmentInfo objects usually associated with owner connector.
	/// </summary>
	[System.Serializable]
	public struct AttachmentInfo
	{
		/// <summary>
		/// Type of this attachment. Determined at the moment of attachment by port layer.
		/// </summary>
		public Types attachmentType;
		/// <summary>
		/// Connector that own this attachmentInfo.
		/// </summary>
		public Connector selfConnector;
		/// <summary>
		/// Connector opposite of attachmentInfo owner.
		/// </summary>
		public Connector otherConnector;
		/// <summary>
		/// If attachment is performed through physics joint, this field will contain joint component (regardless if it's Joint or Joint2D).
		/// </summary>
		public Component joint;
		/// <summary>
		/// If true, attachment is performed through parenting initiating TerminusObject transform to recieving TerminusObject transform. Does not exclude joint attachment, although not recommended.
		/// </summary>
		public bool hierarchyAttachment;
		/// <summary>
		/// If true, attachment is performed through merging rigidbodies of objects. Always involves parenting transforms of <see cref="TerminusObject"/>s.
		/// </summary>
		public bool weldingAttachment;
		/// <summary>
		/// If true, selfConnector initiated attachment. If false, otherConnector is initiator.
		/// </summary>
		public bool selfIsInitiator;
		/// <summary>
		/// If true, this attachment was created automatically to satisfy <see cref="TerminusObject.autoReparentToSideways"/>.
		/// </summary>
		public bool autoReparented;
		/// <summary>
		/// Original difference of positions of <see cref="AttachmentInfo.selfConnector"/> and <see cref="AttachmentInfo.otherConnector"/> in selfConnector space at the moment of attachment in localspace of otherConnector.
		/// </summary>
		public Vector3 originalPositionDifference;
		/// <summary>
		/// Original difference of rotations of <see cref="AttachmentInfo.selfConnector"/> and <see cref="AttachmentInfo.otherConnector"/> in selfConnector space at the moment of attachment in localspace of otherConnector..
		/// </summary>
		public Quaternion originalRotationDifference;

		public AttachmentInfo(Types type,Connector self, Connector other, Component connectionJoint, bool hierarchy, bool welding, bool selfInitiator, bool autoReparent, Vector3 posDiff, Quaternion rotDiff)
		{
			attachmentType = type;
			selfConnector = self;
			otherConnector = other;
			joint = connectionJoint;
			hierarchyAttachment = hierarchy;
			weldingAttachment = welding;
			selfIsInitiator = selfInitiator;
			autoReparented = autoReparent;
			originalPositionDifference = posDiff;
			originalRotationDifference = rotDiff;
		}	

		/// <summary>
		/// Possible types of attachments.
		/// </summary>
		public enum Types
		{
			/// <summary>
			/// Indicates that owner connector isn't attached at the moment.
			/// </summary>
			none = 0,
			/// <summary>
			/// selfConnector is parent of otherConnector in Terminus hierarchy.(Terminus hierarchy are separate from Unity transform hierarchy)
			/// </summary>
			parent = 1,
			/// <summary>
			/// selfConnector is child of otherConnector in Terminus hierarchy.(Terminus hierarchy are separate from Unity transform hierarchy)
			/// </summary>
			child = 2,
			/// <summary>
			/// Sideway connection isn't affecting Terminus hierarchy, but still performs usual attachment actions (Event firing and optional physics joing creating).
			/// </summary>
			sideway = 3
		}
	}
}