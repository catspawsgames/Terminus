using UnityEngine;
using System.Collections;

namespace Terminus 
{
	/// <summary>
	/// Helper component that handles special type of <see cref="TerminusObject"/>s such as struts. Visualizes connection with <see href="http://docs.unity3d.com/ScriptReference/MeshRenderer.html">MeshRenderer</see>.
	/// </summary>
	/// <remarks>
	/// This class requres <see href="http://docs.unity3d.com/ScriptReference/MeshRenderer.html">MeshRenderer</see> and <see href="http://docs.unity3d.com/ScriptReference/MeshFilter.html">MeshFilter</see> components to be present on the same GameObject.
	/// This <see cref="LongConnection"/> type can change mass of parent <see cref="TerminusObject"/> dynamically. It also can create collider for connection.
	/// Long connection TerminusObject is different in its placement procedure since it requres attaching 2 <see cref="TerminusObject.connectors"/> instead of one.
	/// It also modifies itself depending on positions of its connectors.
	/// </remarks>
	/// <seealso cref="LongConnectionLRend"/>
	/// <seealso cref="LongConnection"/>
	public class LongConnectionMesh : LongConnection {

		/// <summary>
		/// Should mass of <see cref="TerminusObject"/>s rigidbody be recalculated based on connection length?
		/// </summary>
		/// <seealso cref="LongConnectionMesh.massPerLengthUnit"/>
		public bool recalculateMass;
		/// <summary>
		/// Mass of <see cref="TerminusObject"/>s rigidbody per Unity worldspace length unit.
		/// </summary>
		/// <seealso cref="LongConnectionMesh.recalculateMass"/>
		public float massPerLengthUnit = 1;

		/// <summary>
		/// 2D cross-section for connection mesh generation.
		/// </summary>
		/// <seealso cref="LongConnectionMesh.smoothNormals"/>
		public Vector2[] crossSection;
		/// <summary>
		/// Should connection mesh normals be smoothed or not?
		/// </summary>
		/// /// <seealso cref="LongConnectionMesh.crossSection"/>
		public bool smoothNormals;
		/// <summary>
		/// If relative positions of <see cref="Connector"/>s belonging to owner <see cref="TerminusObject"/> change more than this value and <see cref="LongConnection.realtimeRecalculate"/> is true, mesh will be regenerated and all parameters will be recalculated.
		/// </summary>
		public float recalculateSensitivity = 0.0001f;

		/// <summary>
		/// <see href="http://docs.unity3d.com/ScriptReference/MeshFilter.html">MeshFilter</see> for rendering connection mesh.
		/// </summary>
		public MeshFilter meshFilter;
		/// <summary>
		/// <see href="http://docs.unity3d.com/ScriptReference/MeshRenderer.html">MeshRenderer</see> for rendering connection mesh.
		/// </summary>
		public MeshRenderer meshRenderer;
		/// <summary>
		/// Material that will be assigned to <see href="http://docs.unity3d.com/ScriptReference/MeshRenderer.html">MeshRenderer</see>.
		/// </summary>
		public Material material;
		/// <summary>
		/// Should this component change tiling of <see cref="LongConnectionMesh.material"/>?
		/// </summary>
		/// <seealso cref="LongConnectionLRend.tilingLength"/>
		public bool tiling;
		/// <summary>
		/// Length of texture tile in Unity worldspace units.
		/// </summary>
		/// <seealso cref="LongConnectionMesh.tiling"/>
		public float tilingLength = 1;
		
		protected Mesh mesh;

