using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Terminus.Demo3
{
	/// <summary>
	/// Controls vehicle when players enters it. Should be placed on cockpit gameObject. Works by communicating with <see cref="VehicleControllableObject"/>s based on input by player.
	/// </summary>
	public class VehicleController : InteractableObject {

		public Transform cameraTransform;
		public Transform playerAnchor;
		public Vector3 playerExitPoint;
		public LayerMask cameraObstructionRaycastLayers;
		public Transform cameraRaycastTarget;
		public AnimationCurve cameraTransitionCurve;
		public float cameraTransitionTime = 0.5f;
		public ControlsMapping[] controlsMapping;
		public List<VehicleControllableObject> controllableObjects;
		public List<VehicleControls> activeControls;

		protected float cameraTransitionCurrentTime = 0;
		protected GameObject playerInside;
		protected bool inCameraTransition = false;
		protected TerminusObject termObj;

		[System.Serializable]
		public class ControlsMapping
		{
			public VehicleControls control;
			public KeyCode key;
			public KeyCode altKey;
		}

		public bool GetControlState(VehicleControls control)
		{
			return activeControls.Contains(control);
		}

		public void EnterVehicle(GameObject player)
		{
			playerInside = player;
			player.SetActive(false);
			player.transform.parent = playerAnchor;
			player.transform.localPosition = Vector3.zero;
			player.transform.localRotation = Quaternion.identity;
			this.enabled = true;
		}

		public void ExitVehicle()
		{
			playerInside.transform.parent = null;
			playerInside.transform.position = transform.position + transform.rotation * playerExitPoint;
			playerInside.transform.rotation = Quaternion.Euler(0,transform.rotation.eulerAngles.y + 180,0);
			playerInside.SetActive(true);
			if (termObj.mainOrWeldedRigidbody != null)
				playerInside.GetComponent<Rigidbody>().velocity = termObj.mainOrWeldedRigidbody.velocity;
			playerInside = null;
			this.enabled = false;
		}


		public override string[] GetPossibleInteractions ()
		{
			return new string[1] {"Enter vehicle"};		
		}

		public override void Interact (int interactionIndex, PlayerActionController player)
		{
			if (interactionIndex == 0)
			{
				EnterVehicle(player.gameObject);
			}
		}
			

		void Update ()
		{
			for (int i = 0; i < controlsMapping.Length; i++)
			{				
				if (Input.GetKeyDown(controlsMapping[i].key) || Input.GetKeyDown(controlsMapping[i].altKey))
				{
					if (controlsMapping[i].control == VehicleControls.exit)
					{
						ExitVehicle();
						return;
					}
					if (!activeControls.Contains(controlsMapping[i].control))
						activeControls.Add(controlsMapping[i].control);
				}
				if (Input.GetKeyUp(controlsMapping[i].key) || Input.GetKeyUp(controlsMapping[i].altKey))
				{
					if (activeControls.Contains(controlsMapping[i].control))
						activeControls.Remove(controlsMapping[i].control);
				}
			}
			for (int i = 0; i < controllableObjects.Count; i++)
				controllableObjects[i].InputChanged();

			Vector3 desiredCamPos;
			Quaternion desiredCamRot;
			if (inCameraTransition)
			{
				desiredCamPos = Vector3.Lerp(Camera.main.transform.position, cameraTransform.position, cameraTransitionCurve.Evaluate(cameraTransitionCurrentTime/cameraTransitionTime));
				desiredCamRot = Quaternion.Lerp(Camera.main.transform.rotation, cameraTransform.rotation, cameraTransitionCurve.Evaluate(cameraTransitionCurrentTime/cameraTransitionTime));
				cameraTransitionCurrentTime -= Time.deltaTime;
				if (cameraTransitionCurrentTime <= 0)
					inCameraTransition = false;
			}
			else
			{
				desiredCamPos = cameraTransform.position;
				desiredCamRot = cameraTransform.rotation;
			}
			RaycastHit hit;
			if (Physics.Raycast(desiredCamPos,(cameraRaycastTarget.position - desiredCamPos),out hit,(cameraRaycastTarget.position - desiredCamPos).magnitude,cameraObstructionRaycastLayers))
				desiredCamPos = hit.point;
			Camera.main.transform.position = desiredCamPos;
			Camera.main.transform.rotation = desiredCamRot;
		}

		void OnEnable ()
		{
			inCameraTransition = true;
			cameraTransitionCurrentTime = cameraTransitionTime;
		}

		void Awake ()
		{
			termObj = GetComponent<TerminusObject>();
		}
	}
}