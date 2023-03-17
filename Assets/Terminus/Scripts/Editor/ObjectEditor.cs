using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;

namespace Terminus.Editors
{
	[CustomEditor( typeof(TerminusObject))]
	public class TerminusObjectEditor : Editor {

		private ReorderableList list;

		private bool isPrefab = true;


		private SerializedProperty connectorsProp;
		private SerializedProperty attachHandProp;
		private SerializedProperty stateHandProp;
		private SerializedProperty nameProp;
		private SerializedProperty jointsProp;
		private SerializedProperty joints2DProp;
		private SerializedProperty uiInfoProp;
		private SerializedProperty rbodyInfo;
		private SerializedProperty rbodyInfo2D;        

        private bool childrenFold;
		private bool sidewaysFold;
		private bool downTreeFold;
		private bool upTreeFold;
		private bool symObjFold;
		private bool symGroupsFold;
		private bool influenceFold;



		public void OnSceneGUI()
		{
			TerminusObject obj = (TerminusObject)target;

			if (obj.drawGizmos)
			{
				
				for (int i = 0; i < obj.connectors.Count; i++)
				{
					if (obj.connectors[i].GetType() == typeof(Port))
					{						
						Port port = (Port)(obj.connectors[i]);
						PortEditor.DrawPortGizmo(port);
					}
				}

			}
		}


		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			TerminusObject obj = (TerminusObject)target;

			if (isPrefab = PrefabUtility.GetPrefabType(obj.gameObject) == PrefabType.Prefab)
			{
				if (nameProp.stringValue == "")
				{
					nameProp.stringValue = obj.gameObject.name;
					serializedObject.ApplyModifiedProperties();
				}
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(nameProp);
				if(EditorGUI.EndChangeCheck())
					serializedObject.ApplyModifiedProperties();
				if ((ProjectManager.settings.prefabs.FindIndex(rec => rec.prefab == obj) == -1)
				    && GUILayout.Button("Add to assembly prefabs"))
				{
					Settings.AssemblyPrefabOptions prefOpt =  new Settings.AssemblyPrefabOptions();
					prefOpt.prefab = obj;
					ProjectManager.settings.prefabs.Add(prefOpt);
				}
			}


            if (obj.CacheInterfaceRecieversWithChangeCheck())
                EditorUtility.SetDirty(target);

			EditorGUILayout.Space();

			if (obj.mainRigidbody2D == null)
				obj.mainRigidbody = (Rigidbody)EditorGUILayout.ObjectField("Main rigidbody",obj.mainRigidbody,typeof(Rigidbody),true);
			if (obj.mainRigidbody == null)
				obj.mainRigidbody2D = (Rigidbody2D)EditorGUILayout.ObjectField("Main 2D rigidbody",obj.mainRigidbody2D,typeof(Rigidbody2D),true);

			EditorGUILayout.Space();

			obj.mode = (TerminusObject.Modes)EditorGUILayout.EnumPopup("Mode",obj.mode);
			obj.doNotAutoUpdate = EditorGUILayout.ToggleLeft("Do not auto-update",obj.doNotAutoUpdate);
			obj.attachAutomatically = EditorGUILayout.ToggleLeft("Attach automatically",obj.attachAutomatically);
			//obj.changeModeOnAttached = EditorGUILayout.ToggleLeft("Change mode when attached",obj.changeModeOnAttached);
			if (obj.longConnection = EditorGUILayout.ToggleLeft("Long connection (strut) object",obj.longConnection))
			{
				if (obj.GetComponent<LongConnection>() == null)
				{
					EditorGUI.indentLevel++;
					EditorGUILayout.LabelField("Can't find Long Connection component");
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Add standard:");
					if (GUILayout.Button("LineRenderer"))
					{
						obj.gameObject.AddComponent<LongConnectionLRend>();
						obj.gameObject.AddComponent<LineRenderer>();
					}
					if (GUILayout.Button("Mesh"))
					{
						LongConnectionMesh lconn = obj.gameObject.AddComponent<LongConnectionMesh>();
						lconn.meshFilter = obj.gameObject.AddComponent<MeshFilter>();
						lconn.meshRenderer = obj.gameObject.AddComponent<MeshRenderer>();
					}
					EditorGUILayout.EndHorizontal();
					EditorGUI.indentLevel--;
				}
				if (obj.connectors.Count < 2)
					EditorGUILayout.HelpBox("LongConnection object should contain at least 2 ports. First port should be on this GameObject.", MessageType.Warning);
			}
			if (!(obj.doNotConnectSideways = EditorGUILayout.ToggleLeft("Do not connect sideways",obj.doNotConnectSideways)))
			{
				EditorGUI.indentLevel++;
				obj.includeEnvironmentInSidewaySearch = EditorGUILayout.ToggleLeft(new GUIContent("Include environment in search","Should raycasting be performed to try and find suitable environment attachment point when searching for sideways attachments?"),obj.includeEnvironmentInSidewaySearch);
				EditorGUI.indentLevel--;
			}
			if (obj.autoReparentToSideways = EditorGUILayout.ToggleLeft("Auto-reparent to sideways attachments",obj.autoReparentToSideways))
			{
				EditorGUI.indentLevel++;
				obj.includeChildrenInAutoSideways = EditorGUILayout.ToggleLeft(new GUIContent("Include children during auto-reparent","Should sideways attachments of children of this object be included in new parent search? Not that if said child is found, Terminus will reverse relationships on the branch between this object and child."),obj.includeChildrenInAutoSideways);
				EditorGUI.indentLevel--;
			}

