using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Terminus 
{
	/// <summary>
	/// Component for managing placement of <see cref="TerminusObject"/>s. Needs <see cref="RaycastHandler"/> or other positioning system.
	/// </summary>
	/// <remarks>
	/// Works in conjunction with <see cref="RaycastHandler"/> to create <see href="http://www.besiege.spiderlinggames.co.uk/">Besiege</see> or <see href="https://kerbalspaceprogram.com">Kerbal Space Program</see>-like construction process.
	/// </remarks>
	public class Placer : MonoBehaviour {

		/// <summary>
		/// Sends "TerminusPlacerObjectCleared" and "TerminusPlacerObjectUpdated" messages to these GameObjects.
		/// </summary>
		/// <seealso cref="Placer.SendMessageToRecievers"/>
		public GameObject[] sendMessageTo;
		/// <summary>
		/// Should <see cref="TerminusObject.AttachSidewaysOnAllPorts"/> be called when attaching <see cref="Placer.activeObject"/>.
		/// </summary>
		public bool sidewaysConnections;
		/// <summary>
		/// The sideways connections distance limit. See <see cref="Placer.sidewaysConnections"/>.
		/// </summary>
		public float sidewaysConnectionDistanceLimit = 0.05f;
		/// <summary>
		/// The sideways connections angle difference (in degrees) limit. See <see cref="Placer.sidewaysConnections"/>.
		/// </summary>
		public float sidewaysConnectionAngleLimit = 180.5f;
		/// <summary>
		/// Current desired symmetry count. Creates this number of clones of <see cref="Placer.activeObject"/> and tries to attach them symmetrically.
		/// </summary>
		public int symmetryAttachmentsCount;

		/// <summary>
		/// When ExecutePlacingUpdate is called, object being placed will be rotated around provided normal (in degrees).
		/// </summary>
		public float additionalYRotation;


		/// <summary>
		/// What to do when <see cref="Placer.activeObject"/> is empty and <see cref="Placer.ExecuteEmptyBehaviour"/> is called.
		/// </summary>
		public EmptyBehaviours whenEmptyBehaviour;
		/// <summary>
		/// Exclude these <see cref="TerminusObject"/>s from <see cref="Placer.EmptyBehaviours.select"/>.
		/// </summary>
		/// <seealso cref="Placer.whenEmptyBehaviour"/>
		/// <seealso cref="Placer.ExecuteEmptyBehaviour"/>
		public List<TerminusObject> excludeFromSelection;
		/// <summary>
		/// Exclude these <see cref="TerminusObject"/>s from <see cref="Placer.EmptyBehaviours.pickup"/>.
		/// </summary>
		/// <seealso cref="Placer.whenEmptyBehaviour"/>
		/// <seealso cref="Placer.ExecuteEmptyBehaviour"/>
		public List<TerminusObject> excludeFromPickup;
		/// <summary>
		/// Exclude these <see cref="TerminusObject"/>s from <see cref="Placer.EmptyBehaviours.destroy"/>.
		/// </summary>
		/// <seealso cref="Placer.whenEmptyBehaviour"/>
		/// <seealso cref="Placer.ExecuteEmptyBehaviour"/>
		public List<TerminusObject> excludeFromDestruction;
		/// <summary>
		/// Affect symmetry siblings with <see cref="Placer.ExecuteEmptyBehaviour"/>.
		/// </summary>
		public bool affectSymmetrySiblings;
		[System.NonSerialized]
		public List<TerminusObject> symmetryObjects = new List<TerminusObject>();
		protected List<PositionInfo> symmetryPositionsInfo = new List<PositionInfo>();
			
		protected TerminusObject activeObjectP;
		protected TerminusObject oldActiveObjectP;

		/// <summary>
		/// Object that <see cref="Placer"/> currently tries attaching.
		/// </summary>
		public TerminusObject activeObject
		{
			get
			{
				return activeObjectP;
			}
			set
			{
				if (activeObjectP != value)
				{
					//CleanUp();
					oldActiveObjectP = activeObjectP;
					activeObjectP = value;
					if (activeObjectP != null)
					{
						activeObjectP.OnAttachmentStart();
						TerminusObject[] childObjects = activeObjectP.treeListDown.Select(rec => rec.otherConnector.owner).ToArray();
						for (int i = 0; i < childObjects.Length; i++)					
							childObjects[i].OnAttachmentStart();
					}
					SendMessageToRecievers(activeObject);
				}
			}
		}

		/// <summary>
		/// Previous <see cref="Placer.activeObject"/>
		/// </summary>
		public TerminusObject oldActiveObject
		{
			get
			{
				return oldActiveObjectP;
			}
		}

		/// <summary>
		/// Possible actions to perform when <see cref="Placer.ExecuteEmptyBehaviour"/> is called.
		/// </summary>
		/// <seealso cref="Placer.whenEmptyBehaviour"/>
		/// <seealso cref="Placer.ExecuteEmptyBehaviour"/>
		public enum EmptyBehaviours
		{
			/// <summary>
			/// Does nothing.
			/// </summary>
			do_nothing = 0,
			/// <summary>
			/// Calls <see cref="Placer.SendMessageToRecievers"/>, and nothing else.
			/// </summary>
			select = 1,
			/// <summary>
			/// Calls <see cref="TerminusObject.DetachFromParent"/> and sets it as <see cref="Placer.activeObject"/>.
			/// </summary>
			pickup = 2,
			/// <summary>
			/// Destroys provided <see cref="TerminusObject"/> 
			/// </summary>
			destroy = 3
		}


		/// <summary>
		/// Destroys leftover symmetry clones and activeObject(if not null).
		/// </summary>
		public void CleanUp()
		{
			if (activeObject != null)
			{
				if (activeObject.container != null)
					Manager.FreeContainer(activeObject.container);
				Destroy(activeObject.gameObject);
			}
			for (int i = 0; i < symmetryObjects.Count; i++)
			{
				if (symmetryObjects[i] != null)
				{
					if (symmetryObjects[i].container != null)
						Manager.FreeContainer(symmetryObjects[i].container);
					Destroy(symmetryObjects[i].gameObject);
				}
			}
			symmetryObjects.Clear();
		}


		// Use this for initialization
		protected void Start () 
		{
			if (activeObject != null)
				activeObject.OnAttachmentStart();
		}


		protected void RecreateSymmetryObjects(int destroyFromIndex = 0)
		{
			for (int i = destroyFromIndex; i < symmetryObjects.Count; i++)
			{
				if (symmetryObjects[i].container != null)
					Manager.FreeContainer(symmetryObjects[i].container);
				Destroy(symmetryObjects[i].gameObject);		
			}
			symmetryObjects.Clear();
			if (activeObject != null)
			{
				for (int i = 0; i < symmetryAttachmentsCount; i++)
				{
					if (activeObject.container != null)
					{
						GameObject obj = (GameObject)Instantiate(activeObject.container.gameObject);
						symmetryObjects.Add(obj.GetComponent<Container>().terminusObject);
					}
					else
					{
						GameObject obj = (GameObject)Instantiate(activeObject.gameObject);
						symmetryObjects.Add(obj.GetComponent<TerminusObject>());
					}
				}
			}
		}

		protected void DesignateSymmetricSiblings(int symmObjCount)
		{
			List<TerminusObject>[] trees = new List<TerminusObject>[symmObjCount+1];

			trees[0] = activeObject.treeListDown.Select(rec => rec.otherConnector.owner).ToList();
			trees[0].Add(activeObject);

			for (int i = 1; i < trees.Length; i++)
			{
				trees[i] = symmetryObjects[i-1].treeListDown.Select(rec => rec.otherConnector.owner).ToList();
				trees[i].Add(symmetryObjects[i-1]);

				for (int x = 0; x < i; x++)
				{
					if (trees[x].Count != trees[i].Count)
					{
						Debug.LogWarning(": Attempting to symmetrically bind object trees of different sizes.");
						return;
					}
				}
			}

			for (int i = 0; i < trees[0].Count; i++)
			{
				for (int x = 0; x < trees.Length; x++)
				{
					trees[x][i].symmetricSiblings = new List<TerminusObject>();
					for (int y = 1; y < trees.Length; y++)
					{
						trees[x][i].symmetricSiblings.Add(trees[(x+y) % trees.Length][i]);
					}
				}
			}

		}

		/// <summary>
		/// Calls <see cref="TerminusObject.SetNextAttachmentPort"/> on <see cref="Placer.activeObject"/>.
		/// </summary>
		public void CyclePortOnActiveObject(int step = 1)
		{
			if (activeObject != null)
			{
				activeObject.SetNextAttachmentPort(step);
				for (int i = 0; i < symmetryObjects.Count; i++)
					symmetryObjects[i].SetNextAttachmentPort(step);
				SendMessageToRecievers(activeObject);
			}
		}

		/// <summary>
		/// Performs rotation of active object depending on <see cref="Settings.LayerPortOptions.rotationType"/> of <see cref="TerminusObject.currentAttachmentPort"/>.
		/// </summary>
		public void RotateActiveObject(float amount)
		{
			if (activeObject != null)
			{
				switch (activeObject.currentAttachmentPort.portOptions.rotationType)
				{
				case Settings.RotationTypes.self:
					int shift = (amount > 0) ? 1 : ((amount == 0) ? 0 : -1);
					activeObject.currentAttachmentPort.Rotate(shift);
					for (int i = 0; i < symmetryObjects.Count; i++)
						symmetryObjects[i].currentAttachmentPort.Rotate(shift);
					SendMessageToRecievers(activeObject);
					break;
				case Settings.RotationTypes.owner:
					additionalYRotation += amount;
					if (additionalYRotation > 360)
						additionalYRotation -= 360;
					else if (additionalYRotation < 0)
						additionalYRotation += 360;
					break;
				}
			}
		}


		protected List<TerminusObject>[] symChildObjects = new List<TerminusObject>[0];
		/// <summary>
		/// Tries to attach or position <see cref="Placer.activeObject"/> according to provided parameters.
		/// </summary>
		/// <param name="attachingUpdate">True to try to attach <see cref="Placer.activeObject"/>, false to just reposition it</param>
		/// <param name="position">Supposed position of <see cref="Placer.activeObject"/></param>
		/// <param name="normal">Supposed normal (orientation) of <see cref="Placer.activeObject"/></param>
		/// <param name="clickedObject">Clicked object provided by <see cref="RaycastHandler"/> </param>
		/// <param name="collider">Collider provided by <see cref="RaycastHandler"/></param>
		public void ExecutePlacingUpdate(bool attachingUpdate, Vector3 position, Vector3 normal, GameObject clickedObject = null, Component collider = null)
		{
			Port attachmentPort = activeObject.currentAttachmentPort;			
			if (attachmentPort != null)
			{
				Vector3 upVect = Vector3.up;
				if (clickedObject != null)
				{
					upVect = clickedObject.transform.up;
					if (normal == upVect)
						upVect = clickedObject.transform.forward;
				}

				attachmentPort.Align(position, Quaternion.LookRotation(normal,upVect));
				activeObject.transform.RotateAround(position,normal,additionalYRotation);

				if (activeObject.multipleAttachmentPortsAllowed)
				{
					List<Port> ports = activeObject.currentAttachmentPorts;
					float distance = float.MaxValue - 1;
					float priority = float.MinValue;
					for (int i = 0; i < ports.Count; i++)
					{
						if (ports[i].CheckPossibleConnections(collider,false))
						{							
							Settings.LayerPortOptions portOptions = ports[i].portOptions;
							if (portOptions.attachmentPriority > priority								
								|| (portOptions.attachmentPriority == priority && ports[i].attachmentCandidateDistance < distance))
							{
								priority = portOptions.attachmentPriority;
								distance = ports[i].attachmentCandidateDistance;
								attachmentPort = ports[i];
							}
						}
					}
				}
					

				if (attachmentPort.CheckPossibleConnections(collider))
				{
					//Symmetry handling
					if (symmetryObjects.Count != symmetryAttachmentsCount)
						RecreateSymmetryObjects();
					
					int currentSymmetryCount = 0;
					if (symmetryAttachmentsCount != 0)
						currentSymmetryCount = attachmentPort.attachmentCandidate.AchievableSymmetryCount(symmetryAttachmentsCount);
					
					if (currentSymmetryCount > 0)
					{
						symmetryPositionsInfo = attachmentPort.attachmentCandidate.GetSymmetryPositions(currentSymmetryCount,
						                                                                                attachmentPort.globalPosition,
						                                                                                attachmentPort.globalRotation 
						                                                                                * Quaternion.Inverse(Quaternion.Euler(new Vector3(0,180,0))),
																										collider);
					}
					
					for (int i = 0; i < symmetryObjects.Count; i++)
					{
						if (i < currentSymmetryCount)
						{
							symmetryObjects[i].containerOrSelf.SetActive(true);
							//Debug.Log(symmetryPositionsInfo.Length.ToString() + " | " + i.ToString());
							symmetryObjects[i].currentAttachmentPort.Align(symmetryPositionsInfo[i].position,symmetryPositionsInfo[i].rotation);
							symmetryObjects[i].currentAttachmentPort.CheckPossibleConnections(symmetryPositionsInfo[i].collider);
							
						}
						else
						{
							symmetryObjects[i].containerOrSelf.SetActive(false);
						}
					}
					
					List<TerminusObject> childObjects = activeObjectP.treeListDownObjects;
					if (symChildObjects.Length != currentSymmetryCount)
						symChildObjects = new List<TerminusObject>[currentSymmetryCount];
					for (int i = 0; i < currentSymmetryCount; i++)
						symChildObjects[i] = symmetryObjects[i].treeListDownObjects;
					
					bool validPlacement = activeObject.ValidPlacementUpdate();
					
					for (int i = 0; i < childObjects.Count; i++)							
						validPlacement &= childObjects[i].ValidPlacementUpdate();
					
					for (int i = 0; i < currentSymmetryCount; i++)
					{
						validPlacement &= symmetryObjects[i].ValidPlacementUpdate();
						for (int x = 0; x < symChildObjects[i].Count; x++)							
							validPlacement &= symChildObjects[i][x].ValidPlacementUpdate();
					}
					
					
					
					if (validPlacement && attachingUpdate)
					{
						if (activeObject.longConnection && activeObject.SetNextAttachmentPort() && activeObject.currentAttachmentPort != attachmentPort)
						{
							//If we need to attach next connector for full attachment
							attachmentPort.AttachTo(attachmentPort.attachmentCandidate);
							for (int i = 0; i < currentSymmetryCount; i++)
								symmetryObjects[i].currentAttachmentPort.AttachTo(symmetryPositionsInfo[i].connector);
							for (int i = 0; i < symmetryObjects.Count; i++)							
								symmetryObjects[i].SetNextAttachmentPort();
							SendMessageToRecievers(activeObject);
						}
						else
						{
							activeObject.OnAttached();

							for (int i = 0; i < childObjects.Count; i++)							
								childObjects[i].OnAttached();

							if (activeObject.longConnection)
								attachmentPort.AttachToSideways(attachmentPort.attachmentCandidate);
							else
								attachmentPort.AttachTo();
							/*
							activeObject.OnAttached();
							
							for (int i = 0; i < childObjects.Length; i++)							
								childObjects[i].OnAttached();							
							*/

							for (int i = 0; i < currentSymmetryCount; i++)
							{
								symmetryObjects[i].OnAttached();
								for (int x = 0 ; x < symChildObjects[i].Count; x++)
									symChildObjects[i][x].OnAttached();
							}

							if (activeObject.longConnection)
							{
								for (int i = 0; i < currentSymmetryCount; i++)						
									symmetryObjects[i].currentAttachmentPort.AttachToSideways(symmetryPositionsInfo[i].connector);
							}
							else
							{
								for (int i = 0; i < currentSymmetryCount; i++)						
									symmetryObjects[i].currentAttachmentPort.AttachTo(symmetryPositionsInfo[i].connector);
							}
							
							/*
							for (int i = 0; i < currentSymmetryCount; i++)
							{
								symmetryObjects[i].OnAttached();
								for (int x = 0 ; x < symChildObjects[i].Length; x++)
									symChildObjects[i][x].OnAttached();
							}
							*/
							
							if (sidewaysConnections)
							{								
								activeObject.AttachSidewaysOnAllPorts(sidewaysConnectionDistanceLimit,sidewaysConnectionAngleLimit);
								for (int i = 0; i < childObjects.Count; i++)
									childObjects[i].AttachSidewaysOnAllPorts(sidewaysConnectionDistanceLimit,sidewaysConnectionAngleLimit);
								for (int i = 0; i < currentSymmetryCount; i++)
								{
									symmetryObjects[i].AttachSidewaysOnAllPorts(sidewaysConnectionDistanceLimit,sidewaysConnectionAngleLimit);
									for (int x = 0 ; x < symChildObjects[i].Count; x++)
										symChildObjects[i][x].AttachSidewaysOnAllPorts(sidewaysConnectionDistanceLimit,sidewaysConnectionAngleLimit);
								}
							}
							
							DesignateSymmetricSiblings(currentSymmetryCount);
							
							activeObject.mode = TerminusObject.Modes.accepting_attachments;
							
							for (int i = 0; i < childObjects.Count; i++)							
								childObjects[i].mode = TerminusObject.Modes.accepting_attachments;
							
							for (int i = 0; i < currentSymmetryCount; i++)
							{
								symmetryObjects[i].mode = TerminusObject.Modes.accepting_attachments;
								for (int x = 0; x < symChildObjects[i].Count; x++)							
									symChildObjects[i][x].mode = TerminusObject.Modes.accepting_attachments;
							}
							
							activeObject = null;
							RecreateSymmetryObjects(currentSymmetryCount);
						}
					}
				}
				else
				{
					activeObject.InvalidPlacementUpdate();
					for (int i = 0; i < symmetryObjects.Count; i++)
						symmetryObjects[i].containerOrSelf.SetActive(false);	
				}
			}
		}

		/// <summary>
		/// Sends "TerminusPlacerObjectUpdated", "TerminusPlacerObjectUpdatedNull" or "TerminusPlacerObjectDeleted" messages to GameObjects contained in <see cref="Placer.sendMessageTo"/> 
		/// </summary>
		/// <param name="selectedObject"><see cref="Placer.activeObject"/> if called from <see cref="Placer.ExecutePlacingUpdate"/>. Any <see cref="TerminusObject"/> if called otherwise.</param>
		/// <param name="deleted">true if selectedObject was deleted, false otherwise</param>
		public void SendMessageToRecievers(TerminusObject selectedObject, bool deleted = false)
		{
			if (deleted)
			{
				for (int i = 0; i < sendMessageTo.Length; i++)
				{
					sendMessageTo[i].SendMessage("TerminusPlacerObjectDeleted",selectedObject);
				}
				return;
			}
			if (selectedObject == null)
			{
				for (int i = 0; i < sendMessageTo.Length; i++)
				{
					sendMessageTo[i].SendMessage("TerminusPlacerObjectUpdatedNull");
				}
				return;
			}
			else
			{
				for (int i = 0; i < sendMessageTo.Length; i++)
				{
					sendMessageTo[i].SendMessage("TerminusPlacerObjectUpdated",selectedObject);
				}
				return;
			}
		}

		/// <summary>
		/// Calls <see cref="TerminusObject.DetachFromParent"/> and sets it as <see cref="Placer.activeObject"/>.
		/// </summary>
		/// <param name="obj">Object to pickup</param>
		public void Pickup(TerminusObject obj)
		{
			if (obj != null && !obj.longConnection)
			{
				if (obj.parent.attachmentType != AttachmentInfo.Types.none)
				{
					obj.DetachFromParent();
				}
				activeObject = obj;
				activeObject.mode = TerminusObject.Modes.being_attached;
				TerminusObject[] childObjects = activeObjectP.treeListDown.Select(rec => rec.otherConnector.owner).ToArray();
				for (int i = 0; i < childObjects.Length; i++)
					childObjects[i].mode = TerminusObject.Modes.being_attached;
				if (affectSymmetrySiblings)
				{
					for (int i = 0; i < activeObject.symmetricSiblings.Count; i++)
					{
						activeObject.symmetricSiblings[i].DetachFromParent();
						activeObject.symmetricSiblings[i].mode = TerminusObject.Modes.being_attached;
						activeObject.symmetricSiblings[i].OnAttachmentStart();
						TerminusObject[] symChildObjects = activeObject.symmetricSiblings[i].treeListDown.Select(rec => rec.otherConnector.owner).ToArray();
						for (int x = 0; x < symChildObjects.Length; x++)
						{
							symChildObjects[x].mode = TerminusObject.Modes.being_attached;
							symChildObjects[x].OnAttachmentStart();
						}
					}
					symmetryObjects = activeObject.symmetricSiblings.ToList();
				}
				else
				{
					if (obj.parent.attachmentType != AttachmentInfo.Types.none)
					{
						obj.parent.otherConnector.owner.RemoveSymmetrySiblings();
					}
				}
				SendMessageToRecievers(activeObject);
			}
		}


		/// <summary>
		/// Executes actions associated with <seealso cref="Placer.whenEmptyBehaviour"/>
		/// </summary>
		/// <param name="position">Position provided by <see cref="RaycastHandler"/> or other similar process.</param>
		/// <param name="normal">Normal (orientation) oprovided by <see cref="RaycastHandler"/> or other similar process</param>
		/// <param name="clickedObject">Clicked object provided by <see cref="RaycastHandler"/> or other similar process</param>
		/// <param name="collider">Collider provided by <see cref="RaycastHandler"/> or other similar process</param>
		public void ExecuteEmptyBehaviour(Vector3 position, Vector3 normal, GameObject clickedObject = null, Component collider = null)
		{
			TerminusObject obj = clickedObject.GetComponent<TerminusObject>();
			Transform parent = clickedObject.transform.parent;

			while (obj == null && parent != null)
			{
				obj = parent.GetComponent<TerminusObject>();
				parent = parent.parent;
			}

			if (obj != null)
			{
				switch (whenEmptyBehaviour)
				{
				case EmptyBehaviours.pickup:
					if (obj != null && !excludeFromPickup.Contains(obj))
					{
						Pickup(obj);
					}
					break;
				case EmptyBehaviours.select:
					if (!excludeFromSelection.Contains(obj))
						SendMessageToRecievers(obj);
					break;
				case EmptyBehaviours.destroy:
					if (!excludeFromDestruction.Contains(obj))
					{
						SendMessageToRecievers(obj,true);
						TerminusObject[] childObjects = obj.treeListDown.Select(rec => rec.otherConnector.owner).ToArray();
						for (int i = 0; i < childObjects.Length; i++)
							Destroy(childObjects[i].gameObject);
						if (affectSymmetrySiblings)
						{
							for (int i = 0; i < obj.symmetricSiblings.Count; i++)
							{
								TerminusObject[] symChildObjects = obj.symmetricSiblings[i].treeListDown.Select(rec => rec.otherConnector.owner).ToArray();
								for (int x = 0; x < symChildObjects.Length; x++)						
									Destroy(symChildObjects[x].gameObject);
								Destroy(obj.symmetricSiblings[i].gameObject);
							}
						}
						Destroy (obj.gameObject);
					}
					break;
				}
			}
		}
	}
}