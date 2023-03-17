﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Terminus 
{
	/// <summary>
	/// Surface <see cref="Connector"/>. Can accept multiple attachments but can not be attached itself. Surfaces are defined by colliders they contain.
	/// </summary>
	[AddComponentMenu("Terminus modular system/Surface")]
	public class Surface : Connector {

		/// <summary>
		/// List of colliders that define shape of <see cref="Surface"/> .
		/// </summary>
		/// <seealso cref="Settings.LayerPortOptions.use2DPhysics"/>
		public List<Collider> colliders = new List<Collider>();
		/// <summary>
		/// List of 2D colliders that define shape of <see cref="Surface"/>.
		/// </summary>
		/// <seealso cref="Settings.LayerPortOptions.use2DPhysics"/>
		public List<Collider2D> colliders2D = new List<Collider2D>();
		/// <summary>
		/// Type of symmetry that is used by this surface. Used for symmetrical attachment of multiple <see cref="TerminusObject"/>s at the same time.
		/// </summary>
		/// <seealso cref="Connector.AchievableSymmetryCount"/>
		/// <seealso cref="Connector.GetSymmetryPositions"/>
		public SymmetryTypes symmetryType;
		/// <summary>
		/// Center of symmetry if <see cref="Surface.symmetryType"/> isn't <see cref="Surface.SymmetryTypes.none"/> .
		/// </summary>
		public Vector3 symmetryPoint;
		/// <summary>
		/// Direction of symmetry line if <see cref="Surface.symmetryType"/>  is <see cref="Surface.SymmetryTypes.linear"/> .
		/// </summary>
		public Vector3 symmetryVector = Vector3.up;
		/// <summary>
		/// Rotation of normal of symmetry plane if <see cref="Surface.symmetryType"/>  is <see cref="Surface.SymmetryTypes.planar"/> .
		/// </summary>
		public Quaternion symmetryRotation;
		/// <summary>
		/// Limit of <see cref="Port"/>s that can be attached to this <see cref="Surface"/>.
		/// </summary>
		public int maxAttachedPorts;

		[SerializeField]
		protected List<AttachmentInfo> attachmentsInfoP = new List<AttachmentInfo>();

		/// <summary>
		/// Maximum simultaneous symmetry attachment points that can be generated by this surface.
		/// </summary>
		public int maxSymmetryObjects
		{
			get
			{
				return maxSymmetryObjectsP;
			}
			set
			{
				maxSymmetryObjectsP = value;
			}
		}

		[SerializeField]
		protected int maxSymmetryObjectsP = 15;


		public override List<AttachmentInfo> attachmentsInfo
		{
			get
			{
				return attachmentsInfoP;
			}
		}


		public override bool isOccupied
		{
			get
			{
				return maxAttachedPorts > 0 && attachmentsInfo.Count > maxAttachedPorts;
			}
		}

		public override AttachmentInfo GetAttachmentInfo(Connector connector)
		{
			return attachmentsInfoP.Find(rec => rec.otherConnector == connector);
		}

		/// <summary>
		/// Types of symmetry that can be achieved by <see cref="Surface"/>.
		/// </summary>
		public enum SymmetryTypes
		{
			/// <summary>
			/// <see cref="Surface"/> isn't symmetrical
			/// </summary>
			none = 0,
			/// <summary>
			/// <see cref="Surface"/> have spherical symmetry around <see cref="Surface.symmetryPoint"/> 
			/// </summary>
			point = 1,
			/// <summary>
			/// <see cref="Surface"/> have linear symmetry with line going through <see cref="Surface.symmetryPoint"/> in direction of <see cref="Surface.symmetryVector"/> 
			/// </summary>
			linear = 2,
			/// <summary>
			/// <see cref="Surface"/> have planar symmetry with plane intersecting <see cref="Surface.symmetryPoint"/> and with normal oriented by <see cref="Surface.symmetryRotation"/> 
			/// </summary>
			planar = 3
		}


		public override void RegisterAttachmentFromConnector(AttachmentInfo attachInfo)
		{
			base.RegisterAttachmentFromConnector(attachInfo);
			attachmentsInfoP.Add(attachInfo);
		}


		public override void RegisterDetachmentFromConnector(Connector conn)
		{
			base.RegisterDetachmentFromConnector(conn);
			attachmentsInfoP.RemoveAll(rec => rec.otherConnector == conn);
		}
				

		protected List<PositionInfo> symmPositions_internal = new List<PositionInfo>();
		public override List<PositionInfo> GetSymmetryPositions(int positionCount, Vector3 originalPosition, Quaternion originalRotation, Component collider = null)
		{
			if (positionCount == 0)
			{
				if (!(symmPositions_internal.Count == 0))
					symmPositions_internal.Clear();
				return symmPositions_internal;
			}

			if (positionCount == owner.symmetricSiblings.Count)
			{
				int colliderIndex = -1;
				if (portOptions.use2DPhysics)			
					colliderIndex = colliders2D.FindIndex(rec => rec == collider);
				else
					colliderIndex = colliders.FindIndex(rec => rec == collider);

				if (colliderIndex == -1)
				{
					Debug.LogWarning(": Couldn't find provided collider for symmetry position determination.");

					if (!(symmPositions_internal.Count == 0))
						symmPositions_internal.Clear();
					return symmPositions_internal;
				}

				Vector3 diffPos = originalPosition - globalPosition;
				Quaternion diffRot = Quaternion.Inverse(globalRotation) * originalRotation;
				//PositionInfo[] result = new PositionInfo[positionCount];

				if (symmPositions_internal.Count != positionCount)
					symmPositions_internal.Clear();
				for (int i = 0; i < positionCount; i++)
				{
					Connector conn = owner.symmetricSiblings[i].connectors[index];
					Component coll = null;
					if (portOptions.use2DPhysics)
						coll = ((Surface)conn).colliders2D[colliderIndex];
					else		
						coll = ((Surface)conn).colliders[colliderIndex];
					symmPositions_internal.Add(new PositionInfo(conn.globalPosition + conn.globalRotation * Quaternion.Inverse(globalRotation) * diffPos,
						conn.globalRotation * diffRot,
						conn,
						coll));

					/*
					result[i].connector = owner.symmetricSiblings[i].connectors[index];
					Vector3 curDiffPos = result[i].connector.globalRotation * Quaternion.Inverse(globalRotation) * diffPos;
					result[i].position = result[i].connector.globalPosition + curDiffPos;
					result[i].rotation = result[i].connector.globalRotation * diffRot;
					if (portOptions.use2DPhysics)
						result[i].collider = ((Surface)result[i].connector).colliders2D[colliderIndex];
					else
						result[i].collider = ((Surface)result[i].connector).colliders[colliderIndex];
					*/
				}
				return symmPositions_internal;
			}
			else
			{
				if (symmetryType != SymmetryTypes.none)
				{
					positionCount = Mathf.Clamp(positionCount,0,maxSymmetryObjects);
					//PositionInfo[] result = new PositionInfo[positionCount];
					switch (symmetryType)
					{
					case SymmetryTypes.point:
						if (symmPositions_internal.Count != positionCount)
							symmPositions_internal.Clear();
						return symmPositions_internal;

					case SymmetryTypes.linear:
						float step = 360.0f/(positionCount+1);
						Vector3 axisStart = transform.TransformPoint(symmetryPoint);
						Vector3 axisDir = transform.rotation * symmetryVector;
						Vector3 proj = Vector3.Project((originalPosition-axisStart),axisDir) + axisStart;
						Vector3 diff = originalPosition - proj;

						if (symmPositions_internal.Count != positionCount)
							symmPositions_internal.Clear();
						for (int i = 0; i < positionCount; i++)
						{
							float angle = step * (i+1);
							Quaternion rot = Quaternion.AngleAxis(angle,axisDir);

							symmPositions_internal.Add(new PositionInfo(proj + rot * diff,
								rot * originalRotation,
								this,
								collider));
							/*
							result[i].position = proj + rot * diff;
							result[i].rotation = rot * originalRotation;
							result[i].connector = this;
							result[i].collider = collider;
							*/
							//result[i] = Utils.RotateAroundAxis(position,rotation,transform.TransformPoint(symmetryPoint),transform.rotation * symmetryVector, step * (i+1));				
						}
						return symmPositions_internal;

					default:
						if (symmPositions_internal.Count != positionCount)
							symmPositions_internal.Clear();
						return symmPositions_internal;
					}
				}
				else
				{
					if (symmPositions_internal.Count != positionCount)
						symmPositions_internal.Clear();
					return symmPositions_internal;
				}
			}
		}

		public override int AchievableSymmetryCount(int desiredCount)
		{
			if (owner.symmetricSiblings == null || owner.symmetricSiblings.Count == 0 || desiredCount != owner.symmetricSiblings.Count)
			{
				switch (symmetryType)
				{
				case SymmetryTypes.none:
					return 0;
				case SymmetryTypes.point:
					return Mathf.Clamp(desiredCount,0,maxSymmetryObjectsP);
				case SymmetryTypes.linear:
					return Mathf.Clamp(desiredCount,0,maxSymmetryObjectsP);
				case SymmetryTypes.planar:
					return 1;			
				}
			}
			return owner.symmetricSiblings.Count;
		}

	}
}