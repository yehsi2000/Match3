using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using KaimiraGames;
using UnityEditor;
using Unity.VisualScripting;

[InitializeOnLoad]
public class GameController : MonoBehaviour {
    

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

    [SerializeField]
    private TimerController timerController;

    [SerializeField]
    private PieceController pieceController;

    [SerializeField]
    private AudioController audioController;


    public static readonly int SPECIALBLOCK = 100;
    //public ArrayLayout boardLayout;
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
    public int autoBlockWeightMultiplier = 100;
    public int perPieceScore = 5;
    public int match4ExtraScore = 20;
    public int match5ExtraScore = 50;
    public int match6plusExtraScore = 100;

    

    [Header("Time")]
    [SerializeField]
    private float clickStopInterval = 0.5f;

    [SerializeField]
    private float comboRetainInterval = 1.5f;

    [Header("Audio")]


    private int width;
    private int height;


    int[] blockSpawnOffset;
    float clickableTime = 0f;
    float comboTime = 0f;

    [HideInInspector]
    public int combo = 0;

    

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
    List<List<ParticleSystem>> specialParticlePool;
    System.Random random;
    List<WeightedList<int>> myWL;

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
        timerController.TimerTick();

        //prevent clicking while special block popping
        PreventClick();

        //update moving pieces and store it for flip check
        var finishedUpdating = new List<NodePiece>();

        for (int i = 0; i < updateList.Count; i++) {
            NodePiece piece = updateList[i];
            if (piece != null && !piece.UpdatePiece()) finishedUpdating.Add(piece);
        }

        //Update for special block activation
        SpecialBlockTick();

