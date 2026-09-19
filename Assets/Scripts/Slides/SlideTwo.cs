using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SlideTwo : MonoBehaviour
{
    [SerializeField] private List<WaveTypeWriter> waveTypewriters,waveTypeWriters2;
    public GameObject startButton,BeakerOne,BeakerTwo,BaseOne,BaseTwo,LeaverOne,LeaverTwo,dragHand,dragHandTwo,titleOne,titleTwo,completeTitle,CompleteDisc,Clock;
    public List<GameObject> Medicines;
    [SerializeField] private List<Transform> beakerOneEndPositions = new List<Transform>();
    [SerializeField] private List<Transform> beakerTwoEndPositions = new List<Transform>();
    [SerializeField] private Transform beakerOneTargetPosY;
    [SerializeField] private Transform beakerTwoTargetPosY;

    public SpriteRenderer sr1,sr2;

    [Header("Drag And Drop")]
    [SerializeField] private int capsulesPerBeaker = 3;
    [SerializeField] private float hitPadding = 0.2f;
    [Tooltip("Keeps the dragged tablet visible above the user's finger.")]
    [SerializeField] private float dragTouchOffsetY = 0.75f;
    [SerializeField] private float returnDuration = 0.35f;
    [SerializeField] private float beakerPathDuration = 0.7f;
    [SerializeField] private float beakerSlotHorizontalSpacing = 0.35f;
    [SerializeField] private float beakerSlotVerticalPosition = 0.42f;
    [SerializeField] private float beakerPathLift = 1.35f;
    [SerializeField] private int sortingOrder=8;
    [SerializeField] private float rotationZ=90f;

    [Header("Start Test")]
    [SerializeField] private float leverTestMoveY = 0.7f;
    [SerializeField] private float leverTestDuration = 5f;
    [SerializeField] private float leverTestSegmentDuration = 0.35f;

    [Header("Start Button Pulse")]
    [SerializeField] private float startButtonPulseScale = 1.08f;
    [SerializeField] private float startButtonPulseDuration = 0.65f;

    private readonly List<CapsuleState> capsuleStates = new List<CapsuleState>();
    private Button startButtonComponent;
    private BeakerTarget beakerOneTarget;
    private BeakerTarget beakerTwoTarget;
    private CapsuleState draggedCapsule;
    private Camera dragCamera;
    private Coroutine typeTextRoutine;
    private Vector3 dragOffset;
    private float dragZ;
    private bool dragEnabled;
    private bool leverPositionsCached;
    private bool leverTestRunning;
    private Vector3 leverOneOriginalLocalPosition;
    private Vector3 leverTwoOriginalLocalPosition;
    private Tween leverOneTween;
    private Tween leverTwoTween;
    private Tween leverTestDelayTween;
    private Tween startButtonTween;
    public bool IsAtCompletedStep { get; private set; }
    private bool enterAtCompletedStep;

    private class CapsuleState
    {
        public GameObject gameObject;
        public Transform transform;
        public SpriteRenderer renderer;
        public Transform originalParent;
        public Vector3 originalLocalPosition;
        public Quaternion originalLocalRotation;
        public Vector3 originalLocalScale;
        public int originalSortingOrder;
        public bool isPlaced;
        public Tween activeTween;
    }

    private class BeakerTarget
    {
        public GameObject gameObject;
        public SpriteRenderer renderer;
        public List<Transform> endPositions;
        public Transform targetPosY;
        public Transform placedParent;
        public int capsuleCount;
    }

    private struct PointerFrame
    {
        public Vector2 screenPosition;
        public bool isDown;
        public bool wasPressed;
        public bool wasReleased;
    }
 public GameObject holder,panel;
    private void SetHolderScale()
    {
        if(FlowManager.screenSize == FlowManager.ScreenSize.Tab)
        {
            panel.transform.localScale=Vector3.one*0.95f;
            holder.transform.localScale=Vector3.one*0.95f;
        }
        else if(FlowManager.screenSize == FlowManager.ScreenSize.iPad)
        {
            waveTypewriters[0].GetComponent<RectTransform>().anchoredPosition = new Vector2(14, 0);
            //startButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            panel.transform.localScale=Vector3.one*0.75f;
            holder.transform.localScale=Vector3.one*0.75f;
        }
        else
        {
            panel.transform.localScale=Vector3.one;
            holder.transform.localScale=Vector3.one;
        }
    }

    private void Awake()
    {
        CacheSceneReferences();
    }

    void OnEnable()
    {
        if (enterAtCompletedStep)
        {
            enterAtCompletedStep = false;
            SkipToCompletedStep();
        }
        else
        {
            RestartFromBeginning();
        }
    }

    public void EnterAtCompletedStep(bool completed)
    {
        enterAtCompletedStep = completed;
    }

    public void RestartFromBeginning()
    {
        StopStepAnimations();
        IsAtCompletedStep = false;
       // SetHolderScale();
        SetSpriteAlpha(sr1, 1f);
        SetSpriteAlpha(sr2, 1f);
        completeTitle.SetActive(false);
        Clock.SetActive(false);
        CompleteDisc.SetActive(false);
        CacheSceneReferences();
        ResetDragState();

        startButton.SetActive(false);
        BeakerOne.SetActive(false);
        BeakerTwo.SetActive(false);
        BaseOne.SetActive(false);
        BaseTwo.SetActive(false);
        dragHand.SetActive(false);
        dragHandTwo.SetActive(false);
        titleOne.SetActive(false);
        titleTwo.SetActive(false);
        foreach (var med in Medicines)
        {
            med.SetActive(false);
        }
        typeTextRoutine = StartCoroutine(TypeText());
    }

    private void OnDisable()
    {
        StopStepAnimations();

        if (startButtonComponent != null)
        {
            startButtonComponent.onClick.RemoveListener(OnStartButtonClicked);
        }

    }

    private void StopStepAnimations()
    {
        StopStartButtonPulse(true);
        StopAllCoroutines();
        typeTextRoutine = null;
        draggedCapsule = null;
        dragEnabled = false;
        KillCapsuleTweens();
        StopLeverTestAnimation(true);
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            child.DOKill();
        }
        foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>(true))
        {
            sprite.DOKill();
        }
        ResetWriters(waveTypewriters);
        ResetWriters(waveTypeWriters2);
        FlowManager.textWaveFinished = false;
        if (AudioHandler.instance != null)
        {
            AudioHandler.instance.StopVO();
        }
    }

    private static void ResetWriters(List<WaveTypeWriter> writers)
    {
        if (writers == null) return;
        foreach (var writer in writers)
        {
            if (writer != null) writer.ResetText();
        }
    }

    private static void SetSpriteAlpha(SpriteRenderer sprite, float alpha)
    {
        if (sprite == null) return;
        Color color = sprite.color;
        color.a = alpha;
        sprite.color = color;
    }

    public void SkipToCompletedStep()
    {
        StopStepAnimations();
        IsAtCompletedStep = false;
        CacheSceneReferences();
        ResetDragState();

        if (beakerOneTarget == null || beakerTwoTarget == null || capsulesPerBeaker <= 0 ||
            capsuleStates.Count != capsulesPerBeaker * 2)
        {
            Debug.LogWarning("Cannot skip capsule placement: assign both beakers and the capsules for every slot.", this);
            RestartFromBeginning();
            return;
        }

        completeTitle.SetActive(false);
        Clock.SetActive(false);
        CompleteDisc.SetActive(false);
        startButton.SetActive(false);
        dragHand.SetActive(false);
        dragHandTwo.SetActive(false);
        BaseOne.SetActive(true);
        BaseTwo.SetActive(true);
        SetSpriteAlpha(BaseOne.GetComponent<SpriteRenderer>(), 1f);
        SetSpriteAlpha(BaseTwo.GetComponent<SpriteRenderer>(), 1f);
        BeakerOne.SetActive(true);
        BeakerTwo.SetActive(true);
        BeakerOne.transform.localScale = Vector3.one * 0.5f;
        BeakerTwo.transform.localScale = Vector3.one * 0.5f;
        titleOne.SetActive(true);
        titleTwo.SetActive(true);
        SetSpriteAlpha(sr1, 1f);
        SetSpriteAlpha(sr2, 1f);

        // Use the same slot positions, rotation, sorting and parent as a completed drag.
        for (int i = 0; i < capsuleStates.Count; i++)
        {
            CapsuleState capsule = capsuleStates[i];
            BeakerTarget beaker = i < capsulesPerBeaker ? beakerOneTarget : beakerTwoTarget;
            int slot = beaker.capsuleCount++;
            capsule.gameObject.SetActive(true);
            capsule.transform.localScale = Vector3.one;
            capsule.transform.rotation = Quaternion.Euler(0, 0, rotationZ);
            capsule.transform.position = GetBeakerSlotPosition(beaker, slot, capsule.transform.position.z);
            SetCapsuleSortingOrder(capsule);
            if (beaker.placedParent != null)
            {
                capsule.transform.SetParent(beaker.placedParent, true);
            }
            capsule.isPlaced = true;
        }

        BeginCompletedStep();
    }

    private void Update()
    {
        if (!dragEnabled)
        {
            return;
        }

        if (!TryGetPointerFrame(out PointerFrame pointerFrame))
        {
            return;
        }

        if (pointerFrame.wasPressed)
        {
            BeginDrag(pointerFrame.screenPosition);
        }

        if (draggedCapsule != null && pointerFrame.isDown)
        {
            DragCapsule(pointerFrame.screenPosition);
        }

        if (draggedCapsule != null && pointerFrame.wasReleased)
        {
            EndDrag();
        }
    }

    private IEnumerator TypeText()
    {
        yield return new WaitForSeconds(0.25f);
         AudioHandler.instance.PlayVO("AsPerIndian");
        foreach (var writer in waveTypewriters)
        {
            writer.PlayTextAnimation();
            yield return new WaitUntil(() => FlowManager.textWaveFinished);
        }
        BaseOne.SetActive(true);
        BaseOne.GetComponent<SpriteRenderer>().DOFade(1, 0.5f).From(0);
        BaseTwo.SetActive(true);
        BaseTwo.GetComponent<SpriteRenderer>().DOFade(1, 0.5f).From(0);
        yield return new WaitForSeconds(0.5f);
        AudioHandler.instance.PlaySFX("Pop2");
        BeakerOne.SetActive(true);
        BeakerOne.transform.DOScale(Vector3.one*0.5f, 0.5f).SetEase(Ease.OutBack).From(Vector3.zero);
        BeakerTwo.SetActive(true);
        BeakerTwo.transform.DOScale(Vector3.one*0.5f, 0.5f).SetEase(Ease.OutBack).From(Vector3.zero);
        yield return new WaitForSeconds(0.5f);
        titleOne.SetActive(true);
        titleTwo.SetActive(true);
        foreach (var med in Medicines)
        {
            med.SetActive(true);
            med.transform.DOScale(Vector3.one*1.0f, 0.5f).SetEase(Ease.OutBack).From(Vector3.zero);
            AudioHandler.instance.PlaySFX("Select");
            yield return new WaitForSeconds(0.15f);
        }
yield return new WaitForSeconds(0.15f);
AudioHandler.instance.PlayVO("DragDrop");
        dragEnabled = true;
        if (dragHand != null)
        {
            dragHand.SetActive(true);
        }
        StartCoroutine(DisableWaveTypewriters());
    }
    private IEnumerator DisableWaveTypewriters()
    {
        yield return new WaitForSeconds(1.0f);
       foreach(var wave in waveTypewriters)
        {
            wave.PlayExitAnimation();
        }
    }

    private IEnumerator ShowWaveTypeWriters2()
    {
        // A restored checkpoint can enter during OnEnable, before the text objects finish Awake.
        yield return null;
        AudioHandler.instance.PlayVO("FilmCoated");
        foreach (var writer in waveTypeWriters2)
        {
            writer.PlayTextAnimation();
            yield return new WaitUntil(() => FlowManager.textWaveFinished);
        }
        yield return new WaitForSeconds(0.50f);
        if (startButton != null)
        {
            AudioHandler.instance.PlaySFX("Pop2");
            AudioHandler.instance.PlayVO("StartTest");
            startButton.SetActive(true);
            ShowStartButtonWithPulse();
        }
    }

    public void OnStartButtonClicked()
    {
        if (leverTestRunning || (FlowManager.Instance != null && FlowManager.Instance.IsTransitioning))
        {
            return;
        }

        StopStartButtonPulse(true);
        PlaySfx("Click");
        if (FlowManager.Instance != null)
        {
            FlowManager.Instance.VibrateDevice();
        }

        // Keep this slide intact until the next video's first frame is displayed.
         if (FlowManager.Instance != null)
        {
            FlowManager.Instance.NextFlowObject();
        }
      // StartLeverTestAnimation();
    }

    private void CacheSceneReferences()
    {
        dragCamera = Camera.main;
        CacheLeverPositions();

        if (startButton != null)
        {
            startButtonComponent = startButton.GetComponent<Button>();
            if (startButtonComponent != null)
            {
                startButtonComponent.onClick.RemoveListener(OnStartButtonClicked);
                startButtonComponent.onClick.AddListener(OnStartButtonClicked);
            }
        }

        beakerOneTarget = CreateBeakerTarget(BeakerOne, beakerOneEndPositions, beakerOneTargetPosY, LeaverOne);
        beakerTwoTarget = CreateBeakerTarget(BeakerTwo, beakerTwoEndPositions, beakerTwoTargetPosY, LeaverTwo);
        CacheCapsules();
    }

    private void ShowStartButtonWithPulse()
    {
        StopStartButtonPulse(false);
        startButton.transform.localScale = Vector3.zero;
        startButtonTween = startButton.transform
            .DOScale(Vector3.one, 0.5f)
            .SetEase(Ease.OutBack)
            .OnComplete(StartStartButtonPulse);
    }

    private void StartStartButtonPulse()
    {
        if (startButton == null || !startButton.activeInHierarchy ||
            (startButtonComponent != null && !startButtonComponent.interactable))
        {
            return;
        }

        startButtonTween = startButton.transform
            .DOScale(Vector3.one * Mathf.Max(1f, startButtonPulseScale), Mathf.Max(0.05f, startButtonPulseDuration))
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopStartButtonPulse(bool resetScale)
    {
        startButtonTween?.Kill();
        startButtonTween = null;
        if (resetScale && startButton != null)
        {
            startButton.transform.localScale = Vector3.one;
        }
    }

    private void CacheLeverPositions()
    {
        if (leverPositionsCached)
        {
            return;
        }

        if (LeaverOne != null)
        {
            leverOneOriginalLocalPosition = LeaverOne.transform.localPosition;
        }

        if (LeaverTwo != null)
        {
            leverTwoOriginalLocalPosition = LeaverTwo.transform.localPosition;
        }

        leverPositionsCached = true;
    }

    private void StartLeverTestAnimation()
    {
        CacheLeverPositions();
        StopLeverTestAnimation(true);

        leverTestRunning = true;
        dragEnabled = false;

        if (startButtonComponent != null)
        {
            startButtonComponent.interactable = false;
        }

        leverOneTween = CreateLeverTestTween(LeaverOne, leverOneOriginalLocalPosition);
        leverTwoTween = CreateLeverTestTween(LeaverTwo, leverTwoOriginalLocalPosition);
        leverTestDelayTween = DOVirtual.DelayedCall(leverTestDuration, CompleteLeverTestAnimation);
    }

    private Tween CreateLeverTestTween(GameObject lever, Vector3 originalLocalPosition)
    {
        if (lever == null)
        {
            return null;
        }

        lever.transform.localPosition = originalLocalPosition;
        return lever.transform
            .DOLocalMoveY(originalLocalPosition.y + leverTestMoveY, Mathf.Max(0.01f, leverTestSegmentDuration))
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void CompleteLeverTestAnimation()
    {
        StopLeverTestAnimation(false);
        StartCoroutine(PlayCompleteSequence());
        
    }

    private IEnumerator PlayCompleteSequence()
    {
         Clock.SetActive(true);
        Clock.transform.DOScale(Vector3.one * 0.5f, 0.5f).SetEase(Ease.OutBack).From(Vector3.zero);
        AudioHandler.instance.PlayVO("OstocalciumCCMTablets");
        completeTitle.SetActive(true);
        completeTitle.transform.DOScale(Vector3.one * 0.5f, 0.5f).SetEase(Ease.OutBack).From(Vector3.zero);
        yield return new WaitForSeconds(1.5f);
        CompleteDisc.SetActive(true);
        CompleteDisc.transform.DOScale(Vector3.one * 0.5f, 0.5f).SetEase(Ease.OutBack).From(Vector3.zero);
        yield return new WaitForSeconds(2.5f);

        if (FlowManager.Instance != null)
        {
            FlowManager.Instance.NextFlowObject();
        }
    }

    private void StopLeverTestAnimation(bool resetPositions)
    {
        leverOneTween?.Kill();
        leverTwoTween?.Kill();
        leverTestDelayTween?.Kill();

        leverOneTween = null;
        leverTwoTween = null;
        leverTestDelayTween = null;
        leverTestRunning = false;

        if (startButtonComponent != null)
        {
            startButtonComponent.interactable = true;
        }

        if (!resetPositions)
        {
            return;
        }

        CacheLeverPositions();
        if (LeaverOne != null)
        {
            LeaverOne.transform.localPosition = leverOneOriginalLocalPosition;
        }

        if (LeaverTwo != null)
        {
            LeaverTwo.transform.localPosition = leverTwoOriginalLocalPosition;
        }
    }

    private BeakerTarget CreateBeakerTarget(GameObject beaker, List<Transform> endPositions, Transform targetPosY, GameObject placedParent)
    {
        if (beaker == null)
        {
            return null;
        }

        return new BeakerTarget
        {
            gameObject = beaker,
            renderer = beaker.GetComponent<SpriteRenderer>(),
            endPositions = endPositions,
            targetPosY = targetPosY,
            placedParent = placedParent != null ? placedParent.transform : null,
            capsuleCount = 0
        };
    }

    private void CacheCapsules()
    {
        foreach (var medicine in Medicines)
        {
            if (medicine == null || GetCapsuleState(medicine) != null)
            {
                continue;
            }

            SpriteRenderer spriteRenderer = medicine.GetComponent<SpriteRenderer>();
            capsuleStates.Add(new CapsuleState
            {
                gameObject = medicine,
                transform = medicine.transform,
                renderer = spriteRenderer,
                originalParent = medicine.transform.parent,
                originalLocalPosition = medicine.transform.localPosition,
                originalLocalRotation = medicine.transform.localRotation,
                originalLocalScale = medicine.transform.localScale,
                originalSortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder : 0
            });
        }
    }

    private CapsuleState GetCapsuleState(GameObject medicine)
    {
        for (int i = 0; i < capsuleStates.Count; i++)
        {
            if (capsuleStates[i].gameObject == medicine)
            {
                return capsuleStates[i];
            }
        }

        return null;
    }

    private void ResetDragState()
    {
        dragEnabled = false;
        draggedCapsule = null;
        StopLeverTestAnimation(true);

        if (beakerOneTarget != null)
        {
            beakerOneTarget.capsuleCount = 0;
        }

        if (beakerTwoTarget != null)
        {
            beakerTwoTarget.capsuleCount = 0;
        }

        if (startButton != null)
        {
            startButton.transform.localScale = Vector3.one;
        }

        foreach (var capsuleState in capsuleStates)
        {
            capsuleState.activeTween?.Kill();
            capsuleState.activeTween = null;
            capsuleState.isPlaced = false;

            if (capsuleState.transform.parent != capsuleState.originalParent)
            {
                capsuleState.transform.SetParent(capsuleState.originalParent, false);
            }

            capsuleState.transform.localPosition = capsuleState.originalLocalPosition;
            capsuleState.transform.localRotation = capsuleState.originalLocalRotation;
            capsuleState.transform.localScale = capsuleState.originalLocalScale;

            if (capsuleState.renderer != null)
            {
                capsuleState.renderer.sortingOrder = capsuleState.originalSortingOrder;
            }
        }
    }

    private void KillCapsuleTweens()
    {
        foreach (var capsuleState in capsuleStates)
        {
            capsuleState.activeTween?.Kill();
            capsuleState.activeTween = null;
        }
    }

    private bool TryGetPointerFrame(out PointerFrame pointerFrame)
    {
        pointerFrame = new PointerFrame();

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            pointerFrame.screenPosition = touch.position;
            pointerFrame.wasPressed = touch.phase == TouchPhase.Began;
            pointerFrame.wasReleased = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            pointerFrame.isDown = touch.phase == TouchPhase.Began ||
                                  touch.phase == TouchPhase.Moved ||
                                  touch.phase == TouchPhase.Stationary;
            return true;
        }

        pointerFrame.screenPosition = Input.mousePosition;
        pointerFrame.wasPressed = Input.GetMouseButtonDown(0);
        pointerFrame.wasReleased = Input.GetMouseButtonUp(0);
        pointerFrame.isDown = Input.GetMouseButton(0);

        return pointerFrame.wasPressed || pointerFrame.wasReleased || pointerFrame.isDown;
    }

    private void BeginDrag(Vector2 screenPosition)
    {
        Vector3 worldPosition = ScreenToWorld(screenPosition, 0);
        draggedCapsule = GetCapsuleAt(worldPosition);

        if (draggedCapsule == null)
        {
            return;
        }

        if (dragHand != null)
        {
            dragHand.SetActive(false);
        }

        if (dragHandTwo != null)
        {
            dragHandTwo.SetActive(false);
        }

        draggedCapsule.activeTween?.Kill();
        draggedCapsule.activeTween = null;
        dragZ = draggedCapsule.transform.position.z;
        Vector3 capsulePosition = draggedCapsule.transform.position;
        Vector3 pointerWorldPosition = ScreenToWorld(screenPosition, dragZ);
        dragOffset = capsulePosition - pointerWorldPosition + Vector3.up * dragTouchOffsetY;

        if (draggedCapsule.renderer != null)
        {
            draggedCapsule.renderer.sortingOrder = 100;
        }
    }

    private CapsuleState GetCapsuleAt(Vector3 worldPosition)
    {
        CapsuleState bestCapsule = null;
        int bestSortingOrder = int.MinValue;

        for (int i = capsuleStates.Count - 1; i >= 0; i--)
        {
            CapsuleState capsuleState = capsuleStates[i];
            if (capsuleState.isPlaced || capsuleState.renderer == null || !capsuleState.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!PointHitsRenderer(capsuleState.renderer, worldPosition))
            {
                continue;
            }

            if (capsuleState.renderer.sortingOrder >= bestSortingOrder)
            {
                bestCapsule = capsuleState;
                bestSortingOrder = capsuleState.renderer.sortingOrder;
            }
        }

        return bestCapsule;
    }

    private bool PointHitsRenderer(SpriteRenderer spriteRenderer, Vector3 worldPosition)
    {
        Bounds bounds = spriteRenderer.bounds;
        bounds.Expand(hitPadding);

        return worldPosition.x >= bounds.min.x &&
               worldPosition.x <= bounds.max.x &&
               worldPosition.y >= bounds.min.y &&
               worldPosition.y <= bounds.max.y;
    }

    private void DragCapsule(Vector2 screenPosition)
    {
        Vector3 worldPosition = ScreenToWorld(screenPosition, dragZ) + dragOffset;
        worldPosition.z = dragZ;
        draggedCapsule.transform.position = worldPosition;
    }

    private void EndDrag()
    {
        CapsuleState releasedCapsule = draggedCapsule;
        draggedCapsule = null;

        BeakerTarget hitBeaker = GetHitBeaker(releasedCapsule);
        if (hitBeaker == null || hitBeaker.capsuleCount >= capsulesPerBeaker)
        {
            ReturnCapsuleToStart(releasedCapsule);
            return;
        }

        MoveCapsuleIntoBeaker(releasedCapsule, hitBeaker);
    }

    private BeakerTarget GetHitBeaker(CapsuleState capsuleState)
    {
        bool hitsBeakerOne = CapsuleHitsBeaker(capsuleState, beakerOneTarget);
        bool hitsBeakerTwo = CapsuleHitsBeaker(capsuleState, beakerTwoTarget);

        if (hitsBeakerOne && hitsBeakerTwo)
        {
            float distanceToOne = Vector3.Distance(capsuleState.transform.position, beakerOneTarget.renderer.bounds.center);
            float distanceToTwo = Vector3.Distance(capsuleState.transform.position, beakerTwoTarget.renderer.bounds.center);
            return distanceToOne <= distanceToTwo ? beakerOneTarget : beakerTwoTarget;
        }

        if (hitsBeakerOne)
        {
            return beakerOneTarget;
        }

        return hitsBeakerTwo ? beakerTwoTarget : null;
    }

    private bool CapsuleHitsBeaker(CapsuleState capsuleState, BeakerTarget beakerTarget)
    {
        if (capsuleState.renderer == null || beakerTarget == null || beakerTarget.renderer == null)
        {
            return false;
        }

        Bounds capsuleBounds = capsuleState.renderer.bounds;
        Bounds beakerBounds = beakerTarget.renderer.bounds;
        capsuleBounds.Expand(hitPadding);
        beakerBounds.Expand(hitPadding);

        return capsuleBounds.Intersects(beakerBounds);
    }

    private void ReturnCapsuleToStart(CapsuleState capsuleState)
    {
        capsuleState.activeTween?.Kill();
        PlaySfx("SlideBack");
        capsuleState.activeTween = capsuleState.transform
            .DOLocalMove(capsuleState.originalLocalPosition, returnDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                capsuleState.transform.localRotation = capsuleState.originalLocalRotation;
                capsuleState.transform.localScale = capsuleState.originalLocalScale;

                if (capsuleState.renderer != null)
                {
                    capsuleState.renderer.sortingOrder = capsuleState.originalSortingOrder;
                }

                capsuleState.activeTween = null;
            });
    }

    private void MoveCapsuleIntoBeaker(CapsuleState capsuleState, BeakerTarget beakerTarget)
    {
        if (capsuleState.transform != null)
        {
            capsuleState.transform.rotation = Quaternion.Euler(0, 0, rotationZ);
        }

        int slotIndex = beakerTarget.capsuleCount;
        beakerTarget.capsuleCount++;
        capsuleState.isPlaced = true;

        Vector3 targetPosition = GetBeakerSlotPosition(beakerTarget, slotIndex, capsuleState.transform.position.z);

        capsuleState.activeTween?.Kill();
        if (beakerTarget.targetPosY != null)
        {
            MoveCapsuleThroughTargetY(capsuleState, beakerTarget, targetPosition);
            return;
        }

        SetCapsuleSortingOrder(capsuleState);
        Vector3[] path = BuildBeakerPath(capsuleState.transform.position, targetPosition, beakerTarget);
        capsuleState.activeTween = capsuleState.transform
            .DOPath(path, beakerPathDuration, PathType.CatmullRom, PathMode.Full3D, 12)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                CompleteCapsulePlacement(capsuleState, targetPosition, beakerTarget);
            });
    }

    private void MoveCapsuleThroughTargetY(CapsuleState capsuleState, BeakerTarget beakerTarget, Vector3 targetPosition)
    {
        Vector3 targetYPosition = new Vector3(
            targetPosition.x,
            beakerTarget.targetPosY.position.y,
            targetPosition.z);
        float segmentDuration = beakerPathDuration * 0.5f;

        capsuleState.activeTween = DOTween.Sequence()
            .Append(capsuleState.transform
                .DOMove(targetYPosition, segmentDuration)
                .SetEase(Ease.InOutSine))
            .AppendCallback(() =>
            {
                SetCapsuleSortingOrder(capsuleState);
                PlaySfx("MedicineDrop");
            })
            .Append(capsuleState.transform
                .DOMove(targetPosition, segmentDuration)
                .SetEase(Ease.InOutSine))
            .OnComplete(() =>
            {
                CompleteCapsulePlacement(capsuleState, targetPosition, beakerTarget);
            });
    }

    private void SetCapsuleSortingOrder(CapsuleState capsuleState)
    {
        if (capsuleState.renderer != null)
        {
            capsuleState.renderer.sortingOrder = sortingOrder;
        }
    }

    private void CompleteCapsulePlacement(CapsuleState capsuleState, Vector3 targetPosition, BeakerTarget beakerTarget)
    {
        capsuleState.transform.position = targetPosition;
        if (beakerTarget.placedParent != null)
        {
            capsuleState.transform.SetParent(beakerTarget.placedParent, true);
        }

        capsuleState.activeTween = null;

        // Once the first beaker is full, prompt the player to fill the second one.
        if (beakerTarget == beakerOneTarget &&
            beakerTarget.capsuleCount >= capsulesPerBeaker &&
            dragHandTwo != null)
        {
            dragHandTwo.SetActive(true);
        }

        CheckCompletion();
    }

    private void PlaySfx(string clipName)
    {
        if (AudioHandler.instance != null)
        {
            AudioHandler.instance.PlaySFX(clipName);
        }
    }

    private Vector3 GetBeakerSlotPosition(BeakerTarget beakerTarget, int slotIndex, float zPosition)
    {
        Transform endPosition = GetBeakerEndPosition(beakerTarget, slotIndex);
        if (endPosition != null)
        {
            Vector3 targetPosition = endPosition.position;
            targetPosition.z = zPosition;
            return targetPosition;
        }

        Bounds bounds = beakerTarget.renderer.bounds;
        int centeredSlotIndex = slotIndex - (capsulesPerBeaker - 1) / 2;
        float x = bounds.center.x + centeredSlotIndex * bounds.extents.x * beakerSlotHorizontalSpacing;
        float y = bounds.min.y + bounds.size.y * beakerSlotVerticalPosition;

        return new Vector3(x, y, zPosition);
    }

    private Transform GetBeakerEndPosition(BeakerTarget beakerTarget, int slotIndex)
    {
        if (beakerTarget.endPositions == null ||
            slotIndex < 0 ||
            slotIndex >= beakerTarget.endPositions.Count)
        {
            return null;
        }

        return beakerTarget.endPositions[slotIndex];
    }

    private Vector3[] BuildBeakerPath(Vector3 startPosition, Vector3 targetPosition, BeakerTarget beakerTarget)
    {
        if (beakerTarget.targetPosY != null)
        {
            Vector3 alignPosition = new Vector3(targetPosition.x, beakerTarget.targetPosY.position.y, targetPosition.z);
            return new[]
            {
                alignPosition,
                targetPosition
            };
        }

        Vector3 beakerCenter = beakerTarget.renderer.bounds.center;
        float liftedY = Mathf.Max(startPosition.y, targetPosition.y) + beakerPathLift;
        Vector3 firstControlPoint = new Vector3(startPosition.x, liftedY, startPosition.z);
        Vector3 secondControlPoint = new Vector3(beakerCenter.x, liftedY, targetPosition.z);

        return new[]
        {
            firstControlPoint,
            secondControlPoint,
            targetPosition
        };
    }

    private void CheckCompletion()
    {
        if (IsAtCompletedStep || beakerOneTarget == null || beakerTwoTarget == null)
        {
            return;
        }

        if (beakerOneTarget.capsuleCount < capsulesPerBeaker || beakerTwoTarget.capsuleCount < capsulesPerBeaker)
        {
            return;
        }

        // Counts reserve slots when a drag starts; wait until every capsule has actually landed.
        foreach (CapsuleState capsule in capsuleStates)
        {
            if (!capsule.isPlaced || capsule.activeTween != null) return;
        }

        BeginCompletedStep();
    }

    private void BeginCompletedStep()
    {
        if (IsAtCompletedStep) return;
        IsAtCompletedStep = true;
        sr1.DOKill();
        sr2.DOKill();
        sr1.DOFade(0, 0.5f);
        sr2.DOFade(0, 0.5f);
        dragEnabled = false;
        dragHand.SetActive(false);
        ResetWriters(waveTypewriters);
        StartCoroutine(ShowWaveTypeWriters2());
    }

    private Vector3 ScreenToWorld(Vector2 screenPosition, float zPosition)
    {
        if (dragCamera == null)
        {
            dragCamera = Camera.main;
        }

        if (dragCamera == null)
        {
            return new Vector3(screenPosition.x, screenPosition.y, zPosition);
        }

        float distanceFromCamera = Mathf.Abs(zPosition - dragCamera.transform.position.z);
        Vector3 worldPosition = dragCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distanceFromCamera));
        worldPosition.z = zPosition;
        return worldPosition;
    }
}
