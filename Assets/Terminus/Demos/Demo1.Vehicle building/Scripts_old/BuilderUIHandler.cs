using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Terminus;

namespace Terminus.Demo1
{
	/// <summary>
	/// Handles UI events and objects for Terminus Demo1.
	/// </summary>
    public class BuilderUIHandler : MonoBehaviour
    {
		/// <summary>
		/// Time scale when not paused.
		/// </summary>
        public float timeScale = 1;
		public AnimationCurve fixedDeltaTimeMutiplier;
		/// <summary>
		/// Start block for "Center on start block" button.
		/// </summary>
        public TerminusObject startBlock;
		/// <summary>
		/// Current selected object. Object panel will be drawn for this object.
		/// </summary>
        public TerminusObject currentObject;
		/// <summary>
		/// Current active <see cref="AssemblyCameraController"/>
		/// </summary>
        public AssemblyCameraController cameraController;
		/// <summary>
		/// Current active <see cref="Terminus.Placer"/> 
		/// </summary>
        public Placer placer;
		/// <summary>
		/// Canvas used to render UI. For enabling and disabling.
		/// </summary>
		public Canvas mainCanvas;
		[Header("Breakable joints control")]
		public Toggle breakableJointsToggle;
		public float jointBreakForce = 100000;
		public float jointBreakTorque = float.MaxValue;
		/// <summary>
		/// Defines contents of main building menu.
		/// </summary>
        [Header("Building blocks definition")]
		public List<BlockGroup> blockGroups;
        [Header("Building blocks controls")]
		public RectTransform buildPanel;
        public Button originalBuildButton;
        public Toggle originalTabToggle;
        public ToggleGroup tabsToggleGroup;
        public RectTransform buildButtonsPanel;
        public Scrollbar buildScrollbar;
        public float buildButtonInterval = 10;
        public float maxBuildPanelWidth = 800;
        [Header("Symmetry control setup")]
        public Text symmetryCountText;
        public Slider symmetrySlider;

        [Header("Position handles")]
        public Transform handles;
        public LayerMask handlesLayers;
        public GameObject upHandle;
        public Color upHandleColor;
        public GameObject rightHandle;
        public Color rightHandleColor;
        public GameObject forwardHandle;
        public Color forwardHandleColor;
        public Color hoverColor;
        public Color draggingColor;
        public float handleSensitivity;
        public Toggle handlesToggle;

        [Header("Object panel")]
        public RectTransform objectPanel;
        public float maxObjectPanelHeight = 300;
        public Text currentObjectName;
        public RectTransform controlsText;
        public RectTransform parametersText;
        public RectTransform placementText;
        public RectTransform connectionsText;
        public RectTransform parametersPanel;
        public RectTransform keySelectionPanel;
        public GameObject standardKeyBinding;
        public GameObject standardSlider;
        public GameObject standardToggle;
        public GameObject placementCyclePort;
        public GameObject placementRotatePort;
        public GameObject standardPortConnection;
        public GameObject standardSurfaceConnection;
        public Button pickupButton;
        public Button deleteButton;
        public KeyCode[] forbiddenKeys;
        [Header("Camera controls")]
        public RectTransform cameraButton;
        public Image cameraButtonFree;
        public Image cameraButtonFollow;
        [Header("Play/stop controrls")]
        public Text playStopText;
        public Text pauseText;
        public Button pauseButton;
        [Header("Save/load controls")]
        public RectTransform loadVehiclesPanel;
        public InputField vehicleNameInput;
        public Button saveVehicleButton;
        public Button defaultVehicleButton;
        public string vehiclesFolder = "Vehicles";
        public Vector3 spawnPosition;


        protected int activeGroup;
        protected int currentBlockIndex;

		protected float originalFixedDeltaTime;

        protected List<GameObject> currentParametersPanelContent = new List<GameObject>();
        protected List<GameObject> currentLoadVehiclePanelContent = new List<GameObject>();
        protected List<GameObject> currentBuildButtons = new List<GameObject>();

        protected List<Toggle> tabs;
        protected float minBuildPanelWidth;

        protected AdjustableParametersHandler paramHandler;

        protected bool paused;

        protected bool inKeySelectionMode;
        protected bool keySelectionAltKey;
        protected int keySelectionIndex;

        protected HandleDirections activeHandle;
        protected HandleStates handleState;
        protected Material[] upHandleMaterials;
        protected Collider[] upHandleColliders;
        protected Material[] rightHandleMaterials;
        protected Collider[] rightHandleColliders;
        protected Material[] forwardHandleMaterials;
        protected Collider[] forwardHandleColliders;

