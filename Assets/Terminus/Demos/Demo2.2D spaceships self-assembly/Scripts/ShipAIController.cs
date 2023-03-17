using UnityEngine;
using System.Collections;

namespace Terminus.Demo2
{
	public class ShipAIController : BaseShipController 
	{
		public bool aggressive;
		public Ship attackTarget;
		public Vector2 moveTarget;

		protected Rigidbody2D rbody;

		float GetTargetCourse(Vector2 target)
		{
			Vector2 fromVector = Utils.XY(ship.transform.up).normalized;
			Vector2 toVector = (target - Utils.XY(ship.transform.position)).normalized;
			Vector3 cross = Vector3.Cross(fromVector, toVector);

			float result = Vector2.Angle(fromVector,toVector);

			if (cross.z > 0)
				result = - result;
			return result;
		}

		public override void AttackedBy (Ship attacker, ShipAttachableBlock block, float damage)
		{
			aggressive = true;
			attackTarget = attacker;
		}

		void Update()
		{
			if (aggressive)
			{
				if (attackTarget == null)
				{
					aggressive = false;
					return;
				}
				float course = GetTargetCourse(Utils.XY(attackTarget.transform.position));
				float angularSpeed = rbody.angularVelocity;
				ship.cruise = true;
				ship.forward = Mathf.Abs(course) < 5;
				ship.turnLeft = course < -2 || (course > -2 && course < 2 && angularSpeed > 10);
				ship.turnRight = course > 2 || (course > -2 && course < 2 && angularSpeed < -10);

				if (course < 5 && course > -5)
					ship.FireAllCannons();
			}
			else
			{
				float course = GetTargetCourse(moveTarget);
				float angularSpeed = rbody.angularVelocity;
				ship.cruise = true;
				ship.forward = Mathf.Abs(course) < 5;
				ship.turnLeft = course < -2 || (course > -2 && course < 2 && angularSpeed > 10);
				ship.turnRight = course > 2 || (course > -2 && course < 2 && angularSpeed < -10);
			}
		}

		void Awake()
		{
			ship = GetComponent<Ship>();
			rbody = ship.GetComponent<Rigidbody2D>();
		}

	}
}