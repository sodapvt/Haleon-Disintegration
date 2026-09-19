using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SlideOne : MonoBehaviour
{
    [SerializeField] private List<WaveTypeWriter> waveTypewriters;
    [Header("Start Button Pulse")]
    [SerializeField] private float startButtonPulseScale = 1.08f;
    [SerializeField] private float startButtonPulseDuration = 0.65f;
    public GameObject MedOne, MedTwo, MedThree, MedFour,arrowOne,arrowTwo,arrowThree,startButton;
    public GameObject holder,panel;
    private Tween startButtonTween;
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
            startButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            panel.transform.localScale=Vector3.one*0.75f;
            holder.transform.localScale=Vector3.one*0.75f;
        }
        else
        {
            panel.transform.localScale=Vector3.one;
            holder.transform.localScale=Vector3.one;
        }
    }
    void OnEnable()
    {
        StopStartButtonPulse(true);
        foreach (var writer in waveTypewriters)
        {
            writer.ResetText();
        }
        MedOne.SetActive(false);
        MedTwo.SetActive(false);
        MedThree.SetActive(false);
        MedFour.SetActive(false);
        arrowOne.SetActive(false);
        arrowTwo.SetActive(false);
        arrowThree.SetActive(false);
        startButton.SetActive(false);
        StartCoroutine(TypeText());
    }

    private void OnDisable()
    {
        StopStartButtonPulse(true);
        StopAllCoroutines();
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            child.DOKill();
        }
        foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>(true))
        {
            sprite.DOKill();
        }
        foreach (var writer in waveTypewriters)
        {
            writer.ResetText();
        }
    }

    private IEnumerator TypeText()
    {
        yield return new WaitForSeconds(0.25f);
        
       // SetHolderScale();
        AudioHandler.instance.PlayVO("Disintegration");
        StartCoroutine(ShowMedications());
        int counter = 0;
        foreach (var writer in waveTypewriters)
        {
            writer.PlayTextAnimation();
            yield return new WaitUntil(() => FlowManager.textWaveFinished);
            counter++;
        }
         yield return new WaitForSeconds(1.25f);
        AudioHandler.instance.PlaySFX("Pop2");
        AudioHandler.instance.PlayVO("StartAnalysis");
        startButton.SetActive(true);
        ShowStartButtonWithPulse();
    }
    private IEnumerator ShowMedications()
    {
        yield return new WaitForSeconds(2.0f);
        MedOne.SetActive(true);
        MedOne.transform.DOScale(Vector3.one*0.55f, 0.5f).SetEase(Ease.OutBack).From(Vector3.zero);
        yield return new WaitForSeconds(0.5f);
        arrowOne.SetActive(true);
        arrowOne.GetComponent<SpriteRenderer>().DOFade(1, 0.5f).From(0);
        yield return new WaitForSeconds(0.5f);
        MedTwo.SetActive(true);
        MedTwo.transform.DOScale(Vector3.one*0.55f, 0.5f).SetEase(Ease.OutBack).From(Vector3.zero);
        yield return new WaitForSeconds(0.5f);
        arrowTwo.SetActive(true);
        arrowTwo.GetComponent<SpriteRenderer>().DOFade(1, 0.5f).From(0);
        yield return new WaitForSeconds(0.5f);
        MedThree.SetActive(true);
        MedThree.transform.DOScale(Vector3.one*0.55f, 0.5f).SetEase(Ease.OutBack).From(Vector3.zero);
        yield return new WaitForSeconds(0.5f);
        arrowThree.SetActive(true);
        arrowThree.GetComponent<SpriteRenderer>().DOFade(1, 0.5f).From(0);
        yield return new WaitForSeconds(0.5f);
        MedFour.SetActive(true);
        MedFour.transform.DOScale(Vector3.one*0.55f, 0.5f).SetEase(Ease.OutBack).From(Vector3.zero);
        yield return new WaitForSeconds(0.5f);
    }

    public void OnStartButtonClicked()
    {
        StopStartButtonPulse(true);
        FlowManager.Instance.VibrateDevice();
         AudioHandler.instance.PlaySFX("Click");
        FlowManager.Instance.NextFlowObject();
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
        if (startButton == null || !startButton.activeInHierarchy)
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
}
