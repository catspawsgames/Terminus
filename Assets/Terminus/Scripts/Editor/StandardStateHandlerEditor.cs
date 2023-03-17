using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;

namespace Terminus .Editors
{
	[CustomEditor( typeof(StandardStateHandler))]
	public class StandardStateHandlerEditor : Editor {

		protected SerializedProperty rigidbodiesProp;
		protected SerializedProperty rigidbodies2DProp;



		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			StandardStateHandler obj = (StandardStateHandler)target;

			EditorGUILayout.HelpBox("Affected objects",MessageType.None);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Auto add:",GUILayout.MaxWidth(60));
			if (GUILayout.Button("Rigidbodies"))
			{
				if (obj.affectedRigidbodies == null)
					obj.affectedRigidbodies = new List<Transform>();
				else
					obj.affectedRigidbodies = obj.affectedRigidbodies.Where(rec => rec != null).ToList();
				Rigidbody[] arr = obj.GetComponentsInChildren<Rigidbody>();
				for (int i = 0; i < arr.Length; i++)
				{
					if (!obj.affectedRigidbodies.Contains(arr[i].transform))
						obj.affectedRigidbodies.Add(arr[i].transform);
				}
				if (obj.affectedRigidbodies2D == null)
					obj.affectedRigidbodies2D = new List<Transform>();
				else
					obj.affectedRigidbodies2D = obj.affectedRigidbodies2D.Where(rec => rec != null).ToList();
				Rigidbody2D[] arr2 = obj.GetComponentsInChildren<Rigidbody2D>();
				for (int i = 0; i < arr2.Length; i++)
				{
					if (!obj.affectedRigidbodies2D.Contains(arr2[i].transform))
						obj.affectedRigidbodies2D.Add(arr2[i].transform);
				}
			}
			EditorGUILayout.EndHorizontal();
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(rigidbodiesProp,true);
			EditorGUILayout.PropertyField(rigidbodies2DProp,true);
			if(EditorGUI.EndChangeCheck())
				serializedObject.ApplyModifiedProperties();
			EditorGUILayout.Space();

			if (GUI.changed)
				EditorUtility.SetDirty (target);

			GUI.enabled = false;
			//EditorGUILayout.ToggleLeft("In assembly state",obj.inAssemblyState);
			GUI.enabled = true;

			//serializedObject.ApplyModifiedProperties();

		}

		void OnEnable () 
		{
			rigidbodiesProp = serializedObject.FindProperty("affectedRigidbodies");
			rigidbodies2DProp = serializedObject.FindProperty("affectedRigidbodies2D");
		}	
	}
}