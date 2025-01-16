using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public TimerController timerController;
    public PieceController pieceController;
    public AudioController audioController;
    public BoardController boardController;
    public ParticleController particleController;
    public GameController gameController;

    public Board boardManager;
    public ScoreManager scoreManager;

    private void Awake() {
        gameController = GetComponent<GameController>();
        boardManager = GetComponent<Board>();
        scoreManager = GetComponent<ScoreManager>();
    }

    
}
