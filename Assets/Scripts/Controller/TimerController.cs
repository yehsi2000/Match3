using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UIElements;

public class TimerController : MonoBehaviour
{
    double timeleft;
    public void TimerTick() {
        if (!Timer.instance.UpdateTimer()) {
            GameManager.Instance.gameController.GameOver();
        }
    }
}
