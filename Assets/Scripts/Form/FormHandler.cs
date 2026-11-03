using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FormHandler : MonoBehaviour
{
    public static FormHandler instance;
    public HCPUIController hcpUIController;
    public TextMeshProUGUI mrCode;
    public TextMeshProUGUI mrName;
    public TextMeshProUGUI date;
    public TextMeshProUGUI time;

    public GameObject loadingIndicator;
    public AudioSource audioSource;
    public Button submitButton;
    public AndroidHaptics androidHaptics;
    [SerializeField] private GameObject fullscreenPrompt;
    [SerializeField] private GameObject formCanvas;
    [SerializeField] private bool skipFormEntry = false;
    public bool SkipFormEntry => skipFormEntry;
    //
    void Awake()
    {
        instance = this;
        if (fullscreenPrompt != null)
        {
            fullscreenPrompt.SetActive(true);
            if (formCanvas != null)
                formCanvas.SetActive(false);
        }
    }
    void Start()
    {
         if (FormHandler.instance.submitButton != null)
        {
            //remove previous listeners to avoid multiple subscriptions
            FormHandler.instance.submitButton.onClick.RemoveAllListeners();
            if (skipFormEntry)
            {
                FormHandler.instance.submitButton.onClick.AddListener(SkipFormAndContinue);
            }
            else
            {
                FormHandler.instance.submitButton.onClick.AddListener(GoogleSheetUploader.instance.OnSubmit);
            }
        }
    }

    public void EnterFullscreenAndShowForm()
    {
        // Keep this synchronous with the OK click for browser user activation.
        WebGLPresentation.EnterFullscreenFromUserGesture();
#if !UNITY_WEBGL && !UNITY_EDITOR
        Screen.fullScreen = true;
#endif
        if (formCanvas != null)
            formCanvas.SetActive(true);
        if (fullscreenPrompt != null)
            fullscreenPrompt.SetActive(false);
    }

    private void SkipFormAndContinue()
    {
        if (audioSource != null)
        {
            audioSource.Play();
        }

        if (androidHaptics != null)
        {
            androidHaptics.Vibrate();
        }

        SceneManager.LoadScene("LauncherScene");
    }
}
