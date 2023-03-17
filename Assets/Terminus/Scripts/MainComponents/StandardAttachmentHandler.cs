using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Terminus 
{
	/// <summary>
	/// <see cref="AttachmentHandler"/> that manages components belonging to <see cref="TerminusObject"/> when it's being placed by <see cref="Placer"/> or similar process.<para>   </para>
	/// Does 3 things:<para>   </para>
	/// 1) Changes main color and blendmode(if applicable) of materials of <see cref="StandardAttachmentHandler.affectedRenderers"/>.<para>   </para>
	/// 2) Changes layers of colliders.<para>   </para>
	/// 3) Changes isKinematic property of rigidbodies.
	/// </summary>
	/// <seealso cref="IntersectChecker"/>
	[DisallowMultipleComponent]
	public class StandardAttachmentHandler : AttachmentHandler {

		/// <summary>
		/// Main color of of materials of <see cref="StandardAttachmentHandler.affectedRenderers"/> when owner <see cref="TerminusObject"/> are placed in valid position.<para>   </para><para>   </para>
		/// Valid position means accepting <see cref="Connector"/> have been found and <see cref="StandardAttachmentHandler.affectedColliders"/> or <see cref="StandardAttachmentHandler.affectedColliders2D"/> aren't intersecting with other colliders.
		/// </summary>
		public Color validColor = new Color(0,1,0,0.33f);
		/// <summary>
		/// Main color of of materials of <see cref="StandardAttachmentHandler.affectedRenderers"/> when owner <see cref="TerminusObject"/> are placed in invalid position.<para>   </para><para>   </para>
		/// Invalind position means accepting <see cref="Connector"/> have not been found.
		/// </summary>
		public Color invalidColor = new Color(1,0,0,0.33f);
		/// <summary>
		/// Main color of of materials of <see cref="StandardAttachmentHandler.affectedRenderers"/> when owner <see cref="TerminusObject"/> are placed in colliding position.<para>   </para><para>   </para>
		/// Colliding position means accepting <see cref="Connector"/> have been found, but <see cref="StandardAttachmentHandler.affectedColliders"/> or <see cref="StandardAttachmentHandler.affectedColliders2D"/> are intersecting with other colliders.
		/// </summary>
		public Color collidingColor = new Color(1,0.7f,0,0.33f);



		[SerializeField]
		protected BlendModeArray[] originalModes;
		[SerializeField]
		protected ColorsArray[] originalColors;
		[SerializeField]
		protected IntersectChecker[] intersectChekers;

		protected Material[][] materials; 


		[System.Serializable]
		protected struct ColorsArray
		{
			public Color[] colors;
		}

		[System.Serializable]
		protected struct BlendModeArray
		{
			public Utils.BlendMode[] modes;
		}

		/// <summary>
		/// Saves Renderer.materials of <see cref="StandardAttachmentHandler.affectedRenderers"/> into internal array. Call if you changed renderers for some reason.
		/// </summary>
		public void CacheMaterials()
		{
			materials = new Material[affectedRenderers.Count][];
			for (int i = 0; i < affectedRenderers.Count; i++)
			{				
				materials[i] = affectedRenderers[i].materials;
			}
		}
				
		/// <summary>
		/// Called automatically by <see cref="Placer"/> when object begins being in attaching mode.<para>   </para>
		/// Call manually if you're using placement method independent of <see cref="Placer"/>.
		/// </summary>
		public override void OnAttachmentStart()
		{
			if (Application.isPlaying && !inAttachingMode)
			{
				inAttachingModeP = true;
				//Save colliders and rigidbodies status, turn rigidbodies to kinematic, turn colliders to triggers, adding intersection checkers
				intersectChekers = new IntersectChecker[affectedColliders.Count + affectedColliders2D.Count];
				for (int i = 0; i < affectedColliders.Count; i++)
				{
					affectedColliders[i].isTrigger = true;
					intersectChekers[i] = affectedColliders[i].gameObject.AddComponent<IntersectChecker>();
					if (overrideColliderLayers)
					{
						affectedColliders[i].gameObject.layer = newLayer;
					}
				}
				for (int i = 0; i < affectedColliders2D.Count; i++)
				{
					affectedColliders2D[i].isTrigger = true;
					intersectChekers[affectedColliders.Count + i] = affectedColliders2D[i].gameObject.AddComponent<IntersectChecker>();
					if (overrideColliderLayers)
					{
						affectedColliders2D[i].gameObject.layer = newLayer;
					}
				}

				for (int i = 0; i < affectedRigidbodies.Count; i++)
				{
					Rigidbody rbody = affectedRigidbodies[i].GetComponent<Rigidbody>();
					if (rbody != null)
						rbody.isKinematic = true;
				}
				for (int i = 0; i < affectedRigidbodies2D.Count; i++)
				{
					Rigidbody2D rbody = affectedRigidbodies2D[i].GetComponent<Rigidbody2D>();
					if (rbody != null)
						rbody.isKinematic = true;
				}

				//Save original blend modes of all materials, then switch them to transparent mode
				originalModes = new BlendModeArray[affectedRenderers.Count];
				originalColors = new ColorsArray[affectedRenderers.Count];

				CacheMaterials();
				for (int i = 0; i < affectedRenderers.Count; i++)
				{
					originalModes[i].modes = new Utils.BlendMode[materials[i].Length];
					originalColors[i].colors = new Color[materials[i].Length];
					for (int x = 0; x < materials[i].Length; x++)
					{						
						originalColors[i].colors[x] = materials[i][x].color;
						if (materials[i][x].HasProperty("_SrcBlend"))
						{
							originalModes[i].modes[x] = (Utils.BlendMode)(materials[i][x].GetInt("_Mode"));
							Utils.SetupMaterialWithBlendMode(materials[i][x],Utils.BlendMode.Transparent);
						}
						else
						{
							originalModes[i].modes[x] = Utils.BlendMode.NotStandardShader;
						}
					}
				}
			}
		}

		/// <summary>
		/// Called automatically by <see cref="Placer"/> when object is attached.<para>   </para>
		/// Call manually if you're using placement method independent of <see cref="Placer"/>.
		/// </summary>
		public override void OnAttached()
		{
			if (Application.isPlaying && inAttachingMode)
			{
				inAttachingModeP = false;
				//Debug.Log(gameObject.name+" exits Attachment");
				for (int i = 0; i < affectedColliders.Count; i++)
				{
					affectedColliders[i].isTrigger = owner.GetSupposedComponentState(affectedColliders[i]).flag;
					if (overrideColliderLayers)			
						affectedColliders[i].gameObject.layer = owner.GetSupposedComponentState(affectedColliders[i]).layer;
				}
				for (int i = 0; i < affectedColliders2D.Count; i++)
				{
					affectedColliders2D[i].isTrigger = owner.GetSupposedComponentState(affectedColliders2D[i]).flag;
					if (overrideColliderLayers)			
						affectedColliders2D[i].gameObject.layer = owner.GetSupposedComponentState(affectedColliders2D[i]).layer;
				}

				for (int i = 0; i < affectedRigidbodies.Count; i++)
				{
					Rigidbody rbody = affectedRigidbodies[i].GetComponent<Rigidbody>();
					if (rbody != null)
						rbody.isKinematic = owner.GetSupposedComponentState(affectedRigidbodies[i]).flag;
		        }
				for (int i = 0; i < affectedRigidbodies2D.Count; i++)
				{
					Rigidbody2D rbody = affectedRigidbodies2D[i].GetComponent<Rigidbody2D>();
					if (rbody != null)
						rbody.isKinematic = owner.GetSupposedComponentState(affectedRigidbodies2D[i]).flag;
				}

				//Restore blend modes of materials
				for (int i = 0; i < affectedRenderers.Count; i++)
				{
					for (int x = 0; x < affectedRenderers[i].materials.Length; x++)
					{
						if (originalModes[i].modes[x] != Utils.BlendMode.NotStandardShader)
							Utils.SetupMaterialWithBlendMode(materials[i][x],originalModes[i].modes[x]);
						materials[i][x].color = originalColors[i].colors[x];
					}
				}

				//Delete intersect checkers
				for (int i = 0; i < intersectChekers.Length; i++)
					Destroy(intersectChekers[i]);
			}
		}
		

		/// <summary>
		/// Called by <see cref="Placer"/> every update during which this object placement is valid, returns true if it still valid after checks done by this component (intersections with other objects).<para>   </para>
		/// Call manually if you're using placement method independent of <see cref="Placer"/>.
		/// </summary>
		public override bool ValidPlacementUpdate()
		{
			if (inAttachingMode)
			{
				if (materials == null)
					CacheMaterials();
				Color col = validColor;
				bool isValid = true;
				for (int i = 0; i < intersectChekers.Length; i++)
				{
					if (intersectChekers[i].Intersects())
					{
						col = collidingColor;
						isValid = false;
						break;
					}
				}

				for (int i = 0; i < affectedRenderers.Count; i++)
				{
					for (int x = 0; x < materials[i].Length; x++)
					{
						materials[i][x].color = col;
					}
				}

				return isValid;
			}
			else
				return false;
		}
		
			
		/// <summary>
		/// Called by <see cref="Placer"/> every update in which this object placement is invalid (<see cref="Placer"/> was unable to find avaliable connector using provided spatial parameters).<para>   </para>
		/// Call manually if you're using placement method independent of <see cref="Placer"/>.
		/// </summary>
		public override void InvalidPlacementUpdate()
		{
			if (materials == null)
				CacheMaterials();
			if (inAttachingMode)
			{
				for (int i = 0; i < affectedRenderers.Count; i++)
				{
					for (int x = 0; x < materials[i].Length; x++)
					{
						materials[i][x].color = invalidColor;
					}
				}
			}
		}
	}
}