			obj.multipleAttachmentPortsAllowed = EditorGUILayout.ToggleLeft("Multiple attachment ports allowed",obj.multipleAttachmentPortsAllowed);

			EditorGUI.BeginChangeCheck();
			//EditorGUILayout.PropertyField(connectorsProp,true);

			if (obj.connectors.GroupBy(rec => rec.connectorName).Where(rec => rec.Count() > 1).Any())
			{
				EditorGUILayout.HelpBox("There's connectors with duplicate names!", MessageType.Error);
			}

			list.DoLayoutList();
			//obj.currentAttachmentPort = (Port)EditorGUILayout.ObjectField("Current attachment port",obj.currentAttachmentPort,typeof(Port),true);
			obj.connectors = obj.connectors.Where(item => item != null).ToList();

			EditorGUILayout.Space();
			if (obj.createContainerWhenRoot = EditorGUILayout.ToggleLeft("Create container when this is root object",obj.createContainerWhenRoot))
				EditorGUILayout.ObjectField("Container",obj.container,typeof(Container),true);
			else 
			{
				/*
				if (!obj.longConnection && obj.connectors.FindIndex(rec => rec.portOptions.attachmentType == Settings.AttachmentTypes.physic_joints) >= 0)
					EditorGUILayout.HelpBox("One of this objects connectors uses physics-only connection. It is highly recommended to use containers when working with this type of connection.", MessageType.Warning);
				*/
			}


			EditorGUILayout.Space();

			GUI.enabled = Application.isPlaying && PrefabUtility.GetPrefabType(obj) != PrefabType.Prefab;
			if (obj.isPlaymodeClone)
				obj.isPlaymodeClone = EditorGUILayout.ToggleLeft("Is playmode clone",obj.isPlaymodeClone);
			else
				obj.inPlaymode = EditorGUILayout.ToggleLeft("In playmode",obj.inPlaymode);
			GUI.enabled = true;

