using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
using System;

public class GoogleSheetUploader : MonoBehaviour
{
     [SerializeField] private string googleSheetURL = "YOUR_SHEET_URL_HERE";
    [SerializeField] private string googleFormURL = "YOUR_WEB_APP_URL_HERE";
    private Queue<HCPRecord> retryQueue = new Queue<HCPRecord>();
    private bool isUploading = false;
    private string currentSubmissionId = string.Empty;
    public static GoogleSheetUploader instance;
    void Awake()
    {
       if (instance != null && instance != this)
        {
            Destroy(gameObject); // Destroy duplicates
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (FormHandler.instance.loadingIndicator != null)
            FormHandler.instance.loadingIndicator.SetActive(false);
    }

    public void OnSubmit()
    {
        FormHandler.instance.audioSource.Play();
        FormHandler.instance.androidHaptics.Vibrate();
        HCPUIController form = FormHandler.instance.hcpUIController;
        if (form == null)
        {
            AndroidToaster.ShowToast("The form is not available.");
            return;
        }

        if (!form.TryCreateRecord(out HCPRecord data, out string validationMessage))
        {
            AndroidToaster.ShowToast(string.IsNullOrEmpty(validationMessage)
                ? "Please complete all required fields."
                : validationMessage);
            return;
        }

        // This call is made directly from the Submit button's click event,
        // which is the moment browsers permit a WebGL page to enter fullscreen.
        WebGLPresentation.EnterFullscreenFromUserGesture();

        // Test entries are allowed to continue through the app without sending any
        // personal data to the Google Sheet. Comparison is case-insensitive so the
        // operator can enter "test user" naturally.
        if (string.Equals(data.eseName, "Test User", StringComparison.OrdinalIgnoreCase))
        {
            currentSubmissionId = string.Empty;
            AndroidToaster.ShowToast("Test form accepted. Upload skipped.");
            LoadGameScene();
            return;
        }

        data.submissionId = Guid.NewGuid().ToString("N");
        currentSubmissionId = data.submissionId;

        AndroidToaster.ShowToast("Data saved. Uploading...");
        Debug.Log("Data saved. Uploading...");

        retryQueue.Enqueue(data);
        StartCoroutine(PostToGoogleWithTimeout(data, 10f));
    }

    IEnumerator PostToGoogleWithTimeout(HCPRecord data, float maxWaitTime)
    {
         FormHandler.instance.loadingIndicator.SetActive(true);
        isUploading = true;

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            AndroidToaster.ShowToast("No internet. Will retry...");
            yield return new WaitForSeconds(10);
            isUploading = false;
            TryUploadNext();
            LoadGameScene();
            yield break;
        }

        AndroidToaster.ShowToast("Preparing to submit...");
        string jsonData = JsonUtility.ToJson(data);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(googleFormURL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = (int)maxWaitTime;

            float elapsedTime = 0f;
            var operation = request.SendWebRequest();

            while (!operation.isDone && elapsedTime < maxWaitTime)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (ServerAccepted(request))
            {
                Debug.Log("Submitted successfully!");
                AndroidToaster.ShowToast("Submitted successfully!");
                retryQueue.Dequeue();
            }
            else
            {
                RegenerateDuplicateSubmissionIdIfNeeded(
                    data,
                    request.downloadHandler.text);
                Debug.LogWarning(
                    "Submission failed: " +
                    GetRequestError(request));
                AndroidToaster.ShowToast("Upload will retry in background.");
                yield return new WaitForSeconds(5);
            }
        }

        isUploading = false;
        TryUploadNext();
        LoadGameScene();
    }

    private void LoadGameScene()
    {
        if (SceneManager.GetActiveScene().name != "LaunchScene")
        {
            SceneManager.LoadScene("LauncherScene");
        }
    }

     void Update()
    {
        if (FormHandler.instance == null)
            return;

        if (FormHandler.instance.date != null)
            FormHandler.instance.date.text = System.DateTime.Now.ToString("yyyy-MM-dd");
        if (FormHandler.instance.time != null)
            FormHandler.instance.time.text = System.DateTime.Now.ToString("HH:mm:ss");
    }

    private void TryUploadNext()
    {
        if (!isUploading && retryQueue.Count > 0)
        {
            StartCoroutine(PostToGoogle(retryQueue.Peek()));
        }
    }

    IEnumerator PostToGoogle(HCPRecord data)
    {
        isUploading = true;

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            AndroidToaster.ShowToast("No internet. Will retry...");
            yield return new WaitForSeconds(10);
            isUploading = false;
            TryUploadNext();
            yield break;
        }

        string jsonData = JsonUtility.ToJson(data);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(googleFormURL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (ServerAccepted(request))
            {
                AndroidToaster.ShowToast("Submitted successfully!");
                retryQueue.Dequeue();
            }
            else
            {
                RegenerateDuplicateSubmissionIdIfNeeded(
                    data,
                    request.downloadHandler.text);
                AndroidToaster.ShowToast("Failed. Will retry in background.");
                yield return new WaitForSeconds(5);
            }
        }

        isUploading = false;
        TryUploadNext();
    }

    private static bool ServerAccepted(UnityWebRequest request)
    {
        if (request.result != UnityWebRequest.Result.Success ||
            request.downloadHandler == null)
            return false;

        string response = request.downloadHandler.text ?? string.Empty;
        return response.Contains("\"ok\":true") ||
               response.Contains("\"success\":true");
    }

    private void RegenerateDuplicateSubmissionIdIfNeeded(
        HCPRecord data,
        string response)
    {
        if (data == null ||
            string.IsNullOrEmpty(response) ||
            response.IndexOf(
                "Duplicate Submission ID",
                StringComparison.OrdinalIgnoreCase) < 0)
            return;

        data.submissionId = Guid.NewGuid().ToString("N");
        currentSubmissionId = data.submissionId;
        Debug.LogWarning(
            "A duplicate submission ID was rejected. Generated a new ID.");
    }

    private static string GetRequestError(UnityWebRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.error))
            return request.error;
        if (request.downloadHandler != null)
            return request.downloadHandler.text;
        return "Unknown server response.";
    }
}
