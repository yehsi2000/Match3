using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using KaimiraGames;
using UnityEditor;

[InitializeOnLoad]
public class SingleGameController : MonoBehaviour {
    public enum SpecialBlockType {
        SITRI,
        DAVI,
        LIZA,
        MONA,
        UMBRELLA,
        WOOKONG
    }

    [System.Serializable]
    public class Node {
        public int value; //0=blank, 1=cube, 2=sphere, 3=cylinder, 4=pyramid, 5=diamond, -1 = hole
        public Point index;
        NodePiece piece;

        public Node(int v, Point i) {
            value = v;
            index = i;
        }

        public void SetPiece(NodePiece p) {
            piece = p;
            value = (piece == null) ? 0 : piece.value;
            if (piece == null) return;
            piece.SetIndex(index);
        }

        public NodePiece GetPiece() {
            return piece;
        }
    }

    [System.Serializable]
    public class FlippedPieces {
        public NodePiece one;
        public NodePiece two;

        public FlippedPieces(NodePiece o, NodePiece t) {
            one = o;
            two = t;
        }

        public NodePiece GetOtherPiece(NodePiece p) {
            if (p == one) return two;
            else if (p == two) return one;
            else return null;
        }
    }

    public static readonly int SPECIALBLOCK = 100;
    public ArrayLayout boardLayout;
    public static bool isClickable = true;

    [Header("UI Elements")]
    public Sprite[] pieces;
    public Sprite[] specialPieces;
    public GameObject gameBoard;
    public GameObject killedBoard;
    public GameObject gameEndScreen;
    public GameObject bgImageObject;
    public TMP_Text finalScore;
    public ComboDisplay comboDisplay;

    [Header("Prefabs")]
    public GameObject nodePiece;
    public GameObject killedPiece;
    public GameObject popParticle;
    public GameObject[] specialParticles;

    [Header("Score")]
    public int score;
    public int autoBlockWeightMultiplier = 100;
    public int perPieceScore = 5;
    public int match4ExtraScore = 20;
    public int match5ExtraScore = 50;
    public int match6plusExtraScore = 100;

    [Header("NodeSize")]
    public float nodeSize = 2f;

    [Header("Time")]
    [SerializeField]
    private float clickStopInterval = 0.5f;

    [SerializeField]
    private float comboRetainInterval = 1.5f;

    [Header("Audio")]
    [SerializeField]
    private AudioSource bgm;

    [SerializeField]
    private AudioSource comboAudio;

    private AudioSource blockPopAudio;

    [SerializeField]
    private AudioClip[] blockPopAudioClips;

    [SerializeField]
    private AudioClip[] bgmAudioClips;

    [SerializeField]
    private AudioClip[] comboAudioClips;

    [Header("Misc")]
    [SerializeField]
    private static int width = 9;

    [SerializeField]
    private static int height = 14;

    int[] blockSpawnOffset;
    float clickableTime = 0f;
    float comboTime = 0f;

    [HideInInspector]
    public int combo = 0;

    [HideInInspector]
    public Node[,] board;

    [HideInInspector]
    static readonly Point[] directions = {
        Point.up,
        Point.right,
        Point.down,
        Point.left
    };

    List<NodePiece> updateList;
    List<Point> specialUpdateList;
    List<FlippedPieces> flippedList;
    List<KilledPiece> killedPieceList;
    List<ParticleSystem> particlePool;
    List<List<ParticleSystem>> specialPool;
    System.Random random;
    List<WeightedList<int>> myWL;

    public static int getWidth() {
        return width;
    }
    public static int getHeight() {
        return height;
    }

    protected virtual void Awake() {
        KilledPiece.onKilledPieceRemove.AddListener(KilledPieceRemoved);
        NodePiece.onSpecialBlockPress.AddListener(SpecialBlockPressed);
    }

    public void Reset() {
        StartGame();
        gameEndScreen.SetActive(false);
    }

