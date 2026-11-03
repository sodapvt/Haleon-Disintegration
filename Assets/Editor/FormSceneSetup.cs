#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FormSceneSetup
{
    private static readonly string[] Regions =
    {
        "North", "Central", "East", "South 1", "South 2", "West 1", "West 2"
    };

    [MenuItem("Tools/HALEON/Convert Form Controls To Scene Objects")]
    public static void ConvertFormControlsToSceneObjects()
    {
        const string formScenePath = "Assets/Scenes/Form.unity";
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != formScenePath)
            EditorSceneManager.OpenScene(formScenePath, OpenSceneMode.Single);

        HCPUIController controller = Object.FindObjectOfType<HCPUIController>(true);
        if (controller == null || controller.EseNameInput == null)
        {
            Debug.LogError("Open Form.unity before running this command. The ESE Name input is required as the style template.");
            return;
        }

        TMP_InputField hq = controller.HqInput ?? ReplaceSlotWithInput("HQ", "HQ", controller.EseNameInput, TMP_InputField.ContentType.Standard);
        TMP_InputField email = controller.EmailInput ?? ReplaceSlotWithInput("Email Id", "Email ID", controller.EseNameInput, TMP_InputField.ContentType.EmailAddress);
        TMP_Dropdown region = controller.RegionDropdown ?? ReplaceSlotWithDropdown("Region", controller.EseNameInput);

        if (hq == null || email == null || region == null)
            return;

        controller.ConfigureSceneControls(hq, email, region);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        EditorSceneManager.SaveScene(controller.gameObject.scene);
        Debug.Log("Form controls are now serialized in Assets/Scenes/Form.unity.");
    }

    private static TMP_InputField ReplaceSlotWithInput(
        string slotName,
        string placeholder,
        TMP_InputField template,
        TMP_InputField.ContentType contentType)
    {
        GameObject slot = GameObject.Find(slotName);
        if (slot == null)
        {
            Debug.LogError($"Form slot '{slotName}' was not found.");
            return null;
        }

        GameObject replacement = Object.Instantiate(template.gameObject, slot.transform.parent);
        Undo.RegisterCreatedObjectUndo(replacement, $"Create {slotName} input");
        replacement.name = slotName;
        CopyRect(slot.GetComponent<RectTransform>(), replacement.GetComponent<RectTransform>());

        TMP_InputField input = replacement.GetComponent<TMP_InputField>();
        input.text = string.Empty;
        input.contentType = contentType;
        if (input.placeholder is TMP_Text placeholderText)
            placeholderText.text = placeholder;

        Undo.DestroyObjectImmediate(slot);
        return input;
    }

    private static TMP_Dropdown ReplaceSlotWithDropdown(string slotName, TMP_InputField styleTemplate)
    {
        GameObject slot = GameObject.Find(slotName);
        if (slot == null)
        {
            Debug.LogError($"Form slot '{slotName}' was not found.");
            return null;
        }

        GameObject dropdownObject = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
        Undo.RegisterCreatedObjectUndo(dropdownObject, "Create Region dropdown");
        dropdownObject.name = slotName;
        dropdownObject.transform.SetParent(slot.transform.parent, false);
        CopyRect(slot.GetComponent<RectTransform>(), dropdownObject.GetComponent<RectTransform>());

        Image oldBackground = slot.GetComponent<Image>();
        Image dropdownBackground = dropdownObject.GetComponent<Image>();
        if (oldBackground != null && dropdownBackground != null)
        {
            dropdownBackground.sprite = oldBackground.sprite;
            dropdownBackground.type = oldBackground.type;
            dropdownBackground.color = oldBackground.color;
        }

        TMP_Dropdown dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
        dropdown.options.Clear();
        foreach (string item in Regions)
            dropdown.options.Add(new TMP_Dropdown.OptionData(item));
        dropdown.value = 0;

        TMP_Text sourceText = styleTemplate.textComponent;
        if (sourceText != null)
        {
            if (dropdown.captionText != null)
            {
                dropdown.captionText.font = sourceText.font;
                dropdown.captionText.fontSize = sourceText.fontSize;
                dropdown.captionText.alignment = TextAlignmentOptions.Left;
            }
            if (dropdown.itemText != null)
                dropdown.itemText.font = sourceText.font;
        }
        dropdown.RefreshShownValue();

        Undo.DestroyObjectImmediate(slot);
        return dropdown;
    }

    private static void CopyRect(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.localScale = source.localScale;
        target.localRotation = source.localRotation;
    }
}
#endif
