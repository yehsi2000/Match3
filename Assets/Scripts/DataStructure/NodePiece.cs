using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class NodePiece : MonoBehaviour {
    [SerializeField]
    INodeType nodeVal;

    public INodeType NodeVal {
        get { return nodeVal; }
    }

    public Point index;

    [HideInInspector]
    public Vector2 pos;

    [SerializeField]
    public float moveSpeed = 16f;

    bool updating;
    SpriteRenderer img;
    float nodeSize;
    Board board;

    public float NodeSize {
        get { return nodeSize; }
    }

    public Board GetBoard {
        get { return board; }
    }

    float boardWidth;
    float boardHeight;

    static internal UnityEvent<Board, Point, SpecialType, bool> onSpecialBlockPress =
        new UnityEvent<Board, Point, SpecialType, bool>();

    public void Initialize(INodeType type, Board b, Point p, Sprite piece, float size, float width, float height) {
        //Debug.Log($"piece {type.ToString()} at Point {p.x}:{p.y}");
        img = GetComponent<SpriteRenderer>();
        board = b;
        nodeSize = size;
        nodeVal = type;
        SetIndex(p);
        img.sprite = piece;
        transform.localScale = new Vector3(size / 2.5f, size / 2.5f, 1);
        boardWidth = width;
        boardHeight = height;
        updating = false;
    }

    public void SetIndex(Point p) {
        index = p;
        ResetPosition();
        UpdateName();
    }

    public void ResetPosition() {
        pos = board.transform.position + new Vector3(
            nodeSize / 2 + (nodeSize * (index.x - boardWidth / 2f)),
            -nodeSize / 2 - (nodeSize * (index.y - boardHeight / 2f))
            );
    }

    void UpdateName() {
        transform.name = "Node [" + index.x + ", " + index.y + "]";
    }

    public void MovePosition(Vector2 move) {
        transform.position += new Vector3(move.x, move.y, 0) * Time.deltaTime * moveSpeed;
    }

    public void MovePositionTo(Vector2 move) {
        transform.position = Vector2.Lerp(transform.position, move, Time.deltaTime * moveSpeed);
    }

    public bool UpdatePiece() {
        if (Vector3.Distance(transform.position, pos) > nodeSize / 32f) {
            MovePositionTo(pos);
            updating = true;
            return true;
        }
        else {
            transform.position = pos;
            updating = false;
            return false;
        }
    }

    public bool GetUpdateState() {
        return updating;
    }

    void OnMouseDown() {
        //if (!GameManager.instance.IsClickable) Debug.Log("Cannot click");

        if (!board.IsPlayerBoard || updating || !GameManager.instance.IsClickable) return;

        PieceController.instance.MovePiece(this);
    }

    void OnMouseUp() {
        if (!board.IsPlayerBoard) return;
        if (nodeVal is SpecialType) {
            onSpecialBlockPress.Invoke(board, index, (nodeVal as SpecialType), false);
        }
        else {
            PieceController.instance.DropPiece(board);
        }
    }
}
