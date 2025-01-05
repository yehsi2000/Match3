using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using System;

public class ScoreBoard : MonoBehaviour
{
    TMP_Text scoreText;
    int score = 0;
    int targetscore = 0;
    public static ScoreBoard instance;
    float t = 0;

    private void Awake() {
        instance = this;
    }
    private void Start() {
        scoreText = GetComponent<TMP_Text>();
    }

    public void UpdateScore(int score)
    {
        targetscore = score;
    }

    private void Update() {
        if(score < targetscore) {
            score = (int)Mathf.Lerp(score, targetscore, t);
            t += Time.deltaTime;
            if (t > 1) {
                score = targetscore;
                t = 0;
            }
        }
        scoreText.text = "Score : " + score;
    }
}
