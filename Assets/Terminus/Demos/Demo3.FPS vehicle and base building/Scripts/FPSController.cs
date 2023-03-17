using UnityEngine;
using System.Collections;


namespace Terminus.Demo3
{
	public class FPSController : MonoBehaviour {

		public Transform cameraTransform;
		public AnimationCurve cameraTransitionCurve;
		public float cameraTransitionTime = 0.5f;
		public KeyCode forward = KeyCode.W;
		public KeyCode back = KeyCode.S;
		public KeyCode left = KeyCode.A;
		public KeyCode right = KeyCode.D;
		public KeyCode jump = KeyCode.Space;

		public Vector2 mouseSensitivity = Vector2.one;
		public AnimationCurve mouseSensitivityCurve;
		public float verticalViewLimit;


		protected SimpleFPSPhysMotor motor;
		protected float cameraTransitionCurrentTime = 0;
		protected bool inCameraTransition = false;


		void Awake () 
		{
			motor = GetComponent<SimpleFPSPhysMotor>();
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;
		}


		
		// Update is called once per frame
		void Update () 
		{
			Vector3 input = Vector3.zero;
			if (Input.GetKey(forward))
				input += Vector3.forward;
			if (Input.GetKey(back))
				input += Vector3.back;
			if (Input.GetKey(left))
				input += Vector3.left;
			if (Input.GetKey(right))
				input += Vector3.right;

			input = cameraTransform.rotation * input;

			motor.moveVector = input.normalized;

			if (Input.GetKey(jump))
				motor.Jump();

			float vertAngle = cameraTransform.rotation.eulerAngles.x;
			float horizAngle = cameraTransform.rotation.eulerAngles.y;
			float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity.x;
			float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity.y;

			mouseX *= mouseSensitivityCurve.Evaluate(Mathf.Abs(mouseX) / Time.deltaTime / 400);
			mouseY *= mouseSensitivityCurve.Evaluate(Mathf.Abs(mouseY) / Time.deltaTime / 400);

			vertAngle = vertAngle - mouseY;
			if (vertAngle > 180)
				vertAngle -= 360;

			if (vertAngle > verticalViewLimit)
				vertAngle = verticalViewLimit;
			if (vertAngle < -verticalViewLimit)
				vertAngle = -verticalViewLimit;
			horizAngle += mouseX;

			cameraTransform.rotation = Quaternion.Euler(vertAngle,horizAngle,0);

			if (inCameraTransition)
			{
				Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, cameraTransform.position, cameraTransitionCurve.Evaluate(cameraTransitionCurrentTime/cameraTransitionTime));
				Camera.main.transform.rotation = Quaternion.Lerp(Camera.main.transform.rotation, cameraTransform.rotation, cameraTransitionCurve.Evaluate(cameraTransitionCurrentTime/cameraTransitionTime));
				cameraTransitionCurrentTime -= Time.deltaTime;
				if (cameraTransitionCurrentTime <= 0)
					inCameraTransition = false;
			}
			else
			{
				Camera.main.transform.position = cameraTransform.position;
				Camera.main.transform.rotation = cameraTransform.rotation;
			}
		}
			
		void OnEnable ()
		{
			inCameraTransition = true;
			cameraTransitionCurrentTime = cameraTransitionTime;
		}
	}
}