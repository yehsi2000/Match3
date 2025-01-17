using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using KaimiraGames;
using UnityEditor;

[InitializeOnLoad]
public class MultiGameController : MonoBehaviour {

    [SerializeField]
    GameManager game;


    public static readonly int SPECIALBLOCK = 100;
    //public ArrayLayout boardLayout;
    public static bool isClickable = true;

    [Header("UI Elements")]


    public GameObject gameEndScreen;
    public GameObject bgImageObject;
    public TMP_Text finalScore;
    public ComboDisplay comboDisplay;

    [Header("Prefabs")]


    [Header("Score")]
    public int autoBlockWeightMultiplier = 100;
    public int perPieceScore = 5;
    public int match4ExtraScore = 20;
    public int match5ExtraScore = 50;
    public int match6plusExtraScore = 100;



    [Header("Time")]
    [SerializeField]
    float clickStopInterval = 0.5f;
    float clickableTime = 0f;

    public float ClickStopInterval {
        get { return clickStopInterval; }
    }

    [SerializeField]
    float comboRetainInterval = 1.5f;

    int width;
    int height;

    [HideInInspector]
    public int combo = 0;

    float comboTime = 0f;

    System.Random random;

    public System.Random Random {
        get { return random; }
    }

    void Awake() {
        game = GetComponent<GameManager>();
    }

    public void Reset() {
        StartGame();
        gameEndScreen.SetActive(false);
    }

    void Start() {
        StartGame();
        gameEndScreen.SetActive(false);
    }

    void Update() {
        //update timer
        game.timerController.TimerTick();

        //prevent clicking while special block popping
        if (comboTime <= 0) combo = 0;
        else comboTime -= Time.deltaTime;

         //TODO
         //for(int i = 0; i < boards; i++) {
         //   game.boardController.boardUpdate(boards.manager);
         //}

        PreventClick();
    }

    /// <summary>
    /// Initialize game variable and set components
    /// </summary>
    public void StartGame() {
        string seed = getRandomSeed();
        random = new System.Random(seed.GetHashCode());


        width = game.boardManager.Width;
        height = game.boardManager.Height;
        if (width == 0 || height == 0) {
            throw new System.Exception("Board size is not set");
        }

        game.scoreManager.Initialize();
        comboDisplay.Initialize(comboRetainInterval);
        Timer.instance.StartTimer();

        game.audioController.PlayBGM();

        //gameBoard.GetComponent<RectTransform>().sizeDelta = new Vector2(nodeSize*width, nodeSize*height);
        //killedBoard.GetComponent<RectTransform>().sizeDelta = new Vector2(nodeSize*width, nodeSize*height);

        // set sprite image background to camera size, currently not used
        SpriteRenderer bgSpriteRenderer = bgImageObject.GetComponent<SpriteRenderer>();

        float _width = bgSpriteRenderer.bounds.size.x;
        float _height = bgSpriteRenderer.bounds.size.y;

        // set background image to gameboard size
        bgImageObject.transform.localScale = new Vector3(game.boardManager.NodeSize * (width + 1)
            / _width, game.boardManager.NodeSize
            * (height + 1) / _height, 1);

        //loop to get non-matching board
        //game.boardController.InitBoard();
    }

    void PreventClick() {
        if (clickableTime <= 0) isClickable = true;
        else clickableTime -= Time.deltaTime;
    }

    /// <summary>
    /// Increment Combo and perform related jobs like sfx, score, combo
    /// </summary>

    public void Matched() {
        combo++;
        comboTime = comboRetainInterval;
        comboDisplay.UpdateCombo(combo);

        if (combo % 5 == 0 && combo > 0) {
            game.audioController.PlayComboAudio(combo);
        }
        game.scoreManager.AddScore(Math.Clamp((combo / 5), 0, 6) * perPieceScore);
        game.audioController.PlayBlockPopAudio();
    }

    public void BacktoTitle() {
        SceneManager.LoadScene("StartScene");
    }

    public void SpecialBlockPressed() {
        isClickable = false;
        clickableTime = ClickStopInterval;
        Matched();
    }

    string getRandomSeed() {
        string seed = "";
        string acceptableChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz123456789!@#$%^&*()";
        for (int i = 0; i < 20; i++)
            seed += acceptableChars[UnityEngine.Random.Range(0, acceptableChars.Length)];
        return seed;
    }

    public void GameOver() {
        //game.boardController.DisableBoards();
        gameEndScreen.SetActive(true);

        game.audioController.Stop();

        finalScore.text = "Final Score : " + game.scoreManager.Score;
        this.enabled = false;
    }
}
