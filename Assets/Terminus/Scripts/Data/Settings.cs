using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Terminus 
{
	/// <summary>
	/// Class containing global Terminus settings.
	/// </summary>
	public class Settings : ScriptableObject {

        public enum UIControlTypes
        {
            Toggle = 0,
            Int = 1,
            Float = 2
        }


        /// <summary>
        /// Terminus layers. See <see cref="Connector.layer"/>
        /// </summary>
        public List<LayerPortOptions> layers;
		/// <summary>
		/// List of serializable prefabs used by <see cref="SerializableAssembly"/>
		/// </summary>
		public List<AssemblyPrefabOptions> prefabs;
		/// <summary>
		/// If set to true, fires OnAttachment and OnDetachment messages even when in edit mode. Otherwise these messages fired only in play mode.
		/// </summary>
		public bool fireMessagesInEditMode;

		/// <summary>
		/// Options for serializing different parts(<see cref="TerminusObject"/>s).
		/// </summary>
		[System.Serializable]
		public class AssemblyPrefabOptions
		{
			/// <summary>
			/// Prefab GameObject of <see cref="TerminusObject"/> 
			/// </summary>
			public TerminusObject prefab;
			public List<AssemblySerializableParameters> parameters = new List<AssemblySerializableParameters>();            
		}

        /// <summary>
        /// Rules for serializing different components by <see cref="SerializableAssembly"/>. Also contains data about parameters editable through UI.
        /// </summary>
        [System.Serializable]
        public class AssemblySerializableParameters
        {
            /// <summary>
            /// Component class name.
            /// </summary>
            public string component;
            //Filled on awake by Manager
            [System.NonSerialized]
            public System.Type componentType;
            /// <summary>
            /// Names of parameters (fields and properties) to serialize. Can be public, private or protected.
            /// </summary>
            public List<AssemblyParameterInfo> parameters;
            /// <summary>
            /// If true and component implements IOnDeserialization interface, OnDeserialization will be called when assembly is spawned from serialized data.
            /// </summary>
            public bool callOnDeserialization;            

            /// <summary>
            /// Returns true if at least one parameter of this component is serialized
            /// </summary>
            /// <returns></returns>
            public bool Serialized()
            {
                if (parameters == null || parameters.Count == 0)
                    return false;
                for (int i = 0; i < parameters.Count; i++)
                {
                    if (parameters[i].serializable)
                        return true;
                }
                return false;
            }
        }

        [System.Serializable]
        public class AssemblyParameterInfo
        {
            /// <summary>
            /// Name of field or property.
            /// </summary>
            public string parameterName;            
            /// <summary>
            /// If true, parameter data will be saved and restored when using Terminus serializer.
            /// </summary>
            public bool serializable;
            /// <summary>
            /// If true, parameter will be avaliable for editing by player through UI. Checked by UI components.
            /// </summary>
            public bool uiEditable;
            /// <summary>
            /// Label that shows when parameter is editable through UI
            /// </summary>
            public string labelName;
            /// <summary>
            /// Type of control that shows when parameter is editable through UI
            /// </summary>
            public UIControlTypes controlType;
            /// <summary>
            /// Range for slider if control type supports it.
            /// </summary>
            public Vector2 minMax;

            //Filled on awake by Manager
            [System.NonSerialized]
            public FieldInfo field;
            //Filled on awake by Manager
            [System.NonSerialized]
            public PropertyInfo property;

            /// <summary>
            /// serializable || uiEditable;
            /// </summary>
            public bool active
            {
                get
                {
                    return serializable || uiEditable;
                }
            }
        }

        

		/// <summary>
		/// Terminus Layer options.
		/// </summary>
        [System.Serializable]
		public class LayerPortOptions
		{
			/// <summary>
			/// Name of this layer. Mainly for UI purposes.
            /// </summary>
			public string name;
			/// <summary>
			/// Color of connector gizmos shown in scene edit window of Unity editor. No effect on gameplay.
			/// </summary>
			public Color inspectorColor = new Color(1,0,0,0.5f);
			/// <summary>
			/// <see cref="Connector"/>s  from what layers can be attached to connectors from this layer.
			/// </summary>
			public int acceptsConnectionFromLayers = -1;
			/// <summary>
			/// Can be <see cref="TerminusObject.currentAttachmentPort"/>. Only one of <see cref="TerminusObject.connectors"/> can be attachment port at the same time.
			/// </summary>
			public bool attachementPort = true;
			/// <summary>
			/// <see cref="Port"/>s belonging to this layer can be made <see cref="TerminusObject.currentAttachmentPort"/> by calling <see cref="TerminusObject.SetNextAttachmentPort"/>
			/// </summary>
			public bool cyclingAttachementPort = true;
			/// <summary>
			/// <see cref="Placer"/> will prioritize ports with higher attachment priority, even if they're more distant from their attachment candidate. Used only when <see cref="TerminusObject.multipleAttachmentPortsAllowed"/> is true.
			/// </summary>
			public float attachmentPriority = 0;
			/// <summary>
			/// Type of action pefromed when <see cref="Port"/> from this layer is attached to other connector.
			/// </summary>
			public AttachmentTypes attachmentType;
			/// <summary>
			/// If <see cref="Settings.AttachmentTypes.rigidbody_welding"/> selected, determines whether Destroy or DestroyImmediate will be called on welded rigidbody. Generally, should be set to true for buildings and false for vehicles. Immediate destruction helps with phantom forces but can cause errors when welded rigidbody affects other rigidbodies.
			/// </summary>
			public bool destroyRigidbodyImmediately;
			/// <summary>
			/// <see cref="Connector"/> uses 2D physics when creating joints and welding rigidbodies, and 2D calculations for proximity and angle differnces.
			/// </summary>
			public bool use2DPhysics;
			/// <summary>
			/// How <see cref="Port"/> orients itself when aligned with other port. Used by <see cref="Port.Align"/>
			/// </summary>
			public OrientationTypes orientationType = OrientationTypes.exact;
			/// <summary>
			/// If <see cref="Settings.LayerPortOptions.attachmentType"/> is <see cref="Settings.attachmentTypes.physic_joints"/> or <see cref="Settings.attachmentTypes.combined"/>, clone of this Joint will be created on attachment.
			/// </summary>
			/// <seealso cref="Settings.LayerPortOptions.use2DPhysics"/>
			public Joint jointPrefab;
			/// <summary>
			/// If <see cref="Settings.LayerPortOptions.attachmentType"/> is <see cref="Settings.attachmentTypes.physic_joints"/> or <see cref="Settings.attachmentTypes.combined"/>, clone of this Joint2D will be created on attachment.
            /// </summary>
			/// <seealso cref="Settings.LayerPortOptions.use2DPhysics"/>
			public Joint2D jointPrefab2D;
			/// <summary>
			/// At what minimum distance two <see cref="Connector"/>s that can be attached according to <see cref="Settings.LayerPortOptions.acceptsConnectionFromLayers"/> can be considered attachment candidates by <see cref="Port.CheckPossibleConnections"/>
			/// </summary>
			public float snapRadius = 1;
			/// <summary>
			/// At what orientation difference (in degrees) two <see cref="Connector"/>s that can be attached according to <see cref="Settings.LayerPortOptions.acceptsConnectionFromLayers"/> can be considered attachment candidates by <see cref="Port.CheckPossibleConnections"/>
            /// </summary>
			public float snapConeAngle = 180;

			// <summary>
			// If true, <see cref="Settings.LayerPortOptions.snapConeAngle"/> considers difference between Z-axes of <see cref="Connector"/>s orientations only.
			// </summary>
			//public bool onlyPlanarSnap = false;

			/// <summary>
			/// Should messages be sent to GameObjects containing <see cref="Connector"/>s participating in attachment or detachment?
			/// </summary>
			/// <seealso cref="Settings.LayerPortOptions.onBeforeAttachmentMessage"/> 
			/// <seealso cref="Settings.LayerPortOptions.onAfterAttachmentMessage"/> 
			/// <seealso cref="Settings.LayerPortOptions.onBeforeDetachmentMessage"/> 
			/// <seealso cref="Settings.LayerPortOptions.onAfterDetachmentMessage"/> 
			public bool sendMessagesToPorts;
			/// <summary>
			/// Should messages be sent to GameObjects containing <see cref="Connector.owner"/>s participating in attachment or detachment?
            /// </summary>
			/// <seealso cref="Settings.LayerPortOptions.onBeforeAttachmentMessage"/> 
			/// <seealso cref="Settings.LayerPortOptions.onAfterAttachmentMessage"/> 
			/// <seealso cref="Settings.LayerPortOptions.onBeforeDetachmentMessage"/> 
			/// <seealso cref="Settings.LayerPortOptions.onAfterDetachmentMessage"/> 
			public bool sendMessagesToOwnerObjects;
			/// <summary>
			/// Should messages be sent to <see cref="Manager.globalMessagesReciever"/> GameObject?
			/// </summary>
			/// <seealso cref="Settings.LayerPortOptions.onBeforeAttachmentMessage"/> 
			/// <seealso cref="Settings.LayerPortOptions.onAfterAttachmentMessage"/> 
			/// <seealso cref="Settings.LayerPortOptions.onBeforeDetachmentMessage"/> 
			/// <seealso cref="Settings.LayerPortOptions.onAfterDetachmentMessage"/> 
            public bool sendMessagesToGameObject;
			/// <summary>
			/// Method name for BeforeAttachment message. <see cref="AttachmentInfo"/> will be provided as parameter.
			/// </summary>
			/// <remarks>
			/// Note that if attachment is performed through physics joint, <see cref="AttachmentInfo.joint"/> will unavaliable at the time of this message since its not created yet.
			/// Use <see cref="Settings.LayerPortOptions.onAfterAttachmentMessage"/> if you need access to physics joint.
			/// </remarks>
			/// <seealso cref="Settings.LayerPortOptions.onAfterAttachmentMessage"/> 
			public string onBeforeAttachmentMessage = "OnBeforeAttachment";
			/// <summary>
			/// Method name for AfterAttachment message. <see cref="AttachmentInfo"/> will be provided as parameter.
            /// </summary>
			/// <seealso cref="Settings.LayerPortOptions.onBeforeAttachmentMessage"/> 
			public string onAfterAttachmentMessage = "OnAfterAttachment";
			/// <summary>
			/// Method name for BeforeDetachment message. <see cref="AttachmentInfo"/> will be provided as parameter.
            /// </summary>
			/// <seealso cref="Settings.LayerPortOptions.onAfterDetachmentMessage"/> 
			public string onBeforeDetachmentMessage = "OnBeforeDetachment";
			/// <summary>
			/// Method name for AfterDetachment message. <see cref="AttachmentInfo"/> will be provided as parameter.
            /// </summary>
			/// <remarks>
			/// Note that if attachment was performed through physics joint, <see cref="AttachmentInfo.joint"/> will unavaliable at the time of this message since its already destroyed.
			/// Use <see cref="Settings.LayerPortOptions.onBeforeDetachmentMessage"/> if you need access to physics joint.
            /// </remarks>
			/// <seealso cref="Settings.LayerPortOptions.onBeforeDetachmentMessage"/> 
			public string onAfterDetachmentMessage = "OnAfterDetachment";
			/// <summary>
			/// Influence messaging fills <see cref="TerminusObject.allInfluences"/> list and fires InfluenceEnter and InfluenceExit messages according to set influence zone.
			/// Influence zones DO NOT affect attachment behaviour, but <see cref="Settings.LayerPortOptions.acceptsConnectionFromLayers"/> still affects possible influences.
			/// </summary>
			public bool useInfluenceMessaging;
			/// <summary>
			/// Radius of influence zone. (see <see cref="Settings.LayerPortOptions.useInfluenceMessaging"/> 
			/// </summary>
			/// <seealso cref="Settings.LayerPortOptions.useInfluenceMessaging"/>
			public float influenceRadius = 0;
			/// <summary>
			/// Maximum orientation difference (in degrees) between <see cref="Connector"/>s to be considered as influencing each other. (see <see cref="Settings.LayerPortOptions.useInfluenceMessaging"/> 
			/// </summary>
			/// <seealso cref="Settings.LayerPortOptions.useInfluenceMessaging"/>
			public float influenceConeAngle = 0;
			/// <summary>
			/// Method name for influence zone enter message. <see cref="ZoneInteraction"/> will be provided as parameter.
			/// </summary>
			/// <seealso cref="Settings.LayerPortOptions.useInfluenceMessaging"/>
			public string influenceMessageEnter = "ZoneEnter";
			/// <summary>
			/// Method name for influence zone exit message. <see cref="ZoneInteraction"/> will be provided as parameter.
			/// </summary>
			/// <seealso cref="Settings.LayerPortOptions.useInfluenceMessaging"/>
			public string influenceMessageExit = "ZoneExit";
			/// <summary>
			/// Determines on how rotation of this port will be handled by <see cref="Placer"/>.
			/// </summary>
			public RotationTypes rotationType = RotationTypes.self;
			/// <summary>
			/// <see cref="Port.rotationShift"/> will be clamped between X and Y of this vector.
			/// </summary>
			/// <seealso cref="Settings.LayerPortOptions.canRotate"/>
			public Vector2 rotationLimits;
			/// <summary>
			/// <see cref="Port.Rotate"/> will use this value for rotation step.
			/// </summary>
			/// <seealso cref="Settings.LayerPortOptions.canRotate"/>
			public float rotationStep = 90;
		}


		/// <summary>
		/// Types of <see cref="Port"/> rotations. Used by <see cref="Placer"/>
		/// </summary>
		public enum RotationTypes
		{
			/// <summary>
			/// <see cref="Port"/> can't be rotated.
			/// </summary>
			none = 0,
			/// <summary>
			/// <see cref="Port"/> will be rotated relative to its <see cref="Connector.owner"/> by changing its <see cref="Port.rotationShift"/>.
			/// </summary>
			self = 1,
			/// <summary>
			/// <see cref="Port"/>s <see cref="Connector.owner"/> will be rotated during placing.
			/// </summary>
			owner = 2
		}

		/// <summary>
		/// Types of possible <see cref="Port"/> orientations when aligned with other port. Used by <see cref="Port.Align"/>
		/// </summary>
		public enum OrientationTypes
		{
			/// <summary>
			/// <see cref="Port"/>s rotations should match exactly.
			/// </summary>
			exact = 0,
			/// <summary>
			/// <see cref="Port"/>s forward(ZY) planes (the one gizmo visualizes) should match.
			/// </summary>
			twosided = 1,
			/// <summary>
			/// <see cref="Port"/>s XY planes should match.
			/// </summary>
			planar = 2
		}

		/// <summary>
		/// Type of actions performed when two <see cref="Connector"/>s are attached.
		/// </summary>
        public enum AttachmentTypes
		{
			/// <summary>
			/// Connection will be performed via created physics joint. If <see cref="Settings.LayerPortOptions.jointPrefab"/> or <see cref="Settings.LayerPortOptions.jointPrefab2D"/> are not provided, default fixed joint will be created.
			/// </summary>
			physic_joints = 0,
			/// <summary>
			/// Connection will be performed via parenting <see cref="Connector.owner"/> transform that's being attached to owner transform of other <see cref="Connector"/>.
			/// </summary>
			hierarchy = 1,
			/// <summary>
			/// Actions associated with both <see cref="Settings.attachmentTypes.physic_joints"/> and <see cref="Settings.attachmentTypes.hierarchy"/> types will be performed.
			/// </summary>
			joints_hierarchy = 2,
			/// <summary>
			/// Combines rigidbodies of attached <see cref="TerminusObject"/>s together. They keep their colliders, but now share one rigidbody. Also parents transforms, same as hierarchy connections.
			/// Works only on connectors with <see cref="Connector.useOwnerRigidbody"/> set to true.
			/// Will fall through to <see cref="Settings.attachmentTypes.physic_joints"/> behaviour if welding is impossible.
			/// </summary>
			rigidbody_welding = 3,
			/// <summary>
			/// No actions will be performed during attachment. All other functionality including messaging and influence messaging will work. Use it to implement your own attachment mechanisms.
			/// </summary>
			empty = 4
		}

		/// <summary>
		/// Returns array of all layer names in order.
		/// </summary>
		public string[] GetLayersNames()
		{
			return layers.Select(rec => rec.name).ToArray();
		}

		/// <summary>
		/// Returns array of GUIContents containing all layer names in order.
		/// </summary>
		public GUIContent[] GetLayersNamesAsGUIContent()
		{
			return layers.Select(rec => new GUIContent(rec.name)).ToArray();
		}



        public void SetParameterInfo(int prefabIndex, string component, AssemblyParameterInfo parameter)
        {
            AssemblySerializableParameters par = prefabs[prefabIndex].parameters.Find(rec => rec.component == component);
            if (par == null)
            {
                if (parameter != null && parameter.active)
                {                    
                    par = new AssemblySerializableParameters();
                    par.component = component;
                    par.parameters = new List<AssemblyParameterInfo>();
                    //par.parameters.Add(parameter);
                    par.parameters.Add(parameter);
                    prefabs[prefabIndex].parameters.Add(par);
                }
                return;
            }

            int ind = par.parameters.FindIndex(rec => rec.parameterName == parameter.parameterName);
            if (parameter != null && ind == -1)
            {
                //par.parameters.Add(parameter);
                if (!parameter.active)
                {
                    par.parameters.RemoveAt(ind);
                    return;
                }
                par.parameters.Add(parameter);
                return;
            }

            if ((parameter == null || !parameter.active) && ind >= 0)
            {
                par.parameters.RemoveAt(ind);
                if (par.parameters.Count == 0)
                    prefabs[prefabIndex].parameters.Remove(par);
                return;
            }

            if (parameter != par.parameters[ind])
            {
                par.parameters.RemoveAt(ind);
                par.parameters.Add(parameter);
                return;
            }                                   
        }

        /*
		public void SetParameterSerialization(int prefabIndex, string component, string parameter, bool value)
		{
			AssemblySerializableParameters par = prefabs[prefabIndex].parameters.Find(rec => rec.component == component);
			if (par == null)
			{
				if (value)
				{
					par = new AssemblySerializableParameters();
					par.component = component;
					par.parameters = new List<string>();
					par.parameters.Add(parameter);
					prefabs[prefabIndex].parameters.Add(par);
				}
				return;
			}

			int ind = par.parameters.FindIndex(rec => rec == parameter);
			if (value && ind == -1)
			{
				par.parameters.Add(parameter);
				return;
			}

			if (!value && ind >= 0)
			{
				par.parameters.RemoveAt(ind);
				if (par.parameters.Count == 0)
					prefabs[prefabIndex].parameters.Remove(par);
				return;
			}
		}
        */


        public AssemblyParameterInfo GetParameterInfo(int prefabIndex, string component, string parameter)
        {
            AssemblySerializableParameters par = prefabs[prefabIndex].parameters.Find(rec => rec.component == component);
            if (par == null)
                return null;
            int ind = par.parameters.FindIndex(rec => rec.parameterName == parameter);
            if (ind == -1)
                return null;
            else
                return par.parameters[ind];
        }


        public bool GetParameterSerialization(int prefabIndex, string component, string parameter)
        {
            AssemblySerializableParameters par = prefabs[prefabIndex].parameters.Find(rec => rec.component == component);
            if (par == null)
                return false;
            int ind = par.parameters.FindIndex(rec => rec.parameterName == parameter);
            if (ind == -1)
                return false;
            else
                return par.parameters[ind].serializable;
        }

        public List<AssemblySerializableParameters> GetParametersByPartName(string partName)
        {
            for (int i = 0; i < prefabs.Count; i++)
            {
                if (prefabs[i].prefab.getPartName == partName)
                    return prefabs[i].parameters;
            }
            return null;
        }

        /*
        public bool GetParameterSerialization(int prefabIndex, string component, string parameter)
		{
			AssemblySerializableParameters par = prefabs[prefabIndex].parameters.Find(rec => rec.component == component);
			if (par == null)
				return false;
			int ind = par.parameters.FindIndex(rec => rec == parameter);
			if (ind == -1)
				return false;
			else
				return true;
		}
        */

    }
}