using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Terminus 
{
	/// <summary>
	/// Base class for connectors. Connectors are objects through which <see cref="TerminusObject"/>s can be attached to each other.<para>   </para>
	/// This is abstract class, <see cref="Port"/> and <see cref="Surface"/> derivatives contain real functionality.
	/// </summary>
	/// <seealso cref="Port"/>
	/// <seealso cref="Surface"/>
	/// <seealso cref="TerminusObject"/>
	[ExecuteInEditMode]
	public abstract class Connector : MonoBehaviour {

        [HideInInspector]
        [SerializeField]
        protected IOnBeforeAttachment[] cachedOnBeforeAttachmentList;
        [HideInInspector]
        [SerializeField]
        protected IOnAfterAttachment[] cachedOnAfterAttachmentList;
        [HideInInspector]
        [SerializeField]
        protected IOnBeforeDetachment[] cachedOnBeforeDetachmentList;
        [HideInInspector]
        [SerializeField]
        protected IOnAfterDetachment[] cachedOnAfterDetachmentList;

        /// <summary>
        /// Draw gizmo in editor. No effect outside editor.
        /// </summary>
        public bool drawGizmos = true;

		/// <summary>
		/// If true, <see cref="Connector.connectorRigidbody"/> will provide <see cref="TerminusObject.mainRigidbody"/> instead.<para>   </para>
		/// Attachment through welding rigidbodies is possible only with this type of connectors.
		/// </summary>
		public bool useOwnerRigidbody = true;

		/// <summary>
		/// Rigidbody associated with connector.<para>   </para>
		/// Either rigidbody placed on same GameObject as this connector, or owner.mainRigidbody if <see cref="Connector.useOwnerRigidbody"/> set to true.<para>   </para>
		/// Used in physics-based attachments.
		/// </summary>
		public Rigidbody connectorRigidbody
		{
			get
			{
				if (!useOwnerRigidbody)
				{
					if (connectorRigidbodyP == null)
						connectorRigidbodyP = GetComponent<Rigidbody>();
					return connectorRigidbodyP;
				}
				else
				{
					if (owner.weldedToObject == null)
						return owner.mainRigidbody;
					else
						return owner.weldedToObject.mainRigidbody;
				}
			}
		}
		protected Rigidbody connectorRigidbodyP;

		/// <summary>
		/// Rigidbody2D associated with connector.<para>   </para>
		/// Either rigidbody2D placed on same GameObject as this connector, or owner.mainRigidbody2D if <see cref="Connector.useOwnerRigidbody"/> set to true.<para>   </para>
		/// Used in 2D physics-based attachments.
		/// </summary>
		public Rigidbody2D connectorRigidbody2D
		{
			get
			{
				if (!useOwnerRigidbody)
				{
					if (connectorRigidbody2DP == null)
						connectorRigidbody2DP = GetComponent<Rigidbody2D>();
					return connectorRigidbody2DP;
				}
				else
				{
					if (owner.weldedToObject == null)
						return owner.mainRigidbody2D;
					else
						return owner.weldedToObject.mainRigidbody2D;
				}
			}
		}
		protected Rigidbody2D connectorRigidbody2DP;

		/// <summary>
		/// Information about rigidbody for use during rigidbody welding. Filled automatically, change at your own risk.
		/// </summary>
		public RigidbodyInfo rigidbodyInfo;

		/// <summary>
		/// Information about rigidbody2D for use during rigidbody welding. Filled automatically, change at your own risk.
		/// </summary>
		public RigidbodyInfo2D rigidbodyInfo2D;

		/// <summary>
		/// Unique name of this connector. Single <see cref="TerminusObject"/> can't have two connectors with the same name.
		/// </summary>
		public string connectorName;

		[SerializeField]
		protected TerminusObject ownerP;
		[SerializeField]
		protected int layerP;
		[SerializeField]
		protected bool overrideLayerOptionsP;
		[SerializeField]
		protected Settings.LayerPortOptions overridenOptionsP;
		[SerializeField]
		protected bool activeP = true;

		/// <summary>
		/// If set to true, changing connector settings will not cause reinitialization of this connector by <see cref="Manager"/>. Use this to change connector parameters in bulk for better performance.
		/// </summary>
		/// <example>
		/// Example of bulk parameter changing.
		/// <code>
		/// public Terminus.Port portConnector;
		/// 
		/// public void ChangePortOptions()
		/// {
		/// 	//Disabling auto-reinitialization
		/// 	portConnector.stopReinitialization = true;
		/// 	portConnector.layer = 1;
		/// 	portConnector.isCurrentAttachmentPort = true;
		/// 	portConnector.offset = Vector3.zero;
		/// 	portConnector.Initialize();
		/// 	//Enabling auto-reinitialization
		/// 	portConnector.stopReinitialization = false;
		/// }
		/// </code>
		/// </example>
		public bool stopReinitialization = false;

		/// <summary>
		/// True if connector is being destroyed. Set during OnDestroy() event, so will not be set correctly if component is disabled.
		/// </summary>
		public bool destroyFlag = false;



		/// <summary>
		/// <see cref="TerminusObject"/> connector belongs to. Connector should always belong to TerminusObject.<para>   </para>
		/// Connector can be on different GameObject from its owner, as long as owner is designated.
		/// </summary>
		public TerminusObject owner
		{
			get
			{
				return ownerP;
			}
			set
			{
				ownerP = value;
				if ((Application.isPlaying && !stopReinitialization) || (!Application.isPlaying && gameObject.activeInHierarchy))
	                Initialize();
	        }
	    }

		/// <summary>
		/// Terminus layer of connector. Terminus <see cref="Settings.layers"/> are separate from Unity layers and are set up from "Layers" tab in Terminus window.<para>   </para>
		/// Determines behaviour of this connector and also what connectors can influence it.
		/// </summary>
		public int layer
		{
			get
			{
				return layerP;
			}
			set
			{
				layerP = value;
				if ((Application.isPlaying && !stopReinitialization) || (!Application.isPlaying && gameObject.activeInHierarchy))
	                Initialize();
	        }
	    }

		/// <summary>
		/// If true, <see cref="Connector.overridenOptions"/> are used instead of options inherited from <see cref="Connector.layer"/> 
		/// </summary>
		public bool overrideLayerOptions
		{
			get
			{
				return overrideLayerOptionsP;
			}
			set
			{
				overrideLayerOptionsP = value;
				if ((Application.isPlaying && !stopReinitialization) || (!Application.isPlaying && gameObject.activeInHierarchy))
					Initialize();
			}
		}

		/// <summary>
		/// Options to use for this connector if <see cref="Connector.overrideLayerOptions"/> set to true.
		/// </summary>
		public Settings.LayerPortOptions overridenOptions
		{
			get
			{
				return overridenOptionsP;
			}
			set
			{
				overridenOptionsP = value;
				if ((Application.isPlaying && !stopReinitialization) || (!Application.isPlaying && gameObject.activeInHierarchy))
					Initialize();
			}
		}

		/// <summary>
		/// Prevents connector from participating in attchment and influence events when set to false.<para>   </para>
		/// It is preferrable to use this instead of .enabled property of MonoBehaviour.
		/// </summary>
		public bool active
		{
			get
	        {
	            return activeP;
	        }
	        set
	        {
	            activeP = value;
				if ((Application.isPlaying && !stopReinitialization) || (!Application.isPlaying && gameObject.activeInHierarchy))
	                Initialize();
	        }
	    }

		/// <summary>
		/// Returns current options of connector.<para>   </para>
		/// If your code requests this property ofter, it is recommended to cache it into variable.
		/// </summary>
		/// <seealso cref="Connector.layer"/>
		/// <seealso cref="Connector.overrideLayerOptions"/> 
		/// <seealso cref="Connector.overridenOptions"/>
		public Settings.LayerPortOptions portOptions
		{
			get
			{
				if (overrideLayerOptions)
					return overridenOptions;
				else
				{
					return ProjectManager.settings.layers[layer];
				}
			}
		}

		/// <summary>
		/// Indicates whether this <see cref="Terminus.Connector"/> can be attached to other connectors.
		/// </summary>
		/// <value><c>true</c> if is attachable; otherwise, <c>false</c>.</value>
		public virtual bool isAttachable
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Indicates whether this <see cref="Terminus.Connector"/> attachment slots(1 for <see cref="Port"/>, several for <see cref="Surface"/>) are full .
		/// </summary>
		/// <value><c>true</c> if is occupied; otherwise, <c>false</c>.</value>
		public virtual bool isOccupied
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Indicates whether this <see cref="Terminus.Connector"/> can accept attachments from other connector.
		/// </summary>
		public virtual bool isAccepting
		{
			get
			{
				return active && !isOccupied && (owner.mode == TerminusObject.Modes.free_connections || owner.mode == TerminusObject.Modes.accepting_attachments);
			}
		}

		/// <summary>
		/// Returns int value with single bit corresponding to <see cref="Connector.layer"/> 
		/// </summary>
		public virtual int layerBit
		{
			get
			{
				return 1 << layer;
			}
		}

		/// <summary>
		/// Gets index of connector inside owner <see cref="TerminusObject.connectors"/> .
		/// </summary>
		public virtual int index
		{
			get
			{
				if (owner != null && owner.connectors != null & owner.connectors.Count > 0)
					return owner.connectors.IndexOf(this);
				else
					return -1;
			}
		}

		/// <summary>
		/// List of active attachments this connector is participating.
		/// </summary>
		public virtual List<AttachmentInfo> attachmentsInfo
		{
			get
			{
				return new List<AttachmentInfo>();
			}
		}

		/// <summary>
		/// Worldspace rotation of this connector.
		/// </summary>
		public virtual Quaternion globalRotation
		{
			get
			{
				return transform.rotation;
			}
		}

		/// <summary>
		/// Worldspace position of this connector.
		/// </summary>
		public virtual Vector3 globalPosition
		{
			get
			{
				return transform.position;
			}
		}

		/// <summary>
		/// Initializes(or re-initializes) this connector with <see cref="Manager"/> component. Called when connector options or status changes.
		/// </summary>
		public virtual void Initialize ()
		{
			Manager.RegisterConnector(this);
		}

		/// <summary>
		/// Gets attachment of this connector with provided connector if it exists. <see cref="AttachmentInfo.attachmentType"/> set to <see cref="AttachmentInfo.Types.none"/> if no such attachment exists.
		/// </summary>
		/// <param name="connector">Opposite connector to search for.</param>
		public virtual AttachmentInfo GetAttachmentInfo(Connector connector)
		{
			return new AttachmentInfo();
		}

		/// <summary>
		/// Registers attachment from other connector. Used for internal puposes, should not be called unless you know excactly what you're doing.
		/// </summary>
		public virtual void RegisterAttachmentFromConnector(AttachmentInfo attachInfo)
		{
			
		}

		/// <summary>
		/// Registers detachment from other connector. Used for internal puposes, should not be called unless you know excactly what you're doing.
		/// </summary>
		public virtual void RegisterDetachmentFromConnector(Connector conn)
		{
			
		}
			
		/// <summary>
		/// Aligns provided port with this connector.
		/// Exists to allow for creating custom connectors such as tilable/grid surfaces.
		/// </summary>
		/// <returns><c>true</c>, if port was aligned with this connector, <c>false</c> otherwise - in case connector was of smiple Surface type.</returns>
		/// <param name="Port">Port to align</param>
		/// <param name="position">Desired position of port.</param>
		/// <param name="rotation">Desired rotation of port.</param>
		/// <param name="collider">Collider - usually provided by raycasting when placing port on surfaces.</param>
		public virtual bool AlignPortWithThisConnector(Port port, Vector3 position, Quaternion rotation, Component collider)
		{
			return false;
		}

		/// <summary>
		/// Gets provided number of symmetric positions for attaching multiple <see cref="TerminusObject"/>s symmetrically.
		/// </summary>
		/// <param name="positionCount">Position count. Use <see cref="Connector.AchievableSymmetryCount"/> to prevent over-drafting symmetry count.</param>
		/// <param name="originalPosition">Global position of <see cref="Port"/>  that tries to attach to this connector.</param>
		/// <param name="originalRotation">Global rotation of <see cref="Port"/>  that tries to attach to this connector.</param>
		/// <param name="collider">Collider provided by raycasting placement of part. Only comes into play when using <see cref="Surface"/> connector.</param>
		public virtual List<PositionInfo> GetSymmetryPositions(int positionCount, Vector3 originalPosition, Quaternion originalRotation, Component collider = null)
		{
			return new List<PositionInfo>();
		}

		/// <summary>
		/// Calculates closest number of possible symmetric points based on provided desired number. 
		/// </summary>
		/// <returns>The symmetry count.</returns>
		/// <param name="desiredCount">Desired count.</param>
		/// <seealso cref="TerminusObject.symmetryGroups"/>
		/// <seealso cref="Surface.symmetryType"/>
		public virtual int AchievableSymmetryCount(int desiredCount)
		{
			if (owner.symmetricSiblings == null || owner.symmetricSiblings.Count == 0 || desiredCount != owner.symmetricSiblings.Count)
				return 0;
			return owner.symmetricSiblings.Count;
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
        /// Same as <see cref="Connector.CacheInterfaceRecievers"/> but returns whether cached recievers changed after. Used in custom editor.
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


        // Use this for initialization
        protected virtual void Start () 
		{
			owner.RegisterConnector(this);
			Initialize();
		}

		/*
		protected virtual void OnDisable ()
		{
			Manager.UnregisterConnector(this);
		}
		*/

		protected virtual void OnEnable()
		{
			if (!destroyFlag)
			{
				if (ownerP == null)
				{
					Transform tr = transform;
					while (tr.gameObject.GetComponent<TerminusObject>() == null && tr.parent != null)
						tr = tr.parent;
					ownerP = tr.gameObject.GetComponent<TerminusObject>();
					if (ownerP != null)
						ownerP.RegisterConnector(this);
				}
				Manager.RegisterConnector(this);
			}
		}


		protected virtual void OnDestroy ()
		{
			destroyFlag = true;
			if (!Manager.quitFlag && !owner.destroyFlag)
			{				
				Manager.UnregisterConnector(this);
			}
		}
	}
}