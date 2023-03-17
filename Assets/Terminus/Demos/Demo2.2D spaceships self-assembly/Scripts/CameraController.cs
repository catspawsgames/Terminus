using UnityEngine;
using System.Collections;

namespace Terminus.Demo2
{
	namespace Terminus.Demo2
	{
		public class CameraController : MonoBehaviour {

			public Ship playerShip;
			public Vector2 sizeLimits;
			public float sensitivity = 1;

			protected Camera cam;

			void Awake ()
			{
				cam = GetComponent<Camera>();
			}
			
			// Update is called once per frame
			void LateUpdate () 
			{
				if (playerShip != null)
				{
					transform.position = new Vector3(playerShip.transform.position.x,
					                                 playerShip.transform.position.y,
					                                 transform.position.z);
				}
				if (Input.GetAxis("Mouse ScrollWheel") != 0)
					cam.orthographicSize = Mathf.Clamp(cam.orthographicSize + Input.GetAxis("Mouse ScrollWheel") * sensitivity, sizeLimits.x,sizeLimits.y);
			}
		}
	}
}