using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    public TMP_Text timerText;
    public static Timer instance;

    private void Awake() {
        instance = this;
    }

    private void Start() {
        timerText = GetComponent<TMP_Text>();
    }
}
