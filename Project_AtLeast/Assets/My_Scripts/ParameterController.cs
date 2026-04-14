using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ParameterController : MonoBehaviour
{
    [System.Serializable]
    public class ControllerImpact
    {
        [Header("Controller Reference")]
        public string controllerName = "CompanyPrefab";
        public Slider controllerSlider; // Alternative: use Slider for easier handling
        public Image controllerFillImage; // Your LinearProgress_Sell image
        
        [Header("Impact on Parameters (%)")]
        [Range(-100f, 100f)]
        public float impactOnParam1 = 0f; // Impact on Graph_Progress_Parameters 1
        [Range(-100f, 100f)]
        public float impactOnParam2 = 0f; // Impact on Graph_Progress_Parameters 2
        [Range(-100f, 100f)]
        public float impactOnParam3 = 0f; // Impact on Graph_Progress_Parameters 3
        [Range(-100f, 100f)]
        public float impactOnParam4 = 0f; // Impact on Graph_Progress_Parameters 4
        
        [Header("Current Value (Auto)")]
        [Range(0f, 1f)]
        public float currentFillAmount = 0.5f; // Current value from controller
    }
    
    [Header("Controller Bars (3)")]
    public List<ControllerImpact> controllers = new List<ControllerImpact>();
    
    [Header("Target Parameter Bars (4)")]
    public List<Image> parameterBars = new List<Image>(); // Your Graph_Progress_Parameters images
    
    [Header("Settings")]
    public bool debugMode = true;
    public float updateInterval = 0.1f; // Update frequency
    
    private float updateTimer = 0f;
    
    void Start()
    {
        // Auto-find components if not assigned
        FindComponents();
        
        // Initialize all parameters
        UpdateAllParameters();
    }
    
    void Update()
    {
        // Update with interval to improve performance
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            
            // Check if any controller value changed
            bool valuesChanged = false;
            foreach (var controller in controllers)
            {
                float currentValue = GetControllerValue(controller);
                if (Mathf.Abs(currentValue - controller.currentFillAmount) > 0.001f)
                {
                    controller.currentFillAmount = currentValue;
                    valuesChanged = true;
                }
            }
            
            // Update parameters if any controller changed
            if (valuesChanged)
            {
                UpdateAllParameters();
            }
        }
    }
    
    // Changed from private to public so editor script can call it
    public void FindComponents()
    {
        // Find controller bars if not assigned
        for (int i = 0; i < controllers.Count; i++)
        {
            var controller = controllers[i];
            
            // Try to find by name if not assigned
            if (controller.controllerFillImage == null)
            {
                GameObject companyObj = GameObject.Find(controller.controllerName);
                if (companyObj != null)
                {
                    GameObject barObj = FindChildByName(companyObj, "Buy_Linear_progressBar");
                    if (barObj != null)
                    {
                        controller.controllerFillImage = FindChildImageByName(barObj, "LinearProgress_Sell");
                    }
                }
                
                // Also try to find Slider component for easier value handling
                if (controller.controllerFillImage != null && controller.controllerSlider == null)
                {
                    controller.controllerSlider = controller.controllerFillImage.GetComponentInParent<Slider>();
                }
            }
            
            controllers[i] = controller;
        }
        
        // Find parameter bars if not assigned (look for Graph_Progress_Parameters in scene)
        if (parameterBars.Count == 0 || parameterBars[0] == null)
        {
            parameterBars.Clear();
            for (int i = 1; i <= 4; i++)
            {
                GameObject paramObj = GameObject.Find("ParameterPrefab_" + i);
                if (paramObj == null) paramObj = GameObject.Find("ParameterPrefab");
                
                if (paramObj != null)
                {
                    Image img = FindChildImageByName(paramObj, "Graph_Progress_Parameters");
                    if (img != null)
                        parameterBars.Add(img);
                    else
                        parameterBars.Add(null);
                }
                else
                {
                    parameterBars.Add(null);
                }
            }
        }
    }
    
    float GetControllerValue(ControllerImpact controller)
    {
        // Try to get from Slider first
        if (controller.controllerSlider != null)
            return controller.controllerSlider.value;
        
        // Otherwise get from Image fill amount
        if (controller.controllerFillImage != null)
            return controller.controllerFillImage.fillAmount;
        
        return 0.5f; // Default
    }
    
    void UpdateAllParameters()
    {
        // Calculate total impact for each parameter
        float[] totalImpacts = new float[4] { 0f, 0f, 0f, 0f };
        float[] parameterValues = new float[4] { 0f, 0f, 0f, 0f };
        
        // Sum up all impacts from controllers
        for (int i = 0; i < controllers.Count; i++)
        {
            var controller = controllers[i];
            float controllerValue = controller.currentFillAmount;
            
            // Convert fill amount (0-1) to effective value (-1 to 1 range impact)
            float effectiveValue = (controllerValue * 2f) - 1f;
            
            // Add impacts to each parameter
            totalImpacts[0] += effectiveValue * (controller.impactOnParam1 / 100f);
            totalImpacts[1] += effectiveValue * (controller.impactOnParam2 / 100f);
            totalImpacts[2] += effectiveValue * (controller.impactOnParam3 / 100f);
            totalImpacts[3] += effectiveValue * (controller.impactOnParam4 / 100f);
        }
        
        // Clamp and convert to fill amount (0-1)
        for (int i = 0; i < 4; i++)
        {
            // Clamp impact between -1 and 1
            float clampedImpact = Mathf.Clamp(totalImpacts[i], -1f, 1f);
            // Convert from -1..1 range to 0..1 fill amount
            parameterValues[i] = (clampedImpact + 1f) / 2f;
        }
        
        // Apply to parameter bars
        for (int i = 0; i < parameterBars.Count && i < 4; i++)
        {
            if (parameterBars[i] != null)
            {
                parameterBars[i].fillAmount = parameterValues[i];
                
                // Optional: Change color based on value
                // parameterBars[i].color = Color.Lerp(Color.red, Color.green, parameterValues[i]);
            }
        }
        
        // Debug output
        if (debugMode)
        {
            string debug = "Controller Values: ";
            foreach (var c in controllers)
                debug += $"{c.currentFillAmount:F2} ";
            debug += "\nParameter Values: ";
            foreach (float v in parameterValues)
                debug += $"{v:F2} ";
            Debug.Log(debug);
        }
    }
    
    // Helper methods
    GameObject FindChildByName(GameObject parent, string name)
    {
        foreach (Transform child in parent.transform)
        {
            if (child.name == name)
                return child.gameObject;
            GameObject found = FindChildByName(child.gameObject, name);
            if (found != null) return found;
        }
        return null;
    }
    
    Image FindChildImageByName(GameObject parent, string name)
    {
        foreach (Transform child in parent.transform)
        {
            if (child.name == name)
            {
                Image img = child.GetComponent<Image>();
                if (img != null) return img;
            }
            Image found = FindChildImageByName(child.gameObject, name);
            if (found != null) return found;
        }
        return null;
    }
    
    // Public methods for manual control
    public void SetControllerValue(int controllerIndex, float value)
    {
        if (controllerIndex >= 0 && controllerIndex < controllers.Count)
        {
            var controller = controllers[controllerIndex];
            controller.currentFillAmount = Mathf.Clamp01(value);
            
            if (controller.controllerSlider != null)
                controller.controllerSlider.value = controller.currentFillAmount;
            else if (controller.controllerFillImage != null)
                controller.controllerFillImage.fillAmount = controller.currentFillAmount;
                
            UpdateAllParameters();
        }
    }
    
    public float GetParameterValue(int parameterIndex)
    {
        if (parameterIndex >= 0 && parameterIndex < parameterBars.Count && parameterBars[parameterIndex] != null)
            return parameterBars[parameterIndex].fillAmount;
        return 0f;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ParameterController))]
public class ParameterControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ParameterController script = (ParameterController)target;
        
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Auto-Find All Components"))
        {
            script.FindComponents(); // Now this works because FindComponents is public
            EditorUtility.SetDirty(script);
        }
        
        EditorGUILayout.Space(5);
        
        // Show preview of current values
        EditorGUILayout.LabelField("Current Parameter Values", EditorStyles.boldLabel);
        for (int i = 0; i < script.parameterBars.Count; i++)
        {
            if (script.parameterBars[i] != null)
            {
                EditorGUILayout.Slider($"Parameter {i + 1}", script.parameterBars[i].fillAmount, 0f, 1f);
            }
        }
    }
}
#endif