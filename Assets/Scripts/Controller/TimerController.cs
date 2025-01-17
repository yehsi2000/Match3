using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimerController : MonoBehaviour
{
    double timeleft;
    GameManager game;

    private void Awake() {
        game = GetComponent<GameManager>();
    }
    public void TimerTick() {
        if (!Timer.instance.UpdateTimer()) {
            game.gameController.GameOver();
        }
    }
}
