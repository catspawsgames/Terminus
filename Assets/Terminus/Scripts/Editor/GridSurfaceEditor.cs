using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Terminus.Editors
{
	[CustomEditor( typeof(GridSurface))]
	public class GridSurfaceEditor : SurfaceEditor {

		protected Vector3[] lines;
		protected Vector3 oldStep;
		protected int oldResX;
		protected int oldResY;
		protected int oldResZ;

		const float sizeModifier = 3.0f;

		public override void OnSceneGUI ()
		{
			base.OnSceneGUI ();

			GridSurface surface = (GridSurface)target;

            if (surface.CacheInterfaceRecieversWithChangeCheck())
                EditorUtility.SetDirty(target);

            if (surface.drawGizmos && surface.gridStep.x > 0 && surface.gridStep.z > 0)
			{
				Color oldCol = Handles.color;

				Handles.color = surface.portOptions.inspectorColor;

				Vector3 pos = surface.transform.position;
				float size = HandleUtility.GetHandleSize(pos);

				int gridResolutionX = Mathf.Max(Mathf.FloorToInt(size * sizeModifier / (surface.gridStep.x * surface.transform.lossyScale.x)),1);
				int gridResolutionY = (surface.gridStep.y == 0) ? 0 : Mathf.Max(Mathf.FloorToInt(size * sizeModifier / (surface.gridStep.y * surface.transform.lossyScale.y)),1);
				int gridResolutionZ = Mathf.Max(Mathf.FloorToInt(size * sizeModifier / (surface.gridStep.z * surface.transform.lossyScale.z)),1);

				if (lines == null || lines.Length == 0 || oldStep != surface.gridStep || oldResX != gridResolutionX || oldResY != gridResolutionY || oldResZ != gridResolutionZ)
				{
					//lines = new Vector3[(gridResolutionX * 2 + 1) * 2 + (gridResolutionZ * 2 + 1) * 2];
					if (gridResolutionY > 0)
						lines = new Vector3[(gridResolutionX * 2 + 1) * 2 + (gridResolutionZ * 2 + 1) * 4 + (gridResolutionY * 2 + 1) * 2];
					else
						lines = new Vector3[(gridResolutionX * 2 + 1) * 2 + (gridResolutionZ * 2 + 1) * 2];

					for (int x = -gridResolutionX; x <= gridResolutionX; x++)
					{						
						lines[(gridResolutionX*2 + x*2)] = surface.transform.TransformPoint(x * surface.gridStep.x,0, -gridResolutionZ * surface.gridStep.z);
						lines[(gridResolutionX*2 + x*2) + 1] = surface.transform.TransformPoint(x * surface.gridStep.x,0, gridResolutionZ * surface.gridStep.z);
					}
					for (int z = -gridResolutionZ; z <= gridResolutionZ; z++)
					{
						lines[(gridResolutionX*4 + 2) + (gridResolutionZ*2 + z*2)] = surface.transform.TransformPoint(-gridResolutionX * surface.gridStep.x,0, z * surface.gridStep.z);
						lines[(gridResolutionX*4 + 2) + (gridResolutionZ*2 + z*2) + 1] = surface.transform.TransformPoint(gridResolutionX * surface.gridStep.x,0, z * surface.gridStep.z);
					}
					if (gridResolutionY > 0)
					{						
						for (int y = -gridResolutionY; y <= gridResolutionY; y++)
						{
							lines[(gridResolutionX*4 + 2) + (gridResolutionZ*4 + 2) + (gridResolutionZ*2 + y*2)] = surface.transform.TransformPoint(0,y * surface.gridStep.y,-gridResolutionZ * surface.gridStep.z);
							lines[(gridResolutionX*4 + 2) + (gridResolutionZ*4 + 2) + (gridResolutionZ*2 + y*2) + 1] = surface.transform.TransformPoint(0,y * surface.gridStep.y,gridResolutionZ * surface.gridStep.z);
						}	
						for (int z = -gridResolutionZ; z <= gridResolutionZ; z++)
						{
							lines[(gridResolutionX*4 + 2) + (gridResolutionZ*4 + 2) + (gridResolutionY*4 + 2) + (gridResolutionZ*2 + z*2)] = surface.transform.TransformPoint(0,-gridResolutionY * surface.gridStep.y, z * surface.gridStep.z);
							lines[(gridResolutionX*4 + 2) + (gridResolutionZ*4 + 2) + (gridResolutionY*4 + 2) + (gridResolutionZ*2 + z*2) + 1] = surface.transform.TransformPoint(0,gridResolutionY * surface.gridStep.y, z * surface.gridStep.z);
						}
					}

					oldStep = surface.gridStep;
					oldResX = gridResolutionX;
					oldResY = gridResolutionY;
					oldResZ = gridResolutionZ;
				}


				//Debug.Log(gridResolutionX.ToString() + ","+gridResolutionZ.ToString());
				//Debug.Log(lines.Length);

				Handles.DrawLines(lines);

				Handles.color = oldCol;
			}
		}

		public override void OnInspectorGUI()
		{
			GridSurface surface = (GridSurface)target;
			surface.gridStep = EditorGUILayout.Vector3Field(new GUIContent("Grid step(local)","Step sizes of the grid in local step. Leave Y=0 to get flat grid behaviour."),surface.gridStep);
			base.OnInspectorGUI();
		}

	}
}