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
        float x, float y, float width, float height, int visible);
    [DllImport("__Internal")]
    private static extern void HALEON_FormInputRemoveAll();

    private TMP_InputField[] fields;
    private TMP_Dropdown region;
    private readonly Vector3[] corners = new Vector3[4];
    private readonly string[] labels = { "ESE Name", "HQ", "Email ID" };
    private bool previousCapture;

    public static void Attach(HCPUIController form)
    {
        var root = new GameObject("HALEON Browser Form Input");
        root.transform.SetParent(form.transform, false);
        var bridge = root.AddComponent<WebGLFormInput>();
        bridge.fields = new[] { form.EseNameInput, form.HqInput, form.EmailInput };
        bridge.region = form.RegionDropdown;
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
            ((RectTransform)field.transform).GetWorldCorners(corners);
            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            HALEON_FormInputUpdate(i, gameObject.name, labels[i], field.text,
                i == 2 ? 1 : 0, field.characterLimit,
                bottomLeft.x / Screen.width, 1f - topRight.y / Screen.height,
                (topRight.x - bottomLeft.x) / Screen.width,
                (topRight.y - bottomLeft.y) / Screen.height,
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
        WebGLInput.captureAllKeyboardInput = previousCapture;
    }
#else
    public static void Attach(HCPUIController form) { }
#endif
}
