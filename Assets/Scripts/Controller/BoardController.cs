using KaimiraGames;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class BoardController : MonoBehaviour {

    [SerializeField]
    GameControllerBase gameController;

    [SerializeField]
    ParticleController particleController;

    public Sprite[] pieces;
    public Sprite[] specialPieces;
    public ArrayLayout boardLayout;

    int[] blockSpawnOffset;

    [SerializeField]
    GameObject nodePiece;

    [SerializeField]
    GameObject killedPiece;

    [SerializeField]
    private float nodeSize = 2f;

    public float NodeSize {
        get { return nodeSize; }
    }


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
        NodePiece.onSpecialBlockPress.AddListener(AddSpecialQueue);
        //gameController = GetComponent<GameController>();
        //particleController = GetComponent<ParticleController>();
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

    public void boardUpdate(Board board) {
        //update moving pieces and store it for flip check
        var finishedUpdating = new List<NodePiece>();

        bool updatedone = true;
        foreach (var piece in board.updateList) {
            if (piece != null) {
                if (piece.UpdatePiece()) {
                    updatedone = false;
                } else {
                    finishedUpdating.Add(piece);
                }
                //Debug.Log($"still updating {piece.index.x}:{piece.index.y}");
            }
        }

        if (updatedone) {
            //if (finishedUpdating.Count > 0) {
            //    StringBuilder stringBuilder = new StringBuilder();
            //    stringBuilder.Append("finishedupdating : ");
            //    foreach (var piece in finishedUpdating) {
            //        stringBuilder.Append($"{piece.index.x}:{piece.index.y}, ");
            //    }
            //    Debug.Log(stringBuilder.ToString());
            //}
            //if (board.updateList.Count > 0) {
            //    StringBuilder stringBuilder = new StringBuilder();
            //    stringBuilder.Append("board.updateList : ");
            //    foreach (var piece in board.updateList) {
            //        stringBuilder.Append($"{piece.index.x}:{piece.index.y}, ");
            //    }
            //    Debug.Log(stringBuilder.ToString());
            //}

            //check if flipped pieces could make a match, else revert flip
            CheckMoveMatched(board, ref finishedUpdating);

            if (board.specialActivationQueue.Count > 0) {
                Board.SpecialActivationInfo actinfo = board.specialActivationQueue.Dequeue();
                ActivateSpecial(board, actinfo.pnt, actinfo.type);
            }

            //Update for special block activation
            SpecialBlockTick(board);

            
        }
    }

    /// <summary>
    /// Fill Board with random value, doesn't check if there are match
    /// Should call VerifyBoard after to check if it's in valid form
    /// </summary>
    void InitializeBoard(Board board) {
        board.BoardNode = new Node[board.Width, board.Height];

        for (int y = 0; y < board.Height; y++) {
            for (int x = 0; x < board.Width; x++) {
                board.BoardNode[x, y] = new Node((boardLayout.rows[y].row[x]) ? new SpecialType(SpecialType.ESpecialType.LIZASP) : GetRandomPieceVal(board), new Point(x, y));
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
                    nodeSize / 2 + (nodeSize * (x - board.Width / 2f)),
                    -nodeSize / 2 - (nodeSize * (y - board.Height / 2f))
                    );
                if (node.typeVal is SpecialType) {
                    piece.Initialize(node.typeVal,
                        board,
                        new Point(x, y), specialPieces[(int)(node.typeVal as SpecialType).TypeVal],
                        nodeSize,
                        board.Width,
                        board.Height);
                }
                else if (node.typeVal is NormalType) {
                    piece.Initialize(node.typeVal,
                        board,
                        new Point(x, y), pieces[(int)(node.typeVal as NormalType).TypeVal],
                        nodeSize,
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
        //if (finishedUpdating.Count > 0) {
        //    StringBuilder stringBuilder = new StringBuilder();
        //    stringBuilder.Append("CheckMoveMatched finished : ");
        //    foreach (var piece in finishedUpdating) {
        //        stringBuilder.Append($"{piece.index.x}:{piece.index.y}, ");
        //    }
        //    Debug.Log(stringBuilder.ToString());
        //}
        var specialDropList = new LinkedList<ValueTuple<SpecialType, int>>();
        bool updated = finishedUpdating.Count>0;
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
                //if (board.IsMultiplayer && board.IsPlayerBoard) Network.instance.SendFlip(piece, flippedPiece);
            }

            if (connected.Count == 0) {
                //if we didn't make a match
                if (wasFlipped) FlipPieces(board, piece.index, flippedPiece.index, false); //revert flip
            }
            else {
                //made a match
                foreach (var e in gameController.ProcessMatch(board, connected)) {
                    specialDropList.AddLast(e);
                }
            }

            board.flippedList.Remove(flip); //remove the flip after update
            board.updateList.Remove(piece); //done updating the piece
        }
        if(updated) DropNewPiece(board, specialDropList);
    }





    /// <summary>
    /// Drop NodePiece if there are hollows below. Generate empty block on top if needed.
    /// </summary>
    /// <param name="specialBlockList">List of infos of special block to generate. Tuple(specialblock type, speical generate xval)</param>

    public void DropNewPiece(Board board, LinkedList<ValueTuple<SpecialType, int>> specialBlockList = null) {
        //if (specialBlockList != null && specialBlockList.Count>0) {
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append("specialblocklist : ");
        //    foreach (var piece in specialBlockList) {
        //        sb.Append($"{piece.Item1.ToString()}:{piece.Item2}, ");
        //    }
        //    Debug.Log(sb.ToString());
        //}
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
                        board.updateList.AddLast(upperPiece);

                        upperNode.SetPiece(null); //Replace the upper piece to blank
                    }
                    else {
                        //if above is top wall or blank create new piece and drop it from the top
                        NormalType newNormalType = GetRandomPieceVal(board);
                        int[] nearColumn = { -2, -1, 1, 2 };
                        var nearValues = new List<INodeType>();

                        foreach (int diff in nearColumn) {
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
                                newNormalType = GetWeightedRandomPieceVal(board, nearValues[i] as NormalType);
                                break;
                            }
                        }

                        //y=-1 is above top line, fills[x] = offset up, as we are dropping more than 1 piece
                        Point fallPoint = new Point(x, -1 - blockSpawnOffset[x]);

                        //create new piece
                        GameObject obj = Instantiate(nodePiece, board.GameBoard.transform);
                        NodePiece piece = obj.GetComponent<NodePiece>();

                        if (specialBlockList != null && specialBlockList.Count > 0) {
                            foreach (ValueTuple<SpecialType, int> specialblock in specialBlockList) {
                                if (specialblock.Item2 == x) {
                                    piece.Initialize(specialblock.Item1,
                                        board,
                                        curPoint,
                                        specialPieces[(int)specialblock.Item1.TypeVal],
                                        nodeSize,
                                        board.Width,
                                        board.Height);

                                    specialBlockList.Remove(specialblock);
                                    break;
                                }
                                else {
                                    piece.Initialize(newNormalType,
                                        board,
                                        curPoint,
                                        pieces[(int)newNormalType.TypeVal],
                                        nodeSize,
                                        board.Width,
                                        board.Height);
                                }
                            }
                        }
                        else {
                            piece.Initialize(newNormalType,
                                board,
                                curPoint,
                                pieces[(int)newNormalType.TypeVal],
                                nodeSize,
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
        if (board.specialUpdateList.Count == 0) return;
        //Debug.Log("SpecialBlockTick");
        //for (int i = 0; i < board.specialUpdateList.Count; ++i) {
        //    KillPiece(board, board.specialUpdateList[i], false);

        //    Node node = board.GetNodeAtPoint(board.specialUpdateList[i]);
        //    NodePiece nodePiece = node.GetPiece();

        //    if (nodePiece != null) {
        //        Destroy(nodePiece.gameObject);
        //    }
        //    node.SetPiece(null);
        //}
        var specialUpdateListCopy = new List<Point>(board.specialUpdateList);
        foreach(var point in specialUpdateListCopy) {
            KillPiece(board, point, false);

            Node node = board.GetNodeAtPoint(point);
            NodePiece nodePiece = node.GetPiece();

            if (nodePiece != null) {
                //Debug.Log($"Sp Destroy : {nodePiece.index.x}:{nodePiece.index.y}");
                Destroy(nodePiece.gameObject);
            }
            node.SetPiece(null);
        }

        DropNewPiece(board);
        board.specialUpdateList.Clear();
        gameController.SpecialBlockPressed(board);
    }

    public void AddSpecialQueue(Board board, Point pnt, SpecialType type, bool collateral=false) {
        board.specialActivationQueue.Enqueue(new Board.SpecialActivationInfo(pnt, type));
        //Debug.Log($"SpecialQ Added {pnt.x}:{pnt.y} type:{type.TypeVal}");
        if (board.IsPlayerBoard && Network.instance != null && !collateral) Network.instance.SendSpecialPressed(pnt, type);
    }

    /// <summary>
    /// Reacts to special blocks pressed and execute it's implementation
    /// </summary>
    /// <param name="pnt">Special block position index</param>
    /// <param name="val">Special block type 0 ~ </param>
    public void ActivateSpecial(Board board, Point pnt, SpecialType val) {
        if (val == null) return;
        //Debug.Log($"activate special {pnt.x}:{pnt.y} on {val.TypeVal.ToString()}");
        KillPiece(board, pnt, false, true);
        Node n = board.GetNodeAtPoint(pnt);
        NodePiece np = n.GetPiece();
        if (np != null) {
            Destroy(np.gameObject);
        }
        n.SetPiece(null);

        switch (val.TypeVal) {
            case SpecialType.ESpecialType.SITRISP:
                for (int i = 0; i < board.Width; ++i) {
                    for (int j = 0; j < board.Height; ++j) {
                        if(pnt.x!=i && pnt.y!=j) //
                            board.specialUpdateList.AddLast(new Point(i, j));
                    }
                }
                break;

            case SpecialType.ESpecialType.DAVISP:
                //board.specialUpdateList.AddLast(pnt);

                for (int i = 1; i <= Math.Max(board.Height, board.Width); i++) {
                    Point[] dir = { new Point(1, -1), new Point(1, 1), new Point(-1, 1), new Point(-1, -1) };
                    foreach (Point p in dir) {
                        Point toAdd = Point.add(pnt, Point.mult(p, i));
                        if (toAdd.x >= 0 && toAdd.x < board.Width && toAdd.y >= 0 && toAdd.y < board.Height) {
                            board.specialUpdateList.AddLast(toAdd);
                        }
                    }
                }
                break;

            case SpecialType.ESpecialType.LIZASP:
                for (int i = 0; i < board.Width; ++i) {
                    if (i == pnt.x) continue; //
                    board.specialUpdateList.AddLast(new Point(i, pnt.y));
                }

                for (int i = 0; i < board.Height; ++i) {
                    if (i == pnt.y) continue;
                    board.specialUpdateList.AddLast(new Point(pnt.x, i));
                }
                break;

            case SpecialType.ESpecialType.MONASP:
                for (int i = -2; i <= 2; ++i) {
                    for (int j = -2; j <= 2; ++j) {
                        if(i==0 && j==0) continue; //
                        Point toAdd = Point.add(pnt, new Point(i, j));

                        if (toAdd.x >= 0 && toAdd.x < board.Width && toAdd.y >= 0 && toAdd.y < board.Height) {
                            board.specialUpdateList.AddLast(toAdd);
                        }
                    }
                }
                break;

            case SpecialType.ESpecialType.MITRASP:
                //board.specialUpdateList.AddLast(pnt);

                while (board.specialUpdateList.Count < 10) {
                    int randomX = board.RngNext(0, board.Width);
                    int randomY = board.RngNext(0, board.Height);

                    Point newpnt = new Point(randomX, randomY);

                    Node newnode = board.GetNodeAtPoint(newpnt);
                    if (newnode == null) continue;

                    NodePiece nodePiece = newnode.GetPiece();
                    if (nodePiece == null || nodePiece.NodeVal is SpecialType) continue;

                    board.specialUpdateList.AddLast(newpnt);
                }
                break;

            case SpecialType.ESpecialType.SANGASP:
                //board.specialUpdateList.AddLast(pnt);
                for (int i = 0; i < board.Width; ++i) {
                    for (int j = 0; j < board.Height; ++j) {
                        Node node = board.GetNodeAtPoint(new Point(i, j));

                        if (node != null) {
                            NodePiece piece = node.GetPiece();

                            if (piece != null && piece.NodeVal is NormalType
                                && piece.NodeVal.isEqual(new NormalType(NormalType.ENormalType.SANGA))) {
                                board.specialUpdateList.AddLast(new Point(i, j));
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

        //for (int i = 0; i < board.flippedList.Count; i++) {
        //    if (board.flippedList[i].GetOtherPiece(p) != null) {
        //        flip = board.flippedList[i];
        //        break;
        //    }
        //}
        foreach(var piece in board.flippedList) {
            if (piece.GetOtherPiece(p) != null) {
                flip = piece;
                break;
            }
        }

        return flip;
    }

    /// <summary>
    /// Doesn't really kill piece. Instantiates killpiece prefabs in killboard and make explosion effect on popped pieces
    /// </summary>
    /// <param name="p">Piece which is killed and to be dropped</param>
    public void KillPiece(Board board, Point p, bool bwithParticle, bool specialTrigger=false) {
        INodeType val = board.GetValueAtPoint(p);

        if (val is BlockedNodeType || val is BlankType) return;

        GameObject kill = GameObject.Instantiate(killedPiece, board.KilledBoard.transform);
        KilledPiece kPiece = kill.GetComponent<KilledPiece>();
        Vector2 pointPos = board.getPositionFromPoint(p);
        if (bwithParticle || val is SpecialType) {
            particleController.KillParticle(board, pointPos, val);
        }

        //if (kPiece != null && val is NormalType && val - 1 < pieces.Length) {
        if (kPiece != null && val is NormalType) {
            kPiece.Initialize(board, pieces[(int)(val as NormalType).TypeVal], pointPos, nodeSize);
            board.killedPieceList.Add(kPiece);
            //} else if (kPiece != null && val < specialPieces.Length) {
        }
        else if (kPiece != null && val is SpecialType) {
            kPiece.Initialize(board, specialPieces[(int)(val as SpecialType).TypeVal], pointPos, nodeSize);
            if(!specialTrigger) {
                //Debug.Log($"collateral sp trigger {p.x}:{p.y} on {(val as SpecialType).TypeVal}");
                AddSpecialQueue(board, p, (val as SpecialType), true);
            }
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

            if (main) board.flippedList.AddLast(new FlippedPieces(pieceOne, pieceTwo));

            board.updateList.AddLast(pieceOne);
            board.updateList.AddLast(pieceTwo);
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
        //Debug.Log($"connect check : {p.x}:{p.y}");
        //foreach (Point pnt in connected) {
        //    Debug.Log($"{pnt.x}:{pnt.y}");
        //}
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
        board.updateList.AddLast(piece);
    }

    /// <summary>
    /// get random normal piece value
    /// </summary>
    /// <returns>random normal piece (starting with 1)</returns>
    NormalType GetRandomPieceVal(Board board) {
        int val = board.RngNext(0, 100) / (100 / System.Enum.GetValues(typeof(NormalType.ENormalType)).Length);
        //Debug.Log("random piece:" + val);
        return new NormalType((NormalType.ENormalType)val);
    }

    /// <summary>
    /// Get weighted random value of selected value
    /// </summary>
    /// <param name="val">values wishes to be generated more often</param>
    /// <returns>weighted random normal piece type(starting with 1)</returns>
    NormalType GetWeightedRandomPieceVal(Board board, NormalType type) {
        int val = (int)type.TypeVal;
        if (val < 0 || val > pieces.Length) {
            return GetRandomPieceVal(board);
        }
        int weightedval = weightedLists[val - 1].Next();
        Debug.Log("weighted random piece:" + val);
        return new NormalType((NormalType.ENormalType)weightedval);
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
        return available[board.RngNext(0, available.Count)];
    }

    /// <summary>
    /// Reacts to killpiece prefab unrendered on screen and remove it from killedlist
    /// </summary>
    /// <param name="killedPiece"></param>
    public void KilledPieceRemoved(Board board, KilledPiece killedPiece) {
        board.killedPieceList.Remove(killedPiece);
    }
}
