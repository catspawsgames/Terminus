using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using Terminus;

namespace Terminus.Demo1
{
    public class SelectedPartPanel : MonoBehaviour
    {
        //[SerializeField]
        //protected float maxPanelHeight = 300;
        [SerializeField]
        protected Text currentObjectName;        
        [SerializeField]
        protected RectTransform keySelectionPanel;
        [SerializeField]
        protected RectTransform keyBindingUIElement;
        [SerializeField]
        protected RectTransform sliderUIElement;
        [SerializeField]
        protected RectTransform toggleUIElement;
        [SerializeField]
        protected GameObject placementCyclePort;
        [SerializeField]
        protected GameObject placementRotatePort;
        [SerializeField]
        protected GameObject standardPortConnection;
        [SerializeField]
        protected GameObject standardSurfaceConnection;
        [SerializeField]
        protected Button pickupButton;
        [SerializeField]
        protected Button deleteButton;
        [SerializeField]
        protected KeyCode[] forbiddenKeys;

        protected List<RectTransform> keyBindings;

        protected AdjustableParametersHandler paramHandler;

        protected List<ChangableParameterComponents> togglesCache;
        protected List<ChangableParameterComponents> slidersCache;
        protected List<ParametersTieIn> parametersTieIn;


        protected bool inKeySelectionMode;
        protected bool keySelectionAltKey;
        protected int keySelectionIndex;

        [System.Serializable]
        public class ParametersTieIn
        {
            public Component controlledComponent;
            public Component controlledByComponent;            
            //public string parameterName;
            public PropertyInfo property;
            public FieldInfo field;

            public void SetParameterValue(object newValue)
            {
                if (field != null)
                {
                    field.SetValue(controlledComponent, newValue);
                }
                else
                {
                    property.SetValue(controlledComponent, newValue, null);
                }
            }

            public object GetParameterValue()
            {
                if (field != null)
                {
                    return field.GetValue(controlledComponent);
                }
                else
                {
                    return property.GetValue(controlledComponent, null);
                }
            }
        }

        [System.Serializable]
        public class ChangableParameterComponents
        {
            public RectTransform rootRectTransform;
            public Component controllableComponent;
            public Text text;
            public Text text2;
            public Text text3;
        }

