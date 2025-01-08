using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public ScoreBoard scoreboard;

    int score;
    int targetscore;
    float t;

    public int Score {
        get {
            return targetscore;
        }
    }

    private void Update() {
        if (score < targetscore) {
            score = (int)Mathf.Lerp(score, targetscore, t);
            t += Time.deltaTime;
            if (t > 1) {
                score = targetscore;
                t = 0;
            }
        }
        scoreboard.UpdateScore(score);
    }

    public void Initialize() {
        score = 0;
        targetscore = 0;
    }

    public void AddScore(int addscore) {
        targetscore += addscore;
    }
}