			if (attachHandProp.objectReferenceValue == null)
			{
				if (obj.GetComponent<AttachmentHandler>() != null)
				{
					attachHandProp.objectReferenceValue = obj.GetComponent<AttachmentHandler>();
					EditorGUILayout.PropertyField(attachHandProp);
				}
				else
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PropertyField(attachHandProp);
					if (GUILayout.Button("Add standard",GUILayout.MaxWidth(100)))
					{
						AttachmentHandler attachHandler = obj.gameObject.AddComponent<StandardAttachmentHandler>();
						attachHandProp.objectReferenceValue = attachHandler;
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			else
			{
				EditorGUILayout.PropertyField(attachHandProp);
			}

			if (stateHandProp.objectReferenceValue == null)
			{
				if (obj.GetComponent<StateHandler>() != null)
				{
					stateHandProp.objectReferenceValue = obj.GetComponent<StateHandler>();
					EditorGUILayout.PropertyField(stateHandProp);
					obj.inAssemblyState = EditorGUILayout.ToggleLeft("In assembly state",obj.inAssemblyState);
				}
				else
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PropertyField(stateHandProp);
					if (GUILayout.Button("Add standard",GUILayout.MaxWidth(100)))
					{
						StateHandler stateHandler = obj.gameObject.AddComponent<StandardStateHandler>();
						stateHandProp.objectReferenceValue = stateHandler;
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			else
			{
				EditorGUILayout.PropertyField(stateHandProp);
				obj.inAssemblyState = EditorGUILayout.ToggleLeft("In assembly state",obj.inAssemblyState);
			}
			if (!Application.isPlaying)
			{
				Color oldColor = GUI.color;
				if ((obj.originalComponentsParams == null || obj.originalComponentsParams.Count == 0)
					&& (obj.getAttachmentHandler != null || obj.getStateHandler != null)
					||
					(obj.mainRigidbody != null && (obj.rigidbodyInfo == null || obj.rigidbodyInfo.mass == 0))
					||
					(obj.mainRigidbody != null && (obj.rigidbodyInfo == null || obj.rigidbodyInfo.mass == 0)))
				{
					GUI.color = Color.red;
				}
				if (GUILayout.Button(new GUIContent("Save original component states","Saves states of rigidbodies, colliders and renderers from AttachmentHandler and StateHandler. Also fills full rigidbody info for MainRigibody and MainRigidbody2D. Press this button when you finished setting up this object.")))
				{
					obj.SaveOriginalComponentState();
					obj.FillRigidbodyInfo();
					obj.FillRigidbodyInfo2D();                    
					EditorUtility.SetDirty(target);
				}
				GUI.color = oldColor;
				/*
				if (obj.originalComponentsParams != null)
					EditorGUILayout.HelpBox("Original comp records: "  + obj.originalComponentsParams.Count.ToString(), MessageType.Info);
				else
					EditorGUILayout.HelpBox("Null", MessageType.Warning);
					*/
			}

			if (obj.useSymmetryGroups = EditorGUILayout.ToggleLeft("Use symmetry port groups",obj.useSymmetryGroups))
			{
				EditorGUI.indentLevel++;
				if (symGroupsFold = EditorGUILayout.Foldout(symGroupsFold,"Symmetry groups"))
				{
					List<string> temp = new List<string>();
					temp.Add ("Add connector to group");
					temp.AddRange(obj.connectors.Select(rec => rec.connectorName + " : " + " (" + rec.gameObject.name+")").ToList());
					string[] addStrings = temp.ToArray();
					temp.RemoveAt(0);
					temp.Add("Remove connector from group");
					string[] popupStrings = temp.ToArray();
					if (obj.symmetryGroups == null)
						obj.symmetryGroups = new List<TerminusObject.SymmetricGroup>();
					for (int i = 0; i < obj.symmetryGroups.Count; i++)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.BeginVertical("box");
						for (int x = 0; x < obj.symmetryGroups[i].connectors.Count; x++)
						{
							EditorGUI.BeginChangeCheck();
							int sel = EditorGUILayout.Popup(obj.symmetryGroups[i].connectors[x].index,popupStrings);
							if (EditorGUI.EndChangeCheck())
							{
								if (sel == popupStrings.Length-1)
								{
									obj.symmetryGroups[i].connectors.Remove(obj.symmetryGroups[i].connectors[x]);
								}
								else
								{
									obj.symmetryGroups[i].connectors[x] = obj.connectors[sel];
								}
								EditorUtility.SetDirty (target);
							}
						}
						int selAdd = EditorGUILayout.Popup(0,addStrings);
						if (selAdd > 0)
						{
							obj.symmetryGroups[i].connectors.Add(obj.connectors[selAdd-1]);
							EditorUtility.SetDirty (target);
						}
						if (GUILayout.Button("Delete this group"))
						{
							obj.symmetryGroups.RemoveAt(i);
							EditorUtility.SetDirty (target);
						}
						EditorGUILayout.EndVertical();
						EditorGUI.indentLevel--;
						EditorGUILayout.Space();
					}
					if (GUILayout.Button("Add new group"))
					{
						obj.symmetryGroups.Add(new TerminusObject.SymmetricGroup());
						EditorUtility.SetDirty (target);
					}

				}
				EditorGUI.indentLevel--;
			}

			if (obj.mainRigidbody != null)
				EditorGUILayout.PropertyField(jointsProp,true);
			if (obj.mainRigidbody2D != null)
				EditorGUILayout.PropertyField(joints2DProp,true);

			EditorGUILayout.PropertyField(uiInfoProp,true);

			if(EditorGUI.EndChangeCheck())
				serializedObject.ApplyModifiedProperties();

			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.HelpBox("Editor options",MessageType.None);
			obj.drawGizmos = EditorGUILayout.ToggleLeft("Show ports gizmos",obj.drawGizmos);
			EditorGUILayout.EndVertical();

			if (GUI.changed)
				EditorUtility.SetDirty (target);


			Color oldCol = GUI.color;
			GUI.color = Color.cyan;

			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.HelpBox("Read-only info",MessageType.None);
			GUI.enabled = false;
			EditorGUILayout.ObjectField("Tree root",obj.treeRoot,typeof(TerminusObject),true);
			EditorGUILayout.ObjectField("Parent",obj.parent.otherConnector,typeof(Connector),true);
			EditorGUI.indentLevel++;
			List<AttachmentInfo> tempList = obj.children;
			if (childrenFold = EditorGUILayout.Foldout(childrenFold,"Children ("+tempList.Count.ToString()+")"))
			{
				for (int i = 0; i < tempList.Count; i++)
					EditorGUILayout.ObjectField("",tempList[i].otherConnector,typeof(Connector),true);
			}
			tempList = obj.sidewaysConnections;
			if (sidewaysFold = EditorGUILayout.Foldout(sidewaysFold,"Sideways connections ("+tempList.Count.ToString()+")"))
			{
				for (int i = 0; i < tempList.Count; i++)
					EditorGUILayout.ObjectField("",tempList[i].otherConnector,typeof(Connector),true);
			}
			tempList = obj.treeListDown;
			if (downTreeFold = EditorGUILayout.Foldout(downTreeFold,"Full down-tree connections ("+tempList.Count.ToString()+")"))
			{
				for (int i = 0; i < tempList.Count; i++)
					EditorGUILayout.ObjectField("",tempList[i].otherConnector,typeof(Connector),true);
			}
			tempList = obj.treeListUp;
			if (upTreeFold = EditorGUILayout.Foldout(upTreeFold,"Full up-tree connections ("+tempList.Count.ToString()+")"))
			{
				for (int i = 0; i < tempList.Count; i++)
					EditorGUILayout.ObjectField("",tempList[i].otherConnector,typeof(Connector),true);
			}
			if (symObjFold = EditorGUILayout.Foldout(symObjFold,"Symmetric sibling objects("+obj.symmetricSiblings.Count.ToString()+")"))
			{
				for (int i = 0; i < obj.symmetricSiblings.Count; i++)
					EditorGUILayout.ObjectField("",obj.symmetricSiblings[i],typeof(TerminusObject),true);
			}
			EditorGUILayout.Space();
			if (!isPrefab && Application.isPlaying && obj.connectors.FindIndex(rec => rec.portOptions.useInfluenceMessaging) >= 0 && (influenceFold = EditorGUILayout.Foldout(influenceFold,"Influences("+obj.allInfluences.Count.ToString()+")")))
			{
				for (int i = 0; i < obj.allInfluences.Count; i++)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.ObjectField("",obj.allInfluences[i].otherConnector.owner,typeof(Connector),true);
					EditorGUILayout.TextField(obj.allInfluences[i].otherConnector.connectorName,GUILayout.MaxWidth(65));
					EditorGUILayout.LabelField("<->",GUILayout.MaxWidth(20));
					EditorGUILayout.TextField(obj.allInfluences[i].thisConnector.connectorName,GUILayout.MaxWidth(65));
					EditorGUILayout.TextField(obj.allInfluences[i].sqrDistance.ToString(),GUILayout.MaxWidth(50));
					EditorGUILayout.EndHorizontal();
				}
			}


			EditorGUILayout.Space();

			EditorGUI.indentLevel--;

			EditorGUILayout.ToggleLeft("Is playmode clone",obj.isPlaymodeClone);

			if (obj.playmodeClone != null)
				EditorGUILayout.ObjectField("Playmode clone",obj.playmodeClone,typeof(TerminusObject),true);

			if (obj.playmodeOriginal != null)
				EditorGUILayout.ObjectField("Playmode original",obj.playmodeOriginal,typeof(TerminusObject),true);

			//EditorGUI.indentLevel--;

			GUI.color = oldCol;

			GUI.enabled = true;

			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(rbodyInfo,true);
			EditorGUILayout.PropertyField(rbodyInfo2D,true);
			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();
            
        }

		void OnEnable () 
		{
			((TerminusObject)target).CleanUpConnectors();

			list = new ReorderableList(serializedObject,serializedObject.FindProperty("connectors"),true,true,false,false);

			list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				var element = list.serializedProperty.GetArrayElementAtIndex(index);
				Connector connector = (Connector)(element.objectReferenceValue);
				rect.y += 2;
				/*EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PropertyField(element);
				EditorGUILayout.EndHorizontal();*/
				if (!isPrefab)
				{
					if (connector is Port)
					{
						Port port = (Port)connector;
						if (connector.attachmentsInfo.Count == 0)
						{
							EditorGUI.LabelField(new Rect(rect.x, rect.y, 15, EditorGUIUtility.singleLineHeight),index.ToString());
							((Port)connector).isCurrentAttachmentPort = EditorGUI.Toggle(new Rect(rect.x+13,rect.y,12,EditorGUIUtility.singleLineHeight),new GUIContent("","Is attachment port"),((Port)connector).isCurrentAttachmentPort);
							EditorGUI.LabelField(new Rect(rect.x+25, rect.y, 50, EditorGUIUtility.singleLineHeight),connector.connectorName);
							EditorGUI.PropertyField(new Rect(rect.x+75, rect.y, rect.width-175, EditorGUIUtility.singleLineHeight), element, GUIContent.none);

							GUI.enabled = port.CheckPossibleConnections(null,false);
							if (GUI.Button(new Rect(rect.x+rect.width-100, rect.y, 50, EditorGUIUtility.singleLineHeight), "Align"))
							{
								port.Align(port.attachmentCandidatePosition,port.attachmentCandidateRotation);
							}
							if (GUI.Button(new Rect(rect.x+rect.width-50, rect.y, 50, EditorGUIUtility.singleLineHeight), "Attach"))
							{
								port.Align(port.attachmentCandidatePosition,port.attachmentCandidateRotation);
								if (port.owner.longConnection && port.index == 0)
									port.AttachToSideways(port.attachmentCandidate);
								else
									port.AttachTo();
								EditorUtility.SetDirty(port);
								EditorUtility.SetDirty(port.owner);
								EditorUtility.SetDirty(port.attachmentCandidate);
								EditorUtility.SetDirty(port.attachmentCandidate.owner);
								if (Manager.Instance != null)
									EditorUtility.SetDirty(Manager.Instance);
							}
							GUI.enabled = true;
						}
						else
						{
							EditorGUI.LabelField(new Rect(rect.x, rect.y, 15, EditorGUIUtility.singleLineHeight),index.ToString());
							((Port)connector).isCurrentAttachmentPort = EditorGUI.Toggle(new Rect(rect.x+13,rect.y,12,EditorGUIUtility.singleLineHeight),new GUIContent("","Is attachment port"),((Port)connector).isCurrentAttachmentPort);
							EditorGUI.LabelField(new Rect(rect.x+25, rect.y, 50, EditorGUIUtility.singleLineHeight),connector.connectorName);
							float halfWidth = (rect.width - 15 - 60 - 50 - 30)/2;
							EditorGUI.PropertyField(new Rect(rect.x+75, rect.y, halfWidth, EditorGUIUtility.singleLineHeight), element, GUIContent.none);
							string attType = "";
							switch (((Port)connector).attachmentInfo.attachmentType)
							{
							case AttachmentInfo.Types.child:
								attType = "->";
								break;
							case AttachmentInfo.Types.parent:
								attType = " <-";
								break;
							case AttachmentInfo.Types.sideway:
								attType = "<->";
								break;
							}
							EditorGUI.LabelField(new Rect(rect.x+75+halfWidth, rect.y, 30, EditorGUIUtility.singleLineHeight),attType);
							EditorGUI.ObjectField(new Rect(rect.x+75+30+halfWidth, rect.y, halfWidth, EditorGUIUtility.singleLineHeight),((Port)connector).attachmentInfo.otherConnector, typeof(Connector),true);

							if (GUI.Button(new Rect(rect.x+rect.width-50, rect.y, 50, EditorGUIUtility.singleLineHeight), "Detach"))
							{
								Connector otherConn = port.attachmentInfo.otherConnector;
								port.Detach();
								EditorUtility.SetDirty(port);
								EditorUtility.SetDirty(port.owner);
								EditorUtility.SetDirty(otherConn);
								EditorUtility.SetDirty(otherConn.owner);
								if (Manager.Instance != null)
									EditorUtility.SetDirty(Manager.Instance);
							}
						}
					}
					else
					{
						if (connector is Surface)
						{
							Surface surface = (Surface)connector;
							EditorGUI.LabelField(new Rect(rect.x, rect.y, 15, EditorGUIUtility.singleLineHeight),index.ToString());
							EditorGUI.LabelField(new Rect(rect.x+15, rect.y, 60, EditorGUIUtility.singleLineHeight),connector.connectorName);
							EditorGUI.PropertyField(new Rect(rect.x+75, rect.y, rect.width-75-75, EditorGUIUtility.singleLineHeight), element, GUIContent.none);

							GUI.enabled = surface.attachmentsInfo.Count > 0;
							if (GUI.Button(new Rect(rect.x+rect.width-75, rect.y, 75, EditorGUIUtility.singleLineHeight), "Detach all"))
							{
								AttachmentInfo[] attInfo = surface.attachmentsInfo.ToArray();
								for (int i = 0; i < attInfo.Length; i++)
								{
									((Port)attInfo[i].otherConnector).Detach();
									EditorUtility.SetDirty(attInfo[i].otherConnector);
									EditorUtility.SetDirty(attInfo[i].otherConnector.owner);
								}
								EditorUtility.SetDirty(surface);
								EditorUtility.SetDirty(surface.owner);

								if (Manager.Instance != null)
									EditorUtility.SetDirty(Manager.Instance);
							}
							GUI.enabled = true;
						}					
					}
				}
				else
				{
					EditorGUI.LabelField(new Rect(rect.x, rect.y, 15, EditorGUIUtility.singleLineHeight),index.ToString());
					//((Port)connector).isCurrentAttachmentPort = EditorGUI.Toggle(new Rect(rect.x+13,rect.y,12,EditorGUIUtility.singleLineHeight),new GUIContent("","Is attachment port"),((Port)connector).isCurrentAttachmentPort);
					EditorGUI.LabelField(new Rect(rect.x+25, rect.y, 50, EditorGUIUtility.singleLineHeight),connector.connectorName);
					EditorGUI.PropertyField(new Rect(rect.x+75, rect.y, rect.width-75, EditorGUIUtility.singleLineHeight), element, GUIContent.none);
				}
			};
			list.drawHeaderCallback = (Rect rect) => {
				EditorGUI.LabelField(rect, "Connectors");
			};
			list.onSelectCallback = (ReorderableList l) => {
				var prefab = l.serializedProperty.GetArrayElementAtIndex(l.index).objectReferenceValue as GameObject;
				if (prefab) EditorGUIUtility.PingObject(prefab.gameObject);
			};
		

			//connectorsProp = serializedObject.FindProperty("connectors");
			attachHandProp = serializedObject.FindProperty("attachmentHandler");
			stateHandProp = serializedObject.FindProperty("stateHandler");
			nameProp = serializedObject.FindProperty("assemblyPartName");

			jointsProp = serializedObject.FindProperty("jointsConnectedToRigidbody");
			joints2DProp = serializedObject.FindProperty("joints2DConnectedToRigidbody");

			uiInfoProp = serializedObject.FindProperty("uiInfo");

			rbodyInfo = serializedObject.FindProperty("rigidbodyInfo");
			rbodyInfo2D = serializedObject.FindProperty("rigidbodyInfo2D");
            

            /*
			handleShifts = new Vector3[5];
			handleShifts[0] = new Vector3(0,-0.3f,0);
			handleShifts[1] = new Vector3(0,-0.3f,0.3f);
			handleShifts[2] = new Vector3(0,0,0.15f);
			handleShifts[3] = new Vector3(0,0.3f,0.15f);
			handleShifts[4] = new Vector3(0,0.3f,0);
			*/
        }
		
	}
}