        protected TerminusObject _selectedPart;
        public TerminusObject SelectedPart
        {
            get { return _selectedPart; }
            set
            {
                if (value != _selectedPart)
                {
                    _selectedPart = value;
                    if (_selectedPart == null)
                    {
                        gameObject.SetActive(false);
                        inKeySelectionMode = false;
                        keySelectionPanel.gameObject.SetActive(false);

                    }
                    else
                    {
                        //initializing UI panel
                        currentObjectName.text = _selectedPart.getPartName;

                        ControllablePart controllablePart = _selectedPart.GetComponent<ControllablePart>();
                        if (controllablePart != null)
                        {
                            //Drawing button remapping
                            InitializeKeyBindingElementList(controllablePart.controls.Count);
                            for (int i = 0; i < controllablePart.controls.Count; i++)
                            {
                                keyBindings[i].GetComponent<Text>().text = controllablePart.controls[i].name;
                                Component[] buttons = keyBindings[i].GetComponentsInChildren<Button>();
                                buttons[0].GetComponentInChildren<Text>().text = controllablePart.controls[i].key.ToString();
                                buttons[1].GetComponentInChildren<Text>().text = controllablePart.controls[i].altKey.ToString();
                            }

                            //Drawing adjustable parameters if present
                            if (_selectedPart.getPartName.Length > 0)
                            {
                                List<Settings.AssemblySerializableParameters> components = ProjectManager.settings.GetParametersByPartName(_selectedPart.getPartName);
                                if (components != null)
                                {                                    
                                    int i = 0;
                                    int sliderInd = 0;
                                    int toggleInd = 0;
                                    foreach (Settings.AssemblySerializableParameters componentParams in components)
                                    {
                                        Component component = _selectedPart.gameObject.GetComponent(componentParams.componentType);
                                        if (componentParams.parameters != null && componentParams.parameters.Count > 0)
                                        {                                            
                                            foreach(Settings.AssemblyParameterInfo param in componentParams.parameters)
                                            {                                                
                                                if (parametersTieIn.Count <= 0)
                                                    parametersTieIn.Add(new ParametersTieIn());

                                                parametersTieIn[i].controlledComponent = component;
                                                parametersTieIn[i].property = param.property;
                                                parametersTieIn[i].field = param.field;

                                                if (param.controlType == Settings.UIControlTypes.Float || param.controlType == Settings.UIControlTypes.Int)
                                                {
                                                    ChangableParameterComponents control = GetSliderElement(sliderInd);
                                                    parametersTieIn[i].controlledByComponent = control.controllableComponent;
                                                    control.text.text = paramHandler.parameters[i].displayedName;
                                                    (control.controllableComponent as Slider).minValue = paramHandler.parameters[i].minMax.x;
                                                    (control.controllableComponent as Slider).maxValue = paramHandler.parameters[i].minMax.y;
                                                    control.text2.text = paramHandler.parameters[i].minMax.x.ToString();
                                                    control.text3.text = paramHandler.parameters[i].minMax.y.ToString();
                                                    (control.controllableComponent as Slider).value = (float)parametersTieIn[i].GetParameterValue();
                                                    sliderInd++;
                                                }
                                                else if (param.controlType == Settings.UIControlTypes.Toggle)
                                                {
                                                    ChangableParameterComponents control = GetToggleElement(toggleInd);
                                                    parametersTieIn[i].controlledByComponent = control.controllableComponent;
                                                    control.text.text = paramHandler.parameters[i].displayedName;
                                                    (control.controllableComponent as Toggle).isOn = (bool)parametersTieIn[i].GetParameterValue();
                                                    toggleInd++;
                                                }                                                
                                                                                                
                                                i++;
                                            }
                                        }
                                    }
                                }
                            }



                            /*
                            paramHandler = _selectedPart.gameObject.GetComponent<AdjustableParametersHandler>();

                            if (paramHandler != null)
                            {
                                for (int i = 0; i < paramHandler.parameters.Length; i++)
                                {                                    
                                    int ind = i;
                                    switch (paramHandler.parameters[i].controlType)
                                    {
                                        case AdjustableParametersHandler.UIControlTypes.slider:
                                            obj = Instantiate(sliderUIElement);
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
                                            obj = Instantiate(toggleUIElement);
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
                            */
                        }
                    }                    
                }
            }
        }
        



        private void KeyBindingChangeInitiated(int index, bool altKey)
        {
            //Debug.Log(index.ToString() + " " + altKey.ToString());
            inKeySelectionMode = true;
            keySelectionAltKey = altKey;
            keySelectionPanel.gameObject.SetActive(true);
            keySelectionIndex = index;
        }

        private void AdjustableParameterChanged(Component control)
        {

        }


        private void Awake()
        {
            keyBindings = new List<RectTransform>();
            keyBindings.Add(keyBindingUIElement);

            slidersCache = new List<ChangableParameterComponents>();
            Slider slider = sliderUIElement.gameObject.GetComponentInChildren<Slider>();
            Text textS = sliderUIElement.gameObject.GetComponent<Text>();
            ChangableParameterComponents controlSlider = new ChangableParameterComponents();
            controlSlider.rootRectTransform = sliderUIElement.GetComponent<RectTransform>();
            controlSlider.controllableComponent = slider;
            controlSlider.text = textS;
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(delegate { AdjustableParameterChanged(slider); });
            slidersCache.Add(controlSlider);

            togglesCache = new List<ChangableParameterComponents>();
            Toggle toggle = toggleUIElement.gameObject.GetComponentInChildren<Toggle>();
            Text textT = toggleUIElement.gameObject.GetComponent<Text>();
            ChangableParameterComponents controlToggle = new ChangableParameterComponents();
            controlToggle.rootRectTransform = toggleUIElement.GetComponent<RectTransform>();
            controlToggle.controllableComponent = toggle;
            controlToggle.text = textT;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(delegate { AdjustableParameterChanged(toggle); });
            togglesCache.Add(controlToggle);
        }        

