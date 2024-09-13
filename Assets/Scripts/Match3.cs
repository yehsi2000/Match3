using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class Match3 : MonoBehaviour
{
    public ArrayLayout boardLayout;

    public static bool isClickable = true;

    [Header("UI Elements")]
    public Sprite[] pieces;
    public Sprite[] specialPieces;
    public GameObject gameBoard;
    public GameObject killedBoard;
    public GameObject gameEndScreen;
    public GameObject bgImageObject;
    public ScoreBoard scoreBoard;
    public TMP_Text finalScore;
    public Timer timer;

    [Header("Prefabs")]
    public GameObject nodePiece;
    public GameObject killedPiece;
    public GameObject popParticle;

    [Header("Score")]
    public int score;
    public int perPieceScore = 5;
    public int match4ExtraScore = 20;
    public int match5ExtraScore = 50;
    public int match6plusExtraScore = 100;
    [Header("NodeSize")]
    public int nodeSize = 80;
    [Header("Time")]
    public float clickStopInterval = 0.5f;


    int width = 14;
    int height = 9;
    int[] fills;
    //float clickableTime = 0f;
    public Node[,] board;
    

    List<NodePiece> update;
    List<FlippedPieces> flipped;
    List<NodePiece> dead;
    List<KilledPiece> killed;
    List<ParticleSystem> particles;

    System.Random random;

    private void Awake() {
        KilledPiece.onKilledPieceRemove.AddListener(KilledPieceRemoved);
    }

    void Start()
    {
        StartGame();
        gameEndScreen.SetActive(false);
    }

    void Update(){
        //update timer
        if (!timer.UpdateTimer()) {
            gameBoard.SetActive(false);
            killedBoard.SetActive(false);
            gameEndScreen.SetActive(true);
            finalScore.text = "Final Score : " + score;
            this.enabled = false;
        }
        //block clicks while new blocks falling
//         if (clickableTime <= 0) isClickable = true;
//         else clickableTime -= Time.deltaTime;

        //update moving pieces and store it for flip check
        List<NodePiece> finishedUpdating = new List<NodePiece>();
        for(int i = 0; i < update.Count; i++){
            NodePiece piece = update[i];
            if (!piece.UpdatePiece()) finishedUpdating.Add(piece);
        }
        
        //bool doneRemoving = false;
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
                int[] matchTypeCnt = new int[5];
                foreach (Point pnt in connected) {  //remove the node pieces connected
                    KillPiece(pnt);
                    Node node = getNodeAtPoint(pnt);
                    if(node.value>0 && node.value<5)
                        matchTypeCnt[node.value-1]++;
                    score += perPieceScore;
                    NodePiece nodePiece = node.GetPiece();
                    if(nodePiece != null){
                        //nodePiece.gameObject.SetActive(false);
                        Destroy(nodePiece.gameObject);
                        //dead.Add(nodePiece);
                    }
                    node.SetPiece(null);
                }
                int matchMax = 0;
                for (int j= 0; j < 5;++j) {
                    // val = j+1
                    // 1=cube, 2=sphere, 3=cylinder, 4=pyramid, 5=diamond,
                    if (matchTypeCnt[j] == 4) {
                        score += match4ExtraScore;
                        matchMax = 1;
                    } else if (matchTypeCnt[j] == 5) {
                        score += match5ExtraScore;
                        matchMax = 2;
                    } else if (matchTypeCnt[j] > 6) {
                        score += match6plusExtraScore;
                        matchMax = 3;
                    }
                }
                //                 isClickable = false;
                //                 clickableTime = clickStopInterval;
                ApplyGravityToBoard(connected[0].x, matchMax);
            }
            flipped.Remove(flip); //remove the flip after update
            update.Remove(piece); //done updating the piece
            
        }
        //if (killed.Count == 0) doneRemoving = true;
        //if (doneRemoving) ApplyGravityToBoard();
        scoreBoard.UpdateScore(score);
    }

    void ApplyGravityToBoard(int specialX, int specialPieceVal){
        bool spawnedSpecial = false;
        for (int x= 0; x<width; x++){
            for(int y = height-1; y>=0; y--){
                //iterate from the top to bottom
                Point curPoint = new Point(x,y);
                Node curNode = getNodeAtPoint(curPoint);
                int curVal = getValueAtPoint(curPoint);
                if(curVal!=0) continue; //find blank space where connected block disappeared
                for(int ny = y-1; ny >= -1; ny--){
                    //y=-1 is above the top line
                    //for pieces above this blank drop down
                    Point next = new Point(x, ny) ;
                    int nextVal = getValueAtPoint(next);
                    if(nextVal==0) continue; //another blank, find upper one
                    if(nextVal != -1) { 
                        //if we did not hit top(or intentional hole) then drag upper ones down to the hole
                        Node got = getNodeAtPoint(next);
                        NodePiece piece = got.GetPiece();

                        //Set the hole to upper piece
                        curNode.SetPiece(piece);
                        update.Add(piece);

                        got.SetPiece(null); //Replace the upper piece to blank
                    } else {
                        //if above is top wall or hole create new piece and drop it from the top
                        int newVal = GetRandomPieceVal();
                        
                        //y=-1 is above top line, fills[x] = offset up, as we are dropping more than 1 piece
                        Point fallPoint = new Point(x, -1 - fills[x]);
                        
                        //create new piece
                        GameObject obj = Instantiate(nodePiece, gameBoard.transform);
                        NodePiece piece = obj.GetComponent<NodePiece>();

                        //put new piece on top so it looks like falling down
                        if (!spawnedSpecial && x==specialX  && specialPieceVal > 0) {
                            piece.Initialize(99, curPoint, specialPieces[specialPieceVal - 1], nodeSize);
                            spawnedSpecial = true;
                        } else piece.Initialize(newVal, curPoint, pieces[newVal - 1], nodeSize);
                        piece.rect.anchoredPosition = GetPositionFromPoint(fallPoint); 

                        Node hole = getNodeAtPoint(curPoint);
                        hole.SetPiece(piece);
                        ResetPiece(piece);
                        fills[x]++; //move offset upper for more piece to drop

                    }
                    break;
                }
            }
        }

    }

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
        particles = new List<ParticleSystem>();
        gameBoard.GetComponent<RectTransform>().sizeDelta = new Vector2(nodeSize*width, nodeSize*height);
        killedBoard.GetComponent<RectTransform>().sizeDelta = new Vector2(nodeSize*width, nodeSize*height);
        // set sprite image background to camera size, currently not used
        SpriteRenderer bgSpriteRenderer = bgImageObject.GetComponent<SpriteRenderer>();
        float _width = bgSpriteRenderer.bounds.size.x;
        float _height = bgSpriteRenderer.bounds.size.y;
        //float worldScreenHeight = (float)(Camera.main.orthographicSize * 2.0f);
        //float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;
        //bgImageObject.transform.localScale = new Vector3(worldScreenWidth / _width*nodeSize*width/1024, worldScreenHeight/_height*nodeSize*height/640,1);
        bgImageObject.transform.localScale = new Vector3(nodeSize*(width+1) / _width, nodeSize*(height+1)/_height,1);
        
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
                board[x,y] = new Node((boardLayout.rows[y].row[x]) ? -1 : GetRandomPieceVal(), new Point(x,y));
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
                GameObject p = Instantiate(nodePiece, gameBoard.transform);
                NodePiece piece =p.GetComponent<NodePiece>();
                RectTransform rect = p.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(nodeSize/2 + (nodeSize * x), -nodeSize/2 - (nodeSize*y));
                piece.Initialize(val, new Point(x,y), pieces[val-1], nodeSize);
                node.SetPiece(piece);
            }
        }
    }

    int GetRandomPieceVal()
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
        int val = getValueAtPoint(p) - 1;
        if (val < 0) return;
        GameObject kill = GameObject.Instantiate(killedPiece, killedBoard.transform);
        KilledPiece kPiece = kill.GetComponent<KilledPiece>();

        Vector2 pointPos = GetPositionFromPoint(p);

        List<ParticleSystem> available = new List<ParticleSystem>();
        for(int i=0; i<particles.Count; i++) {
            Debug.Log(particles[i].isStopped);
            if (particles[i].isStopped) {
                available.Add(particles[i]);
            }
        }
        ParticleSystem particle = null;
        if(available.Count > 0) {
            particle = available[0];
        } else {
            
            GameObject particleObject = GameObject.Instantiate(popParticle,killedBoard.transform);
            ParticleSystem objParticle = particleObject.GetComponent<ParticleSystem>();
            particle = objParticle;
            particles.Add(objParticle);
        }

        Debug.Log(pointPos);
        particle.gameObject.GetComponent<RectTransform>().anchoredPosition = pointPos;
        particle.Play();
        
        if (kPiece != null && val < pieces.Length)
        {
            kPiece.Initialize(pieces[val], pointPos);
            killed.Add(kPiece);
        }
    }

    public void KilledPieceRemoved(KilledPiece killedPiece) {
        killed.Remove(killedPiece);
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

        for (int i = 0; i < 4; i++) {
            //Check for a 2x2
            List<Point> square = new List<Point>();

            int same = 0;
            int next = i + 1;
            if (next >= 4)
                next -= 4;

            Point[] check = { Point.add(p, directions[i]), Point.add(p, directions[next]), Point.add(p, Point.add(directions[i], directions[next])) };
            foreach (Point pnt in check) { 
                //Check all sides of the piece, if they are the same value, add them to the list
                if (getValueAtPoint(pnt) == val) {
                    square.Add(pnt);
                    same++;
                }
            }

            if (same > 2)
                AddPoints(ref connected, square);
        }

        if (main){ //checks for other matches along the current match
            for(int i = 0; i<connected.Count; i++){
                List<Point> more = isConnected(connected[i], false);
                int additional_match = AddPoints(ref connected, more);
                if (additional_match != 0) {
                    Debug.LogFormat("add : {0} more : {1}",additional_match,more.Count);
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
