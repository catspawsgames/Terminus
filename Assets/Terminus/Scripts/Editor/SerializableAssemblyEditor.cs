using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using System.Linq;

namespace Terminus .Editors
{
	[CustomEditor( typeof(SerializableAssembly))]
	public class SerializableAssemblyEditor : Editor {

		private SerializedProperty objectsProp;
		private TerminusObject readObject;
		private Vector3 spawnPosition;

		[MenuItem ("Assets/Create/ Serializable Assembly")]
		public static void CreateAssembly () 
		{
			string path = "Assets";
			foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
			{
				path = AssetDatabase.GetAssetPath(obj);
				if (File.Exists(path))
				{
					path = Path.GetDirectoryName(path);
				}
				break;
			}
			SerializableAssembly asset = ScriptableObject.CreateInstance<SerializableAssembly>();
			AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath (path + "/Assembly.asset"));
		}

		public override void OnInspectorGUI()
		{
			SerializableAssembly assembly = (SerializableAssembly)target;

			if (!Application.isPlaying && !ProjectManager.settings.fireMessagesInEditMode)
				EditorGUILayout.HelpBox("Firing messages in Edit mode is disabled. It can lead to improper generation of object in Edit mode. Be careful.", MessageType.Info);

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(objectsProp,true);
			if(EditorGUI.EndChangeCheck())
				serializedObject.ApplyModifiedProperties();

			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();
			GUI.enabled = readObject != null;
			if (GUILayout.Button("Read"))
			{
				assembly.ReadFromObject(readObject);
				EditorUtility.SetDirty(assembly);
				serializedObject.ApplyModifiedProperties();
				this.Repaint();
			}
			GUI.enabled = true;
			readObject = (TerminusObject)EditorGUILayout.ObjectField(readObject,typeof(TerminusObject),true);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();
			GUI.enabled = objectsProp.arraySize > 0;
			if (GUILayout.Button("Spawn"))
			{
				assembly.Spawn(spawnPosition);
			}
			GUI.enabled = true;
			spawnPosition = EditorGUILayout.Vector3Field("",spawnPosition);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();

			GUI.enabled = objectsProp.arraySize > 0;
			if (GUILayout.Button("Save to XML"))
			{
				string path = EditorUtility.SaveFilePanel("Save assembly as XML",
				                                          "",
				                                          target.name,
				                                          "xml");
				if (path.Length > 0)
					assembly.SaveToXML(path);
			}
			GUI.enabled = true;

			if (GUILayout.Button("Load from XML"))
			{
				string path = EditorUtility.OpenFilePanel("Load assembly from XML",
				                                          "",
				                                          "xml");
				if (path.Length > 0)
				{
					assembly.LoadFromXML(path);
					EditorUtility.SetDirty(assembly);
					serializedObject.ApplyModifiedProperties();
					this.Repaint();
				}
			}
		}

		void OnEnable () 
		{
			objectsProp = serializedObject.FindProperty("objects");
		}
	}
}