using UnityEngine;
using System.Collections;

namespace Terminus.Demo3
{
	public class PlayerActionController : MonoBehaviour {

		public RaycastHandler handler;
		public Placer placer;
		public LayerMask objectRaycastLayers;
		public RaycastHandler raycastHandler;
		public TerminusObject obj;
		public PlayerGun gun;
		public KeyCode[] inventoryHotkeys;
		public KeyCode inventorySwitchKey = KeyCode.Tab;

		protected PlayerData playerData;
		protected PlayerUI ui;
		protected float yAxisRotation;
		protected int objectIndex;


		void Awake()
		{
			playerData = GetComponent<PlayerData>();
			ui = GetComponent<PlayerUI>();
		}

		// Update is called once per frame
		void Update () 
		{
			if (Input.GetMouseButtonDown(0))
			{
				if (placer.activeObject != null)
					raycastHandler.activeUpdate = true;
				else
				{
					gun.Fire();
					raycastHandler.activeUpdate = false;
				}
			}
			else
				raycastHandler.activeUpdate = false;

			if (Input.GetMouseButtonDown(1))
			{
				
				if (placer.activeObject != null)
				{
					placer.CleanUp();
				}
			}

			if (placer.activeObject != null)
			{				
				placer.RotateActiveObject(Input.GetAxis("Mouse ScrollWheel") * 100);
			}

			if (Input.anyKeyDown)
			{
				if (Input.GetKeyDown(inventorySwitchKey))
					playerData.SwitchInventories();
				for (int i = 0; i < inventoryHotkeys.Length; i++)
				{
					if (Input.GetKeyDown(inventoryHotkeys[i]))
					{
						if (playerData.inventory.blocks.Count > i && playerData.inventory.blocks[i] != null)
						{
							placer.CleanUp();
							placer.activeObject = GameObject.Instantiate(playerData.inventory.blocks[i]).GetComponent<TerminusObject>();				
							placer.activeObject.gameObject.name += objectIndex++.ToString();
						}
					}
				}
			}
		}
	}
}