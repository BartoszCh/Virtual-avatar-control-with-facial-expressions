using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TMP_InputField ipInputField;
    public TMP_InputField portInputField;
    public TMP_Dropdown scriptDropdown;
    public Button updateButton;

    private ZeroMQFACSvatar[] scriptInstances;

    private void Start()
    {
        scriptInstances = FindObjectsOfType<ZeroMQFACSvatar>();

        scriptDropdown.ClearOptions();
        int index = 1;
        foreach (var scriptInstance in scriptInstances)
        {
            string displayName = $"{index} {scriptInstance.DisplayName} ";
            scriptDropdown.options.Add(new TMP_Dropdown.OptionData(displayName));
            index++;
        }
        
        scriptDropdown.value = -1;

        updateButton.onClick.AddListener(UpdateScriptValues);
    }

    private void UpdateScriptValues()
    {
        int selectedIndex = scriptDropdown.value;
        if (selectedIndex >= 0 && selectedIndex < scriptInstances.Length)
        {
            ZeroMQFACSvatar selectedScript = scriptInstances[selectedIndex];

            // Update the IP and port values
            selectedScript.sub_to_ip = ipInputField.text;
            selectedScript.sub_to_port = portInputField.text;
        }
        else
        {
            Debug.LogError("Error");
        }
    }
}