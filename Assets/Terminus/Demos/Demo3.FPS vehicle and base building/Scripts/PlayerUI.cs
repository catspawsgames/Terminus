using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

namespace Terminus.Demo3
{
	public class PlayerUI : MonoBehaviour {

		public RectTransform inventoryPanel;
		public RectTransform panel;
		public Text currentInventoryName;
		public InventorySlot[] slots;
		public int slotsCount;
		public Color slotActiveColor;
		public Color slotInactiveColor;

		public Text interactionText;
		public LayerMask interactionLayers;
		public float maxInteractionDistance = 5.0f;
		public KeyCode[] interactionKeys;

		protected PlayerData playerData;
		protected PlayerActionController controller;
		protected InteractableObject currentInteractableObject;
		protected int currentInteractionsCount;
		protected Collider currentRaycastedCollider;
		protected RaycastHit hit;

		[System.Serializable]
		public class InventorySlot
		{
			public RectTransform mainTransform;
			public Image panel;
			public Image icon;
			public Text text;
		}

		void Awake()
		{
			playerData = GetComponent<PlayerData>();
			controller = GetComponent<PlayerActionController>();

			Array.Resize(ref slots, slotsCount);

			for (int i = 1; i < slotsCount; i++)
			{
				slots[i] = new InventorySlot();
				slots[i].mainTransform = (RectTransform)((GameObject)GameObject.Instantiate(slots[0].mainTransform.gameObject,slots[0].mainTransform.parent)).transform;
				slots[i].mainTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, slots[0].mainTransform.offsetMin.x + i * slots[i].mainTransform.rect.width, slots[i].mainTransform.rect.width);
				slots[i].panel = slots[i].mainTransform.GetComponent<Image>();
				slots[i].icon = slots[i].mainTransform.Find("Icon").GetComponent<Image>();
				slots[i].text = slots[i].mainTransform.Find("Text").GetComponent<Text>();
			}

			panel.sizeDelta = new Vector2(slots[1].mainTransform.sizeDelta.x * slotsCount + slots[0].mainTransform.offsetMin.x * 2,panel.sizeDelta.y);
		}


		public void RefreshInventoryUI()
		{
			currentInventoryName.text = playerData.inventory.name;
			for (int i = 0; i < Mathf.Min(playerData.inventory.blocks.Count,slots.Length); i++)
			{
				if (playerData.inventory.blocks[i] != null)
				{
					slots[i].panel.color = slotActiveColor;
					slots[i].icon.gameObject.SetActive(true);
					slots[i].icon.sprite = playerData.inventory.blocks[i].uiInfo.icon;
					slots[i].text.text = GetKeyString(i) + "." + playerData.inventory.blocks[i].uiInfo.partName;
				}
				else
				{
					SetSlotInactive(i);
				}
			}

			for (int i = Mathf.Min(playerData.inventory.blocks.Count,slots.Length); i < slots.Length; i++)
			{
				SetSlotInactive(i);
			}
		}

		void SetSlotInactive(int ind)
		{
			slots[ind].panel.color = slotInactiveColor;
			slots[ind].icon.gameObject.SetActive(false);
			slots[ind].text.text = GetKeyString(ind);
		}

		string GetKeyString(int ind)
		{
			string output = (ind + 1).ToString();
			return output[output.Length-1].ToString();
		}

		void CheckInteractableObjects()
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

			if (Physics.Raycast(ray,out hit,maxInteractionDistance,interactionLayers))
			{				
				if (currentRaycastedCollider != hit.collider)
				{
					currentRaycastedCollider = hit.collider;
					InteractableObject intObj = hit.collider.gameObject.GetComponent<InteractableObject>();

					if (intObj != null && intObj.GetType() == typeof(RedirectedInteractableObject))
						intObj = ((RedirectedInteractableObject)intObj).redirectToObject;

					if (intObj == null || hit.distance > intObj.interactionDistance)
					{
						currentRaycastedCollider = null;
						currentInteractableObject = null;
						interactionText.text = "";
						return;
					}

					if (currentInteractableObject != intObj)
					{
						currentInteractableObject = intObj;
						interactionText.text = "";
						string[] interactions = intObj.GetPossibleInteractions();
						currentInteractionsCount = interactions.Length;
						for (int i = 0; i < interactions.Length; i++)
						{
							if (interactionKeys.Length > i)
								interactionText.text += interactionKeys[i].ToString() + " : " + interactions[i] + Environment.NewLine;
							else
								interactionText.text += "UNDEFINED : " + interactions[i] + Environment.NewLine;
						}
					}
				}
			}
			else
			{
				currentRaycastedCollider = null;
				currentInteractableObject = null;
				interactionText.text = "";
				return;
			}
		}

		void Update ()
		{
			CheckInteractableObjects();
			if (currentInteractableObject != null)
			{
				for (int i = 0; i < Mathf.Min(currentInteractionsCount, interactionKeys.Length); i++)
				{
					if (Input.GetKeyDown(interactionKeys[i]))
					{
						currentInteractableObject.Interact(i,controller);
						currentRaycastedCollider = null;
						currentInteractableObject = null;
						interactionText.text = "";
						return;
					}
				}
			}
		}
	}
}