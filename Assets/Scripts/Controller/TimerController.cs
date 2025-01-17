using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimerController : MonoBehaviour
{
    public void TimerTick() {
        if (!Timer.instance.UpdateTimer()) {
            GameManager.instance.GameOver();
        }
    }
}
