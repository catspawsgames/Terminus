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
	/// Port is type of <see cref="Connector"/> that can be defined just by it's position and rotation, unlike <see cref="Surface"/> which is defined by colliders.
	/// Port can recieve and initiate attachments, but it only supports one attachment at the same time.
	/// <seealso cref="Connector"/>
	/// <seealso cref="Surface"/>
	/// </summary>
	[ExecuteInEditMode]
	[AddComponentMenu("Terminus modular system/Port")]
	[HelpURL("http://scriptreference.terminus-unity.com/html/T_Terminus_Port.htm")]
	public class Port : Connector {

		protected bool appQuit = false;

		/// <summary>
		/// If true, changing port position will not move <see cref="Connector.owner"/>. Main use is when owner is <see cref="LongConnection"/>-type object (for example, strut).
		/// </summary>
		public bool doNotMoveOwner;


		/// <summary>
		/// Draw controls for port <see cref="Port.rotation"/> and <see cref="Port.offset"/>. No effect outside editor.
		/// </summary>
		public bool drawControls = false;

		[SerializeField]
		protected bool isCurrentAttachmentPortP;
		[SerializeField]
		protected Vector3 offsetP;
		[SerializeField]
		protected Quaternion rotationP = Quaternion.identity;
		[SerializeField]
		protected float rotationShiftP;
		[SerializeField]
		protected AttachmentInfo attachmentInfoP;

		protected HashSet<Port> influencingPortsP;

		private static Quaternion flipZ = Quaternion.Euler(0,0,180);
		private static Quaternion flipY = Quaternion.Euler(0,180,0);

		/// <summary>
		/// List of influences from other ports currently affecting this port. Read-only unless you know exactly what you're doing.
		/// </summary>
		[System.NonSerialized]
		public List<ZoneInteraction> influences = new List<ZoneInteraction>();

		/// <summary>
		/// Information about current attachment.
		/// </summary>
		/// <example>
		/// Example of accessing attachment of port.
		/// <code>
		/// {
		/// 	public Terminus.Port port;	
		/// 
		/// 	void MakeSurfaceJointBreakable()
		/// 	{
		/// 		//Checks if port is attached to something
		/// 		if (port.attachmentInfo.attachmentType != AttachmentInfo.Types.none)
		/// 		{
		/// 			//Checks if other connector is surface
		/// 			if (port.attachmentInfo.otherConnector is typeof(Terminus.Surface))
		/// 			{
		/// 				//Access attachment joint
		/// 				((Joint)port.attachmentInfo.joint).breakForce = 1000;
		/// 			}
		/// 		}
		/// 	}
		/// }
		/// </code>
		/// </example>
		public AttachmentInfo attachmentInfo
		{
			get
			{
				return attachmentInfoP;
			}
		}

		/// <summary>
		/// Connectors that this port can possibly be attached to. Filled by <see cref="Manager"/>.
		/// </summary>
		public List<Connector> connectorsToCheck = new List<Connector>();

		[SerializeField]
		protected Connector attachmentCandidateP;
		/// <summary>
		/// Most recently detected connector that accepts attachment from this port with regards to <see cref="Settings.LayerPortOptions"/> (layer compatiblity, proximity and rotation difference).
		/// Read-only, filled by <see cref="Port.CheckPossibleConnections"/>  or by <see cref="Manager.updateEvent"/> 
		/// </summary>
		public Connector attachmentCandidate
		{
			get
			{
				return attachmentCandidateP;
			}
		}

		[SerializeField]
		protected float attachmentCandidateDistanceP;
		/// <summary>
		/// Distance to <see cref="Port.attachmentCandidate"/> when <see cref="Port.CheckPossibleConnections"/> was called. Value of -1 means that attachment candidate is surface.
		/// </summary>
		public float attachmentCandidateDistance
		{
			get
			{
				return attachmentCandidateDistanceP;
			}
		}

		[SerializeField]
		protected float attachmentCandidateAngleDiffP;
		/// <summary>
		/// Angle difference between <see cref="Port.attachmentCandidate"/> and this port when <see cref="Port.CheckPossibleConnections"/> was called. Value of -1 means that attachment candidate is surface.
		/// </summary>
		public float attachmentCandidateAngleDiff
		{
			get
			{
				return attachmentCandidateAngleDiffP;
			}
		}

		[SerializeField]
		protected Vector3 attachmentCandidatePositionP;
		/// <summary>
		/// Position of <see cref="Port.attachmentCandidate"/> when <see cref="Port.CheckPossibleConnections"/> was called.
		/// </summary>
		public Vector3 attachmentCandidatePosition
		{
			get
			{
				return attachmentCandidatePositionP;
			}
		}

		[SerializeField]
		protected Quaternion attachmentCandidateRotationP;
		/// <summary>
		/// Rotation of <see cref="Port.attachmentCandidate"/> when <see cref="Port.CheckPossibleConnections"/> was called.
		/// </summary>
		public Quaternion attachmentCandidateRotation
		{
			get
			{
				return attachmentCandidateRotationP;
			}
		}

		
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Terminus.Port"/> is current attachment port of owner <see cref="TerminusObject"/>. TerminusObject can only have one attachment port at the same time.
		/// </summary>
		public bool isCurrentAttachmentPort
		{
			get
			{
				return isCurrentAttachmentPortP;
			}
			set
			{
				if (portOptions.attachementPort && isAttachable)
				{
					isCurrentAttachmentPortP = value;
					if (value && !owner.multipleAttachmentPortsAllowed)
					{
						for (int i = 0; i < ownerP.connectors.Count;i++)
						{
							if (ownerP.connectors[i] != null && ownerP.connectors[i].GetType() == typeof(Port) && ((Port)(ownerP.connectors[i])) != this)
								((Port)(ownerP.connectors[i])).isCurrentAttachmentPort = false;
						}
					}
					if ((Application.isPlaying && !stopReinitialization) || (!Application.isPlaying && gameObject.activeInHierarchy))
						Initialize();
				}
			}
		}

		/// <summary>
		/// Local position of this port.
		/// </summary>
	    public Vector3 offset
		{
			get
			{
				return offsetP;
			}
			set
			{
				offsetP = value;
				if ((Application.isPlaying && !stopReinitialization) || (!Application.isPlaying && gameObject.activeInHierarchy))
					Initialize();
	        }
	    }

		/// <summary>
		/// Rotation difference from default <see cref="Port.rotation"/>. Final local port rotation calculated as rotationShift * <see cref="Port.rotation"/> 
		/// </summary>
		public float rotationShift
		{
			get
			{
				return rotationShiftP;
			}
		}

		/// <summary>
		/// Local rotation of the port. If you want to rotate port during runtime, do not change this value.
		/// Use <see cref="Port.Rotate"/> , <see cref="Port.RotateByAngle"/> and <see cref="Port.rotationShift"/> 
		/// </summary>
	    public Quaternion rotation
		{
			get
			{
				if (portOptions.rotationType != Settings.RotationTypes.self)
					return rotationP;
				else
					return rotationP * Quaternion.Euler(0,0,rotationShiftP);
			}
			set
			{
				rotationP = value;
				if ((Application.isPlaying && !stopReinitialization) || (!Application.isPlaying && gameObject.activeInHierarchy))
					Initialize();
	        }
	    }
	    
		/// <summary>
		/// Returns true if port can be attached to other <see cref="Connector"/> . Other connector should be <see cref="Connector.isAccepting"/>.
		/// </summary>
		public override bool isAttachable
		{
			get
			{
				return active && !isOccupied && (owner.mode == TerminusObject.Modes.free_connections
				                  				|| (owner.mode == TerminusObject.Modes.being_attached));
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="Terminus.Port"/> is already attached to something.
		/// </summary>
		/// <value><c>true</c> if is occupied; otherwise, <c>false</c>.</value>
		public override bool isOccupied
		{
			get
			{
				return attachmentInfoP.attachmentType != AttachmentInfo.Types.none;
			}
		}

		/// <summary>
		/// Returns <see cref="Port.rotation"/> translated to global space.
		/// </summary>
	    public override Quaternion globalRotation
		{
			get
			{
				return transform.rotation * rotation;
			}
		}

		/// <summary>
		/// Returns <see cref="Port.offset"/> translated to global space.
		/// </summary>
		public override Vector3 globalPosition
		{
			get
			{
				return transform.TransformPoint(offset);
			}
		}

		/// <summary>
		/// Returns empty list when port isn't attached to anything, and list with one element containing <see cref="Port.attachmentInfo"/> otherwise.
		/// </summary>
		public override List<AttachmentInfo> attachmentsInfo
		{
			get
			{
				if (attachmentInfoP.attachmentType == AttachmentInfo.Types.none)
					return new List<AttachmentInfo>();
				else
					return new List<AttachmentInfo>(){attachmentInfoP};
			}
		}

		    
		protected struct DistanceCheck
		{
			public int index;
			public float distance;
			public float angleDiff;
			
			public DistanceCheck(int ind, float dist, float angle)
			{
				index = ind;
				distance = dist;
				angleDiff = angle;
			}
		}


		/// <summary>
		/// Rotates <see cref="Port.rotationShift"/> by <see cref="Settings.LayerPortOptions.rotationStep"/> if <see cref="Settings.LayerPortOptions.canRotate"/> set to true.
		/// </summary>
		/// <param name="steps">rotationStep will be multiplied by that number. Can be negative for rotation in opposite direction.</param>
		public void Rotate(int steps)
		{
			if (portOptions.rotationType == Settings.RotationTypes.self && portOptions.rotationStep > 0)
			{
				if (portOptions.rotationLimits != Vector2.zero)
					rotationShiftP = Mathf.Clamp(rotationShiftP + portOptions.rotationStep * steps,portOptions.rotationLimits.x,portOptions.rotationLimits.y);
				else
				{
					rotationShiftP += portOptions.rotationStep * steps;
					if (rotationShiftP >= 360)
						rotationShiftP -= 360;
					else if (rotationShiftP < 0)
					{
						rotationShiftP += 360;
					}
				}
			}
		}

		/// <summary>
		/// Rotates <see cref="Port.rotationShift"/> by specific angle if <see cref="Settings.LayerPortOptions.canRotate"/> set to true.
		/// </summary>
		/// <param name="angle">Angle (in degrees).</param>
		public void RotateByAngle(float angle)
		{
			if (portOptions.rotationType == Settings.RotationTypes.self)
			{
				if (portOptions.rotationLimits != Vector2.zero)
					rotationShiftP = Mathf.Clamp(rotationShiftP + angle,portOptions.rotationLimits.x,portOptions.rotationLimits.y);
				else
				{
					rotationShiftP += angle;
					if (rotationShiftP >= 360)
						rotationShiftP -= 360;
					else if (rotationShiftP < 0)
					{
						rotationShiftP += 360;
					}
				}
			}
		}

		/// <summary>
		/// Moves transform of this port's (and its <see cref="Connector.owner"/>) so that its <see cref="Port.globalPosition"/> and <see cref="Port.globalRotation"/> are aligned with provided parameters, according to orientation type selected.
		/// </summary>
		/// <example>
		/// Example of aligning one port to another.
		/// <code>
		/// public Terminus.Port port1;
		/// public Terminus.Port port2;
		/// 
		/// public void AlignPorts()
		/// {
		/// 	port1.Align(port2.globalPosition,port2.globalRotation);
		/// }
		/// </code>
		/// </example>
		/// <param name="position">Desired position of port.</param>
		/// <param name="orientation">Desired orientation of port. Note that port will look in opposite direction from this.</param>
		/// <seealso cref="Settings.LayerPortOptions.onlyPlanarSnap"/>
		/// <seealso cref="Port.doNotMoveOwner"/>
		public void Align(Vector3 position, Quaternion orientation)
		{
			switch (portOptions.orientationType)
			{
			case Settings.OrientationTypes.exact:
				if (doNotMoveOwner)
				{
					transform.rotation = orientation * Quaternion.Inverse(rotation * flipY);
					transform.position += position - transform.TransformPoint(offset);
				}
				else
				{
					owner.rotation = orientation * Quaternion.Inverse(Quaternion.Inverse(owner.transform.rotation) * transform.rotation * rotation * flipY);
					owner.position += position - transform.TransformPoint(offset);
				}
				break;
			case Settings.OrientationTypes.planar:
				if (doNotMoveOwner)
				{
					transform.rotation = Quaternion.LookRotation(orientation * Vector3.forward, globalRotation * Vector3.up) * Quaternion.Inverse(rotation * flipY);
					transform.position += position - transform.TransformPoint(offset);
				}
				else
				{
					owner.rotation = Quaternion.LookRotation(orientation * Vector3.forward, globalRotation * Vector3.up) * Quaternion.Inverse(Quaternion.Inverse(owner.transform.rotation) * transform.rotation * rotation * Quaternion.Euler(new Vector3(0,180,0)));
					owner.position += position - transform.TransformPoint(offset);
				}
				break;
			case Settings.OrientationTypes.twosided:
				if (doNotMoveOwner)
				{
					Quaternion rotation1 = orientation * Quaternion.Inverse(rotation * flipY);
					Quaternion rotation2 = rotation1 * flipZ;
					if (Quaternion.Angle(rotation1, transform.rotation) < Quaternion.Angle(rotation2,transform.rotation))						
						transform.rotation = rotation1;
					else
						transform.rotation = rotation2;
					transform.position += position - transform.TransformPoint(offset);
				}
				else
				{					
					Quaternion rotation1 = orientation * Quaternion.Inverse(Quaternion.Inverse(owner.transform.rotation) * transform.rotation * rotation * flipY);
					Quaternion rotation2 = orientation * flipZ * Quaternion.Inverse(Quaternion.Inverse(owner.transform.rotation) * transform.rotation * rotation * flipY);
					if (Quaternion.Angle(rotation1, owner.transform.rotation) < Quaternion.Angle(rotation2,owner.transform.rotation))						
						owner.transform.rotation = rotation1;
					else
						owner.transform.rotation = rotation2;					
					owner.position += position - transform.TransformPoint(offset);
				}
				break;
			}				
		}

		/// <summary>
		/// Returns <see cref="Port.attachmentInfo"/> if <see cref="AttachmentInfo.otherConnector"/> is provided <see cref="Connector"/> .
		/// </summary>
		/// <param name="connector">Connector to check for.</param>
		public override AttachmentInfo GetAttachmentInfo(Connector connector)
		{
			if ((attachmentInfoP.attachmentType != AttachmentInfo.Types.none && connector == attachmentInfoP.otherConnector) || connector == this)
				return attachmentInfoP;
			else
				return new AttachmentInfo();
		}

		/// <summary>
		/// Usually called by <see cref="Manager"/> automatically. Removes influence links to ports that's no longer can affect this <see cref="Port"/>.
		/// </summary>
		public void CleanUpInfluenceInfo()
		{
			if (Application.isPlaying && portOptions.useInfluenceMessaging && owner.allInfluences != null)
			{
				if (!isAttachable)
				{
					influencingPortsP.Clear();
					influences.Clear();
					owner.allInfluences.RemoveAll(rec => rec.thisConnector == this);
				}
				else
				{
					influencingPortsP.RemoveWhere(rec => !connectorsToCheck.Contains(rec));
					influences.RemoveAll(rec => !connectorsToCheck.Contains(rec.otherConnector));
					owner.allInfluences.RemoveAll(rec => rec.thisConnector == this && !connectorsToCheck.Contains(rec.otherConnector));
				}
			}
		}

		/// <summary>
		/// Checks for possible <see cref="Connector"/>s that this connector can be attached to. Fills <see cref="Port.attachmentCandidate"/> field with closest candidate if several were found.
		/// </summary>
		/// <remarks>
		/// Note that only way to consider <see cref="Surface"/>s as attachment candidates is to provide Collider. If you're using raycasting for placing your objects, provide collider that was hit with raycast. Alternative method is to use collisions for determination of suitable collider.
		/// </remarks>
		/// <returns><c>true</c>, if at least one possible candidate was found, <c>false</c> otherwise.</returns>
		/// <param name="placementCollider">Collider to check for possible <see cref="Surface"/> attachments. Provided by <see cref="Placer"/> and <see cref="RaycastHandler"/> classes if you use them.</param>
		/// <param name="autoconnect">If set to <c>true</c>, will <see cref="Port.Connect"/>  port to closest candidate depending on <see cref="TerminusObject.attachAutomatically"/> .</param>
		public bool CheckPossibleConnections(Component placementCollider = null, bool autoconnect = true)
		{
			Settings.LayerPortOptions currentPortOptions = portOptions;
			DistanceCheck minCheck;
			minCheck.distance = currentPortOptions.snapRadius * currentPortOptions.snapRadius;
			minCheck.angleDiff = currentPortOptions.snapConeAngle;
			minCheck.index = -1;
			float distance = float.MaxValue;

			for (int x = 0; x < connectorsToCheck.Count; x++)
			{
				if (connectorsToCheck[x] != null)
				{
					distance = float.MaxValue;

					AttachmentInfo selfParent = owner.parent;
					AttachmentInfo checkParent = connectorsToCheck[x].owner.parent;
					if (connectorsToCheck[x] != null 
						&& ((selfParent.attachmentType == AttachmentInfo.Types.none || connectorsToCheck[x].owner != selfParent.otherConnector.owner) && (checkParent.attachmentType == AttachmentInfo.Types.none || owner != checkParent.otherConnector.owner)))
					{					
						if (connectorsToCheck[x].GetType() == typeof(Port))
						{
							if (!currentPortOptions.use2DPhysics)
								distance = Vector3.SqrMagnitude(transform.TransformPoint(offsetP)
								                                - connectorsToCheck[x].transform.TransformPoint(((Port)connectorsToCheck[x]).offset));
							else
								distance = Vector2.SqrMagnitude(Utils.XY(transform.TransformPoint(offsetP))
								                                - Utils.XY(connectorsToCheck[x].transform.TransformPoint(((Port)connectorsToCheck[x]).offset)));
						}
						else
						{
							//TODO SURFACE DISTANCE DETERMINATION USING Collider.ClosestPointOnBounds and optional raycasting
							//TODO don't forget to raycast not from center of port but farther
							//TODO create ClosestPointOnCollider
						}
					

						float angleDiff = 0;
						if (currentPortOptions.snapConeAngle != 0)
						{
							switch (currentPortOptions.orientationType)
							{
							case Settings.OrientationTypes.exact:
								angleDiff = Quaternion.Angle(globalRotation * flipY,connectorsToCheck[x].globalRotation);
								break;
							case Settings.OrientationTypes.planar:
								angleDiff = Vector3.Angle(globalRotation * Vector3.forward, connectorsToCheck[x].globalRotation * (-Vector3.forward));
								break;
							case Settings.OrientationTypes.twosided:
								angleDiff = Mathf.Min(Quaternion.Angle(globalRotation * flipY,connectorsToCheck[x].globalRotation),
									                  Quaternion.Angle(globalRotation * flipY * flipZ,connectorsToCheck[x].globalRotation));
								break;
							}
							//angleDiff = (currentPortOptions.onlyPlanarSnap) ? Vector3.Angle(globalRotation * Vector3.forward, connectorsToCheck[x].globalRotation * (-Vector3.forward)) : Quaternion.Angle(globalRotation * Quaternion.Euler(new Vector3(0,180,0)),connectorsToCheck[x].globalRotation);				
						}

						if (distance <= minCheck.distance
						    && (currentPortOptions.snapConeAngle == 0
						    	|| angleDiff < currentPortOptions.snapConeAngle))
						{
							minCheck.index = x;
							minCheck.distance = distance;
							minCheck.angleDiff = angleDiff;
						}

					}



					if (Application.isPlaying && currentPortOptions.useInfluenceMessaging && connectorsToCheck[x] != null && connectorsToCheck[x].GetType() == typeof(Port))
					{
						if  (distance <= currentPortOptions.influenceRadius * currentPortOptions.influenceRadius
					    	 && (currentPortOptions.influenceConeAngle == 0
						   		 || Quaternion.Angle(globalRotation * Quaternion.Euler(new Vector3(0,180,0)),((Port)connectorsToCheck[x]).globalRotation) < currentPortOptions.influenceConeAngle))
						{

							if (!influencingPortsP.Contains(((Port)connectorsToCheck[x])))					
							{
								ZoneInteraction zoneIntInfo = new ZoneInteraction();
								zoneIntInfo.thisConnector = this;
								zoneIntInfo.otherConnector = ((Port)connectorsToCheck[x]);
								zoneIntInfo.sqrDistance = distance;
								influencingPortsP.Add(((Port)connectorsToCheck[x]));
								influences.Add(zoneIntInfo);
								owner.allInfluences.Add(zoneIntInfo);
								Utils.SendMessage(owner.gameObject,currentPortOptions.influenceMessageEnter,zoneIntInfo);
							}
							else
							{
								for (int i = 0; i < influences.Count; i++)
								{
									if (influences[i].otherConnector == connectorsToCheck[x])
									{
										influences[i].sqrDistance = distance;
										break;
									}
								}
								//influences.Find(rec => rec.otherConnector == connectorsToCheck[x]).sqrDistance = distance;
								//owner.allInfluences.Find(rec => rec.thisConnector == this && rec.otherConnector == connectorsToCheck[x]).sqrDistance = distance;
							}
						}
						else
						{
							if (influencingPortsP.Contains(((Port)connectorsToCheck[x])))
							{
								ZoneInteraction zoneIntInfo = new ZoneInteraction();
								zoneIntInfo.thisConnector = this;
								zoneIntInfo.otherConnector = ((Port)connectorsToCheck[x]);
								zoneIntInfo.sqrDistance = distance;
								influencingPortsP.Remove(((Port)connectorsToCheck[x]));
								//influencesP.RemoveAll(rec => rec.thisConnector == this && rec.otherConnector == ((Port)connectorsToCheck[x]));
								for (int i = 0; i < influences.Count; i++)
								{
									if (influences[i].otherConnector == connectorsToCheck[x])
									{
										influences.RemoveAt(i);
										break;
									}
								}
								//owner.allInfluences.RemoveAll(rec => rec.thisConnector == this && rec.otherConnector == (Port)connectorsToCheck[x]);
								for (int i = 0; i < owner.allInfluences.Count; i++)
								{
									if (owner.allInfluences[i].thisConnector == this && owner.allInfluences[i].otherConnector == connectorsToCheck[x])
									{
										owner.allInfluences.RemoveAt(i);
										break;
									}
								}
								//owner.SendMessage(currentPortOptions.influenceMessageExit,zoneIntInfo);
								Utils.SendMessage(owner.gameObject,currentPortOptions.influenceMessageExit,zoneIntInfo);
							}
						}
					}
				}
			}


			if (minCheck.index != -1)
			{				
				attachmentCandidateP = connectorsToCheck[minCheck.index];
				//Debug.Log("ATTCAND: "+attachmentCandidateP.owner.gameObject.name + "." + attachmentCandidateP.connectorName);
				if (attachmentCandidateP is Port)
				{
					attachmentCandidatePositionP = attachmentCandidateP.globalPosition;
					attachmentCandidateRotationP = attachmentCandidateP.globalRotation;
					attachmentCandidateDistanceP = Mathf.Sqrt(minCheck.distance);
					attachmentCandidateAngleDiffP = minCheck.angleDiff;
				}
				if (autoconnect)
					Connect(connectorsToCheck[minCheck.index],placementCollider);
				return true;
			}
			else
			{
				if (placementCollider is Collider)				
				{
					for (int i = 0; i < connectorsToCheck.Count; i++)
					{
						if (connectorsToCheck[i] is Surface)
						{							
							List<Collider> colliders = ((Surface)connectorsToCheck[i]).colliders;
							if (colliders != null)
							{
								for (int x = 0; x < colliders.Count; x++)
								{
									if (colliders[x] == placementCollider)
									{
										attachmentCandidateP = connectorsToCheck[i];
										attachmentCandidatePositionP = attachmentCandidateP.globalPosition;
										attachmentCandidateRotationP = attachmentCandidateP.globalRotation;
										attachmentCandidateDistanceP = -1;
										attachmentCandidateAngleDiffP = -1;
										if (autoconnect)
											Connect(attachmentCandidateP,placementCollider);
										return true;
									}
								}
							}
						}
					}
				}
				if (placementCollider is Collider2D)
				{
					//attachmentCandidateP = connectorsToCheck.Where( rec => rec.GetType() == typeof(Surface)).ToList().Find(rec => ((Surface)rec).colliders2D.Contains((Collider2D)placementCollider));
					for (int i = 0; i < connectorsToCheck.Count; i++)
					{
						if (connectorsToCheck[i] is Surface)
						{
							List<Collider2D> colliders = ((Surface)connectorsToCheck[i]).colliders2D;
							if (colliders != null)
							{
								for (int x = 0; x < colliders.Count; x++)
								{
									if (colliders[x] == placementCollider)
									{
										attachmentCandidateP = connectorsToCheck[i];
										attachmentCandidatePositionP = attachmentCandidateP.globalPosition;
										attachmentCandidateRotationP = attachmentCandidateP.globalRotation;
										attachmentCandidateDistanceP = -1;
										attachmentCandidateAngleDiffP = -1;
										if (autoconnect)
											Connect(attachmentCandidateP,placementCollider);
										return true;
									}
								}
							}
						}
					}
					//return attachmentCandidateP != null;
				}

				//Last check for environment connection
				if (placementCollider != null && Manager.Instance.environmentSurface != null
					&& Manager.Instance.environmentLayers.value == (Manager.Instance.environmentLayers.value | (1 << placementCollider.gameObject.layer))
					&& (Manager.Instance.environmentSurface.portOptions.acceptsConnectionFromLayers & (1 << layer)) > 0)
				{
					attachmentCandidateP = Manager.Instance.environmentSurface;
					if (autoconnect)
						Connect(attachmentCandidateP,placementCollider);
					return true;
				}

				attachmentCandidateP = null;
				attachmentCandidateDistanceP = float.MaxValue;
				attachmentCandidateAngleDiffP = float.MaxValue;
				//owner.awaitingAttachmentTo = null;
				return false;
			}
		}


		/// <summary>
		/// Returns closest avaliable connector, similar to <see cref="Port.CheckPossibleConnections"/>, but without changing <see cref="Port.attachmentCandidate"/> and without spatial limitations of <see cref="Settings.LayerPortOptions"/>. As of current version, this method can't consider <see cref="Surface"/>s as possible candidates except for environment surface.
		/// </summary>
		/// <returns>Closest avaliable connector.</returns>
		/// <param name="maxDistance">Max distance between ports.</param>
		/// <param name="maxAngle">Max angle difference between ports alignment.</param>
		/// <param name="includeSurfaces">Currently do nothing due to <see cref="Surface"/>s search being extremely computationally-heavy.</param>
		/// <param name="useEnvironmentRaycasting">Will shoot a raycast to try and find suitable environment attachment point. Used by <see cref="TerminusObject.includeEnvironmentInAutoSideways"/> </param>
		public Connector ClosestAvaliableConnector(float maxDistance = 0.025f, float maxAngle = 0, bool includeSurfaces = false, bool useEnvironmentRaycasting = false)
		{
			List<DistanceCheck> candidates = new List<DistanceCheck>();
			Settings.LayerPortOptions currentPortOptions = portOptions;
			maxDistance *= maxDistance;
			float distance;
			for (int x = 0; x < connectorsToCheck.Count; x++)
			{
				distance = float.MaxValue;
				if (connectorsToCheck[x].GetType() == typeof(Port))
				{
					Port portToCheck = (Port)connectorsToCheck[x];
					AttachmentInfo selfParent = owner.parent;
					AttachmentInfo checkParent = portToCheck.owner.parent;
					if ((selfParent.attachmentType == AttachmentInfo.Types.none || portToCheck.owner != selfParent.otherConnector.owner) && (checkParent.attachmentType == AttachmentInfo.Types.none || owner != checkParent.otherConnector.owner))
					{
						if (!currentPortOptions.use2DPhysics)
							distance = Vector3.SqrMagnitude(transform.TransformPoint(offsetP)
						    	                            - connectorsToCheck[x].transform.TransformPoint((portToCheck).offset));
						else
							distance = Vector2.SqrMagnitude(Utils.XY(transform.TransformPoint(offsetP))
							                                - Utils.XY(connectorsToCheck[x].transform.TransformPoint((portToCheck).offset)));


						//float angleDiff = 0;
						//if (maxAngle != 0)
						//	angleDiff = (portOptions.onlyPlanarSnap) ? Vector3.Angle(globalRotation * Vector3.forward, connectorsToCheck[x].globalRotation * (-Vector3.forward)) : Quaternion.Angle(globalRotation * Quaternion.Euler(new Vector3(0,180,0)),connectorsToCheck[x].globalRotation);
						float angleDiff = 0;
						if (maxAngle != 0)
						{
							switch (currentPortOptions.orientationType)
							{
							case Settings.OrientationTypes.exact:
								angleDiff = Quaternion.Angle(globalRotation * flipY,connectorsToCheck[x].globalRotation);
								break;
							case Settings.OrientationTypes.planar:
								angleDiff = Vector3.Angle(globalRotation * Vector3.forward, connectorsToCheck[x].globalRotation * (-Vector3.forward));
								break;
							case Settings.OrientationTypes.twosided:
								angleDiff = Mathf.Min(Quaternion.Angle(globalRotation * flipY,connectorsToCheck[x].globalRotation),
								                      Quaternion.Angle(globalRotation * flipY * flipZ,connectorsToCheck[x].globalRotation));
								break;
							}
							//angleDiff = (currentPortOptions.onlyPlanarSnap) ? Vector3.Angle(globalRotation * Vector3.forward, connectorsToCheck[x].globalRotation * (-Vector3.forward)) : Quaternion.Angle(globalRotation * Quaternion.Euler(new Vector3(0,180,0)),connectorsToCheck[x].globalRotation);				
						}

						if (distance <= maxDistance
							&& (maxAngle == 0
								|| angleDiff < maxAngle))
						{
							candidates.Add(new DistanceCheck(x,distance,angleDiff));
						}
					}
				}
				else if (includeSurfaces && connectorsToCheck[x].GetType() == typeof(Surface))
				{
					//TODO SURFACE DISTANCE DETERMINATION USING Collider.ClosestPointOnBounds and raycasting
					//Consider bounds.contains since it means distance = 0 (that's best approximation without raycasting)
					/*
					if (portOptions.use2DPhysics)
						distance = connectorsToCheck.SelectMany(rec => ((Surface)rec).colliders2D).Min(rec => rec.bounds.ClosestPoint(transform.TransformPoint(offsetP)));
					else
						distance = connectorsToCheck.SelectMany(rec => ((Surface)rec).colliders).Min(rec => rec.bounds.ClosestPoint(transform.TransformPoint(offsetP)));
					if (distance < maxDistance)
					{
						candidates.Add(new DistanceCheck(x,distance));
					}
					*/
				}
			}
			if (candidates.Count > 0)
			{
				float min = candidates.Min(rec => rec.distance);
				return connectorsToCheck[candidates.First(rec => rec.distance == min).index];
			}
			else
			{
				if (useEnvironmentRaycasting)
				{					
					//RaycastHit hit;
					if (Physics.Raycast(globalRotation * Vector3.back * 0.02f +  globalPosition, globalRotation * Vector3.forward, maxDistance+0.02f, Manager.Instance.environmentLayers))
					{				
						return Manager.Instance.environmentSurface;
					}
				}
				return null;
			}
		}

		/// <summary>
		/// Calls either <see cref="Connector.AlignPortWithThisConnector"/>(and possibly <see cref="Port.AttachTo"/> depending on <see cref="TerminusObject.attachAutomatically"/>) on provided <see cref="Connector"/>.
		/// If null provided as connector, will try to connecto to <see cref="Port.attachmentCandidate"/> 
		/// </summary>
		/// <param name="connector">Opposite connector.</param>
		/// <returns><c>true</c> if successfuly connected,<c>false</c> if both provided connector and attachmentCandidate are null.
		public bool Connect(Connector connector = null,Component collider = null)
		{
			if (connector == null)
			{
				if (attachmentCandidateP == null)
					return false;
				else
				{
					connector = attachmentCandidateP;
					return true;
				}
			}
			connector.AlignPortWithThisConnector(this,globalPosition,globalRotation,collider);
			if (owner.attachAutomatically)
				AttachTo(connector);
			return true;
		}


		public override void RegisterAttachmentFromConnector(AttachmentInfo attachInfo)
		{
			base.RegisterAttachmentFromConnector(attachInfo);

			if (attachmentInfoP.attachmentType == AttachmentInfo.Types.none)
				attachmentInfoP = attachInfo;
			else
				Debug.LogWarning(gameObject.name + " : Trying to register attachment to port that already have connection. Something went wrong.");
		}


		public override void RegisterDetachmentFromConnector(Connector conn)
		{			
			base.RegisterDetachmentFromConnector(conn);

			if (attachmentInfoP.otherConnector == conn)
				attachmentInfoP.attachmentType = AttachmentInfo.Types.none;
		}

		public override bool AlignPortWithThisConnector(Port port, Vector3 position, Quaternion rotation, Component collider)
		{
			port.Align(globalPosition,globalRotation);
			return true;
		}

		/// <summary>
		/// Creates attachment to provided <see cref="Connector"/> or to <see cref="Port.attachmentCandidate"/> if null was provided.
		/// Exact actions performed by attachment is dependent on <see cref="Settings.LayerPortOptions"/> of this port.
		/// Only consisnent action AttachTo performs is to declare this port as child to provided connector.
		/// In general, this action calls OnBeforeAttachment and OnAfterAttachment events, parent transforms of owners <see cref="TerminusObject"/>s, and/or creates physics joints.
		/// </summary>
		/// <returns><c>true</c>, if port was attached succesfully, <c>false</c> otherwise (for example, if port was already <see cref="Port.isOccupied"/>.</returns>
		/// <param name="connector">Connector to attach this port to. Leave null to attach this port to <see cref="Port.attachmentCandidate"/>.</param>
		/// <param name="autoReparent">If true, indicates that this attachment attempt is caused by <see cref="TerminusObject.autoReparentToSideways"/>.</param>
		public bool AttachTo(Connector connector = null, bool autoReparented = false)
		{
			Settings.LayerPortOptions currentPortOptions = portOptions;
			if (attachmentInfoP.attachmentType == AttachmentInfo.Types.none)
			{
				if (connector == null)
				{
					if (attachmentCandidateP == null)
					{
						Debug.LogWarning(gameObject.name + " : Trying to attach port without designating opposing connector while attachmentCandidate is null.");
						return false;
					}
					else
						connector = attachmentCandidateP;
				}

				//Performance drain. Look for other solution.
				if (owner.GetChildLevelFrom(connector.owner) > 0 || connector.owner.GetChildLevelFrom(owner) > 0)
				{
					Debug.LogWarning(owner.gameObject.name + ", " + "Port:" + connectorName + " : Trying to attach port to object already in hierarchy of its TerminusObject. Only sideways attachment allowed in this situation.");
					return false;
				}

				bool weldingPossible = (connector.useOwnerRigidbody && useOwnerRigidbody);

				Component connJoint = null;

				TerminusObject weldCandidate = null;
				if (weldingPossible && currentPortOptions.attachmentType == Settings.AttachmentTypes.rigidbody_welding)
				{
					if (connector.owner.weldedToObject == null)
						weldCandidate = connector.owner;
					else
						weldCandidate = connector.owner.weldedToObject;
				}

				bool parenting = currentPortOptions.attachmentType == Settings.AttachmentTypes.hierarchy 
					|| currentPortOptions.attachmentType == Settings.AttachmentTypes.joints_hierarchy
					|| (weldingPossible && currentPortOptions.attachmentType == Settings.AttachmentTypes.rigidbody_welding);

				Vector3 invPosDiff = offsetP - transform.InverseTransformPoint(connector.globalPosition);
				Vector3 posDiff = connector.transform.InverseTransformPoint(connector.globalPosition) - connector.transform.InverseTransformPoint(globalPosition);
				Quaternion invRotDiff = Quaternion.Inverse(globalRotation) * connector.globalRotation;
				Quaternion rotDiff = Quaternion.Inverse(connector.globalRotation) * globalRotation;

				AttachmentInfo newAttInfo = new AttachmentInfo(AttachmentInfo.Types.child,this,connector,connJoint,parenting,(weldingPossible && currentPortOptions.attachmentType == Settings.AttachmentTypes.rigidbody_welding),true,autoReparented,posDiff,rotDiff);
				AttachmentInfo portAttInfo = new AttachmentInfo(AttachmentInfo.Types.parent,connector,this,connJoint,parenting,(weldingPossible && currentPortOptions.attachmentType == Settings.AttachmentTypes.rigidbody_welding),false,autoReparented,invPosDiff,invRotDiff);

				/*
				if (newAttInfo.weldingAttachment)
				{
					if (weldCandidate.weldedObjects.Count == 0)
					{
						weldCandidate.FillRigidbodyInfo();
						weldCandidate.FillRigidbodyInfo2D();
					}
				}
				*/

				if (Application.isPlaying || ProjectManager.settings.fireMessagesInEditMode)
				{
                    if (currentPortOptions.sendMessagesToPorts)
                        OnBeforeAttachment(newAttInfo);
                    if (currentPortOptions.sendMessagesToOwnerObjects)
                        owner.OnBeforeAttachment(newAttInfo);
                    if (currentPortOptions.sendMessagesToGameObject)
                        Manager.SendOnBeforeAttachmentToGlobalReciever(newAttInfo);
                    else if (connector.portOptions.sendMessagesToGameObject)
                        Manager.SendOnBeforeAttachmentToGlobalReciever(newAttInfo);

                    if (connector.portOptions.sendMessagesToPorts)
                        connector.OnBeforeAttachment(portAttInfo);
                    if (connector.portOptions.sendMessagesToOwnerObjects)
                        connector.owner.OnBeforeAttachment(portAttInfo);
                }

				attachmentInfoP = newAttInfo;


				if (currentPortOptions.attachmentType == Settings.AttachmentTypes.physic_joints 
					|| currentPortOptions.attachmentType == Settings.AttachmentTypes.joints_hierarchy
					|| (!weldingPossible && currentPortOptions.attachmentType == Settings.AttachmentTypes.rigidbody_welding)
					)
				{					
					if ((!currentPortOptions.use2DPhysics && currentPortOptions.jointPrefab == null)
					    || (currentPortOptions.use2DPhysics && currentPortOptions.jointPrefab2D == null))
					{
						if (currentPortOptions.use2DPhysics)
						{
							DistanceJoint2D joint = connectorRigidbody2D.gameObject.AddComponent<DistanceJoint2D>();
							connJoint = joint;
							joint.connectedBody = connector.connectorRigidbody2D;
							joint.distance = Vector3.Distance(connectorRigidbody2D.transform.position,connector.connectorRigidbody2D.transform.position); 
							joint.anchor = Utils.XY(connectorRigidbody2D.transform.InverseTransformPoint(globalPosition));
							if (connector.GetType() == typeof(Port))						
								joint.connectedAnchor = Utils.XY(connectorRigidbody2D.transform.InverseTransformPoint(connector.globalPosition));
						}
						else
						{
							FixedJoint joint = connectorRigidbody.gameObject.AddComponent<FixedJoint>();
							connJoint = joint;
							joint.connectedBody = connector.connectorRigidbody;
							joint.anchor = connectorRigidbody.transform.InverseTransformPoint(transform.TransformPoint(offset));
							if (connector.GetType() == typeof(Port))
								joint.connectedAnchor = connectorRigidbody.transform.InverseTransformPoint(connector.transform.TransformPoint(((Port)connector).offset));
						}
					}
					else
					{
						if (currentPortOptions.use2DPhysics)
						{
							Joint2D joint = (Joint2D)Utils.CopyJoint(currentPortOptions.jointPrefab2D,connectorRigidbody2D.gameObject);
							connJoint = joint;
							joint.connectedBody = connector.connectorRigidbody2D;
							if (joint is AnchoredJoint2D)
							{
								Vector3 anchor = connectorRigidbody2D.transform.InverseTransformPoint(transform.TransformPoint(offset));
								((AnchoredJoint2D)joint).anchor = new Vector2(anchor.x,anchor.y);
								if (connector.GetType() == typeof(Port))
								{
									anchor = connectorRigidbody.transform.InverseTransformPoint(connector.transform.TransformPoint(((Port)connector).offset));
									((AnchoredJoint2D)joint).connectedAnchor = new Vector2(anchor.x,anchor.y);
								}
							}
						}
						else
						{
							Joint joint = (Joint)Utils.CopyJoint(currentPortOptions.jointPrefab,connectorRigidbody.gameObject);
							connJoint = joint;
							joint.connectedBody = connector.connectorRigidbody;
							joint.anchor = connectorRigidbody.transform.InverseTransformPoint(transform.TransformPoint(offset));
							if (connector.GetType() == typeof(Port))
								joint.connectedAnchor = connectorRigidbody.transform.InverseTransformPoint(connector.transform.TransformPoint(((Port)connector).offset));
						}
					}
				}


				if (parenting)
		        {					
					owner.transform.SetParent(connector.transform);
					if (owner.container != null)
						Manager.FreeContainer(owner.container);					
				}
				else
				{
					TerminusObject root = connector.owner.treeRoot;
					if (root.container != null)
					{
						Transform oldParent = owner.transform.parent;

						owner.transform.parent = root.container.transform;

						Transform[] childArr = owner.treeListDown.Select(rec => rec.otherConnector.owner.transform).ToArray();
						for (int i = 0; i < childArr.Length; i++)
						{
							if (childArr[i].parent == oldParent)
								childArr[i].parent = root.container.transform;
						}
					}
					else
					{
						if (owner.container != null)
							owner.transform.parent = Manager.Instance.globalTransform;
					}
					if (owner.container != null)
						Manager.FreeContainer(owner.container);					
				}
					
				attachmentInfoP.joint = connJoint;
				attachmentInfoP.hierarchyAttachment = parenting;
				portAttInfo.joint = connJoint;
				portAttInfo.hierarchyAttachment = parenting;

				connector.RegisterAttachmentFromConnector(portAttInfo);



				if (portAttInfo.weldingAttachment)
				{					
					owner.WeldToRigidbody(weldCandidate,currentPortOptions.use2DPhysics,currentPortOptions.destroyRigidbodyImmediately);				
					List<TerminusObject> weldedObjs = owner.weldedObjects;
					for (int i = 0; i < weldedObjs.Count; i++)
						weldedObjs[i].WeldToRigidbody(weldCandidate,currentPortOptions.use2DPhysics,currentPortOptions.destroyRigidbodyImmediately);					
				}


					
				owner.inAssemblyState = connector.owner.inAssemblyState;

				owner.ClearInfluences();

				Initialize();
				connector.Initialize();

				if (Application.isPlaying || ProjectManager.settings.fireMessagesInEditMode)
				{
                    if (currentPortOptions.sendMessagesToPorts)
                        OnAfterAttachment(attachmentInfoP);
					if (currentPortOptions.sendMessagesToOwnerObjects)				
						owner.OnAfterAttachment(attachmentInfoP);
                    if (currentPortOptions.sendMessagesToGameObject)
                        Manager.SendOnAfterAttachmentToGlobalReciever(attachmentInfoP);
                    else if (connector.portOptions.sendMessagesToGameObject)
                        Manager.SendOnAfterAttachmentToGlobalReciever(attachmentInfoP);                    

                    if (connector.portOptions.sendMessagesToPorts)
                        connector.OnAfterAttachment(portAttInfo);
					if (connector.portOptions.sendMessagesToOwnerObjects)
                        connector.owner.OnAfterAttachment(portAttInfo);
                }

				if (owner.longConnection)
				{
					LongConnection lConn = owner.GetComponent<LongConnection>();
					if (lConn != null)
						lConn.LongConnectionAfterAttachment(attachmentInfoP);
					else
						Debug.LogError("LongConnection component not present on long connection TerminusObject.");
				}

				if (owner.attachAutomatically)
					owner.mode = connector.owner.mode;

				return true;
			}
			else
			{
				Debug.LogWarning(gameObject.name + ",port:" + connectorName + " : Trying to attach port that's already attached. Something went wrong.");
				return false;
			}
		}

		/// <summary>
		/// Similar to <see cref="Port.AttachTo"/>, but creates sideway attachment.
		/// </summary>
		/// <remarks>
		/// Sideway attachment function identical as normal attachment, but do not affect Terminus hierarchy (noone is declared child or parent). It also can't affect transform hierarchy.
		/// </remarks>
		/// <returns><c>true</c>, if sideways attachment successful, <c>false</c> otherwise.</returns>
		/// <param name="connector">Connector to attach to.</param>
		public bool AttachToSideways(Connector connector)
		{
			Settings.LayerPortOptions currentPortOptions = portOptions;
			if (attachmentInfoP.attachmentType == AttachmentInfo.Types.none)
			{
				if (currentPortOptions.attachmentType == Settings.AttachmentTypes.physic_joints
					|| currentPortOptions.attachmentType == Settings.AttachmentTypes.joints_hierarchy
					|| currentPortOptions.attachmentType == Settings.AttachmentTypes.rigidbody_welding)
				{
					Component connJoint = null;

					Vector3 posDiff = offsetP - transform.InverseTransformPoint(connector.globalPosition);
					Vector3 invPosDiff = connector.transform.InverseTransformPoint(connector.globalPosition) - connector.transform.InverseTransformPoint(globalPosition);
					Quaternion rotDiff = Quaternion.Inverse(globalRotation) * connector.globalRotation;
					Quaternion invRotDiff = Quaternion.Inverse(connector.globalRotation) * globalRotation;

					AttachmentInfo newAttInfo = new AttachmentInfo(AttachmentInfo.Types.sideway,this,connector,connJoint,false,false,true,false,posDiff,rotDiff);
					AttachmentInfo portAttInfo = new AttachmentInfo(AttachmentInfo.Types.sideway,connector,this,connJoint,false,false,false,false,invPosDiff,invRotDiff);

					if (Application.isPlaying || ProjectManager.settings.fireMessagesInEditMode)
					{
                        if (currentPortOptions.sendMessagesToPorts)
                            OnBeforeAttachment(attachmentInfoP);
                        if (currentPortOptions.sendMessagesToOwnerObjects)
                            owner.OnBeforeAttachment(attachmentInfoP);
                        if (currentPortOptions.sendMessagesToGameObject)
                            Manager.SendOnBeforeAttachmentToGlobalReciever(attachmentInfoP);
                        else if (connector.portOptions.sendMessagesToGameObject)
                            Manager.SendOnBeforeAttachmentToGlobalReciever(attachmentInfoP);

                        if (connector.portOptions.sendMessagesToPorts)
                            connector.OnBeforeAttachment(portAttInfo);
                        if (connector.portOptions.sendMessagesToOwnerObjects)
                            connector.owner.OnBeforeAttachment(portAttInfo);                        
					}


					attachmentInfoP = newAttInfo;

					if (((connectorRigidbody != null) || (connectorRigidbody2D != null))
						&&
						(currentPortOptions.attachmentType == Settings.AttachmentTypes.physic_joints ||
						 currentPortOptions.attachmentType == Settings.AttachmentTypes.joints_hierarchy ||
						 (currentPortOptions.attachmentType == Settings.AttachmentTypes.rigidbody_welding && owner.weldedToObject
							!= connector.owner && connector.owner.weldedToObject != owner && owner.weldedToObject != connector.owner.weldedToObject)))
					{
						if ((!currentPortOptions.use2DPhysics && currentPortOptions.jointPrefab == null)
							|| (currentPortOptions.use2DPhysics && currentPortOptions.jointPrefab2D == null))
						{
							if (currentPortOptions.use2DPhysics)
							{
								DistanceJoint2D joint = connectorRigidbody2D.gameObject.AddComponent<DistanceJoint2D>();
								connJoint = joint;
								joint.connectedBody = connector.connectorRigidbody2D;
								joint.distance = Vector3.Distance(globalPosition,connector.globalPosition);
								Vector3 anchor = connectorRigidbody2D.transform.InverseTransformPoint(transform.TransformPoint(offset));
								joint.anchor = new Vector2(anchor.x,anchor.y);
								if (connector.GetType() == typeof(Port))
								{
									anchor = connectorRigidbody2D.transform.InverseTransformPoint(connector.transform.TransformPoint(((Port)connector).offset));
									joint.connectedAnchor = new Vector2(anchor.x,anchor.y);
								}
							}
							else
							{
								FixedJoint joint = connectorRigidbody.gameObject.AddComponent<FixedJoint>();
								connJoint = joint;
								joint.connectedBody = connector.connectorRigidbody;
								joint.anchor = connectorRigidbody.transform.InverseTransformPoint(transform.TransformPoint(offset));
								if (connector.GetType() == typeof(Port))
									joint.connectedAnchor = connectorRigidbody.transform.InverseTransformPoint(connector.transform.TransformPoint(((Port)connector).offset));
							}
						}
						else
						{
							if (currentPortOptions.use2DPhysics)
							{
								Joint2D joint = (Joint2D)Utils.CopyJoint(currentPortOptions.jointPrefab2D,connectorRigidbody2D.gameObject);
								connJoint = joint;
								joint.connectedBody = connector.connectorRigidbody2D;
								if (joint is AnchoredJoint2D)
								{
									Vector3 anchor = connectorRigidbody2D.transform.InverseTransformPoint(transform.TransformPoint(offset));
									((AnchoredJoint2D)joint).anchor = new Vector2(anchor.x,anchor.y);
									if (connector.GetType() == typeof(Port))
									{
										anchor = connectorRigidbody2D.transform.InverseTransformPoint(connector.transform.TransformPoint(((Port)connector).offset));
										((AnchoredJoint2D)joint).connectedAnchor = new Vector2(anchor.x,anchor.y);
									}
								}
							}
							else
							{
								Joint joint = (Joint)Utils.CopyJoint(currentPortOptions.jointPrefab,connectorRigidbody.gameObject);
								joint.connectedBody = connector.connectorRigidbody;
								joint.anchor = connectorRigidbody.transform.InverseTransformPoint(transform.TransformPoint(offset));
								if (connector.GetType() == typeof(Port))
									joint.connectedAnchor = connectorRigidbody.transform.InverseTransformPoint(connector.transform.TransformPoint(((Port)connector).offset));
							}
						}
					}

					attachmentInfoP.joint = connJoint;
					portAttInfo.joint = connJoint;

					connector.RegisterAttachmentFromConnector(portAttInfo);

					Initialize();
					connector.Initialize();

					if (Application.isPlaying || ProjectManager.settings.fireMessagesInEditMode)
					{
                        if (currentPortOptions.sendMessagesToPorts)
                            OnAfterAttachment(attachmentInfoP);
                        if (currentPortOptions.sendMessagesToOwnerObjects)
                            owner.OnAfterAttachment(attachmentInfoP);
                        if (currentPortOptions.sendMessagesToGameObject)
                            Manager.SendOnAfterAttachmentToGlobalReciever(attachmentInfoP);
                        else if (connector.portOptions.sendMessagesToGameObject)
                            Manager.SendOnAfterAttachmentToGlobalReciever(attachmentInfoP);

                        if (connector.portOptions.sendMessagesToPorts)
                            connector.OnAfterAttachment(portAttInfo);
                        if (connector.portOptions.sendMessagesToOwnerObjects)
                            connector.owner.OnAfterAttachment(portAttInfo);                        
                    }

					if (owner.longConnection)
					{
						LongConnection lConn = owner.GetComponent<LongConnection>();
						if (lConn != null)
							lConn.LongConnectionAfterAttachment(attachmentInfoP);
						else
							Debug.LogError(": LongConnection component not present on long connection TerminusObject");
					}

					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				Debug.LogWarning(": Trying to sideway attach a port <" + connectorName + "> on object <" + gameObject.name +"> that's already attached. Something went wrong.");
				return false;
			}
		}

		/// <summary>
		/// Destroys current attachment and reverts all actions done by attaching. Called automatically if joint breaks.
		/// </summary>
		/// <remarks>
		/// Also can call OnBeforeDetachment and OnAfterDetachment events.
		/// </remarks>
		/// <param name="doNotTouchHierarchy">If set to <c>true</c>, transform hierarchy will not be reverted.</param>
		/// <param name="doNotRestoreRigidbody">If set to <c>true</c>, Rigidbody destroyed by rigidbody welding process will not be restored.</param>
		/// <param name="noAutoReparenting">If set to <c>true</c>, <see cref="TerminusObject.autoReparentToSideways"/> setting will be ignored.</param>
		/// <param name="noOppositeAutoReparenting">If set to <c>true</c>, <see cref="TerminusObject.autoReparentToSideways"/> of <see cref="TerminusObject"/> attached to this port setting will be ignored.</param>
		/// <param name="oldRoot">Used by auto-reparenting, tells what root object reparenting must aim for.</param>
		public void Detach(bool doNotTouchHierarchy = false, bool doNotRestoreRigidbody = false, bool noAutoReparenting = false, bool noOppositeAutoReparenting = false, TerminusObject oldRoot = null)
		{
			if (attachmentInfo.attachmentType != AttachmentInfo.Types.none)
			{								
				Connector oppositeConnector = attachmentInfoP.otherConnector;
				bool reparent = !noAutoReparenting && !destroyFlag && !owner.destroyFlag && owner.autoReparentToSideways && attachmentInfoP.attachmentType == AttachmentInfo.Types.child && oppositeConnector.owner != oldRoot;
				bool oppositeReparent = !noOppositeAutoReparenting && oppositeConnector != null && !oppositeConnector.destroyFlag && !oppositeConnector.owner.destroyFlag && oppositeConnector.owner.autoReparentToSideways && attachmentInfoP.attachmentType == AttachmentInfo.Types.parent && owner != oldRoot;

				AttachmentInfo otherAttInf = oppositeConnector.GetAttachmentInfo(this);

				if (Application.isPlaying || ProjectManager.settings.fireMessagesInEditMode)
				{
                    if (portOptions.sendMessagesToPorts)
                        OnBeforeDetachment(this.attachmentInfoP);
                    if (portOptions.sendMessagesToOwnerObjects)
                        owner.OnBeforeDetachment(this.attachmentInfoP);
                    if (portOptions.sendMessagesToGameObject)
                        Manager.SendOnBeforeDetachmentToGlobalReciever(this.attachmentInfoP);
                    else if (oppositeConnector.portOptions.sendMessagesToGameObject)
                        Manager.SendOnBeforeDetachmentToGlobalReciever(this.attachmentInfoP);

                    if (oppositeConnector.portOptions.sendMessagesToPorts)
                        oppositeConnector.OnBeforeDetachment(otherAttInf);
                    if (oppositeConnector.portOptions.sendMessagesToOwnerObjects)
                        oppositeConnector.owner.OnBeforeDetachment(otherAttInf);
                }

				if (attachmentInfoP.joint != null)
				{
					if (Application.isPlaying)
						Destroy(attachmentInfo.joint);
					else
					{
						DestroyImmediate(attachmentInfo.joint);
					}
				}
					

				if (!doNotTouchHierarchy)
				{
					switch (attachmentInfoP.attachmentType)
					{
					case AttachmentInfo.Types.sideway:
						//owner.sidewaysConnections.Remove(selfConnection);
						//selfConnection.otherConnector.owner.sidewaysConnections.Remove(otherConnection);
						break;
					case AttachmentInfo.Types.child:
						if (attachmentInfoP.hierarchyAttachment)
						{
							if (owner.createContainerWhenRoot && Application.isPlaying)
								Manager.SetContainer(owner);
							else
							{
								owner.transform.parent = Manager.staticGlobalTransform;							
							}
						}
						break;
					case AttachmentInfo.Types.parent:
						if (attachmentInfoP.hierarchyAttachment)
						{
							if (oppositeConnector.owner.createContainerWhenRoot)
								Manager.SetContainer(oppositeConnector.owner);
							else
							{
								oppositeConnector.owner.transform.parent = Manager.staticGlobalTransform;
							}
						}
						break;
					}
				}

				oppositeConnector.RegisterDetachmentFromConnector(this);
				
				attachmentInfoP.attachmentType = AttachmentInfo.Types.none;

				Initialize();

				oppositeConnector.Initialize();

				otherAttInf.attachmentType = AttachmentInfo.Types.none;

				if (attachmentInfoP.weldingAttachment)
				{
					if (attachmentInfoP.selfIsInitiator)
						owner.UnweldRigidbody(doNotRestoreRigidbody);
					else
						oppositeConnector.owner.UnweldRigidbody(doNotRestoreRigidbody);
					
				}
					

				if (Application.isPlaying || ProjectManager.settings.fireMessagesInEditMode)
				{
                    if (portOptions.sendMessagesToPorts)
                        OnAfterDetachment(this.attachmentInfoP);
                    if (portOptions.sendMessagesToOwnerObjects)
                        owner.OnAfterDetachment(this.attachmentInfoP);
                    if (portOptions.sendMessagesToGameObject)
                        Manager.SendOnAfterDetachmentToGlobalReciever(this.attachmentInfoP);
                    else if (oppositeConnector.portOptions.sendMessagesToGameObject)
                        Manager.SendOnAfterDetachmentToGlobalReciever(this.attachmentInfoP);

                    if (oppositeConnector.portOptions.sendMessagesToPorts)
                        oppositeConnector.OnAfterDetachment(otherAttInf);
                    if (oppositeConnector.portOptions.sendMessagesToOwnerObjects)
                        oppositeConnector.owner.OnAfterDetachment(otherAttInf);                    
                }

				if (reparent && !destroyFlag && !owner.destroyFlag)
				{
					owner.ReparentWithSidewaysConnections(oldRoot);
				}

				if (oppositeReparent && !oppositeConnector.destroyFlag && !oppositeConnector.owner.destroyFlag )
				{
					oppositeConnector.owner.ReparentWithSidewaysConnections(oldRoot);
				}
			}
		}

		/// <summary>
		/// Gets closest rigidbody in transform hierarchy by looking up the transform tree.
		/// </summary>
		public Rigidbody GetClosestRigidbody()
		{
			if (connectorRigidbody != null)
				return connectorRigidbody;
			Rigidbody current = GetComponent<Rigidbody>();
			while (current == null && current.transform.parent != null)
			{
				current = current.transform.parent.GetComponent<Rigidbody>();
			}
			return current;
		}

		/// <summary>
		/// Gets the closest rigidbody2D in transform hierarchy by looking up the transform tree.
		/// </summary>
		public Rigidbody2D GetClosestRigidbody2D()
		{
			if (connectorRigidbody2D != null)
				return connectorRigidbody2D;
			Rigidbody2D current = GetComponent<Rigidbody2D>();
			while (current == null && current.transform.parent != null)
			{
				current = current.transform.parent.GetComponent<Rigidbody2D>();
			}
			return current;
		}

		/// <summary>
		/// Calculates closest number of possible symmetric <see cref="Port"/>s based on provided desired number. 
		/// </summary>
		/// <returns>The symmetry count.</returns>
		/// <param name="desiredCount">Desired count of attachable symmetric objects.</param>
		/// <seealso cref="TerminusObject.symmetryGroups"/>
		public override int AchievableSymmetryCount(int desiredCount)
		{
			if (owner.symmetricSiblings == null || owner.symmetricSiblings.Count == 0 || desiredCount != owner.symmetricSiblings.Count)
			{
				if (owner.useSymmetryGroups)
				{
					//List<Connector> symmConns = owner.symmetryGroups.Where(rec => rec.connectors.Contains(this)).OrderBy(rec => Mathf.Abs(desiredCount + 1-rec.connectors.Count)).FirstOrDefault().connectors;
					List<Connector> symmConns = null;
					List<TerminusObject.SymmetricGroup> symmGroups = owner.symmetryGroups;
					int closestMatch = int.MaxValue;
					for (int i = 0; i < symmGroups.Count; i++)
					{
						if (symmGroups[i].connectors.Contains(this) &&  Mathf.Abs((desiredCount + 1 - symmGroups[i].connectors.Count)) < closestMatch)
						{
							closestMatch = Mathf.Abs((desiredCount + 1 - symmGroups[i].connectors.Count));
							symmConns = symmGroups[i].connectors;
						}
					}

					if (symmConns != null)
						return symmConns.Count-1;
					else
						return 0;
				}
				else
					return 0;
			}
			return owner.symmetricSiblings.Count;
		}

		protected List<PositionInfo> symmPositions_internal = new List<PositionInfo>();
		/// <summary>
		/// Gets positions of symmetric <see cref="Port"/>s for attaching multiple <see cref="TerminusObject"/>s symmetrically.
		/// </summary>
		/// <param name="positionCount">Position count. Use <see cref="Port.AchievableSymmetryCount"/> to prevent over-drafting symmetry count.</param>
		/// <param name="originalPosition">Global position of port that tries to attach to this port.</param>
		/// <param name="originalRotation">Global rotation of port that tries to attach to this port.</param>
		/// <param name="collider">Collider provided by raycasting placement of part. Can be null, do not affect anything in case of port symmetry.</param>
		public override List<PositionInfo> GetSymmetryPositions(int positionCount, Vector3 originalPosition, Quaternion originalRotation, Component collider = null)
		{
			if (owner.symmetricSiblings != null && owner.symmetricSiblings.Count == positionCount)
			{
				Vector3 diffPos = originalPosition - globalPosition;
				Quaternion diffRot = Quaternion.Inverse(globalRotation) * originalRotation;
				//PositionInfo[] result = new PositionInfo[positionCount];
				if (symmPositions_internal.Count != positionCount)
					symmPositions_internal.Clear();
				for (int i = 0; i < positionCount; i++)
				{
					Connector conn = owner.symmetricSiblings[i].connectors[index];
					symmPositions_internal.Add(new PositionInfo(conn.globalPosition + conn.globalRotation * Quaternion.Inverse(globalRotation) * diffPos,
						conn.globalRotation * diffRot,
						conn,
						collider));
					//result[i].connector = owner.symmetricSiblings[i].connectors[index];
					//Vector3 curDiffPos = result[i].connector.globalRotation * Quaternion.Inverse(globalRotation) * diffPos;
					//result[i].position = result[i].connector.globalPosition + curDiffPos;
					//result[i].rotation = result[i].connector.globalRotation * diffRot;
					//result[i].collider = collider;
				}
				return symmPositions_internal;
			}
			else
			{
				if (owner.useSymmetryGroups)
				{
					//List<Port> symmConns = owner.symmetryGroups.Find(rec => rec.connectors.Count == positionCount+1 && rec.connectors.Contains(this)).connectors.Select(rec => (Port)rec).ToList();
					List<Connector> symmConns = null;
					for (int i = 0; i < owner.symmetryGroups.Count; i++)
					{
						if (owner.symmetryGroups[i].connectors.Count == positionCount+1 && owner.symmetryGroups[i].connectors.Contains(this))
						{
							symmConns = owner.symmetryGroups[i].connectors;
						}
					}

					if (symmConns != null)
					{
						//symmConns.Remove(this);
						Vector3 diffPos = originalPosition - globalPosition;
						Quaternion diffRot = Quaternion.Inverse(globalRotation) * originalRotation;
						//PositionInfo[] result = new PositionInfo[positionCount];
						if (symmPositions_internal.Count != positionCount)
							symmPositions_internal.Clear();
						for (int i = 0; i < symmConns.Count; i++)
						{
							if (symmConns[i] != this)
							{
								Connector conn = symmConns[i];
								symmPositions_internal.Add(new PositionInfo(conn.globalPosition + conn.globalRotation * Quaternion.Inverse(globalRotation) * diffPos,
									conn.globalRotation * diffRot,
									conn,
									collider));
							}
							/*
							result[i].connector = symmConns[i];
							Vector3 curDiffPos = result[i].connector.globalRotation * Quaternion.Inverse(globalRotation) * diffPos;
							result[i].position = result[i].connector.globalPosition + curDiffPos;
							result[i].rotation = result[i].connector.globalRotation * diffRot;
							result[i].collider = collider;
							*/
						}
						return symmPositions_internal;
					}					
				}
			}
			if (symmPositions_internal.Count != positionCount)
				symmPositions_internal.Clear();
			return symmPositions_internal;
		}


		// Handles detachment on joint breaks
		protected IEnumerator OnJointBreak(float breakForce)
		{		
			bool checkJoint = attachmentInfoP.attachmentType != AttachmentInfo.Types.none && attachmentInfoP.joint != null;
			if (!checkJoint)
				yield break;
			yield return null;
			if (checkJoint && attachmentInfoP.joint == null)
			{
				Detach();
			}
		}


		protected void Awake ()
		{
			if (Application.isPlaying)
			{
				influencingPortsP = new HashSet<Port>();
				influences = new List<ZoneInteraction>();
			}
		}
		

		void OnApplicationQuit()
		{
			appQuit = true;
		}


		protected override void OnDestroy ()
		{			
			destroyFlag = true;
			#if UNITY_EDITOR
			if (!appQuit && !Manager.quitFlag && !owner.destroyFlag)
			{								
				if (attachmentInfo.attachmentType != AttachmentInfo.Types.none)
				{
					Connector otherConn = attachmentInfoP.otherConnector;
					TerminusObject otherObj = otherConn.owner;
					Detach(false,attachmentInfo.attachmentType == AttachmentInfo.Types.child);
					EditorUtility.SetDirty(otherConn);
					EditorUtility.SetDirty(otherObj);
				}
				Manager.UnregisterConnector(this);
				if (Manager.Instance != null)
					EditorUtility.SetDirty(Manager.Instance);
			}
			#else	
			if (!appQuit && !owner.destroyFlag)
			{			
				destroyFlag = true;
				if (attachmentInfo.attachmentType != AttachmentInfo.Types.none)
				{
					Detach(false,attachmentInfo.attachmentType == AttachmentInfo.Types.child);
				}
				Manager.UnregisterConnector(this);
			}
			#endif
		}
	}
}