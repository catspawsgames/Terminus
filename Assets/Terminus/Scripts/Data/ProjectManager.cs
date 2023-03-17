using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;

namespace Terminus 
{
	/// <summary>
	/// Class for accessing Terminus <see cref="Settings"/>. All methods are static.
	/// </summary>
	public class ProjectManager
	{	
		protected static Settings _settings;

		/// <summary>
		/// Gets current <see cref="Settings"/> or creates instance of it if it's null.
		/// </summary>
		public static Settings settings
		{
			get
			{
				if (_settings == null)
				{
					_settings = (Settings)Resources.Load("TerminusSettings",typeof(Settings));
					#if UNITY_EDITOR
					if (_settings == null)				
						_settings =  CreateSettingsAsset();
					#endif
					return _settings;
				}
				else
					return _settings;
			}
		}

		/// <summary>
		/// Deletes current <see cref="Settings"/> and creates default instance of it. Works only when called from editor.
		/// </summary>
		public static void DefaultSettings()
		{
			#if UNITY_EDITOR
			string path = AssetDatabase.GetAssetPath(_settings);
			AssetDatabase.DeleteAsset(path);
			CreateSettingsAsset(path);
			#endif
		}

		#if UNITY_EDITOR
		/// <summary>
		/// Creates the settings asset. Editor-only.
		/// </summary>
		/// <returns>The settings asset.</returns>
		/// <param name="path">Path to save <see cref="Settings"/> asset.</param>
		public static Settings CreateSettingsAsset(string path = "")
		{
			if (path == "")
				path = "Assets/Terminus/Resources/TerminusSettings.asset";
			Settings settings = ScriptableObject.CreateInstance<Settings>();
			settings.layers = new List<Settings.LayerPortOptions>();
			settings.prefabs = new List<Settings.AssemblyPrefabOptions>();
			Settings.LayerPortOptions tempLayer = new Settings.LayerPortOptions();
			tempLayer = new Settings.LayerPortOptions();
			tempLayer.name = "Default";
			settings.layers.Add(tempLayer);

			tempLayer = new Settings.LayerPortOptions();
			tempLayer.name = "No cycling";
			tempLayer.attachementPort = true;
			tempLayer.cyclingAttachementPort = false;
			tempLayer.inspectorColor = new Color(1,0.33f,0,0.5f);
			settings.layers.Add(tempLayer);

			tempLayer = new Settings.LayerPortOptions();
			tempLayer.name = "No attachment";
			tempLayer.attachementPort = false;
			tempLayer.cyclingAttachementPort = false;
			tempLayer.inspectorColor = new Color(1,0.66f,0,0.5f);
			settings.layers.Add(tempLayer);


			AssetDatabase.CreateAsset(settings,path);
			AssetDatabase.SaveAssets();
			return settings;
		}
		#endif

	}
}