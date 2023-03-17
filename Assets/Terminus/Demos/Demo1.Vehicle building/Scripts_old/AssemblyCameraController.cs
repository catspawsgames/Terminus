using UnityEngine;
using System.Collections;

namespace Terminus.Demo1
{
	/// <summary>
	/// Camera controller for Terminus Demo1.
	/// </summary>
	public class AssemblyCameraController : MonoBehaviour {

		/// <summary>
		/// Camera follows this object.
		/// </summary>
		public Transform focus;
		/// <summary>
		/// With follow enabled, will force camera to repeat movement of this object.
		/// </summary>
		public TerminusObject target;
		/// <summary>
		/// If true, follows target.
		/// </summary>
		public bool follow;
		public float minDistance = 1;
		public float maxDistance = 15;
		public float MaxLat = 70;
		public float MinLat = 5;
		public float defaultYLevel = 1;
		public float WheelSensitivity = 1;
		public float MouseSensitivity = -5;
		public float MouseMoveSensitivity = -1;
		public float Long = 0;
		public float Lat = 0;
		public float distance = 7;
		public float MoveSpeed = 10;
		public bool currentlyAutomoving;
		public Vector3 automoveTarget;
		public AnimationCurve automoveCurve;

		protected float MoveX,MoveZ,IndDeltaTime,OldTimeSinceStartup;
		protected float autoTotalTime;
		protected float autoCurrentTime;
		protected Vector3 autoOrigPos;
		

		protected void RefreshCamera() {
			gameObject.transform.position = new Vector3
				(focus.position.x + (Mathf.Cos (Long * Mathf.PI / 180) * Mathf.Cos (Lat * Mathf.PI / 180) * (distance)),
				 focus.position.y + (Mathf.Sin (Lat * Mathf.PI / 180) * (distance)),
				 focus.position.z + (Mathf.Sin (Long * Mathf.PI / 180) * Mathf.Cos (Lat * Mathf.PI / 180) * (distance)));
			//Looking at focus
			gameObject.transform.LookAt(focus.position);
		}
		
		
		protected void MoveFocusPoint(float dX,float dZ)
		{
			focus.position = new Vector3(focus.transform.position.x + (Mathf.Cos ((Long+180) * Mathf.PI / 180)) * dX + (float)(Mathf.Sin (-Long * Mathf.PI / 180))*dZ ,
			                                       focus.transform.position.y,
			                                       focus.transform.position.z + (Mathf.Sin ((Long+180) * Mathf.PI / 180)) * dX + (float)(Mathf.Cos (-Long * Mathf.PI / 180))*dZ);
		}
		
		public void FocusOnPoint(Vector3 point, float time = 0.25f)
		{
			currentlyAutomoving = true;
			autoCurrentTime = 0;
			autoTotalTime = time;
			automoveTarget = point;
			autoOrigPos = focus.transform.position;
		}
		
		
		// Use this for initialization
		void Start () {
			
		}
		
		// Update is called once per frame
		void LateUpdate ()
		{
			//focus point movement
			if (!follow)
			{
				MoveX = 0;
				MoveZ = 0;
				if (Input.GetKey(KeyCode.UpArrow))
				{
					MoveX = MoveX + MoveSpeed * Time.unscaledDeltaTime;
				}
				if (Input.GetKey(KeyCode.DownArrow))
				{	
					MoveX = MoveX - MoveSpeed * Time.unscaledDeltaTime;
				}
				if (Input.GetKey(KeyCode.LeftArrow))
				{	
					MoveZ = MoveZ - MoveSpeed * Time.unscaledDeltaTime;
				}
				if (Input.GetKey(KeyCode.RightArrow))
				{	
					MoveZ = MoveZ + MoveSpeed * Time.unscaledDeltaTime;
				}
				if (Input.GetMouseButton(2))
				{
					MoveX = MoveX + Input.GetAxis("Mouse Y") * MouseMoveSensitivity;
					MoveZ = MoveZ + Input.GetAxis("Mouse X") * MouseMoveSensitivity;
				}
			}
			else
			{
				if (!currentlyAutomoving)
					focus.position = target.realPosition;
				else
					automoveTarget = target.realPosition;
			}
			
			if (currentlyAutomoving)
			{
				autoCurrentTime += Time.unscaledDeltaTime;
				focus.transform.position = Vector3.Lerp(autoOrigPos,automoveTarget,Mathf.Clamp01(automoveCurve.Evaluate(autoCurrentTime/autoTotalTime)));
				if (autoCurrentTime >= autoTotalTime)
					currentlyAutomoving = false;
			}
			else
				MoveFocusPoint(MoveX,MoveZ);
			
			
			
			//Zoom in and out with mouse wheel
			if (Input.GetAxis("Mouse ScrollWheel") != 0) {
				//Debug.Log(Input.GetAxis("Mouse ScrollWheel"));
				distance = distance * (1 + WheelSensitivity * Input.GetAxis("Mouse ScrollWheel"));
				if (distance < minDistance) {
					distance = minDistance;
				}
				if (distance > maxDistance) {
					distance = maxDistance;
				}					
				
			}
			

			if (Input.GetMouseButton(1) && (Input.GetAxis("Mouse Y") !=0)) {
				Lat = Lat + Input.GetAxis("Mouse Y") * MouseSensitivity;
				if (Lat > MaxLat) {
					Lat = MaxLat;
				}
				if (Lat < MinLat) {
					Lat = MinLat;
				}
			}
			if (Input.GetMouseButton(1) && (Input.GetAxis("Mouse X") !=0)) {
				Long = Long + Input.GetAxis("Mouse X") * MouseSensitivity;
				if (Long > 360) {
					Long = Long - 360;
				}
				if (Long < 0) {
					Long = Long + 360;
				}
			}
			RefreshCamera();
		}
		
	}
}