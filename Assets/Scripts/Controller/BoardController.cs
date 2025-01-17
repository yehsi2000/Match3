using KaimiraGames;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardController : MonoBehaviour {

    [SerializeField]
    GameControllerBase gameController;

    public Sprite[] pieces;
    public Sprite[] specialPieces;
    public ArrayLayout boardLayout;

    int[] blockSpawnOffset;

    [SerializeField]
    GameObject nodePiece;

    [SerializeField]
    GameObject killedPiece;

    ParticleController particleController;
    List<WeightedList<int>> weightedLists;

    [HideInInspector]
    static readonly Point[] directions = {
        Point.up,
        Point.right,
        Point.down,
        Point.left
    };

    private void Awake() {
        KilledPiece.onKilledPieceRemove.AddListener(KilledPieceRemoved);
        NodePiece.onSpecialBlockPress.AddListener(ActivateSpecial);
        gameController = GetComponent<GameController>();
        particleController = GetComponent<ParticleController>();
    }

    public void InitBoard(Board board) {
        weightedLists = new List<WeightedList<int>>();
        blockSpawnOffset = new int[board.Width];
        for (int i = 0; i < System.Enum.GetValues(typeof(NormalType.ENormalType)).Length; ++i) {
            var newWL = new WeightedList<int>(board.rng);
            for (int j = 0; j < System.Enum.GetValues(typeof(NormalType.ENormalType)).Length; ++j) {
                if (j == i) newWL.Add(j, gameController.AutoBlockWeightMultiplier);
                else newWL.Add(j, 100);
            }
            weightedLists.Add(newWL);
        }

        do {
            InitializeBoard(board);
            InstantiateBoard(board);
        } while (isDeadlocked(board));
    }

    public void boardUpdate(Board board)
    {
        //update moving pieces and store it for flip check
        var finishedUpdating = new List<NodePiece>();

        for (int i = 0; i < board.updateList.Count; i++) {
            NodePiece piece = board.updateList[i];
            if (piece != null && !piece.UpdatePiece()) finishedUpdating.Add(piece);
        }

        //Update for special block activation
        SpecialBlockTick(board);

        //check if flipped pieces could make a match, else revert flip
        CheckMoveMatched(board, ref finishedUpdating);
    }

    /// <summary>
    /// Fill Board with random value, doesn't check if there are match
    /// Should call VerifyBoard after to check if it's in valid form
    /// </summary>
    void InitializeBoard(Board board) {
        board.BoardNode = new Node[board.Width, board.Height];

        for (int y = 0; y < board.Height; y++) {
            for (int x = 0; x < board.Width; x++) {
                board.BoardNode[x, y] = new Node((boardLayout.rows[y].row[x]) ? new SpecialType(SpecialType.ESpecialType.LIZA) : GetRandomPieceVal(board), new Point(x, y));
            }
        }

        VerifyBoard(board);
    }

    bool isDeadlocked(Board board) {
        for (int x = 0; x < board.Width; x++) {
            for (int y = 0; y < board.Height; y++) {
                Point p = new Point(x, y);

                INodeType val = board.GetValueAtPoint(p);
                if (val is BlankType || val is BlockedNodeType) {
                    continue;
                }

                if (y < board.Height - 1) {
                    Point down = new Point(x, y + 1);
                    if (FindConnected(board, p, false, down).Count > 0) return false;
                }

                if (x < board.Width - 1) {
                    Point right = new Point(x + 1, y);
                    if (FindConnected(board, p, false, right).Count > 0) return false;
                }
            }
        }
        return true;
    }


    /// <summary>
    /// Check if there's any connected block in current board. If there is, remove it and regenerated block in place
    /// </summary>
    void VerifyBoard(Board board) {
        for (int x = 0; x < board.Width; x++) {
            for (int y = 0; y < board.Height; y++) {
                Point p = new Point(x, y);

                INodeType pointVal = board.GetValueAtPoint(p);
                if (pointVal.isEqual(new BlankType()) ||
                    pointVal.isEqual(new BlockedNodeType())) {
                    continue;
                }

                var excludeList = new List<INodeType>();

                while (FindConnected(board, p, true).Count > 0) {
                    pointVal = board.GetValueAtPoint(p);

                    if (!excludeList.Contains(pointVal)) excludeList.Add(pointVal);

                    board.SetValueAtPoint(p, GetNewValue(board, ref excludeList));
                }
            }
        }
    }

    /// <summary>
    /// Generate and place instances of nodepiece prefabs in node-size-aligned position
    /// </summary>
    void InstantiateBoard(Board board) {
        for (int x = 0; x < board.Width; x++) {
            for (int y = 0; y < board.Height; y++) {
                Node node = board.GetNodeAtPoint(new Point(x, y));
                if (node.typeVal.isEqual(new BlockedNodeType())) {
                    continue;
                }

                GameObject p = Instantiate(nodePiece, board.GameBoard.transform);
                NodePiece piece = p.GetComponent<NodePiece>();

                //RectTransform rect = p.GetComponent<RectTransform>();

                p.transform.localPosition = new Vector3(
                    board.NodeSize / 2 + (board.NodeSize * (x - board.Width / 2f)),
                    -board.NodeSize / 2 - (board.NodeSize * (y - board.Height / 2f))
                    );
                if (node.typeVal is SpecialType) {
                    piece.Initialize(node.typeVal,
                        board,
                        new Point(x, y), specialPieces[(int)(node.typeVal as SpecialType).TypeVal],
                        board.NodeSize,
                        board.Width,
                        board.Height);
                }
                else if(node.typeVal is NormalType) {
                    piece.Initialize(node.typeVal,
                        board,
                        new Point(x, y), pieces[(int)(node.typeVal as NormalType).TypeVal],
                        board.NodeSize,
                        board.Width,
                        board.Height);
                }
                node.SetPiece(piece);
            }
        }
    }

    public void DisableBoards(Board boardManager) {
        boardManager.GameBoard.SetActive(false);
        boardManager.KilledBoard.SetActive(false);
    }

    

    void CheckMoveMatched(Board board, ref List<NodePiece> finishedUpdating) {
        for (int i = 0; i < finishedUpdating.Count; i++) {

            NodePiece piece = finishedUpdating[i]; //updated piece
            FlippedPieces flip = GetFlipped(board, piece); //flipped by updated piece
            NodePiece flippedPiece = null;

            int x = piece.index.x; //"x"th column

            blockSpawnOffset[x] = Mathf.Clamp(blockSpawnOffset[x] - 1, 0, board.Width);

            //check if user controlled piece made a match
            List<Point> connected = FindConnected(board, piece.index, true);

            bool wasFlipped = (flip != null);

            if (wasFlipped) {
                flippedPiece = flip.GetOtherPiece(piece);
                AddPoints(ref connected, FindConnected(board, flippedPiece.index, true));
            }

            if (connected.Count == 0) {
                //if we didn't make a match
                if (wasFlipped) FlipPieces(board, piece.index, flippedPiece.index, false); //revert flip
            }
            else {
                //made a match
                gameController.ProcessMatch(board, connected);
            }

            board.flippedList.Remove(flip); //remove the flip after update
            board.updateList.Remove(piece); //done updating the piece
        }
    }


    


    /// <summary>
    /// Drop NodePiece if there are hollows below. Generate empty block on top if needed.
    /// </summary>
    /// <param name="specialBlockList">List of infos of special block to generate. Tuple(specialblock type, speical generate xval)</param>

    public void DropNewPiece(Board board, List<ValueTuple<SpecialType, int>> specialBlockList = null) {
        for (int x = 0; x < board.Width; x++) {
            for (int y = board.Height - 1; y >= 0; y--) {

                //iterate from the top to bottom
                Point curPoint = new Point(x, y);
                Node curNode = board.GetNodeAtPoint(curPoint);
                INodeType curVal = board.GetValueAtPoint(curPoint);

                if (curVal is not BlankType) continue; //find blank space where connected block disappeared

                //y=-1 is above the top line
                //for pieces above this blank drop down
                for (int ny = y - 1; ny >= -1; ny--) {
                    Point next = new Point(x, ny);
                    INodeType nextVal = board.GetValueAtPoint(next);

                    if (nextVal is BlankType) continue;

                    if (nextVal is not BlockedNodeType) {
                        //if we did not hit top(or intentional hole) then drag upper ones down to the hole
                        Node upperNode = board.GetNodeAtPoint(next);
                        NodePiece upperPiece = upperNode.GetPiece();

                        //Set the blank to upper piece
                        curNode.SetPiece(upperPiece);
                        board.updateList.Add(upperPiece);

                        upperNode.SetPiece(null); //Replace the upper piece to blank
                    }
                    else {
                        //if above is top wall or blank create new piece and drop it from the top
                        NormalType newVal = GetRandomPieceVal(board);
                        int[] nearRow = { -2, -1, 1, 2 };
                        var nearValues = new List<INodeType>();

                        foreach (int diff in nearRow) {
                            if (x + diff >= board.Width ||
                                x + diff < 0 ||
                                y + blockSpawnOffset[x] < 0 ||
                                y + blockSpawnOffset[x] >= board.Height)
                                continue;

                            NodePiece np = board.BoardNode[x + diff, y].GetPiece();

                            if (np != null) {
                                nearValues.Add(np.NodeVal);
                            }
                        }

                        for (int i = 0; i < nearValues.Count - 1; ++i) {
                            if (nearValues[i] == nearValues[i + 1] && nearValues[i] is not BlockedNodeType) {
                                newVal = GetWeightedRandomPieceVal(board, nearValues[i] as NormalType);
                                break;
                            }
                        }

                        //y=-1 is above top line, fills[x] = offset up, as we are dropping more than 1 piece
                        Point fallPoint = new Point(x, -1 - blockSpawnOffset[x]);

                        //create new piece
                        GameObject obj = Instantiate(nodePiece, board.GameBoard.transform);
                        NodePiece piece = obj.GetComponent<NodePiece>();

                        if (specialBlockList != null && specialBlockList.Count > 0) {
                            for (int i = 0; i < specialBlockList.Count; ++i) {
                                if (specialBlockList[i].Item2 == x) {
                                    piece.Initialize(specialBlockList[i].Item1,
                                        board,
                                        curPoint,
                                        specialPieces[(int)specialBlockList[i].Item1.TypeVal],
                                        board.NodeSize,
                                        board.Width,
                                        board.Height);

                                    specialBlockList.RemoveAt(i);
                                    break;

                                }
                                else {
                                    piece.Initialize(newVal,
                                        board,
                                        curPoint,
                                        pieces[(int)(newVal as NormalType).TypeVal],
                                        board.NodeSize,
                                        board.Width,
                                        board.Height);
                                }
                            }
                        }
                        else {
                            piece.Initialize(newVal,
                                board,
                                curPoint,
                                pieces[(int)newVal.TypeVal],
                                board.NodeSize,
                                board.Width,
                                board.Height);
                        }

                        //put new piece on top so it looks like falling down
                        piece.transform.position = board.getPositionFromPoint(fallPoint);

                        Node hole = board.GetNodeAtPoint(curPoint);
                        hole.SetPiece(piece);

                        ResetPiece(board, piece);
                        blockSpawnOffset[x]++; //move offset upper for more piece to drop
                    }
                    break;
                }
            }
        }

    }

    void SpecialBlockTick(Board board) {
        if (board.specialUpdateList.Count > 0) {
            for (int i = 0; i < board.specialUpdateList.Count; ++i) {
                KillPiece(board, board.specialUpdateList[i], false);

                Node node = board.GetNodeAtPoint(board.specialUpdateList[i]);
                NodePiece nodePiece = node.GetPiece();

                

                if (nodePiece != null) {
                    Destroy(nodePiece.gameObject);
                }
                node.SetPiece(null);
            }

            DropNewPiece(board);
            board.specialUpdateList.Clear();
            gameController.SpecialBlockPressed();
        }
    }

    /// <summary>
    /// Reacts to special blocks pressed and execute it's implementation
    /// </summary>
    /// <param name="pnt">Special block position index</param>
    /// <param name="val">Special block type 0 ~ </param>
    void ActivateSpecial(Board board, Point pnt, SpecialType val) {
        if (val == null) return;
        switch (val.TypeVal) {
            case SpecialType.ESpecialType.SITRI:
                for (int i = 0; i < board.Width; ++i) {
                    for (int j = 0; j < board.Height; ++j) {
                        board.specialUpdateList.Add(new Point(i, j));
                    }
                }
                break;

            case SpecialType.ESpecialType.DAVI:
                board.specialUpdateList.Add(pnt);

                for (int i = 1; i <= Math.Max(board.Height, board.Width); i++) {
                    Point[] dir = { new Point(1, -1), new Point(1, 1), new Point(-1, 1), new Point(-1, -1) };
                    foreach (Point p in dir) {
                        Point toAdd = Point.add(pnt, Point.mult(p, i));
                        if (toAdd.x >= 0 && toAdd.x < board.Width && toAdd.y >= 0 && toAdd.y < board.Height) {
                            board.specialUpdateList.Add(toAdd);
                        }
                    }
                }
                break;

            case SpecialType.ESpecialType.LIZA:
                for (int i = 0; i < board.Width; ++i) {
                    board.specialUpdateList.Add(new Point(i, pnt.y));
                }

                for (int i = 0; i < board.Height; ++i) {
                    if (i == pnt.y) continue; //prevent dual update for pressed
                    board.specialUpdateList.Add(new Point(pnt.x, i));
                }
                break;

            case SpecialType.ESpecialType.MONA:
                for (int i = -2; i <= 2; ++i) {
                    for (int j = -2; j <= 2; ++j) {
                        Point toAdd = Point.add(pnt, new Point(i, j));

                        if (toAdd.x >= 0 && toAdd.x < board.Width && toAdd.y >= 0 && toAdd.y < board.Height) {
                            board.specialUpdateList.Add(toAdd);
                        }
                    }
                }
                break;

            case SpecialType.ESpecialType.UMBRELLA:
                board.specialUpdateList.Add(pnt);

                while (board.specialUpdateList.Count < 10) {
                    int randomX = board.rng.Next(0, board.Width);
                    int randomY = board.rng.Next(0, board.Height);

                    Point newpnt = new Point(randomX, randomY);

                    Node newnode = board.GetNodeAtPoint(newpnt);
                    if (newnode == null) continue;

                    NodePiece nodePiece = newnode.GetPiece();
                    if (nodePiece == null || nodePiece.NodeVal is SpecialType) continue;

                    board.specialUpdateList.Add(newpnt);
                }
                break;

            case SpecialType.ESpecialType.WOOKONG:
                board.specialUpdateList.Add(pnt);
                for (int i = 0; i < board.Width; ++i) {
                    for (int j = 0; j < board.Height; ++j) {
                        Node node = board.GetNodeAtPoint(new Point(i, j));

                        if (node != null) {
                            NodePiece piece = node.GetPiece();

                            if (piece != null && piece.NodeVal is NormalType
                                && piece.NodeVal.isEqual(new NormalType(NormalType.ENormalType.SANGA))) {
                                board.specialUpdateList.Add(new Point(i, j));
                            }
                        }
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Get flipped piece of current selected NodePiece
    /// </summary>
    /// <param name="p">selected piece</param>
    /// <returns></returns>
    FlippedPieces GetFlipped(Board board, NodePiece p) {
        FlippedPieces flip = null;

        for (int i = 0; i < board.flippedList.Count; i++) {
            if (board.flippedList[i].GetOtherPiece(p) != null) {
                flip = board.flippedList[i];
                break;
            }
        }

        return flip;
    }

    /// <summary>
    /// Doesn't really kill piece. Instantiates killpiece prefabs in killboard and make explosion effect on popped pieces
    /// </summary>
    /// <param name="p">Piece which is killed and to be dropped</param>
    public void KillPiece(Board board, Point p, bool bwithParticle) {
        INodeType val = board.GetValueAtPoint(p);

        if (val is BlockedNodeType || val is BlankType) return;

        GameObject kill = GameObject.Instantiate(killedPiece, board.KilledBoard.transform);
        KilledPiece kPiece = kill.GetComponent<KilledPiece>();
        Vector2 pointPos = board.getPositionFromPoint(p);
        if (bwithParticle || val is SpecialType) {
            particleController.KillParticle(pointPos, val);
        }

        //if (kPiece != null && val is NormalType && val - 1 < pieces.Length) {
        if (kPiece != null && val is NormalType) {
            kPiece.Initialize(board, pieces[(int)(val as NormalType).TypeVal], pointPos, board.NodeSize);
            board.killedPieceList.Add(kPiece);
            //} else if (kPiece != null && val < specialPieces.Length) {
        }
        else if (kPiece != null && val is SpecialType) {
            kPiece.Initialize(board, specialPieces[(int)(val as SpecialType).TypeVal], pointPos, board.NodeSize);
            ActivateSpecial(board, p, (val as SpecialType));
            board.killedPieceList.Add(kPiece);
        }
    }

    /// <summary>
    /// Flip two piece
    /// </summary>
    /// <param name="one">piece to be flipped</param>
    /// <param name="two">piece to be flipped</param>
    /// <param name="main">is this called in user action not revert flip?</param>
    public void FlipPieces(Board board, Point one, Point two, bool main) {
        if (board.GetValueAtPoint(one) is BlockedNodeType || board.GetValueAtPoint(one) is BlankType) return; //if first one's hole do nothing

        Node nodeOne = board.GetNodeAtPoint(one);
        NodePiece pieceOne = nodeOne.GetPiece();

        if (board.GetValueAtPoint(two) is not BlockedNodeType || board.GetValueAtPoint(two) is not BlankType) {
            //second one's also not hole
            Node nodeTwo = board.GetNodeAtPoint(two);
            NodePiece pieceTwo = nodeTwo.GetPiece();

            nodeOne.SetPiece(pieceTwo);
            nodeTwo.SetPiece(pieceOne);

            if (main) board.flippedList.Add(new FlippedPieces(pieceOne, pieceTwo));

            board.updateList.Add(pieceOne);
            board.updateList.Add(pieceTwo);
        }
        else {
            ResetPiece(board, pieceOne); //second one's hole, reset first one's position
        }
    }

    /// <summary>
    /// Find and return all blocks connected with current block.
    /// </summary>
    /// <param name="p">Initial block position</param>
    /// <param name="main">is this called in Update() function, not recursively in itself?</param>
    /// <returns></returns>
    List<Point> FindConnected(Board board, Point p, bool main, Point exchanged = null) {
        List<Point> connected = new List<Point>();

        INodeType val;

        if (exchanged != null) {
            val = board.GetValueAtPoint(exchanged);
        }
        else {
            val = board.GetValueAtPoint(p);
        }
        //if (val > pieces.Length + 1) return connected;



        foreach (Point dir in directions) {
            List<Point> line = new List<Point>();

            line.Add(p);

            int same = 0;

            for (int i = 1; i < 3; i++) {
                Point check = Point.add(p, Point.mult(dir, i));
                if (board.GetValueAtPoint(check) == null || check == exchanged) continue;
                if (board.GetValueAtPoint(check).isEqual(val)) {
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
                if (board.GetValueAtPoint(next) == null || next == exchanged) continue;
                if (board.GetValueAtPoint(next).isEqual(val)) {
                    line.Add(next);
                    same++;
                }
            }
            if (same > 1) AddPoints(ref connected, line);
        }

        if (main) { //checks for other matches along the current match
            for (int i = 0; i < connected.Count; i++) {
                List<Point> more = FindConnected(board, connected[i], false);
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
    public void ResetPiece(Board board, NodePiece piece) {
        piece.ResetPosition();
        board.updateList.Add(piece);
    }

    /// <summary>
    /// get random normal piece value
    /// </summary>
    /// <returns>random normal piece (starting with 1)</returns>
    NormalType GetRandomPieceVal(Board board) {
        int val = board.rng.Next(0, 100) / (100 / System.Enum.GetValues(typeof(NormalType.ENormalType)).Length);
        return new NormalType((NormalType.ENormalType)val);
    }

    /// <summary>
    /// Get weighted random value of selected value
    /// </summary>
    /// <param name="val">values wishes to be generated more often</param>
    /// <returns>weighted random normal piece type(starting with 1)</returns>
    NormalType GetWeightedRandomPieceVal(Board board, NormalType type) {
        int val = (int)type.TypeVal;
        if (val < 0 || val > pieces.Length) return GetRandomPieceVal(board);
        return new NormalType((NormalType.ENormalType)weightedLists[val - 1].Next());
    }

    /// <summary>
    /// Get Piece type value except ones in remove List
    /// </summary>
    /// <param name="remove">piece type values not wanted to be generated</param>
    /// <returns>new piece type value which is not in remove List</returns>
    INodeType GetNewValue(Board board, ref List<INodeType> remove) {
        var available = new List<INodeType>();

        foreach (int i in System.Enum.GetValues(typeof(NormalType.ENormalType))) {
            if (i == 0 || i == -1) continue;
            available.Add(new NormalType((NormalType.ENormalType)i));
        }

        foreach (INodeType i in remove) available.Remove(i);

        if (available.Count <= 0) return new BlankType();
        return available[board.rng.Next(0, available.Count)];
    }

    /// <summary>
    /// Reacts to killpiece prefab unrendered on screen and remove it from killedlist
    /// </summary>
    /// <param name="killedPiece"></param>
    public void KilledPieceRemoved(Board board, KilledPiece killedPiece) {
        board.killedPieceList.Remove(killedPiece);
    }
}