		/// <summary>
		/// Buildable block definition for main building menu.
		/// </summary>
        [System.Serializable]
        public class BuildableBlock
        {
			/// <summary>
			/// Displayed name.
			/// </summary>
            public string name;
			/// <summary>
			/// <see cref="TerminusObject"/> prefab.
			/// </summary>
            public TerminusObject TerminusObject;
			/// <summary>
			/// Image for build button.
			/// </summary>
            public Sprite sprite;
        }

		/// <summary>
		/// Defines group of buildable block that will be joined together as a tab of build menu.
		/// </summary>
        [System.Serializable]
        public class BlockGroup
        {
			/// <summary>
			/// Displayed name.
			/// </summary>
            public string name;
			/// <summary>
			/// Buildable blocks.
			/// </summary>
			public List<TerminusObject> blocks;
        }


        protected enum HandleDirections
        {
            up = 0,
            right = 1,
            forward = 2,
            none = 3
        }

		protected enum HandleStates
        {
            free = 0,
            hover = 1,
            drag = 2
        }


        void OnGUI()
        {
            if (inKeySelectionMode)
            {
                Event e = Event.current;
                if (e.keyCode != KeyCode.None && !forbiddenKeys.Contains(e.keyCode))
                {
                    KeyCode keyCode = e.keyCode;
                    if (e.keyCode == KeyCode.Escape)
                        keyCode = KeyCode.None;
                    inKeySelectionMode = false;
                    keySelectionPanel.gameObject.SetActive(false);
                    if (keySelectionAltKey)
                    {
                        currentObject.GetComponent<ControllablePart>().controls[keySelectionIndex].altKey = keyCode;
                    }
                    else
                        currentObject.GetComponent<ControllablePart>().controls[keySelectionIndex].key = keyCode;
                    UpdateCurrentObject(currentObject);
                }
            }
        }

        void Awake()
        {
			originalFixedDeltaTime = Time.fixedDeltaTime;

            upHandleColliders = upHandle.GetComponentsInChildren<Collider>();
            rightHandleColliders = rightHandle.GetComponentsInChildren<Collider>();
            forwardHandleColliders = forwardHandle.GetComponentsInChildren<Collider>();

            upHandleMaterials = upHandle.GetComponentsInChildren<Renderer>().Select(rec => rec.material).ToArray();
            rightHandleMaterials = rightHandle.GetComponentsInChildren<Renderer>().Select(rec => rec.material).ToArray();
            forwardHandleMaterials = forwardHandle.GetComponentsInChildren<Renderer>().Select(rec => rec.material).ToArray();

            for (int i = 0; i < upHandleMaterials.Length; i++)
            {
                upHandleMaterials[i].color = upHandleColor;
                upHandleMaterials[i].renderQueue = 4000;
            }
            for (int i = 0; i < rightHandleMaterials.Length; i++)
                rightHandleMaterials[i].color = rightHandleColor;
            for (int i = 0; i < forwardHandleMaterials.Length; i++)
                forwardHandleMaterials[i].color = forwardHandleColor;
        }


        // Use this for initialization
        void Start()
        {
			ChangeTimescale();
                
            float left = 0;
            tabs = new List<Toggle>();
            for (int i = 0; i < blockGroups.Count; i++)
            {
                GameObject obj = Instantiate(originalTabToggle.gameObject);
                obj.SetActive(true);
                //currentParametersPanelContent.Add(obj);
                RectTransform rectTr = obj.GetComponent<RectTransform>();
                rectTr.SetParent(originalTabToggle.transform.parent, false);
                rectTr.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, left, rectTr.rect.width);
                obj.GetComponentInChildren<Text>().text = "F" + (i + 1).ToString() + "." + blockGroups[i].name;
                int index = i;
                tabs.Add(obj.GetComponent<Toggle>());
                tabs[i].onValueChanged.AddListener(delegate { SetupActiveTab(index, false); });
                left += rectTr.rect.width;
                tabsToggleGroup.RegisterToggle(tabs[i]);
                tabs[i].group = tabsToggleGroup;
            }
            minBuildPanelWidth = left;

            if (blockGroups.Count > 0)
            {
                SetupActiveTab(0);
            }

            HandlesToggle();
        }

