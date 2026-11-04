using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;

public class GoogleSheetUploader : MonoBehaviour
{
     [SerializeField] private string googleSheetURL = "YOUR_SHEET_URL_HERE";
    [SerializeField] private string googleFormURL = "YOUR_WEB_APP_URL_HERE";
    private readonly Queue<PendingUpload> retryQueue = new Queue<PendingUpload>();
    private bool isUploading = false;
    private FormHandler submittedForm;

    private class PendingUpload
    {
        public HCPRecord data;
        public bool firstAttemptCompleted;
    }
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

        if (FormHandler.instance != null && FormHandler.instance.loadingIndicator != null)
            FormHandler.instance.loadingIndicator.SetActive(false);
    }

    public void OnSubmit()
    {
        FormHandler handler = FormHandler.instance;
        if (handler == null || submittedForm == handler)
            return;

        if (handler.audioSource != null)
            handler.audioSource.Play();
        if (handler.androidHaptics != null)
            handler.androidHaptics.Vibrate();
        HCPUIController form = handler.hcpUIController;
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

        // Test entries are allowed to continue through the app without sending any
        // personal data to the Google Sheet. Comparison is case-insensitive so the
        // operator can enter "test user" naturally.
        if (string.Equals(data.eseName, "Test User", StringComparison.OrdinalIgnoreCase))
        {
            submittedForm = handler;
            AndroidToaster.ShowToast("Test form accepted. Upload skipped.");
            LoadGameScene();
            return;
        }

        data.submissionId = Guid.NewGuid().ToString("N");
        submittedForm = handler;
        if (handler.submitButton != null)
            handler.submitButton.interactable = false;

        AndroidToaster.ShowToast("Data saved. Uploading...");
        Debug.Log("Data saved. Uploading...");

        PendingUpload pending = new PendingUpload { data = data };
        retryQueue.Enqueue(pending);
        TryUploadNext();
        StartCoroutine(ContinueAfterFirstAttempt(pending, handler));
    }

    private IEnumerator ContinueAfterFirstAttempt(PendingUpload pending, FormHandler handler)
    {
        if (handler != null && handler.loadingIndicator != null)
            handler.loadingIndicator.SetActive(true);

        while (!pending.firstAttemptCompleted)
            yield return null;

        if (handler != null && handler == FormHandler.instance)
        {
            if (handler.loadingIndicator != null)
                handler.loadingIndicator.SetActive(false);
            LoadGameScene();
        }
    }

    private void LoadGameScene()
    {
        if (SceneManager.GetActiveScene().name != "LauncherScene")
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
            // Set the guard before starting the coroutine so all submissions share
            // one worker, including new forms submitted while an older one retries.
            isUploading = true;
            StartCoroutine(ProcessUploadQueue());
        }
    }

    private IEnumerator ProcessUploadQueue()
    {
        try
        {
            while (retryQueue.Count > 0)
            {
                PendingUpload pending = retryQueue.Peek();
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    AndroidToaster.ShowToast("No internet. Will retry...");
                    pending.firstAttemptCompleted = true;
                    yield return new WaitForSecondsRealtime(10);
                    continue;
                }

                bool accepted;
                using (UnityWebRequest request = CreateUploadRequest(pending.data, 30))
                {
                    // Await completion or the request's own timeout. A local timer
                    // cannot tell whether the server has already saved the row.
                    yield return request.SendWebRequest();
                    accepted = ServerAccepted(request);
                    if (!accepted)
                        Debug.LogWarning("Submission failed: " + GetRequestError(request));
                }

                pending.firstAttemptCompleted = true;
                if (accepted)
                {
                    retryQueue.Dequeue();
                    AndroidToaster.ShowToast("Submitted successfully!");
                }
                else
                {
                    // Keep the same ID even after a timeout: the original POST may
                    // have saved successfully while its response was lost.
                    AndroidToaster.ShowToast("Upload will retry in background.");
                    yield return new WaitForSecondsRealtime(5);
                }
            }
        }
        finally
        {
            isUploading = false;
        }
    }

    private UnityWebRequest CreateUploadRequest(HCPRecord data, int timeoutSeconds)
    {
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
        UnityWebRequest request = new UnityWebRequest(googleFormURL, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
#if UNITY_WEBGL && !UNITY_EDITOR
        // Apps Script parses the JSON body regardless of its MIME type. text/plain
        // avoids the browser's OPTIONS preflight, which Apps Script cannot serve.
        request.SetRequestHeader("Content-Type", "text/plain;charset=UTF-8");
#else
        request.SetRequestHeader("Content-Type", "application/json");
#endif
        request.timeout = timeoutSeconds;
        return request;
    }

    [Serializable]
    private class UploadResponse
    {
        public bool ok = false;
        public bool success = false;
        public string error = null;
    }

    private static bool ServerAccepted(UnityWebRequest request)
    {
        if (request.result != UnityWebRequest.Result.Success ||
            request.downloadHandler == null)
            return false;

        try
        {
            UploadResponse response = JsonUtility.FromJson<UploadResponse>(
                request.downloadHandler.text);
            // The server explicitly rejecting this ID as a duplicate confirms it
            // already has the submission. Stop retrying rather than create a new ID.
            return response != null && (response.ok || response.success ||
                (!string.IsNullOrEmpty(response.error) && response.error.Trim().StartsWith(
                    "Duplicate Submission ID", StringComparison.OrdinalIgnoreCase)));
        }
        catch (ArgumentException)
        {
            return false;
        }
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
