using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using KaimiraGames;
using UnityEditor;

public class MultiGameController : GameControllerBase {

    [SerializeField]
    BoardController boardController;

    [SerializeField]
    ScoreManager scoreManager;

    [SerializeField]
    TimerController timerController;

    [SerializeField]
    AudioController audioController;

    [SerializeField]
    Board[] gameBoards;

    [Header("UI Elements")]
    public GameObject gameEndScreen;
    public GameObject bgImageObject;
    public TMP_Text finalScore;
    public ComboDisplay comboDisplay;

    [Header("Score")]
    public int autoBlockWeightMultiplier = 100;
    public int perPieceScore = 5;
    public int match4ExtraScore = 20;
    public int match5ExtraScore = 50;
    public int match6plusExtraScore = 100;

    public override int AutoBlockWeightMultiplier {
        get { return autoBlockWeightMultiplier; }
    }

    [Header("Time")]
    [SerializeField]
    float clickStopInterval = 0.5f;
    [SerializeField]
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
        timerController.TimerTick();

        //prevent clicking while special block popping
        if (comboTime <= 0) combo = 0;
        else comboTime -= Time.deltaTime;

        foreach (Board gameBoard in gameBoards) {
            boardController.boardUpdate(gameBoard);
        }
        PreventClick();
        if (Network.instance.isConnected) {
            if (Network.instance.packetQueue.Count > 0) {
                SwapPacket packet = Network.instance.packetQueue.Dequeue();
                ProcessFlip(gameBoards[1], packet);
            }
        }
    }

    /// <summary>
    /// Initialize game variable and set components
    /// </summary>
    public void StartGame() {
        string seed = getRandomSeed();
        for (int i = 0; i < gameBoards.Length; i++) {
            gameBoards[i].rng = new System.Random(seed.GetHashCode());
        }


        //width = gameBoard.Width;
        //height = gameBoard.Height;
        //if (width == 0 || height == 0) {
        //    throw new System.Exception("Board size is not set");
        //}

        scoreManager.Initialize();
        comboDisplay.Initialize(comboRetainInterval);
        Timer.instance.StartTimer();

        audioController.PlayBGM();

        //gameBoard.GetComponent<RectTransform>().sizeDelta = new Vector2(nodeSize*width, nodeSize*height);
        //killedBoard.GetComponent<RectTransform>().sizeDelta = new Vector2(nodeSize*width, nodeSize*height);

        // set sprite image background to camera size, currently not used
        SpriteRenderer bgSpriteRenderer = bgImageObject.GetComponent<SpriteRenderer>();

        float _width = bgSpriteRenderer.bounds.size.x;
        float _height = bgSpriteRenderer.bounds.size.y;

        // set background image to gameboard size
        //bgImageObject.transform.localScale = new Vector3(gameBoard.NodeSize * (width + 1)
        //    / _width, gameBoard.NodeSize
        //    * (height + 1) / _height, 1);

        //loop to get non-matching board
        foreach (Board gameBoard in gameBoards) {
            boardController.InitBoard(gameBoard);
        }
    }
    public override void ProcessMatch(Board board, List<Point> connected) {
        // idx : piece value ,
        // item1 :piece cnt,
        // item2 : xpos of last updated piece
        var matchTypeCnt = new Dictionary<NormalType.ENormalType, ValueTuple<int, int>>();

        //remove the node pieces connected
        foreach (Point pnt in connected) {
            Node node = board.GetNodeAtPoint(pnt);

            //if node is normal piece(not special, not hole, not blank)
            if (node.typeVal is NormalType) {
                NormalType.ENormalType idx = (node.typeVal as NormalType).TypeVal;
                if (!matchTypeCnt.TryAdd(idx, new ValueTuple<int, int>(1, pnt.x))) {
                    matchTypeCnt[idx] = new ValueTuple<int, int>(matchTypeCnt[idx].Item1 + 1, pnt.x);
                }
            }

            scoreManager.AddScore(perPieceScore);

            boardController.KillPiece(board, pnt, true);

            NodePiece nodePiece = node.GetPiece();
            if (nodePiece != null) Destroy(nodePiece.gameObject);
            node.SetPiece(null);
        }

        var matched5list = new List<ValueTuple<SpecialType, int>>();

        foreach (NormalType.ENormalType j in System.Enum.GetValues(typeof(NormalType.ENormalType))) {
            if (!matchTypeCnt.ContainsKey(j)) continue;
            if (matchTypeCnt[j].Item1 == 4) {
                scoreManager.AddScore(match4ExtraScore);

            }
            else if (matchTypeCnt[j].Item1 == 5) {
                scoreManager.AddScore(match5ExtraScore);

                //send block's info which matched 5
                matched5list.Add(new ValueTuple<SpecialType, int>(new SpecialType((SpecialType.ESpecialType)j), matchTypeCnt[j].Item2));

            }
            else if (matchTypeCnt[j].Item1 > 5) {
                scoreManager.AddScore(match6plusExtraScore);

                //send block's info 5or more matched block is in  line
                matched5list.Add(new ValueTuple<SpecialType, int>(new SpecialType((SpecialType.ESpecialType)j), matchTypeCnt[j].Item2));
            }
        }

        Matched();
        boardController.DropNewPiece(board, matched5list);
    }

    void ProcessFlip(Board board, SwapPacket packet) {
        Debug.Log($"Flip {packet.x1} {packet.y1} with {packet.x2} {packet.y2}");
        Node selected = board.GetNodeAtPoint(new Point(packet.x1, packet.y1));
        Node flipped = board.GetNodeAtPoint(new Point(packet.x2, packet.y2));
        if (selected != null && flipped != null) {
            if (selected.GetPiece() != null && flipped.GetPiece() != null) {
                //selected.GetPiece().MovePositionTo(flipped.GetPiece().transform.position);
                //flipped.GetPiece().MovePositionTo(selected.GetPiece().transform.position);
                boardController.FlipPieces(board, new Point(packet.x1, packet.y1), new Point(packet.x2, packet.y2), true);
            }
        }
    }

    void PreventClick() {
        if (clickableTime <= 0) GameManager.instance.IsClickable = true;
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
            audioController.PlayComboAudio(combo);
        }
        scoreManager.AddScore(Math.Clamp((combo / 5), 0, 6) * perPieceScore);
        audioController.PlayBlockPopAudio();
    }

    public void BacktoTitle() {
        SceneManager.LoadScene("StartScene");
    }

    public override void SpecialBlockPressed() {
        scoreManager.AddScore(perPieceScore);
        GameManager.instance.IsClickable = false;
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

    public override void GameOver() {
        foreach (Board gameBoard in gameBoards)
            boardController.DisableBoards(gameBoard);

        gameEndScreen.SetActive(true);

        audioController.Stop();

        finalScore.text = "Final Score : " + scoreManager;
        this.enabled = false;
    }
}
