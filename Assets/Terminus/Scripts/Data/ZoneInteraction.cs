using UnityEngine;
using System.Collections;

namespace Terminus 
{
	/// <summary>
	/// Information about interaction of two <see cref="Connector"/>s.
	/// </summary>
	/// <seealso cref="Settings.LayerPortOptions.useInfluenceMessaging"/>
	public class ZoneInteraction 
	{
		public Connector thisConnector;
		public Connector otherConnector;
		/// <summary>
		/// Square of distance between <see cref="Connector"/>ss. Get Mathf.Sqrt of this to see real distance.
		/// </summary>
		/// <remarks>
		/// Stored as a square for performance purposes.
		/// </remarks>
		public float sqrDistance;
	}
}