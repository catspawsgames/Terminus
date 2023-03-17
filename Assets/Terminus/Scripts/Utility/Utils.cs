using UnityEngine;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;

namespace Terminus 
{
	/// <summary>
	/// Helper class containing useful static methods.
	/// </summary>
	public class Utils
	{

		public enum BlendMode
		{
			Opaque,
			Cutout,
			Fade,		// Old school alpha-blending mode, fresnel does not affect amount of transparency
			Transparent, // Physically plausible transparency mode, implemented as alpha pre-multiply
			NotStandardShader
		}

		static Dictionary<System.Type,Dictionary<int,MethodInfo>> reflectionDict = new Dictionary<System.Type, Dictionary<int, MethodInfo>>();

		/// <summary>
		/// Same functionality as GameObject.SendMessage, but works without error in edit mode.
		/// </summary>
		/// <param name="reciever">Reciever GameObject.</param>
		/// <param name="methodName">Method name.</param>
		/// <param name="value">Method parameter.</param>
		public static void SendMessage(GameObject reciever, string methodName, object value = null)
		{
			Component[] components = reciever.GetComponents<Component>();		
			object[] values = null;
			if (value != null)
			{
				values = new object[1];
				values[0] = value;
			}
			for (int i = 0; i < components.Length; i++)
			{
				System.Type type = components[i].GetType();
				MethodInfo tMethod = null;
				int methodHash = methodName.GetHashCode();
				if (reflectionDict.ContainsKey(type))
				{
					Dictionary<int,MethodInfo> typeReflInfo = reflectionDict[type];
					if (typeReflInfo.ContainsKey(methodHash))
						tMethod = typeReflInfo[methodHash];
					else
					{
						tMethod = type.GetMethod(methodName,BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
						typeReflInfo.Add(methodHash,tMethod);
					}
				}
				else
				{
					tMethod = type.GetMethod(methodName,BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					Dictionary<int,MethodInfo> typeReflInfo = new Dictionary<int, MethodInfo>();
					typeReflInfo.Add(methodHash,tMethod);
					reflectionDict.Add(type,typeReflInfo);
				}

				//tMethod = components[i].GetType().GetMethod(methodName,BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if(tMethod != null)
				{
					tMethod.Invoke(components[i], values);
				}
			}
		}

		/// <summary>
		/// Same functionality as GameObject.SendMessage, but works without error in edit mode and send message only to provided component.
		/// </summary>
		/// <param name="reciever">Reciever Component.</param>
		/// <param name="methodName">Method name.</param>
		/// <param name="value">Method parameter.</param>
		public static void SendMessageToComponent(Component reciever, string methodName, object value = null)
		{
			object[] values = null;
			if (value != null)
			{
				values = new object[1];
				values[0] = value;
			}
			MethodInfo tMethod = reciever.GetType().GetMethod(methodName,BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if(tMethod != null)
			{
				tMethod.Invoke(reciever, values);
			}
		}


		/// <summary>
		/// Makes copy of provided Joint or Joint2D and attaches it to destination GameObject.
		/// </summary>
		/// <returns>Copy of the component.</returns>
		/// <param name="original">Original component.</param>
		/// <param name="destination">Destination GameObject.</param>
		public static Component CopyJoint(Component original, GameObject destination)
		{

			System.Type type = original.GetType();
			Component copy = destination.AddComponent(type);

			System.Reflection.PropertyInfo[] properties;
			if (original is Joint)
			{
				properties = typeof(Joint).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly); 
			}
			else
			{
				properties = typeof(Joint2D).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly); 
			}
			System.Reflection.PropertyInfo[] propertiesDerived = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly); 

			for (int i = 0; i < properties.Length; i++)
			{
				if (properties[i].CanWrite)
					properties[i].SetValue(copy, properties[i].GetValue(original,null),null);
			}
			for (int i = 0; i < propertiesDerived.Length; i++)
			{
				if (propertiesDerived[i].CanWrite)
					propertiesDerived[i].SetValue(copy, propertiesDerived[i].GetValue(original,null),null);
			}
			return copy;
		}

		/// <summary>
		/// Returns XY part of Vector3.
		/// </summary>
		public static Vector2 XY(Vector3 input)
		{
			return new Vector2(input.x,input.y);
		}
			

		/// <summary>
		/// Changes blendmove of material that uses Unity Standard shader.
		/// </summary>
		public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
		{
			material.SetInt("_Mode",(int)blendMode);
			switch (blendMode)
			{
			case BlendMode.Opaque:
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.SetInt("_ZWrite", 1);
				material.DisableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = -1;
				break;
			case BlendMode.Cutout:
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.SetInt("_ZWrite", 1);
				material.EnableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = 2450;
				break;
			case BlendMode.Fade:
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ZWrite", 0);
				material.DisableKeyword("_ALPHATEST_ON");
				material.EnableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = 3000;
				break;
			case BlendMode.Transparent:
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ZWrite", 0);
				material.DisableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = 3000;
				break;
			}
		}
	}
}