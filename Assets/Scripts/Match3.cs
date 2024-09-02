using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Match3 : MonoBehaviour
{
    public ArrayLayout boardLayout;

    [Header("UI Elements")]
    public Sprite[] pieces;
    public RectTransform gameBoard;
    public RectTransform killedBoard;
    public GameObject gameEndScreen;
    public ScoreBoard scoreBoard;
    public TMP_Text finalScore;
    public Timer timer;

    [Header("Prefabs")]
    public GameObject nodePiece;
    public GameObject killedPiece;

    [Header("Score")]
    public int score;
    public int perPieceScore = 100;

    [SerializeField]
    public int nodeSize = 80;

    int width = 14;
    int height = 9;
    int[] fills;
    Node[,] board;
    

    List<NodePiece> update;
    List<FlippedPieces> flipped;
    List<NodePiece> dead;
    List<KilledPiece> killed;

    System.Random random;

    void Start()
    {
        StartGame();
        gameEndScreen.SetActive(false);
    }

    void Update(){
        //update timer
        if (!timer.UpdateTimer()) {
            gameEndScreen.SetActive(true);
            finalScore.text = "Final Score : " + score;
            this.enabled = false;
        }

        //update moving pieces and store it for flip check
        List<NodePiece> finishedUpdating = new List<NodePiece>();
        for(int i = 0; i < update.Count; i++){
            NodePiece piece = update[i];
            if (!piece.UpdatePiece()) finishedUpdating.Add(piece);
        }

        //check if flipped pieces could make a match, else revert flip
        for (int i = 0; i < finishedUpdating.Count; i++){
            NodePiece piece = finishedUpdating[i]; //updated piece
            FlippedPieces flip = GetFlipped(piece); //flipped by updated piece
            NodePiece flippedPiece = null;
            
            int x = piece.index.x; //"x"th column
            fills[x] = Mathf.Clamp(fills[x]-1, 0, width);

            List<Point> connected = isConnected(piece.index, true); //check if user controlled piece made a match
            bool wasFlipped = (flip != null);

            if (wasFlipped) {
                flippedPiece = flip.GetOtherPiece(piece);
                AddPoints(ref connected, isConnected(flippedPiece.index, true));
            }

            if (connected.Count == 0){
                //if we didn't make a match
                if (wasFlipped)
                    FlipPieces(piece.index, flippedPiece.index, false); //revert flip
            }
            else {
                //made a match
                foreach (Point pnt in connected) {  //remove the node pieces connected
                    KillPiece(pnt);
                    Node node = getNodeAtPoint(pnt);
                    score += perPieceScore;
                    NodePiece nodePiece = node.GetPiece();
                    if(nodePiece != null){
                        nodePiece.gameObject.SetActive(false);
                        dead.Add(nodePiece);
                    }
                    node.SetPiece(null);
                }

                ApplyGravityToBoard();
            }

            flipped.Remove(flip); //remove the flip after update
            update.Remove(piece);
        }
        scoreBoard.UpdateScore(score);
    }

    void ApplyGravityToBoard(){
        for(int x= 0; x<width; x++){
            for(int y = height-1; y>=0; y--){
                Point p = new Point(x,y);
                Node node = getNodeAtPoint(p);
                int val = getValueAtPoint(p);
                if(val!=0) continue; //if not a hole , do nothing
                for(int ny = y-1; ny >= -1; ny--){
                    Point next = new Point(x, ny) ;
                    int nextVal = getValueAtPoint(next);
                    if(nextVal==0) continue;
                    if(nextVal != -1) { //if we did not hit end then use this to fill the current hole
                        Node got = getNodeAtPoint(next);
                        NodePiece piece = got.GetPiece();

                        //Set the hole
                        node.SetPiece(piece);
                        update.Add(piece);

                        //Replace the hole
                        got.SetPiece(null);
                    } else { // use dead ones or create new pieces to fill holes(hit a-1) only if we choose to
                        int newVal = fillPiece();
                        NodePiece piece;
                        Point fallPoint = new Point(x, -1 - fills[x]);

                        if (dead.Count > 0) {
                            NodePiece revived = dead[0];
                            revived.gameObject.SetActive(true);
                            piece = revived;
                            dead.RemoveAt(0);

                        } else
                        { //shouldnt be called if there's hole from the start and you unlock by doing something
                            GameObject obj = Instantiate(nodePiece, gameBoard);
                            NodePiece n = obj.GetComponent<NodePiece>();
                            piece = n;
                        }

                        piece.Initialize(newVal, p, pieces[newVal - 1], nodeSize);
                        piece.rect.anchoredPosition = GetPositionFromPoint(fallPoint); //put new piece on top so it looks like falling down

                        Node hole = getNodeAtPoint(p);
                        hole.SetPiece(piece);
                        ResetPiece(piece);
                        fills[x]++;

                    }
                    break;
                }
            }
        }

    }

    /*
     * 
     */
    FlippedPieces GetFlipped(NodePiece p){
        FlippedPieces flip = null;
        for(int i=0; i<flipped.Count; i++){
            if (flipped[i].GetOtherPiece(p) !=null){
                flip = flipped[i];
                break;
            }
        }
        return flip;
    }

    public void StartGame(){
        string seed = GetRandomSeed();
        random = new System.Random(seed.GetHashCode());
        update = new List<NodePiece>();
        flipped = new List<FlippedPieces>();
        dead = new List<NodePiece>();
        fills = new int[width];
        killed = new List<KilledPiece>();
        InitializeBoard();
        VerifyBoard();
        InstantiateBoard();
        score = 0;
        timer.StartTimer();
    }

    void InitializeBoard(){
        board = new Node[width, height];
        for(int y = 0; y<height; y++){
            for(int x=0; x<width; x++){
                board[x,y] = new Node((boardLayout.rows[y].row[x]) ? -1 : fillPiece(), new Point(x,y));
            }
        }
    }

    void VerifyBoard(){
        List<int> remove;
        for(int x=0; x<width; x++){
            for(int y = 0; y<height; y++){
                Point p = new Point(x,y);
                int val = getValueAtPoint(p);
                if(val<=0) continue;
                remove = new List<int>();
                while(isConnected(p, true).Count > 0 ){
                    val = getValueAtPoint(p);
                    if(!remove.Contains(val))
                        remove.Add(val);
                    setValueAtPoint(p, newValue(ref remove));
                }
            }
        }
    }

    void InstantiateBoard() {
        for (int x = 0; x < width; x++){
            for (int y = 0; y < height; y++){
                Node node = getNodeAtPoint(new Point(x, y));
                
                int val = node.value;
                if (val <= 0) continue;
                GameObject p = Instantiate(nodePiece, gameBoard);
                NodePiece piece =p.GetComponent<NodePiece>();
                RectTransform rect = p.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(nodeSize/2 + (nodeSize * x), -nodeSize/2 - (nodeSize*y));
                piece.Initialize(val, new Point(x,y), pieces[val-1], nodeSize);
                node.SetPiece(piece);
            }
        }
    }

    int fillPiece()
    {
        int val = 1;
        val = (random.Next(0, 100) / (100 / pieces.Length)) + 1;
        return val;
    }

    public void ResetPiece(NodePiece piece){
        piece.ResetPosition();
        update.Add(piece);
    }

    void KillPiece(Point p)
    {
        List<KilledPiece> available = new List<KilledPiece>();
        for (int i = 0; i < killed.Count; i++)
        {
            if (!killed[i].falling) available.Add(killed[i]);
        }

        KilledPiece set = null;
        if (available.Count > 0)
        {
            set = available[0];
        }
        else
        {
            GameObject kill = GameObject.Instantiate(killedPiece, killedBoard);
            KilledPiece kPiece = kill.GetComponent<KilledPiece>();
            set = kPiece;
            killed.Add(kPiece);
        }

        int val = getValueAtPoint(p) - 1;
        if (set != null && val >= 0 && val < pieces.Length)
        {
            set.Initialize(pieces[val], GetPositionFromPoint(p));
        }
    }

    public void FlipPieces(Point one, Point two, bool main){
        if(getValueAtPoint(one) < 0) return; //if first one's hole do nothing
        Node nodeOne = getNodeAtPoint(one);
        NodePiece pieceOne = nodeOne.GetPiece();
        if(getValueAtPoint(two) > 0){
            //second one's also not hole
            Node nodeTwo = getNodeAtPoint(two);
            NodePiece pieceTwo = nodeTwo.GetPiece();
            nodeOne.SetPiece(pieceTwo);
            nodeTwo.SetPiece(pieceOne);
            if(main)
                flipped.Add(new FlippedPieces(pieceOne, pieceTwo));

            update.Add(pieceOne);
            update.Add(pieceTwo);
        } else 
            ResetPiece(pieceOne); //second one's hole, reset first one's position
    }

    List<Point> isConnected(Point p, bool main){
        List<Point> connected = new List<Point>();
        int val = getValueAtPoint(p);
        Point[] directions = {
            Point.up, 
            Point.right,
            Point.down,
            Point.left
        };

        foreach(Point dir in directions) {
            List<Point> line = new List<Point>();
            line.Add(p);

            int same = 0;
            for(int i = 1; i < 3; i++) {
                Point check = Point.add(p, Point.mult(dir, i));
                if(getValueAtPoint(check) == val) {
                    line.Add(check);
                    same++;
                }
            }
            if (same > 1) {
                AddPoints(ref connected, line); //Add these points to the overarching connected list
            }
        }
        for(int i = 0; i < 2; i++) {
            List<Point> line = new List<Point>();
            line.Add(p);

            int same = 0;
            Point[] check = { Point.add(p, directions[i]), Point.add(p, directions[i + 2]) };
            foreach (Point next in check){
                if (getValueAtPoint(next) == val){
                    line.Add(next);
                    same++;
                }
            }

            if (same > 1)
                AddPoints(ref connected, line);
        }

        if(main){ //checks for other matches along the current match
            for(int i = 0; i<connected.Count; i++){
                List<Point> more = isConnected(connected[i], false);
                int additional_match = AddPoints(ref connected, more);
                if (additional_match != 0) {
                    Debug.LogFormat("add : %d more : %d",additional_match,more.Count);
                }
            }
        }

        return connected;

    }

    int AddPoints(ref List<Point>points, List<Point> add){
        int added = 0;
        foreach(Point p in add){
            bool doAdd = true;
            for(int i=0; i < points.Count; i++){
                if(points[i].Equals(p)){
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

    int getValueAtPoint(Point p){
        if(p.x<0 || p.x  >= width || p.y<0 || p.y >= height) return -1;
        return board[p.x, p.y].value;
    }

    void setValueAtPoint(Point p, int v){
        board[p.x, p.y].value = v;
    }

    Node getNodeAtPoint(Point p){
        return board[p.x, p.y];
    }

    int newValue(ref List<int> remove){
        List<int> available = new List<int>();
        for(int i=0; i<pieces.Length; i++){
            available.Add(i+1);
        }
        foreach(int i in remove)
            available.Remove(i);
        if(available.Count<=0) return 0;
        return available[random.Next(0,available.Count)];
        
    }

    string GetRandomSeed(){
        string seed = "";
        string acceptableChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz123456789!@#$%^&*()";
        for(int i=0; i<20; i++)
            seed += acceptableChars[Random.Range(0, acceptableChars.Length)];
        return seed;
    }

    public Vector2 GetPositionFromPoint(Point p){
        return new Vector2(nodeSize/2 + (nodeSize * p.x), -nodeSize/2 - (nodeSize*p.y));
    }
}


[System.Serializable]
public class Node{
    public int value; //0=blank, 1=cube, 2=sphere, 3=cylinder, 4=pyramid, 5=diamond, -1 = hole
    public Point index;
    NodePiece piece;
    public Node(int v, Point i){
        value = v;
        index = i;
    }
    public void SetPiece(NodePiece p){
        piece = p;
        value = (piece == null) ? 0 : piece.value;
        if(piece==null) return;
        piece.SetIndex(index);
    }

    public NodePiece GetPiece(){
        return piece;
    }
}

[System.Serializable]
public class FlippedPieces{
    public NodePiece one;
    public NodePiece two;

    public FlippedPieces(NodePiece o, NodePiece t){
        one = o;
        two = t;
    }

    public NodePiece GetOtherPiece(NodePiece p){
        if(p==one) return two;
        else if(p==two) return one;
        else return null;
    }
}
