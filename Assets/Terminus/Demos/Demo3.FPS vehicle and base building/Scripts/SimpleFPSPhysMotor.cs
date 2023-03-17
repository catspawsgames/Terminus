using UnityEngine;
using System.Collections;

namespace Terminus.Demo3
{
	public class SimpleFPSPhysMotor : MonoBehaviour 
	{		
		public Transform feetRaycastAnchor;
		public float feetCastDistance = 0.05f;
		public float feetCastRadius = 0.5f;
		public LayerMask groundLayers;
		public Vector3 moveVector;
		public float maxVelocity;
		public float velocityStopThreshold;
		public float groundedForce;
		public AnimationCurve forceCurve;
		public float airborneForce;
		public float jumpForce;
		public float jumpRecharge;
		public float jumpVelocityLimiter;

		protected Rigidbody rbody;
		protected Vector3 forceVector = Vector3.zero;
		protected Collider standingOnCollider;
		protected Rigidbody standingOnRigidbody;
		protected bool jump;
		protected bool airborne;

		protected float jumpTimeout;


		public bool Jump()
		{
			if (jump || jumpTimeout > 0)
				return false;

			jump = true;
			jumpTimeout = jumpRecharge;
			return true;
		}

		private Vector3 Flatten(Vector3 original)
		{
			return new Vector3(original.x,0,original.z);
		}

		void FixedUpdate ()
		{			
			if (forceVector != Vector3.zero)
			{				
				if (airborne)
					rbody.AddForce(forceVector * airborneForce,ForceMode.Force);
				else
				{
					Vector3 force = forceVector * groundedForce * forceCurve.Evaluate(rbody.velocity.magnitude * Vector3.Dot(rbody.velocity.normalized,moveVector.normalized) / maxVelocity);
					rbody.AddForce(force ,ForceMode.Force);
					if (standingOnRigidbody != null)
						standingOnRigidbody.AddForceAtPosition(-force,feetRaycastAnchor.position + Vector3.down * feetCastDistance, ForceMode.Force);
				}
			}
			if (!airborne && moveVector == Vector3.zero) 
			{
				if (rbody.velocity.magnitude < velocityStopThreshold)
					rbody.velocity = Vector3.zero;
			}
			if (jump
				&& !airborne
				&& ((standingOnRigidbody == null && rbody.velocity.sqrMagnitude < jumpVelocityLimiter * jumpVelocityLimiter) 
					|| (standingOnRigidbody != null && (rbody.velocity - standingOnRigidbody.GetPointVelocity(feetRaycastAnchor.position + Vector3.down * feetCastRadius)).sqrMagnitude < jumpVelocityLimiter * jumpVelocityLimiter)))
				rbody.AddForce((Vector3.up * 2 + moveVector).normalized * jumpForce,ForceMode.Impulse);
			jump = false;
		}

		void Update()
		{
			RaycastHit hit;
			if (Physics.SphereCast(feetRaycastAnchor.position, feetCastRadius, Vector3.down,out hit,feetCastDistance,groundLayers.value))
			{
				airborne = false;
				standingOnCollider = hit.collider;
				standingOnRigidbody = hit.rigidbody;
				Vector3 velocity = rbody.velocity;
				Vector3 flatVelocity = Flatten(velocity).normalized;
				Vector3 flatMoveVec = Flatten(moveVector).normalized;
				float velocityMag = velocity.magnitude;
				if (velocityMag > maxVelocity || moveVector == Vector3.zero)
				{
					if (velocityMag < velocityStopThreshold)
						forceVector = Vector3.zero;
					else
						forceVector = -flatVelocity;
				}
				else
				{
					if (velocityMag < 0.05f)
					{
						forceVector = flatMoveVec;
					}
					else
					{
						if (Vector3.Dot(flatVelocity,flatMoveVec) > 0)
						{
							forceVector = Vector3.Reflect(-flatVelocity,flatMoveVec);
						}
						else
						{
							forceVector = flatMoveVec;				
						}
					}
				}
			}
			else
			{
				airborne = true;
				standingOnCollider = null;
				standingOnRigidbody = null;
				forceVector = Flatten(moveVector).normalized;
			}
			if (jumpTimeout > 0)
				jumpTimeout -= Time.deltaTime;

		}

		void Awake()
		{
			rbody = GetComponent<Rigidbody>();
		}

	}
}
