using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{

    [SerializeField]
    private int width = 9;

    [SerializeField]
    private int height = 14;

    [SerializeField]
    private readonly float nodeSize = 2f;

    [SerializeField]
    public BoardController boardController;

    [SerializeField]
    GameObject gameBoard;

    [SerializeField]
    GameObject killedBoard;

    public List<NodePiece> updateList;
    public List<Point> specialUpdateList;
    public List<FlippedPieces> flippedList;
    public List<KilledPiece> killedPieceList;

    public System.Random rng;

    public float NodeSize {
        get { return nodeSize; }
    }

    [HideInInspector]
    public Node[,] boardNode;

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
            if(value<=0) return;
            else width = value;
        }
    }

    public int Height{
        get { return height; }
        set {
            if (value <= 0) return;
            else height = value;
        }
    }

    void Awake() {
        updateList = new List<NodePiece>();
        specialUpdateList = new List<Point>();
        flippedList = new List<FlippedPieces>();
        killedPieceList = new List<KilledPiece>();
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
        return this.transform.position + new Vector3(NodeSize / 2 + (NodeSize * (p.x - width / 2f)), 
            -NodeSize / 2 - (NodeSize * (p.y - height / 2f)));
    }
}
