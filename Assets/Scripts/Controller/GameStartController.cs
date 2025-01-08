using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartController : MonoBehaviour
{
    AudioSource bgmAudio;
    public AudioClip[] bgmClips;
    public int bgmIndex;
    private void Start() {
        bgmAudio = GetComponent<AudioSource>();
        bgmIndex = PlayerPrefs.GetInt("bgm");
        bgmAudio.clip = bgmClips[bgmIndex % bgmClips.Length];
        bgmAudio.Play();
    }
    public void StartGame()
    {
        SceneManager.LoadScene("SingleGameScene");
    }

    public void NextBGM() {
        bgmIndex++;
        bgmIndex %= bgmClips.Length;
        PlayerPrefs.SetInt("bgm", bgmIndex);
        bgmAudio.clip = bgmClips[bgmIndex % bgmClips.Length];
        bgmAudio.Play();
    }
}
