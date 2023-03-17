using UnityEngine;
using System.Collections;

namespace Terminus.Demo1
{
	public class ControllableElasticString : ControllablePart {
		
		public SpringJoint joint;
		public float power = 500;
		public float maxDistance = 0;
		public float minDistance = 0;

		protected override void ControlDown(int index)
		{
			joint.spring = power;
			joint.minDistance = minDistance;
			joint.maxDistance = maxDistance;
		}
		
		protected override void ControlUp(int index)
		{
			joint.spring = 0;
			joint.minDistance = 0;
			joint.maxDistance = float.MaxValue;
		}
	}
}