        private void InitializeKeyBindingElementList(int count)
        {
            if (keyBindings.Count < count)
            {
                int addCount = count - keyBindings.Count;
                for (int i = 0; i < addCount; i++)
                {
                    GameObject obj = Instantiate(keyBindingUIElement.gameObject, keyBindingUIElement.parent);
                    Component[] buttons = obj.GetComponentsInChildren<Button>();
                    int index = i;
                    ((Button)buttons[0]).onClick.AddListener(delegate { KeyBindingChangeInitiated(index, false); });
                    ((Button)buttons[1]).onClick.AddListener(delegate { KeyBindingChangeInitiated(index, true); });
                    keyBindings.Add(obj.GetComponent<RectTransform>());
                }
            }
            for (int i = 0; i < keyBindings.Count; i++)
                keyBindings[i].gameObject.SetActive(i < count);
        }

        private ChangableParameterComponents GetSliderElement(int index)
        {
            if (slidersCache.Count == index)
            {
                GameObject obj = Instantiate(sliderUIElement.gameObject, keyBindingUIElement.parent);
                obj.transform.SetParent(sliderUIElement.parent);
                Slider slider = obj.GetComponentInChildren<Slider>();
                Text text = obj.GetComponent<Text>();
                Text text2 = obj.transform.Find("MinText").GetComponent<Text>();
                Text text3 = obj.transform.Find("MaxText").GetComponent<Text>();

                ChangableParameterComponents control = new ChangableParameterComponents();
                control.rootRectTransform = obj.GetComponent<RectTransform>();
                control.controllableComponent = slider;
                control.text = text;
                control.text2 = text2;
                control.text3 = text3;
                slidersCache.Add(control);
                
                control.rootRectTransform.SetParent(sliderUIElement.parent, false);
                slider.onValueChanged.RemoveAllListeners();
                slider.onValueChanged.AddListener(delegate { AdjustableParameterChanged(slider); });
                control.rootRectTransform.gameObject.SetActive(true);                
                return control;
            }
            else
            {
                slidersCache[index].rootRectTransform.gameObject.SetActive(true);
                return slidersCache[index];
            }            
        }

        private ChangableParameterComponents GetToggleElement(int index)
        {
            if (togglesCache.Count == index)
            {
                GameObject obj = Instantiate(toggleUIElement.gameObject, keyBindingUIElement.parent);
                obj.transform.SetParent(toggleUIElement.parent);
                Toggle toggle = obj.GetComponentInChildren<Toggle>();
                Text text = obj.GetComponent<Text>();

                ChangableParameterComponents control = new ChangableParameterComponents();
                control.rootRectTransform = obj.GetComponent<RectTransform>();
                control.controllableComponent = toggle;
                control.text = text;
                togglesCache.Add(control);

                control.rootRectTransform.SetParent(toggleUIElement.parent, false);
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener(delegate { AdjustableParameterChanged(toggle); });
                control.rootRectTransform.gameObject.SetActive(true);
                return control;
            }
            else
            {
                togglesCache[index].rootRectTransform.gameObject.SetActive(true);
                return togglesCache[index];
            }
        }

        /*
        private void InitializeElementList(List<RectTransform> list, RectTransform defaultElement, int count)
        {
            if (list.Count < count)
            {
                int addCount = count - list.Count;
                for (int i = 0; i < addCount; i++)
                {
                    GameObject obj = Instantiate(defaultElement.gameObject, defaultElement.parent);
                    Component[] buttons = obj.GetComponentsInChildren<Button>();
                    int index = i;
                    ((Button)buttons[0]).onClick.RemoveAllListeners();
                    ((Button)buttons[1]).onClick.RemoveAllListeners();
                    ((Button)buttons[0]).onClick.AddListener(delegate { KeyBindingChangeInitiated(index, false); });
                    ((Button)buttons[1]).onClick.AddListener(delegate { KeyBindingChangeInitiated(index, true); });
                    list.Add(obj.GetComponent<RectTransform>());
                }                
            }
            for (int i = 0; i < list.Count; i++)
                list[i].gameObject.SetActive(i < count);
        }
        */
    }
}