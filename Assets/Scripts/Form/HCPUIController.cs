using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using System;
using System.Net.Mail;

public class HCPUIController : MonoBehaviour
{
    [FormerlySerializedAs("mrCodeInput")]
    [FormerlySerializedAs("employeeCodeInput")]
    public TMP_InputField eseNameInput;

    public TextMeshProUGUI date;
    [SerializeField] private TMP_InputField hqInput;
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_Dropdown regionDropdown;

    public TMP_InputField EseNameInput => eseNameInput;
    public TMP_InputField HqInput => hqInput;
    public TMP_InputField EmailInput => emailInput;
    public TMP_Dropdown RegionDropdown => regionDropdown;

    private static readonly string[] Regions =
    {
        "North", "Central", "East", "South 1", "South 2", "West 1", "West 2"
    };

    private void Awake()
    {
        // The scene already supplies the ESE field. The other three visual slots are
        // converted to controls here so the existing form artwork and layout are kept.
        ConfigureExistingInput(eseNameInput, "ESE Name", TMP_InputField.ContentType.Standard);
        if (hqInput == null)
            hqInput = CreateInputInSlot("HQ", "HQ", TMP_InputField.ContentType.Standard);
        if (emailInput == null)
            emailInput = CreateInputInSlot("Email Id", "Email ID", TMP_InputField.ContentType.EmailAddress);
        if (regionDropdown == null)
            regionDropdown = CreateRegionDropdown();
        RefreshDate();
        WebGLFormInput.Attach(this);
    }

    private void Update() => RefreshDate();

    public bool TryCreateRecord(out HCPRecord record, out string validationMessage)
    {
        record = null;
        validationMessage = string.Empty;

        string eseName = Read(eseNameInput);
        string hq = Read(hqInput);
        string email = Read(emailInput);
        string region = regionDropdown == null || regionDropdown.value < 0 || regionDropdown.value >= regionDropdown.options.Count
            ? string.Empty : regionDropdown.options[regionDropdown.value].text;

        if (string.IsNullOrWhiteSpace(eseName))
        {
            validationMessage = "Please enter your ESE name.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(hq))
        {
            validationMessage = "Please enter your HQ.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(email))
        {
            validationMessage = "Please enter your email address.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(region))
        {
            validationMessage = "Please select a region.";
            return false;
        }

        if (!IsValidEmail(email))
        {
            validationMessage = "Please enter a valid email address.";
            return false;
        }

        record = new HCPRecord(eseName, hq, email, region, DateTime.Now.ToString("yyyy-MM-dd"));
        return true;
    }

    private void RefreshDate()
    {
        if (date != null)
            date.text = DateTime.Now.ToString("yyyy-MM-dd");
    }

    private static bool IsValidEmail(string email)
    {
        foreach (char character in email)
            if (char.IsWhiteSpace(character)) return false;

        try
        {
            var address = new MailAddress(email);
            return string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase) &&
                address.Host.Contains(".") && !address.Host.StartsWith(".") &&
                !address.Host.EndsWith(".") && !address.Host.Contains("..") &&
                !address.User.StartsWith(".") && !address.User.EndsWith(".") &&
                !address.User.Contains("..");
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private TMP_InputField CreateInputInSlot(string slotName, string placeholder, TMP_InputField.ContentType contentType)
    {
        GameObject slot = GameObject.Find(slotName);
        if (slot == null)
        {
            Debug.LogError($"Form slot '{slotName}' was not found.", this);
            return null;
        }

        TMP_InputField input = slot.GetComponent<TMP_InputField>();
        if (input == null)
            input = slot.AddComponent<TMP_InputField>();

        TextMeshProUGUI label = slot.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null)
        {
            Debug.LogError($"Form slot '{slotName}' has no text label.", this);
            return input;
        }

        GameObject valueObject = new GameObject("Input Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        valueObject.transform.SetParent(slot.transform, false);
        TextMeshProUGUI value = valueObject.GetComponent<TextMeshProUGUI>();
        value.font = label.font;
        value.fontSize = label.fontSize;
        value.color = label.color;
        value.alignment = label.alignment;
        value.enableWordWrapping = false;
        Stretch(value.rectTransform);

        label.text = placeholder;
        label.fontStyle = FontStyles.Italic;
        Color placeholderColor = label.color;
        placeholderColor.a = 0.55f;
        label.color = placeholderColor;

        input.targetGraphic = slot.GetComponent<Image>();
        input.textComponent = value;
        input.placeholder = label;
        input.textViewport = value.rectTransform;
        input.contentType = contentType;
        input.lineType = TMP_InputField.LineType.SingleLine;
        return input;
    }

    private void ConfigureExistingInput(TMP_InputField input, string placeholder, TMP_InputField.ContentType contentType)
    {
        if (input == null)
            return;

        input.contentType = contentType;
        if (input.placeholder is TextMeshProUGUI placeholderText)
            placeholderText.text = placeholder;
    }

    private TMP_Dropdown CreateRegionDropdown()
    {
        GameObject slot = GameObject.Find("Region");
        if (slot == null)
        {
            Debug.LogError("Form slot 'Region' was not found.", this);
            return null;
        }

        TextMeshProUGUI oldLabel = slot.GetComponentInChildren<TextMeshProUGUI>(true);
        if (oldLabel != null)
            oldLabel.gameObject.SetActive(false);

        GameObject dropdownObject = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
        dropdownObject.name = "Region Dropdown";
        dropdownObject.transform.SetParent(slot.transform, false);
        Stretch(dropdownObject.GetComponent<RectTransform>());

        TMP_Dropdown dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
        dropdown.targetGraphic = dropdownObject.GetComponent<Image>();
        dropdown.options.Clear();
        foreach (string region in Regions)
            dropdown.options.Add(new TMP_Dropdown.OptionData(region));
        dropdown.value = 0;
        dropdown.RefreshShownValue();

        if (dropdown.captionText != null)
        {
            dropdown.captionText.fontSize = 30;
            dropdown.captionText.alignment = TextAlignmentOptions.Left;
        }
        return dropdown;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(18f, 8f);
        rect.offsetMax = new Vector2(-18f, -8f);
    }

    private static string Read(TMP_InputField input) => input == null ? string.Empty : input.text.Trim();

    public void ConfigureSceneControls(
        TMP_InputField hq,
        TMP_InputField email,
        TMP_Dropdown region)
    {
        hqInput = hq;
        emailInput = email;
        regionDropdown = region;
    }
}
