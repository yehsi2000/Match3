using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public ScoreBoard scoreboard;
    public ScoreBoard opScoreboard;

    int score;
    int targetscore;
    int opScore;
    int opTargetscore;
    float t;
    float opT;

    public int Score {
        get {
            return targetscore;
        }
    }

    public int OpScore {
        get {
            return opTargetscore;
        }
    }

    public static ScoreManager instance;

    private void Awake() {
        instance = this;
    }

    private void Update() {
        if (score < targetscore) {
            score = (int)Mathf.Lerp(score, targetscore, t);
            t += Time.deltaTime;
            if (t > 1) {
                score = targetscore;
                t = 0;
            }
            scoreboard.UpdateScore(score);
        }
        //if (Network.instance == null) Debug.Log("null network!");
        //else Debug.Log($"opScore={opScore}, opTargetscore={opTargetscore}");
        if (Network.instance != null && opScore < opTargetscore) {
            opScore = (int)Mathf.Lerp(opScore, opTargetscore, opT);
            opT += Time.deltaTime;
            if (opT > 1) {
                opScore = opTargetscore;
                opT = 0;
            }
            opScoreboard.UpdateScore(opScore);
        }
        

    }

    public void Initialize() {
        score = 0;
        targetscore = 0;
        opScore = 0;
        opTargetscore = 0;
        if (scoreboard != null) scoreboard.UpdateScore(score);
        if (opScoreboard !=null) opScoreboard.UpdateScore(score);
    }

    public void AddScore(int addscore) {
        targetscore += addscore;
        if (Network.instance != null) {
            Network.instance.SendScore(targetscore);
        }
    }

    public void SetOpScore(int addscore) {
        //Debug.Log("Set Opponent Score " + addscore);
        opTargetscore = addscore;
    }
}
