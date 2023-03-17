using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Terminus.Demo3
{
	public class VehicleControllableObject : MonoBehaviour 
	{
		public VehicleController activeController;
		public List<VehicleControls> activeControls;
		protected TerminusObject termObj;
		public List<VehicleController> allControllers;

		public virtual void InputChanged()
		{
		}
			

		void Awake()
		{
			termObj = GetComponent<TerminusObject>();
		}
	}
}