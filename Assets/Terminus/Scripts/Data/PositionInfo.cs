using UnityEngine;
using System.Collections;

namespace Terminus 
{
	/// <summary>
	/// Structure for storing spatial information of <see cref="Connector"/>s.
	/// </summary>
	public struct PositionInfo 
	{
		public Vector3 position;
		public Quaternion rotation;
		public Connector connector;
		public Component collider;

		public PositionInfo(Vector3 position, Quaternion rotation, Connector connector, Component collider)
		{
			this.position = position;
			this.rotation = rotation;
			this.connector = connector;
			this.collider = collider;
		}
	}
}