    protected virtual void Start() {
        StartGame();
        gameEndScreen.SetActive(false);
    }

    protected virtual void Update() {
        //update timer
        TimerTick();

        //prevent clicking while special block popping
        PreventClick();

        //update moving pieces and store it for flip check
        List<NodePiece> finishedUpdating = new List<NodePiece>();

        for (int i = 0; i < updateList.Count; i++) {
            NodePiece piece = updateList[i];
            if (piece != null && !piece.UpdatePiece()) finishedUpdating.Add(piece);
        }

        //Update for special block activation
        SpecialBlockTick();

        //check if flipped pieces could make a match, else revert flip
        CheckMoveMatched(ref finishedUpdating);
    }

    /// <summary>
    /// Initialize game variable and set components
    /// </summary>
    public void StartGame() {
        string seed = getRandomSeed();
        random = new System.Random(seed.GetHashCode());
        myWL = new List<WeightedList<int>>();

        for (int i = 1; i <= pieces.Length; ++i) {
            var newWL = new WeightedList<int>(random);
            for (int j = 1; j <= pieces.Length; ++j) {
                if (j == i) newWL.Add(j, autoBlockWeightMultiplier);
                else newWL.Add(j, 100);
            }
            myWL.Add(newWL);
        }

        updateList = new List<NodePiece>();
        specialUpdateList = new List<Point>();
        flippedList = new List<FlippedPieces>();
        killedPieceList = new List<KilledPiece>();

        blockSpawnOffset = new int[width];

        particlePool = new List<ParticleSystem>();
        specialPool = new List<List<ParticleSystem>>();

        //fill the pool
        for (int i = 0; i <= pieces.Length; ++i) {
            specialPool.Add(new List<ParticleSystem>());
        }

        score = 0;
        comboDisplay.Initialize(comboRetainInterval);
        Timer.instance.StartTimer();

        blockPopAudio = GetComponent<AudioSource>();

        comboAudio.volume = PlayerPrefs.GetFloat("sfx_volume", 1);
        blockPopAudio.volume = PlayerPrefs.GetFloat("sfx_volume", 1);

        bgm.clip = bgmAudioClips[PlayerPrefs.GetInt("bgm") % bgmAudioClips.Length];
        if (!PlayerPrefs.HasKey("bgm")) PlayerPrefs.SetInt("bgm", 0);
        bgm.volume = PlayerPrefs.GetFloat("volume", 1);

        bgm.Play();

        //gameBoard.GetComponent<RectTransform>().sizeDelta = new Vector2(nodeSize*width, nodeSize*height);
        //killedBoard.GetComponent<RectTransform>().sizeDelta = new Vector2(nodeSize*width, nodeSize*height);

        // set sprite image background to camera size, currently not used
        SpriteRenderer bgSpriteRenderer = bgImageObject.GetComponent<SpriteRenderer>();

        float _width = bgSpriteRenderer.bounds.size.x;
        float _height = bgSpriteRenderer.bounds.size.y;

        // set background image to gameboard size
        bgImageObject.transform.localScale = new Vector3(nodeSize * (width + 1)
            / _width, nodeSize
            * (height + 1) / _height, 1);

        //loop to get non-matching board
        do {
            InitializeBoard();
            VerifyBoard();
            InstantiateBoard();
        } while (isDeadlocked());
    }
    /// <summary>
    /// Fill Board with random value, doesn't check if there are match
    /// Should call VerifyBoard after to check if it's in valid form
    /// </summary>
    void InitializeBoard() {
        board = new Node[width, height];

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                board[x, y] = new Node(
                    (boardLayout.rows[y].row[x]) ? 103 : GetRandomPieceVal(),
                    new Point(x, y));
            }
        }
    }

    bool isDeadlocked() {

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Point p = new Point(x, y);

                int val = GetValueAtPoint(p);
                if (val <= 0) continue;

                if (y < height - 1) {
                    Point down = new Point(x, y + 1);
                    if (findConnected(p, false, down).Count > 0) return false;
                }

                if (x < width - 1) {
                    Point right = new Point(x + 1, y);
                    if (findConnected(p, false, right).Count > 0) return false;
                }
            }
        }
        return true;
    }


    /// <summary>
    /// Check if there's any connected block in current board. If there is, remove it and regenerated block in place
    /// </summary>
    void VerifyBoard() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Point p = new Point(x, y);

                int val = GetValueAtPoint(p);
                if (val <= 0) continue;

                List<int> removeList = new List<int>();

                while (findConnected(p, true).Count > 0) {
                    val = GetValueAtPoint(p);

                    if (!removeList.Contains(val)) removeList.Add(val);

                    SetValueAtPoint(p, newValue(ref removeList));
                }
            }
        }
    }

    /// <summary>
    /// Generate and place instances of nodepiece prefabs in node-size-aligned position
    /// </summary>
    void InstantiateBoard() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Node node = GetNodeAtPoint(new Point(x, y));

                int val = node.value;
                if (val <= 0) continue;

                GameObject p = Instantiate(nodePiece, gameBoard.transform);
                NodePiece piece = p.GetComponent<NodePiece>();

                //RectTransform rect = p.GetComponent<RectTransform>();

                p.transform.position = new Vector3(
                    nodeSize / 2 + (nodeSize * (x - width / 2f)),
                    -nodeSize / 2 - (nodeSize * (y - height / 2f))
                    );

                if (val >= SingleGameController.SPECIALBLOCK) {
                    piece.Initialize(val, new Point(x, y), specialPieces[val - SPECIALBLOCK], nodeSize);
                } else {
                    piece.Initialize(val, new Point(x, y), pieces[val - 1], nodeSize);
                }

                node.SetPiece(piece);
            }
        }
    }

    // TODO : send this to network class + link with delegate and event
    protected virtual void SendFlip(NodePiece selected, NodePiece flipped) { }

    void TimerTick() {
        if (!Timer.instance.UpdateTimer()) {

            gameBoard.SetActive(false);
            killedBoard.SetActive(false);
            gameEndScreen.SetActive(true);

            bgm.Stop();

            finalScore.text = "Final Score : " + score;
            this.enabled = false;
        }
    }

    void PreventClick() {
        if (clickableTime <= 0) isClickable = true;
        else clickableTime -= Time.deltaTime;

        if (comboTime <= 0) combo = 0;
        else comboTime -= Time.deltaTime;
    }

    void ProcessMatch(List<Point> connected) {
        // idx : piece value ,
        // item1 :piece cnt,
        // item2 : xpos of last updated piece
        ValueTuple<int, int>[] matchTypeCnt = new ValueTuple<int, int>[pieces.Length];

        //remove the node pieces connected
        foreach (Point pnt in connected) {
            Node node = GetNodeAtPoint(pnt);

            //if node is normal piece(not special, not hole, not blank)
            if (0 < node.value && node.value <= pieces.Length) {
                matchTypeCnt[node.value - 1].Item1++;
                matchTypeCnt[node.value - 1].Item2 = pnt.x;
            }

            score += perPieceScore;

            KillPiece(pnt, true);

            NodePiece nodePiece = node.GetPiece();

            if (nodePiece != null) Destroy(nodePiece.gameObject);
            node.SetPiece(null);
        }

        List<ValueTuple<int, int>> matched5list = new List<ValueTuple<int, int>>();

        for (int j = 0; j < pieces.Length; ++j) {
            if (matchTypeCnt[j].Item1 == 4) {
                score += match4ExtraScore;
            } else if (matchTypeCnt[j].Item1 == 5) {
                score += match5ExtraScore;

                //send block's info which matched 5
                matched5list.Add(new ValueTuple<int, int>(j + 1, matchTypeCnt[j].Item2));
            } else if (matchTypeCnt[j].Item1 > 5) {
                score += match6plusExtraScore;

                //send block's info 5or more matched block is in  line
                matched5list.Add(new ValueTuple<int, int>(j + 1, matchTypeCnt[j].Item2));
            }
        }

        Matched();
        DropNewPiece(matched5list);

        blockPopAudio.clip = blockPopAudioClips[UnityEngine.Random.Range(0, blockPopAudioClips.Length - 1)];
        blockPopAudio.Play();
    }

    void CheckMoveMatched(ref List<NodePiece> finishedUpdating) {
        for (int i = 0; i < finishedUpdating.Count; i++) {

            NodePiece piece = finishedUpdating[i]; //updated piece
            FlippedPieces flip = GetFlipped(piece); //flipped by updated piece
            NodePiece flippedPiece = null;

            int x = piece.index.x; //"x"th column

            blockSpawnOffset[x] = Mathf.Clamp(blockSpawnOffset[x] - 1, 0, width);

            //check if user controlled piece made a match
            List<Point> connected = findConnected(piece.index, true); 

            bool wasFlipped = (flip != null);

            if (wasFlipped) {
                SendFlip(piece, flippedPiece);
                flippedPiece = flip.GetOtherPiece(piece);
                AddPoints(ref connected, findConnected(flippedPiece.index, true));
            }
            if (connected.Count == 0) {
                //if we didn't make a match
                if (wasFlipped) FlipPieces(piece.index, flippedPiece.index, false); //revert flip
            } else {
                //made a match
                ProcessMatch(connected);
            }

            flippedList.Remove(flip); //remove the flip after update
            updateList.Remove(piece); //done updating the piece
        }
    }

    void SpecialBlockTick() {
        if (specialUpdateList.Count > 0) {
            for (int i = 0; i < specialUpdateList.Count; ++i) {
                KillPiece(specialUpdateList[i], false);

                Node node = GetNodeAtPoint(specialUpdateList[i]);
                NodePiece nodePiece = node.GetPiece();

                score += perPieceScore;

                if (nodePiece != null) {
                    Destroy(nodePiece.gameObject);
                }
                node.SetPiece(null);
            }

            DropNewPiece();

            isClickable = false;
            clickableTime = clickStopInterval;
            blockPopAudio.clip = blockPopAudioClips[UnityEngine.Random.Range(0, blockPopAudioClips.Length - 1)];
            Matched();
            blockPopAudio.Play();
            specialUpdateList.Clear();
        }
    }

    /// <summary>
    /// Increment Combo and perform additional jobs related to combo, ex)sfx, score, combotimer
    /// </summary>

    void Matched() {
        combo++;
        comboTime = comboRetainInterval;
        comboDisplay.UpdateCombo(combo);

        if (combo % 5 == 0 && combo > 0) {
            int combosfxindex = Math.Clamp((combo / 5) - 1, 0, comboAudioClips.Length - 1);
            comboAudio.clip = comboAudioClips[combosfxindex];
            comboAudio.Play();
        }

        score += Math.Clamp((combo / 5), 0, 6) * perPieceScore;
        ScoreBoard.instance.UpdateScore(score);
    }


    /// <summary>
    /// Drop NodePiece if there are hollows below. Generate empty block on top if needed.
    /// </summary>
    /// <param name="specialBlockList">List of infos of special block to generate. Tuple(speical generate xval, specialblock type)</param>

    void DropNewPiece(List<ValueTuple<int, int>> specialBlockList = null) {
        for (int x = 0; x < width; x++) {
            for (int y = height - 1; y >= 0; y--) {

                //iterate from the top to bottom
                Point curPoint = new Point(x, y);
                Node curNode = GetNodeAtPoint(curPoint);
                int curVal = GetValueAtPoint(curPoint);

                if (curVal != 0) continue; //find blank space where connected block disappeared

                //y=-1 is above the top line
                //for pieces above this blank drop down
                for (int ny = y - 1; ny >= -1; ny--) {
                    Point next = new Point(x, ny);
                    int nextVal = GetValueAtPoint(next);

                    //another blank, find upper one
                    if (nextVal == 0) continue; 

                    if (nextVal != -1) {
                        //if we did not hit top(or intentional hole) then drag upper ones down to the hole
                        Node got = GetNodeAtPoint(next);
                        NodePiece piece = got.GetPiece();

                        //Set the hole to upper piece
                        curNode.SetPiece(piece);
                        updateList.Add(piece);

                        got.SetPiece(null); //Replace the upper piece to blank
                    } else {
                        //if above is top wall or hole create new piece and drop it from the top
                        int newVal = GetRandomPieceVal();
                        int[] nearRow = { -2, -1, 1, 2 };
                        List<int> nearValues = new List<int>();

                        foreach (int diff in nearRow) {
                            if (x + diff >= width || 
                                x + diff < 0 || 
                                y + blockSpawnOffset[x] < 0 || 
                                y + blockSpawnOffset[x] >= height) 
                                continue;

                            NodePiece np = board[x + diff, y].GetPiece();

                            if (np != null) {
                                nearValues.Add(np.value);
                            }
                        }

                        for (int i = 0; i < nearValues.Count - 1; ++i) {
                            if (nearValues[i] == nearValues[i + 1] && nearValues[i] != -1) {

                                if (newVal < SingleGameController.SPECIALBLOCK) 
                                    newVal = GetWeightedRandomPieceVal(nearValues[i]);

                                break;
                            }
                        }

                        //y=-1 is above top line, fills[x] = offset up, as we are dropping more than 1 piece
                        Point fallPoint = new Point(x, -1 - blockSpawnOffset[x]);

                        //create new piece
                        GameObject obj = Instantiate(nodePiece, gameBoard.transform);
                        NodePiece piece = obj.GetComponent<NodePiece>();

                        if (specialBlockList != null && specialBlockList.Count > 0) {
                            for (int i = 0; i < specialBlockList.Count; ++i) {
                                if (specialBlockList[i].Item2 == x) {
                                    piece.Initialize(SingleGameController.SPECIALBLOCK + specialBlockList[i].Item1, 
                                        curPoint, specialPieces[specialBlockList[i].Item1], nodeSize);

                                    specialBlockList.RemoveAt(i);
                                    break;

                                } else {
                                    piece.Initialize(newVal, curPoint, pieces[newVal - 1], nodeSize);
                                }
                            }
                        } else {
                            piece.Initialize(newVal, curPoint, pieces[newVal - 1], nodeSize);
                        }

                        //put new piece on top so it looks like falling down
                        piece.transform.position = getPositionFromPoint(fallPoint);

                        Node hole = GetNodeAtPoint(curPoint);
                        hole.SetPiece(piece);

                        ResetPiece(piece);
                        blockSpawnOffset[x]++; //move offset upper for more piece to drop
                    }
                    break;
                }
            }
        }

    }

    /// <summary>
    /// Get flipped piece of current selected NodePiece
    /// </summary>
    /// <param name="p">selected piece</param>
    /// <returns></returns>
    FlippedPieces GetFlipped(NodePiece p) {
        FlippedPieces flip = null;

        for (int i = 0; i < flippedList.Count; i++) {
            if (flippedList[i].GetOtherPiece(p) != null) {
                flip = flippedList[i];
                break;
            }
        }

        return flip;
    }

    public void BacktoTitle() {
        SceneManager.LoadScene("StartScene");
    }

    // TODO : Move this to another class
    public void SetVolume(float volume) {
        bgm.volume = volume;
        PlayerPrefs.SetFloat("volume", volume);
    }

    public void SetSFXVolume(float volume) {
        comboAudio.volume = volume;
        blockPopAudio.volume = volume;
        PlayerPrefs.SetFloat("sfx_volume", volume);
    }

    /// <summary>
    /// Doesn't really kill piece. Instantiates killpiece prefabs in killboard and make explosion effect on popped pieces
    /// </summary>
    /// <param name="p">Piece which is killed and to be dropped</param>
    void KillPiece(Point p, bool bwithParticle) {
        int val = GetValueAtPoint(p);
        if (val <= 0) return;

        GameObject kill = GameObject.Instantiate(killedPiece, killedBoard.transform);
        KilledPiece kPiece = kill.GetComponent<KilledPiece>();
        Vector2 pointPos = getPositionFromPoint(p);

        if (bwithParticle || val >= SPECIALBLOCK) {
            List<ParticleSystem> available = new List<ParticleSystem>();

            if (val >= SPECIALBLOCK) {
                for (int i = 0; i < specialPool[val - SPECIALBLOCK].Count; i++) {
                    if (specialPool[val - SPECIALBLOCK][i].isStopped) {
                        available.Add(specialPool[val - SPECIALBLOCK][i]);
                    }
                }
            } else {
                for (int i = 0; i < particlePool.Count; i++) {
                    if (particlePool[i].isStopped) {
                        available.Add(particlePool[i]);
                    }
                }
            }

            ParticleSystem particle = null;

            if (available.Count > 0) {
                particle = available[0];
            } else {
                GameObject particleEffect;

                if (val >= SPECIALBLOCK) {
                    particleEffect = specialParticles[val - SPECIALBLOCK];
                } else {
                    particleEffect = popParticle;
                }

                GameObject particleObject = GameObject.Instantiate(particleEffect, killedBoard.transform);
                ParticleSystem objParticle = particleObject.GetComponent<ParticleSystem>();
                particle = objParticle;

                if (val >= SPECIALBLOCK) {
                    specialPool[val - SPECIALBLOCK].Add(objParticle);
                } else {
                    particlePool.Add(objParticle);
                }
            }
            particle.transform.position = pointPos;
            particle.Play();
        }


        if (kPiece != null && val - 1 < pieces.Length) {
            kPiece.Initialize(pieces[val - 1], pointPos, nodeSize);
            killedPieceList.Add(kPiece);
        } else if (kPiece != null && val - SPECIALBLOCK < specialPieces.Length) {
            kPiece.Initialize(specialPieces[val - SPECIALBLOCK], pointPos, nodeSize);
            SpecialBlockPressed(p, (SpecialBlockType)val - SPECIALBLOCK);
            killedPieceList.Add(kPiece);
        }
    }

    /// <summary>
    /// Reacts to killpiece prefab unrendered on screen and remove it from killedlist
    /// </summary>
    /// <param name="killedPiece"></param>
    public void KilledPieceRemoved(KilledPiece killedPiece) {
        killedPieceList.Remove(killedPiece);
    }

    

    /// <summary>
    /// Reacts to special blocks pressed and execute it's implementation
    /// </summary>
    /// <param name="pnt">Special block position index</param>
    /// <param name="val">Special block type 0 ~ </param>
    void SpecialBlockPressed(Point pnt, SpecialBlockType val) {
        switch (val) {
            case SpecialBlockType.SITRI:
                for (int i = 0; i < width; ++i) {
                    for (int j = 0; j < height; ++j) {
                        specialUpdateList.Add(new Point(i, j));
                    }
                }
                break;

            case SpecialBlockType.DAVI:
                specialUpdateList.Add(pnt);

                for (int i = 1; i <= Math.Max(height, width); i++) {
                    Point[] dir = { new Point(1, -1), new Point(1, 1), new Point(-1, 1), new Point(-1, -1) };
                    foreach (Point p in dir) {
                        Point toAdd = Point.add(pnt, Point.mult(p, i));
                        if (toAdd.x >= 0 && toAdd.x < width && toAdd.y >= 0 && toAdd.y < height) {
                            specialUpdateList.Add(toAdd);
                        }
                    }
                }
                break;

            case SpecialBlockType.LIZA:
                for (int i = 0; i < width; ++i) {
                    specialUpdateList.Add(new Point(i, pnt.y));
                }

                for (int i = 0; i < height; ++i) {
                    if (i == pnt.y) continue; //prevent dual update for pressed
                    specialUpdateList.Add(new Point(pnt.x, i));
                }
                break;

            case SpecialBlockType.MONA:
                for (int i = -2; i <= 2; ++i) {
                    for (int j = -2; j <= 2; ++j) {
                        Point toAdd = Point.add(pnt, new Point(i, j));

                        if (toAdd.x >= 0 && toAdd.x < width && toAdd.y >= 0 && toAdd.y < height) {
                            specialUpdateList.Add(toAdd);
                        }
                    }
                }
                break;

            case SpecialBlockType.UMBRELLA:
                specialUpdateList.Add(pnt);

                while (specialUpdateList.Count < 10) {
                    int randomX = random.Next(0, width);
                    int randomY = random.Next(0, height);

                    Point newpnt = new Point(randomX, randomY);
                    Node newnode = GetNodeAtPoint(newpnt);

                    if (newnode.value >= SingleGameController.SPECIALBLOCK) continue;
                    specialUpdateList.Add(newpnt);
                }
                break;

            case SpecialBlockType.WOOKONG:
                specialUpdateList.Add(pnt);
                for (int i = 0; i < width; ++i) {
                    for (int j = 0; j < height; ++j) {
                        Node node = GetNodeAtPoint(new Point(i, j));

                        if (node != null) {
                            NodePiece piece = node.GetPiece();

                            if (piece != null && piece.value == 5) {
                                specialUpdateList.Add(new Point(i, j));
                            }
                        }
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Flip two piece
    /// </summary>
    /// <param name="one">piece to be flipped</param>
    /// <param name="two">piece to be flipped</param>
    /// <param name="main">is this called in user action not revert flip?</param>
    public void FlipPieces(Point one, Point two, bool main) {
        if (GetValueAtPoint(one) < 0) return; //if first one's hole do nothing

        Node nodeOne = GetNodeAtPoint(one);
        NodePiece pieceOne = nodeOne.GetPiece();

        if (GetValueAtPoint(two) > 0) {
            //second one's also not hole
            Node nodeTwo = GetNodeAtPoint(two);
            NodePiece pieceTwo = nodeTwo.GetPiece();

            nodeOne.SetPiece(pieceTwo);
            nodeTwo.SetPiece(pieceOne);

            if (main) flippedList.Add(new FlippedPieces(pieceOne, pieceTwo));

            updateList.Add(pieceOne);
            updateList.Add(pieceTwo);
        } else {
            ResetPiece(pieceOne); //second one's hole, reset first one's position
        }
    }

    /// <summary>
    /// Find and return all blocks connected with current block.
    /// </summary>
    /// <param name="p">Initial block position</param>
    /// <param name="main">is this called in Update() function, not recursively in itself?</param>
    /// <returns></returns>
    List<Point> findConnected(Point p, bool main, Point exchanged = null) {
        List<Point> connected = new List<Point>();

        int val;

        if (exchanged != null) {
            val = GetValueAtPoint(exchanged);
        } else {
            val = GetValueAtPoint(p);
        }
        if (val > pieces.Length + 1) return connected;


        foreach (Point dir in directions) {
            List<Point> line = new List<Point>();

            line.Add(p);

            int same = 0;

            for (int i = 1; i < 3; i++) {
                Point check = Point.add(p, Point.mult(dir, i));
                if (check == exchanged) continue;
                if (GetValueAtPoint(check) == val) {
                    line.Add(check);
                    same++;
                }
            }
            if (same > 1) {
                AddPoints(ref connected, line); //Add these points to the overarching connected list
            }
        }

        for (int i = 0; i < 2; i++) {
            Point[] check = { Point.add(p, directions[i]), Point.add(p, directions[i + 2]) };
            List<Point> line = new List<Point>();
            line.Add(p);
            
            int same = 0;

            foreach (Point next in check) {
                if (next == exchanged) continue;
                if (GetValueAtPoint(next) == val) {
                    line.Add(next);
                    same++;
                }
            }
            if (same > 1) AddPoints(ref connected, line);
        }

        if (main) { //checks for other matches along the current match
            for (int i = 0; i < connected.Count; i++) {
                List<Point> more = findConnected(connected[i], false);
                int additional_match = AddPoints(ref connected, more);
            }
        }
        return connected;

    }


    /// <summary>
    /// Helper function to concat two List of points
    /// </summary>
    /// <param name="points">Base list to be concatenated</param>
    /// <param name="add">List to be added in the first List</param>
    /// <returns></returns>
    int AddPoints(ref List<Point> points, List<Point> add) {
        int added = 0;

        foreach (Point p in add) {
            bool doAdd = true;

            for (int i = 0; i < points.Count; i++) {
                if (points[i].Equals(p)) {
                    doAdd = false;
                    break;
                }
            }

            if (doAdd) {
                points.Add(p);
                added++;
            }
        }
        return added;
    }

    /// <summary>
    /// Push piece in update list to place it in index-position.
    /// Used to initiate NodePiece or revert flip 
    /// </summary>
    /// <param name="piece">node to reset position</param>
    public void ResetPiece(NodePiece piece) {
        piece.ResetPosition();
        updateList.Add(piece);
    }

    /// <summary>
    /// get random normal piece value
    /// </summary>
    /// <returns>random normal piece (starting with 1)</returns>
    int GetRandomPieceVal() {
        int val = 1;
        val = (random.Next(0, 100) / (100 / pieces.Length)) + 1;
        return val;
    }

    /// <summary>
    /// Get weighted random value of selected value
    /// </summary>
    /// <param name="val">values wishes to be generated more often</param>
    /// <returns>weighted random normal piece type(starting with 1)</returns>
    int GetWeightedRandomPieceVal(int val) {
        if (val < 0 || val > pieces.Length) return GetRandomPieceVal();
        return myWL[val - 1].Next();
    }

    protected int GetValueAtPoint(Point p) {
        if (p.x < 0 || p.x >= width || p.y < 0 || p.y >= height) {
            return -1;
        }
        return board[p.x, p.y].value;
    }

    protected void SetValueAtPoint(Point p, int v) {
        board[p.x, p.y].value = v;
    }

    protected Node GetNodeAtPoint(Point p) {
        return board[p.x, p.y];
    }

    /// <summary>
    /// Get Piece type value except ones in remove List
    /// </summary>
    /// <param name="remove">piece type values not wanted to be generated</param>
    /// <returns>new piece type value which is not in remove List</returns>
    int newValue(ref List<int> remove) {
        List<int> available = new List<int>();

        for (int i = 0; i < pieces.Length; i++) {
            available.Add(i + 1);
        }

        foreach (int i in remove) available.Remove(i);

        if (available.Count <= 0) return 0;
        return available[random.Next(0, available.Count)];
    }

    string getRandomSeed() {
        string seed = "";
        string acceptableChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz123456789!@#$%^&*()";
        for (int i = 0; i < 20; i++)
            seed += acceptableChars[UnityEngine.Random.Range(0, acceptableChars.Length)];
        return seed;
    }

    public Vector2 getPositionFromPoint(Point p) {
        return new Vector3(nodeSize / 2 + (nodeSize * (p.x - width / 2f)), -nodeSize / 2 - (nodeSize * (p.y - height / 2f)));
    }
}
