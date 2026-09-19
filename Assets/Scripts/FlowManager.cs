using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FlowManager : MonoBehaviour
{
    internal static FlowManager Instance;
    [SerializeField] private GameObject[] flowObjects;
    //[SerializeField] private GameObject Bg;
    public static int flowIndex = 0;
    public GameObject resetButton,nextButton,previousButton,homeButton,belowtext;
    public static bool textWaveFinished = false;
    public AndroidHaptics androidHaptics;
    public bool IsTransitioning => pendingTransitionVideo != null;

    private VideoPlayerDisplay pendingTransitionVideo;
    private Coroutine videoTransitionTimeout;
    private int pendingFlowIndex;
    private Button nextButtonComponent;
    private Button previousButtonComponent;

    public Transform refImage,refCloseButton,infoIcon;

     public enum ScreenSize { iPhone, Tab, iPad }
    public static ScreenSize screenSize;
    private float GetScreenAspectRatio()
    {
        return (float)Screen.width / Screen.height;
    }
    private ScreenSize DetermineScreenSize(float aspectRatio)
    {
        if (aspectRatio > 1.7f) return ScreenSize.iPhone;
        if (aspectRatio > 1.4f) return ScreenSize.Tab;
        return ScreenSize.iPad;
    }
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void OnEnable()
    {
        nextButtonComponent = nextButton != null ? nextButton.GetComponent<Button>() : null;
        previousButtonComponent = previousButton != null ? previousButton.GetComponent<Button>() : null;
        if (nextButtonComponent != null)
        {
            nextButtonComponent.onClick.AddListener(NextStep);
        }
        if (previousButtonComponent != null)
        {
            previousButtonComponent.onClick.AddListener(PreviousFlowObject);
        }
    }
    void Start()
    {
        refImage.gameObject.SetActive(false);
         Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.targetFrameRate = 120; //
        float aspect = GetScreenAspectRatio();
        screenSize = DetermineScreenSize(aspect);
        Debug.Log("Aspect ratio = " + aspect + ", ScreenSize = " + screenSize);
        ActivateFlowObject(flowIndex);
    }

#if UNITY_EDITOR
    private void Update()
    {
        bool controlPressed =
            Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (!controlPressed)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            NextStep();
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            PreviousFlowObject();
        }
    }
#endif

