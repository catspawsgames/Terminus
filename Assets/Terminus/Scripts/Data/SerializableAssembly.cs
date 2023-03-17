using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Terminus 
{
	/// <summary>
	/// ScriptableObject allowing you to save your creations and load/spawn them as you please.
	/// </summary>
	public class SerializableAssembly : ScriptableObject 
	{
		/// <summary>
		/// Information about construction. Fully serializable.
		/// </summary>
		public ObjectInfo[] objects;

		/// <summary>
		/// Information about singular part(<see cref="TerminusObject"/>) of construction. Fully serializable.
		/// </summary>
		/// <remarks>
		/// Part with index = 0 is considered root part. All spatial info is saved in its localspace.
		/// </remarks>
		[System.Serializable]
		public struct ObjectInfo
		{
			/// <summary>
			/// Name of part. See <see cref="TerminusObject.getPartName"/> 
			/// </summary>
			public string name;
			/// <summary>
			/// ID (index) of part. Used in <see cref="SerializableAssembly.ObjectInfo.connectors"/> and <see cref="SerializableAssembly.ObjectInfo.symmetricSiblings"/>.
			/// </summary>
			public int id;
			/// <summary>
			/// Position of part(<see cref="TerminusObject"/>) in root part space.
			/// </summary>
			public Vector3 position;
			/// <summary>
			/// Rotation of part(<see cref="TerminusObject"/>) in root part space.
			/// </summary>
			public Quaternion rotation;
			/// <summary>
			/// Attached <see cref="Connector"/>s of part(<see cref="TerminusObject"/>) and information about attachment.
			/// </summary>
			public ConnectorInfo[] connectors;
			/// <summary>
			/// List of symmetric siblings of this part(<see cref="TerminusObject"/>).
			/// </summary>
			public int[] symmetricSiblings;
			/// <summary>
			/// Serialized parameters of component of this part. See <see cref="Settings.AssemblySerializableParameters"/> 
			/// </summary>
			public ParameterInfo[] parameters;
		}

		/// <summary>
		/// Struct containing information about attached <see cref="Connector"/>  and attachment parameters.
		/// </summary>
		[System.Serializable]
		public struct ConnectorInfo
		{
			/// <summary>
			/// Name of <see cref="Connector"/>. See <see cref="Connector.connectorName"/>
			/// </summary>
			public string name;
			/// <summary>
			/// Position of this <see cref="Connector"/> in root part space.
			/// </summary>
			public Vector3 position;
			/// <summary>
			/// Rotation of this <see cref="Connector"/> in root part space.
			/// </summary>
			public Quaternion rotation;
			/// <summary>
			/// Position difference between this <see cref="Connector"/> and its attachment partner.
			/// </summary>
			public Vector3 positionDifference;
			/// <summary>
			/// Rotation difference between this <see cref="Connector"/> and its attachment partner.
			/// </summary>
			public Quaternion rotationDifference;
			/// <summary>
			/// Port angle (if applicable).
			/// </summary>
			public float angle;
			/// <summary>
			/// Index(<see cref="SerializableAssembly.ObjectInfo.id"/>) of part that this <see cref="Connector"/> is attached.
			/// </summary>
			public int otherObjectIndex;
			/// <summary>
			/// <see cref="Connector.connectorName"/> of connector that this <see cref="Connector"/> is attached.
			/// </summary>
			public string otherConnectorName;
			/// <summary>
			/// If attachment is <see cref="AttachmentInfo.Types.sideway"/> or not.
			/// </summary>
			public bool sideway;
		}

		/// <summary>
		/// Struct containing information about serialized parameter of component belonging to part(<see cref="TerminusObject"/>).
		/// </summary>
		[System.Serializable]
		public struct ParameterInfo
		{
			/// <summary>
			/// Class name of component
			/// </summary>
			public string component;
			/// <summary>
			/// Field or property name.
			/// </summary>
			public string parameter;
			/// <summary>
			/// Type name of <see cref="SerializableAssembly.ParameterInfo.parameter"/> 
			/// </summary>
			public string type;
			/// <summary>
			/// Serialized value of component. Either string or base64 representation.
			/// </summary>
			public string value;
			/// <summary>
			/// If true, <see cref="SerializableAssembly.ParameterInfo.value"/> contains base64 representation, otherwise it contains simple string representation.
			/// </summary>
			public bool binary;

			public ParameterInfo(string component,string parameter, string type, string value,bool binary) 
			{
				this.component = component;
				this.parameter = parameter;
				this.type = type;
				this.value = value;
				this.binary = binary;
			}
		}


		public static HashSet<System.Type> toStringTypes = new HashSet<System.Type>
		{
			typeof(byte),
			typeof(sbyte),
			typeof(uint),
			typeof(int),
			typeof(long),
			typeof(ulong),
			typeof(short),
			typeof(ushort),
			typeof(bool),
			typeof(decimal),
			typeof(float),
			typeof(double),
			typeof(string),
			typeof(Vector2),
			typeof(Vector3),
			typeof(Vector4),
			typeof(Quaternion)
		};

		//Returns true if output is not binary
		public static bool SerializeValue(object val, out string outputString, bool forceBinary)
		{
			if (!forceBinary && toStringTypes.Contains(val.GetType()))
			{
				outputString = val.ToString();
				return true;
			}
			else
			{
				if (!val.GetType().IsSerializable)
				{
					outputString = "";
					return false;
				}
				
				using (MemoryStream stream = new MemoryStream())
				{
					new BinaryFormatter().Serialize(stream, val);
					outputString = System.Convert.ToBase64String(stream.ToArray());
					return false;
				}
			}
		}

		public static object DeserializeValue(string inputString, bool binary, System.Type type)
		{
			if (binary)
			{
				byte[] bytes = System.Convert.FromBase64String(inputString);
				
				using (MemoryStream stream = new MemoryStream(bytes))
				{
					return new BinaryFormatter().Deserialize(stream);
				}
			}
			else
			{
				if (type == typeof(Vector2))
				{
					string[] temp = inputString.Substring(1,inputString.Length-2).Split(',');
					float x = float.Parse(temp[0]);
					float y = float.Parse(temp[1]);
					return new Vector2(x,y);
				}
				else if (type == typeof(Vector3))
				{
					string[] temp = inputString.Substring(1,inputString.Length-2).Split(',');
					float x = float.Parse(temp[0]);
					float y = float.Parse(temp[1]);
					float z = float.Parse(temp[2]);
					return new Vector3(x,y,z);
				}
				else if (type == typeof(Vector4))
				{
					string[] temp = inputString.Substring(1,inputString.Length-2).Split(',');
					float x = float.Parse(temp[0]);
					float y = float.Parse(temp[1]);
					float z = float.Parse(temp[2]);
					float w = float.Parse(temp[3]);
					return new Vector4(x,y,z,w);
				}
				else if (type == typeof(Quaternion))
				{
					string[] temp = inputString.Substring(1,inputString.Length-2).Split(',');
					float x = float.Parse(temp[0]);
					float y = float.Parse(temp[1]);
					float z = float.Parse(temp[2]);
					float w = float.Parse(temp[3]);
					return new Quaternion(x,y,z,w);
				}
				else
				{
					return System.Convert.ChangeType(inputString,type);
				}
			}
		}


		/// <summary>
		/// Fills <see cref="SerializableAssembly.objects"/> by reading provided <see cref="TerminusObject"/> and its <see cref="TerminusObject.treeListDown"/> 
		/// </summary>
		/// <param name="TerminusObject">Root Terminus object of construction.</param>
		public void ReadFromObject(TerminusObject terminusObject)
		{
			List<TerminusObject> tree = new List<TerminusObject>();
			tree.Add(terminusObject);
			tree.AddRange(terminusObject.treeListDown.Select(rec => rec.otherConnector.owner));
			objects = new ObjectInfo[tree.Count];

			Vector3 pos = terminusObject.position;
			Quaternion rot = terminusObject.rotation;

			for (int i = 0; i < objects.Length; i++)
			{
				objects[i].id = i;
				objects[i].position = tree[i].transform.position-pos;
				objects[i].rotation = tree[i].transform.rotation;
				if (tree[i].getPartName == "")
				{
					Debug.LogError(": Trying to generate serializable assembly while object "+tree[i].gameObject.name+" haven't been registered as serializable Terminus prefab.");
					objects = new ObjectInfo[0];
					return;
				}
				else
					objects[i].name = tree[i].getPartName;

				Settings.AssemblyPrefabOptions prefabOptions = ProjectManager.settings.prefabs.Find(rec => rec.prefab.getPartName == tree[i].getPartName);
				if (prefabOptions == null)
				{
					Debug.LogError("Terminus: Unable to read serializable prefab options for " + tree[i].getPartName);
				}
				if (prefabOptions.parameters.Count > 0)
				{
					List<ParameterInfo> paramsToSave = new List<ParameterInfo>();
					string[] componentList = prefabOptions.parameters.Select(rec => rec.component).Distinct().ToArray();
					string paramValue;
					for (int x = 0; x < componentList.Length; x++)
					{
						Component component = tree[i].GetComponent(componentList[x]);
						if (component != null)
						{
							//string[] parameterList = prefabOptions.parameters.Find(rec => rec.component == componentList[x]).parameters.ToArray();
                            Settings.AssemblyParameterInfo[] parameterList = prefabOptions.parameters.Find(rec => rec.component == componentList[x]).parameters.ToArray();
                            System.Type compType = component.GetType();
							for (int y = 0; y < parameterList.Length; y++)
							{
								System.Reflection.FieldInfo field = compType.GetField(parameterList[y].parameterName,BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
								if (field != null)
								{
									bool binary = !SerializeValue(field.GetValue(component), out paramValue, false);
									paramsToSave.Add(new ParameterInfo(componentList[x],parameterList[y].parameterName, field.FieldType.Name, paramValue,binary));
								}
								else
								{
									System.Reflection.PropertyInfo property = compType.GetProperty(parameterList[y].parameterName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
									if (property != null)
									{
										bool binary = !SerializeValue(property.GetValue(component,null), out paramValue, false);
										paramsToSave.Add(new ParameterInfo(componentList[x],parameterList[y].parameterName, property.PropertyType.Name, paramValue,binary));
									}
									else
									{
										Debug.LogWarning("Could not find field or property "+parameterList[y]+" of component "+component.GetType().Name);
									}
								}
							}
						}
					}
					objects[i].parameters = paramsToSave.ToArray();
				}

				List<ConnectorInfo> connectors = new List<ConnectorInfo>();

				for (int x = 0; x < tree[i].connectors.Count; x++)
				{
					if (tree[i].connectors[x] is Port)
					{
						Port port = (Port)tree[i].connectors[x];
						if (port.attachmentInfo.attachmentType == AttachmentInfo.Types.child
							|| (port.attachmentInfo.attachmentType == AttachmentInfo.Types.sideway
								&& port.attachmentInfo.selfIsInitiator))
						{							
							int ind = tree.FindIndex(rec => rec == port.attachmentInfo.otherConnector.owner);
							if (ind >= 0 || (Manager.Instance != null && port.attachmentInfo.otherConnector == Manager.Instance.environmentSurface))
							{
								ConnectorInfo newConn = new ConnectorInfo();
								newConn.name = port.connectorName;
								newConn.position = port.globalPosition - pos;
								newConn.rotation = port.globalRotation * Quaternion.Inverse(rot);
								newConn.positionDifference = port.attachmentInfo.originalPositionDifference;
								newConn.rotationDifference = port.attachmentInfo.originalRotationDifference;
								newConn.sideway = ((Port)(tree[i].connectors[x])).attachmentInfo.attachmentType == AttachmentInfo.Types.sideway;
								newConn.otherObjectIndex = ind;
								newConn.otherConnectorName = ((Port)(tree[i].connectors[x])).attachmentInfo.otherConnector.connectorName;
								connectors.Add(newConn);
							}
						}
					}
				}
				objects[i].connectors = connectors.ToArray();

				List<int> siblings = new List<int>();
				for (int x = 0; x < tree[i].symmetricSiblings.Count; x++)
				{
					int ind = tree.FindIndex(rec => rec == tree[i].symmetricSiblings[x]);
					if (ind >= 0)
						siblings.Add(ind);
				}
				objects[i].symmetricSiblings = siblings.ToArray();
			}
		}

		/// <summary>
		/// Spawns saved construction at given position. Returns root of spawned construction.
		/// </summary>
		/// <param name="position">Position of root <see cref="TerminusObject"/>  of spawned construction.</param>
		/// <param name="mode">Mode to put root <see cref="TerminusObject"/> after spawning.</param>
		/// <param name="inAssemblyState"><see cref="TerminusObject.inAssemblyState"/> of root <see cref="TerminusObject"/> after spawning.</param>
		/// <param name="useOriginalSpatialParams">If true, deserialized objects will be placed according to shift and rotation relative to their parent object at the moment of attachment. If false, they will be placed according to shift and rotation relative to root object at the moment of serialization.</param>
		/// <param name="restoreEnvironmentAttachments">SerializableAssembly automatically stores attachments to environment (wihtout data about environment itself). If set to true, those attachments will be restored during deserialization.</param>
		public TerminusObject Spawn(Vector3 position, TerminusObject.Modes mode = TerminusObject.Modes.accepting_attachments, bool inAssemblyState = false, bool useOriginalSpatialParams = false, bool restoreEnvironmentAttachments = false)
		{
			List<TerminusObject> tree = new List<TerminusObject>();
			Settings.AssemblyPrefabOptions[] treeOptions = new Settings.AssemblyPrefabOptions[objects.Length];

			if (objects == null || objects.Length == 0)
				return null;

			for (int i = 0; i < objects.Length; i++)
			{
				treeOptions[i] = ProjectManager.settings.prefabs.Find(rec => rec.prefab.getPartName == objects[i].name);
				GameObject obj = Instantiate(treeOptions[i].prefab.gameObject);
				tree.Add(obj.GetComponent<TerminusObject>());
				tree[i].transform.position = objects[i].position+position;
				tree[i].transform.rotation = objects[i].rotation;
				tree[i].mode = mode;
				tree[i].inAssemblyState = inAssemblyState;

				/*
				for (int x = 0; x < objects[i].connectors.Length; x++)
				{
					Port port = (Port)tree[i].connectors.Find(rec => rec.connectorName == objects[i].connectors[x].name);
					if (port.doNotMoveOwner)
						port.Align(objects[i].connectors[x].position + position,objects[i].connectors[x].rotation * Quaternion.Euler(0,180,0));
				}
				*/


				if (objects[i].parameters != null)
				{
					string componentName = "";
					Component component = null;
					System.Type type = null;
					FieldInfo field;
					PropertyInfo property;

					for (int x = 0; x < objects[i].parameters.Length; x++)
					{
						if (componentName != objects[i].parameters[x].component)
						{
							componentName = objects[i].parameters[x].component;
							component = tree[i].GetComponent(componentName);
							type = component.GetType();
						}

						field = type.GetField(objects[i].parameters[x].parameter);
						if (field != null)
						{
							field.SetValue(component,DeserializeValue(objects[i].parameters[x].value,objects[i].parameters[x].binary,field.FieldType));
						}
						else
						{
							property = type.GetProperty(objects[i].parameters[x].parameter);
							if (property != null)
								property.SetValue(component,DeserializeValue(objects[i].parameters[x].value,objects[i].parameters[x].binary,property.PropertyType),null);
						}
					}
				}
			}

			for (int i = 0; i < objects.Length; i++)
			{
				tree[i].symmetricSiblings = new List<TerminusObject>();
				for (int x = 0; x < objects[i].symmetricSiblings.Length; x++)
					tree[i].symmetricSiblings.Add(tree[objects[i].symmetricSiblings[x]]);

				for (int x = 0; x < objects[i].connectors.Length; x++)
				{
					Connector otherConn = null;
					if (objects[i].connectors[x].otherObjectIndex == -1)
						otherConn = Manager.Instance.environmentSurface;
					else
						otherConn = tree[objects[i].connectors[x].otherObjectIndex].connectors.Find(rec => rec.connectorName == objects[i].connectors[x].otherConnectorName);
					
					Port port = (Port)tree[i].connectors.Find(rec => rec.connectorName == objects[i].connectors[x].name);

					if (useOriginalSpatialParams)
					{
						//port.Align(otherConn.transform.TransformPoint(objects[i].connectors[x].positionDifference), otherConn.globalRotation * (objects[i].connectors[x].rotationDifference * Quaternion.Euler(0,180,0)));
						if (!objects[i].connectors[x].sideway || port.doNotMoveOwner)
							port.Align(otherConn.globalPosition + otherConn.transform.TransformDirection(objects[i].connectors[x].positionDifference), otherConn.globalRotation * (objects[i].connectors[x].rotationDifference * Quaternion.Euler(0,180,0)));
					}
					else
						if (port.doNotMoveOwner)
						{
							port.Align(objects[i].connectors[x].position + position,objects[i].connectors[x].rotation * Quaternion.Euler(0,180,0));
						}

					if (objects[i].connectors[x].sideway)
						port.AttachToSideways(otherConn);
					else
						port.AttachTo(otherConn);
				}
			}

			for (int i = 0; i < objects.Length; i++)
			{
				for (int x = 0; x < treeOptions[i].parameters.Count; x++)
				{
                    //if (treeOptions[i].parameters[x].sendMessage)
                    //Utils.SendMessageToComponent(tree[i].gameObject.GetComponent(treeOptions[i].parameters[x].component),treeOptions[i].parameters[x].messageMethod);
                    if (treeOptions[i].parameters[x].callOnDeserialization)
                        tree[i].gameObject.GetComponent<IOnDeserialization>().OnDeserialization();
                }
			}

			return tree[0];
		}

		/// <summary>
		/// Serializes current saved <see cref="SerializableAssembly.objects"/> to XML file.
		/// </summary>
		/// <remarks>
		/// Rewrites file if already existing.
		/// </remarks>
		/// <param name="path">Path for creating/replacing.</param>
		/// <seealso cref="SerializableAssembly.LoadFromXML"/>
		public void SaveToXML(string path)
		{
			System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ObjectInfo[]));
			StreamWriter file = File.CreateText(path);
			serializer.Serialize(file,objects);
			file.Close();
		}

		/// <summary>
		/// Loads <see cref="SerializableAssembly.objects"/> from XML file.
		/// </summary>
		/// <param name="path">Path.</param>
		/// <seealso cref="SerializableAssembly.SaveToXML"/>
		public void LoadFromXML(string path)
		{
			System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ObjectInfo[]));
			StreamReader file = File.OpenText(path);
			objects = (ObjectInfo[])(serializer.Deserialize(file));
			file.Close();
		}

		/// <summary>
		/// Static method for saving construction directly to XML file, bypassing SerializableAssembly instance creation. Still creates temporary SerializableAssembly internally.
		/// </summary>
		/// <param name="path">Path to saved XML file.</param>
		/// <param name="root">Root <see cref="TerminusObject"/>.</param>
		public static void SaveToXML(string path,TerminusObject root)
		{
			SerializableAssembly assembly = ScriptableObject.CreateInstance<SerializableAssembly>();
			assembly.ReadFromObject(root);
			assembly.SaveToXML(path);
		}

		/// <summary>
		/// Static method for creating <see cref="SerializableAssembly"/> directly from XML file/
		/// </summary>
		/// <returns>
		/// Created <see cref="SerializableAssembly"/>
		/// </returns>
		/// <param name="path">Path to XML file.</param>
		public static SerializableAssembly CreateFromXML(string path)
		{
			SerializableAssembly assembly = ScriptableObject.CreateInstance<SerializableAssembly>();
			assembly.LoadFromXML(path);
			return assembly;
		}

		/// <summary>
		/// Static method for loading construction directly from XML file, bypassing SerializableAssembly instance creation. Still creates temporary SerializableAssembly internally.
		/// </summary>
		/// <returns>
		/// Root <see cref="TerminusObject"/> of spawned constuction.
		/// </returns>
		/// <param name="path">Path to XML file.</param>
		/// <param name="position">Position of root <see cref="TerminusObject"/>  of spawned construction.</param>
		/// <param name="mode">Mode to put root <see cref="TerminusObject"/> after spawning.</param>
		/// <param name="inAssemblyState"><see cref="TerminusObject.inAssemblyState"/> of root <see cref="TerminusObject"/> after spawning.</param>
		/// <param name="useOriginalSpatialParams">If true, deserialized objects will be placed according to shift and rotation relative to their parent object at the moment of attachment. If false, they will be placed according to shift and rotation relative to root object at the moment of serialization.</param>
		/// <param name="restoreEnvironmentAttachments">SerializableAssembly automatically stores attachments to environment (wihtout data about environment itself). If set to true, those attachments will be restored during deserialization.</param>
		public static TerminusObject SpawnFromXML(string path, Vector3 position, TerminusObject.Modes mode = TerminusObject.Modes.accepting_attachments, bool inAssemblyState = false, bool useOriginalSpatialParams = false, bool restoreEnvironmentAttachments = false)
		{
			SerializableAssembly assembly = CreateFromXML(path);
			return assembly.Spawn(position,mode,inAssemblyState,useOriginalSpatialParams,restoreEnvironmentAttachments);
		}
	}
}