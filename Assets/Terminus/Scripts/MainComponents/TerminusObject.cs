using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Terminus 
{
	/// <summary>
	/// Main component of Terminus system. It combines several <see cref="Connector"/>s with transforms, rigidbodies and helper components such as <see cref="StateHandler"/>, <see cref="LongConnection"/> and <see cref="AttachmentHandler"/>. Each building block in your assembly should have one(and only one) TerminusObject component.
	/// </summary>
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(-50)]
	[AddComponentMenu("Terminus modular system/Terminus Object")]
	[HelpURL("http://scriptreference.terminus-unity.com/html/T_Terminus_TerminusObject.htm")]
	public class TerminusObject : MonoBehaviour {

		/// <summary>
		/// Excludes this <see cref="TerminusObject"/> from <see cref="Manager.updateEvent"/>.
		/// </summary>
		public bool doNotAutoUpdate;


		[SerializeField]
		protected string assemblyPartName;

		protected List<Port> attachmentPorts_Internal = new List<Port>();
		protected List<AttachmentInfo> children_Internal = new List<AttachmentInfo>();
		protected List<TerminusObject> childrenObjects_Internal = new List<TerminusObject>();
		protected List<AttachmentInfo> treeListDown_Internal = new List<AttachmentInfo>();
		protected List<TerminusObject> treeListDownObjects_Internal = new List<TerminusObject>();
		protected List<TerminusObject> weldedObjs_Internal = new List<TerminusObject>();
        
        //[HideInInspector]
        [SerializeField]
        public IOnBeforeAttachment[] cachedOnBeforeAttachmentList;
        //[HideInInspector]
        [SerializeField]
        public IOnAfterAttachment[] cachedOnAfterAttachmentList;
        [HideInInspector]
        [SerializeField]
        protected IOnBeforeDetachment[] cachedOnBeforeDetachmentList;
        [HideInInspector]
        [SerializeField]
        protected IOnAfterDetachment[] cachedOnAfterDetachmentList;

        /// <summary>
        /// Rigidbody that determines position of <see cref="TerminusObject"/>. Should be used when TerminusObject attached to transform that isn't indicative of its real position.
        /// </summary>
        /// <seealso cref="TerminusObject.mainRigidbody2D"/>
        public Rigidbody mainRigidbody;
		/// <summary>
		/// Rigidbody2D that determines position of <see cref="TerminusObject"/>. Should be used when TerminusObject attached to transform that isn't indicative of its real position.
		/// </summary>
		/// <seealso cref="TerminusObject.mainRigidbody"/>
		public Rigidbody2D mainRigidbody2D;

		/// <summary>
		/// Returns <see cref="TerminusObject.mainRigidbody"/> of <see cref="Settings.attachmentTypes.rigidbody_welding">welding</see> root object if this object is welded to something, otherwise returns <see cref="mainRigidbody"/>.
		/// </summary>
		public Rigidbody mainOrWeldedRigidbody
		{
			get
			{
				if (weldedToObjectP == null)
					return mainRigidbody;
				else
					return weldedToObjectP.mainRigidbody;
			}
		}

		/// <summary>
		/// Returns <see cref="TerminusObject.mainRigidbody2D"/> of <see cref="Settings.attachmentTypes.rigidbody_welding">welding</see> root object if this object is welded to something, otherwise returns <see cref="mainRigidbody2D"/>.
		/// </summary>
		public Rigidbody2D mainOrWeldedRigidbody2D
		{
			get
			{
				if (weldedToObjectP == null)
					return mainRigidbody2D;
				else
					return weldedToObjectP.mainRigidbody2D;
			}
		}

		/// <summary>
		/// Information about rigidbody for <see cref="Settings.attachmentTypes.rigidbody_welding"/>  attachments. Filled automatically, change at your own risk.
		/// </summary>
		public RigidbodyInfo rigidbodyInfo;

		/// <summary>
		/// Information about rigidbody2D for <see cref="Settings.attachmentTypes.rigidbody_welding"/>  attachments. Filled automatically, change at your own risk.
		/// </summary>
		public RigidbodyInfo2D rigidbodyInfo2D;

		/// <summary>
		/// When this object is <see cref="TerminusObject.inPlaymode"/>, this field contains its playmode clone.
		/// </summary>
		public TerminusObject playmodeClone;
		/// <summary>
		/// If this object <see cref="isPlaymodeClone"/>, this field contains its original.
		/// </summary>
		public TerminusObject playmodeOriginal;

		protected bool inPlaymodeP;

		/// <summary>
		/// Enters and exits playmode.
		/// When <see cref="TerminusObject"/> enters playmode, following things happen:
		/// 1) GameObject of <see cref="TerminusObject"/> is disabled.
		/// 2) Terminus creates clone of this <see cref="TerminusObject"/> that is designated as its <see cref="TerminusObject.playmodeClone"/> 
		/// 3) <see cref="TerminusObject.inAssemblyState"/> of <see cref="TerminusObject.playmodeClone"/> set to false.
		/// Exiting playmode destroys <see cref="TerminusObject.playmodeClone"/> and reverts all changes to <see cref="TerminusObject"/>.
		/// </summary>
		/// <seealso cref="Manager.globalPlaymode"/> 
		public bool inPlaymode
		{
			get
			{
				return inPlaymodeP;
			}
			set
			{
				if (inPlaymodeP != value)
				{
					if (value)
					{
						EnterPlaymode();
					}
					else
					{
						ExitPlaymode();
					}
				}
			}
		}

		protected bool isPlaymodeCloneP;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Terminus.TerminusObject"/> is playmode clone. Can be set only from true to false.
		/// </summary>
		/// <remarks>
		/// If set to false on playmode clone, calls <see cref="TerminusObject.DeclarePlaymodeNonClone"/>
		/// </remarks>
		/// <seealso cref="TerminusObject.inPlaymode"/> 
		public bool isPlaymodeClone
		{
			get
			{
				return isPlaymodeCloneP;
			}
			set
			{
				if (value != isPlaymodeCloneP)
				{
					if (value)
					{
						Debug.LogWarning(gameObject.name + "(Terminus) : Trying to set isPlaymodeClone to true. Use EnterPlaymode instead.");
					}
					else
					{
						DeclarePlaymodeNonClone();
					}
				}
			}
		}
			
		/// <summary>
		/// Unique name of assembly part associated with this <see cref="TerminusObject"/>. Used by <see cref="SerializableAssembly"/> for assembling serialized constructions.
		/// Gets protected assemblyPartName variable that can be set only on prefab from editor (or by using reflections).
		/// </summary>
		public string getPartName
		{
			get
			{
				return assemblyPartName;
			}
		}

		protected bool appQuit = false;

		protected bool destroyFlagP = false;
		/// <summary>
		/// Set to true when this object is being destroyed.
		/// </summary>
		public bool destroyFlag
		{
			get
			{
				return destroyFlagP;
			}
		}
			


		/// <summary>
		/// List of <see cref="Connector"/>s of this <see cref="TerminusObject"/>. <see cref="Connector.owner"/> of each connector in this list should be set to this TerminusObject.
		/// </summary>
		public List<Connector> connectors = new List<Connector>();
		/// <summary>
		/// List of joints connected to main rigidbody if this TerminusObject. Used for <see cref="Settings.attachmentTypes.rigidbody_welding"/> attachments.
		/// </summary>
		public List<Joint> jointsConnectedToRigidbody;
		/// <summary>
		/// List of 2D joints connected to main rigidbody if this TerminusObject. Used for <see cref="Settings.attachmentTypes.rigidbody_welding"/> attachments.
		/// </summary>
		public List<Joint2D> joints2DConnectedToRigidbody;

		[SerializeField]
		protected TerminusObject weldedToObjectP;

		/// <summary>
		/// Welding tree root this object is welded to. Null if it's not welded to anything.
		/// </summary>
		/// <seealso cref="Settings.attachmentTypes.rigidbody_welding"/>
		public TerminusObject weldedToObject
		{
			get
			{
				return weldedToObjectP;
			}
		}
				


		/// <summary>
		/// Should this <see cref="TerminusObject"/> be attached when <see cref="Port.Connect"/> is called.
		/// </summary>
		public bool attachAutomatically = false;
		/// <summary>
		/// Can multiple <see cref="Port"/>s be <see cref="Port.isCurrentAttachmentPort">attachment ports</>. Useful for parts that should snapped through multiple points, like floors and walls.
		/// </summary>
		public bool multipleAttachmentPortsAllowed = false;
		/// <summary>
		/// If this <see cref="TerminusObject"/> is destroyed, should Terminus leave its children in its hierarchy so they would be destroyed too?
		/// </summary>
		public bool destroyChildrenAlong = false;
		/// <summary>
		/// Draw gizmos of all connectors in the editor. No effect outside editor.
		/// </summary>
		public bool drawGizmos;
		/*
		/// <summary>
		/// Closest candidate for attachment. Filled when <see cref="Port.ClosestAvaliableConnector"/> on one of <see cref="Port"/>s of this <see cref="TerminusObject"/> is called.
		/// </summary>
		public Connector awaitingAttachmentTo;
		*/

		[SerializeField]
		protected AttachmentHandler attachmentHandler;
		[SerializeField]
		protected StateHandler stateHandler;
		/// <summary>
		/// Returns <see cref="AttachmentHandler"/> associated with this object.
		/// </summary>
		public AttachmentHandler getAttachmentHandler
		{
			get
			{
				return attachmentHandler;
			}
		}
		/// <summary>
		/// Returns <see cref="StateHandler"/> associated with this object.
		/// </summary>
		public StateHandler getStateHandler
		{
			get
			{
				return stateHandler;
			}
		}

		/// <summary>
		/// Set to true if this object is <see cref="LongConnection"/>-type (e.g., strut).
		/// </summary>
		public bool longConnection;
		/// <summary>
		/// Forbid this <see cref="TerminusObject"/> to participate in <see cref="AttachmentInfo.Types.sideway"/>  attachments.
		/// </summary>
		public bool doNotConnectSideways;
		/// <summary>
		/// If <see cref="TerminusObject.autoReparentToSideways"/> is true, should raycasting be performed to try and find suitable environment attachment point when searching for sideways attachments?
		/// </summary>
		public bool includeEnvironmentInSidewaySearch;
		/// <summary>
		/// If this <see cref="TerminusObject"/> is detached from its parent, it will transform one of its sideways attachment to parent attachment.
		/// </summary>
		public bool autoReparentToSideways;
		/// <summary>
		/// If <see cref="TerminusObject.autoReparentToSideways"/> is true, should sideways attachments of children of this object be included in new parent search? Not that if said child is found, Terminus will reverse relationships on the branch between this object and child.
		/// </summary>
		public bool includeChildrenInAutoSideways;
		/// <summary>
		/// If set to true, <see cref="Container"/> GameObject will be created when this <see cref="TerminusObject"/> is root (not child of any other TerminusObject).
		/// </summary>
		/// <seealso cref="TerminusObject.container"/>
		public bool createContainerWhenRoot;
		/// <summary>
		/// <see cref="Container"/> object created if <see cref="TerminusObject.createContainerWhenRoot"/> set to true.
		/// </summary>
		/// <seealso cref="TerminusObject.createContainerWhenRoot"/>
		public Container container;

		/// <summary>
		/// Use groups of <see cref="Connector"/>s as one of the method of generating attachment points when trying to symmetrically attach several <see cref="TerminusObject"/>s.
		/// </summary>
		/// <remarks>
		/// Mainly used to provide symmetry for objects with no surfaces.
		/// For example, cube with 6 ports can have 3 2-way groups, 3 4-way groups and 1 6-way group of symmetry.
		/// </remarks>
		/// <seealso cref="TerminusObject.symmetryGroups"/> 
		public bool useSymmetryGroups;
		/// <summary>
		/// See <see cref="useSymmetryGroups"/> 
		/// </summary>
		public List<SymmetricGroup> symmetryGroups = new List<SymmetricGroup>();	

		/// <summary>
		/// Sibling <see cref="TerminusObject"/>s that were attached using symmetry mechanism.
		/// </summary>
		public List<TerminusObject> symmetricSiblings = new List<TerminusObject>();

		/// <summary>
		/// Information for usage in user interfaces about part this object represents. This information isn't used by Terminus system, but exists as convinience container.
		/// </summary>
		[Tooltip("Information for usage in user interfaces.This information isn't used by Terminus system, but exists as convenience container.")]
		public UIInfoContainer uiInfo;

		/// <summary>
		/// Dictionary with information about original states of rigidbodies, colliders and renderers from <see cref="AttachmentHandler"/> and <see cref="StateHandler"/>. Filled by <see cref="TerminusObject.SaveOriginalComponentState"/>. Modify at your own risk. Transform used as key for rigidbodies.
		/// </summary>
		[SerializeField]
		public OrigComponentInfoDict originalComponentsParams;

		[SerializeField]
		protected OriginalComponentInfo mainRBodyInfo;
		[SerializeField]
		protected OriginalComponentInfo mainRBodyInfo2D;

		[System.Serializable]
		public class OrigComponentInfoDict : SerializableDictionary<Component,OriginalComponentInfo> {}

		[System.Serializable]
		public class OriginalComponentInfo
		{
			public int layer;
			public bool flag;
			public bool stateAffected;
			public bool attachmentAffected;

			public OriginalComponentInfo(int layer, bool flag, bool state, bool attachment) 
			{
				this.layer = layer;
				this.flag = flag;
				this.stateAffected = state;
				this.attachmentAffected = attachment;
			}
		}
			
		/// <summary>
		/// Information about group of <see cref="TerminusObject"/> siblings that are in the same symmetric group.
		/// </summary>
		/// <seealso cref="TerminusObject.symmetricSiblings"/>
		[System.Serializable]
		public class SymmetricGroup
		{
			/// <summary>
			/// Symmetric connectors of <see cref="TerminusObject"/>s  inside this symmetric group.
			/// </summary>
			public List<Connector> connectors = new List<Connector>();
		}


		/// <summary>
		/// Position of this <see cref="TerminusObject"/> with regards to <see cref="TerminusObject.mainRigidbody"/> and <see cref="TerminusObject.mainRigidbody2D"/>.
		/// Returns transform.position if <see cref="TerminusObject.mainRigidbody"/> and <see cref="TerminusObject.mainRigidbody2D"/> are null.
		/// </summary>
		/// <seealso cref="TerminusObject.mainRigidbody"/>
		/// <seealso cref="TerminusObject.mainRigidbody2D"/>
		public Vector3 realPosition
		{
			get
			{
				if (mainRigidbody == null && mainRigidbody2D == null)
					return transform.position;
				else if (mainRigidbody2D == null)
					return mainRigidbody.transform.position;
				else return mainRigidbody2D.transform.position;
			}
		}

		/// <summary>
		/// Rotation of this <see cref="TerminusObject"/> with regards to <see cref="TerminusObject.mainRigidbody"/> and <see cref="TerminusObject.mainRigidbody2D"/>.
		/// Returns transform.rotation if <see cref="TerminusObject.mainRigidbody"/> and <see cref="TerminusObject.mainRigidbody2D"/> are null.
		/// </summary>
		/// <seealso cref="TerminusObject.mainRigidbody"/>
		/// <seealso cref="TerminusObject.mainRigidbody2D"/>
		public Quaternion realRotation
		{
			get
			{
				if (mainRigidbody != null)
					return mainRigidbody.transform.rotation;
				else if (mainRigidbody2D != null)
					return mainRigidbody2D.transform.rotation;
				else
					return transform.rotation;
			}
		}


		/// <summary>
		/// Manipulates position of this <see cref="TerminusObject"/> and all its <see cref="TerminusObject.treeListDown"/> children with regards to its <see cref="TerminusObject.container"/> 
		/// </summary>
		public Vector3 position
		{
			get
			{
				return transform.position;
			}
			set
			{
				if (container == null)
				{
					transform.position = value;
				}
				else
				{
					if (container.transform.position == transform.position && container.transform.rotation == rotation)
					{
						container.transform.position = value;				
					}
					else
					{
						Transform[] childArr = new Transform[container.transform.childCount];
						int ind = 0;
						foreach (Transform child in container.transform) 
						{
							childArr[ind] = child;
							ind++;
						}
						container.transform.DetachChildren();
						container.transform.position = transform.position;
						container.transform.rotation = transform.rotation;
						for (int i = 0; i < childArr.Length; i++)
							childArr[i].parent = container.transform;
						container.transform.position = value;
					}
				}
			}
		}

		/// <summary>
		/// Manipulates rotation of this <see cref="TerminusObject"/> and all its <see cref="TerminusObject.treeListDown"/> children with regards to its <see cref="TerminusObject.container"/> 
		/// </summary>
		public Quaternion rotation
		{
			get
			{
				return transform.rotation;
			}
			set
			{
				if (container == null)
				{
					transform.rotation = value;
				}
				else
				{
					if (container.transform.rotation == transform.rotation && container.transform.rotation == rotation)
					{
						container.transform.rotation = value;				
					}
					else
					{
						Transform[] childArr = new Transform[container.transform.childCount];
						int ind = 0;
						foreach (Transform child in container.transform) 
						{
							childArr[ind] = child;
							ind++;
						}
						container.transform.DetachChildren();
						container.transform.rotation = transform.rotation;
						container.transform.rotation = transform.rotation;
						for (int i = 0; i < childArr.Length; i++)
							childArr[i].parent = container.transform;
						container.transform.rotation = value;
					}
				}
			}
		}

		/// <summary>
		/// Returns <see cref="TerminusObject.container"/> gameobject if it's not null, <see cref="TerminusObject"/> gameObject otherwise.
		/// </summary>
		public GameObject containerOrSelf
		{
			get
			{
				if (container == null)
					return gameObject;
				else
					return container.gameObject;
			}
		}

		/// <summary>
		/// Returns most senior <see cref="TerminusObject"/> of hierarchy tree this object is part of. (Terminus hierarchy is separate from Unity transform hierarchy).
		/// </summary>
		public TerminusObject treeRoot
		{
			get
			{
				AttachmentInfo output;
                output.attachmentType = AttachmentInfo.Types.none;
                output.otherConnector = null;
				AttachmentInfo current = parent;
				while (current.attachmentType != AttachmentInfo.Types.none)
				{
					output = current;
					current = current.otherConnector.owner.parent;
				}
				if (output.attachmentType == AttachmentInfo.Types.none || output.otherConnector == null)
					return this;
				else
					return output.otherConnector.owner;
			}
		}

		/// <summary>
		/// Returns <see cref="AttachmentInfo"/> of attachment to <see cref="AttachmentInfo.Types.parent"/>  of this <see cref="TerminusObject"/>. (Terminus hierarchy is separate from Unity transform hierarchy).
		/// Get <see cref="AttachmentInfo.otherConnector"/>.owner to get parent <see cref="TerminusObject"/>.
		/// </summary>
		/// <seealso cref="TerminusObject.treeRoot"/> 
		/// <seealso cref="TerminusObject.treeListUp"/> 
		public AttachmentInfo parent
		{
			get
			{
				//return connectors.SelectMany(rec => rec.attachmentsInfo).Where(rec => rec.attachmentType == AttachmentInfo.Types.child).FirstOrDefault();
				for (int i = 0; i < connectors.Count; i++)
				{
					if (connectors[i] is Port && (((Port)connectors[i]).attachmentInfo.attachmentType == AttachmentInfo.Types.child))
						return ((Port)connectors[i]).attachmentInfo;
				}
				return new AttachmentInfo();
			}
		}

		/// <summary>
		/// Returns <see cref="AttachmentInfo"/>s of attachments to <see cref="AttachmentInfo.Types.child"/>ren of this <see cref="TerminusObject"/>. (Terminus hierarchy is separate from Unity transform hierarchy).
		/// Get <see cref="AttachmentInfo.otherConnector"/>.owner or use <see cref="TermonisObject.childrenObjects"/>  to get children <see cref="TerminusObject"/>s.
		/// </summary>
		/// <seealso cref="TerminusObject.treeListDown"/>
		/// <seealso cref="TerminusObject.childrenObjects"/>
		public List<AttachmentInfo> children
		{
			get
			{
				//return connectors.SelectMany(rec => rec.attachmentsInfo).Where(rec => rec.attachmentType == AttachmentInfo.Types.parent).ToList();
				//List<AttachmentInfo> result = new List<AttachmentInfo>();
				int count = 0;
				for (int i = 0; i < connectors.Count; i++)
				{
					if (connectors[i] is Port)
					{
						Port port = (Port)connectors[i];
						if(port.attachmentInfo.attachmentType == AttachmentInfo.Types.parent && port.attachmentInfo.otherConnector != null && port.attachmentInfo.otherConnector.owner != null)
						{
							if (children_Internal.Count > count)
								children_Internal[count] = port.attachmentInfo;
							else
								children_Internal.Add(port.attachmentInfo);
							count++;
						}
					}
					else
					{
						Surface surf = ((Surface)connectors[i]);
						List<AttachmentInfo> surfInfos = surf.attachmentsInfo;
						for (int x = 0; x < surfInfos.Count; x++)
						{
							if (surfInfos[x].attachmentType == AttachmentInfo.Types.parent && surfInfos[x].otherConnector != null && surfInfos[x].otherConnector.owner != null)
							{
								if (children_Internal.Count > count)
									children_Internal[count] = surfInfos[x];
								else
									children_Internal.Add(surfInfos[x]);
								count++;
							}
						}
					}
				}
				if (children_Internal.Count > count)
					children_Internal = children_Internal.GetRange(0,count);
				return children_Internal;
			}
		}

		/// <summary>
		/// Returns all <see cref="AttachmentInfo.Types.child"/>ren of this <see cref="TerminusObject"/>. (Terminus hierarchy is separate from Unity transform hierarchy).
		/// </summary>
		/// <seealso cref="TerminusObject.children"/> 
		public List<TerminusObject> childrenObjects
		{
			get
			{
				//return children.Select(rec => rec.otherConnector.owner).ToList();
				int count = 0;
				for (int i = 0; i < connectors.Count; i++)
				{
					if (connectors[i] is Port)
					{						
						Port port = (Port)connectors[i];
						if(port.attachmentInfo.attachmentType == AttachmentInfo.Types.parent && port.attachmentInfo.otherConnector != null && port.attachmentInfo.otherConnector.owner != null)
						{
							if (childrenObjects_Internal.Count > count)
								childrenObjects_Internal[count] = port.attachmentInfo.otherConnector.owner;
							else
								childrenObjects_Internal.Add(port.attachmentInfo.otherConnector.owner);
							count++;
						}
					}
					else
					{
						Surface surf = ((Surface)connectors[i]);
						List<AttachmentInfo> surfInfos = surf.attachmentsInfo;
						for (int x = 0; x < surfInfos.Count; x++)
						{
							if (surfInfos[x].attachmentType == AttachmentInfo.Types.parent && surfInfos[x].otherConnector != null && surfInfos[x].otherConnector.owner != null)
							{
								if (childrenObjects_Internal.Count > count)
									childrenObjects_Internal[count] = surfInfos[x].otherConnector.owner;
								else
									childrenObjects_Internal.Add(surfInfos[x].otherConnector.owner);
								count++;
							}
						}
					}
				}
				if (childrenObjects_Internal.Count > count)
					childrenObjects_Internal = childrenObjects_Internal.GetRange(0,count);
				return childrenObjects_Internal;
			}
		}

		/// <summary>
		/// Returns <see cref="AttachmentInfo"/>s of <see cref="AttachmentInfo.Types.sideway"/> attachments of this <see cref="TerminusObject"/>.
		/// Get <see cref="AttachmentInfo.otherConnector"/>.owner to get sideway-attached <see cref="TerminusObject"/>s.
		/// </summary>
		public List<AttachmentInfo> sidewaysConnections
		{
			get
			{
				return connectors.SelectMany(rec => rec.attachmentsInfo).Where(rec => rec.attachmentType == AttachmentInfo.Types.sideway).ToList();
			}
		}

		/// <summary>
		/// Returns <see cref="AttachmentInfo"/>s of all attachments that are up-tree from this <see cref="TerminusObject"/>. (Terminus hierarchy is separate from Unity transform hierarchy).
		/// Get <see cref="TerminusObject.treeListUpObjects"/> to get <see cref="TerminusObject"/>s instead of <see cref="AttachmentInfo"/>s.
		/// </summary>
		/// <seealso cref="TerminusObject.treeRoot"/> 
		/// <seealso cref="TerminusObject.treeListDown"/> 
		/// <seealso cref="TerminusObject.treeListUpObjects"/> 
		public List<AttachmentInfo> treeListUp
		{
			get
			{
				List<AttachmentInfo> output = new List<AttachmentInfo>();
				AttachmentInfo current = parent;
				while (current.attachmentType != AttachmentInfo.Types.none)
				{
					output.Add(current);
					current = current.otherConnector.owner.parent;
				}
				return output;
			}
		}

		/// <summary>
		/// Returns <see cref="TerminusObjects"/>s that are up-tree from this <see cref="TerminusObject"/>. (Terminus hierarchy is separate from Unity transform hierarchy).
		/// </summary>
		/// <seealso cref="TerminusObject.treeRoot"/> 
		/// <seealso cref="TerminusObject.treeListDown"/> 
		public List<TerminusObject> treeListUpObjects
		{
			get
			{
				List<TerminusObject> output = new List<TerminusObject>();
				AttachmentInfo current = parent;
				while (current.attachmentType != AttachmentInfo.Types.none)
				{
					output.Add(current.otherConnector.owner);
					current = current.otherConnector.owner.parent;
				}
				return output;
			}
		}

		/// <summary>
		/// Returns <see cref="AttachmentInfo"/>s of all attachments that are down-tree from this <see cref="TerminusObject"/>. (Terminus hierarchy is separate from Unity transform hierarchy).
		/// Get <see cref="TerminusObject.treeListDownObjects"/> to get <see cref="TerminusObject"/>s instead of <see cref="AttachmentInfo"/>s.
		/// </summary>
		/// <seealso cref="TerminusObject.treeRoot"/> 
		/// <seealso cref="TerminusObject.treeListUp"/> 
		public List<AttachmentInfo> treeListDown
		{
			get
			{
				/*
				List<AttachmentInfo> output = new List<AttachmentInfo>();
				List<AttachmentInfo> currentLevel = new List<AttachmentInfo>();
				List<AttachmentInfo> nextLevel = new List<AttachmentInfo>();
				currentLevel.AddRange(children);
				while (currentLevel.Count > 0)
				{
					output.AddRange(currentLevel);
					nextLevel.Clear();
					for (int i = 0; i < currentLevel.Count; i++)
					{
						nextLevel.AddRange(currentLevel[i].otherConnector.owner.children);
					}
					currentLevel = nextLevel;
				}
				return output;
				*/
				int count = 0;

				List<AttachmentInfo> chObjs = children;
				int oldRangeStart = 0;
				int oldRangeFinish = chObjs.Count-1;

				for (int i = 0; i < chObjs.Count; i++)
				{
					if (treeListDown_Internal.Count > count)
						treeListDown_Internal[count] = chObjs[i];
					else
						treeListDown_Internal.Add(chObjs[i]);
					count++;
				}
					
				bool stopFlag = (count == 0);
				while (!stopFlag)
				{
					int oldCount = count;
					for (int i = oldRangeStart; i <= oldRangeFinish; i++)
					{
						chObjs = treeListDown_Internal[i].otherConnector.owner.children;
						for (int x = 0; x < chObjs.Count; x++)
						{
							if (treeListDown_Internal.Count > count)
								treeListDown_Internal[count] = chObjs[x];
							else
								treeListDown_Internal.Add(chObjs[x]);
							count++;
						}
					}
					oldRangeStart = oldRangeFinish + 1;
					oldRangeFinish = count - 1;
					stopFlag = count == oldCount;
				}
				if (treeListDown_Internal.Count > count)
					treeListDown_Internal = treeListDown_Internal.GetRange(0,count);
				return treeListDown_Internal;
			}
		}

		/// <summary>
		/// Returns <see cref="TerminusObject"/>s that are down-tree from this <see cref="TerminusObject"/>. (Terminus hierarchy is separate from Unity transform hierarchy).
		/// Get <see cref="AttachmentInfo.otherConnector"/>.owner to get <see cref="TerminusObject"/>s.
		/// </summary>
		/// <seealso cref="TerminusObject.treeRoot"/> 
		/// <seealso cref="TerminusObject.treeListUp"/> 
		public List<TerminusObject> treeListDownObjects
		{
			get
			{
				//return treeListDown.Select( rec => rec.otherConnector.owner).ToList();
				//List<TerminusObject> output = new List<TerminusObject>();
				//List<TerminusObject> nextLevel = new List<TerminusObject>();
				int count = 0;

				List<TerminusObject> chObjs = childrenObjects;
				int oldRangeStart = 0;
				int oldRangeFinish = chObjs.Count-1;

				for (int i = 0; i < chObjs.Count; i++)
				{
					if (treeListDownObjects_Internal.Count > count)
						treeListDownObjects_Internal[count] = chObjs[i];
					else
						treeListDownObjects_Internal.Add(chObjs[i]);
					count++;
				}
					
				bool stopFlag = (count == 0);
				while (!stopFlag)
				{					
					int oldCount = count;
					for (int i = oldRangeStart; i <= oldRangeFinish; i++)
					{												
						chObjs = treeListDownObjects_Internal[i].childrenObjects;
						for (int x = 0; x < chObjs.Count; x++)
						{
							if (treeListDownObjects_Internal.Count > count)
								treeListDownObjects_Internal[count] = chObjs[x];
							else
								treeListDownObjects_Internal.Add(chObjs[x]);
							count++;
						}
					}
					oldRangeStart = oldRangeFinish + 1;
					oldRangeFinish = count - 1;
					stopFlag = count == oldCount;
				}
				if (treeListDownObjects_Internal.Count > count)
					treeListDownObjects_Internal = treeListDownObjects_Internal.GetRange(0,count);
				return treeListDownObjects_Internal;
			}
		}
			
		/// <summary>
		/// Returns all <see cref="TerminusObject"/>s attached to this TerminusObject.
		/// </summary>
		public List<TerminusObject> allAttachedObjects
		{
			get
			{
				return connectors.SelectMany(rec => rec.attachmentsInfo).Where(rec => rec.attachmentType != AttachmentInfo.Types.none).Select(rec => rec.otherConnector.owner).Distinct().ToList();
			}
		}
			
		/// <summary>
		/// All <see cref="TerminusObject"/>s that welded to rigidbody of this TerminusObject.
		/// Performs down-tree search of all downward <see cref="AttachmentInfo"/>s with <see cref="AttachmentInfo.weldingAttachment"/> set to true.
		/// </summary>
		public List<TerminusObject> weldedObjects
		{
			get
			{
				int count = 0;

				List<AttachmentInfo> childAttachments = children;

				for (int i = 0; i < childAttachments.Count; i++)
				{
					if (childAttachments[i].weldingAttachment)
					{
						if (weldedObjs_Internal.Count > count)
							weldedObjs_Internal[count] = childAttachments[i].otherConnector.owner;
						else
							weldedObjs_Internal.Add(childAttachments[i].otherConnector.owner);
						count++;
					}
				}

				int oldRangeStart = 0;
				int oldRangeFinish = count-1;

				bool stopFlag = (count != 0);
				while (!stopFlag)
				{
					int oldCount = count;
					for (int i = oldRangeStart; i <= oldRangeFinish; i++)
					{
						childAttachments = weldedObjs_Internal[i].children;
						for (int x = 0; x < childAttachments.Count; x++)
						{							
							if (childAttachments[x].weldingAttachment)
							{
								if (weldedObjs_Internal.Count > count)
									weldedObjs_Internal[count] = childAttachments[x].otherConnector.owner;
								else
									weldedObjs_Internal.Add(childAttachments[x].otherConnector.owner);
								count++;
							}
						}
					}
					oldRangeStart = oldRangeFinish + 1;
					oldRangeFinish = count - 1;
					stopFlag = count == oldCount;
				}
				if (weldedObjs_Internal.Count > count)
					weldedObjs_Internal = weldedObjs_Internal.GetRange(0,count);
				return weldedObjs_Internal;
				/*
				List<AttachmentInfo> output = new List<AttachmentInfo>();
				List<AttachmentInfo> currentLevel = new List<AttachmentInfo>();
				List<AttachmentInfo> nextLevel;
				//output.Add(this);
				currentLevel.AddRange(children.Where(rec => rec.weldingAttachment).ToList());
				while (currentLevel.Count > 0)
				{
					output.AddRange(currentLevel);
					nextLevel = new List<AttachmentInfo>();
					for (int i = 0; i < currentLevel.Count; i++)
					{
						nextLevel.AddRange(currentLevel[i].otherConnector.owner.children.Where(rec => rec.weldingAttachment).ToList());
					}
					currentLevel = nextLevel;
				}
				return output.Select(rec => rec.otherConnector.owner).ToList();
				*/
			}
		}

		/// <summary>
		/// All interactions with other connectors according to <see cref="Settings.LayerPortOptions.useInfluenceMessaging"/>.
		/// </summary>
		[System.NonSerialized]
		public List<ZoneInteraction> allInfluences = new List<ZoneInteraction>();



		/// <summary>
		/// Closest interaction with other connectors according to <see cref="Settings.LayerPortOptions.useInfluenceMessaging"/>.
		/// </summary>
		public ZoneInteraction closestInfluence
		{
			get
			{
				if (allInfluences.Count > 0)
				{
					float minDist = float.MaxValue;
					int ind = 0;
					for (int i = 0; i < allInfluences.Count; i++)
					{
						if (allInfluences[i].sqrDistance < minDist)
						{
							ind = i;
							minDist = allInfluences[i].sqrDistance;
						}
					}
					return allInfluences[ind];
				}
				else
					return null;
			}
		}

		/// <summary>
		/// Mode of this object. Changing this re-initializes all <see cref="TerminusObject.connectors"/> of this <see cref="TerminusObject"/>.
		/// </summary>
		public Modes mode
		{
			get
			{
				return modeP;
			}
			set
			{
				modeP = value;
				for (int i = 0; i < connectors.Count; i++)
					Manager.RegisterConnector(connectors[i]);
	        }
	    }

		/// <summary>
		/// Current <see cref="Port"/> that is used by <see cref="Placer"/> class to try to attach this <see cref="TerminusObject"/>.
		/// Will return first attachment port if <see cref="TerminusObject.multipleAttachmentPortsAllowed"/> is true. If that's the case, it's better to use <see cref="TerminusObject.currentAttachmentPorts"/>.
		/// </summary>
		/// <remarks>
		/// Setting <see cref="Port.isCurrentAttachmentPort"/> changes this field and vice-versa.
		/// </remarks>
		public Port currentAttachmentPort
		{
			get
			{
				for (int i = 0; i < connectors.Count;i++)
				{
					if (connectors[i] != null && connectors[i] is Port && connectors[i].isAttachable && ((Port)(connectors[i])).isCurrentAttachmentPort)
						return (Port)(connectors[i]);
				}
				return null;
			}
			set
			{
				if (value != null && connectors.Contains(value))
					value.isCurrentAttachmentPort = true;
			}
		}

		/// <summary>
		/// Current <see cref="Port"/>s that is used by <see cref="Placer"/> class to try to attach this <see cref="TerminusObject"/>.
		/// Only worth using if <see cref="TerminusObject.multipleAttachmentPortsAllowed"/> is true. Otherwise use <see cref="TerminusObject.currentAttachmentPort"/>.
		/// </summary>
		public List<Port> currentAttachmentPorts
		{
			get
			{
				if (!multipleAttachmentPortsAllowed)
				{					
					Port attPort = currentAttachmentPort;
					if (attPort == null)
					{
						if (!(attachmentPorts_Internal.Count == 0))
							attachmentPorts_Internal.Clear();
						return attachmentPorts_Internal;
					}
					else
					{
						if (attachmentPorts_Internal.Count == 1)
							attachmentPorts_Internal[0] = attPort;
						else
						{
							attachmentPorts_Internal.Clear();
							attachmentPorts_Internal.Add(attPort);
						}
						return attachmentPorts_Internal;
					}
				}
				else
				{					
					int count = 0;
					for (int i = 0; i < connectors.Count;i++)
					{
						if (connectors[i] != null && connectors[i] is Port && connectors[i].isAttachable && ((Port)(connectors[i])).isCurrentAttachmentPort)
						{
							if (attachmentPorts_Internal.Count > count)
								attachmentPorts_Internal[count] = (Port)connectors[i];
							else
								attachmentPorts_Internal.Add((Port)connectors[i]);
							count++;
						}
					}
					if (attachmentPorts_Internal.Count > count)
						attachmentPorts_Internal = attachmentPorts_Internal.GetRange(0,count);
					return attachmentPorts_Internal;
				}
			}
		}

		/// <summary>
		/// Controls associated <see cref="StateHandler"/>. <see cref="StandardStateHandler"/> switches isKinematic property of <see cref="StateHandler.affectedRigidbodies"/> and <see cref="StateHandler.affectedRigidbodies2D"/> according to this value.
		/// </summary>
		public bool inAssemblyState
		{
			get
			{
				return inAssemblyStateP;
			}
			set
			{
				if (Application.isPlaying && value != inAssemblyStateP)
				{
					inAssemblyStateP = value;
					ChangedAssemblyState();
				}
				else
				{
					inAssemblyStateP = value;
				}
			}
		}


		[SerializeField]
		protected Modes modeP;

		[SerializeField]
		protected bool inAssemblyStateP;


		/// <summary>
		/// Mode of TerminusObject determinig behaviour of its <see cref="TerminusObject.connectors"/>.
		/// </summary>
		public enum Modes
		{
			/// <summary>
			/// <see cref="TerminusObject.connectors"/> of this <see cref="TerminusObject"/> can only accept attachments.
			/// </summary>
			accepting_attachments = 0,
			/// <summary>
			/// <see cref="TerminusObject.connectors"/> of this <see cref="TerminusObject"/> can only be attached to <see cref="Connector"/>s that can accept attachments .
			/// </summary>
			being_attached = 1,
			/// <summary>
			/// <see cref="TerminusObject.connectors"/> of this <see cref="TerminusObject"/> can both accept attachments and be attached.
			/// </summary>
			free_connections = 2,
			/// <summary>
			/// <see cref="TerminusObject.connectors"/> of this <see cref="TerminusObject"/> can neither accept attachment nor be attached.
			/// </summary>
			inactive = 3
		}

		/// <summary>
		/// Checks if this object is child (including multi-level) of provided <see cref="TerminusObject"/> and returns separation level(1 for direct child, 2 for grandchild etc). Returns -1 if it's not a child of provided object.
		/// </summary>
		public int GetChildLevelFrom(TerminusObject possibleParent)
		{			
			AttachmentInfo current = parent;
			int level = 0;
			while (current.attachmentType != AttachmentInfo.Types.none)
			{
				level++;
				if (current.otherConnector.owner == possibleParent)
					return level;
				current = current.otherConnector.owner.parent;
			}
			return -1;
		}

		/// <summary>
		/// Enters the playmode (see <see cref="TerminusObject.inPlaymode"/>. Use <see cref="Manager.globalPlaymode"/> if you want to correctly handle sideway attachments between different vehicles (object trees).
		/// </summary>
		/// <returns>Created <see cref="TerminusObject.playmodeClone"/></returns>
		/// <param name="changeAssemblyState">If set to <c>false</c>, does not change assembly state of created clone.</param>
		/// <seealso cref="TerminusObject.inPlaymode"/>
		public TerminusObject EnterPlaymode(bool changeAssemblyState = true)
		{
			if (Application.isPlaying)
			{
				if (isPlaymodeCloneP)
				{
					Debug.LogWarning(gameObject.name + "(Terminus) : Trying to enter playmode with playmode clone. Declare it non-clone first.");
					return null;
				}

				containerOrSelf.SetActive(false);
				GameObject cloneObj = Instantiate(containerOrSelf);
				TerminusObject clone;
				if (container == null)
					clone = cloneObj.GetComponent<TerminusObject>();
				else
					clone = cloneObj.GetComponent<Container>().terminusObject;
				playmodeClone = clone;
				playmodeClone.isPlaymodeCloneP = true;
				clone.playmodeOriginal = this;
				clone.inPlaymodeP = true;
				TerminusObject[] children = treeListDown.Select(rec => rec.otherConnector.owner).ToArray();
				TerminusObject[] cloneChildren = clone.treeListDown.Select(rec => rec.otherConnector.owner).ToArray();
				for (int i = 0; i < children.Length; i++)
				{
					children[i].inPlaymodeP = true;
					cloneChildren[i].isPlaymodeCloneP = true;
					cloneChildren[i].inPlaymodeP = true;
					cloneChildren[i].playmodeOriginal = children[i];
				}
				clone.containerOrSelf.SetActive(true);
				if (changeAssemblyState)
					clone.inAssemblyState = false;
				inPlaymodeP = true;
				return clone;
			}
			else
				return null;
		}

		/// <summary>
		/// Works only if this <see cref="TerminusObject"/> <see cref="TerminusObject.isPlaymodeClone"/>. Declares it non-clone so it persists after playmode ends.
		/// </summary>
		/// <seealso cref="TerminusObject.inPlaymode"/>
		public void DeclarePlaymodeNonClone()
		{
			if (Application.isPlaying)
			{
				if (isPlaymodeCloneP)
				{
					playmodeOriginal.playmodeClone = null;
					playmodeOriginal = null;
					isPlaymodeCloneP = false;
					TerminusObject[] children = treeListDown.Select(rec => rec.otherConnector.owner).ToArray();
					for (int i = 0; i < children.Length; i++)
					{
						children[i].isPlaymodeCloneP = false;
						children[i].playmodeOriginal = null;
					}
					inPlaymodeP = false;
				}
			}
		}

		/// <summary>
		/// Exits the playmode (see <see cref="TerminusObject.inPlaymode"/>. Can be called on either clone or original. Always returns original.
		/// </summary>
		/// <returns><see cref="TerminusObject.playmodeOriginal"/></returns>
		/// <seealso cref="TerminusObject.inPlaymode"/>
		public TerminusObject ExitPlaymode()
		{
			if (!appQuit)
			{
				TerminusObject clone = null;
				TerminusObject original = null;

				if (isPlaymodeCloneP)
				{
					clone = this;
					original = playmodeOriginal;
				}
				else
				{
					clone = playmodeClone;
					original = this;
				}
					
				if (clone != null)
				{
					clone.destroyFlagP = true;

					List<TerminusObject> children = clone.treeListDownObjects;
					for (int i = (children.Count-1); i >= 0; i--)
					{
						if (!children[i].destroyFlagP)
						{
							children[i].destroyFlagP = true;
							Destroy(children[i].gameObject);
						}
					}

					Destroy(clone.gameObject);


					if (clone.container != null)						
						Manager.FreeContainer(clone.container);					
				}

				if (original != null)
				{
					original.containerOrSelf.SetActive(true);
					original.inPlaymodeP = false;
				}

				return original;
			}
			else
				return null;
		}

		/// <summary>
		/// Tries to find new parent for this object using its sideways connections. Used by <see cref="TerminusObject.autoReparentToSideways"/> .
		/// </summary>
		public void ReparentWithSidewaysConnections(TerminusObject desiredRoot)
		{
			for (int i = 0; i < connectors.Count; i++)
			{				
				if (connectors[i] is Port)
				{			
					Port port = (Port)connectors[i];				
					if (port.attachmentInfo.attachmentType == AttachmentInfo.Types.sideway
						&& !port.attachmentInfo.otherConnector.owner.destroyFlag
						&& port.attachmentInfo.otherConnector.owner.treeRoot == desiredRoot
						&& (port.attachmentInfo.otherConnector.owner.GetChildLevelFrom(port.owner) < 0)
						&& (port.owner.GetChildLevelFrom(port.attachmentInfo.otherConnector.owner) < 0))
					{
						Connector oppositeConn = port.attachmentInfo.otherConnector;					
						port.Detach(false,false,true,true);
						port.AttachTo(oppositeConn,true);
						return;
					}
				}
			}
				
			if (includeChildrenInAutoSideways)
			{
				List<TerminusObject> lookup = childrenObjects;
				List<TerminusObject> newLookup = new List<TerminusObject>();

				while (lookup.Count > 0)
				{					
					for (int i = 0; i < lookup.Count; i++)
					{
						if (lookup[i].autoReparentToSideways)
						{
							for (int x = 0; x < lookup[i].connectors.Count; x++)
							{	
								if (lookup[i].connectors[x] is Port)
								{			
									Port port = (Port)lookup[i].connectors[x];
									Connector oppositeConn = port.attachmentInfo.otherConnector;
									if (port.attachmentInfo.attachmentType == AttachmentInfo.Types.sideway
										&& !oppositeConn.owner.destroyFlag
										&& oppositeConn.owner.treeRoot == desiredRoot
										&& (oppositeConn.owner.GetChildLevelFrom(port.owner) < 0)
										&& (port.owner.GetChildLevelFrom(oppositeConn.owner) < 0))
									{										
										//Getting upward branch of Terminus hierarchy, cutting off if we encounter Surface (since we can't reverse attachment through Surface) or TerminusObject that doesn't support reparenting
										bool cutoff = false;
										List<AttachmentInfo> branch = new List<AttachmentInfo>();
										AttachmentInfo current = lookup[i].parent;
										while (current.attachmentType != AttachmentInfo.Types.none && current.otherConnector != null)
										{
											branch.Add(current);
											if (!current.otherConnector.owner.includeChildrenInAutoSideways
												|| !current.otherConnector.owner.autoReparentToSideways
												|| !(current.selfConnector is Port)
												|| !(current.otherConnector is Port))
											{
												cutoff = true;
												break;
											}
											current = current.otherConnector.owner.parent;
										}
											
										if (!cutoff)
										{											
											for (int y = branch.Count-1; y >= 0; y--)
											{												
												((Port)branch[y].selfConnector).Detach(false,false,true,true);
											}
											for (int y = 0; y < branch.Count; y++)
											{												
												((Port)branch[y].otherConnector).AttachTo(branch[y].selfConnector,true);
											}
											port.Detach(false,false,true,true);
											port.AttachTo(oppositeConn,true);
											return;
										}
									}
								}
							}
							if (lookup[i].includeChildrenInAutoSideways)
								newLookup.AddRange(lookup[i].childrenObjects);
						}
					}
					lookup.Clear();
					lookup.AddRange(newLookup);
					newLookup.Clear();
				}
			}
		}

		protected void ChangedAssemblyState()
		{
			for (int i = 0; i < children.Count; i++)
			{
				children[i].otherConnector.owner.inAssemblyState = inAssemblyStateP;
			}
			if (inAssemblyStateP)
				stateHandler.EnterAssemblyState();
			else
				stateHandler.ExitAssemblyState();
		}

		/// <summary>
		/// Removes null refernces from <see cref="TerminusObject.connectors"/> 
		/// </summary>
		public void CleanUpConnectors()
		{
			connectors = connectors.Where(rec => rec != null).ToList();
		}

		/// <summary>
		/// Removes null refernces from <see cref="TerminusObject.allInfluences"/> 
		/// </summary>
		public void ClearInfluences()
		{
			if (allInfluences != null)
				allInfluences.Clear();
			for (int i = 0; i < connectors.Count; i++)
			{
				if (connectors[i] is Port && ((Port)connectors[i]).influences != null)
					((Port)connectors[i]).influences.Clear();
			}
		}

		/// <summary>
		/// Move <see cref="TerminusObject"/> to specified position with regards to <see cref="TerminusObject.container"/> 
		/// </summary>
		/// <param name="position">New worldspace position.</param>
		/// <param name="rotation">New worldspace rotation.</param>
		public void Move(Vector3 position, Quaternion rotation)
		{
			if (container == null)
			{
				transform.position = position;
				transform.rotation = rotation;
			}
			else
			{
				if (container.transform.position == transform.position && container.transform.rotation == rotation)
				{
					container.transform.position = position;
					container.transform.rotation = rotation;
				}
				else
				{
					Transform[] childArr = new Transform[container.transform.childCount];
					int ind = 0;
					foreach (Transform child in container.transform) 
					{
						childArr[ind] = child;
						ind++;
					}
					container.transform.DetachChildren();
					container.transform.position = transform.position;
					container.transform.rotation = transform.rotation;
					for (int i = 0; i < childArr.Length; i++)
						childArr[i].parent = container.transform;
					container.transform.position = position;
					container.transform.rotation = rotation;
				}
			}
		}


		/// <summary>
		/// Cycles through possible ports that can be <see cref="TerminusObject.currentAttachmentPort"/> 
		/// </summary>
		/// <returns><c>true</c>, if next attachment port was set, <c>false</c> otherwise.</returns>
		/// <param name="step">Amount of indexes to skip through.</param>
		public bool SetNextAttachmentPort(int step = 1)
		{
			List<Connector> allSuitablePorts = connectors.Where(rec => (rec.isAttachable && ((Port)rec).portOptions.attachementPort && ((Port)rec).portOptions.cyclingAttachementPort)).ToList();
			if (allSuitablePorts.Count > 1)
			{
				int ind = allSuitablePorts.FindIndex(rec => ((Port)rec).isCurrentAttachmentPort);
				if (ind < 0)
					ind = 0;
				ind += step;

				if (ind > allSuitablePorts.Count - 1)
					ind = ind % (allSuitablePorts.Count);

				for (int i = 0; i < allSuitablePorts.Count; i++)
					((Port)allSuitablePorts[i]).isCurrentAttachmentPort = (i == ind);
				return true;
			}
			else
			{
				if (allSuitablePorts.Count == 1)
				{
					((Port)allSuitablePorts[0]).isCurrentAttachmentPort = true;
					return true;
				}
				else
				{
					return false;
				}
			}
		}


		/// <summary>
		/// Detachs this <see cref="TerminusObject"/> from its <see cref="TerminusObject.parent"/>.
		/// </summary>
		/// <param name="destroyLongConnections">If set to <c>true</c>, <see cref="LongConnection"/>-type <see cref="TerminusObject"/>s leading outisde <see cref="TerminusObject.treeListDown"/> will be destroyed.</param>
		public void DetachFromParent(bool destroyLongConnections = true)
		{
			if (parent.attachmentType != AttachmentInfo.Types.none)
			{
				((Port)(parent.selfConnector)).Detach();
				//Detaching outside sideway connections
				List<AttachmentInfo> sidewayConn;
				List<AttachmentInfo> tempTreeListDown = treeListDown;
				List<TerminusObject> treeObjects = tempTreeListDown.Select(rec => rec.otherConnector.owner).ToList();
				treeObjects.AddRange(tempTreeListDown.Select(rec => rec.selfConnector.owner).ToList());
				treeObjects.Add (this);
				treeObjects = treeObjects.Distinct().ToList();
				//treeObjects.Add(this);
				for (int x = 0; x < treeObjects.Count; x++)
				{
					sidewayConn =  treeObjects[x].sidewaysConnections;

					if (destroyLongConnections)
					{
						List<TerminusObject> longConnObjects = treeObjects[x].allAttachedObjects.Where(rec => rec.longConnection).ToList();
											
						for (int i = 0; i < longConnObjects.Count; i++)
						{
							TerminusObject attachedObject = longConnObjects[i].allAttachedObjects.FirstOrDefault(rec => rec != treeObjects[x]);
							if (attachedObject == null || !treeObjects.Contains(attachedObject))
							{
								Destroy(longConnObjects[i].gameObject);
							}
						}
					}

					for (int i = 0; i < sidewayConn.Count; i++)
					{
						if (sidewayConn[i].otherConnector.GetType() == typeof(Port)
						    && !treeObjects.Contains(sidewayConn[i].otherConnector.owner))
						{					
							((Port)(sidewayConn[i].otherConnector)).Detach();
						}
						if (sidewayConn[i].selfConnector.GetType() == typeof(Port)
						    && !treeObjects.Contains(sidewayConn[i].selfConnector.owner))
						{
							((Port)(sidewayConn[i].selfConnector)).Detach();
						}				
					}
				}
			}
		}

		/// <summary>
		/// <see cref="Port.Detach"/>es  all attachments of designated <see cref="AttachmentInfo.Types"/>.
		/// </summary>
		/// <param name="type">Type of attachment to detach</param>
		public void DetachOfType(AttachmentInfo.Types type)
		{
			for (int i = 0; i < connectors.Count; i++)
			{
				if (connectors.GetType() == typeof(Port) && ((Port)connectors[i]).attachmentInfo.attachmentType == type)
					((Port)connectors[i]).Detach();
			}
		}

		/// <summary>
		/// Searches through all unoccupied <see cref="Port"/>s inside <see cref="TerminusObject.connectors"/> for possible sideway connections. Used to automatically connect to suitable connectors for structural integrity of resulting construction.
		/// </summary>
		/// <param name="distanceLimit">Distance limit for searching opposing ports.</param>
		/// <param name="angleLimit">Angle limit for searching opposing ports.</param>
		public void AttachSidewaysOnAllPorts(float distanceLimit = 0.05f, float angleLimit = 180.5f)
		{
			if (!doNotConnectSideways)
			{
				for (int i = 0; i < connectors.Count;i++)
				{
					//Debug.Log("Checking " + gameObject.name + "." + connectors[i].connectorName);
					if (connectors[i].GetType() == typeof(Port) && connectors[i].isAttachable)
					{
						Manager.UpdateCheckDataForPort(((Port)(connectors[i])));
						if (connectors[i].isAttachable)
						{							
							Connector sideConn = ((Port)(connectors[i])).ClosestAvaliableConnector(distanceLimit,angleLimit,false,includeEnvironmentInSidewaySearch);
							/*
							if (sideConn != null)
								Debug.LogWarning("Result for " + gameObject.name + "." + connectors[i].connectorName + " : " + sideConn.gameObject.name + "." + sideConn.connectorName);
							else
								Debug.Log("sideConn is null");
								*/
							if (sideConn != null)
							{
								((Port)(connectors[i])).AttachToSideways(sideConn);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Registers the connector with this <see cref="TerminusObject"/>.
		/// </summary>
		public void RegisterConnector(Connector connector)
		{
			if (!connectors.Contains(connector))
			{
				connectors.Add(connector);
				if (currentAttachmentPort == null)
					SetNextAttachmentPort(0);
			}
		}

		/// <summary>
		/// Fills <see cref="TerminusObject.rigidbodyInfo"/> of this object with parameters of <see cref="TerminusObject.mainRigidbody"/>.
		/// </summary>
		/// <seealso cref="Settings.attachmentTypes.rigidbody_welding"/>
		public void FillRigidbodyInfo()
		{
			if (mainRigidbody != null)
			{
				//Debug.Log(gameObject.name + "," + mainRigidbody.isKinematic.ToString());
				if (rigidbodyInfo == null)
					rigidbodyInfo = new RigidbodyInfo();
				rigidbodyInfo.mass = mainRigidbody.mass;
				rigidbodyInfo.drag = mainRigidbody.drag;
				rigidbodyInfo.angularDrag = mainRigidbody.angularDrag;
				rigidbodyInfo.centerOfMass = mainRigidbody.centerOfMass;
				rigidbodyInfo.collisionDetection = mainRigidbody.collisionDetectionMode;
				rigidbodyInfo.constraints = mainRigidbody.constraints;
				rigidbodyInfo.interpolation = mainRigidbody.interpolation;
				rigidbodyInfo.isKinematic = mainRigidbody.isKinematic;
				rigidbodyInfo.useGravity = mainRigidbody.useGravity;
				rigidbodyInfo.parent = mainRigidbody.transform;
			}
		}

		/// <summary>
		/// Fills <see cref="TerminusObject.rigidbodyInfo2D"/> of this object with parameters of <see cref="TerminusObject.mainRigidbody2D"/>.
		/// </summary>
		/// <seealso cref="Settings.attachmentTypes.rigidbody_welding"/>
		public void FillRigidbodyInfo2D()
		{
			if (mainRigidbody2D != null)
			{
				if (rigidbodyInfo2D == null)
					rigidbodyInfo2D = new RigidbodyInfo2D();
				rigidbodyInfo2D.mass = mainRigidbody2D.mass;
				rigidbodyInfo2D.linearDrag = mainRigidbody2D.drag;
				rigidbodyInfo2D.angularDrag = mainRigidbody2D.angularDrag;
				rigidbodyInfo2D.centerOfMass = mainRigidbody2D.centerOfMass;
				rigidbodyInfo2D.collisionDetection = mainRigidbody2D.collisionDetectionMode;
				rigidbodyInfo2D.constraints = mainRigidbody2D.constraints;
				rigidbodyInfo2D.interpolation = mainRigidbody2D.interpolation;
				rigidbodyInfo2D.isKinematic = mainRigidbody2D.isKinematic;
				rigidbodyInfo2D.gravityScale = mainRigidbody2D.gravityScale;
				rigidbodyInfo2D.parent = mainRigidbody2D.transform;
			}
		}

		/// <summary>
		/// Restores <see cref="TerminusObject.mainRigidbody"/> with information from <see cref="TerminusObject.rigidbodyInfo"/>.
		/// </summary>
		/// <seealso cref="Settings.attachmentTypes.rigidbody_welding"/>
		protected Rigidbody RestoreRigidbody()
		{			
			if (!destroyFlagP)
			{		
				Rigidbody rbody = null;
				if (rigidbodyInfo.parent == null || rigidbodyInfo.parent.gameObject == null)
					return mainRigidbody;				
				if (mainRigidbody == null)
				{
					rbody = rigidbodyInfo.parent.gameObject.AddComponent<Rigidbody>();
				}
				else
				{
					//Debug.LogError(gameObject.name + " : Unable to restore rigidbody since rigidbody already exists.");
					rbody = mainRigidbody;
					//return mainRigidbody;
				}
				rbody.mass = rigidbodyInfo.mass;
				rbody.drag = rigidbodyInfo.drag;
				rbody.angularDrag = rigidbodyInfo.angularDrag;
				rbody.centerOfMass = rigidbodyInfo.centerOfMass;
				rbody.collisionDetectionMode = rigidbodyInfo.collisionDetection;
				rbody.constraints = rigidbodyInfo.constraints;
				rbody.interpolation = rigidbodyInfo.interpolation;
				rbody.isKinematic = rigidbodyInfo.isKinematic;
				rbody.useGravity = rigidbodyInfo.useGravity;
				mainRigidbody = rbody;
				for (int i = 0; i < jointsConnectedToRigidbody.Count; i++)
				{
					jointsConnectedToRigidbody[i].connectedBody = rbody;
				}
				return rbody;
			}
			else
				return null;
		}


		/// <summary>
		/// Restores <see cref="TerminusObject.mainRigidbody2D"/> with information from <see cref="TerminusObject.rigidbodyInfo2D"/>.
		/// </summary>
		/// <seealso cref="Settings.attachmentTypes.rigidbody_welding"/>
		protected Rigidbody2D RestoreRigidbody2D()
		{
			if (!destroyFlag)
			{
				Rigidbody2D rbody = null;
				if (rigidbodyInfo2D.parent == null || rigidbodyInfo2D.parent.gameObject == null)
					return mainRigidbody2D;
				if (mainRigidbody2D == null)
				{					
					rbody = rigidbodyInfo2D.parent.gameObject.AddComponent<Rigidbody2D>();
				}
				else
				{
					//Debug.LogError(gameObject.name + " : Unable to restore rigidbody2D since rigidbody2D already exists.");
					rbody = mainRigidbody2D;
					//return mainRigidbody2D;
				}
				rbody.mass = rigidbodyInfo2D.mass;
				rbody.drag = rigidbodyInfo2D.linearDrag;
				rbody.angularDrag = rigidbodyInfo2D.angularDrag;
				rbody.centerOfMass = rigidbodyInfo2D.centerOfMass;
				rbody.collisionDetectionMode = rigidbodyInfo2D.collisionDetection;
				rbody.constraints = rigidbodyInfo2D.constraints;
				rbody.interpolation = rigidbodyInfo2D.interpolation;
				rbody.isKinematic = rigidbodyInfo2D.isKinematic;
				rbody.gravityScale = rigidbodyInfo2D.gravityScale;
				mainRigidbody2D = rbody;
				for (int i = 0; i < joints2DConnectedToRigidbody.Count; i++)
				{
					joints2DConnectedToRigidbody[i].connectedBody = rbody;
				}
				return rbody;
			}
			else
				return null;
		}

		/// <summary>
		/// Attempts to welds part to designated TerminusObject. Part of <see cref="Settings.attachmentTypes.rigidbody_welding"/> attachment.
		/// </summary>
		/// <param name="weldCandidate">Weld candidate.</param>
		/// <param name="phys2D">Use 2D physics and rigidbodies.</param>
		/// <seealso cref="Settings.attachmentTypes.rigidbody_welding"/>
		public void WeldToRigidbody(TerminusObject weldCandidate, bool phys2D, bool destroyRigidbodyImmediately)
		{
			if (weldCandidate.weldedToObject == null)
			{
				if (phys2D)
				{
					for (int i = 0; i < joints2DConnectedToRigidbody.Count; i++)
					{
						joints2DConnectedToRigidbody[i].connectedBody = weldCandidate.mainRigidbody2D;
					}

					List<AttachmentInfo> childrenInfos = children;
					for (int i = 0; i < childrenInfos.Count; i++)
					{
						if (childrenInfos[i].joint != null && childrenInfos[i].joint is Joint2D)
							((Joint2D)childrenInfos[i].joint).connectedBody = weldCandidate.mainRigidbody2D;
					}
						
					if (mainRigidbody2D != null)
					{	
						if (rigidbodyInfo2D.parent == null)
							FillRigidbodyInfo2D();

						if (destroyRigidbodyImmediately || !Application.isPlaying)
							DestroyImmediate(mainRigidbody2D);
						else
							Destroy(mainRigidbody2D);

						weldCandidate.RecalculateWeldedRigidbody();
					}					
					weldedToObjectP = weldCandidate;
				}
				else
				{
					for (int i = 0; i < jointsConnectedToRigidbody.Count; i++)
					{						
						jointsConnectedToRigidbody[i].connectedBody = weldCandidate.mainRigidbody;
					}
						
					List<AttachmentInfo> childrenInfos = children;
					for (int i = 0; i < childrenInfos.Count; i++)
					{
						if (childrenInfos[i].joint != null && childrenInfos[i].joint is Joint)
							((Joint)childrenInfos[i].joint).connectedBody = weldCandidate.mainRigidbody;
					}

					if (mainRigidbody != null)
					{
						if (rigidbodyInfo.parent == null)
							FillRigidbodyInfo();

						if (destroyRigidbodyImmediately)
							DestroyImmediate(mainRigidbody);
						else
							Destroy(mainRigidbody);

						weldCandidate.RecalculateWeldedRigidbody();
					}

					weldedToObjectP = weldCandidate;
				}
			}
			else
			{
				Debug.LogWarning(gameObject.name + " : Trying to weld to this object that's welded itself to another object. Something went wrong.");
			}
		}

		/// <summary>
		/// Unwelds this TerminusObject from combined rigidbody, and recreates original Rigidbody. Called automatically on detachment.
		/// </summary>
		/// <seealso cref="Settings.attachmentTypes.rigidbody_welding"/>
		public void UnweldRigidbody(bool doNotRestoreRigidbody = false)
		{
			if (weldedToObjectP == null)
				return;
			
			TerminusObject oldWeldingRoot = weldedToObjectP;

			if (doNotRestoreRigidbody || destroyFlagP)
			{
				oldWeldingRoot.RecalculateWeldedRigidbody();
				return;
			}

			bool flag2D = rigidbodyInfo.parent == null;

			/*
			bool flag2D = false;

			for (int i = 0; i < connectors.Count; i++)
			{
				Debug.Log(((Port)connectors[i]).attachmentInfo.attachmentType);
				if (connectors[i] is Port && ((Port)connectors[i]).attachmentInfo.attachmentType == AttachmentInfo.Types.child)
				{
					flag2D = ((Port)connectors[i]).portOptions.use2DPhysics;
					break;
				}
			}

			Debug.Log(flag2D);
			*/

			if (oldWeldingRoot != null)
			{				
				if (!flag2D)
				{
					RestoreRigidbody();

					for (int i = 0; i < jointsConnectedToRigidbody.Count; i++)
						jointsConnectedToRigidbody[i].connectedBody = mainRigidbody;

					/*
					if (stateHandler != null)
						stateHandler.affectedRigidbodies.Add(mainRigidbody);
					
					if (attachmentHandler != null)
						attachmentHandler.affectedRigidbodies.Add(mainRigidbody);
						*/

					List<AttachmentInfo> childrenInfos = children;
					for (int i = 0; i < childrenInfos.Count; i++)
					{
						if (childrenInfos[i].joint != null && childrenInfos[i].joint is Joint)
							((Joint)childrenInfos[i].joint).connectedBody = mainRigidbody;
					}
					
					for (int i = 0; i < jointsConnectedToRigidbody.Count; i++)
						jointsConnectedToRigidbody[i].connectedBody = mainRigidbody;
				}
				else
				{
					RestoreRigidbody2D();

					for (int i = 0; i < joints2DConnectedToRigidbody.Count; i++)
						joints2DConnectedToRigidbody[i].connectedBody = mainRigidbody2D;

					/*
					if (stateHandler != null)
						stateHandler.affectedRigidbodies2D.Add(mainRigidbody2D);				

					if (attachmentHandler != null)
						attachmentHandler.affectedRigidbodies2D.Add(mainRigidbody2D);
						*/

					List<AttachmentInfo> childrenInfos = children;
					for (int i = 0; i < childrenInfos.Count; i++)
					{
						if (childrenInfos[i].joint != null && childrenInfos[i].joint is Joint2D)
							((Joint2D)childrenInfos[i].joint).connectedBody = mainRigidbody2D;
					}

					for (int i = 0; i < joints2DConnectedToRigidbody.Count; i++)
						joints2DConnectedToRigidbody[i].connectedBody = mainRigidbody2D;
				}
				weldedToObjectP = null;
				List<TerminusObject> weldedObjs = weldedObjects;
				for (int i = 0; i < weldedObjs.Count; i++)
				{					
					weldedObjs[i].WeldToRigidbody(this,rigidbodyInfo == null,false);
				}
				oldWeldingRoot.RecalculateWeldedRigidbody();
				RecalculateWeldedRigidbody();
			}
		}
			
		/// <summary>
		/// Recalculates combined rigidbody of all welded parts. Should be called on TerminusObjects that owns combined rigidbody (highest object in welded hierarchy).
		/// </summary>
		/// <seealso cref="Settings.attachmentTypes.rigidbody_welding"/>
		public void RecalculateWeldedRigidbody()
		{
			if (weldedToObjectP == null)
			{
				List<TerminusObject> weldedObjs = weldedObjects;
				weldedObjs.Add(this);
				if (mainRigidbody != null)
				{					
					Vector3 center = Vector3.zero;

					float origMass = rigidbodyInfo.mass;
					float mass = 0;

					Matrix4x4 trMatr = Matrix4x4.identity;
					Matrix4x4 trMatr2 = Matrix4x4.identity;

					trMatr.SetTRS(mainRigidbody.transform.position,mainRigidbody.transform.rotation,Vector3.one);
					for (int i = 0; i < weldedObjs.Count; i++)
					{
						if (weldedObjs[i].rigidbodyInfo.parent != null)
						{
							trMatr2.SetTRS(weldedObjs[i].rigidbodyInfo.parent.position,weldedObjs[i].rigidbodyInfo.parent.rotation,Vector3.one);
							//center += transform.InverseTransformPoint(weldedObjs[i].rigidbodyInfo.parent.TransformPoint(weldedObjs[i].rigidbodyInfo.centerOfMass)) * weldedObjs[i].rigidbodyInfo.mass/origMass;
							center += trMatr.inverse.MultiplyPoint3x4(trMatr2.MultiplyPoint3x4(weldedObjs[i].rigidbodyInfo.centerOfMass)) * weldedObjs[i].rigidbodyInfo.mass/origMass;
							mass += weldedObjs[i].rigidbodyInfo.mass/origMass;
						}
					}
					mainRigidbody.centerOfMass = center / mass;
					mainRigidbody.mass = origMass * mass;
				}
				else
				{
					if (mainRigidbody2D != null)
					{
						Vector2 center = Vector2.zero;

						float origMass = rigidbodyInfo2D.mass;
						float mass = 0;

						Matrix4x4 trMatr = Matrix4x4.identity;
						Matrix4x4 trMatr2 = Matrix4x4.identity;

						trMatr.SetTRS(mainRigidbody2D.transform.position,mainRigidbody2D.transform.rotation,Vector3.one);
						for (int i = 0; i < weldedObjs.Count; i++)
						{
							if (weldedObjs[i].rigidbodyInfo2D.parent != null)
							{
								trMatr2.SetTRS(weldedObjs[i].rigidbodyInfo2D.parent.position,weldedObjs[i].rigidbodyInfo2D.parent.rotation,Vector3.one);
								//center += Utils.XY(transform.InverseTransformPoint(weldedObjs[i].rigidbodyInfo2D.parent.TransformPoint(weldedObjs[i].rigidbodyInfo2D.centerOfMass))) * weldedObjs[i].rigidbodyInfo2D.mass/origMass;
								center += Utils.XY(trMatr.inverse.MultiplyPoint3x4(trMatr2.MultiplyPoint3x4(weldedObjs[i].rigidbodyInfo2D.centerOfMass))) * weldedObjs[i].rigidbodyInfo2D.mass/origMass;
								mass += weldedObjs[i].rigidbodyInfo2D.mass/origMass;
							}
						}
						mainRigidbody2D.centerOfMass = center / mass;
						mainRigidbody2D.mass = origMass * mass;
					}
				}
			}
		}
			

		/// <summary>
		/// Removes symmetric siblings connections from this <see cref="TerminusObject"/> and all <see cref="TerminusObject.treeListDown"/> TerminusObjects.
		/// </summary>
		/// <remarks>
		/// Also removes this <see cref="TerminusObject"/> and all <see cref="TerminusObject.treeListDown"/> TerminusObjects from symmetric siblings lists of its symmetric siblings.
		/// </remarks>
		public void RemoveSymmetrySiblings()
		{
			for (int i = 0; i < symmetricSiblings.Count; i++)
			{
				TerminusObject[] symChildObjects = symmetricSiblings[i].treeListDown.Select(rec => rec.otherConnector.owner).ToArray();
				for (int x = 0; x < symChildObjects.Length; x++)
					symChildObjects[x].symmetricSiblings.Clear();
				symmetricSiblings[i].symmetricSiblings.Clear();
			}
			symmetricSiblings.Clear();
		}

		/// <summary>
		/// Gets original state of provided component. Used by <see cref="StateHandler"/> and <see cref="AttachmentHandler"/> 
		/// </summary>
		public OriginalComponentInfo GetSupposedComponentState(Component component)
		{
			if (originalComponentsParams.ContainsKey(component))
			{
				OriginalComponentInfo compInfo = originalComponentsParams[component];
				if (compInfo.stateAffected && stateHandler.inAssemblyState)
				{
					if (compInfo.attachmentAffected && attachmentHandler.inAttachingMode && (component is Collider || component is Collider2D) && attachmentHandler.overrideColliderLayers)
						return new OriginalComponentInfo(attachmentHandler.newLayer,true,false,false);
					else
						return new OriginalComponentInfo(compInfo.layer,true,false,false);
				}
				if (compInfo.attachmentAffected && attachmentHandler.inAttachingMode)
				{
					if ((component is Collider || component is Collider2D) && attachmentHandler.overrideColliderLayers)
						return new OriginalComponentInfo(attachmentHandler.newLayer,true,false,false);
					else
						return new OriginalComponentInfo(compInfo.layer,true,false,false);
				}
				return compInfo;
			}
			else
			{
				return new OriginalComponentInfo(0,false,false,false);
			}
		}


		/// <summary>
		/// Calls <see cref="AttachmentHandler.OnAttached"/> if recipient is not null.
		/// </summary>
		public void OnAttached()
		{
			if (attachmentHandler != null)
				attachmentHandler.OnAttached();
		}

		/// <summary>
		/// Calls <see cref="AttachmentHandler.OnAttachmentStart"/> if recipient is not null.
		/// </summary>
		public void OnAttachmentStart()
		{
			if (attachmentHandler != null)
				attachmentHandler.OnAttachmentStart();
		}

		/// <summary>
		/// Calls <see cref="AttachmentHandler.ValidPlacementUpdate"/> if recipient is not null.
		/// </summary>
		public bool ValidPlacementUpdate()
		{
			if (attachmentHandler != null)
				return attachmentHandler.ValidPlacementUpdate();
			else
				return true;
		}

		/// <summary>
		/// Calls <see cref="AttachmentHandler.InvalidPlacementUpdate"/> if recipient is not null.
		/// </summary>
		public void InvalidPlacementUpdate()
		{
			if (attachmentHandler != null)
				attachmentHandler.InvalidPlacementUpdate();
		}

		/// <summary>
		/// Saves states of all components referenced by <see cref="StateHandler"/> and <see cref="AttachmentHandler"/> 
		/// </summary>
		public void SaveOriginalComponentState()
		{
			originalComponentsParams = new OrigComponentInfoDict();
			
			if (attachmentHandler != null)
			{
				for (int i = 0; i < attachmentHandler.affectedColliders.Count; i++)
				{
					originalComponentsParams.Add(attachmentHandler.affectedColliders[i],
					                             new OriginalComponentInfo(attachmentHandler.affectedColliders[i].gameObject.layer,
					                          attachmentHandler.affectedColliders[i].isTrigger,
					                          false,
					                          true));
				}
				for (int i = 0; i < attachmentHandler.affectedColliders2D.Count; i++)
				{
					originalComponentsParams.Add(attachmentHandler.affectedColliders2D[i],
					                             new OriginalComponentInfo(attachmentHandler.affectedColliders2D[i].gameObject.layer,
					                          attachmentHandler.affectedColliders2D[i].isTrigger,
					                          false,
					                          true));
				}
				for (int i = 0; i < attachmentHandler.affectedRigidbodies.Count; i++)
				{
					originalComponentsParams.Add(attachmentHandler.affectedRigidbodies[i],
					                             new OriginalComponentInfo(attachmentHandler.affectedRigidbodies[i].gameObject.layer,
												attachmentHandler.affectedRigidbodies[i].GetComponent<Rigidbody>().isKinematic,
					                          	false,
					                          	true));
				}
				for (int i = 0; i < attachmentHandler.affectedRigidbodies2D.Count; i++)
				{
					originalComponentsParams.Add(attachmentHandler.affectedRigidbodies2D[i],
					                             new OriginalComponentInfo(attachmentHandler.affectedRigidbodies2D[i].gameObject.layer,
												attachmentHandler.affectedRigidbodies2D[i].GetComponent<Rigidbody2D>().isKinematic,
					                          	false,
					                          	true));
				}
			}
			if (stateHandler != null)
			{
				for (int i = 0; i < stateHandler.affectedRigidbodies.Count; i++)
				{
					if (!originalComponentsParams.ContainsKey(stateHandler.affectedRigidbodies[i]))
					{
						originalComponentsParams.Add(stateHandler.affectedRigidbodies[i],
						                             new OriginalComponentInfo(stateHandler.affectedRigidbodies[i].gameObject.layer,
													stateHandler.affectedRigidbodies[i].GetComponent<Rigidbody>().isKinematic,
						                          	true,
						                          	false));
					}
					else
					{
						originalComponentsParams[stateHandler.affectedRigidbodies[i].transform] = new OriginalComponentInfo(stateHandler.affectedRigidbodies[i].gameObject.layer,
																													stateHandler.affectedRigidbodies[i].GetComponent<Rigidbody>().isKinematic,
						                                                                                          true,
						                                                                                          true);
					}
				}
				for (int i = 0; i < stateHandler.affectedRigidbodies2D.Count; i++)
				{
					if (!originalComponentsParams.ContainsKey(stateHandler.affectedRigidbodies2D[i]))
					{
						originalComponentsParams.Add(stateHandler.affectedRigidbodies2D[i],
						                             new OriginalComponentInfo(stateHandler.affectedRigidbodies2D[i].gameObject.layer,
													stateHandler.affectedRigidbodies2D[i].GetComponent<Rigidbody2D>().isKinematic,
						                          	true,
						                          	false));
					}
					else
					{
						originalComponentsParams[stateHandler.affectedRigidbodies2D[i]] = new OriginalComponentInfo(stateHandler.affectedRigidbodies2D[i].gameObject.layer,
																															stateHandler.affectedRigidbodies2D[i].GetComponent<Rigidbody2D>().isKinematic,
						                                                                                            		true,
						                                                                                            		true);
					}
				}
			}
			if (mainRigidbody != null && originalComponentsParams.ContainsKey(mainRigidbody.transform))
				mainRBodyInfo = originalComponentsParams[mainRigidbody.transform];
			else
				mainRBodyInfo = null;
			
			if (mainRigidbody2D != null && originalComponentsParams.ContainsKey(mainRigidbody2D.transform))
				mainRBodyInfo2D = originalComponentsParams[mainRigidbody2D.transform];
			else
				mainRBodyInfo2D = null;
		}


		// Handles detachment on joint breaks for ports using owners rigidbody
		protected IEnumerator OnJointBreak(float breakForce)
		{	
			List<Port> portsToCheck = new List<Port>();
			for (int i = 0; i < connectors.Count; i++)
			{
				if (connectors[i] is Port)
				{
					if (connectors[i].useOwnerRigidbody
					    && ((Port)connectors[i]).attachmentInfo.attachmentType != AttachmentInfo.Types.none
					    && ((Port)connectors[i]).attachmentInfo.joint != null)
					{
						portsToCheck.Add((Port)connectors[i]);
					}
				}
			}
			if (portsToCheck.Count == 0)
				yield break;
			yield return null;
			for (int i = 0; i < portsToCheck.Count; i++)
			{
				if (portsToCheck[i].attachmentInfo.attachmentType != AttachmentInfo.Types.none
				    && portsToCheck[i].attachmentInfo.joint == null)
				{
					portsToCheck[i].Detach();
				}
			}
		}


		void Awake ()
		{
			allInfluences = new List<ZoneInteraction>();

			if (attachmentHandler == null)		
				attachmentHandler = GetComponent<AttachmentHandler>();
			if (stateHandler == null)
				stateHandler = GetComponent<StateHandler>();

			if (inAssemblyState)
				ChangedAssemblyState();
		}


		void Start () 
		{
			Manager.RegisterObject(this);
			if (Manager.Instance != null && Manager.Instance.globalPlaymode)
			{
				isPlaymodeCloneP = true;
			}
				

			if (mode == Modes.being_attached && currentAttachmentPort == null)
			{
				SetNextAttachmentPort(0);		
			}
		}

        /// <summary>
        /// Checks all components on this GameObject for interfaces that recieve Terminus events (OnBeforeAttachment, OnAfterAttachment, OnBeforeDetachment, OnAfterDetachment) and saves them.
        /// </summary>
        public void CacheInterfaceRecievers()
        {            
            cachedOnBeforeAttachmentList = gameObject.GetComponents<IOnBeforeAttachment>();
            cachedOnAfterAttachmentList = gameObject.GetComponents<IOnAfterAttachment>();
            cachedOnBeforeDetachmentList = gameObject.GetComponents<IOnBeforeDetachment>();
            cachedOnAfterDetachmentList = gameObject.GetComponents<IOnAfterDetachment>();
        }

        /// <summary>
        /// Same as <see cref="TerminusObject.CacheInterfaceRecievers"/> but returns whether cached recievers changed after. Used in custom editor.
        /// </summary>        
        public bool CacheInterfaceRecieversWithChangeCheck()
        {
            IOnBeforeAttachment[] _cachedOnBeforeAttachmentList = gameObject.GetComponents<IOnBeforeAttachment>();
            IOnAfterAttachment[] _cachedOnAfterAttachmentList = gameObject.GetComponents<IOnAfterAttachment>();
            IOnBeforeDetachment[] _cachedOnBeforeDetachmentList = gameObject.GetComponents<IOnBeforeDetachment>();
            IOnAfterDetachment[] _cachedOnAfterDetachmentList = gameObject.GetComponents<IOnAfterDetachment>();
            bool changed = false;
            if (cachedOnBeforeAttachmentList == null || !_cachedOnBeforeAttachmentList.SequenceEqual(cachedOnBeforeAttachmentList))
            {
                cachedOnBeforeAttachmentList = _cachedOnBeforeAttachmentList;
                changed = true;
            }
            if (cachedOnAfterAttachmentList == null || !_cachedOnAfterAttachmentList.SequenceEqual(cachedOnAfterAttachmentList))
            {
                cachedOnAfterAttachmentList = _cachedOnAfterAttachmentList;
                changed = true;
            }
            if (cachedOnBeforeDetachmentList == null || !_cachedOnBeforeDetachmentList.SequenceEqual(cachedOnBeforeDetachmentList))
            {
                cachedOnBeforeDetachmentList = _cachedOnBeforeDetachmentList;
                changed = true;
            }
            if (cachedOnAfterDetachmentList == null || !_cachedOnAfterDetachmentList.SequenceEqual(cachedOnAfterDetachmentList))
            {
                cachedOnAfterDetachmentList = _cachedOnAfterDetachmentList;
                changed = true;
            }
            return changed;
        }

        /// <summary>
        /// Sends OnBeforeAttachment to cached recievers.
        /// </summary>
        public void OnBeforeAttachment(AttachmentInfo attachmentInfo)
        {
            if (cachedOnBeforeAttachmentList == null)
                CacheInterfaceRecievers();
            for (int i = 0; i < cachedOnBeforeAttachmentList.Length; i++)
                cachedOnBeforeAttachmentList[i].OnBeforeAttachment(attachmentInfo);
        }

        /// <summary>
        /// Sends OnAfterAttachment to cached recievers.
        /// </summary>
        public void OnAfterAttachment(AttachmentInfo attachmentInfo)
        {
            if (cachedOnAfterAttachmentList == null)
                CacheInterfaceRecievers();
            for (int i = 0; i < cachedOnAfterAttachmentList.Length; i++)
                cachedOnAfterAttachmentList[i].OnAfterAttachment(attachmentInfo);
        }

        /// <summary>
        /// Sends OnBeforeDetachment to cached recievers.
        /// </summary>
        public void OnBeforeDetachment(AttachmentInfo attachmentInfo)
        {
            if (cachedOnBeforeDetachmentList == null)
                CacheInterfaceRecievers();
            for (int i = 0; i < cachedOnBeforeDetachmentList.Length; i++)
                cachedOnBeforeDetachmentList[i].OnBeforeDetachment(attachmentInfo);
        }


        /// <summary>
        /// Sends OnAfterDetachment to cached recievers.
        /// </summary>
        public void OnAfterDetachment(AttachmentInfo attachmentInfo)
        {
            if (cachedOnAfterDetachmentList == null)
                CacheInterfaceRecievers();
            for (int i = 0; i < cachedOnAfterDetachmentList.Length; i++)
                cachedOnAfterDetachmentList[i].OnAfterDetachment(attachmentInfo);
        }

        // Debug gizmo for center of mass
        /*
		void Update ()
		{
			if (mainRigidbody != null)
			{
				Debug.DrawLine(transform.TransformPoint(mainRigidbody.centerOfMass + Vector3.up * 0.3f),transform.TransformPoint(mainRigidbody.centerOfMass - Vector3.up * 0.3f),Color.green);
				Debug.DrawLine(transform.TransformPoint(mainRigidbody.centerOfMass + Vector3.left * 0.3f),transform.TransformPoint(mainRigidbody.centerOfMass - Vector3.left * 0.3f),Color.red);
				Debug.DrawLine(transform.TransformPoint(mainRigidbody.centerOfMass + Vector3.forward * 0.3f),transform.TransformPoint(mainRigidbody.centerOfMass - Vector3.forward * 0.3f),Color.blue);
			}
		}
		*/

        void OnApplicationQuit()
		{
			appQuit = true;
		}

		public static void DestroyObject(GameObject obj, float t = 0)
		{						
			TerminusObject tObj = obj.GetComponent<TerminusObject>();
			if (tObj != null)
			{
				for (int i = 0; i < tObj.connectors.Count; i++)
				{					
					if (tObj.connectors[i] is Port)
					{					
						Port port = (Port)tObj.connectors[i];
						if (port.attachmentInfo.attachmentType != AttachmentInfo.Types.none && !(port.attachmentInfo.otherConnector.owner.destroyFlag))
						{
							port.Detach(tObj.destroyChildrenAlong,port.attachmentInfo.attachmentType == AttachmentInfo.Types.child);
						}
					}
				}
			}
			Destroy(obj,t);
		}
			


		void OnDestroy() 
		{			
			if (!appQuit && !destroyFlagP && !Manager.quitFlag)
			{
				destroyFlagP = true;
				TerminusObject root = treeRoot;
				for (int i = 0; i < connectors.Count; i++)
				{
					connectors[i].destroyFlag = true;
					if (connectors[i] is Port)
					{					
						Port port = (Port)connectors[i];
						#if UNITY_EDITOR
						if (port.attachmentInfo.attachmentType != AttachmentInfo.Types.none && !(port.attachmentInfo.otherConnector.owner == null || port.attachmentInfo.otherConnector.owner.destroyFlag))
						{							
							Connector otherConn = port.attachmentInfo.otherConnector;
							TerminusObject otherObj = otherConn.owner;
							if (port.attachmentInfo.attachmentType == AttachmentInfo.Types.parent && (destroyChildrenAlong || (!Application.isPlaying && port.attachmentInfo.hierarchyAttachment)))
							{
								if (Application.isPlaying)
									Destroy(port.attachmentInfo.otherConnector.owner.gameObject);
								else
									DestroyImmediate(port.attachmentInfo.otherConnector.owner.gameObject);
							}
							else
							{								
								port.Detach(false,port.attachmentInfo.attachmentType == AttachmentInfo.Types.child,true,false,root);
							}
							if (otherConn != null)
								EditorUtility.SetDirty(otherConn);
							if (otherObj != null)
								EditorUtility.SetDirty(otherObj);
						}
						Manager.UnregisterConnector(port);
						if (Manager.Instance != null)
							EditorUtility.SetDirty(Manager.Instance);
						#else	
						if (port.attachmentInfo.attachmentType != AttachmentInfo.Types.none && !(port.attachmentInfo.otherConnector.owner == null || port.attachmentInfo.otherConnector.owner.destroyFlag))
						{
							if (destroyChildrenAlong && port.attachmentInfo.attachmentType == AttachmentInfo.Types.parent)
							{								
								Destroy(port.attachmentInfo.otherConnector.owner.gameObject);
							}
							else
							{								
								port.Detach(false,port.attachmentInfo.attachmentType == AttachmentInfo.Types.child,true,false,root);
							}
						}
						Manager.UnregisterConnector(port);
						#endif
					}
				}
				if (symmetricSiblings != null && symmetricSiblings.Count > 0)
				{
					for (int i = 0; i < symmetricSiblings.Count; i++)
					{
						if (symmetricSiblings[i] != null && !symmetricSiblings[i].destroyFlag)
							symmetricSiblings[i].symmetricSiblings.Remove(this);
					}
				}
				Manager.UnregisterObject(this);
			}
		}
	}
}