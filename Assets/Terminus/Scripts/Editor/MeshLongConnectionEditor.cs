using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Terminus .Editors
{
	[CustomEditor( typeof(LongConnectionMesh))]
	public class MeshLongConnectionEditor : Editor {

		private SerializedProperty prop2D;
		private SerializedProperty propJointPrefab;
		private SerializedProperty propJointPrefab2D;
		private SerializedProperty propOffset1;
		private SerializedProperty propOffset2;
		private SerializedProperty propRealtimeRecalc;
		private SerializedProperty propCrossSection;
		private SerializedProperty propSmoothNormals;
		private SerializedProperty propRecalsSens;
		private SerializedProperty propMeshFilter;
		private SerializedProperty propMeshRenderer;
		private SerializedProperty propMaterial;
		private SerializedProperty propTiling;
		private SerializedProperty propTilingLength;
		private SerializedProperty propColliderType;
		private SerializedProperty propBoxCollider;
		private SerializedProperty propBoxSize;
		private SerializedProperty propCapsuleCollider;
		private SerializedProperty propBox2DCollider;
		private SerializedProperty propColliderSize;
		private SerializedProperty propColliderMargin;
		private SerializedProperty propRecalcMass;
		private SerializedProperty propMassPerLen;


		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			LongConnectionMesh lconn = (LongConnectionMesh)target;

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(prop2D);
			EditorGUILayout.PropertyField(propOffset1);
			EditorGUILayout.PropertyField(propOffset2);

			EditorGUILayout.PropertyField(propRecalcMass);

			if (propRecalcMass.boolValue)
			{
				EditorGUILayout.PropertyField(propMassPerLen);
			}

			EditorGUILayout.PropertyField(propCrossSection,true);
			EditorGUILayout.PropertyField(propSmoothNormals);
			EditorGUILayout.PropertyField(propRealtimeRecalc);
			if (lconn.realtimeRecalculate)
				EditorGUILayout.PropertyField(propRecalsSens);
			EditorGUILayout.PropertyField(propMaterial);
			EditorGUILayout.PropertyField(propTiling);
			if (lconn.tiling)
				EditorGUILayout.PropertyField(propTilingLength);
			EditorGUILayout.PropertyField(propColliderType);
			if (lconn.use2D)
			{
				switch (lconn.colliderType)
				{
				case LongConnectionMesh.ColliderTypes.box:
					EditorGUILayout.PropertyField(propBox2DCollider);
					EditorGUILayout.PropertyField(propColliderSize);
					EditorGUILayout.PropertyField(propColliderMargin);
					break;
				case LongConnectionMesh.ColliderTypes.capsule:
					EditorGUILayout.HelpBox("2D capsule colliders are not supported", MessageType.Warning);
					break;
				}
			}
			else
			{
				switch (lconn.colliderType)
				{
				case LongConnectionMesh.ColliderTypes.box:
					EditorGUILayout.PropertyField(propBoxCollider);
					EditorGUILayout.PropertyField(propBoxSize);
					EditorGUILayout.PropertyField(propColliderMargin);
					break;
				case LongConnectionMesh.ColliderTypes.capsule:
					EditorGUILayout.PropertyField(propCapsuleCollider);
					EditorGUILayout.PropertyField(propColliderSize);
					EditorGUILayout.PropertyField(propColliderMargin);
					break;
				}
			}
				
			EditorGUILayout.PropertyField(propMeshFilter);
			EditorGUILayout.PropertyField(propMeshRenderer);

			if(EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
				EditorUtility.SetDirty(lconn);
			}

		}

		void OnEnable () 
		{
			prop2D = serializedObject.FindProperty("use2D");
			propOffset1 = serializedObject.FindProperty("offset1");
			propOffset2 = serializedObject.FindProperty("offset2");
			propRealtimeRecalc = serializedObject.FindProperty("realtimeRecalculate");
			propCrossSection = serializedObject.FindProperty("crossSection");
			propSmoothNormals = serializedObject.FindProperty("smoothNormals");
			propRecalsSens = serializedObject.FindProperty("recalculateSensitivity");
			propMeshFilter = serializedObject.FindProperty("meshFilter");
			propMeshRenderer = serializedObject.FindProperty("meshRenderer");
			propMaterial = serializedObject.FindProperty("material");
			propTiling = serializedObject.FindProperty("tiling");
			propTilingLength = serializedObject.FindProperty("tilingLength");
			propColliderType = serializedObject.FindProperty("colliderType");
			propBoxCollider = serializedObject.FindProperty("boxCollider");
			propBoxSize = serializedObject.FindProperty("boxSize");
			propCapsuleCollider = serializedObject.FindProperty("capsuleCollider");
			propBox2DCollider = serializedObject.FindProperty("boxCollider2D");
			propColliderSize = serializedObject.FindProperty("colliderSize");
			propColliderMargin = serializedObject.FindProperty("colliderMargin");
			propRecalcMass = serializedObject.FindProperty("recalculateMass");
			propMassPerLen = serializedObject.FindProperty("massPerLengthUnit");

		}
	}
}