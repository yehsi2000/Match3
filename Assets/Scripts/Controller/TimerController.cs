using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TimerController : MonoBehaviour
{
    float startTime;
    public float initTime = 120f;
    float timeleft;

    public void StartTimer() {
        timeleft = initTime;
        startTime = Time.time;
        StartCoroutine(TimerCoroutine());
    }

    IEnumerator TimerCoroutine() {
        while (timeleft > 0) {
            timeleft = Mathf.Floor(this.initTime + startTime - Time.time);
            string minutes = Mathf.Floor(timeleft / 60).ToString("00");
            string seconds = (timeleft % 60).ToString("00");
            Timer.instance.timerText.text = $"{minutes}:{seconds}";
            yield return null;

            if (timeleft <= 0) {
                GameManager.instance.GameOver();
            }
        }
    }

    bool UpdateTimer() {
        float timeleft = Mathf.Floor(initTime + startTime - Time.time);
        Timer.instance.timerText.text = Mathf.Floor(timeleft / 60) + ":" + Mathf.RoundToInt(timeleft) % 60;
        //Debug.Log(Time.time);
        if (timeleft <= 0) return false;
        return true;
    }
}
