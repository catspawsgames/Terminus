using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Terminus.Demo2
{
	public class Ship : MonoBehaviour {


		public bool isPlayerShip;

		public bool cruise;

		public List<ShipCannon> cannons;
		public List<ShipThruster> thrusters;
		public List<ShipThruster> forwardThrusters;
		public List<ShipThruster> backThrusters;
		public List<ShipThruster> turnLeftThrusters;
		public List<ShipThruster> turnRightThrusters;
		public List<ShipThruster> strafeLeftThrusters;
		public List<ShipThruster> strafeRightThrusters;

		public bool forward;
		public bool back;
		public bool turnLeft;
		public bool turnRight;
		public bool strafeLeft;
		public bool strafeRight;

		protected BaseShipController controller;

		public void RecalculateThrusterLists()
		{
			Rigidbody2D rbody = GetComponent<Rigidbody2D>();
			forwardThrusters.Clear();
			backThrusters.Clear();
			turnLeftThrusters.Clear();
			turnRightThrusters.Clear();
			strafeLeftThrusters.Clear();
			strafeRightThrusters.Clear();
			Vector3 center = rbody.worldCenterOfMass;
			for (int i = 0; i < thrusters.Count; i++)
			{
				if (Vector3.Angle(thrusters[i].transform.up, transform.up) <= 45)
					forwardThrusters.Add(thrusters[i]);
				if (Vector3.Angle(thrusters[i].transform.up, -transform.up) <= 45)
					backThrusters.Add(thrusters[i]);
				if (Vector3.Angle(thrusters[i].transform.up, transform.right) <= 45)
					strafeRightThrusters.Add(thrusters[i]);
				if (Vector3.Angle(thrusters[i].transform.up, -transform.right) <= 45)
					strafeLeftThrusters.Add(thrusters[i]);
				if (Vector3.Cross(thrusters[i].transform.position - center, thrusters[i].transform.up * thrusters[i].power).z > 0.5f)
					turnLeftThrusters.Add(thrusters[i]);
				if (Vector3.Cross(thrusters[i].transform.position - center, thrusters[i].transform.up * thrusters[i].power).z < -0.5f)
					turnRightThrusters.Add(thrusters[i]);
			}
		}

		public void FireAllCannons()
		{
			for (int i = 0; i < cannons.Count; i++)
				cannons[i].Fire();
		}
			
		public void AttackedBy(Ship attacker, ShipAttachableBlock block, float damage)
		{
			controller.AttackedBy(attacker, block, damage);
		}

		public void Destroyed()
		{
			controller.Destroyed();
		}

		public void ChangeShipColor(Color color)
		{
			TerminusObject tObj = GetComponent<TerminusObject>();
			List<TerminusObject> parts = tObj.treeListDown.Select(rec => rec.otherConnector.owner).ToList();
			parts.Add(tObj);
			for (int i = 0; i < parts.Count; i++)
			{	
				ShipAttachableBlock block = parts[i].GetComponent<ShipAttachableBlock>();
				block.color = color;
				block.Damage(0);
			}
		}

		void SwitchThrusters(List<ShipThruster> thrustList, bool active)
		{
			for (int i = 0; i < thrustList.Count; i++)
			{
				thrustList[i].active = active;
			}
		}

		void Awake ()
		{
			controller = GetComponent<BaseShipController>();
		}

		void Start ()
		{
			RecalculateThrusterLists();
		}
		
		void Update () 
		{
			SwitchThrusters(thrusters,false);
			if (forward)
				SwitchThrusters(forwardThrusters,true);
			if (back)
				SwitchThrusters(backThrusters,true);
			if (turnLeft)
				SwitchThrusters(turnLeftThrusters,true);
			if (turnRight)
				SwitchThrusters(turnRightThrusters,true);
			if (strafeLeft)
				SwitchThrusters(strafeLeftThrusters,true);
			if (strafeRight)
				SwitchThrusters(strafeRightThrusters,true);
		}
	}
}