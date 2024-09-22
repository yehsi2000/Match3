using System;
using System.Collections.Generic;
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
    public float nodeSize = 0.8f;
    [Header("Time")]
    public float clickStopInterval = 0.5f;
    [Header("Audio")]
    AudioSource audio;
    public AudioClip[] audioclips;


    private static int width = 14;
    private static int height = 9;
    int[] fills;
    float clickableTime = 0f;
    public Node[,] board;
    

    List<NodePiece> update;
    List<Point> specialUpdate;
    List<FlippedPieces> flipped;
    List<NodePiece> dead;
    List<KilledPiece> killed;
    List<ParticleSystem> particles;

    System.Random random;

    public static int getWidth() {
        return width;
    }
    public static int getHeight() {
        return height;
    }

    private void Awake() {
        KilledPiece.onKilledPieceRemove.AddListener(KilledPieceRemoved);
        NodePiece.onSpecialBlockPress.AddListener(SpecialBlockPressed);
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
        //prevent clicking while special block popping
         if (clickableTime <= 0) isClickable = true;
         else clickableTime -= Time.deltaTime;

        //update moving pieces and store it for flip check
        List<NodePiece> finishedUpdating = new List<NodePiece>();
        for(int i = 0; i < update.Count; i++){
            NodePiece piece = update[i];
            if (piece!=null && !piece.UpdatePiece()) finishedUpdating.Add(piece);
        }
        //Update for special block activation
        if (specialUpdate.Count > 0) {
            for (int i=0; i<specialUpdate.Count; ++i) {
                KillPiece(specialUpdate[i]);
                Node node = getNodeAtPoint(specialUpdate[i]);
                score += perPieceScore;
                NodePiece nodePiece = node.GetPiece();
                if (nodePiece != null) {
                    Destroy(nodePiece.gameObject);
                }
                node.SetPiece(null);
                
            }
            DropNewPiece();
            isClickable = false;
            clickableTime = clickStopInterval;
            audio.clip = audioclips[UnityEngine.Random.Range(0, audioclips.Length - 1)];
            audio.Play();
            specialUpdate.Clear();
            return;
        }
        
        //bool doneRemoving = false;
        //check if flipped pieces could make a match, else revert flip
        for (int i = 0; i < finishedUpdating.Count; i++){
            NodePiece piece = finishedUpdating[i]; //updated piece
            FlippedPieces flip = GetFlipped(piece); //flipped by updated piece
            NodePiece flippedPiece = null;
            
            int x = piece.index.x; //"x"th column
            fills[x] = Mathf.Clamp(fills[x]-1, 0, width);

            List<Point> connected = findConnected(piece.index, true); //check if user controlled piece made a match
            bool wasFlipped = (flip != null);

            if (wasFlipped) {
                flippedPiece = flip.GetOtherPiece(piece);
                AddPoints(ref connected, findConnected(flippedPiece.index, true));
            }

            if (connected.Count == 0){
                //if we didn't make a match
                if (wasFlipped)
                    FlipPieces(piece.index, flippedPiece.index, false); //revert flip
            }
            else {
                //made a match

                //idx= piece value , item1 = piece cnt, item2 = sum of all piece x
                ValueTuple<int,int>[] matchTypeCnt = new ValueTuple<int,int>[pieces.Length];
                
                foreach (Point pnt in connected) {  //remove the node pieces connected
                    KillPiece(pnt);
                    Node node = getNodeAtPoint(pnt);
                    //if node is normal piece(not special, not hole, not blank)
                    if (0 < node.value && node.value < pieces.Length) {
                        matchTypeCnt[node.value-1].Item1++;
                        matchTypeCnt[node.value-1].Item2 = pnt.x;
                    }
                    score += perPieceScore;
                    NodePiece nodePiece = node.GetPiece();
                    if(nodePiece != null){
                        //nodePiece.gameObject.SetActive(false);
                        Destroy(nodePiece.gameObject);
                        //dead.Add(nodePiece);
                    }
                    node.SetPiece(null);
                }
                List<ValueTuple<int,int>> matched5list = new List<ValueTuple<int,int>>();
                for (int j=0; j<pieces.Length; ++j) {
                    if (matchTypeCnt[j].Item1 == 4) {
                        score += match4ExtraScore;
                    }
                    if (matchTypeCnt[j].Item1 == 5) {
                        score += match5ExtraScore;
                        //send block's info which matched 5
                        matched5list.Add(new ValueTuple<int,int>(j+1, matchTypeCnt[j].Item2));
                    }
                    if (matchTypeCnt[j].Item1 > 5) {
                        score += match6plusExtraScore;
                        //send block's info which matched more than 5
                        matched5list.Add(new ValueTuple<int, int>(0, matchTypeCnt[j].Item2));
                    }
                }
                
                DropNewPiece(matched5list);
                audio.clip = audioclips[UnityEngine.Random.Range(0, audioclips.Length - 1)];
                audio.Play();
            }
            
            flipped.Remove(flip); //remove the flip after update
            update.Remove(piece); //done updating the piece
            
        }
        //if (killed.Count == 0) doneRemoving = true;
        //if (doneRemoving) ApplyGravityToBoard();
        scoreBoard.UpdateScore(score);
    }

    void DropNewPiece(List<ValueTuple<int, int>> specialBlockList = null){
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
                        if (specialBlockList != null && specialBlockList.Count > 0) {
                            for (int i = 0; i < specialBlockList.Count; ++i) {
                                if (specialBlockList[i].Item2 == x) {
                                    piece.Initialize(100 + specialBlockList[i].Item1, curPoint, specialPieces[specialBlockList[i].Item1], nodeSize);
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
        string seed = getRandomSeed();
        random = new System.Random(seed.GetHashCode());
        update = new List<NodePiece>();
        specialUpdate = new List<Point>();
        flipped = new List<FlippedPieces>();
        dead = new List<NodePiece>();
        fills = new int[width];
        killed = new List<KilledPiece>();
        particles = new List<ParticleSystem>();
        //gameBoard.GetComponent<RectTransform>().sizeDelta = new Vector2(nodeSize*width, nodeSize*height);
        //killedBoard.GetComponent<RectTransform>().sizeDelta = new Vector2(nodeSize*width, nodeSize*height);
        audio = GetComponent<AudioSource>();
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
                board[x,y] = new Node((boardLayout.rows[y].row[x]) ? 100 : GetRandomPieceVal(), new Point(x,y));
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
                while(findConnected(p, true).Count > 0 ){
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
                //RectTransform rect = p.GetComponent<RectTransform>();
                p.transform.position = new Vector3(nodeSize/2 + (nodeSize * (x-width/2f)), -nodeSize/2 - (nodeSize*(y-height/2f)));
                if(val>=100) piece.Initialize(val, new Point(x,y), specialPieces[val-100], nodeSize);
                else piece.Initialize(val, new Point(x,y), pieces[val-1], nodeSize);
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
        int val = getValueAtPoint(p);
        if (val <= 0) return;
        GameObject kill = GameObject.Instantiate(killedPiece, killedBoard.transform);
        KilledPiece kPiece = kill.GetComponent<KilledPiece>();

        Vector2 pointPos = getPositionFromPoint(p);

        List<ParticleSystem> available = new List<ParticleSystem>();
        for(int i=0; i<particles.Count; i++) {
            //Debug.Log(particles[i].isStopped);
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

        //Debug.Log(pointPos);
        particle.transform.position = pointPos;
        particle.Play();
        
        if (kPiece != null && val-1 < pieces.Length)
        {
            kPiece.Initialize(pieces[val-1], pointPos);
            killed.Add(kPiece);
        } else if(kPiece != null && val-100 < specialPieces.Length) {
            kPiece.Initialize(specialPieces[val-100], pointPos);
            SpecialBlockPressed(p,val-100);
            killed.Add(kPiece);
        }
    }

    public void KilledPieceRemoved(KilledPiece killedPiece) {
        killed.Remove(killedPiece);
    }

    public void SpecialBlockPressed(Point pnt, int val) {
        Debug.LogErrorFormat("Special val {0}", val);
        if (val == 0) {
            //싞틀이
            //화면 모든 블럭 제거
            for (int i = 0; i < width; ++i) 
                for (int j = 0; j < height; ++j) 
                    specialUpdate.Add(new Point(i, j));
            } else if (val == 1) {
            //다비
            //대각선 모두 제거
            specialUpdate.Add(pnt);
            for (int i = 1; i <= Math.Max(height,width); i++) {
                Point[] dir = { new Point(1, -1), new Point(1, 1), new Point(-1, 1), new Point(-1, -1) };
                foreach (Point p in dir) {
                    Point toAdd = Point.add(pnt, Point.mult(p, i));
                    if (toAdd.x >= 0 && toAdd.x < width && toAdd.y >= 0 && toAdd.y < height) {
                        specialUpdate.Add(toAdd);
                    }
                }
            }
        } else if (val == 2) {
            //리자
            //가로세로 모두 제거
            for (int i = 0; i < width; ++i)
                specialUpdate.Add(new Point(i, pnt.y));
            for (int i = 0; i < height; ++i) {
                if (i == pnt.y) continue; //prevent dual update for pressed
                specialUpdate.Add(new Point(pnt.x, i));
            }
        } else if (val == 3) {
            //모나
            //인접 5x5 블럭 파괴
            for (int i = -2; i <= 2; ++i) {
                for (int j = -2; j <= 2; ++j) {
                    Point toAdd = Point.add(pnt, new Point(i, j));
                    if (toAdd.x >= 0 && toAdd.x < width && toAdd.y >= 0 && toAdd.y < height) {
                        specialUpdate.Add(toAdd);
                    }
                }
            }
        } else if (val == 4) {
            //우산
            //랜덤 블럭제거
            specialUpdate.Add(pnt);
            while(specialUpdate.Count < 10) {
                int randomX = random.Next(0, width);
                int randomY = random.Next(0, height);
                specialUpdate.Add(new Point(randomX, randomY));
            }
        } else if (val == 5) {
            //우콩
            //모든 상아제거
            for (int i = 0; i < width; ++i) {
                for (int j = 0; j < height; ++j) {
                    if (getNodeAtPoint(new Point(i, j)).GetPiece().value == 5) {
                        specialUpdate.Add(new Point(i, j));
                    }
                }
            }
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

    List<Point> findConnected(Point p, bool main){
        List<Point> connected = new List<Point>();
        int val = getValueAtPoint(p);
        if (val > pieces.Length + 1) return connected;
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
                List<Point> more = findConnected(connected[i], false);
                int additional_match = AddPoints(ref connected, more);
                if (additional_match != 0) {
                    //Debug.LogFormat("add : {0} more : {1}",additional_match,more.Count);
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

    string getRandomSeed(){
        string seed = "";
        string acceptableChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz123456789!@#$%^&*()";
        for(int i=0; i<20; i++)
            seed += acceptableChars[UnityEngine.Random.Range(0, acceptableChars.Length)];
        return seed;
    }

    public Vector2 getPositionFromPoint(Point p){
        return new Vector3(nodeSize/2 + (nodeSize * (p.x-width/2f)), -nodeSize/2 - (nodeSize * (p.y-height/2f)));
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
