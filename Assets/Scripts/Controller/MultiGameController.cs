using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using KaimiraGames;

public class MultiGameController : GameControllerBase {
    public struct SwapPacket {
        public int x1;
        public int y1;
        public int x2;
        public int y2;
    }

    [SerializeField]
    BoardController boardController;

    [SerializeField]
    TimerController timerController;

    [SerializeField]
    AudioController audioController;

    [SerializeField]
    Board[] gameBoards;

    [Header("UI Elements")]
    public GameObject gameEndScreen;
    public GameObject bgImageObject;
    public TMP_Text myIdText;
    public TMP_Text opIdText;
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

    [Header("Network")]
    string opponentId;
    static int randomSeed = int.MaxValue;
    public bool isReady = false;
    public bool isOpponentReady = false;
    public Queue<SwapPacket> packetQueue;
    bool hasEnded;
    bool hasOpEnded;

    public string OpponentId {
        set { if (opponentId == null) opponentId = value; }
    }

    public int RandomSeed {
        get { return randomSeed; }
        set { if (value <= randomSeed) randomSeed = value; }
    }

    public float ClickStopInterval {
        get { return clickStopInterval; }
    }

    [SerializeField]
    float comboRetainInterval = 2f;

    int width;
    int height;

    [HideInInspector]
    public int combo = 0;

    float comboTime = 0f;


    void Reset() {
        StartGame();
        gameEndScreen.SetActive(false);
    }

    void Start() {
        packetQueue = new Queue<SwapPacket>();
        gameEndScreen.SetActive(false);
    }

    void Update() {
        if (!isReady || !isOpponentReady) return;

        if (packetQueue.Count > 0) {
            //Debug.Log("packet count : " + packetQueue.Count);
            SwapPacket packet = packetQueue.Dequeue();
            ProcessFlip(gameBoards[1], packet);
        }
        //prevent clicking while special block popping
        if (comboTime <= 0) combo = 0;
        else comboTime -= Time.deltaTime;

        foreach (Board gameBoard in gameBoards) {
            boardController.boardUpdate(gameBoard);
        }
        PreventClick();


    }

    /// <summary>
    /// Initialize game variable and set components
    /// </summary>
    public void StartGame() {
        Debug.Log("start game with seed " + randomSeed);
        //TODO : 3초세고 시작

        for (int i = 0; i < gameBoards.Length; i++) {
            gameBoards[i].rng = new CustomRandom(randomSeed);
        }

        ScoreManager.instance.Initialize();
        comboDisplay.Initialize(comboRetainInterval);
        timerController.StartTimer();
        myIdText.text = PlayerPrefs.GetString("myid", "Player1");
        opIdText.text = opponentId;

        audioController.PlayBGM();

        // set sprite image background to camera size, currently not used
        SpriteRenderer bgSpriteRenderer = bgImageObject.GetComponent<SpriteRenderer>();

        float _width = bgSpriteRenderer.bounds.size.x;
        float _height = bgSpriteRenderer.bounds.size.y;

        //loop to get non-matching board
        foreach (Board gameBoard in gameBoards) {
            boardController.InitBoard(gameBoard);
        }
    }
    public override LinkedList<ValueTuple<SpecialType, int>> ProcessMatch(Board board, List<Point> connected) {
        //Debug.Log("ProcessMatch");
        // idx : piece value ,
        // item1 :piece cnt,
        // item2 : xpos of last updated piece
        var matchTypeCnt = new Dictionary<NormalType.ENormalType, ValueTuple<int, int>>();
        var matched5list = new LinkedList<ValueTuple<SpecialType, int>>();

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

            if (board.IsPlayerBoard) ScoreManager.instance.AddScore(perPieceScore);

            boardController.KillPiece(board, pnt, true);

            NodePiece nodePiece = node.GetPiece();


            if (nodePiece != null) {
                Destroy(nodePiece.gameObject);
                //Debug.Log($"Destroy : {nodePiece.index.x}:{nodePiece.index.y}");
                node.SetPiece(null);
            }

        }

        foreach (NormalType.ENormalType j in System.Enum.GetValues(typeof(NormalType.ENormalType))) {
            if (!matchTypeCnt.ContainsKey(j)) continue;
            if (matchTypeCnt[j].Item1 == 4) {
                if (board.IsPlayerBoard) ScoreManager.instance.AddScore(match4ExtraScore);

            }
            else if (matchTypeCnt[j].Item1 == 5) {
                if (board.IsPlayerBoard) ScoreManager.instance.AddScore(match5ExtraScore);

                //send block's info which matched 5
                matched5list.AddLast(new ValueTuple<SpecialType, int>(new SpecialType((SpecialType.ESpecialType)j), matchTypeCnt[j].Item2));

            }
            else if (matchTypeCnt[j].Item1 > 5) {
                if (board.IsPlayerBoard) ScoreManager.instance.AddScore(match6plusExtraScore);

                //send block's info 5or more matched block is in  line
                matched5list.AddLast(new ValueTuple<SpecialType, int>(new SpecialType((SpecialType.ESpecialType)j), matchTypeCnt[j].Item2));
            }
        }

        Matched(board);
        //boardController.DropNewPiece(board, matched5list);
        return matched5list;
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

    public void Matched(Board board) {
        if (!board.IsPlayerBoard) return;

        combo++;
        comboTime = comboRetainInterval;
        comboDisplay.UpdateCombo(combo);

        if (combo % 5 == 0 && combo > 0) {
            audioController.PlayComboAudio(combo);
        }
        ScoreManager.instance.AddScore(Math.Clamp((combo / 5), 0, 6) * perPieceScore);
        audioController.PlayBlockPopAudio();
    }

    public void BacktoTitle() {
        if (FindObjectOfType<SignalingClient>() != null) {
            Destroy(FindObjectOfType<SignalingClient>().gameObject);
        }
        if (Network.instance != null) {
            Destroy(Network.instance.gameObject);
            Network.instance = null;
        }
        SceneManager.LoadScene("StartScene");
    }

    public void ReceiveSpecialPress(Point pnt, SpecialType type) {
        //Debug.Log("Special Pressed received");
        boardController.AddSpecialQueue(gameBoards[1], pnt, type);
    }

    public override void SpecialBlockPressed(Board board) {
        if (!board.IsPlayerBoard) return;
        ScoreManager.instance.AddScore(perPieceScore);
        GameManager.instance.IsClickable = false;
        clickableTime = ClickStopInterval;
        Matched(board);
    }

    public override void GameOver() {
        foreach (Board gameBoard in gameBoards)
            boardController.DisableBoards(gameBoard);

        gameEndScreen.SetActive(true);

        audioController.Stop();

        FinalScoreTextUpdate(true);

        Network.instance.SendGameOver(ScoreManager.instance.Score);
        //this.enabled = false;
    }
    public void FinalScoreTextUpdate(bool self) {

        if (self) hasEnded = true;
        else hasOpEnded = true;

        if (hasEnded && hasOpEnded) finalScore.text =
                $"Your Score : {ScoreManager.instance.Score}\n " +
                $"Opponent Score : {ScoreManager.instance.OpScore}\n " +
                $"YOU {(ScoreManager.instance.Score > ScoreManager.instance.OpScore ? "WON" : "LOST")}";
    }
}
