using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoPlayerDisplay : MonoBehaviour
{
    [SerializeField] private GameObject displayRoot;
    [SerializeField] private RawImage videoImage;
    [SerializeField] private AspectRatioFitter aspectRatioFitter;
    [Tooltip("MP4 filename in Assets/StreamingAssets/Video, used by WebGL builds. Leave empty for non-WebGL-only players.")]
    [SerializeField] private string webVideoFileName;
    public bool moveToNextSlideOnFinish = true;

    [Tooltip("Source pixels hidden at the right (X) and bottom (Y) edges. These videos have 8 pixels of codec padding; 10 also excludes filtering along that border. Set to zero to disable.")]
    [SerializeField] private Vector2 videoEdgeCropPixels = new Vector2(10f, 10f);

    [Tooltip("Hold the first video frame so static objects can be aligned with the video.")]
    [SerializeField] private bool pauseOnFirstFrame;

    public event Action FirstFrameDisplayed;
    public event Action PlaybackFailed;
    public bool HasVisibleFrame { get; private set; }

    private VideoPlayer videoPlayer;
    private bool preparing;
    private bool playWhenPrepared;
    private bool holdingLastFrame;
    private Coroutine showFirstFrameRoutine;
    private bool previewingFirstFrame;
    private bool waitingForFirstFrame;
    private bool previousSkipOnDrop;
    private bool previousSendFrameReadyEvents;

    private void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        if (!string.IsNullOrWhiteSpace(webVideoFileName))
        {
            // Videos are shipped in StreamingAssets and played via URL. This avoids embedding
            // duplicate VideoClip assets in the player build and is required by WebGL.
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = Application.streamingAssetsPath + "/Video/" + webVideoFileName;
        }
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoImage.texture = videoPlayer.targetTexture;
        VideoEdgeCrop edgeCrop = videoImage.GetComponent<VideoEdgeCrop>();
        if (edgeCrop == null)
        {
            edgeCrop = videoImage.gameObject.AddComponent<VideoEdgeCrop>();
        }
        edgeCrop.CropPixels = videoEdgeCropPixels;
        displayRoot.SetActive(false);
    }

    private void OnEnable()
    {
        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.frameReady += OnFrameReady;
        videoPlayer.loopPointReached += OnFinished;
        videoPlayer.errorReceived += OnError;
        // These players stay active outside the slide roots, so preload while earlier slides run.
        PrepareVideo();
    }

    private void OnDisable()
    {
        videoPlayer.prepareCompleted -= OnPrepared;
        videoPlayer.frameReady -= OnFrameReady;
        videoPlayer.loopPointReached -= OnFinished;
        videoPlayer.errorReceived -= OnError;
        StopVideo();
    }

    private void Update()
    {
        // VideoPlayer.Stop() has no event. Also support callers using the player directly.
        if (!waitingForFirstFrame && !holdingLastFrame && displayRoot.activeSelf && !videoPlayer.isPlaying && !videoPlayer.isPaused)
        {
            HasVisibleFrame = false;
            displayRoot.SetActive(false);
        }
    }

    public void PrepareVideo()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || preparing || videoPlayer.isPrepared)
        {
            return;
        }

        preparing = true;
        videoPlayer.Prepare();
    }

    private void OnPrepared(VideoPlayer player)
    {
        preparing = false;
        if (playWhenPrepared)
        {
            playWhenPrepared = false;
            player.Play();
        }
    }

    [ContextMenu("Play Video")]
    public void PlayVideo()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        // The incoming slide's OnEnable may call PlayVideo again after the handoff.
        if (waitingForFirstFrame || (HasVisibleFrame && (videoPlayer.isPlaying || previewingFirstFrame)))
        {
            return;
        }

        if (pauseOnFirstFrame)
        {
            PreviewFirstFrame();
            return;
        }

        ResumeVideo();
    }

    [ContextMenu("Preview First Frame")]
    public void PreviewFirstFrame()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        StopVideo();
        previewingFirstFrame = true;
        BeginWaitingForFirstFrame();
        // Decode the first frame before pausing; pausing immediately can leave a blank texture.
        PlayWhenReady();
    }

    [ContextMenu("Resume Video")]
    public void ResumeVideo()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        previewingFirstFrame = false;
        holdingLastFrame = false;
        if (!HasVisibleFrame)
        {
            BeginWaitingForFirstFrame();
        }
        PlayWhenReady();
    }

    private void PlayWhenReady()
    {
        if (videoPlayer.isPrepared)
        {
            playWhenPrepared = false;
            videoPlayer.Play();
        }
        else
        {
            playWhenPrepared = true;
            PrepareVideo();
        }
    }

    private void BeginWaitingForFirstFrame()
    {
        if (waitingForFirstFrame)
        {
            return;
        }

        waitingForFirstFrame = true;
        previousSkipOnDrop = videoPlayer.skipOnDrop;
        previousSendFrameReadyEvents = videoPlayer.sendFrameReadyEvents;
        videoPlayer.skipOnDrop = false;
        videoPlayer.sendFrameReadyEvents = true;
    }

    [ContextMenu("Stop Video")]
    public void StopVideo()
    {
        if (showFirstFrameRoutine != null)
        {
            StopCoroutine(showFirstFrameRoutine);
            showFirstFrameRoutine = null;
        }
        FinishWaitingForFirstFrame();
        preparing = false;
        playWhenPrepared = false;
        HasVisibleFrame = false;
        holdingLastFrame = false;
        previewingFirstFrame = false;
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (displayRoot != null)
        {
            displayRoot.SetActive(false);
        }
    }

    private void OnFrameReady(VideoPlayer player, long frameIndex)
    {
        if (!waitingForFirstFrame || showFirstFrameRoutine != null)
        {
            return;
        }

        if (previewingFirstFrame)
        {
            player.Pause();
            if (frameIndex != 0)
            {
                player.frame = 0;
                return;
            }
        }

        showFirstFrameRoutine = StartCoroutine(ShowFirstFrame(player));
    }

    private IEnumerator ShowFirstFrame(VideoPlayer player)
    {
        // Let the decoded frame reach the RenderTexture before switching the visible slide.
        yield return new WaitForEndOfFrame();
        showFirstFrameRoutine = null;
        FinishWaitingForFirstFrame();
        ShowVideo(player);
        HasVisibleFrame = true;
        FirstFrameDisplayed?.Invoke();
    }

    private void FinishWaitingForFirstFrame()
    {
        if (!waitingForFirstFrame)
        {
            return;
        }

        waitingForFirstFrame = false;
        videoPlayer.skipOnDrop = previousSkipOnDrop;
        videoPlayer.sendFrameReadyEvents = previousSendFrameReadyEvents;
    }

    private void ShowVideo(VideoPlayer player)
    {
        if (player.width > 0 && player.height > 0)
        {
            aspectRatioFitter.aspectRatio = (float)player.width / player.height;
        }

        displayRoot.SetActive(true);
    }

    private void OnFinished(VideoPlayer player)
    {
        if (previewingFirstFrame || holdingLastFrame)
        {
            return;
        }

        if (!player.isLooping)
        {
           
            if (moveToNextSlideOnFinish)
            {
                if (FlowManager.Instance != null)
                {
                    // Preserve the last rendered image until the next video's frame is ready.
                    holdingLastFrame = true;
                    player.Pause();
                    FlowManager.Instance.NextFlowObject();
                }
            }   
        }
    }

    private void OnError(VideoPlayer player, string message)
    {
        StopVideo();
        Debug.LogError("Video playback failed: " + message, this);
        PlaybackFailed?.Invoke();
    }
}
