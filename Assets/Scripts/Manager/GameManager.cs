using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
    public static GameManager instance;

    [SerializeField]
    GameControllerBase gameController;

    private void Awake() {
        if (instance == null) {
            instance = this;
        }
        else {
            Destroy(gameObject);
        }
    }

    private bool isClickable = true;

    public bool IsClickable {
        get { return isClickable; }
        set { isClickable = value; }
    }

    public void GameOver() {
        gameController.GameOver();
    }

}
