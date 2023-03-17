using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Terminus .Editors
{
	[CustomEditor( typeof(Settings))]
	public class SettingsEditor : Editor {
		
		public override void OnInspectorGUI()
		{
			EditorGUILayout.HelpBox("Use 'Window -> Terminus setup' interface to change  settings",MessageType.Info);
		}


	}
}