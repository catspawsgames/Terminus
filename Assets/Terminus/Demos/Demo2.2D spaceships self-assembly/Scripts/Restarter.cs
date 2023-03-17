using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Terminus.Demo2
{
	public class Restarter : MonoBehaviour {

		public float restartTime = 3;
		public Text text;

		protected float restartTimer = 0;
		protected bool countdown = false;

		public void Restart()
		{
			text.enabled = true;
			restartTimer = restartTime;
			countdown = true;
		}

		public void Update()
		{			
			if (countdown)
			{
				restartTimer -= Time.deltaTime;
				if (restartTimer < 0)
					UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
			}
		}
	}
}