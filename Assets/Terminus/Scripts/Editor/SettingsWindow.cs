using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Terminus.Editors
{
	public class SettingsWindow : EditorWindow {

		//SerializedObject serializedObject;
		//SerializedProperty layersProp;
		int mode = 0;
		Vector2 scrollPos;
		List<bool> layerFoldouts;
		List<bool> messagingFoldouts;
		bool[] paramsFoldouts;
		bool[][] parametersFoldouts;        

		private GUIStyle ToggleButtonStyleNormal = null;
		private GUIStyle ToggleButtonStyleToggled = null;

        private Settings.AssemblyParameterInfo oldParamInfo = new Settings.AssemblyParameterInfo();

        public static void DrawLayerOptions(Settings.LayerPortOptions layer,ref bool messFoldout)
		{
			layer.inspectorColor = EditorGUILayout.ColorField(new GUIContent("Gizmo color","Color of connectors gizmos shown in the editor preview window. Does not affect gameplay"),layer.inspectorColor);
			layer.acceptsConnectionFromLayers = EditorGUILayout.MaskField(new GUIContent("Accepts connection from","Connectors from which layers can be attached to connectors on this layer."),layer.acceptsConnectionFromLayers,ProjectManager.settings.GetLayersNames());
			if (layer.attachementPort = EditorGUILayout.ToggleLeft(new GUIContent("Can be attachment port","Can be TerminusObject.currentAttachmentPort. Only one of connectors(on single TerminusObject) can be attachment port at the same time. See script reference for more info."),layer.attachementPort))
			{
				EditorGUI.indentLevel++;
				layer.cyclingAttachementPort = EditorGUILayout.ToggleLeft(new GUIContent("Can be cycled","Ports belonging to this layer can be made CurrentAttachmentPort by calling TerminusObject.SetNextAttachmentPort"),layer.cyclingAttachementPort);
				EditorGUI.indentLevel--;
			}
			layer.attachmentPriority = EditorGUILayout.FloatField(new GUIContent("Attachment priority","Placer component will prioritize ports with higher priority, even if they're more distant from their attachment candidate. Used only when TerminusObject.multipleAttachmentPortsAllowed is true."),layer.attachmentPriority);
			if ((layer.attachmentType = (Settings.AttachmentTypes)EditorGUILayout.EnumPopup(new GUIContent("Attachment type","Type of action pefromed when Port from this layer is attached to other connector."),layer.attachmentType))
				== Settings.AttachmentTypes.rigidbody_welding)
			{
				EditorGUI.indentLevel++;
				layer.destroyRigidbodyImmediately = EditorGUILayout.ToggleLeft(new GUIContent("Destroy rigidbodies immedeately","Determines whether Destroy or DestroyImmediate will be called on welded rigidbody. Generally, should be set to true for buildings and false for vehicles. Immediate destruction helps with phantom forces but can cause errors when welded rigidbody affects other rigidbodies."),layer.destroyRigidbodyImmediately);
				EditorGUI.indentLevel--;
			}
			if (layer.use2DPhysics = EditorGUILayout.ToggleLeft(new GUIContent("Use 2D physics","Connector uses 2D physics when creating joints and welding rigidbodies, and 2D calculations for proximity and angle differnces."),layer.use2DPhysics))
				layer.jointPrefab2D = (Joint2D)EditorGUILayout.ObjectField(new GUIContent("Joint prefab(2D)","Leave empty for unbreakable distance joint"),layer.jointPrefab2D,typeof(Joint2D),false);
			else
				layer.jointPrefab = (Joint)EditorGUILayout.ObjectField(new GUIContent("Joint prefab","Leave empty for unbreakable fixed joint"),layer.jointPrefab,typeof(Joint),false);
			layer.orientationType = (Settings.OrientationTypes)EditorGUILayout.EnumPopup(new GUIContent("Orientation type","How Port orients itself when aligned with other port."
				+System.Environment.NewLine+"Exact - Ports rotations should match exactly."
				+System.Environment.NewLine+"TwoSided - Ports forward(ZY) planes (the one gizmo visualizes) should match."
				+System.Environment.NewLine+"Planar - Ports XY planes should match."),layer.orientationType);
			layer.snapRadius = EditorGUILayout.FloatField(new GUIContent("Snap radius","At what distance two connectors that can be attached according to AcceptsConnectionFromLayers can be considered attachment candidates by Port.CheckPossibleConnections"),layer.snapRadius);
			layer.snapConeAngle = EditorGUILayout.FloatField(new GUIContent("Snap cone angle","At what orientation difference (in degrees) two connectors that can be attached according to AcceptsConnectionFromLayers can be considered attachment candidates by Port.CheckPossibleConnections"),layer.snapConeAngle);

			//layer.onlyPlanarSnap = EditorGUILayout.ToggleLeft(new GUIContent("Only planar snap (no rotation around Z axis)","If true, Snap Cone Angle above considers difference between Z-axes of Connectors orientations only."), layer.onlyPlanarSnap); 

			EditorGUILayout.Space();

			if (messFoldout = EditorGUILayout.Foldout(messFoldout,new GUIContent("Messaging","Terminus can call designated methods on all viable components of objects specified below (similar to Unity SendMessage). Toggle in settings can be set to force Terminus to fire these messages in editor mode.")))
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.HelpBox("AttachmentInfo class will be provided as parameter",MessageType.Info);
				layer.sendMessagesToPorts = EditorGUILayout.ToggleLeft("Send message to participating connectors",layer.sendMessagesToPorts);
				layer.sendMessagesToOwnerObjects = EditorGUILayout.ToggleLeft("Send message to participating TerminusObject",layer.sendMessagesToOwnerObjects);
				if (layer.sendMessagesToGameObject = EditorGUILayout.ToggleLeft("Send message to global reciever",layer.sendMessagesToGameObject))
					EditorGUILayout.HelpBox("You can setup global reciever GameObject in  Manager settings",MessageType.Info);
				EditorGUILayout.Space();
				layer.onBeforeAttachmentMessage = EditorGUILayout.TextField("Before attachment message",layer.onBeforeAttachmentMessage);
				layer.onAfterAttachmentMessage = EditorGUILayout.TextField("After attachment message",layer.onAfterAttachmentMessage);
				layer.onBeforeDetachmentMessage = EditorGUILayout.TextField("Before detachment message",layer.onBeforeDetachmentMessage);
				layer.onAfterDetachmentMessage = EditorGUILayout.TextField("After detachment message",layer.onAfterDetachmentMessage);


				EditorGUILayout.Space();

				if (layer.useInfluenceMessaging = EditorGUILayout.ToggleLeft(new GUIContent("Use influence zone messaging","Influence messaging fills TerminusObject.allInfluences list and fires InfluenceEnter and InfluenceExit messages according to set influence zone. Influence zones DO NOT affect attachment behaviour, but Accepts Connection From Layers setting still affects possible influences."),layer.useInfluenceMessaging))
				{
					EditorGUI.indentLevel++;
					EditorGUILayout.HelpBox("ZoneInteraction class will be provided as parameter",MessageType.Info);
					layer.influenceRadius = EditorGUILayout.FloatField("Influence zone radius",layer.influenceRadius);
					layer.influenceConeAngle = EditorGUILayout.FloatField("Influence cone angle",layer.influenceConeAngle);
					layer.influenceMessageEnter = EditorGUILayout.TextField("Enter message",layer.influenceMessageEnter);
					layer.influenceMessageExit = EditorGUILayout.TextField("Exit message",layer.influenceMessageExit);
					EditorGUI.indentLevel--;
				}
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.Space();

			layer.rotationType = (Settings.RotationTypes)EditorGUILayout.EnumPopup(new GUIContent("Rotation type","Determines on how rotation of this port will be handled by Placer"),layer.rotationType);

			if (layer.rotationType == Settings.RotationTypes.self)
			{
				EditorGUI.indentLevel++;
				layer.rotationStep = EditorGUILayout.FloatField("Rotation step",layer.rotationStep);
				layer.rotationLimits = EditorGUILayout.Vector2Field(new GUIContent("Rotation limits (min,max)","Leave (0,0) to allow full-circle rotation"),layer.rotationLimits);
				EditorGUI.indentLevel--;
			}
			/*
			if (layer.canRotate = EditorGUILayout.ToggleLeft(new GUIContent("In-game rotation allowed","Ports belonging to this layer can change its Port.rotationShift."),layer.canRotate))
			{
				EditorGUI.indentLevel++;
				layer.rotationStep = EditorGUILayout.FloatField("Rotation step",layer.rotationStep);
				layer.rotationLimits = EditorGUILayout.Vector2Field(new GUIContent("Rotation limits (min,max)","Leave (0,0) to allow full-circle rotation"),layer.rotationLimits);
				EditorGUI.indentLevel--;
			}
			*/
		}


		[MenuItem ("Window/Terminus setup")]
		public static void  ShowWindow () {
			EditorWindow.GetWindow(typeof(SettingsWindow),false,"Terminus");
		}
		
		void OnGUI () {
			ToggleButtonStyleNormal = "Button";
			ToggleButtonStyleToggled = new GUIStyle(ToggleButtonStyleNormal);
			ToggleButtonStyleToggled.normal.background = ToggleButtonStyleToggled.active.background;

			Settings set = ProjectManager.settings;
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Layers", (mode == 0) ? ToggleButtonStyleToggled : ToggleButtonStyleNormal ))
				mode = 0;
			if (GUILayout.Button("Prefabs", (mode == 1) ? ToggleButtonStyleToggled : ToggleButtonStyleNormal ))
				mode = 1;
			if (GUILayout.Button("Settings", (mode == 2) ? ToggleButtonStyleToggled : ToggleButtonStyleNormal ))
				mode = 2;
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();

            Settings.AssemblyParameterInfo tempParamInfo;

			switch (mode)
			{
			case 0:
				    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
				    EditorGUILayout.BeginVertical();
				    EditorGUILayout.LabelField("Layers setup:");
				    //EditorGUILayout.PropertyField(layersProp,true);
				    int toDelete = -1;
				    for (int i = 0; i < set.layers.Count; i++)
				    {
					    EditorGUILayout.BeginVertical("box");
					    if (layerFoldouts[i] = EditorGUILayout.Foldout(layerFoldouts[i],i.ToString()+". "+set.layers[i].name))
					    {
						    //EditorGUILayout.LabelField(i.ToString()+")"+set.layers[i].name,EditorStyles.boldLabel);
						    set.layers[i].name = EditorGUILayout.TextField("Name",set.layers[i].name);
						    bool messFold = messagingFoldouts[i];
						    DrawLayerOptions(set.layers[i],ref messFold);
						    messagingFoldouts[i] = messFold;
						    EditorGUILayout.BeginHorizontal();
						    if (i !=0 && GUILayout.Button("Delete")
						        && (EditorUtility.DisplayDialog("Are you sure?",
						                                        "Deleting layer settings is irreversible.",
						                                        "YES, delete layer <"+set.layers[i].name+">",
	                                                              "NO, I changed my mind")))
							    toDelete = i;
						    EditorGUILayout.EndHorizontal();
					    }
					    EditorGUILayout.EndVertical();
					    EditorGUILayout.Space();
				    }
				    EditorGUILayout.BeginHorizontal();
				    if (GUILayout.Button("Add layer"))
				    {
					    set.layers.Add(new Settings.LayerPortOptions());
					    layerFoldouts.Add(true);
					    messagingFoldouts.Add(true);
				    }
				    if (GUILayout.Button("Set default settings"))
				    {
					    if (EditorUtility.DisplayDialog("Are you sure?",
					                                    "Performing this action is irreversible. You will lose all changes to settings you've made. Are you sure you want to continue?",
					                                    "YES, delete my changes",
					                                    "NO, I changed my mind"))
						    ProjectManager.DefaultSettings();
				    }
				    EditorGUILayout.EndHorizontal();
				    EditorGUILayout.EndVertical();
				    if (toDelete != -1)
				    {
					    set.layers.RemoveAt(toDelete);
					    layerFoldouts.RemoveAt(toDelete);
					    messagingFoldouts.RemoveAt(toDelete);
				    }
				    EditorGUILayout.EndScrollView();
				break;
			case 1:
				    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
				    for (int i = set.prefabs.Count-1; i >= 0; i--)
				    {
					    if (set.prefabs[i].prefab == null)
						    set.prefabs.RemoveAt(i);
				    }
				    if (set.prefabs.GroupBy(rec => rec.prefab.getPartName).Where(rec => rec.Count() > 1).Any())
				    {
					    EditorGUILayout.HelpBox("There are prefabs with duplicate names.", MessageType.Error);
				    }
				    if (paramsFoldouts.Length != set.prefabs.Count)
					    paramsFoldouts = new bool[set.prefabs.Count];
				    if (parametersFoldouts.Length != set.prefabs.Count)
					    parametersFoldouts = new bool[set.prefabs.Count][];                    
				    EditorGUILayout.BeginVertical();                    
				    for (int i = 0; i < set.prefabs.Count; i++)
				    {
					    EditorGUILayout.BeginVertical("box");
					    EditorGUILayout.LabelField(set.prefabs[i].prefab.getPartName);
                        set.prefabs[i].prefab = (TerminusObject)EditorGUILayout.ObjectField("Prefab: ",set.prefabs[i].prefab,typeof(TerminusObject),false);
					    Component[] components = set.prefabs[i].prefab.GetComponents<Component>().Where(rec => !(rec is TerminusObject || rec is Connector || rec is AttachmentHandler || rec is StateHandler || rec is LongConnection)).ToArray();
					    if (paramsFoldouts[i] = EditorGUILayout.Foldout(paramsFoldouts[i],"Serialized and editable parameters"))
					    {
						    EditorGUI.indentLevel++;
						    for (int x = 0; x < components.Length; x++)
						    {
							    System.Type type = components[x].GetType();
							    if (parametersFoldouts[i] == null || parametersFoldouts[i].Length != components.Length)
								    parametersFoldouts[i] = new bool[components.Length];

							    EditorGUILayout.BeginHorizontal();
							    parametersFoldouts[i][x] = EditorGUILayout.Foldout(parametersFoldouts[i][x], type.Name);
							    Settings.AssemblySerializableParameters assSerPar = ProjectManager.settings.prefabs[i].parameters.Find(rec => rec.component == type.Name);
							    if (assSerPar != null && typeof(IOnDeserialization).IsAssignableFrom(type))
							    {
                                    //assSerPar.sendMessage = EditorGUILayout.ToggleLeft("Send message",assSerPar.sendMessage,GUILayout.MaxWidth(120));
                                    GUI.enabled = assSerPar.Serialized();
                                    assSerPar.callOnDeserialization = EditorGUILayout.ToggleLeft(new GUIContent("Call OnDeserialization"), assSerPar.callOnDeserialization, GUILayout.MaxWidth(140));
                                    GUI.enabled = true;
                                }                                                      
                                
							    EditorGUILayout.EndHorizontal();
                                

                                float toggleWidth = 40;
                                float nameWidth = 400;
                                float labelNameWidth = 150;
                                float controlTypeWidth = 85;
                                float minMaxWidth = 120;
							    if (parametersFoldouts[i][x])
							    {                                    
                                    EditorGUI.indentLevel++;
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField(new GUIContent("Serializable", "Serializable with Terminus serialization"), GUILayout.Width(toggleWidth));
                                    EditorGUILayout.LabelField(new GUIContent("Editable", "Editable by player through UI"), GUILayout.Width(toggleWidth));
                                    EditorGUILayout.LabelField(new GUIContent("Parameter name", "Fields are marked with <>, properties are marked with []"), GUILayout.Width(nameWidth));                                    
                                    EditorGUILayout.EndHorizontal();
                                    System.Reflection.FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
								    foreach(System.Reflection.FieldInfo field in fields)
								    {
									    if (field.GetCustomAttributes(typeof(System.ObsoleteAttribute),false).Length == 0
									        && !field.FieldType.IsSubclassOf(typeof(UnityEngine.Object)))
									    {
                                            tempParamInfo = ProjectManager.settings.GetParameterInfo(i, type.Name, field.Name);
                                            if (tempParamInfo != null)
                                            {
                                                oldParamInfo.serializable = tempParamInfo.serializable;
                                                oldParamInfo.uiEditable = tempParamInfo.uiEditable;
                                                oldParamInfo.controlType = tempParamInfo.controlType;
                                                oldParamInfo.minMax = tempParamInfo.minMax;
                                                oldParamInfo.labelName = tempParamInfo.labelName;
                                            }
                                            else
                                            {
                                                oldParamInfo.serializable = false;
                                                oldParamInfo.uiEditable = false;
                                            }
                                            EditorGUILayout.BeginHorizontal();
                                            bool serializable = EditorGUILayout.Toggle(oldParamInfo.serializable, GUILayout.Width(toggleWidth));
                                            bool uiEditable = EditorGUILayout.Toggle(oldParamInfo.uiEditable, GUILayout.Width(toggleWidth));
                                            Settings.UIControlTypes controlType = oldParamInfo.controlType;
                                            Vector2 minMax = oldParamInfo.minMax;
                                            string labelName = oldParamInfo.labelName;
                                            EditorGUILayout.LabelField(field.Name + " [" + field.FieldType.Name + "]");
                                            EditorGUILayout.EndHorizontal();
                                            if (uiEditable)
                                            {
                                                EditorGUILayout.BeginHorizontal();
                                                EditorGUILayout.LabelField("Displayed name", GUILayout.Width(labelNameWidth));
                                                EditorGUILayout.LabelField(new GUIContent("Type", "Type of control for editable fields"), GUILayout.Width(controlTypeWidth));
                                                EditorGUILayout.LabelField(new GUIContent("Min Max", "For editable fields with range control only"), GUILayout.Width(minMaxWidth));
                                                EditorGUILayout.EndHorizontal();
                                                EditorGUILayout.BeginHorizontal();
                                                labelName = EditorGUILayout.TextField(labelName, GUILayout.Width(labelNameWidth));
                                                controlType = (Settings.UIControlTypes)EditorGUILayout.EnumPopup(controlType, GUILayout.Width(controlTypeWidth));
                                                if (controlType == Settings.UIControlTypes.Float || controlType == Settings.UIControlTypes.Int)
                                                {
                                                    minMax = EditorGUILayout.Vector2Field("", minMax, GUILayout.Width(minMaxWidth));
                                                }
                                                EditorGUILayout.EndHorizontal();
                                            }
                                            if (oldParamInfo.serializable != serializable || oldParamInfo.uiEditable != uiEditable
                                                || oldParamInfo.labelName != labelName || oldParamInfo.controlType != controlType || oldParamInfo.minMax != minMax)
                                            {
                                                if (tempParamInfo == null)
                                                    tempParamInfo = new Settings.AssemblyParameterInfo();
                                                tempParamInfo.parameterName = field.Name;
                                                tempParamInfo.serializable = serializable;
                                                tempParamInfo.uiEditable = uiEditable;
                                                tempParamInfo.labelName = labelName;
                                                tempParamInfo.controlType = controlType;
                                                tempParamInfo.minMax = minMax;
                                                ProjectManager.settings.SetParameterInfo(i, type.Name, tempParamInfo);
                                            }
                                        }
								    }

								    System.Reflection.PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
								    foreach (System.Reflection.PropertyInfo property in properties)
								    {
									    if (property.CanRead && property.CanWrite
										    && property.GetCustomAttributes(typeof(System.ObsoleteAttribute),false).Length == 0
									        && !property.PropertyType.IsSubclassOf(typeof(UnityEngine.Object)))									        
									    {
                                            tempParamInfo = ProjectManager.settings.GetParameterInfo(i, type.Name, property.Name);
                                            if (tempParamInfo != null)
                                            {
                                                oldParamInfo.serializable = tempParamInfo.serializable;
                                                oldParamInfo.uiEditable = tempParamInfo.uiEditable;
                                                oldParamInfo.controlType = tempParamInfo.controlType;
                                                oldParamInfo.minMax = tempParamInfo.minMax;
                                                oldParamInfo.labelName = tempParamInfo.labelName;
                                            }
                                            else
                                            {
                                                oldParamInfo.serializable = false;
                                                oldParamInfo.uiEditable = false;                                                
                                            }
                                            EditorGUILayout.BeginHorizontal();
                                            bool serializable = EditorGUILayout.Toggle(oldParamInfo.serializable, GUILayout.Width(toggleWidth));
                                            bool uiEditable = EditorGUILayout.Toggle(oldParamInfo.uiEditable, GUILayout.Width(toggleWidth));
                                            Settings.UIControlTypes controlType = oldParamInfo.controlType;
                                            Vector2 minMax = oldParamInfo.minMax;
                                            string labelName = oldParamInfo.labelName;
                                            EditorGUILayout.LabelField(property.Name + " [" + property.PropertyType.Name + "]");
                                            EditorGUILayout.EndHorizontal();
                                            if (uiEditable)
                                            {                                                
                                                EditorGUILayout.BeginHorizontal();
                                                EditorGUILayout.LabelField("Displayed name", GUILayout.Width(labelNameWidth));
                                                EditorGUILayout.LabelField(new GUIContent("Type", "Type of control for editable fields"), GUILayout.Width(controlTypeWidth));
                                                EditorGUILayout.LabelField(new GUIContent("Min Max", "For editable fields with range control only"), GUILayout.Width(minMaxWidth));
                                                EditorGUILayout.EndHorizontal();
                                                EditorGUILayout.BeginHorizontal();
                                                labelName = EditorGUILayout.TextField(labelName, GUILayout.Width(labelNameWidth));                                                
                                                controlType = (Settings.UIControlTypes)EditorGUILayout.EnumPopup(controlType, GUILayout.Width(controlTypeWidth));
                                                if (controlType == Settings.UIControlTypes.Float || controlType == Settings.UIControlTypes.Int)
                                                {
                                                    minMax = EditorGUILayout.Vector2Field("", minMax, GUILayout.Width(minMaxWidth));
                                                }
                                                EditorGUILayout.EndHorizontal();
                                            }                                            
                                            if (oldParamInfo.serializable != serializable || oldParamInfo.uiEditable != uiEditable
                                                || oldParamInfo.labelName != labelName || oldParamInfo.controlType != controlType || oldParamInfo.minMax != minMax)
                                            {                                                
                                                if (tempParamInfo == null)                                                                                              
                                                    tempParamInfo = new Settings.AssemblyParameterInfo();                                                
                                                tempParamInfo.parameterName = property.Name;
                                                tempParamInfo.serializable = serializable;
                                                tempParamInfo.uiEditable = uiEditable;                                                
                                                tempParamInfo.labelName = labelName;                                                
                                                tempParamInfo.controlType = controlType;
                                                tempParamInfo.minMax = minMax;
                                                ProjectManager.settings.SetParameterInfo(i, type.Name, tempParamInfo);                                                
                                            }                                            
                                        }
								    }
								    EditorGUI.indentLevel--;
							    }
							    EditorGUILayout.Space();
						    }
						    EditorGUI.indentLevel--;
					    }                        
					    EditorGUILayout.EndVertical();
				    }
				    EditorGUILayout.EndVertical();
				    EditorGUILayout.EndScrollView();
				break;
			case 2:
				    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
				    EditorGUILayout.BeginVertical();
				    set.fireMessagesInEditMode = EditorGUILayout.ToggleLeft("Fire messages in edit mode",set.fireMessagesInEditMode);
				    EditorGUILayout.EndVertical();
	                EditorGUILayout.EndScrollView();
	            break;
	        }


			if (GUI.changed)
			{
				EditorUtility.SetDirty (ProjectManager.settings);
				if (Application.isPlaying && Manager.Instance != null)
					Manager.Instance.UpdateSettings();
			}

			//serializedObject.ApplyModifiedProperties();
		}

		void OnEnable ()
		{
			//serializedObject = new SerializedObject(ProjectManager.settings);
			layerFoldouts = new List<bool>();
			messagingFoldouts = new List<bool>();
			parametersFoldouts = new bool[ProjectManager.settings.prefabs.Count][];
			paramsFoldouts = new bool[ProjectManager.settings.prefabs.Count];            
			for (int i = 0; i < ProjectManager.settings.layers.Count; i++)
				layerFoldouts.Add (false);
			for (int i = 0; i < ProjectManager.settings.layers.Count; i++)
				messagingFoldouts.Add (false);
		}

	}
}