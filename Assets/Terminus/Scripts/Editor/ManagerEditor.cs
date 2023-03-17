using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Terminus .Editors
{
	[CustomEditor( typeof(Manager))]
	public class ManagerEditor : Editor {

		protected SerializedProperty updateProp;
		protected SerializedProperty globalRecProp;
		protected SerializedProperty environmentSurfaceProp;
		protected SerializedProperty environmentLayersProp;

		bool rootObjectsFold;
		bool registeredObjectsFold;
		bool registeredConnectorsFold;
		bool acceptingConnectorsFold;
		bool activePortsFold;
		Vector2 scrollPos;

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			Manager obj = (Manager)target;
			obj.DeclareSingleton();
			Manager.UpdateCheckData();

			//scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(updateProp);
			EditorGUILayout.PropertyField(globalRecProp);
			EditorGUILayout.PropertyField(environmentLayersProp);
			EditorGUILayout.PropertyField(environmentSurfaceProp);
			if(EditorGUI.EndChangeCheck())
				serializedObject.ApplyModifiedProperties();
			if (GUI.changed)
				EditorUtility.SetDirty (target);

			if (obj.rootObjects == null || obj.registeredObjects == null || obj.registeredConnectors == null || obj.acceptingConnectors == null || obj.activePorts == null)
			{
				obj.RecreateHierarchy();
			}

			Color oldCol = GUI.color;
			GUI.color = Color.yellow;

			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.HelpBox(" objects info (read-only)",MessageType.None);
			GUI.enabled = false;
			EditorGUI.indentLevel++;
			if (registeredObjectsFold = EditorGUILayout.Foldout(registeredObjectsFold,"Registered objects ("+obj.registeredObjects.Count.ToString()+")"))
			{
				EditorGUILayout.LabelField("Size: "+obj.registeredObjects.Count.ToString());
				for (int i = 0; i < obj.registeredObjects.Count;i++)
					EditorGUILayout.ObjectField("",obj.registeredObjects[i],typeof(TerminusObject),true);
			}
			if (rootObjectsFold = EditorGUILayout.Foldout(rootObjectsFold,"Root objects ("+obj.rootObjects.Count.ToString()+")"))
			{
				EditorGUILayout.LabelField("Size: "+obj.rootObjects.Count.ToString());
				for (int i = 0; i < obj.rootObjects.Count;i++)
					EditorGUILayout.ObjectField("",obj.rootObjects[i],typeof(TerminusObject),true);
			}
			if (registeredConnectorsFold = EditorGUILayout.Foldout(registeredConnectorsFold,"Registered connectors ("+obj.registeredConnectors.Count.ToString()+")"))
			{
				EditorGUILayout.LabelField("Size: "+obj.registeredConnectors.Count.ToString());
				for (int i = 0; i < obj.registeredConnectors.Count;i++)
					EditorGUILayout.ObjectField("",obj.registeredConnectors[i],typeof(Connector),true);
			}
			if (activePortsFold = EditorGUILayout.Foldout(activePortsFold,"Active ports ("+obj.activePorts.Count.ToString()+")"))
			{
				EditorGUILayout.LabelField("Size: "+obj.activePorts.Count.ToString());
				for (int i = 0; i < obj.activePorts.Count;i++)
					EditorGUILayout.ObjectField("",obj.activePorts[i],typeof(Connector),true);
			}
			if (acceptingConnectorsFold = EditorGUILayout.Foldout(acceptingConnectorsFold,"Accepting connectors ("+obj.acceptingConnectors.Count.ToString()+")"))
			{
				EditorGUILayout.LabelField("Size: "+obj.acceptingConnectors.Count.ToString());
				for (int i = 0; i < obj.acceptingConnectors.Count;i++)
					EditorGUILayout.ObjectField("",obj.acceptingConnectors[i],typeof(Connector),true);
			}

			EditorGUI.indentLevel--;

			GUI.color = oldCol;

			GUI.enabled = true;
			EditorGUILayout.EndVertical();
			//EditorGUILayout.EndScrollView();
		}

		void OnEnable () 
		{
			updateProp = serializedObject.FindProperty("updateEvent");
			globalRecProp = serializedObject.FindProperty("globalEventsReciever");
			environmentSurfaceProp = serializedObject.FindProperty("environmentSurface");
			environmentLayersProp = serializedObject.FindProperty("environmentLayers");
		}
	}
}