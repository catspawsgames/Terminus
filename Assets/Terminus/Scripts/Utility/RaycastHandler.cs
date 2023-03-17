using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

namespace Terminus 
{
	/// <summary>
	/// Component for providing user input data to <see cref="Placer"/>.
	/// Works in conjunction with <see cref="Placer"/> to create <see href="http://www.besiege.spiderlinggames.co.uk/">Besiege-</see> or <see href="https://kerbalspaceprogram.com">Kerbal Space Program</see>-like construction process.
	/// Raycasting is done from main camera position by ScreenPointToRay.
	/// </summary>
	public class RaycastHandler : MonoBehaviour {

		/// <summary>
		/// Unity Layers to raycast for possible attachment points.
		/// </summary>
		public LayerMask activeRaycastLayers;
		/// <summary>
		/// Raycasting distance to raycast for possible attachment points.
		/// </summary>
		public float distance = 100;
		/// <summary>
		/// If raycast not hit anything, this toggle makes it raycast downward.
		/// </summary>
		public bool FPSMode = false;
		/// <summary>
		/// Direction of additional raycast for FPSMode.
		/// </summary>
		public Vector3 FPSModeVector = Vector3.down;
		/// <summary>
		/// Distance of additional raycast for FPSMode.
		/// </summary>
		public float FPSModeDistance = 4;
		public bool activeUpdate;
		public bool activeOnMouseClick = true;
		public bool useCursorForAim = true;

		protected RaycastHit hit;
		protected Placer placer;
		
		protected void Awake () 
		{
			placer = GetComponent<Placer>();	
		}


		// Update is called once per frame
		protected void Update () 
		{
			if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
			{
				if (placer.activeObject != null)
				{
					Ray ray;
					if (useCursorForAim)
						ray = Camera.main.ScreenPointToRay(Input.mousePosition);
					else
						ray = new Ray(transform.position, transform.forward);

					if (Physics.Raycast(ray,out hit,distance,activeRaycastLayers))
					{
						placer.ExecutePlacingUpdate(activeUpdate || (activeOnMouseClick && Input.GetMouseButton(0)),hit.point,hit.normal,hit.transform.gameObject,hit.collider);
					}
					else if (FPSMode)
					{
						Vector3 castPoint = ray.origin + ray.direction * distance;
						if (Physics.Raycast(castPoint,FPSModeVector,out hit,FPSModeDistance,activeRaycastLayers))
						{
							placer.ExecutePlacingUpdate(activeUpdate || (activeOnMouseClick && Input.GetMouseButton(0)),hit.point,hit.normal,hit.transform.gameObject,hit.collider);
						}
						else
						{
							placer.ExecutePlacingUpdate(activeUpdate || (activeOnMouseClick && Input.GetMouseButton(0)),ray.origin + ray.direction.normalized * distance + FPSModeVector.normalized * FPSModeDistance,-FPSModeVector);
						}
					}
					else
					{
						placer.ExecutePlacingUpdate(activeUpdate || (activeOnMouseClick && Input.GetMouseButton(0)),ray.origin + ray.direction.normalized * distance,-ray.direction);
					}
				}
				else
				{
					if (activeUpdate || (activeOnMouseClick && Input.GetMouseButton(0)))
					{
						Ray ray;
						if (useCursorForAim)
							ray = Camera.main.ScreenPointToRay(Input.mousePosition);
						else
							ray = new Ray(transform.position, transform.forward);
	                    
						if (Physics.Raycast(ray,out hit,distance,activeRaycastLayers))	
							placer.ExecuteEmptyBehaviour(hit.point,hit.normal,hit.collider.transform.gameObject,hit.collider);
	                }
	            }
	        }
	    }
	}
}