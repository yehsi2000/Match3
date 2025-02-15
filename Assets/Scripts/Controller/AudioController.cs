using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    [SerializeField]
    private AudioSource bgm;

    [SerializeField]
    private AudioSource comboAudio;

    [SerializeField]
    private AudioSource blockPopAudio;

    [SerializeField]
    public AudioClip[] blockPopAudioClips;

    [SerializeField]
    public AudioClip[] bgmAudioClips;

    [SerializeField]
    public AudioClip[] comboAudioClips;

    private int bgmIndex;

    public void PlayBGM() {
        if (bgm == null) return;
        bgm.clip = bgmAudioClips[PlayerPrefs.GetInt("bgm") % bgmAudioClips.Length];
        bgm.Play();
    }

    public void PlayComboAudio(int combo) {
        if (comboAudio == null) return;
        int combosfxindex = Math.Clamp((combo / 5) - 1, 0, comboAudioClips.Length - 1);
        comboAudio.clip = comboAudioClips[combosfxindex];
        comboAudio.Play();
    }

    public void PlayBlockPopAudio() {
        if (blockPopAudio == null) return;
        blockPopAudio.clip =  blockPopAudioClips[UnityEngine.Random.Range(0, blockPopAudioClips.Length - 1)];
        blockPopAudio.Play();
    }

    void Start() {
        if (comboAudio != null) comboAudio.volume = PlayerPrefs.GetFloat("sfx_volume", 1);
        if (blockPopAudio != null) blockPopAudio.volume = PlayerPrefs.GetFloat("sfx_volume", 1);

        if (!PlayerPrefs.HasKey("bgm")) PlayerPrefs.SetInt("bgm", 0);
        bgmIndex = PlayerPrefs.GetInt("bgm");
        if (bgm != null) bgm.volume = PlayerPrefs.GetFloat("volume", 1);
    }

    public void SetVolume(float volume) {
        if (bgm != null) bgm.volume = volume;
        PlayerPrefs.SetFloat("volume", volume);
    }

    public void SetSFXVolume(float volume) {
        comboAudio.volume = volume;
        blockPopAudio.volume = volume;
        PlayerPrefs.SetFloat("sfx_volume", volume);
    }

    public void Stop() {
        bgm.Stop();
    }

    public void NextBGM() {
        bgmIndex++;
        bgmIndex %= bgmAudioClips.Length;
        PlayerPrefs.SetInt("bgm", bgmIndex);
        bgm.clip = bgmAudioClips[bgmIndex % bgmAudioClips.Length];
        bgm.Play();
    }
}