        //check if flipped pieces could make a match, else revert flip
        CheckMoveMatched(ref finishedUpdating);
    }

    void PreventClick() {
        if (clickableTime <= 0) isClickable = true;
        else clickableTime -= Time.deltaTime;

        if (comboTime <= 0) combo = 0;
        else comboTime -= Time.deltaTime;
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
            for (int j = 0; j <= System.Enum.GetValues(typeof(NormalType.ENormalType)).Length; ++j) {
                if (j == i) newWL.Add(j, autoBlockWeightMultiplier);
                else newWL.Add(j, 100);
            }
            myWL.Add(newWL);
        }

        width = GameManager.Instance.boardManager.Width;
        height = GameManager.Instance.boardManager.Height;
        if (width == 0 || height == 0) {
            throw new System.Exception("Board size is not set");
        }
        updateList = new List<NodePiece>();
        specialUpdateList = new List<Point>();
        flippedList = new List<FlippedPieces>();
        killedPieceList = new List<KilledPiece>();

        blockSpawnOffset = new int[width];

        particlePool = new List<ParticleSystem>();
        specialParticlePool = new List<List<ParticleSystem>>();

        //fill the pool
        for (int i = 0; i <= pieces.Length; ++i) {
            specialParticlePool.Add(new List<ParticleSystem>());
        }

        GameManager.Instance.scoreManager.Initialize();
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
        bgImageObject.transform.localScale = new Vector3(GameManager.Instance.boardManager.NodeSize * (width + 1)
            / _width, GameManager.Instance.boardManager.NodeSize
            * (height + 1) / _height, 1);

        //loop to get non-matching board
        do {
            InitializeBoard();
            InstantiateBoard();
        } while (isDeadlocked());
    }

    /// <summary>
    /// Fill Board with random value, doesn't check if there are match
    /// Should call VerifyBoard after to check if it's in valid form
    /// </summary>
    void InitializeBoard() {
        GameManager.Instance.boardManager.Board = new Node[width, height];
        
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                GameManager.Instance.boardManager.Board[x, y] = new Node(GetRandomPieceVal(), new Point(x, y));
            }
        }
        
        VerifyBoard();
    }

    bool isDeadlocked() {

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Point p = new Point(x, y);

                INodeType val = GetValueAtPoint(p);
                if (val is BlankType || val is BlockedNodeType) {
                    continue;
                }

                if (y < height - 1) {
                    Point down = new Point(x, y + 1);
                    if (FindConnected(p, false, down).Count > 0) return false;
                }

                if (x < width - 1) {
                    Point right = new Point(x + 1, y);
                    if (FindConnected(p, false, right).Count > 0) return false;
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

                INodeType pointVal = GetValueAtPoint(p);
                if (pointVal.isEqual(new BlankType()) ||
                    pointVal.isEqual(new BlockedNodeType())) {
                    continue;
                }

                var excludeList = new List<INodeType>();

                while (FindConnected(p, true).Count > 0) {
                    pointVal = GetValueAtPoint(p);

                    if (!excludeList.Contains(pointVal)) excludeList.Add(pointVal);

                    SetValueAtPoint(p, GetNewValue(ref excludeList));
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
                if (node.typeVal.isEqual(new BlockedNodeType())) {
                    continue;
                }

                GameObject p = Instantiate(nodePiece, gameBoard.transform);
                NodePiece piece = p.GetComponent<NodePiece>();

                //RectTransform rect = p.GetComponent<RectTransform>();

                p.transform.localPosition = new Vector3(
                    GameManager.Instance.boardManager.NodeSize / 2 + (GameManager.Instance.boardManager.NodeSize * (x - width / 2f)),
                    -GameManager.Instance.boardManager.NodeSize / 2 - (GameManager.Instance.boardManager.NodeSize * (y - height / 2f))
                    );
                int val = (int)(node.typeVal as NormalType).TypeVal;
                piece.Initialize(node.typeVal, 
                    new Point(x, y), pieces[val], 
                    GameManager.Instance.boardManager.NodeSize, 
                    GameManager.Instance.boardManager.Width, 
                    GameManager.Instance.boardManager.Height);
                

                node.SetPiece(piece);
            }
        }
    }

    // TODO : send this to network class + link with delegate and event
    protected virtual void SendFlip(NodePiece selected, NodePiece flipped) { }

    void CheckMoveMatched(ref List<NodePiece> finishedUpdating) {
        for (int i = 0; i < finishedUpdating.Count; i++) {

            NodePiece piece = finishedUpdating[i]; //updated piece
            FlippedPieces flip = GetFlipped(piece); //flipped by updated piece
            NodePiece flippedPiece = null;

            int x = piece.index.x; //"x"th column

            blockSpawnOffset[x] = Mathf.Clamp(blockSpawnOffset[x] - 1, 0, width);

            //check if user controlled piece made a match
            List<Point> connected = FindConnected(piece.index, true); 

            bool wasFlipped = (flip != null);

            if (wasFlipped) {
                SendFlip(piece, flippedPiece);
                flippedPiece = flip.GetOtherPiece(piece);
                AddPoints(ref connected, FindConnected(flippedPiece.index, true));
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


    void ProcessMatch(List<Point> connected) {
        // idx : piece value ,
        // item1 :piece cnt,
        // item2 : xpos of last updated piece
        var matchTypeCnt = new Dictionary<NormalType.ENormalType, ValueTuple<int, int>>();

        //remove the node pieces connected
        foreach (Point pnt in connected) {
            Node node = GetNodeAtPoint(pnt);

            //if node is normal piece(not special, not hole, not blank)
            if (node.typeVal is NormalType) {
                NormalType.ENormalType idx = (node.typeVal as NormalType).TypeVal;
                if (!matchTypeCnt.TryAdd(idx, new ValueTuple<int, int>(1, pnt.x))) {
                    matchTypeCnt[idx] = new ValueTuple<int, int>(matchTypeCnt[idx].Item1 + 1, pnt.x);
                }
            }

            GameManager.Instance.scoreManager.AddScore(perPieceScore);

            KillPiece(pnt, true);

            NodePiece nodePiece = node.GetPiece();
            if (nodePiece != null) Destroy(nodePiece.gameObject);
            node.SetPiece(null);
        }

        var matched5list = new List<ValueTuple<SpecialType, int>>();

        foreach (NormalType.ENormalType j in System.Enum.GetValues(typeof(NormalType.ENormalType))) {
            if (!matchTypeCnt.ContainsKey(j)) continue;
            if (matchTypeCnt[j].Item1 == 4) {
                GameManager.Instance.scoreManager.AddScore(match4ExtraScore);

            } else if (matchTypeCnt[j].Item1 == 5) {
                GameManager.Instance.scoreManager.AddScore(match5ExtraScore);

                //send block's info which matched 5
                matched5list.Add(new ValueTuple<SpecialType, int>(new SpecialType((SpecialType.ESpecialType)j), matchTypeCnt[j].Item2));

            } else if (matchTypeCnt[j].Item1 > 5) {
                GameManager.Instance.scoreManager.AddScore(match6plusExtraScore);

                //send block's info 5or more matched block is in  line
                matched5list.Add(new ValueTuple<SpecialType, int>(new SpecialType((SpecialType.ESpecialType)j), matchTypeCnt[j].Item2));
            }
        }

        Matched();
        DropNewPiece(matched5list);
    }


    void SpecialBlockTick() {
        if (specialUpdateList.Count > 0) {
            for (int i = 0; i < specialUpdateList.Count; ++i) {
                KillPiece(specialUpdateList[i], false);

                Node node = GetNodeAtPoint(specialUpdateList[i]);
                NodePiece nodePiece = node.GetPiece();

                GameManager.Instance.scoreManager.AddScore(perPieceScore);

                if (nodePiece != null) {
                    Destroy(nodePiece.gameObject);
                }
                node.SetPiece(null);
            }

            DropNewPiece();

            isClickable = false;
            clickableTime = clickStopInterval;
            specialUpdateList.Clear();
            Matched();
        }
    }

    /// <summary>
    /// Increment Combo and perform related jobs like sfx, score, combo
    /// </summary>

    void Matched() {
        combo++;
        comboTime = comboRetainInterval;
        comboDisplay.UpdateCombo(combo);

        if (combo % 5 == 0 && combo > 0) {
            audioController.PlayComboAudio(combo);
        }
        GameManager.Instance.scoreManager.AddScore(Math.Clamp((combo / 5), 0, 6) * perPieceScore);
        audioController.PlayBlockPopAudio();
    }


    /// <summary>
    /// Drop NodePiece if there are hollows below. Generate empty block on top if needed.
    /// </summary>
    /// <param name="specialBlockList">List of infos of special block to generate. Tuple(specialblock type, speical generate xval)</param>

    void DropNewPiece(List<ValueTuple<SpecialType, int>> specialBlockList = null) {
        for (int x = 0; x < width; x++) {
            for (int y = height - 1; y >= 0; y--) {

                //iterate from the top to bottom
                Point curPoint = new Point(x, y);
                Node curNode = GetNodeAtPoint(curPoint);
                INodeType curVal = GetValueAtPoint(curPoint);

                if (curVal is not BlankType) continue; //find blank space where connected block disappeared

                //y=-1 is above the top line
                //for pieces above this blank drop down
                for (int ny = y - 1; ny >= -1; ny--) {
                    Point next = new Point(x, ny);
                    INodeType nextVal = GetValueAtPoint(next);

                    if (nextVal is BlankType) continue; 

                    if (nextVal is not BlockedNodeType) {
                        //if we did not hit top(or intentional hole) then drag upper ones down to the hole
                        Node upperNode = GetNodeAtPoint(next);
                        NodePiece upperPiece = upperNode.GetPiece();

                        //Set the blank to upper piece
                        curNode.SetPiece(upperPiece);
                        updateList.Add(upperPiece);

                        upperNode.SetPiece(null); //Replace the upper piece to blank
                    } else {
                        //if above is top wall or blank create new piece and drop it from the top
                        NormalType newVal = GetRandomPieceVal();
                        int[] nearRow = { -2, -1, 1, 2 };
                        var nearValues = new List<INodeType>();

                        foreach (int diff in nearRow) {
                            if (x + diff >= width || 
                                x + diff < 0 || 
                                y + blockSpawnOffset[x] < 0 || 
                                y + blockSpawnOffset[x] >= height) 
                                continue;

                            NodePiece np = GameManager.Instance.boardManager.Board[x + diff, y].GetPiece();

                            if (np != null) {
                                nearValues.Add(np.NodeVal);
                            }
                        }

                        for (int i = 0; i < nearValues.Count - 1; ++i) {
                            if (nearValues[i] == nearValues[i + 1] && nearValues[i] is not BlockedNodeType) {
                                newVal = GetWeightedRandomPieceVal(nearValues[i] as NormalType);
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
                                    piece.Initialize(specialBlockList[i].Item1, 
                                        curPoint, 
                                        specialPieces[(int)specialBlockList[i].Item1.TypeVal], 
                                        GameManager.Instance.boardManager.NodeSize,
                                        GameManager.Instance.boardManager.Width, 
                                        GameManager.Instance.boardManager.Height);

                                    specialBlockList.RemoveAt(i);
                                    break;

                                } else {
                                    piece.Initialize(newVal,
                                        curPoint,
                                        pieces[(int)(newVal as NormalType).TypeVal], 
                                        GameManager.Instance.boardManager.NodeSize,
                                        GameManager.Instance.boardManager.Width,
                                        GameManager.Instance.boardManager.Height);
                                }
                            }
                        } else {
                            piece.Initialize(newVal, 
                                curPoint, 
                                pieces[(int)newVal.TypeVal], 
                                GameManager.Instance.boardManager.NodeSize,
                                GameManager.Instance.boardManager.Width,
                                GameManager.Instance.boardManager.Height);
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

    /// <summary>
    /// Doesn't really kill piece. Instantiates killpiece prefabs in killboard and make explosion effect on popped pieces
    /// </summary>
    /// <param name="p">Piece which is killed and to be dropped</param>
    void KillPiece(Point p, bool bwithParticle) {
        INodeType val = GetValueAtPoint(p);
        
        if(val is BlockedNodeType || val is BlankType) return;

        GameObject kill = GameObject.Instantiate(killedPiece, killedBoard.transform);
        KilledPiece kPiece = kill.GetComponent<KilledPiece>();
        Vector2 pointPos = getPositionFromPoint(p);

        if (bwithParticle || val is SpecialType) {
            List<ParticleSystem> available = new List<ParticleSystem>();
            //special effect
            if (val is SpecialType) {
                var specialval = val as SpecialType;
                for (int i = 0; i < specialParticlePool[(int)specialval.TypeVal].Count; i++) {
                    if (specialParticlePool[(int)specialval.TypeVal][i].isStopped) {
                        available.Add(specialParticlePool[(int)specialval.TypeVal][i]);
                    }
                }
            } else {
                //no special effect
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

                if (val is SpecialType) {
                    particleEffect = specialParticles[(int)(val as SpecialType).TypeVal];
                } else {
                    particleEffect = popParticle;
                }

                GameObject particleObject = GameObject.Instantiate(particleEffect, killedBoard.transform);
                ParticleSystem objParticle = particleObject.GetComponent<ParticleSystem>();
                particle = objParticle;

                if (val is SpecialType) {
                    specialParticlePool[(int)(val as SpecialType).TypeVal].Add(objParticle);
                } else {
                    particlePool.Add(objParticle);
                }
            }
            particle.transform.position = pointPos;
            particle.Play();
        }


        //if (kPiece != null && val is NormalType && val - 1 < pieces.Length) {
        if (kPiece != null && val is NormalType) {
            kPiece.Initialize(pieces[(int)(val as NormalType).TypeVal], pointPos, GameManager.Instance.boardManager.NodeSize);
            killedPieceList.Add(kPiece);
        //} else if (kPiece != null && val < specialPieces.Length) {
        } else if (kPiece != null && val is SpecialType) {
            kPiece.Initialize(specialPieces[(int)(val as SpecialType).TypeVal], pointPos, GameManager.Instance.boardManager.NodeSize);
            SpecialBlockPressed(p, (val as SpecialType));
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
    void SpecialBlockPressed(Point pnt, SpecialType val) {
        if (val == null) return;
        switch (val.TypeVal) {
            case SpecialType.ESpecialType.SITRI:
                for (int i = 0; i < width; ++i) {
                    for (int j = 0; j < height; ++j) {
                        specialUpdateList.Add(new Point(i, j));
                    }
                }
                break;

            case SpecialType.ESpecialType.DAVI:
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

            case SpecialType.ESpecialType.LIZA:
                for (int i = 0; i < width; ++i) {
                    specialUpdateList.Add(new Point(i, pnt.y));
                }

                for (int i = 0; i < height; ++i) {
                    if (i == pnt.y) continue; //prevent dual update for pressed
                    specialUpdateList.Add(new Point(pnt.x, i));
                }
                break;

            case SpecialType.ESpecialType.MONA:
                for (int i = -2; i <= 2; ++i) {
                    for (int j = -2; j <= 2; ++j) {
                        Point toAdd = Point.add(pnt, new Point(i, j));

                        if (toAdd.x >= 0 && toAdd.x < width && toAdd.y >= 0 && toAdd.y < height) {
                            specialUpdateList.Add(toAdd);
                        }
                    }
                }
                break;

            case SpecialType.ESpecialType.UMBRELLA:
                specialUpdateList.Add(pnt);

                while (specialUpdateList.Count < 10) {
                    int randomX = random.Next(0, width);
                    int randomY = random.Next(0, height);

                    Point newpnt = new Point(randomX, randomY);

                    Node newnode = GetNodeAtPoint(newpnt);
                    if(newnode == null) continue;

                    NodePiece nodePiece = newnode.GetPiece();
                    if (nodePiece == null || nodePiece.NodeVal is SpecialType) continue;

                    specialUpdateList.Add(newpnt);
                }
                break;

            case SpecialType.ESpecialType.WOOKONG:
                specialUpdateList.Add(pnt);
                for (int i = 0; i < width; ++i) {
                    for (int j = 0; j < height; ++j) {
                        Node node = GetNodeAtPoint(new Point(i, j));

                        if (node != null) {
                            NodePiece piece = node.GetPiece();

                            if (piece != null && piece.NodeVal is NormalType 
                                && piece.NodeVal.isEqual(new NormalType(NormalType.ENormalType.SANGA))) {
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
        if (GetValueAtPoint(one) is BlockedNodeType || GetValueAtPoint(one) is BlankType) return; //if first one's hole do nothing

        Node nodeOne = GetNodeAtPoint(one);
        NodePiece pieceOne = nodeOne.GetPiece();

        if (GetValueAtPoint(two) is not BlockedNodeType || GetValueAtPoint(two) is not BlankType) {
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
    List<Point> FindConnected(Point p, bool main, Point exchanged = null) {
        List<Point> connected = new List<Point>();

        INodeType val;

        if (exchanged != null) {
            val = GetValueAtPoint(exchanged);
        } else {
            val = GetValueAtPoint(p);
        }
        //if (val > pieces.Length + 1) return connected;
        


        foreach (Point dir in directions) {
            List<Point> line = new List<Point>();

            line.Add(p);

            int same = 0;

            for (int i = 1; i < 3; i++) {
                Point check = Point.add(p, Point.mult(dir, i));
                if (GetValueAtPoint(check) == null || check == exchanged) continue;
                if (GetValueAtPoint(check).isEqual(val)) {
                    line.Add(check);
                    same++;
                }
            }
            if (same > 1) {
                AddPoints(ref connected, line); //Add these points to the overarching connected list
            }
        }

        for (int i = 0; i < 2; i++) {
            Point[] check = {Point.add(p, directions[i]), Point.add(p, directions[i + 2])};
            List<Point> line = new List<Point>();
            line.Add(p);
            
            int same = 0;

            foreach (Point next in check) {
                if (GetValueAtPoint(next) == null || next == exchanged) continue;
                if (GetValueAtPoint(next).isEqual(val)) {
                    line.Add(next);
                    same++;
                }
            }
            if (same > 1) AddPoints(ref connected, line);
        }

        if (main) { //checks for other matches along the current match
            for (int i = 0; i < connected.Count; i++) {
                List<Point> more = FindConnected(connected[i], false);
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
    NormalType GetRandomPieceVal() {
        int val = random.Next(0, 100) / (100 / System.Enum.GetValues(typeof(NormalType.ENormalType)).Length);
        return new NormalType((NormalType.ENormalType) val);
    }

    /// <summary>
    /// Get weighted random value of selected value
    /// </summary>
    /// <param name="val">values wishes to be generated more often</param>
    /// <returns>weighted random normal piece type(starting with 1)</returns>
    NormalType GetWeightedRandomPieceVal(NormalType type) {
        int val = (int)type.TypeVal;
        if (val < 0 || val > pieces.Length) return GetRandomPieceVal();
        return new NormalType((NormalType.ENormalType)myWL[val - 1].Next());
    }

    protected INodeType GetValueAtPoint(Point p) {
        if (p.x < 0 || p.x >= width || p.y < 0 || p.y >= height) {
            return new BlockedNodeType();
        }
        //return GameManager.Instance.boardManager.Board[p.x, p.y].GetValue();
        INodeType tt = GameManager.Instance.boardManager.Board[p.x, p.y].GetValue();
        return tt;
    }

    protected void SetValueAtPoint(Point p, INodeType v) {
        GameManager.Instance.boardManager.Board[p.x, p.y].typeVal = v;
    }

    protected Node GetNodeAtPoint(Point p) {
        return GameManager.Instance.boardManager.Board[p.x, p.y];
    }

    /// <summary>
    /// Get Piece type value except ones in remove List
    /// </summary>
    /// <param name="remove">piece type values not wanted to be generated</param>
    /// <returns>new piece type value which is not in remove List</returns>
    INodeType GetNewValue(ref List<INodeType> remove) {
        var available = new List<INodeType>();
        
        foreach (int i in System.Enum.GetValues(typeof(NormalType.ENormalType))) {
            if(i==0 || i==-1) continue;
            available.Add(new NormalType((NormalType.ENormalType)i));
        }

        foreach (INodeType i in remove) available.Remove(i);

        if (available.Count <= 0) return new BlankType();
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
        return new Vector3(GameManager.Instance.boardManager.NodeSize / 2 + (GameManager.Instance.boardManager.NodeSize * (p.x - width / 2f)),
            -GameManager.Instance.boardManager.NodeSize / 2 - (GameManager.Instance.boardManager.NodeSize * (p.y - height / 2f)));
    }

    public void GameOver() {
        gameBoard.SetActive(false);
        killedBoard.SetActive(false);
        gameEndScreen.SetActive(true);

        audioController.Stop();

        finalScore.text = "Final Score : " + GameManager.Instance.scoreManager.Score;
        this.enabled = false;
    }
}
