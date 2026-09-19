using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideVideoDissolve : MonoBehaviour
{
    public VideoPlayerDisplay videoPlayerDisplay;
    void OnEnable()
    {
        StartCoroutine(PlayVideo());
    }
    IEnumerator PlayVideo()
    {
        videoPlayerDisplay.PlayVideo();
        
        yield return new WaitForSeconds(0.1f);
    }
}
