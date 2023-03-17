using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Terminus.Demo3
{
	public class PlayerData : MonoBehaviour {

		public int maxInventorySlotCount;
		public int selectedInventory;
		[SerializeField]
		protected Inventory[] inventories;


		public Inventory inventory
		{
			get
			{
				return inventories[selectedInventory];
			}
		}

		protected PlayerUI ui;

		[System.Serializable]
		public class Inventory 
		{
			public string name;
			public List<TerminusObject> blocks;
		}

		public void SwitchInventories(int step = 1)
		{
			selectedInventory += step;
			if (selectedInventory < 0)
				selectedInventory += inventories.Length;
			else if (selectedInventory >= inventories.Length)
				selectedInventory -= inventories.Length;
			ui.RefreshInventoryUI();
		}

		void Awake()
		{			
			ui = GetComponent<PlayerUI>();
		}

		void Start()
		{
			ui.RefreshInventoryUI();
		}
	}
}