using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreBoard : MonoBehaviour
{
    public TMP_Text textMeshPro;

    public void UpdateScore(int score)
    {
        textMeshPro.text = "Score : " + score;
    }
}
