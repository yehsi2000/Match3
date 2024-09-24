using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using JetBrains.Annotations;


public class Timer : MonoBehaviour
{
    TMP_Text timerText;
    float timerTime;
    public float TIMELEFT = 120f;

    private void Start() {
        timerText = GetComponent<TMP_Text>();
    }

    public void StartTimer()
    {
        timerTime = Time.time;
    }

    public bool UpdateTimer()
    {
        float timeleft = TIMELEFT + timerTime - Time.time;
        timerText.text = Mathf.Floor(timeleft / 60) + ":" + Mathf.RoundToInt(timeleft) % 60;
        //Debug.Log(Time.time);
        if (timeleft <= 0) return false;
        return true;
    }
}
