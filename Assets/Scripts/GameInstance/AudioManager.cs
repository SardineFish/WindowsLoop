﻿using UnityEngine;
using System.Collections;
using System.Linq;

public class AudioManager : Singleton<AudioManager>
{
    public AudioClip BGM;
    public AudioClip FootstepSFX;
    public AudioClip JumpSFX;
    public AudioClip LandSFX;
    public AudioClip GemSFX;

    System.Lazy<bool> isAudioHost = new System.Lazy<bool>(() => System.Environment.GetCommandLineArgs().Any(arg => arg == "-audiohost"));

    public bool IsAudioHost => isAudioHost.Value;

    public AudioSource WalkAudioSource;
    public AudioSource SFXAudioSource;
    public AudioSource BGMAudioSource;

    bool walking = false;

    protected override void Awake()
    {
        base.Awake();

        // Run audio deamon in first instance.
        if (WindowSnap.SharedMemory.Others.Count == 0 && !Application.isEditor)
        {
            var path = System.Environment.GetCommandLineArgs()[0];
            System.Diagnostics.Process.Start(path, "-batchmode -nographics -audiohost");
        }
    }

    // Use this for initialization
    void Start()
    {
        if(IsAudioHost)
        {
            BGMAudioSource.clip = BGM;
            BGMAudioSource.loop = true;
            BGMAudioSource.Play();

        }
        else
        {
            WalkAudioSource.clip = FootstepSFX;
            WalkAudioSource.volume = 0;
            WalkAudioSource.Play();
        }
        if (IsAudioHost)
            SceneLoader.Instance.StartCoroutine(CloseCheck());
    }

    // Update is called once per frame
    void Update()
    {
    }
    IEnumerator CloseCheck()
    {
        while(true)
        {
            
            if (System.Diagnostics.Process.GetProcessesByName(System.Diagnostics.Process.GetCurrentProcess().ProcessName).Length <= 1)
                Application.Quit();
            yield return new WaitForSeconds(1);
        }
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

    public void GetGem()
    {
        SFXAudioSource.PlayOneShot(GemSFX);
    }
}
