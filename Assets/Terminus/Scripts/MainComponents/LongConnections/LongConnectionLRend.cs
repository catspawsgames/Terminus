using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Terminus 
{
	/// <summary>
	/// Helper component that handles special type of <see cref="TerminusObject"/>s such as struts. Visualizes connection as <see href="http://docs.unity3d.com/ScriptReference/LineRenderer.html">LineRenderer</see>.
	/// </summary>
	/// <remarks>
	/// This class requres <see href="http://docs.unity3d.com/ScriptReference/LineRenderer.html">LineRenderer</see> component to be present on the same GameObject.
	/// Long connection TerminusObject is different in its placement procedure since it requres attaching 2 <see cref="TerminusObject.connectors"/> instead of one.
	/// It also modifies itself depending on positions of its connectors.
	/// </remarks>
	/// <seealso cref="LongConnectionMesh"/>
	/// <seealso cref="LongConnection"/>
	public class LongConnectionLRend : LongConnection {

		protected LineRenderer linerend;
		/// <summary>
		/// Should this component change tiling of LineRenderer material?
		/// </summary>
		/// <seealso cref="LongConnectionLRend.tilingLength"/>
		public bool tiling;
		/// <summary>
		/// Length of texture tile in Unity worldspace units.
		/// </summary>
		/// <seealso cref="LongConnectionLRend.tiling"/>
		public float tilingLength = 1;

		public override void LongConnectionAfterAttachment(AttachmentInfo attInfo)
		{
			base.LongConnectionAfterAttachment(attInfo);
			if (tiling)
			{
				float tilingX = linerend.material.GetTextureScale("_MainTex").x;
				linerend.material.SetTextureScale("_MainTex",new Vector2(tilingX,Vector3.Distance(owner.connectors[0].transform.TransformPoint(offset1),owner.connectors[1].transform.TransformPoint(offset2))/tilingLength));
			}
		}

		protected override void Awake()
		{
			linerend = GetComponent<LineRenderer>();
			linerend.useWorldSpace = false;
			base.Awake();
		}

		public override void Recalculate()
		{
			if (linerend != null)
			{
				linerend.SetPosition(0,offset1);
				linerend.SetPosition(1,owner.connectors[0].transform.InverseTransformPoint(owner.connectors[1].transform.TransformPoint(offset2)));
			}
		}

		
		protected void Update()
		{
			if (realtimeRecalculate)
				Recalculate();
		}
	}
}