private void ShowResetButton()
    {
        CancelInvoke(nameof(ShowResetButton));
         resetButton.SetActive(true);
    }

    public void OnInfoIconClicked()
    {
        if (AudioHandler.instance != null)
        {
            AudioHandler.instance.PlaySFX("Click");
        }
        refImage.gameObject.SetActive(true);
        refCloseButton.gameObject.SetActive(false);
        refImage.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).From(Vector3.zero).OnComplete(() =>
        {
            refCloseButton.gameObject.SetActive(true);
        });
    }
    public void OnRefCloseButtonClicked()
    {
        if (AudioHandler.instance != null)
        {
            AudioHandler.instance.PlaySFX("Click");
        }
        refCloseButton.gameObject.SetActive(false);
        refImage.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
        {
            refImage.gameObject.SetActive(false);
        });
    }
    private void ActivateFlowObject(int index)
    {
        if(index==flowObjects.Length-1)
        {
           homeButton.SetActive(false);
           infoIcon.gameObject.SetActive(true);
        }
        else
        {
        homeButton.SetActive(true);
           infoIcon.gameObject.SetActive(false);
        }
        if (index < 0 || index >= flowObjects.Length) return;
        if (AudioHandler.instance != null)
        {
            AudioHandler.instance.StopVO();
        }

        for (int i = 0; i < flowObjects.Length; i++)
        {
            if (i != index && flowObjects[i].activeSelf)
            {
                // Video players live outside the slide roots, so stop the outgoing display explicitly.
                VideoPlayerDisplay outgoingVideo = GetVideoDisplay(flowObjects[i]);
                if (outgoingVideo != null)
                {
                    outgoingVideo.StopVideo();
                }
            }
            flowObjects[i].SetActive(i == index);
        }
        CancelInvoke(nameof(ShowResetButton));
        resetButton.SetActive(false);
        if (index == flowObjects.Length - 1)
        {
            Invoke(nameof(ShowResetButton),3.0f);
            //resetButton.SetActive(true); // Show reset button on the last flow object
        }
        else
        {
            resetButton.SetActive(false); // Hide reset button on other flow objects
        }
        UpdateNavigationButtons();
        if(index==0||index==1)
         belowtext.SetActive(true);
         else
          belowtext.SetActive(false);
        
    }

    private void UpdateNavigationButtons()
    {
        bool showNavigation = flowIndex > 0 && flowIndex < flowObjects.Length - 1;
        nextButton.SetActive(showNavigation);
        previousButton.SetActive(showNavigation);
        if (nextButtonComponent != null)
        {
            nextButtonComponent.interactable = showNavigation && !IsTransitioning && flowIndex < flowObjects.Length - 1;
        }
        if (previousButtonComponent != null)
        {
            previousButtonComponent.interactable = showNavigation && !IsTransitioning;
        }
    }

    public void NextStep()
    {
        if (IsTransitioning || flowIndex <= 0 || flowIndex >= flowObjects.Length - 1)
        {
            return;
        }

        if (AudioHandler.instance != null)
        {
            AudioHandler.instance.PlaySFX("Click");
        }

        var slideTwo = GetCurrentFlowObject().GetComponent<SlideTwo>();
        if (slideTwo != null && !slideTwo.IsAtCompletedStep)
        {
            slideTwo.SkipToCompletedStep();
            return;
        }

        NextFlowObject();
    }

    public void NextFlowObject()
    {
        // Automatic video completion and slide-specific Start buttons advance a whole slide.
        if (IsTransitioning || flowIndex >= flowObjects.Length - 1)
        {
            return;
        }
        RequestFlowObject(flowIndex + 1);
    }

    private static VideoPlayerDisplay GetVideoDisplay(GameObject slide)
    {
        var dissolve = slide.GetComponent<SlideVideoDissolve>();
        if (dissolve != null)
        {
            return dissolve.videoPlayerDisplay;
        }

        var finalSlide = slide.GetComponent<SlideThree>();
        return finalSlide != null ? finalSlide.videoPlayerDisplay : null;
    }

    private void RequestFlowObject(int index)
    {
        if (index < 0 || index >= flowObjects.Length)
        {
            return;
        }

        CancelVideoTransition(true);
        var slideTwo = flowObjects[index].GetComponent<SlideTwo>();
        if (slideTwo != null && index != flowIndex)
        {
            slideTwo.EnterAtCompletedStep(index < flowIndex);
        }
        VideoPlayerDisplay nextVideo = GetVideoDisplay(flowObjects[index]);
        if (nextVideo != null && nextVideo.isActiveAndEnabled && !nextVideo.HasVisibleFrame)
        {
            // Decode on the already-active player while the current slide remains visible.
            pendingFlowIndex = index;
            pendingTransitionVideo = nextVideo;
            nextVideo.FirstFrameDisplayed += OnTransitionFrameDisplayed;
            nextVideo.PlaybackFailed += OnTransitionVideoFailed;
            UpdateNavigationButtons();
            videoTransitionTimeout = StartCoroutine(WaitForTransitionVideo());
            nextVideo.PlayVideo();
            return;
        }

        flowIndex = index;
        ActivateFlowObject(index);
    }

    private void OnTransitionFrameDisplayed()
    {
        int index = pendingFlowIndex;
        CancelVideoTransition(false);
        flowIndex = index;
        ActivateFlowObject(index);
    }

    private IEnumerator WaitForTransitionVideo()
    {
        yield return new WaitForSecondsRealtime(15f);
        videoTransitionTimeout = null;
        Debug.LogWarning("Video startup timed out. Keeping the current slide visible; navigation can be retried.", this);
        OnTransitionVideoFailed();
    }

    private void OnTransitionVideoFailed()
    {
        CancelVideoTransition(true);
        UpdateNavigationButtons();
    }

    private void CancelVideoTransition(bool stopVideo)
    {
        if (videoTransitionTimeout != null)
        {
            StopCoroutine(videoTransitionTimeout);
            videoTransitionTimeout = null;
        }

        if (pendingTransitionVideo != null)
        {
            pendingTransitionVideo.FirstFrameDisplayed -= OnTransitionFrameDisplayed;
            pendingTransitionVideo.PlaybackFailed -= OnTransitionVideoFailed;
            if (stopVideo)
            {
                pendingTransitionVideo.StopVideo();
            }
            pendingTransitionVideo = null;
        }
    }

    private void OnDisable()
    {
        CancelVideoTransition(true);
        if (nextButtonComponent != null)
        {
            nextButtonComponent.onClick.RemoveListener(NextStep);
        }
        if (previousButtonComponent != null)
        {
            previousButtonComponent.onClick.RemoveListener(PreviousFlowObject);
        }
    }

    public void PreviousFlowObject()
    {
        if (IsTransitioning || flowIndex <= 0)
        {
            return;
        }

        if (AudioHandler.instance != null)
        {
            AudioHandler.instance.PlaySFX("Click");
        }

        var slideTwo = GetCurrentFlowObject().GetComponent<SlideTwo>();
        if (slideTwo != null && slideTwo.IsAtCompletedStep)
        {
            slideTwo.RestartFromBeginning();
            return;
        }

        RequestFlowObject(flowIndex - 1);
    }

    public void ResetFlow()
    {
        CancelVideoTransition(true);
        textWaveFinished = false;

            AudioHandler.instance.PlaySFX("Click");
            VibrateDevice();
            flowIndex = 0;
            SceneManager.LoadScene("LauncherScene"); // Reload the scene if at the last flow object
            //SceneManager.LoadScene("DebugScene"); // Reload the scene if at the last flow object
        

    }

    public int GetCurrentFlowIndex()
    {
        return flowIndex;
    }

    public GameObject GetCurrentFlowObject()
    {
        if (flowIndex < 0 || flowIndex >= flowObjects.Length) return null;
        return flowObjects[flowIndex];
    }

    public GameObject GetFlowObject(int index)
    {
        return index >= 0 && index < flowObjects.Length
            ? flowObjects[index]
            : null;
    }
    public void SetFlowIndex(int index)
    {
        RequestFlowObject(index);
    }
    public void VibrateDevice(long milliseconds = 100)
    {
        androidHaptics.Vibrate(milliseconds);
    }
}
