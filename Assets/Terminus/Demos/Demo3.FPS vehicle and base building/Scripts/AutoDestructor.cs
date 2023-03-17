using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terminus.Demo3
{
	public class AutoDestructor : MonoBehaviour {

		public float timer = 5.0f;
		
		// Update is called once per frame
		void Update () {
			timer -= Time.deltaTime;
			if (timer < 0)
				Destroy(gameObject);
		}
	}
}