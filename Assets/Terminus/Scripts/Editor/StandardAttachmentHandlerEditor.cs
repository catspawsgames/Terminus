using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;

namespace Terminus .Editors
{
	[CustomEditor( typeof(StandardAttachmentHandler))]
	public class StandardAttachmentHandlerEditor : Editor {
		
		protected Vector3[] handleShifts;
		protected SerializedProperty renderersProp;
		protected SerializedProperty collidersProp;
		protected SerializedProperty colliders2DProp;
		protected SerializedProperty rigidbodiesProp;
		protected SerializedProperty rigidbodies2DProp;


		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			StandardAttachmentHandler obj = (StandardAttachmentHandler)target;

			EditorGUILayout.HelpBox("Affected objects",MessageType.None);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Auto add:",GUILayout.MaxWidth(60));
			if (GUILayout.Button("Renderers"))
			{
				if (obj.affectedRenderers == null)
					obj.affectedRenderers = new List<Renderer>();
				else
					obj.affectedRenderers = obj.affectedRenderers.Where(rec => rec != null).ToList();
				Renderer[] arr = obj.GetComponentsInChildren<Renderer>();
				for (int i = 0; i < arr.Length; i++)
				{
					if (!obj.affectedRenderers.Contains(arr[i]))
						obj.affectedRenderers.Add(arr[i]);
				}
			}
			if (GUILayout.Button("Colliders"))
			{
				if (obj.affectedColliders == null)
					obj.affectedColliders = new List<Collider>();
				else
					obj.affectedColliders = obj.affectedColliders.Where(rec => rec != null).ToList();
				Collider[] arr = obj.GetComponentsInChildren<Collider>();
				for (int i = 0; i < arr.Length; i++)
				{
					if (!obj.affectedColliders.Contains(arr[i]))
						obj.affectedColliders.Add(arr[i]);
				}

				if (obj.affectedColliders2D == null)
					obj.affectedColliders2D = new List<Collider2D>();
				else
					obj.affectedColliders2D = obj.affectedColliders2D.Where(rec => rec != null).ToList();
				Collider2D[] arr2 = obj.GetComponentsInChildren<Collider2D>();
				for (int i = 0; i < arr2.Length; i++)
				{
					if (!obj.affectedColliders2D.Contains(arr2[i]))
						obj.affectedColliders2D.Add(arr2[i]);
				}
			}
			if (GUILayout.Button("R.Bodies"))
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
			EditorGUILayout.PropertyField(renderersProp,true);
			EditorGUILayout.PropertyField(collidersProp,true);
			EditorGUILayout.PropertyField(colliders2DProp,true);
			EditorGUILayout.PropertyField(rigidbodiesProp,true);
			EditorGUILayout.PropertyField(rigidbodies2DProp,true);
			if(EditorGUI.EndChangeCheck())
				serializedObject.ApplyModifiedProperties();
			EditorGUILayout.Space();

			if (obj.overrideColliderLayers = EditorGUILayout.ToggleLeft("Override colliders layers",obj.overrideColliderLayers))
				obj.newLayer = EditorGUILayout.LayerField("New layer",obj.newLayer);

			obj.validColor = EditorGUILayout.ColorField("Valid color", obj.validColor);
			obj.invalidColor = EditorGUILayout.ColorField("Invalid color", obj.invalidColor);
			obj.collidingColor = EditorGUILayout.ColorField("Colliding color", obj.collidingColor);


			if (GUI.changed)
				EditorUtility.SetDirty (target);

			//serializedObject.ApplyModifiedProperties();

		}

		void OnEnable () 
		{
			renderersProp = serializedObject.FindProperty("affectedRenderers");
			collidersProp = serializedObject.FindProperty("affectedColliders");
			colliders2DProp = serializedObject.FindProperty("affectedColliders2D");
			rigidbodiesProp = serializedObject.FindProperty("affectedRigidbodies");
			rigidbodies2DProp = serializedObject.FindProperty("affectedRigidbodies2D");
		}	
	}
}