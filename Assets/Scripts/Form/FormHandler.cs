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
    [SerializeField] private bool skipFormEntry = true;
    public bool SkipFormEntry => skipFormEntry;
    //
    void Awake()
    {
        instance = this;

       
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
