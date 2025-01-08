using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;

    public static GameManager Instance {
        get {
            return instance;
        }
    }

    public GameController gameController;

    public BoardManager boardManager;

    public ScoreManager scoreManager;

    private void Awake() {
        instance = this;
    }

    private void Start() {
        gameController = GetComponent<GameController>();
        boardManager = GetComponent<BoardManager>();
        scoreManager = GetComponent<ScoreManager>();
    }
}
