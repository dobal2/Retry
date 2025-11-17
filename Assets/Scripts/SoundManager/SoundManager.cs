using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    
    

    void Awake()
    {
        Init();
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    
    [Header("#BGM")]
    public AudioSource bgmPlayer;
    public AudioClip[] bgmClips;
    public float bgmVolume;
    private int _bgmChannelIndex;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;

    
    [Header("#SFX")]
    public AudioSource[] sfxPlayers;
    public AudioClip[] sfxClips;
    public int sfxChannelCount;
    public float sfxVolume;
    private int _sfxChannelIndex;
    
    // ✅ 시계 소리 전용 AudioSource 추가
    [Header("#Clock Sound")]
    private AudioSource clockSoundPlayer;
    
    public enum Bgm{MenuBgm,MainBGM,ClearBGM}
    public enum Sfx{TimeStop,ClockSound,EnergyCollect,ProjectileHit,OpenChest}

    void Init()
    {
        GameObject bgmObject = new GameObject("BgmPlayer");
        bgmObject.transform.parent = transform;
        bgmPlayer = bgmObject.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop = true;
        bgmPlayer.volume = bgmVolume;
        bgmPlayer.outputAudioMixerGroup = bgmMixerGroup;

        GameObject sfxObject = new GameObject("SfxPlayerGroup");
        sfxObject.transform.parent = transform;
        sfxPlayers = new AudioSource[sfxChannelCount];

        for (int i = 0; i < sfxPlayers.Length;i++)
        {
            sfxPlayers[i] = sfxObject.AddComponent<AudioSource>();
            sfxPlayers[i].playOnAwake = false;
            sfxPlayers[i].volume = sfxVolume;
        }

        // ✅ 시계 소리 전용 AudioSource 초기화
        GameObject clockObject = new GameObject("ClockSoundPlayer");
        clockObject.transform.parent = transform;
        clockSoundPlayer = clockObject.AddComponent<AudioSource>();
        clockSoundPlayer.playOnAwake = false;
        clockSoundPlayer.volume = sfxVolume;
        
        PlayBgm(Bgm.MenuBgm);
    }
    

    public void PlayBgm(Bgm bgm)
    {
        bgmPlayer.clip = bgmClips[(int)bgm];
        bgmPlayer.Play();
    }
    
    public void PlaySfx(Sfx sfx)
    {
        // ✅ ClockSound는 전용 플레이어 사용
        if (sfx == Sfx.ClockSound)
        {
            PlayClockSound();
            return;
        }

        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            int loopIndex = (i + _sfxChannelIndex) % sfxPlayers.Length;

            if (sfxPlayers[loopIndex].isPlaying)
            {
                continue;
            }

            _sfxChannelIndex = loopIndex;
            sfxPlayers[loopIndex].clip = sfxClips[(int)sfx];
            sfxPlayers[loopIndex].Play();
            break;
        }
    }

    // ✅ 시계 소리 재생 (겹치지 않음)
    private void PlayClockSound()
    {
        if (clockSoundPlayer.isPlaying)
        {
            return; // 이미 재생 중이면 무시
        }

        clockSoundPlayer.clip = sfxClips[(int)Sfx.ClockSound];
        clockSoundPlayer.Play();
    }

    // ✅ 시계 소리 중단
    public void StopClockSound()
    {
        if (clockSoundPlayer != null && clockSoundPlayer.isPlaying)
        {
            clockSoundPlayer.Stop();
            clockSoundPlayer.clip = null;
        }
    }

    // ✅ 특정 Sfx를 중단하는 메서드
    public void StopSfx(Sfx sfx)
    {
        if (sfx == Sfx.ClockSound)
        {
            StopClockSound();
            return;
        }

        AudioClip targetClip = sfxClips[(int)sfx];
        
        foreach (var sfxPlayer in sfxPlayers)
        {
            if (sfxPlayer.isPlaying && sfxPlayer.clip == targetClip)
            {
                sfxPlayer.Stop();
                sfxPlayer.clip = null;
            }
        }
    }

    // ✅ 모든 ClockSound를 즉시 중단
    public void StopAllClockSounds()
    {
        StopClockSound();
    }

    public void PlayBgm()
    {
        bgmPlayer.Play();
    }

    public void StopBgm()
    {
        bgmPlayer.Stop();
    }
    
    public void PlaySfx()
    {
        foreach (var sfxPlayer in sfxPlayers)
        {
            sfxPlayer.Play();
            sfxPlayer.volume = sfxVolume;
        }
    }
    
    public void StopSfx()
    {
        foreach (var sfxPlayer in sfxPlayers)
        {
            sfxPlayer.Stop();
            sfxPlayer.clip = null;
            sfxPlayer.volume = 0;
        }
    }
    
    public void ChangeBgm(bool isOn)
    {
        if (isOn)
        {
            PlayBgm();
        }
        else
        {
            StopBgm();
        }
    }
    
    public void ChangeSfx(bool isOn)
    {
        Debug.Log(isOn);
        if (isOn)
        {
            PlaySfx();
        }
        else
        {
            StopSfx();
        }
    }
}