using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Terminus;

namespace Terminus.Demo2
{
	public class Attractor2D : MonoBehaviour {

		public float force;
		public AnimationCurve forceCurve;

		protected ZoneInteraction currentInteraction;
		protected TerminusObject tObj;
		[SerializeField]
		protected bool affected = false;

		protected Vector2 forceVector;

		void Awake ()
		{			
			tObj = GetComponent<TerminusObject>();
		}

	
		void SetCurrentInteraction()
		{
			currentInteraction = tObj.closestInfluence;
			if (currentInteraction != null)
			{
				affected = true;
				float coef = forceCurve.Evaluate(Mathf.Sqrt(currentInteraction.sqrDistance)/currentInteraction.thisConnector.portOptions.influenceRadius);
				forceVector = (currentInteraction.otherConnector.globalPosition-currentInteraction.thisConnector.globalPosition).normalized * force * coef;
			}
			else
				affected = false;
		}

		// Use this for initialization
		void Start () 
		{
			SetCurrentInteraction();
		}
		
		// Update is called once per frame
		void Update () 
		{
			SetCurrentInteraction();
		}

		void FixedUpdate()
		{
			/*if (affected && rbodyManager.currentRigidbody != null)
				rbodyManager.currentRigidbody.AddForceAtPosition(forceVector,currentInteraction.thisConnector.globalPosition);*/
			if (affected && currentInteraction.thisConnector.connectorRigidbody2D != null)
				currentInteraction.thisConnector.connectorRigidbody2D.AddForceAtPosition(forceVector,currentInteraction.thisConnector.globalPosition);
		}
	}
}