		/// <summary>
		/// Type of connection collider used.
		/// </summary>
		public ColliderTypes colliderType;
		/// <summary>
		/// Collider if <see cref="LongConnectionMesh.colliderType"/> == <see cref="LongConnectionMesh.ColliderTypes.box"/> and <see cref="LongConnection.use2D"/> is false.
		/// </summary>
		public BoxCollider boxCollider;
		/// <summary>
		/// Size of cross-section of <see cref="LongConnectionMesh.boxCollider"/>.
		/// </summary>
		public Vector2 boxSize;
		/// <summary>
		/// Collider if <see cref="LongConnectionMesh.colliderType"/> == <see cref="LongConnectionMesh.ColliderTypes.capsule"/> and <see cref="LongConnection.use2D"/> is false.
		/// </summary>
		public CapsuleCollider capsuleCollider;
		/// <summary>
		/// Collider if <see cref="LongConnectionMesh.colliderType"/> == <see cref="LongConnectionMesh.ColliderTypes.box"/> and <see cref="LongConnection.use2D"/> is true.
		/// </summary>
		public BoxCollider2D boxCollider2D;
		/// <summary>
		/// Radius of <see cref="LongConnectionMesh.capsuleCollider"/> or width of <see cref="LongConnectionMesh.boxCollider2D"/>
		/// </summary>
		public float colliderSize;
		/// <summary>
		/// Margins of connection collider from <see cref="Connector"/>s positions.
		/// </summary>
		public float colliderMargin;


		protected Rigidbody rbody;
		protected Rigidbody2D rbody2D;

		protected Vector3[] vertices;
		protected Vector2[] uvs;
		protected int[] triangles;

		protected Vector3 oldPos1;
		protected Vector3 oldDiff;

		protected bool meshGenFlag = false;

		/// <summary>
		/// Types of possible connection colliders used by <see cref="LongConnectionMesh"/>.
		/// </summary>
		public enum ColliderTypes
		{
			none = 0,
			box = 1,
			capsule = 2
		}

		public override void LongConnectionAfterAttachment(AttachmentInfo attInfo)
		{
			base.LongConnectionAfterAttachment(attInfo);
			if (tiling)
			{
				float tilingX = meshRenderer.material.GetTextureScale("_MainTex").x;
				meshRenderer.material.SetTextureScale("_MainTex",new Vector2(tilingX,Vector3.Distance(owner.connectors[0].transform.TransformPoint(offset1),owner.connectors[1].transform.TransformPoint(offset2))/tilingLength));
			}
		}

		protected override void Awake()
		{
			base.Awake();

			meshRenderer.material = material;

			if (mesh == null)
				mesh = new Mesh();

			if (smoothNormals)
			{
				vertices = new Vector3[crossSection.Length * 2];
				uvs = new Vector2[crossSection.Length * 2];
			}
			else
			{
				vertices = new Vector3[crossSection.Length * 4];
				uvs = new Vector2[crossSection.Length * 4];
			}
			triangles = new int[crossSection.Length * 6];

			if (use2D)
			{
				rbody2D = GetComponent<Rigidbody2D>();
				if (rbody2D == null)
				{
					Debug.LogWarning(gameObject.name + ": LongConnection recalculate mass set to True while object doesn't have Rigidbody2D, disabling mass recalculation");
					recalculateMass = false;
				}
			}
			else
			{
				rbody = GetComponent<Rigidbody>();
				if (rbody == null)
				{
					Debug.LogWarning(gameObject.name + ": LongConnection recalculate mass set to True while object doesn't have Rigidbody, disabling mass recalculation");
					recalculateMass = false;
				}
			}
		}

		protected override void Start ()
		{
			base.Start();
			meshFilter.mesh = mesh;
		}
			
