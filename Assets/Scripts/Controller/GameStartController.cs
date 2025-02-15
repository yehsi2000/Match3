using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartController : MonoBehaviour {
    [SerializeField]
    AudioController audiocontroller;
    public int bgmIndex;
    private void Start() {
        audiocontroller.PlayBGM();
    }
    public void StartGame() {
        SceneManager.LoadScene("ModeSelectionScene");
    }
}
