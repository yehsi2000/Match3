using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreBoard : MonoBehaviour
{
    TMP_Text scoreText;

    private void Start() {
        scoreText = GetComponent<TMP_Text>();
    }

    public void UpdateScore(int score)
    {
        scoreText.text = "Score : " + score;
    }
}
