using UnityEngine;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;

namespace Terminus.Demo1
{

	/// <summary>
	/// Component working together with <see cref="BuilderUIHandler"/> to allow player to adjust some parameters of attachable part(<see cref="TerminusObject"/>.
	/// </summary>
	[DisallowMultipleComponent]
	public class AdjustableParametersHandler : MonoBehaviour {

		/// <summary>
		/// Parameters that can be adjusted.
		/// </summary>
		public AdjustableParameter[] parameters;
		protected ReflectionInfo[] reflections;

        protected Dictionary<UIControlTypes, int> controlTypeCount;

		/// <summary>
		/// Information about adjustable parameter
		/// </summary>
		[System.Serializable]
		public struct AdjustableParameter
		{
			/// <summary>
			/// Name of adjustable parameter displayed to the player.
			/// </summary>
			public string displayedName;
			/// <summary>
			/// Class name of component that contains field or property corresponding to this parameter.
			/// </summary>
			public string componentName;
			/// <summary>
			/// Name of field or property corresponding to this parameter.
			/// </summary>
			public string parameterName;
			/// <summary>
			/// The type of the control <see cref="BuilderUIHandler"/> will show to the player.
			/// </summary>
			public UIControlTypes controlType;
			/// <summary>
			/// Min,Max(X,Y) for Slider control, have no effect on Toggle control.
			/// </summary>
			public Vector2 minMax;
		}

		protected class ReflectionInfo
		{
			public Component component;
			public FieldInfo field;
			public PropertyInfo property;
		}

		///<summary>
		/// Possible control types for adjustable parameters to be drawn by <see cref="BuilderUIHandler"/>.
		/// </summary>
		public enum UIControlTypes
		{
			slider = 0,
			toggle = 1
		}

		/// <summary>
		/// Sets parameter value.
		/// </summary>
		/// <param name="index">Index of parameter inside <see cref="AdjustableParametersHandler.parameters"/> .</param>
		/// <param name="newValue">New value.</param>
		public void SetParameterValue(int index, object newValue)
		{
			if (reflections[index].field != null)
			{
				reflections[index].field.SetValue(reflections[index].component,newValue);
			}
			else
			{
				reflections[index].property.SetValue(reflections[index].component,newValue,null);
			}
		}

		/// <summary>
		/// Gets parameter value.
		/// </summary>
		/// <returns>The parameter value.</returns>
		/// <param name="index">Index of parameter inside <see cref="AdjustableParametersHandler.parameters"/> .</param>
		public object GetParameterValue(int index)
		{
			if (reflections[index].field != null)
			{
				return reflections[index].field.GetValue(reflections[index].component);
			}
			else
			{
				return reflections[index].property.GetValue(reflections[index].component,null);
			}
		}


		void Awake()
		{
			reflections = new ReflectionInfo[parameters.Length];
            controlTypeCount = new Dictionary<UIControlTypes, int>();
            var controlTypes = System.Enum.GetValues(typeof(UIControlTypes));
            foreach (UIControlTypes type in controlTypes)
            {
                controlTypeCount.Add(type, 0);
            }
            for (int i = 0; i < reflections.Length; i++)
			{
				reflections[i] = new ReflectionInfo();
				reflections[i].component = GetComponent(parameters[i].componentName);
				reflections[i].field = reflections[i].component.GetType().GetField(parameters[i].parameterName);
				if (reflections[i].field == null)
					reflections[i].property = reflections[i].component.GetType().GetProperty(parameters[i].parameterName);
                controlTypeCount[parameters[i].controlType]++;
			}
		}
	}
}