using UnityEngine;
using System.Collections;


namespace Terminus
{
	/// <summary>
	/// Container for storing information pertaining to user interface about your <see cref="TerminusObject"/>s.
	/// Terminus system doesn't use that information, but it could be useful to store UI information directly in TerminusObject.
	/// You can inherit from this class if you need to store additional info.
	/// </summary>
	[System.Serializable]
	public class UIInfoContainer {	

		public Sprite icon;
		public string partName;
		public string partDescription;

	}
}