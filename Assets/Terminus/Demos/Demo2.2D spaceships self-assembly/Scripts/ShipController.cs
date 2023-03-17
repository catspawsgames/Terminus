using UnityEngine;
using System.Collections;
using Terminus;
using System.Linq;

namespace Terminus.Demo2
{
	public class ShipController : BaseShipController {

		public float detachForcePerMass;
		public bool godMode;
		public Restarter restarter;
		protected TerminusObject shipObj;
		protected Rigidbody2D selfRbody;

		void Awake()
		{
			ship = GetComponent<Ship>();
			shipObj = GetComponent<TerminusObject>();
			selfRbody = GetComponent<Rigidbody2D>();
		}

		void DetachPart(TerminusObject obj)
		{
			AttachmentInfo parent = obj.parent;
			if (parent.attachmentType != AttachmentInfo.Types.none)
			{
				TerminusObject[] children = obj.children.Select(rec => rec.otherConnector.owner).ToArray();
				for (int i = 0; i < children.Length; i++)
				{
					DetachPart(children[i]);
				}			
				Vector3 detachVect = parent.selfConnector.globalRotation * -Vector3.forward;
				Vector3 detachPos =  parent.selfConnector.globalPosition;
				obj.DetachFromParent();
				Rigidbody2D rbody = obj.GetComponent<Rigidbody2D>();
				rbody.transform.position += detachVect * 0.2f;
				rbody.velocity = selfRbody.GetPointVelocity(rbody.transform.position);
				rbody.AddForceAtPosition(Utils.XY(detachVect) * detachForcePerMass * rbody.mass, Utils.XY(detachPos),ForceMode2D.Impulse);
			}
		}

		public override void Destroyed ()
		{
			restarter.Restart();
		}

		// Update is called once per frame
		void Update () 
		{
			if (Input.GetKeyDown(KeyCode.W))
				ship.forward = true;
			if (Input.GetKeyDown(KeyCode.S))
				ship.back = true;
			if (Input.GetKeyDown(KeyCode.A))
				ship.turnLeft = true;
			if (Input.GetKeyDown(KeyCode.D))
				ship.turnRight = true;
			if (Input.GetKeyDown(KeyCode.Q))
				ship.strafeLeft = true;
			if (Input.GetKeyDown(KeyCode.E))
				ship.strafeRight = true;

			if (Input.GetKeyUp(KeyCode.W))
				ship.forward = false;
			if (Input.GetKeyUp(KeyCode.S))
				ship.back = false;
			if (Input.GetKeyUp(KeyCode.A))
				ship.turnLeft = false;
			if (Input.GetKeyUp(KeyCode.D))
				ship.turnRight = false;
			if (Input.GetKeyUp(KeyCode.Q))
				ship.strafeLeft = false;
			if (Input.GetKeyUp(KeyCode.E))
				ship.strafeRight = false;

			if (Input.GetKey(KeyCode.Space))
			    ship.FireAllCannons();

			if (Input.GetMouseButtonDown(0))
			{
				RaycastHit2D hit = Physics2D.Raycast(new Vector2(Camera.main.ScreenToWorldPoint(Input.mousePosition).x,Camera.main.ScreenToWorldPoint(Input.mousePosition).y), Vector2.zero, 0f);
				if (hit.collider != null)
				{
					//TerminusObject obj = hit.collider.transform.GetComponent<TerminusObject>();
					ShipAttachableBlock block = hit.collider.transform.GetComponent<ShipAttachableBlock>();
					if (block != null && !block.cockpit && block.currentShip != null && block.currentShip.isPlayerShip)
					{
						block.DetachPart();
						//DetachPart(obj);
					}
					else if (godMode && block != null && (block.currentShip == null || !block.currentShip.isPlayerShip))
					{
						block.Damage(1000);
					}
				}
			}
		}
	}
}