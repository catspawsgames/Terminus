using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Terminus 
{
	///<summary>
	/// Class containing information about Terminus objects and connectors currently active on scene.
	/// It also performs periodic updates to check what connectors can interact with each other.
	///</summary>
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(-51)]
	[AddComponentMenu("Terminus modular system/Manager")]
	public class Manager : MonoBehaviour {
		///<summary>
		/// What Unity event will be used to perform regular check for possible interactions between connectors.
		///</summary>
		public UpdateEvent updateEvent;
		///<summary>
		/// If some of your connectors have global messaging enabled, this object will recieve messages from them.
		///</summary>
		public GameObject globalEventsReciever;
        protected GameObject cachedGlobalEventsReciever;
        protected IOnBeforeAttachment[] cachedOnBeforeAttachmentList;
        protected IOnAfterAttachment[] cachedOnAfterAttachmentList;
        protected IOnBeforeDetachment[] cachedOnBeforeDetachmentList;
        protected IOnAfterDetachment[] cachedOnAfterDetachmentList;


        ///<summary>
        /// <see cref="TerminusObject"/> with no parent(or their containers) will be parented to this transform.
        ///</summary>
        public Transform globalTransform;

		/// <summary>
		/// Global environment connector for attaching objects to environemnt instead of other objects.
		/// This surface don't need to contain any colliders, all colliders withing <see cref="Manager.environmentLayers"/> will be interpreted as part of this surface.
		/// </summary>
		public Surface environmentSurface;

		/// <summary>
		/// Layers that will be interpreted as part of <see cref="Manager.environmentSurface"/>.
		/// </summary>
		public LayerMask environmentLayers;

		protected bool quitFlagP = false;

		protected bool globalPlaymodeP;

		//protected bool cleanupCheck = false;

		protected List<Container>[] containerCleanupQuery;
		protected int currentContainerQuery = 0;
        

		///<summary>
		/// Toggle for entering and exiting global playmode. Global playmode tries to put all <see cref="TerminusObject"/>s on scene into <see cref="TerminusObject.inPlaymode">playmode</see>.
		/// Setting it to true calls <see cref="Maganer.EnterGlobalPlaymode"/>, and setting it to false calls <see cref="Manager.ExitGlobalPlaymode"/>.
		///</summary>
		/// <seealso cref="Manager.EnterGlobalPlaymode"/>
		/// <seealso cref="Manager.ExitGlobalPlaymode"/>
		/// <seealso cref="TerminusObject.inPlaymode"/>
		public bool globalPlaymode
		{
			get
			{
				return globalPlaymodeP;
			}
			set
			{
				if (value)
				{
					EnterGlobalPlaymode();
				}
				else
				{
					ExitGlobalPlaymode();
				}
			}			
		}

		///<summary>
		/// Set to true when application is quitting.
		///</summary>
		public static bool quitFlag
		{
			get
			{
				if (Instance != null)
					return Instance.quitFlagP;
				else
					return false;
			}
		}

		///<summary>
		/// Current layers. Loaded from global Terminus.Settings on play.
		///</summary>
		public Settings.LayerPortOptions[] layers
		{
			get
			{
				return layersP;
			}
		}

		///<summary>
		/// Returns all root <see cref="TerminusObject"/>s (objects with no Terminus parent).
		///</summary>
		public List<TerminusObject> rootObjects
		{
			get
			{
				return registeredObjectsP.FindAll(rec => rec != null && rec.parent.attachmentType == AttachmentInfo.Types.none).ToList();
			}
		}

		///<summary>
		/// Returns all <see cref="TerminusObject"/>s on scene.
		///</summary>
		public List<TerminusObject> registeredObjects
		{
			get
			{
				return registeredObjectsP;
			}
		}

		///<summary>
		/// Returns all active <see cref="Connector"/>s  on scene.
		///</summary>
		public List<Connector> registeredConnectors
		{
			get
			{
				return registeredConnectorsP;
			}
		}

		///<summary>
		/// Returns all <see cref="Connector"/>s that are in "accepting connections" state.
		///</summary>
		public List<Connector> acceptingConnectors
		{
			get
			{
				return acceptingConnectorsP;
			}
		}

		///<summary>
		/// Returns all <see cref="Port"/>s that are in "being attached" state. (<see cref="Surface"/>s  can't be in that state).
		///</summary>
		public List<Port> activePorts
		{
			get
			{
				return activePortsP;
			}
		}

		protected Settings.LayerPortOptions[] layersP;
		[SerializeField]
		protected List<TerminusObject> registeredObjectsP;
		[SerializeField]
		protected List<Connector> registeredConnectorsP;
		[SerializeField]
		protected List<Connector> acceptingConnectorsP;
		[SerializeField]
		protected List<Port> activePortsP;

		protected List<Container> freeContainers = new List<Container>();

		protected bool addedNonIdlePorts;


		///<summary>
		/// Types of Unity events that can be used to to perform regular check for possible connections between connectors.
		///</summary>
		public enum UpdateEvent
		{
			LateUpdate = 0,
			Update = 1,
			FixedUpdate = 2,
			ManualUpdate = 3
		}


		///<summary>
		/// Singleton linking to currently active instance of Manager class.
		///</summary>
		public static Manager Instance { get; private set;}


		///<summary>
		/// <see cref="globalTransform"/>  of current <see cref="Manager.Instance"/> . Null if there's no active instance of Manager.
		///</summary>
		public static Transform staticGlobalTransform
		{
			get
			{
				if (Instance == null)
					return null;
				else
					return Instance.globalTransform;
			}
		}


		///<summary>
		/// Creates lists of current <see cref="TerminusObject"/>s and <see cref="Connector"/>s.
		///</summary>
		///<param name="forceRecreate"> If set to false, will only create lists if they are null.</param>
		public void RecreateHierarchy (bool forceRecreate = false)
		{
			if (forceRecreate || acceptingConnectorsP == null)
				acceptingConnectorsP = new List<Connector>();
			if (forceRecreate || registeredConnectorsP == null)
				registeredConnectorsP = new List<Connector>();
			if (forceRecreate || activePortsP == null)
				activePortsP = new List<Port>();
			if (forceRecreate || registeredObjectsP == null)
				registeredObjectsP = new List<TerminusObject>();
		}


		///<summary>
		/// Reads current layer settings from global <see cref="Settings"/>  object.
		///</summary>
		public void UpdateSettings()
		{
			layersP = ProjectManager.settings.layers.ToArray();
		}

		///<summary>
		/// Assigns static <see cref="Manager.Instance"/>  to current object if it's currently null, or adds registered objects and connectors from this Manager instance to current static Instance.
		///</summary>
		public void DeclareSingleton()
		{
			if (Manager.Instance == null || Manager.Instance == this)
				Instance = this;
			else
			{
				for (int i = 0; i < registeredObjectsP.Count; i++)
					RegisterObject(registeredObjectsP[i]);
				for (int i = 0; i < registeredConnectorsP.Count; i++)
					RegisterConnector(registeredConnectorsP[i]);
				Destroy(this);
			}
		}


		///<summary>
		/// Returns current layer names.
		///</summary>
		public string[] GetlayersPNames()
		{
			return layersP.Select(rec => rec.name).ToArray();
		}

		///<summary>
		/// Gets cached <see cref="Container"/>  or creates new and assigns it to <see cref="TerminusObject"/>.
		///</summary>
		public static Container SetContainer(TerminusObject forObject)
		{
			if (!quitFlag && forObject != null)
			{
				Container container = null;
				if (Instance == null || Instance.freeContainers.Count == 0)
				{
					container = CreateContainer(forObject);
				}
				else
				{
					while (container == null && Instance.freeContainers.Count > 0)
					{
						container = Instance.freeContainers[0];
						if (container == null)
							Instance.freeContainers.RemoveAt(0);
					}
					if (container == null)
						container = CreateContainer(forObject);
					else
						Instance.freeContainers.RemoveAt(0);
					container.gameObject.SetActive(true);
					container.transform.parent = Instance.globalTransform;
					container.gameObject.name = forObject.gameObject.name + " (Container)";
					container.terminusObject = forObject;
				}
				container.transform.position = forObject.transform.position;
				container.transform.rotation = forObject.transform.rotation;
				Transform oldParent = forObject.transform.parent;
				forObject.transform.parent = container.transform;
				forObject.container = container;

				Transform[] childArr = forObject.treeListDown.Where(rec => rec.otherConnector.owner != null).Select(rec => rec.otherConnector.owner.transform).ToArray();
				for (int i = 0; i < childArr.Length; i++)
				{
					if (childArr[i].parent == oldParent)
						childArr[i].parent = container.transform;
				}
				return container;
			}
			else
				return null;
		}

		///<summary>
		/// Detaches <see cref="Container"/> from its current <see cref="TerminusObject"/> and puts it back into cache or deletes it depending on amount of containers avaliable in cache.
		///</summary>
		public static void FreeContainer(Container container)
		{
			if (container.terminusObject != null && container.terminusObject.container == container)
				container.terminusObject.container = null;
			if (Instance != null)
			{
				if (Instance.containerCleanupQuery == null)
				{
					Instance.containerCleanupQuery = new List<Container>[2];
					Instance.containerCleanupQuery[0] = new List<Container>();
					Instance.containerCleanupQuery[1] = new List<Container>();
				}
				Instance.containerCleanupQuery[Instance.currentContainerQuery].Add(container);
			}
			else
				Destroy(container.gameObject);
			/*
			if (Instance == null || Instance.freeContainers.Count >= 20)
			{
				container.destroyFlag = true;
				Destroy(container.gameObject);
			}
			else
			{
				container.transform.parent = Instance.transform;
				container.terminusObject = null;
				container.gameObject.SetActive(false);
				Instance.freeContainers.Add(container);
			}
			*/
		}

		protected void CleanupContainers()
		{
			if (containerCleanupQuery == null)
			{
				containerCleanupQuery = new List<Container>[2];
				containerCleanupQuery[0] = new List<Container>();
				containerCleanupQuery[1] = new List<Container>();
			}
			for (int i = 0; i < containerCleanupQuery[currentContainerQuery].Count; i++)
			{
				Container container = containerCleanupQuery[currentContainerQuery][i];
				if (freeContainers.Count >= 20)
				{
					container.destroyFlag = true;
					Destroy(container.gameObject);
				}
				else
				{
					container.transform.parent = Instance.transform;
					container.terminusObject = null;
					container.gameObject.SetActive(false);
					freeContainers.Add(container);
				}
			}
			containerCleanupQuery[currentContainerQuery].Clear();
			currentContainerQuery = 1 - currentContainerQuery;
		}

		protected static Container CreateContainer(TerminusObject forObject)
		{
			GameObject obj = new GameObject(forObject.gameObject.name + " (Container)");
			obj.transform.parent = staticGlobalTransform;
			Container cont = obj.AddComponent<Container>();
			cont.terminusObject = forObject;
			return cont;
		}



		///<summary>
		/// Enters global playmode. Identical to setting <see cref="Manager.globalPlaymode"/> to true.
		///</summary>
		/// <remarks>
		/// Enters all root <see cref="TerminusObjects"/> into <see cref="TerminusObject.inPlaymode">playmode</see> with preservation of sideway attachment between them.
		/// If you manually set every root <see cref="TerminusObject"/> to <see cref="TerminusObject.inPlaymode">playmode</see>, sideways attachment between them will still be connected to originals and not playmode clones.
		/// All <see cref="TerminusObjects"/> instantiated during global playmode will be designated as clones and destroyed upon exiting playmode. Call <see cref="TerminusObject.DeclarePlaymodeNonClone"/> to preserve it.
		/// </remarks>
        /// <param name="changeAssemblyState">If true, <see cref="TerminusObject.inAssemblyState"/> will be set to true on all created playmode clones.</param>
		public void EnterGlobalPlaymode(bool changeAssemblyState = true)
		{
			ExitGlobalPlaymode();

			System.Reflection.FieldInfo playmodeCloneField = typeof(TerminusObject).GetField("isPlaymodeCloneP",BindingFlags.NonPublic | BindingFlags.Instance);
			System.Reflection.FieldInfo inPlaymodeField = typeof(TerminusObject).GetField("inPlaymodeP",BindingFlags.NonPublic | BindingFlags.Instance);

			GameObject tempParent = new GameObject("TempPlaymodeObject");

			List<TerminusObject> curRootObjects = Instance.rootObjects;
			Dictionary<TerminusObject,Transform> originalParents = new Dictionary<TerminusObject, Transform>();
			for (int i = 0; i < curRootObjects.Count; i++)
			{
				originalParents.Add(curRootObjects[i],curRootObjects[i].containerOrSelf.transform.parent);
				curRootObjects[i].containerOrSelf.transform.SetParent(tempParent.transform);
			}

			GameObject tempCloneParent = Instantiate(tempParent);

			TerminusObject[] originals = tempParent.GetComponentsInChildren<TerminusObject>();
			TerminusObject[] clones = tempCloneParent.GetComponentsInChildren<TerminusObject>();

			for (int i = 0; i < originals.Length; i++)
			{
				inPlaymodeField.SetValue(originals[i],true);
				originals[i].playmodeClone = clones[i];
				playmodeCloneField.SetValue(clones[i],true);
				clones[i].playmodeOriginal = originals[i];
				if (changeAssemblyState)
					clones[i].inAssemblyState = false;
				if (clones[i].parent.attachmentType == AttachmentInfo.Types.none)
				{
					clones[i].containerOrSelf.transform.SetParent(originalParents[originals[i]]);
					originals[i].containerOrSelf.transform.SetParent(originalParents[originals[i]]);
					originals[i].containerOrSelf.SetActive(false);
					clones[i].containerOrSelf.SetActive(true);
				}
			}

			Destroy(tempParent,0.1f);
			Destroy(tempCloneParent,0.1f);

			globalPlaymodeP = true;			
		}

		///<summary>
		/// Exits global playmode. Identical to setting <see cref="Manager.globalPlaymode"/> to false.
		///</summary>
		public void ExitGlobalPlaymode()
		{			
			List<TerminusObject> curRootObjects = Instance.rootObjects;
			for (int i = 0; i < curRootObjects.Count; i++)
			{
				curRootObjects[i].ExitPlaymode();
			}

			globalPlaymodeP = false;
		}



		///<summary>
		/// Registers <see cref="Connector"/> or updates its state. Should be called if connector settings that affect its interaction with other connectors have been changed (called automatically by <see cref="Port"/> and <see cref="Surface"/> classes).
		///</summary>
		public static void RegisterConnector(Connector connector)
		{
			if (Instance != null)
				Instance.RegisterConnectorInternal(connector);

			if (Application.isPlaying && !Manager.quitFlag && !connector.destroyFlag && !connector.owner.destroyFlag
			    && connector.owner.gameObject.activeInHierarchy && connector.owner.createContainerWhenRoot
			    && connector.owner.parent.attachmentType == AttachmentInfo.Types.none && connector.owner.container == null)
			{
				SetContainer(connector.owner);
			}
		}



		protected void RegisterConnectorInternal(Connector connector)
		{
			if (connector.gameObject.activeInHierarchy)
			{
				//if (connector.layer == 8 && Application.isPlaying)					
					//Debug.Log(Time.time + " | Registering: " + connector.owner.gameObject.name + "." + connector.connectorName + " | " + connector.isAccepting.ToString());
				
				if (!registeredConnectorsP.Contains(connector))
					registeredConnectorsP.Add(connector);

				if (connector.isAttachable)
				{
					addedNonIdlePorts = true;
					if (connector.GetType() == typeof(Port) && !activePortsP.Contains((Port)connector))
					{
						activePortsP.Add((Port)connector);
					}
				}
				else
				{
					if (connector.GetType() == typeof(Port))
						activePortsP.Remove((Port)connector);
				}

				if (connector.isAccepting)
				{					
					addedNonIdlePorts = true;
					if (!acceptingConnectorsP.Contains(connector))
						acceptingConnectorsP.Add(connector);
				}
				else
				{
					acceptingConnectorsP.Remove(connector);
				}

				if (connector.GetType() == typeof(Port))
				{
					((Port)connector).CleanUpInfluenceInfo();
				}

				if (addedNonIdlePorts && !Application.isPlaying)
				{
					UpdateCheckDataInternal();
					addedNonIdlePorts = false;
				}
			}
		}



		///<summary>
		/// Deletes <see cref="Connector"/> from list of current connectors. Called automatically on connector destruction.
		///</summary>
		public static void UnregisterConnector (Connector connector)
		{
			if (Instance != null)
				Instance.UnregisterConnectorInternal(connector);
		}



		protected void UnregisterConnectorInternal (Connector connector)
		{
			//if (connector.layer == 8 && Application.isPlaying)
				//Debug.Log(Time.time + " | UNREGISTERING: " + connector.owner.gameObject.name + "." + connector.connectorName + " | " + connector.isAccepting.ToString());
			registeredConnectorsP.Remove(connector);
			if (connector.GetType() == typeof(Port))
				activePortsP.Remove((Port)connector);
			acceptingConnectorsP.Remove(connector);
			addedNonIdlePorts = true;
		}


		///<summary>
		/// Registers freshly-created <see cref="TerminusObject"/>. Called automatically.
		///</summary>
		public static void RegisterObject(TerminusObject obj)
		{
			if (Instance != null)
				Instance.RegisterObjectInternal(obj);
		}



		protected void RegisterObjectInternal(TerminusObject obj)
		{
			if (!registeredObjectsP.Contains(obj))
				registeredObjectsP.Add(obj);
		}


		///<summary>
		/// Unregister destroyed <see cref="TerminusObject"/>. Called automatically.
		///</summary>
		public static void UnregisterObject(TerminusObject obj)
		{
			if (Instance != null)
				Instance.UnregisterObjectInternal(obj);
			if (obj.container != null)
				FreeContainer(obj.container);
		}



		protected void UnregisterObjectInternal(TerminusObject obj)
		{
			registeredObjectsP.Remove(obj);
			addedNonIdlePorts = true;
		}



		protected void UpdateCheckDataInternal()
		{			
			registeredObjectsP.RemoveAll(rec => rec == null);
			registeredConnectorsP.RemoveAll(rec => rec == null);
			acceptingConnectorsP.RemoveAll(rec => rec == null);
			activePortsP.RemoveAll(rec => rec == null);
			for (int i = 0; i < activePortsP.Count; i++)
			{				
				UpdateCheckDataForPortInternal(activePortsP[i]);				
			}
			addedNonIdlePorts = false;
		}


		///<summary>
		/// Checks for destroyed <see cref="TerminusObject"/>s and <see cref="Connector"/>s and removes them from current lists, and updates lists of possible interactable connectors for all connectors.
		///</summary>
		/// <remarks>
		/// Also cleans up <see cref="TerminusObject.allInfluences"/>.
		/// </remarks>
		public static void UpdateCheckData()
		{
			if (Instance != null)
			{
				Instance.UpdateCheckDataInternal();
			}
		}



		protected void UpdateCheckDataForPortInternal(Port conn)
		{
			conn.connectorsToCheck.Clear();
			for (int x = 0; x < acceptingConnectorsP.Count; x++)
			{
				if (conn.owner != acceptingConnectorsP[x].owner
					&& (acceptingConnectorsP[x].portOptions.acceptsConnectionFromLayers & conn.layerBit) > 0)
				{
					conn.connectorsToCheck.Add(acceptingConnectorsP[x]);
				}
			}
			if (conn.portOptions.useInfluenceMessaging)
				conn.CleanUpInfluenceInfo();
			/*
			if (Application.isPlaying && conn.portOptions.useInfluenceMessaging)
			{
				int count = conn.owner.allInfluences.Count-1;
				for (int i = count; i >= 0; i--)
				{
					if (conn.owner.allInfluences[i].thisConnector == conn
					    && !conn.connectorsToCheck.Contains(conn.owner.allInfluences[i].otherConnector))
						conn.owner.allInfluences.RemoveAt(i);
				}
			}
			*/
		}


		///<summary>
		/// Updates lists of possible interactable <see cref="Connector"/>ss for provided connector.
		///</summary>
		public static void UpdateCheckDataForPort(Port conn)
		{
			if (Instance != null)
				Instance.UpdateCheckDataForPortInternal(conn);
		}


		///<summary>
		/// Searches all <see cref="Connector"/>s for valid interactions with other connectors.
		///</summary>
		public void CheckPossibleConnections()
		{
			for (int i = 0; i < activePortsP.Count; i++)
			{
				if (!activePortsP[i].owner.doNotAutoUpdate)
					activePortsP[i].CheckPossibleConnections();
			}
		}



        ///<summary>
        /// Calls OnBeforeAttachment on <see cref="Manager.globalEventsReciever"/>.
        ///</summary>		
        ///<param name="value"> This object will be provided as parameter to event.</param>
        public static void SendOnBeforeAttachmentToGlobalReciever(AttachmentInfo attachmentInfo)
        {            
            if (Instance != null && Instance.globalEventsReciever != null)
            {
                Instance.CheckGlobalReciever();                
                foreach (IOnBeforeAttachment iObj in Instance.cachedOnBeforeAttachmentList)                                    
                    iObj.OnBeforeAttachment(attachmentInfo);                
            }
        }

        ///<summary>
        /// Calls OnAfterAttachment on <see cref="Manager.globalEventsReciever"/>.
        ///</summary>		
        ///<param name="value"> This object will be provided as parameter to event.</param>
        public static void SendOnAfterAttachmentToGlobalReciever(AttachmentInfo attachmentInfo)
        {
            if (Instance != null && Instance.globalEventsReciever != null)
            {
                Instance.CheckGlobalReciever();
                foreach (IOnAfterAttachment iObj in Instance.cachedOnAfterAttachmentList)
                    iObj.OnAfterAttachment(attachmentInfo);
            }
        }

        ///<summary>
        /// Calls OnBeforeDetachment on <see cref="Manager.globalEventsReciever"/>.
        ///</summary>		
        ///<param name="value"> This object will be provided as parameter to event.</param>
        public static void SendOnBeforeDetachmentToGlobalReciever(AttachmentInfo attachmentInfo)
        {
            if (Instance != null && Instance.globalEventsReciever != null)
            {
                Instance.CheckGlobalReciever();
                foreach (IOnBeforeDetachment iObj in Instance.cachedOnBeforeDetachmentList)
                    iObj.OnBeforeDetachment(attachmentInfo);
            }
        }

        ///<summary>
        /// Calls OnAfterDetachment on <see cref="Manager.globalEventsReciever"/>.
        ///</summary>		
        ///<param name="value"> This object will be provided as parameter to event.</param>
        public static void SendOnAfterDetachmentToGlobalReciever(AttachmentInfo attachmentInfo)
        {
            if (Instance != null && Instance.globalEventsReciever != null)
            {
                Instance.CheckGlobalReciever();
                foreach (IOnAfterDetachment iObj in Instance.cachedOnAfterDetachmentList)
                    iObj.OnAfterDetachment(attachmentInfo);
            }
        }

        /// <summary>
        /// Checks if <see cref="Manager.globalEventsReciever"/> is the same as it was when event interfaces were cached.
        /// </summary>
        protected void CheckGlobalReciever()
        {
            if (globalEventsReciever != cachedGlobalEventsReciever)
            {                
                cachedOnBeforeAttachmentList = globalEventsReciever.GetComponents<IOnBeforeAttachment>();
                cachedOnAfterAttachmentList = globalEventsReciever.GetComponents<IOnAfterAttachment>();
                cachedOnBeforeDetachmentList = globalEventsReciever.GetComponents<IOnBeforeDetachment>();
                cachedOnAfterDetachmentList = globalEventsReciever.GetComponents<IOnAfterDetachment>();
                cachedGlobalEventsReciever = globalEventsReciever;
            }
        }
        /*
		public static void SendMessageToReciever(string methodName, object value)
		{
			if (Instance != null && Instance.globalEventsReciever != null)		
				Utils.SendMessage(Instance.globalMessagesReciever,methodName,value);
		}
        */



		void Awake ()
		{			
			Application.SetStackTraceLogType(LogType.Error,StackTraceLogType.Full);
			Application.SetStackTraceLogType(LogType.Exception,StackTraceLogType.Full);
			Application.SetStackTraceLogType(LogType.Assert,StackTraceLogType.Full);

			quitFlagP = false;
			UpdateSettings();
			DeclareSingleton();
			RecreateHierarchy();
			if (!Application.isPlaying)
			{
				TerminusObject[] objects = GameObject.FindObjectsOfType<TerminusObject>();
				for (int i = 0; i < objects.Length; i++)
					RegisterObjectInternal(objects[i]);

				Connector[] connectors = GameObject.FindObjectsOfType<Connector>();
				for (int i = 0; i < connectors.Length; i++)
					RegisterConnectorInternal(connectors[i]);
			}
	    }
	    
		void LateUpdate ()
		{
			//Debug.Log("Late update");
			if (Application.isPlaying && addedNonIdlePorts)
				UpdateCheckDataInternal();
			if (Application.isPlaying && updateEvent == UpdateEvent.LateUpdate)
				CheckPossibleConnections();
		}

		void Update()
		{
			//Debug.Log("Update");
			if (Application.isPlaying && updateEvent == UpdateEvent.Update)
			{
				CheckPossibleConnections();
			}
			CleanupContainers();
		}

		void FixedUpdate ()
		{
			//Debug.Log("Fixed update");
			if (Application.isPlaying && updateEvent == UpdateEvent.FixedUpdate)		
				Update();
		}

		void OnApplicationQuit ()
		{
			quitFlagP = true;
			ExitGlobalPlaymode();
		}

	}
}