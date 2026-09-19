using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SlideThree : MonoBehaviour
{
    public GameObject title,discOne,discTwo,discThree,medicine,india;
    public Vector3 titleInitialPosition,discOneInitialPosition,discTwoInitialPosition,discThreeInitialPosition,indiaInitialPosition;
    public Vector3 titleTargetPosition,discOneTargetPosition,discTwoTargetPosition,discThreeTargetPosition,indiaTargetPosition;
      public VideoPlayerDisplay videoPlayerDisplay;
    void OnEnable()
    {
        title.SetActive(false);
        discOne.SetActive(false);
        discTwo.SetActive(false);
        discThree.SetActive(false);
        medicine.SetActive(false);
        india.SetActive(false);
        title.transform.localPosition = titleInitialPosition;
        discOne.transform.localPosition = discOneInitialPosition;
        discTwo.transform.localPosition = discTwoInitialPosition;
        discThree.transform.localPosition = discThreeInitialPosition;
        india.transform.localPosition = indiaInitialPosition;

        StartCoroutine(PlaySlideThreeSequence());
        StartCoroutine(PlayVideo());
    }
    IEnumerator PlayVideo()
    {
        videoPlayerDisplay.PlayVideo();
        
        yield return new WaitForSeconds(0.1f);
    }
    private IEnumerator PlaySlideThreeSequence()
    {
        yield return new WaitForSeconds(0.25f);
       // AudioHandler.instance.PlayVO("OstocalciumCCMIndia");
       // title.SetActive(true);
        //title.transform.DOLocalMove(titleTargetPosition, 0.5f).SetEase(Ease.OutBack).From(titleInitialPosition);
        yield return new WaitForSeconds(  0.25f);

      //  medicine.SetActive(true);
      //  medicine.transform.DOScale(Vector3.one * 0.5f, 0.5f).SetEase(Ease.OutBack).From(Vector3.zero);

      //  discOne.SetActive(true);
      //  discOne.transform.DOLocalMove(discOneTargetPosition, 0.5f).SetEase(Ease.OutBack).From(discOneInitialPosition);
        yield return new WaitForSeconds(0.25f);
      //  discTwo.SetActive(true);
      //  discTwo.transform.DOLocalMove(discTwoTargetPosition, 0.5f).SetEase(Ease.OutBack).From(discTwoInitialPosition);
        yield return new WaitForSeconds(0.25f);
      // discThree.SetActive(true);
       // discThree.transform.DOLocalMove(discThreeTargetPosition, 0.5f).SetEase(Ease.OutBack).From(discThreeInitialPosition);
        yield return new WaitForSeconds(0.25f);
      //  india.SetActive(true);
      //  india.transform.DOLocalMove(indiaTargetPosition, 0.5f).SetEase(Ease.OutBack).From(indiaInitialPosition);
        yield return new WaitForSeconds(4.75f);
        //AudioHandler.instance.PlayVO("WithFaster");
    }
}
