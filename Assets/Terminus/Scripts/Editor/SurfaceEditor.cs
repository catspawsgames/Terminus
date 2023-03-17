using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Terminus.Editors
{
	[CustomEditor( typeof(Surface))]
	public class SurfaceEditor : Editor {

		bool messagingFoldout;
		bool attachmentsFoldout;
		private SerializedProperty collidersProp;
		private SerializedProperty colliders2DProp;

		private SerializedProperty nameProp;


		public virtual void OnSceneGUI()
		{
			Surface surface = (Surface)target;

			Vector3 pos = surface.transform.position;
			float size = HandleUtility.GetHandleSize(pos);

			Color oldCol = Handles.color;
			Handles.color = new Color(0.0f,0.8f,1.0f,0.6f);

			switch (surface.symmetryType)
			{
			case Surface.SymmetryTypes.point:
				Handles.color = new Color(0.0f,0.8f,1.0f,0.6f);
				//Handles.SphereCap(-1,surface.transform.TransformPoint(surface.symmetryPoint),Quaternion.identity,size * 0.15f);
                Handles.SphereHandleCap(-1, surface.transform.TransformPoint(surface.symmetryPoint), Quaternion.identity, size * 0.15f, EventType.Ignore);                    
				break;
			case Surface.SymmetryTypes.linear:
				Handles.color = new Color(0.0f,0.8f,1.0f,1.0f);
				Vector3[] lpoints = new Vector3[2];
				lpoints[0] = surface.transform.TransformPoint(surface.symmetryPoint-surface.symmetryVector*size*3);
				lpoints[1] = surface.transform.TransformPoint(surface.symmetryPoint+surface.symmetryVector*size*3);
				Handles.DrawPolyLine(lpoints);
				break;
			case Surface.SymmetryTypes.planar:
				Handles.color = new Color(0.0f,0.8f,1.0f,0.6f);
				Vector3[] points = new Vector3[4];
				points[0] = surface.transform.TransformPoint(surface.symmetryPoint + surface.symmetryRotation * new Vector3(size,size,0));
				points[1] = surface.transform.TransformPoint(surface.symmetryPoint + surface.symmetryRotation * new Vector3(-size,size,0));
				points[2] = surface.transform.TransformPoint(surface.symmetryPoint + surface.symmetryRotation * new Vector3(-size,-size,0));
				points[3] = surface.transform.TransformPoint(surface.symmetryPoint + surface.symmetryRotation * new Vector3(size,-size,0));
				Handles.DrawAAConvexPolygon(points);
				break;
			}
			Handles.color = oldCol;
		}

		/*
		public virtual void OnDrawGizmosSelected()
		{
			Gizmos.DrawCube(Vector3.zero,Vector3.one);
		}
		*/

		
		public override void OnInspectorGUI()
		{
			Surface surface = (Surface)target;

            if (surface.CacheInterfaceRecieversWithChangeCheck())
                EditorUtility.SetDirty(target);

            surface.active = EditorGUILayout.ToggleLeft("Active",surface.active);
			if (surface.owner == null)
				EditorGUILayout.HelpBox("This surface has no owner TerminusObject and will not function correctly.",MessageType.Warning);
			
			surface.owner = (TerminusObject)EditorGUILayout.ObjectField("Owner",surface.owner,typeof(TerminusObject),true);
			surface.useOwnerRigidbody = EditorGUILayout.ToggleLeft("Use owners Rigidbody",surface.useOwnerRigidbody);
			
			if (nameProp.stringValue == "")
			{
				nameProp.stringValue = surface.index.ToString();
				serializedObject.ApplyModifiedProperties();
			}
			
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(nameProp,true);
			if(EditorGUI.EndChangeCheck())
				serializedObject.ApplyModifiedProperties();

			surface.layer = EditorGUILayout.Popup(" Layer",surface.layer,ProjectManager.settings.GetLayersNames());

			if (surface.portOptions.use2DPhysics)
			{
				/*
				EditorGUILayout.BeginHorizontal();
				surface.connectorRigidbody2D = (Rigidbody2D)EditorGUILayout.ObjectField("2D Rigidbody",surface.connectorRigidbody2D,typeof(Rigidbody2D),true);
				if (surface.connectorRigidbody2D == null)
				{
					if (GUILayout.Button("Search",GUILayout.MaxWidth(50)))
					{
						surface.connectorRigidbody2D = surface.GetComponent<Rigidbody2D>();
						Transform searchTransform = surface.transform;
						while (surface.connectorRigidbody2D == null && searchTransform.parent != null)
						{
							searchTransform = searchTransform.parent;
							surface.connectorRigidbody2D = searchTransform.GetComponent<Rigidbody2D>();
							if (searchTransform == surface.owner.transform)
								break;
						}
					}
				}
				EditorGUILayout.EndHorizontal();
				*/
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(colliders2DProp,true);
				if(EditorGUI.EndChangeCheck())
					serializedObject.ApplyModifiedProperties();
			}
			else
			{
				/*
				EditorGUILayout.BeginHorizontal();
				surface.connectorRigidbody = (Rigidbody)EditorGUILayout.ObjectField("Rigidbody",surface.connectorRigidbody,typeof(Rigidbody),true);
				if (surface.connectorRigidbody == null)
				{
					if (GUILayout.Button("Search",GUILayout.MaxWidth(50)))
					{
						surface.connectorRigidbody = surface.GetComponent<Rigidbody>();
						Transform searchTransform = surface.transform;
						while (surface.connectorRigidbody == null && searchTransform.parent != null)
						{
							searchTransform = searchTransform.parent;
							surface.connectorRigidbody = searchTransform.GetComponent<Rigidbody>();
							if (searchTransform == surface.owner.transform)
								break;
						}
					}
				}
				EditorGUILayout.EndHorizontal();
				*/
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(collidersProp,true);
				if(EditorGUI.EndChangeCheck())
					serializedObject.ApplyModifiedProperties();
			}

			if (surface.overrideLayerOptions = EditorGUILayout.ToggleLeft("Override layer options",surface.overrideLayerOptions))
			{
				EditorGUI.indentLevel++;
				SettingsWindow.DrawLayerOptions(surface.overridenOptions,ref messagingFoldout);
				if (GUILayout.Button("Set settings to layer default"))
				{
					surface.overridenOptions.inspectorColor = ProjectManager.settings.layers[surface.layer].inspectorColor;
					surface.overridenOptions.acceptsConnectionFromLayers = ProjectManager.settings.layers[surface.layer].acceptsConnectionFromLayers;
					surface.overridenOptions.attachementPort = ProjectManager.settings.layers[surface.layer].attachementPort;
					surface.overridenOptions.cyclingAttachementPort = ProjectManager.settings.layers[surface.layer].cyclingAttachementPort;
					surface.overridenOptions.attachmentType = ProjectManager.settings.layers[surface.layer].attachmentType;
					surface.overridenOptions.use2DPhysics = ProjectManager.settings.layers[surface.layer].use2DPhysics;
					surface.overridenOptions.jointPrefab = ProjectManager.settings.layers[surface.layer].jointPrefab;
					surface.overridenOptions.jointPrefab2D = ProjectManager.settings.layers[surface.layer].jointPrefab2D;
					surface.overridenOptions.snapRadius = ProjectManager.settings.layers[surface.layer].snapRadius;
					surface.overridenOptions.snapConeAngle = ProjectManager.settings.layers[surface.layer].snapConeAngle;
					surface.overridenOptions.orientationType = ProjectManager.settings.layers[surface.layer].orientationType;
					surface.overridenOptions.sendMessagesToPorts = ProjectManager.settings.layers[surface.layer].sendMessagesToPorts;
					surface.overridenOptions.sendMessagesToOwnerObjects = ProjectManager.settings.layers[surface.layer].sendMessagesToOwnerObjects;
					surface.overridenOptions.sendMessagesToGameObject = ProjectManager.settings.layers[surface.layer].sendMessagesToGameObject;
					surface.overridenOptions.useInfluenceMessaging = ProjectManager.settings.layers[surface.layer].useInfluenceMessaging;
					surface.overridenOptions.influenceRadius = ProjectManager.settings.layers[surface.layer].influenceRadius;
					surface.overridenOptions.influenceConeAngle = ProjectManager.settings.layers[surface.layer].influenceConeAngle;
					surface.overridenOptions.influenceMessageEnter = ProjectManager.settings.layers[surface.layer].influenceMessageEnter;
					surface.overridenOptions.influenceMessageExit = ProjectManager.settings.layers[surface.layer].influenceMessageExit;
					surface.overridenOptions.rotationType = ProjectManager.settings.layers[surface.layer].rotationType;
					surface.overridenOptions.rotationLimits = ProjectManager.settings.layers[surface.layer].rotationLimits;
					surface.overridenOptions.rotationStep = ProjectManager.settings.layers[surface.layer].rotationStep;
				}
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.Space();

			switch (surface.symmetryType = (Surface.SymmetryTypes)EditorGUILayout.EnumPopup("Symmetry type",surface.symmetryType))
			{
			case Surface.SymmetryTypes.point:
				EditorGUI.indentLevel++;
				surface.symmetryPoint = EditorGUILayout.Vector3Field("Point",surface.symmetryPoint);
				surface.maxSymmetryObjects = EditorGUILayout.IntField("Maximum symmetry clones",surface.maxSymmetryObjects);
				EditorGUI.indentLevel--;
				break;
			case Surface.SymmetryTypes.linear:
				EditorGUI.indentLevel++;
				surface.symmetryPoint = EditorGUILayout.Vector3Field("Start point",surface.symmetryPoint);
				surface.symmetryVector = EditorGUILayout.Vector3Field("Direction",surface.symmetryVector);
				surface.maxSymmetryObjects = EditorGUILayout.IntField("Maximum symmetry clones",surface.maxSymmetryObjects);
				EditorGUI.indentLevel--;
				break;
			case Surface.SymmetryTypes.planar:
				EditorGUI.indentLevel++;
				surface.symmetryPoint = EditorGUILayout.Vector3Field("Point",surface.symmetryPoint);
				EditorGUI.BeginChangeCheck();		
				Vector3 eulerAngles = EditorGUILayout.Vector3Field("Rotation(local)",surface.symmetryRotation.eulerAngles);	
				if (EditorGUI.EndChangeCheck()) 
					surface.symmetryRotation = Quaternion.Euler(eulerAngles);
				EditorGUI.indentLevel--;
				break;
			}

			if (GUI.changed)
				EditorUtility.SetDirty (target);

			EditorGUILayout.Space();

			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.HelpBox("Editor options",MessageType.None);
			surface.drawGizmos = EditorGUILayout.ToggleLeft("Show gizmo",surface.drawGizmos);
			EditorGUILayout.EndVertical();
			
			Color oldCol = GUI.color;
			GUI.color = new Color(0,1,0.5f,1);
			
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.HelpBox("Current state (read-only)",MessageType.None);
			
			GUI.enabled = false;
			
			EditorGUILayout.IntField("Index",surface.index);
			EditorGUILayout.ToggleLeft("Can accept attachments",surface.isAccepting);
			EditorGUI.indentLevel++;
			if (attachmentsFoldout = EditorGUILayout.Foldout(attachmentsFoldout,"Attached ports ("+surface.attachmentsInfo.Count.ToString()+")"))
			{
				for (int i = 0; i < surface.attachmentsInfo.Count; i++)
					EditorGUILayout.ObjectField("",surface.attachmentsInfo[i].otherConnector,typeof(Connector),true);
			}
			EditorGUI.indentLevel--;
			EditorGUILayout.EndVertical();
			GUI.enabled = true;
			
			EditorGUILayout.Space();
			GUI.color = oldCol;
		}
		
		public virtual void OnEnable () 
		{
			Surface surface = (Surface)target;

			collidersProp = serializedObject.FindProperty("colliders");
			colliders2DProp = serializedObject.FindProperty("colliders2D");
			nameProp = serializedObject.FindProperty("connectorName");

			if (surface.owner == null)
			{
				Transform tr = surface.transform;
				while (tr.gameObject.GetComponent<TerminusObject>() == null && tr.parent != null)
					tr = tr.parent;
				surface.owner = tr.gameObject.GetComponent<TerminusObject>();
				surface.owner.RegisterConnector(surface);
			}
			else
			{
				if (surface.owner.connectors == null)
					surface.owner.connectors = new List<Connector>();
				if (!surface.owner.connectors.Contains(surface))
					surface.owner.RegisterConnector(surface);
			}
			/*
			if (surface.connectorRigidbody2D == null)
			{
				surface.connectorRigidbody2D = surface.GetComponent<Rigidbody2D>();
				Transform searchTransform = surface.transform;
				while (surface.connectorRigidbody2D == null && searchTransform.parent != null)
				{
					searchTransform = searchTransform.parent;
					surface.connectorRigidbody2D = searchTransform.GetComponent<Rigidbody2D>();
					if (searchTransform == surface.owner.transform)
						break;
				}
			}
			if (surface.connectorRigidbody == null)
			{
				surface.connectorRigidbody = surface.GetComponent<Rigidbody>();
				Transform searchTransform = surface.transform;
				while (surface.connectorRigidbody == null && searchTransform.parent != null)
				{
					searchTransform = searchTransform.parent;
					surface.connectorRigidbody = searchTransform.GetComponent<Rigidbody>();
					if (searchTransform == surface.owner.transform)
						break;
				}
			}
			*/

			if (!surface.portOptions.use2DPhysics && surface.colliders.Count == 0 && surface.GetComponent<Collider>() != null)
				surface.colliders.Add(surface.GetComponent<Collider>());
			if (surface.portOptions.use2DPhysics && surface.colliders2D.Count == 0 && surface.GetComponent<Collider2D>() != null)
				surface.colliders2D.Add(surface.GetComponent<Collider2D>());

		}
	}
}