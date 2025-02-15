using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreBoard : MonoBehaviour
{
    public TMP_Text scoreText;

    private void Start() {
        
    }

    public void UpdateScore(int score)
    {
        scoreText.text = "Score : " + score;
    }
}
