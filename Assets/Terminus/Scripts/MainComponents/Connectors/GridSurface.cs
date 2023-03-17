using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terminus
{
	/// <summary>
	/// <see cref="Surface"/> that snaps <see cref="Ports"/> trying to connect with it into a grid (either flat or 3D). Useful if you want a neat grid-based building system.
	/// Is always aligned with XZ plane of transform this component is on, with coordinates starting at (0,0,0) in local space.
	/// </summary>
	[AddComponentMenu("Terminus modular system/Surface - grid")]
	public class GridSurface : Surface {

		/// <summary>
		/// Grid step. Leave Y component 0 for flat grid behaviour.
		/// </summary>
		public Vector3 gridStep = new Vector3(1,0,1);
	

		public override bool AlignPortWithThisConnector (Port port, Vector3 position, Quaternion rotation, Component collider)
		{
			Vector3 localPos = transform.InverseTransformPoint(position);
			Quaternion localRot = rotation * Quaternion.Inverse(transform.rotation);

			//Snapping rotation - it must be aligned with XZ plane and always orthogonal around Y axis - selecting closest orthogonal angle to original Y angle.
			localRot = Quaternion.Euler(270,Mathf.Round(localRot.eulerAngles.y / 90) * 90,0);

			//Snapping position to grid step, leaving Y coordinate alone.
			localPos = new Vector3(Mathf.Round(localPos.x / gridStep.x) * gridStep.x,
				(gridStep.y == 0) ? localPos.y : Mathf.Round(localPos.y / gridStep.y) * gridStep.y,
				Mathf.Round(localPos.z / gridStep.z) * gridStep.z);
			
			port.Align(transform.TransformPoint(localPos), transform.rotation * localRot);
			return true;
		}
	}
}