        void Update()
        {
            if (!Manager.Instance.globalPlaymode && currentObject != null && currentObject != placer.activeObject)
            {
                if (!EventSystem.current.IsPointerOverGameObject() && handleState != HandleStates.drag)
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;

                    HandleDirections newHandle = HandleDirections.none;
                    if (Physics.Raycast(ray, out hit, 1000, handlesLayers))
                    {
                        if (upHandleColliders.Contains(hit.collider))
                            newHandle = HandleDirections.up;
                        else if (rightHandleColliders.Contains(hit.collider))
                            newHandle = HandleDirections.right;
                        else if (forwardHandleColliders.Contains(hit.collider))
                            newHandle = HandleDirections.forward;
                    }

                    if (newHandle != HandleDirections.none)
                        handleState = HandleStates.hover;
                    else
                        handleState = HandleStates.free;

                    if (activeHandle != newHandle)
                    {
                        switch (activeHandle)
                        {
                            case HandleDirections.up:
                                for (int i = 0; i < upHandleMaterials.Length; i++)
                                    upHandleMaterials[i].color = upHandleColor;
                                break;
                            case HandleDirections.right:
                                for (int i = 0; i < rightHandleMaterials.Length; i++)
                                    rightHandleMaterials[i].color = rightHandleColor;
                                break;
                            case HandleDirections.forward:
                                for (int i = 0; i < forwardHandleMaterials.Length; i++)
                                    forwardHandleMaterials[i].color = forwardHandleColor;
                                break;
                        }

                        activeHandle = newHandle;

                        switch (newHandle)
                        {
                            case HandleDirections.up:
                                for (int i = 0; i < upHandleMaterials.Length; i++)
                                    upHandleMaterials[i].color = hoverColor;
                                break;
                            case HandleDirections.right:
                                for (int i = 0; i < rightHandleMaterials.Length; i++)
                                    rightHandleMaterials[i].color = hoverColor;
                                break;
                            case HandleDirections.forward:
                                for (int i = 0; i < forwardHandleMaterials.Length; i++)
                                    forwardHandleMaterials[i].color = hoverColor;
                                break;
                        }
                    }

                    if (activeHandle != HandleDirections.none && Input.GetMouseButtonDown(0))
                    {
                        handleState = HandleStates.drag;

                        switch (activeHandle)
                        {
                            case HandleDirections.up:
                                for (int i = 0; i < upHandleMaterials.Length; i++)
                                    upHandleMaterials[i].color = draggingColor;
                                break;
                            case HandleDirections.right:
                                for (int i = 0; i < rightHandleMaterials.Length; i++)
                                    rightHandleMaterials[i].color = draggingColor;
                                break;
                            case HandleDirections.forward:
                                for (int i = 0; i < forwardHandleMaterials.Length; i++)
                                    forwardHandleMaterials[i].color = draggingColor;
                                break;
                        }
                    }
                }

                if (handleState == HandleStates.drag)
                {
                    if (Input.GetMouseButtonUp(0))
                    {
                        handleState = HandleStates.free;

                        switch (activeHandle)
                        {
                            case HandleDirections.up:
                                for (int i = 0; i < upHandleMaterials.Length; i++)
                                    upHandleMaterials[i].color = upHandleColor;
                                break;
                            case HandleDirections.right:
                                for (int i = 0; i < rightHandleMaterials.Length; i++)
                                    rightHandleMaterials[i].color = rightHandleColor;
                                break;
                            case HandleDirections.forward:
                                for (int i = 0; i < forwardHandleMaterials.Length; i++)
                                    forwardHandleMaterials[i].color = forwardHandleColor;
                                break;
                        }
                    }
                    else
                    {
                        float distanceToHandle = 0;
                        switch (activeHandle)
                        {
                            case HandleDirections.up:
                                distanceToHandle = (Camera.main.transform.position - upHandleColliders[0].transform.position).magnitude;
                                currentObject.treeRoot.position += Vector3.up * handleSensitivity * distanceToHandle * Input.GetAxis("Mouse Y")
                                                                   * Vector3.Dot(Camera.main.transform.up, currentObject.treeRoot.rotation * Vector3.up);
                                break;
                            case HandleDirections.right:
                                distanceToHandle = (Camera.main.transform.position - rightHandleColliders[0].transform.position).magnitude;
                                currentObject.treeRoot.position += Vector3.right * handleSensitivity * distanceToHandle
                                                                   * (Input.GetAxis("Mouse Y") * Vector3.Dot(Camera.main.transform.up, currentObject.treeRoot.rotation * Vector3.right)
                                                                     + Input.GetAxis("Mouse X") * Vector3.Dot(Camera.main.transform.right, currentObject.treeRoot.rotation * Vector3.right));
                                break;
                            case HandleDirections.forward:
                                distanceToHandle = (Camera.main.transform.position - forwardHandleColliders[0].transform.position).magnitude;
                                currentObject.treeRoot.position += Vector3.forward * handleSensitivity * distanceToHandle
                                                                   * (Input.GetAxis("Mouse Y") * Vector3.Dot(Camera.main.transform.up, currentObject.treeRoot.rotation * Vector3.forward)
                                                                      + Input.GetAxis("Mouse X") * Vector3.Dot(Camera.main.transform.right, currentObject.treeRoot.rotation * Vector3.forward));
                                break;
                        }
                    }
                }

                handles.position = currentObject.treeRoot.position;
            }
        }

		public void ChangeTimescale(Slider timeSlider = null)
		{
			if (timeSlider != null)
				timeScale = timeSlider.value;
			Time.timeScale = timeScale;
			Time.fixedDeltaTime = originalFixedDeltaTime * fixedDeltaTimeMutiplier.Evaluate(timeScale);
		}

        protected void SetupActiveTab(int tab, bool ignoreTabs = true)
        {
            if (ignoreTabs || tabs[tab].isOn)
            {
                for (int i = 0; i < currentBuildButtons.Count; i++)
                    Destroy(currentBuildButtons[i]);
                currentBuildButtons.Clear();

                activeGroup = tab;
                tabs[tab].isOn = true;

                float left = buildButtonInterval;

                for (int i = 0; i < blockGroups[tab].blocks.Count; i++)
                {
                    GameObject obj = Instantiate(originalBuildButton.gameObject);
                    obj.SetActive(true);
                    currentBuildButtons.Add(obj);
                    RectTransform rectTr = obj.GetComponent<RectTransform>();
                    rectTr.SetParent(originalBuildButton.transform.parent, false);
                    rectTr.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, left, rectTr.rect.width);
                    if (i < 9)
						obj.GetComponentInChildren<Text>().text = (i + 1).ToString() + "." + blockGroups[tab].blocks[i].uiInfo.partName;
                    else if (i == 9)
                        obj.GetComponentInChildren<Text>().text = "0." + blockGroups[tab].blocks[i].name;
                    else
                        obj.GetComponentInChildren<Text>().text = blockGroups[tab].blocks[i].name;
					obj.transform.Find("Image").GetComponent<Image>().sprite = blockGroups[tab].blocks[i].uiInfo.icon;
                    int index = i;
                    obj.GetComponent<Button>().onClick.AddListener(delegate { OnBuildKeyPressed(index); });
                    left += rectTr.rect.width + buildButtonInterval;
                }
                buildButtonsPanel.sizeDelta = new Vector2(Mathf.Clamp(left, minBuildPanelWidth, maxBuildPanelWidth), buildButtonsPanel.sizeDelta.y);
				buildPanel.sizeDelta = new Vector2(buildButtonsPanel.sizeDelta.x,buildPanel.sizeDelta.y);
                buildScrollbar.gameObject.SetActive(left > maxBuildPanelWidth);
            }
        }


        public void OnSymmetryValueChanged(int newValue)
        {
            if (newValue == -1)
            {
                placer.symmetryAttachmentsCount = (int)symmetrySlider.value;
            }
            else
            {
                placer.symmetryAttachmentsCount = newValue;
            }
            symmetryCountText.text = placer.symmetryAttachmentsCount.ToString();
        }

        public void OnBuildKeyPressed(int buttonInd)
        {
            GameObject obj = Instantiate(blockGroups[activeGroup].blocks[buttonInd].gameObject);
            obj.name += currentBlockIndex++.ToString();
            placer.CleanUp();
            TerminusObject termObj = obj.GetComponent<TerminusObject>();
            placer.activeObject = termObj;
            UpdateCurrentObject(termObj);
        }

        public void OnTabKeyPressed(int buttonInd)
        {
            SetupActiveTab(buttonInd);
        }


        public void HandlesToggle()
        {
            if (handles.gameObject.activeSelf && !handlesToggle.isOn)
                handles.gameObject.SetActive(false);
            UpdateCurrentObject(currentObject);
        }


		public void BreakableJointsToggle()
		{
			List<TerminusObject> roots = Manager.Instance.rootObjects;
			List<AttachmentInfo> attachments = new List<AttachmentInfo>();		
			for (int i = 0; i < roots.Count; i++)
				attachments.AddRange(roots[i].treeListDown);

			float currentJointBreakForce = float.MaxValue;
			float currentJointBreakTorque = float.MaxValue;
			if (breakableJointsToggle.isOn)
			{
				currentJointBreakForce = jointBreakForce;
				currentJointBreakTorque = jointBreakTorque;
			}

			for (int i = 0; i < attachments.Count; i++)
			{
				if (attachments[i].joint != null && attachments[i].joint is Joint)
				{
					((Joint)attachments[i].joint).breakForce = currentJointBreakForce;
					((Joint)attachments[i].joint).breakTorque = currentJointBreakTorque;
				}
			}
		}


		/// <summary>
		/// Changes current selected object and redraws object panel.
		/// </summary>
		/// <param name="newObject">New selected object. Null to cancel selection.</param>
        public void UpdateCurrentObject(TerminusObject newObject)
        {
            keySelectionPanel.gameObject.SetActive(false);
            inKeySelectionMode = false;
            handleState = HandleStates.free;
            if (newObject != currentObject
                && !(newObject != null && currentObject != null &&
                     ((newObject.playmodeClone != null && newObject.playmodeClone == currentObject.playmodeOriginal)
                        || (newObject.playmodeOriginal != null && newObject.playmodeOriginal == currentObject.playmodeClone))))
            {
                if (cameraController.follow)
                {
                    if (newObject == null || placer.activeObject == newObject)
                        ChangeCameraBehaviour(true);
                    else
                    {
                        cameraController.FocusOnPoint(newObject.position);
                        cameraController.target = newObject;
                    }
                }
            }
            if (newObject == null)
            {
                handles.gameObject.SetActive(false);
                objectPanel.gameObject.SetActive(false);
                controlsText.gameObject.SetActive(false);
                parametersText.gameObject.SetActive(false);
                placementText.gameObject.SetActive(false);
                connectionsText.gameObject.SetActive(false);
                saveVehicleButton.interactable = false;
                vehicleNameInput.interactable = false;
                if (placer.activeObject == currentObject)
                {
                    placer.CleanUp();
                    placer.activeObject = null;
                }
                currentObject = null;
            }
            else
            {
                controlsText.gameObject.SetActive(false);
                parametersText.gameObject.SetActive(false);
                placementText.gameObject.SetActive(false);
                connectionsText.gameObject.SetActive(false);

                saveVehicleButton.interactable = currentObject != placer.activeObject;
                vehicleNameInput.interactable = currentObject != placer.activeObject;

                currentObject = newObject;
                objectPanel.gameObject.SetActive(true);
                for (int i = 0; i < currentParametersPanelContent.Count; i++)
                {
                    Destroy(currentParametersPanelContent[i]);
                }
                currentParametersPanelContent.Clear();

                currentObjectName.text = currentObject.getPartName;

                GameObject obj;
                RectTransform rectTr;

                float top = 0;

                if (placer.activeObject == currentObject)
                {
                    ChangeCameraBehaviour(true);
                    handles.gameObject.SetActive(false);
                    cameraButton.gameObject.SetActive(false);
                    pickupButton.gameObject.SetActive(false);
                    deleteButton.gameObject.SetActive(false);
                    if (!currentObject.longConnection)
                    {
                        if (currentObject.currentAttachmentPort == null)
                            placer.activeObject.SetNextAttachmentPort();

                        if (currentObject.currentAttachmentPort != null)
                        {
                            if (top != 0)
                                top += 5;

                            placementText.gameObject.SetActive(true);
                            placementText.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, top, placementText.rect.height);
                            top += placementText.rect.height;

                            obj = Instantiate(placementCyclePort);
                            obj.SetActive(true);
                            currentParametersPanelContent.Add(obj);
                            rectTr = obj.GetComponent<RectTransform>();
                            rectTr.SetParent(parametersPanel, false);
                            rectTr.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, top, rectTr.rect.height);
                            rectTr.GetChild(0).GetComponent<Text>().text = currentObject.currentAttachmentPort.connectorName;
                            top += rectTr.rect.height;

							if (currentObject.currentAttachmentPort.portOptions.rotationType == Settings.RotationTypes.self)
                            {
                                obj = Instantiate(placementRotatePort);
                                obj.SetActive(true);
                                currentParametersPanelContent.Add(obj);
                                rectTr = obj.GetComponent<RectTransform>();
                                rectTr.SetParent(parametersPanel, false);
                                rectTr.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, top, rectTr.rect.height);
                                rectTr.GetChild(0).GetComponent<Text>().text = currentObject.currentAttachmentPort.rotationShift.ToString() + "°";
                                top += rectTr.rect.height;
                            }
                        }
                    }
                }
                else
                {
                    handles.gameObject.SetActive(!Manager.Instance.globalPlaymode && handlesToggle.isOn);
                    handles.position = currentObject.position;
                    handles.rotation = Quaternion.identity;
                    cameraButton.gameObject.SetActive(true);
                    deleteButton.gameObject.SetActive(true);
                    pickupButton.gameObject.SetActive(!currentObject.longConnection);
                    pickupButton.interactable = !placer.excludeFromPickup.Contains(currentObject);
                    deleteButton.interactable = !placer.excludeFromDestruction.Contains(currentObject);

                    if (top != 0)
                        top += 5;

                    connectionsText.gameObject.SetActive(true);
                    connectionsText.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, top, connectionsText.rect.height);
                    top += connectionsText.rect.height;

                    for (int i = 0; i < currentObject.connectors.Count; i++)
                    {
                        if (currentObject.connectors[i] is Port)
                        {
                            Port port = (Port)currentObject.connectors[i];
                            if (port.attachmentInfo.attachmentType != AttachmentInfo.Types.none)
                            {
                                obj = Instantiate(standardPortConnection);
                                obj.SetActive(true);
                                currentParametersPanelContent.Add(obj);
                                rectTr = obj.GetComponent<RectTransform>();
                                rectTr.SetParent(parametersPanel, false);
                                rectTr.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, top, rectTr.rect.height);

                                switch (port.attachmentInfo.attachmentType)
                                {
                                    case AttachmentInfo.Types.child:
                                        obj.GetComponent<Text>().text = "Port:" + port.connectorName + "→" + port.attachmentInfo.otherConnector.owner.getPartName;
                                        break;
                                    case AttachmentInfo.Types.parent:
                                        obj.GetComponent<Text>().text = "Port:" + port.connectorName + "←" + port.attachmentInfo.otherConnector.owner.getPartName;
                                        break;
                                    case AttachmentInfo.Types.sideway:
                                        obj.GetComponent<Text>().text = "Port:" + port.connectorName + "↔" + port.attachmentInfo.otherConnector.owner.getPartName;
                                        break;
                                }

                                int index = i;
                                rectTr.GetChild(0).GetComponent<Button>().onClick.AddListener(delegate { DetachConnectionClick(index); });
                                top += rectTr.rect.height;
                            }
                        }
                        else
                        {
                            Surface surface = (Surface)currentObject.connectors[i];
                            if (surface.attachmentsInfo.Count > 0)
                            {
                                obj = Instantiate(standardSurfaceConnection);
                                obj.SetActive(true);
                                currentParametersPanelContent.Add(obj);
                                rectTr = obj.GetComponent<RectTransform>();
                                rectTr.SetParent(parametersPanel, false);
                                rectTr.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, top, rectTr.rect.height);
                                obj.GetComponent<Text>().text = "Surface:" + surface.connectorName;
                                int index = i;
                                rectTr.GetChild(0).GetComponent<Button>().onClick.AddListener(delegate { DetachConnectionClick(index); });
                                top += rectTr.rect.height;
                            }
                        }
                    }
                }

                ControllablePart controlPart = currentObject.GetComponent<ControllablePart>();

                if (controlPart != null)
                {
                    if (top != 0)
                        top += 5;

                    controlsText.gameObject.SetActive(true);
                    controlsText.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, top, controlsText.rect.height);
                    top += controlsText.rect.height;

                    for (int i = 0; i < controlPart.controls.Count; i++)
                    {
                        obj = Instantiate(standardKeyBinding);
                        obj.SetActive(true);
                        currentParametersPanelContent.Add(obj);
                        rectTr = obj.GetComponent<RectTransform>();
                        rectTr.SetParent(parametersPanel, false);
                        rectTr.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, top, rectTr.rect.height);
                        obj.GetComponent<Text>().text = controlPart.controls[i].name;
                        Component[] buttons = obj.GetComponentsInChildren<Button>();
                        int ind = i;
                        ((Button)buttons[0]).onClick.AddListener(delegate { KeyBindingChangeInitiated(ind, false); });
                        ((Button)buttons[1]).onClick.AddListener(delegate { KeyBindingChangeInitiated(ind, true); });
                        buttons[0].GetComponentInChildren<Text>().text = controlPart.controls[i].key.ToString();
                        buttons[1].GetComponentInChildren<Text>().text = controlPart.controls[i].altKey.ToString();
                        top += rectTr.rect.height;
                    }
                }

                paramHandler = currentObject.GetComponent<AdjustableParametersHandler>();

                if (paramHandler != null)
                {
                    if (top != 0)
                        top += 5;

                    parametersText.gameObject.SetActive(true);
                    parametersText.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, top, parametersText.rect.height);
                    top += parametersText.rect.height;

                    for (int i = 0; i < paramHandler.parameters.Length; i++)
                    {
                        int ind = i;
                        switch (paramHandler.parameters[i].controlType)
                        {
                            case AdjustableParametersHandler.UIControlTypes.slider:
                                obj = Instantiate(standardSlider);
                                obj.SetActive(true);
                                currentParametersPanelContent.Add(obj);
                                rectTr = obj.GetComponent<RectTransform>();
                                rectTr.SetParent(parametersPanel, false);
                                rectTr.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, top, rectTr.rect.height);
                                obj.GetComponent<Text>().text = paramHandler.parameters[i].displayedName;
                                Slider slider = obj.transform.GetChild(0).GetComponent<Slider>();
                                slider.minValue = paramHandler.parameters[i].minMax.x;
                                slider.maxValue = paramHandler.parameters[i].minMax.y;
                                slider.transform.Find("MinText").GetComponent<Text>().text = slider.minValue.ToString();
								slider.transform.Find("MaxText").GetComponent<Text>().text = slider.maxValue.ToString();
                                slider.onValueChanged.AddListener(delegate { AdjustableParameterChanged(ind, slider); });
                                slider.value = (float)paramHandler.GetParameterValue(i);
                                top += rectTr.rect.height;
                                break;
                            case AdjustableParametersHandler.UIControlTypes.toggle:
                                obj = Instantiate(standardToggle);
                                obj.SetActive(true);
                                currentParametersPanelContent.Add(obj);
                                rectTr = obj.GetComponent<RectTransform>();
                                rectTr.SetParent(parametersPanel, false);
                                rectTr.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, top, rectTr.rect.height);
                                rectTr.GetChild(1).GetComponent<Text>().text = paramHandler.parameters[i].displayedName;
                                Toggle toggle = obj.GetComponent<Toggle>();
                                toggle.onValueChanged.AddListener(delegate { AdjustableParameterChanged(ind, toggle); });
                                toggle.isOn = (bool)paramHandler.GetParameterValue(i);
                                top += rectTr.rect.height;
                                break;
                        }
                    }
                }

                objectPanel.sizeDelta = new Vector2(objectPanel.sizeDelta.x, Mathf.Min(maxObjectPanelHeight, top + currentObjectName.rectTransform.rect.height));
                parametersPanel.sizeDelta = new Vector2(parametersPanel.sizeDelta.x, top);
            }
        }

        public void KeyBindingChangeInitiated(int index, bool altKey)
        {
            //Debug.Log(index.ToString() + " " + altKey.ToString());
            inKeySelectionMode = true;
            keySelectionAltKey = altKey;
            keySelectionPanel.gameObject.SetActive(true);
            keySelectionIndex = index;
        }

        public void AdjustableParameterChanged(int index, Component control)
        {
            //Debug.Log(index.ToString() + " " + control.GetType().ToString());
            object value = null;
            switch (paramHandler.parameters[index].controlType)
            {
                case AdjustableParametersHandler.UIControlTypes.slider:
                    value = ((Slider)control).value;
                    break;
                case AdjustableParametersHandler.UIControlTypes.toggle:
                    value = ((Toggle)control).isOn;
                    break;
            }
            paramHandler.SetParameterValue(index, value);


            if (placer.activeObject == currentObject)
            {
                for (int i = 0; i < placer.symmetryObjects.Count; i++)
                {
                    placer.symmetryObjects[i].GetComponent<AdjustableParametersHandler>().SetParameterValue(index, value);
                }
            }
            else
            {
                if (placer.affectSymmetrySiblings)
                {
                    for (int i = 0; i < placer.symmetryObjects.Count; i++)
                    {
						if (currentObject.symmetricSiblings[i] != null)
                        	currentObject.symmetricSiblings[i].GetComponent<AdjustableParametersHandler>().SetParameterValue(index, value);
                    }
                }
            }
        }

        public void DetachConnectionClick(int index)
        {
            if (currentObject.connectors[index] is Port)
                ((Port)currentObject.connectors[index]).Detach();
            else
            {
                Surface surface = (Surface)currentObject.connectors[index];
                for (int i = 0; i < surface.attachmentsInfo.Count; i++)
                {
                    ((Port)surface.attachmentsInfo[i].otherConnector).Detach();
                }
            }
            UpdateCurrentObject(currentObject);
        }

        void TerminusPlacerObjectUpdated(TerminusObject selectedObject)
        {
            if (handleState == HandleStates.free)
                UpdateCurrentObject(selectedObject);
        }

		void TerminusPlacerObjectUpdatedNull()
        {
            if (handleState == HandleStates.free)
                UpdateCurrentObject(placer.oldActiveObject);
        }

		void TerminusPlacerObjectDeleted(TerminusObject selectedObject)
		{
			if (selectedObject == currentObject || selectedObject.treeListDown.Find(rec => rec.otherConnector.owner == currentObject).attachmentType != AttachmentInfo.Types.none)
				UpdateCurrentObject(null);
		}

        public void ObjectPanelExit()
        {
            UpdateCurrentObject(null);
        }

        public void DeleteObject()
        {
            if (placer.activeObject == currentObject)
            {
                placer.CleanUp();
            }
            else
            {
                if (currentObject.container != null)
                    Manager.FreeContainer(currentObject.container);
                Destroy(currentObject.gameObject);
            }
            UpdateCurrentObject(null);
        }

        public void ChangeCameraBehaviour(bool reset = false)
        {
            if (reset || cameraController.follow)
            {
                cameraButtonFree.gameObject.SetActive(true);
                cameraButtonFollow.gameObject.SetActive(false);
                cameraController.target = null;
                cameraController.follow = false;
                return;
            }
            else
            {
                cameraButtonFree.gameObject.SetActive(false);
                cameraButtonFollow.gameObject.SetActive(true);
                cameraController.target = currentObject;
                cameraController.FocusOnPoint(currentObject.position);
                cameraController.follow = true;
                return;
            }
        }

        public void PickupObject()
        {
            placer.Pickup(currentObject);
        }


        public void SetPlacerBehaviour(int behaviour)
        {
            placer.whenEmptyBehaviour = (Placer.EmptyBehaviours)behaviour;
        }



        public void PlayStop()
        {
			ChangeTimescale();
            paused = false;
            pauseText.text = "ll";
            if (Manager.Instance.globalPlaymode)
            {
                Manager.Instance.ExitGlobalPlaymode();
                if (currentObject != null)
                {
                    UpdateCurrentObject(currentObject.playmodeOriginal);
                    if (cameraController.follow)
                        cameraController.target = currentObject;
                }
                playStopText.text = "►";
                pauseButton.interactable = false;
            }
            else
            {
                Manager.Instance.EnterGlobalPlaymode();
                if (currentObject != null)
                {
                    UpdateCurrentObject(currentObject.playmodeClone);
                    if (cameraController.follow)
                        cameraController.target = currentObject;
                }
                playStopText.text = "■";
                pauseButton.interactable = true;
            }
        }

        public void Pause()
        {
            if (paused)
            {
                pauseText.text = "ll";
                ChangeTimescale();
                paused = false;
            }
            else
            {
                pauseText.text = "l►";
                Time.timeScale = 0;
                paused = true;
            }
        }


        public void SaveVehicle()
        {
            if (vehicleNameInput.text != "")
            {
                Debug.Log(vehiclesFolder + vehicleNameInput.text + ".xml");
                Debug.Log(currentObject.treeRoot);
                SerializableAssembly.SaveToXML(vehiclesFolder + "/" + vehicleNameInput.text + ".xml", currentObject.treeRoot);
            }
        }

        public void LoadVehicleInit()
        {
			DirectoryInfo dirInfo = new DirectoryInfo(vehiclesFolder);
            //FileInfo[] files = dirInfo.GetFiles("*.xml");
			FileInfo[] files = dirInfo.GetFiles("*.xml");

            loadVehiclesPanel.gameObject.SetActive(true);

            float top = 0;
            for (int i = 0; i < files.Length; i++)
            {
                GameObject obj = Instantiate(defaultVehicleButton.gameObject);
                obj.SetActive(true);
                currentLoadVehiclePanelContent.Add(obj);
                RectTransform rectTr = obj.GetComponent<RectTransform>();
                rectTr.SetParent(defaultVehicleButton.transform.parent, false);
                rectTr.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, top, rectTr.rect.height);
                obj.transform.GetChild(0).GetComponent<Text>().text = files[i].Name.Substring(0, files[i].Name.Length - 4);
                string path = files[i].FullName;
                obj.GetComponent<Button>().onClick.AddListener(delegate { LoadVehicle(path); });
                top += rectTr.rect.height;
            }
        }

		public void LoadVehiclePanelClose()
		{
			for (int i = 0; i < currentLoadVehiclePanelContent.Count; i++)
				Destroy(currentLoadVehiclePanelContent[i]);
			currentLoadVehiclePanelContent.Clear();
			
			loadVehiclesPanel.gameObject.SetActive(false);
		}

        public void LoadVehicle(string path)
        {
			LoadVehiclePanelClose();

            SerializableAssembly.SpawnFromXML(path, spawnPosition, TerminusObject.Modes.accepting_attachments, !Manager.Instance.globalPlaymode, true);
        }

        public void CenterOnStartingBlock()
        {
            if (startBlock.inPlaymode && startBlock.playmodeClone != null)
            {
                UpdateCurrentObject(startBlock.playmodeClone);                
            }
            else
            {
                UpdateCurrentObject(startBlock);                
            }
            cameraController.follow = false;
            ChangeCameraBehaviour();
        }


    }
}