		public override void Recalculate()
		{
			Vector3 pos1 = owner.transform.InverseTransformPoint(owner.connectors[0].transform.TransformPoint(offset1));
			Vector3 pos2 = owner.transform.InverseTransformPoint(owner.connectors[1].transform.TransformPoint(offset2));
			Vector3 diff = pos2 - pos1;
			if (recalculateSensitivity == 0 || (oldPos1-pos1).sqrMagnitude > recalculateSensitivity || (oldDiff-diff).sqrMagnitude > recalculateSensitivity)
			{
				oldPos1 = pos1;
				oldDiff = diff;
				mesh.Clear();
				Quaternion rot = Quaternion.LookRotation(diff,Quaternion.Inverse(owner.transform.rotation) * owner.connectors[0].globalRotation * Vector3.forward);
				
				if (smoothNormals)
				{
					for (int i = 0; i < crossSection.Length; i++)
					{
						vertices[i] = pos1 + rot * Vector3.up * crossSection[i].y + rot * Vector3.left * crossSection[i].x;
						vertices[crossSection.Length + i] = vertices[i] + diff;
						uvs[i] = new Vector2(((float)i)/crossSection.Length,0);
						uvs[crossSection.Length + i] = new Vector2(((float)i)/crossSection.Length,1);
					}
					for (int i = 0; i < crossSection.Length-1; i++)
					{
						triangles[i*6] = i;
						triangles[i*6+1] = i+1;
						triangles[i*6+2] = crossSection.Length + i;
						triangles[i*6+3] = crossSection.Length + i + 1;
						triangles[i*6+4] = crossSection.Length + i;
						triangles[i*6+5] = i+1;
					}
					triangles[(crossSection.Length-1)*6] = (crossSection.Length-1);
					triangles[(crossSection.Length-1)*6+1] = 0;
					triangles[(crossSection.Length-1)*6+2] = crossSection.Length;
					triangles[(crossSection.Length-1)*6+3] = (crossSection.Length-1);
					triangles[(crossSection.Length-1)*6+4] = crossSection.Length;
					triangles[(crossSection.Length-1)*6+5] = crossSection.Length *2 - 1;
				}
				else
				{
					for (int i = 0; i < crossSection.Length; i++)
					{
						vertices[i*crossSection.Length] = vertices[i*crossSection.Length+2] = pos1 + rot * Vector3.up * crossSection[i].y + rot * Vector3.left * crossSection[i].x;					 
						vertices[i*crossSection.Length+1] = vertices[i*crossSection.Length+3] = vertices[i*crossSection.Length] + diff;
						uvs[i*crossSection.Length] = uvs[i*crossSection.Length+2] = new Vector2(((float)i)/crossSection.Length,0);
						uvs[i*crossSection.Length+1] = uvs[i*crossSection.Length+3] = new Vector2(((float)i)/crossSection.Length,1);
					}
					for (int i = 0; i < crossSection.Length-1; i++)
					{
						triangles[i*6] = i * crossSection.Length + 4;
						triangles[i*6+1] = i * crossSection.Length + 3;
						triangles[i*6+2] = i * crossSection.Length + 2;
						triangles[i*6+3] = i * crossSection.Length + 5;
						triangles[i*6+4] = i * crossSection.Length + 3;
						triangles[i*6+5] = i * crossSection.Length + 4;
					}
					triangles[(crossSection.Length-1)*6] = 0;
					triangles[(crossSection.Length-1)*6+1] = (vertices.Length-1);
					triangles[(crossSection.Length-1)*6+2] = (vertices.Length-2);
					triangles[(crossSection.Length-1)*6+3] = 1;
					triangles[(crossSection.Length-1)*6+4] = (vertices.Length-1);
					triangles[(crossSection.Length-1)*6+5] = 0;
				}
				
				mesh.vertices = vertices;
				mesh.uv = uvs;
				mesh.triangles = triangles;
				mesh.RecalculateNormals();

				if (colliderType != ColliderTypes.none)
				{
					if (use2D)
					{
						if (colliderType == ColliderTypes.box && boxCollider2D != null)
						{
							boxCollider2D.transform.localPosition = pos1 + diff/2;
							boxCollider2D.transform.localRotation = rot;
							boxCollider2D.size = new Vector2(colliderSize,diff.magnitude - colliderMargin);
						}
					}
					else
					{
						if (colliderType == ColliderTypes.box && boxCollider != null)
						{
							boxCollider.transform.localPosition = pos1 + diff/2;
							boxCollider.transform.localRotation = rot;
							boxCollider.size = new Vector3(boxSize.x,boxSize.y,diff.magnitude-colliderMargin);
						}
						else if (colliderType == ColliderTypes.capsule && capsuleCollider != null)
						{
							capsuleCollider.transform.localPosition = pos1 + diff/2;
							capsuleCollider.transform.localRotation = rot;
							capsuleCollider.radius = colliderSize;
							capsuleCollider.height = diff.magnitude-colliderMargin;
						}
					}
				}

				if (recalculateMass)
				{
					rbody.mass = diff.magnitude * massPerLengthUnit;
				}
			}
		}


		// Update is called once per frame
		protected void Update () 
		{
			if (realtimeRecalculate)
			{
				Recalculate();
			}
		}
	}
}