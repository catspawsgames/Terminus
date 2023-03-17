using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Terminus.DemoShared
{
	public class TerminusDemo_SceneQuitter : MonoBehaviour {

		public KeyCode quitButton = KeyCode.Escape;

		// Update is called once per frame
		void Update () 
		{
			if (Input.GetKeyDown(quitButton))
				SceneManager.LoadScene("Terminus_demo_scenes_switcher");		
		}
	}
}