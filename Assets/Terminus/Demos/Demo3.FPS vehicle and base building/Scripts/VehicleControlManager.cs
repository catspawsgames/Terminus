using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Terminus.Demo3
{
	/// <summary>
	/// Connects <see cref="VehicleController"/>s with <see cref="VehicleControllableObject"/>s.<para>   </para>
	/// Accepts OnAfterAttachment and OnAfterDetachment events to automatically perform this function.
	/// </summary>
	public class VehicleControlManager : MonoBehaviour {

		protected List<VehicleController> allControllers = new List<VehicleController>();
		protected List<VehicleControllableObject> allControllableObjects = new List<VehicleControllableObject>();

		protected void UpdateSingleControlObject(GameObject obj)
		{
			VehicleController controller = obj.GetComponent<VehicleController>();
			if (controller != null)
				allControllers.Add(controller);

			VehicleControllableObject contrObj = obj.GetComponent<VehicleControllableObject>();
			if (contrObj != null)
				allControllableObjects.Add(contrObj);
		}

		/// <summary>
		/// Finds all <see cref="VehicleController"/>s with <see cref="VehicleControllableObject"/>s in provided objects and all <see cref="TerminusObject"/>s down from it, and connects them to each other.
		/// First found controller becomes active controller.<para>   </para>
		/// </summary>
		public void UpdateControls(TerminusObject root)
		{
			if (root != null)
			{
				List<VehicleController> allControllers = new List<VehicleController>();
				List<VehicleControllableObject> allControllableObjects = new List<VehicleControllableObject>();
				List<TerminusObject> tree = root.treeListDownObjects;

				UpdateSingleControlObject(root.gameObject);
				for (int i = 0; i < tree.Count; i++)
				{
					UpdateSingleControlObject(tree[i].gameObject);
				}

				for (int i  = 0; i < allControllers.Count; i++)
				{
					allControllers[i].controllableObjects = allControllableObjects;
				}

				for (int i  = 0; i < allControllableObjects.Count; i++)
				{
					allControllableObjects[i].allControllers = allControllers;
					if (allControllers.Count > 0)
						allControllableObjects[i].activeController = allControllers[0];
				}
			}
		}


		public void OnAfterAttachment(AttachmentInfo info)
		{
			UpdateControls(info.selfConnector.owner.treeRoot);
		}
			
		public void OnAfterDetachment(AttachmentInfo info)
		{
			if (info.selfConnector != null && !info.selfConnector.owner.destroyFlag)
				UpdateControls(info.selfConnector.owner.treeRoot);
			if (info.otherConnector != null && !info.otherConnector.owner.destroyFlag)
				UpdateControls(info.otherConnector.owner.treeRoot);
		}
	}
}