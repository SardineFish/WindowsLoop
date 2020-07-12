using UnityEngine;
using System.Collections;
using System.Linq;

public class AudioManager : Singleton<AudioManager>
{
    public AudioClip BGM;
    public AudioClip FootstepSFX;
    public AudioClip JumpSFX;
    public AudioClip LandSFX;

    public bool IsAudioHost => System.Environment.GetCommandLineArgs().Any(arg => arg == "-audiohost");

    public AudioSource WalkAudioSource;
    public AudioSource SFXAudioSource;
    public AudioSource BGMAudioSource;

    bool walking = false;

    private void Awake()
    {
        System.Environment.GetCommandLineArgs().ForEach(t => Debug.Log(t));
    }

    // Use this for initialization
    void Start()
    {
        if(IsAudioHost)
        {
            BGMAudioSource.clip = BGM;
            BGMAudioSource.Play();

        }
        else
        {
            WalkAudioSource.clip = FootstepSFX;
            WalkAudioSource.volume = 0;
            WalkAudioSource.Play();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    IEnumerator Mute(AudioSource source, float time)
    {
        foreach (var t in Utility.TimerNormalized(time))
        {
            source.volume = Mathf.Lerp(source.volume, 0, t);
            yield return null;
        }
    }
    IEnumerator Inmute(AudioSource source, float time)
    {
        foreach(var t in Utility.TimerNormalized(time))
        {
            source.volume = Mathf.Lerp(source.volume, 1, t);
            yield return null;
        }
    }

    public void Walking(bool isWalking)
    {
        if(isWalking && !walking)
        {
            StopAllCoroutines();
            StartCoroutine(Inmute(WalkAudioSource, 0.1f));
        }
        else if(!isWalking && walking)
        {
            StopAllCoroutines();
            StartCoroutine(Mute(WalkAudioSource, 0.1f));
        }
        walking = isWalking;
    }
    public void Jump()
    {
        SFXAudioSource.PlayOneShot(JumpSFX);
    }

    public void Land()
    {
        SFXAudioSource.PlayOneShot(LandSFX);
    }
}
