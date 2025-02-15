using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour {

    [SerializeField]
    private int width = 9;

    [SerializeField]
    private int height = 14;

    [SerializeField]
    public BoardController boardController;

    [SerializeField]
    bool isPlayerBoard = false;

    [SerializeField]
    bool isMultiplayer = false;

    public bool IsPlayerBoard {
        get { return isPlayerBoard; }
    }

    public bool IsMultiplayer {
        get { return isMultiplayer; }
    }

    [SerializeField]
    GameObject gameBoard;

    [SerializeField]
    GameObject killedBoard;

    [HideInInspector]
    public LinkedList<NodePiece> updateList;

    [HideInInspector]
    public LinkedList<Point> specialUpdateList;

    [HideInInspector]
    public LinkedList<FlippedPieces> flippedList;

    [HideInInspector]
    public List<KilledPiece> killedPieceList;

    public CustomRandom rng;

    public GameObject bgImageObject;

    public Node[,] boardNode;

    public class SpecialActivationInfo {
        public SpecialActivationInfo(Point p, SpecialType t) {
            pnt = p;
            type = t;
        }
        public Point pnt;
        public SpecialType type;
    }

    public Queue<SpecialActivationInfo> specialActivationQueue;

    public GameObject GameBoard {
        get { return gameBoard; }
    }

    public GameObject KilledBoard {
        get { return killedBoard; }
    }

    public Node[,] BoardNode {
        get { return boardNode; }
        set { boardNode = value; }
    }

    public int Width {
        get { return width; }
        set {
            if (value <= 0) return;
            else width = value;
        }
    }

    public int Height {
        get { return height; }
        set {
            if (value <= 0) return;
            else height = value;
        }
    }

    public int RngNext(int a, int b) {
        int val = rng.Next(a, b);
        
        //Debug.Log($"{rng.GetHashCode()} {gameObject.name} 's val {val}");
        return val;
    }

    void Awake() {
        updateList = new LinkedList<NodePiece>();
        specialUpdateList = new LinkedList<Point>();
        flippedList = new LinkedList<FlippedPieces>();
        killedPieceList = new List<KilledPiece>();
        specialActivationQueue = new Queue<SpecialActivationInfo> ();
    }

    private void Start() {
        // set sprite image background to camera size, currently not used
        SpriteRenderer bgSpriteRenderer = bgImageObject.GetComponent<SpriteRenderer>();

        float _width = bgSpriteRenderer.bounds.size.x;
        float _height = bgSpriteRenderer.bounds.size.y;

        // set background image to gameboard size
        bgImageObject.transform.localScale = new Vector3(boardController.NodeSize * (width + 1)
            / _width, boardController.NodeSize
            * (height + 1) / _height, 1);
        bgImageObject.transform.position = gameObject.transform.position;
    }

    public Node GetNodeAtPoint(Point p) {
        return BoardNode[p.x, p.y];
    }

    public INodeType GetValueAtPoint(Point p) {
        if (p.x < 0 || p.x >= width || p.y < 0 || p.y >= height) {
            return new BlockedNodeType();
        }
        //return game.boardManager.Board[p.x, p.y].GetValue();
        INodeType nodeval = BoardNode[p.x, p.y].GetValue();
        return nodeval;
    }

    public void SetValueAtPoint(Point p, INodeType v) {
        BoardNode[p.x, p.y].typeVal = v;
    }

    public Vector2 getPositionFromPoint(Point p) {
        return this.transform.position + new Vector3(boardController.NodeSize / 2 + (boardController.NodeSize * (p.x - width / 2f)),
            -boardController.NodeSize / 2 - (boardController.NodeSize * (p.y - height / 2f)));
    }
}
