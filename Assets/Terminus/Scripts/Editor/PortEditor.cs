using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Terminus .Editors
{
	[CustomEditor( typeof(Port))]
	public class PortEditor : Editor {

		private static Vector3[] gizmoBaseLine = new Vector3[4] {new Vector3(0,2,0),
			new Vector3(0,-2,0),
			new Vector3(0,-2,1),
			new Vector3(0,2,1)};		
		private static Vector3[] gizmoLowerPolygon = new Vector3[4] {new Vector3(0,0,1),
			new Vector3(0,-2,1),
			new Vector3(0,-2,2),
			new Vector3(0,-1,2)};
		private static Vector3[] gizmoUpperSquare = new Vector3[4] {new Vector3(0,2,1),
			new Vector3(0,1,1),
			new Vector3(0,1,2),
			new Vector3(0,2,2)};
		private static Vector3[] gizmoLowerSquare = new Vector3[4] {new Vector3(0,-1,1),
			new Vector3(0,-2,1),
			new Vector3(0,-2,2),
			new Vector3(0,-1,2)};
		private static Vector3[] gizmoMiddlePolygon = new Vector3[4] {new Vector3(0,1,1),
			new Vector3(0,-1,1),
			new Vector3(0,-1,2),
			new Vector3(0,0,2)};
		private static Vector3[] gizmoMiddleSquare = new Vector3[4] {new Vector3(0,1,1),
			new Vector3(0,-1,1),
			new Vector3(0,-1,2),
			new Vector3(0,1,2)};
		private static Vector3[] gizmoPerpendicularPlate = new Vector3[8] {new Vector3(2,1,0),
			new Vector3(1,2,0),
			new Vector3(-1,2,0),
			new Vector3(-2,1,0),
			new Vector3(-2,-1,0),
			new Vector3(-1,-2,0),
			new Vector3(1,-2,0),
			new Vector3(2,-1,0)};

		
		private Quaternion oldRotation;
		private Vector3 oldPosition;

		private SerializedProperty nameProp;

		bool messagingFoldout;
		bool influenceFoldout;

		public static Vector3[] TransformPointsToView(Vector3[] original, Vector3 position, Quaternion rotation, float size)
		{
			Vector3[] result = new Vector3[original.Length];
			for (int i = 0; i < result.Length;i++)
				result[i] = position + rotation * original[i] * size * 0.2f;
			return result;
		}

		public static void DrawPortGizmo(Port port)
		{
			Vector3 pos = port.transform.TransformPoint(port.offset);
			Quaternion rot = port.transform.rotation * port.rotation;
			float size = HandleUtility.GetHandleSize(pos);

			Color oldCol = Handles.color;

			Settings.LayerPortOptions portOptions = port.portOptions;

			Handles.color = portOptions.inspectorColor;

			Handles.DrawAAConvexPolygon(TransformPointsToView(gizmoBaseLine,pos,rot,size));

			switch (portOptions.orientationType)
			{
			case Settings.OrientationTypes.exact:
				if (port.owner.mode == TerminusObject.Modes.accepting_attachments || port.owner.mode == TerminusObject.Modes.free_connections)
				{
					Handles.DrawAAConvexPolygon(TransformPointsToView(gizmoUpperSquare,pos,rot,size));
					Handles.DrawAAConvexPolygon(TransformPointsToView(gizmoLowerPolygon,pos,rot,size));
				}
				else
				{
					Handles.DrawAAConvexPolygon(TransformPointsToView(gizmoMiddlePolygon,pos,rot,size));
				}
				break;
			case Settings.OrientationTypes.planar:
				if (port.owner.mode == TerminusObject.Modes.accepting_attachments || port.owner.mode == TerminusObject.Modes.free_connections)
				{
					Handles.DrawAAConvexPolygon(TransformPointsToView(gizmoUpperSquare,pos,rot,size));
					Handles.DrawAAConvexPolygon(TransformPointsToView(gizmoLowerSquare,pos,rot,size));
				}
				else
				{
					Handles.DrawAAConvexPolygon(TransformPointsToView(gizmoMiddleSquare,pos,rot,size));
				}
				Handles.DrawAAConvexPolygon(TransformPointsToView(gizmoPerpendicularPlate,pos,rot,size));
				break;
			case Settings.OrientationTypes.twosided:
				if (port.owner.mode == TerminusObject.Modes.accepting_attachments || port.owner.mode == TerminusObject.Modes.free_connections)
				{
					Handles.DrawAAConvexPolygon(TransformPointsToView(gizmoUpperSquare,pos,rot,size));
					Handles.DrawAAConvexPolygon(TransformPointsToView(gizmoLowerSquare,pos,rot,size));
				}
				else
				{
					Handles.DrawAAConvexPolygon(TransformPointsToView(gizmoMiddleSquare,pos,rot,size));
				}
				break;
			}

			GUIStyle style = new GUIStyle();
			style.normal.textColor = new Color(portOptions.inspectorColor.r,
				portOptions.inspectorColor.g,
				portOptions.inspectorColor.b,
				1.0f);
			if (port.index.ToString() != port.connectorName)
				Handles.Label(pos,port.index.ToString()+'.'+port.connectorName,style);
			else
				Handles.Label(pos,port.index.ToString(),style);

			Handles.color = oldCol;
		}

		public void OnSceneGUI()
		{
			Port port = (Port)target;

			Vector3 pos = port.transform.TransformPoint(port.offset);
			Quaternion rot = port.transform.rotation * port.rotation;
			//float size = HandleUtility.GetHandleSize(pos);

			if (port.drawControls)
			{
				Quaternion newRotation = Handles.RotationHandle(rot,pos);

				if (newRotation != oldRotation)
					port.rotation = Quaternion.Inverse(port.transform.rotation) * newRotation;
				oldRotation = newRotation;

				Vector3 newPosition = Handles.PositionHandle(pos,rot);
				if (newPosition != oldPosition)
					port.offset = port.transform.InverseTransformPoint(newPosition);
				oldPosition = newPosition;

				if (GUI.changed)
					EditorUtility.SetDirty (target);
			}

			if (port.drawGizmos)
			{
				DrawPortGizmo(port);
			}
		}


		public override void OnInspectorGUI()
		{
			Port port = (Port)target;

            if (port.CacheInterfaceRecieversWithChangeCheck())
                EditorUtility.SetDirty(target);

            port.active = EditorGUILayout.ToggleLeft(new GUIContent("Active","Prevents connector from participating in attchment and influence events when set to false."),port.active);
			if (port.owner == null)
				EditorGUILayout.HelpBox("This port has no owner TerminusObject and will not function correctly.",MessageType.Error);

			port.owner = (TerminusObject)EditorGUILayout.ObjectField(new GUIContent("Owner","TerminusObject this port belongs to. Connector should always belong to some TerminusObject. Connector can be on different GameObject from its owner, as long as owner is designated."),port.owner,typeof(TerminusObject),true);
			port.useOwnerRigidbody = EditorGUILayout.ToggleLeft(new GUIContent("Use owners Rigidbody","Port will use TerminusObject.mainRigidbody for all physics-related activities if set to true. Attachment through welding rigidbodies is possible only with this type of connectors."),port.useOwnerRigidbody);

			if (nameProp.stringValue == "")
			{
				nameProp.stringValue = port.index.ToString();
				serializedObject.ApplyModifiedProperties();
			}

			/*
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(nameProp);
			if(EditorGUI.EndChangeCheck())
				serializedObject.ApplyModifiedProperties();
			*/
			port.connectorName = EditorGUILayout.TextField(new GUIContent("Name","Unique name of this connector. Single TerminusObject can't have two connectors with the same name."),port.connectorName);

			port.layer = EditorGUILayout.Popup(new GUIContent("Layer","Terminus layer of connector. Terminus layers are separate from Unity layers and are set up from Layers tab in Terminus Settings window. Determines behaviour of this connector and also what connectors can influence or be attached to it."),port.layer,ProjectManager.settings.GetLayersNamesAsGUIContent());
			/*
			if (port.portOptions.use2DPhysics)
			{
				EditorGUILayout.BeginHorizontal();
				port.connectorRigidbody2D = (Rigidbody2D)EditorGUILayout.ObjectField("2D Rigidbody",port.connectorRigidbody2D,typeof(Rigidbody2D),true);
				if (port.connectorRigidbody2D == null)
				{
					if (GUILayout.Button("Search",GUILayout.MaxWidth(50)))
					{
						port.connectorRigidbody2D = port.GetComponent<Rigidbody2D>();
						Transform searchTransform = port.transform;
						while (port.connectorRigidbody2D == null && searchTransform.parent != null)
						{
							searchTransform = searchTransform.parent;
							port.connectorRigidbody2D = searchTransform.GetComponent<Rigidbody2D>();
							if (searchTransform == port.owner.transform)
								break;
						}
					}
				}
				EditorGUILayout.EndHorizontal();
			}
			else
			{
				EditorGUILayout.BeginHorizontal();
				port.connectorRigidbody = (Rigidbody)EditorGUILayout.ObjectField("Rigidbody",port.connectorRigidbody,typeof(Rigidbody),true);
				if (port.connectorRigidbody == null)
				{
					if (GUILayout.Button("Search",GUILayout.MaxWidth(50)))
					{
						port.connectorRigidbody = port.GetComponent<Rigidbody>();
						Transform searchTransform = port.transform;
						while (port.connectorRigidbody == null && searchTransform.parent != null)
						{
							searchTransform = searchTransform.parent;
							port.connectorRigidbody = searchTransform.GetComponent<Rigidbody>();
							if (searchTransform == port.owner.transform)
								break;
						}
					}
				}
				EditorGUILayout.EndHorizontal();
			}
			*/
			if (port.portOptions.attachementPort && port.isAttachable)
				port.isCurrentAttachmentPort = EditorGUILayout.ToggleLeft(new GUIContent("Is attachment port at the moment","Indicates whether this port is current attachment port of owner TerminusObject. TerminusObject can only have one attachment port at the same time (unless multiple attachment ports toggle selected on owner TerminusObject)."),port.isCurrentAttachmentPort);
			port.offset = EditorGUILayout.Vector3Field("Position offset(local)",port.offset);

			EditorGUI.BeginChangeCheck();		
			Vector3 eulerAngles = EditorGUILayout.Vector3Field("Rotation(local)",port.rotation.eulerAngles);	
			if (EditorGUI.EndChangeCheck())
			{
				port.rotation = Quaternion.Euler(eulerAngles);	
				EditorUtility.SetDirty(port);
			}

			port.doNotMoveOwner = EditorGUILayout.ToggleLeft(new GUIContent("Do not move owner","If true, changing port position will not move owner TerminusObject. Main use is when owner is LongConnection-type object (for example, strut)."),port.doNotMoveOwner);

			if (port.overrideLayerOptions = EditorGUILayout.ToggleLeft(new GUIContent("Override layer options","Allows to change layer properties only for this port."),port.overrideLayerOptions))
			{
				EditorGUI.indentLevel++;
				SettingsWindow.DrawLayerOptions(port.overridenOptions,ref messagingFoldout);
				if (GUILayout.Button("Set settings to layer default"))
				{
					port.overridenOptions.inspectorColor = ProjectManager.settings.layers[port.layer].inspectorColor;
					port.overridenOptions.acceptsConnectionFromLayers = ProjectManager.settings.layers[port.layer].acceptsConnectionFromLayers;
					port.overridenOptions.attachementPort = ProjectManager.settings.layers[port.layer].attachementPort;
					port.overridenOptions.cyclingAttachementPort = ProjectManager.settings.layers[port.layer].cyclingAttachementPort;
					port.overridenOptions.attachmentType = ProjectManager.settings.layers[port.layer].attachmentType;
					port.overridenOptions.use2DPhysics = ProjectManager.settings.layers[port.layer].use2DPhysics;
					port.overridenOptions.jointPrefab = ProjectManager.settings.layers[port.layer].jointPrefab;
					port.overridenOptions.jointPrefab2D = ProjectManager.settings.layers[port.layer].jointPrefab2D;
					port.overridenOptions.snapRadius = ProjectManager.settings.layers[port.layer].snapRadius;
					port.overridenOptions.snapConeAngle = ProjectManager.settings.layers[port.layer].snapConeAngle;
					port.overridenOptions.orientationType = ProjectManager.settings.layers[port.layer].orientationType;
					port.overridenOptions.sendMessagesToPorts = ProjectManager.settings.layers[port.layer].sendMessagesToPorts;
					port.overridenOptions.sendMessagesToOwnerObjects = ProjectManager.settings.layers[port.layer].sendMessagesToOwnerObjects;
					port.overridenOptions.sendMessagesToGameObject = ProjectManager.settings.layers[port.layer].sendMessagesToGameObject;
					port.overridenOptions.useInfluenceMessaging = ProjectManager.settings.layers[port.layer].useInfluenceMessaging;
					port.overridenOptions.influenceRadius = ProjectManager.settings.layers[port.layer].influenceRadius;
					port.overridenOptions.influenceConeAngle = ProjectManager.settings.layers[port.layer].influenceConeAngle;
					port.overridenOptions.influenceMessageEnter = ProjectManager.settings.layers[port.layer].influenceMessageEnter;
					port.overridenOptions.influenceMessageExit = ProjectManager.settings.layers[port.layer].influenceMessageExit;
					port.overridenOptions.rotationType = ProjectManager.settings.layers[port.layer].rotationType;
					port.overridenOptions.rotationLimits = ProjectManager.settings.layers[port.layer].rotationLimits;
					port.overridenOptions.rotationStep = ProjectManager.settings.layers[port.layer].rotationStep;
				}
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.HelpBox("Editor options",MessageType.None);
			port.drawGizmos = EditorGUILayout.ToggleLeft("Show gizmo",port.drawGizmos);
			port.drawControls = EditorGUILayout.ToggleLeft("Show controls",port.drawControls);
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();

			if (GUI.changed)
				EditorUtility.SetDirty (target);

			EditorGUILayout.Space();

			Color oldCol = GUI.color;
			GUI.color = Color.green;

			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.HelpBox("Current state (read-only)",MessageType.None);

			GUI.enabled = false;

			EditorGUILayout.IntField(new GUIContent("Index","List index of this port inside owner connectors list."),port.index);
			EditorGUILayout.ToggleLeft("Can be attached",port.isAttachable);
			EditorGUILayout.ToggleLeft("Can accept attachments",port.isAccepting);
			EditorGUILayout.ToggleLeft("Occupied",port.isOccupied);
			if (port.attachmentCandidate != null)
				EditorGUILayout.ObjectField("Attachment candidate",port.attachmentCandidate,typeof(Connector),true);

			if (PrefabUtility.GetPrefabType(port.gameObject) != PrefabType.Prefab && Application.isPlaying && port.portOptions.useInfluenceMessaging)
			{
				EditorGUILayout.Space();
				EditorGUI.indentLevel++;
				if (influenceFoldout = EditorGUILayout.Foldout(influenceFoldout,"Influences("+port.influences.Count.ToString()+")"))
				{
					GUI.enabled = false;
					for (int i = 0; i < port.influences.Count; i++)
					{
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.ObjectField("",port.influences[i].otherConnector.owner,typeof(Connector),true);
						EditorGUILayout.TextField(port.influences[i].otherConnector.connectorName,GUILayout.MaxWidth(65));
						EditorGUILayout.TextField(port.influences[i].sqrDistance.ToString(),GUILayout.MaxWidth(50));
						EditorGUILayout.EndHorizontal();
					}
					GUI.enabled = true;
				}
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndVertical();
			GUI.enabled = true;

			EditorGUILayout.Space();

			if (port.isOccupied)
			{
				EditorGUILayout.Space();
				EditorGUILayout.BeginVertical("box");
				EditorGUILayout.HelpBox("Current attachment (read-only)",MessageType.None);

				GUI.enabled = false;

				EditorGUILayout.EnumPopup("Type",port.attachmentInfo.attachmentType);
				EditorGUILayout.ObjectField("Attached to",port.attachmentInfo.otherConnector,typeof(Connector),true);
				if (port.attachmentInfo.joint != null)
					EditorGUILayout.ObjectField("Attachment joint",port.attachmentInfo.joint,typeof(Component),true);
				EditorGUILayout.ToggleLeft("Hierarchy attachment",port.attachmentInfo.hierarchyAttachment);
				EditorGUILayout.ToggleLeft("Rigidbody welding attachment",port.attachmentInfo.hierarchyAttachment);
				EditorGUILayout.EndVertical();
				GUI.enabled = true;
			}

			EditorGUILayout.Space();


			GUI.color = oldCol;
		}

		void OnEnable () 
		{
			Port port = (Port)target;

			oldRotation = port.transform.rotation * port.rotation;
			oldPosition = port.transform.TransformPoint(port.offset);

			if (port.owner == null)
			{
				Transform tr = port.transform;
				while (tr.gameObject.GetComponent<TerminusObject>() == null && tr.parent != null)
					tr = tr.parent;
				port.owner = tr.gameObject.GetComponent<TerminusObject>();
				port.owner.RegisterConnector(port);
			}
			else
			{
				if (port.owner.connectors == null)
					port.owner.connectors = new List<Connector>();
				if (!port.owner.connectors.Contains(port))
					port.owner.RegisterConnector(port);
			}

			/*
			if (port.connectorRigidbody2D == null)
			{
				port.connectorRigidbody2D = port.GetComponent<Rigidbody2D>();
				Transform searchTransform = port.transform;
				while (port.connectorRigidbody2D == null && searchTransform.parent != null)
				{
					searchTransform = searchTransform.parent;
					port.connectorRigidbody2D = searchTransform.GetComponent<Rigidbody2D>();
					if (searchTransform == port.owner.transform)
						break;
				}
			}
			if (port.connectorRigidbody == null)
			{
				port.connectorRigidbody = port.GetComponent<Rigidbody>();
				Transform searchTransform = port.transform;
				while (port.connectorRigidbody == null && searchTransform.parent != null)
				{
					searchTransform = searchTransform.parent;
					port.connectorRigidbody = searchTransform.GetComponent<Rigidbody>();
					if (searchTransform == port.owner.transform)
						break;
				}
			}
			*/
			/*
			handleShifts = new Vector3[5];
			handleShifts[0] = new Vector3(0,-0.3f,0);
			handleShifts[1] = new Vector3(0,-0.3f,0.3f);
			handleShifts[2] = new Vector3(0,0,0.15f);
			handleShifts[3] = new Vector3(0,0.3f,0.15f);
			handleShifts[4] = new Vector3(0,0.3f,0);
			*/

			nameProp = serializedObject.FindProperty("connectorName");
		}
		
	}
}