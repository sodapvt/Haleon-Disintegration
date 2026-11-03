using System;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting;

// Native browser fields receive the actual tap, so mobile keyboard activation
// does not depend on a delayed Unity Update or a synthetic JavaScript focus.
public sealed class WebGLFormInput : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void HALEON_FormInputUpdate(int id, string receiver,
        string label, string value, int email, int limit,
        float x, float y, float width, float height, float fontSize, int alignment, string textStyle, int visible);
    [DllImport("__Internal")]
    private static extern void HALEON_FormInputRemoveAll();

    private TMP_InputField[] fields;
    private TMP_Dropdown region;
    private readonly Vector3[] corners = new Vector3[4];
    private readonly string[] labels = { "ESE Name", "HQ", "Email ID" };
    private bool previousCapture;
    private bool[] textEnabled;
    private bool[] placeholderEnabled;
    private string[] textStyles;

    [Serializable]
    private class BrowserTextStyle
    {
        public string family;
        public string color;
        public int weight;
        public bool italic;
        public string placeholder;
        public string placeholderFamily;
        public string placeholderColor;
        public int placeholderWeight;
        public bool placeholderItalic;
        public float placeholderSizeRatio;
    }

    private static int BrowserFontWeight(TMP_Text text)
    {
        return (text.fontStyle & FontStyles.Bold) != 0 ? 700 : (int)text.fontWeight;
    }

    public static void Attach(HCPUIController form)
    {
        var root = new GameObject("HALEON Browser Form Input");
        root.transform.SetParent(form.transform, false);
        var bridge = root.AddComponent<WebGLFormInput>();
        bridge.fields = new[] { form.EseNameInput, form.HqInput, form.EmailInput };
        bridge.region = form.RegionDropdown;
        bridge.textEnabled = new bool[bridge.fields.Length];
        bridge.placeholderEnabled = new bool[bridge.fields.Length];
        bridge.textStyles = new string[bridge.fields.Length];
        for (int i = 0; i < bridge.fields.Length; i++)
        {
            TMP_InputField field = bridge.fields[i];
            if (field == null) continue;
            bridge.textEnabled[i] = field.textComponent != null && field.textComponent.enabled;
            bridge.placeholderEnabled[i] = field.placeholder != null && field.placeholder.enabled;
            TMP_Text text = field.textComponent;
            TMP_Text placeholder = field.placeholder as TMP_Text;
            if (text == null) continue;
            TMP_Text hintStyle = placeholder != null ? placeholder : text;
            bridge.textStyles[i] = JsonUtility.ToJson(new BrowserTextStyle
            {
                family = text.font.faceInfo.familyName,
                color = "#" + ColorUtility.ToHtmlStringRGBA(text.color),
                weight = BrowserFontWeight(text),
                italic = (text.fontStyle & FontStyles.Italic) != 0,
                placeholder = placeholder != null ? placeholder.text : bridge.labels[i],
                placeholderFamily = hintStyle.font.faceInfo.familyName,
                placeholderColor = "#" + ColorUtility.ToHtmlStringRGBA(hintStyle.color),
                placeholderWeight = BrowserFontWeight(hintStyle),
                placeholderItalic = (hintStyle.fontStyle & FontStyles.Italic) != 0,
                placeholderSizeRatio = hintStyle.fontSize / Mathf.Max(1f, text.fontSize)
            });
        }
    }

    private void OnEnable()
    {
        previousCapture = WebGLInput.captureAllKeyboardInput;
        WebGLInput.captureAllKeyboardInput = false;
    }

    private void LateUpdate()
    {
        if (fields == null || Screen.width == 0 || Screen.height == 0) return;
        bool dropdownOpen = region != null && region.transform.Find("Dropdown List") != null;
        for (int i = 0; i < fields.Length; i++)
        {
            TMP_InputField field = fields[i];
            if (field == null) continue;
            Canvas canvas = field.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera : null;
            // Use the text area, rather than the artwork's outer rectangle.
            RectTransform textRect = field.textViewport != null
                ? field.textViewport : (RectTransform)field.transform;
            textRect.GetWorldCorners(corners);
            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            float fontSize = field.textComponent != null ? field.textComponent.fontSize : 30f;
            float screenFontSize = fontSize * (topRight.y - bottomLeft.y) / Mathf.Max(1f, textRect.rect.height);
            int alignment = field.textComponent != null && field.textComponent.horizontalAlignment == HorizontalAlignmentOptions.Center
                ? 1 : field.textComponent != null && field.textComponent.horizontalAlignment == HorizontalAlignmentOptions.Right ? 2 : 0;
            // One renderer must own both text and caret, otherwise their font
            // metrics and horizontal scrolling disagree when users tap or edit.
            if (field.textComponent != null) field.textComponent.enabled = dropdownOpen && textEnabled[i];
            if (field.placeholder != null) field.placeholder.enabled = dropdownOpen && placeholderEnabled[i];
            HALEON_FormInputUpdate(i, gameObject.name, labels[i], field.text,
                i == 2 ? 1 : 0, field.characterLimit,
                bottomLeft.x / Screen.width, 1f - topRight.y / Screen.height,
                (topRight.x - bottomLeft.x) / Screen.width,
                (topRight.y - bottomLeft.y) / Screen.height,
                screenFontSize / Screen.height, alignment, textStyles[i],
                !dropdownOpen && field.isActiveAndEnabled && field.IsInteractable() && !field.readOnly ? 1 : 0);
        }
    }

    [Serializable]
    private class InputValue
    {
        public int id = -1;
        public string value = string.Empty;
    }

    [Preserve]
    public void OnBrowserInput(string json)
    {
        InputValue input = JsonUtility.FromJson<InputValue>(json);
        if (input != null && fields != null && input.id >= 0 && input.id < fields.Length && fields[input.id] != null)
            fields[input.id].text = input.value ?? string.Empty;
    }

    private void OnDisable()
    {
        HALEON_FormInputRemoveAll();
        if (fields != null)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                TMP_InputField field = fields[i];
                if (field == null) continue;
                if (field.textComponent != null) field.textComponent.enabled = textEnabled[i];
                if (field.placeholder != null) field.placeholder.enabled = placeholderEnabled[i];
            }
        }
        WebGLInput.captureAllKeyboardInput = previousCapture;
    }
#else
    public static void Attach(HCPUIController form) { }
#endif
}
