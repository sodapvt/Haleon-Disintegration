using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class AudioHandler : MonoBehaviour
{
    internal static AudioHandler instance;
    [SerializeField] private AudioSource sfxAudioSource, voAudioSource,extraAudioSource,bgmAudioSource;
   [SerializeField] private AudioClip AsPerIndian,Disintegration,filmCoated,OstocalciumCCMTablets,
   OstocalciumCCMIndia,withFaster,dragDrop,StartAnalysis,StartTest;
    [SerializeField]private AudioClip click,pop,pop2,slide,select,chime,medicineDrop,SlideBack;

public Image musicImage;
public Sprite musicOnSprite,musicOffSprite;
     int shakingIndex = 0;
    void Start()
    {
        instance = this;
    }
    public float PlaySFX(string clipName,int index=0)
    {

         AudioClip clip = null;
        switch (clipName)
        {
            case "Click":
                clip = click;
                break;
            case "Pop":
                clip = pop;
                break;
            case "Pop2":
                clip = pop2;
                break;
            case "Slide":
                clip = slide;
                break;
            case "Select":
                clip = select;
                break;
            case "Chime":
            clip=chime;
            break;
            case "MedicineDrop":
            clip=medicineDrop;
            break;
            case "SlideBack":
            clip=SlideBack;
            break;
            // Add more cases for other SFX clips as needed
            default:
                Debug.LogWarning("Unknown SFX clip name: " + clipName);
                return 0f;
        }
         sfxAudioSource.PlayOneShot(clip);
        return clip.length;
    }
    public void PlayOneShotSFX(string clipName)
    {
        AudioClip clip = null;
        switch (clipName)
        {
            default:
                Debug.LogWarning("Unknown SFX clip name: " + clipName);
                return;
        }
        extraAudioSource.PlayOneShot(clip);
    }

    public void StopSFX()
    {
        sfxAudioSource.Stop();
    }
    public float PlayVO(string clipName)
    {
        AudioClip clip = null;
        switch (clipName)
        {
            case "AsPerIndian":
                clip = AsPerIndian;
                break;
            case "Disintegration":
                clip = Disintegration;
                break;
            case "FilmCoated":
                clip = filmCoated;
                break;
            case "OstocalciumCCMTablets":
                clip = OstocalciumCCMTablets;
                break;
            case "OstocalciumCCMIndia":
                clip = OstocalciumCCMIndia;
                break;
            case "WithFaster":
                clip = withFaster;
                break;
            case "DragDrop":
                clip = dragDrop;
                break;
            case "StartAnalysis":
                clip = StartAnalysis;
                break;
            case "StartTest":
                clip = StartTest;
                break;

            // Add more cases for other VO clips as needed
            default:
                Debug.LogWarning("Unknown VO clip name: " + clipName);
                return 0f;
        }
        voAudioSource.clip = clip;
        voAudioSource.Play();
        return clip.length;
    }
    public void StopVO()
    {
        voAudioSource.Stop();
    }
    public bool IsVOPlaying()
    {
        return voAudioSource.isPlaying;
    }
    public bool IsSFXPlaying()
    {
        return sfxAudioSource.isPlaying;
    }
    public void PlayAppreciationVO()
    {
        // int randomIndex = Random.Range(0, appreciationVOs.Length);
        // voAudioSource.clip = appreciationVOs[randomIndex];
        // voAudioSource.Play();
    }
    bool isMuted = false;
    public void ToggleBGM()
    {
        isMuted = !isMuted;
        bgmAudioSource.mute = isMuted;
        if (isMuted)
        {
            musicImage.sprite = musicOffSprite;
        }
        else
        {
            musicImage.sprite = musicOnSprite;
        }
    }
}
