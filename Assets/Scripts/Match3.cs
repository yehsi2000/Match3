using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using KaimiraGames;
using static UnityEngine.ParticleSystem;
using System.Linq;

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
    public TMP_Text finalScore;
    public ComboDisplay comboDisplay;

    [Header("Prefabs")]
    public GameObject nodePiece;
    public GameObject killedPiece;
    public GameObject popParticle;

    [Header("Score")]
    public int score;
    public int autoBlockWeightMultiplier = 5;
    public int perPieceScore = 5;
    public int match4ExtraScore = 20;
    public int match5ExtraScore = 50;
    public int match6plusExtraScore = 100;
    [Header("NodeSize")]
    public float nodeSize = 0.8f;
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

    private static int width = 14;
    private static int height = 9;
    int[] fills;
    float clickableTime = 0f;
    float comboTime = 0f;
    [HideInInspector]
    public int combo = 0;
    public Node[,] board;
    

    List<NodePiece> update;
    List<Point> specialUpdate;
    List<FlippedPieces> flipped;
    List<NodePiece> dead;
    List<KilledPiece> killed;
    List<ParticleSystem> particles;

    System.Random random;
    List<WeightedList<int>> myWL;

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

    public void BacktoTitle() {
        SceneManager.LoadScene("StartScene");
    }

    public void Reset() {
        StartGame();
        gameEndScreen.SetActive(false);
    }

    void Start()
    {
        StartGame();
        gameEndScreen.SetActive(false);
    }

    void Update(){
        //update timer
        if (!Timer.instance.UpdateTimer()) {
            gameBoard.SetActive(false);
            killedBoard.SetActive(false);
            gameEndScreen.SetActive(true);
            bgm.Stop();
            finalScore.text = "Final Score : " + score;
            this.enabled = false;
        }
        //prevent clicking while special block popping
         if (clickableTime <= 0) isClickable = true;
         else clickableTime -= Time.deltaTime;
         if (comboTime <= 0) combo = 0;
         else comboTime -= Time.deltaTime;


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
            blockPopAudio.clip = blockPopAudioClips[UnityEngine.Random.Range(0, blockPopAudioClips.Length - 1)];
            AddCombo();
            blockPopAudio.Play();
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

                //idx= piece value , item1 = piece cnt, item2 = xpos of last updated piece
                ValueTuple<int,int>[] matchTypeCnt = new ValueTuple<int,int>[pieces.Length];
                
                foreach (Point pnt in connected) {  //remove the node pieces connected
                    Node node = getNodeAtPoint(pnt);
                    //if node is normal piece(not special, not hole, not blank)
                    if (0 < node.value && node.value <= pieces.Length) {
                        matchTypeCnt[node.value-1].Item1++;
                        matchTypeCnt[node.value-1].Item2 = pnt.x;
                    }
                    score += perPieceScore;
                    KillPiece(pnt);
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
                    else if (matchTypeCnt[j].Item1 == 5) {
                        score += match5ExtraScore;
                        //send block's info which matched 5
                        matched5list.Add(new ValueTuple<int,int>(j+1, matchTypeCnt[j].Item2));
                    }
                    else if (matchTypeCnt[j].Item1 > 5) {
                        score += match6plusExtraScore;
                        matched5list.Add(new ValueTuple<int,int>(j+1, matchTypeCnt[j].Item2));
                        //send block's info 5or more matched block is in  line
                        //matched5list.Add(new ValueTuple<int, int>(0, matchTypeCnt[j].Item2));
                    }
                }
                AddCombo();

                Debug.Log(isDeadlocked());
                
                DropNewPiece(matched5list);
                
                blockPopAudio.clip = blockPopAudioClips[UnityEngine.Random.Range(0, blockPopAudioClips.Length - 1)];
                blockPopAudio.Play();
            }
            
            flipped.Remove(flip); //remove the flip after update
            update.Remove(piece); //done updating the piece
            
        }
        //if (killed.Count == 0) doneRemoving = true;
        //if (doneRemoving) ApplyGravityToBoard();
        ScoreBoard.instance.UpdateScore(score);
    }
    /// <summary>
    /// Increment Combo and perform additional jobs related to combo, ex)sfx, score, combotimer
    /// </summary>
    void AddCombo() {
        combo++;
        score += Math.Clamp((combo/5),0,6) * perPieceScore;
        comboTime = comboRetainInterval;
        comboDisplay.UpdateCombo(combo);
        if (combo % 5 == 0 && combo > 0) {
            int combosfxindex = Math.Clamp((combo/5)-1, 0, comboAudioClips.Length - 1);
            comboAudio.clip = comboAudioClips[combosfxindex];
            comboAudio.Play();
        }
    }
    /// <summary>
    /// Drop NodePiece if there are hollows below. Generate empty block on top if needed.
    /// </summary>
    /// <param name="specialBlockList">List of infos of special block to generate. Tuple(speical generate xval, specialblock type)</param>
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
                        int[] nearRow = { -2, -1, 1, 2};
                        List<int> nearValues = new List<int>();
                        foreach (int diff in nearRow) {
                            if (x + diff >= width || x+diff < 0 || y + fills[x]<0 || y + fills[x] >= height) continue;
                                NodePiece np = board[x + diff, y].GetPiece();
                            if (np != null) {
                                nearValues.Add(np.value);
                            }
                        }
                        for(int i=0; i<nearValues.Count-1; ++i) {
                            if (nearValues[i] == nearValues[i + 1] && nearValues[i]!=-1) {
                                if(newVal<100) newVal = GetWeightedRandomPieceVal(nearValues[i]);
                                break;
                            }
                        }
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

    /// <summary>
    /// Get flipped piece of current selected NodePiece
    /// </summary>
    /// <param name="p">selected piece</param>
    /// <returns></returns>
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

    /// <summary>
    /// Initialize game variable and set components
    /// </summary>
    public void StartGame(){
        string seed = getRandomSeed();
        random = new System.Random(seed.GetHashCode());
        myWL = new List<WeightedList<int>>();
        for (int i = 1; i <= pieces.Length; ++i) {
            var newWL = new WeightedList<int>(random);
            for (int j = 1; j <= pieces.Length; ++j) {
                if(j==i) newWL.Add(j, autoBlockWeightMultiplier);
                else newWL.Add(j, 1);
            }
            myWL.Add(newWL);
        }
        update = new List<NodePiece>();
        specialUpdate = new List<Point>();
        flipped = new List<FlippedPieces>();
        dead = new List<NodePiece>();
        fills = new int[width];
        killed = new List<KilledPiece>();
        particles = new List<ParticleSystem>();
        comboDisplay.Initialize(comboRetainInterval);
        //gameBoard.GetComponent<RectTransform>().sizeDelta = new Vector2(nodeSize*width, nodeSize*height);
        //killedBoard.GetComponent<RectTransform>().sizeDelta = new Vector2(nodeSize*width, nodeSize*height);
        blockPopAudio = GetComponent<AudioSource>();
        if (!PlayerPrefs.HasKey("bgm")) PlayerPrefs.SetInt("bgm", 0);
        bgm.clip = bgmAudioClips[PlayerPrefs.GetInt("bgm") % bgmAudioClips.Length];
        bgm.Play();
        // set sprite image background to camera size, currently not used
        SpriteRenderer bgSpriteRenderer = bgImageObject.GetComponent<SpriteRenderer>();
        float _width = bgSpriteRenderer.bounds.size.x;
        float _height = bgSpriteRenderer.bounds.size.y;
        //float worldScreenHeight = (float)(Camera.main.orthographicSize * 2.0f);
        //float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;
        //bgImageObject.transform.localScale = new Vector3(worldScreenWidth / _width*nodeSize*width/1024, worldScreenHeight/_height*nodeSize*height/640,1);
        bgImageObject.transform.localScale = new Vector3(nodeSize*(width+1) / _width, nodeSize*(height+1)/_height,1);
        do {
            InitializeBoard();
            VerifyBoard();
            InstantiateBoard();
        } while (isDeadlocked());
        score = 0;
        Timer.instance.StartTimer();
    }
    /// <summary>
    /// Fill Board with random value, doesn't check if there are match.
    /// Should call VerifyBoard after to check if it's in valid form
    /// </summary>
    void InitializeBoard(){
        board = new Node[width, height];
        for(int y = 0; y<height; y++){
            for(int x=0; x<width; x++){
                board[x,y] = new Node((boardLayout.rows[y].row[x]) ? 5 : GetRandomPieceVal(), new Point(x,y));
            }
        }
    }

    bool isDeadlocked() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Point p = new Point(x, y);
                int val = getValueAtPoint(p);
                if (val <= 0) continue;
                if (y < height- 1) {
                    Point down = new Point(x, y+1);
                    if (findConnected(p, false, down).Count > 0) return false;
                }
                if (x < width - 1) {
                    Point right = new Point(x+1, y);
                    if (findConnected(p, false, right).Count > 0) return false;
                }
            }
        }
        return true;
    }


    /// <summary>
    /// Check if there's any connected block in current board. If there is, remove it and regenerated block in place
    /// </summary>
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

    /// <summary>
    /// Generate and place instances of nodepiece prefabs in node-size-aligned position
    /// </summary>
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

    /// <summary>
    /// get random normal piece value
    /// </summary>
    /// <returns>random normal piece (starting with 1)</returns>
    int GetRandomPieceVal()
    {
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
        if(val<0 || val>pieces.Length) 
            return GetRandomPieceVal();
        return myWL[val-1].Next();
        //if (!myWL.Contains(val)) {
        //    return myWL.Next();
        //}
        //myWL.SetWeight(val, autoBlockWeightMultiplier);
        //int ret = myWL.Next();
        //myWL.SetWeight(val, 1);
        //return ret;
    }

    /// <summary>
    /// Push piece in update list to place it in index-position.
    /// Used to initiate NodePiece or revert flip 
    /// </summary>
    /// <param name="piece">node to reset position</param>
    public void ResetPiece(NodePiece piece){
        piece.ResetPosition();
        update.Add(piece);
    }

    /// <summary>
    /// Doesn't really kill piece. Instantiates killpiece prefabs in killboard and make explosion effect on popped pieces
    /// </summary>
    /// <param name="p">Piece which is killed and to be dropped</param>
    void KillPiece(Point p)
    {
        int val = getValueAtPoint(p);
        if (val <= 0) return;
        GameObject kill = GameObject.Instantiate(killedPiece, killedBoard.transform);
        KilledPiece kPiece = kill.GetComponent<KilledPiece>();

        Vector2 pointPos = getPositionFromPoint(p);

        List<ParticleSystem> available = new List<ParticleSystem>();
        for(int i=0; i<particles.Count; i++) {
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

    /// <summary>
    /// Reacts to killpiece prefab unrendered on screen and remove it from killedlist
    /// </summary>
    /// <param name="killedPiece"></param>
    public void KilledPieceRemoved(KilledPiece killedPiece) {
        killed.Remove(killedPiece);
    }

    /// <summary>
    /// Reacts to special blocks pressed and execute it's implementation
    /// </summary>
    /// <param name="pnt">Special block position index</param>
    /// <param name="val">Special block type 0 ~ </param>
    public void SpecialBlockPressed(Point pnt, int val) {
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
                Point newpnt = new Point(randomX, randomY);
                Node newnode = getNodeAtPoint(newpnt);
                if (newnode.value >= 100) continue;
                specialUpdate.Add(newpnt);
            }
        } else if (val == 5) {
            //우콩
            //모든 상아제거
            specialUpdate.Add(pnt);
            for (int i = 0; i < width; ++i) {
                for (int j = 0; j < height; ++j) {
                    Node node = getNodeAtPoint(new Point(i, j));
                    if (node != null) {
                        NodePiece piece = node.GetPiece();
                        if (piece != null && piece.value == 5) {
                            specialUpdate.Add(new Point(i, j));
                        }
                    }
                }
            }
        }
    }
    /// <summary>
    /// Flip two piece
    /// </summary>
    /// <param name="one">piece to be flipped</param>
    /// <param name="two">piece to be flipped</param>
    /// <param name="main">is this called in user action not revert flip?</param>
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

    /// <summary>
    /// Find and return all blocks connected with current block.
    /// </summary>
    /// <param name="p">Initial block position</param>
    /// <param name="main">is this called in Update() function, not recursively in itself?</param>
    /// <returns></returns>
    List<Point> findConnected(Point p, bool main, Point exchanged = null){
        List<Point> connected = new List<Point>();
        int val;
        if (exchanged!=null) {
            val = getValueAtPoint(exchanged);
        } else {
            val = getValueAtPoint(p);
        }
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
                if (check == exchanged) continue;
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
                if(next== exchanged) continue;
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
                if(pnt == exchanged) continue;
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

    /// <summary>
    /// Get Piece type value except ones in remove List
    /// </summary>
    /// <param name="remove">piece type values not wanted to be generated</param>
    /// <returns>new piece type value which is not in remove List</returns>
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
