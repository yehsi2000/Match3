using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePieces : MonoBehaviour
{
    public static MovePieces instance;
    Match3 game;

    NodePiece moving;
    Point newIndex;
    Vector2 mouseStart;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        game = GetComponent<Match3>();
    }

    void Update()
    {
        if(moving != null)
        {
            Vector2 dir = ((Vector2)Input.mousePosition - mouseStart); //mouse moved vector
            Vector2 nDir = dir.normalized;
            Vector2 aDir = new Vector2(Mathf.Abs(dir.x), Mathf.Abs(dir.y)); //for checking which direction to flip

            newIndex = Point.clone(moving.index);
            Point add = Point.zero;
            if (dir.magnitude > game.nodeSize/2f) {
                // If our mouse is away from the starting point for certain amount,
                // select move position based on most moved direction (by checking abs x,y val)
                if (aDir.x > aDir.y)
                    add = (new Point((nDir.x > 0) ? 1 : -1, 0));
                else if(aDir.y > aDir.x)
                    add = (new Point(0, (nDir.y > 0) ? -1 : 1));
            }
            newIndex.add(add); //new index for flicked piece
            //bool isOpponentMoving = game.board[newIndex.x, newIndex.y].GetPiece().GetUpdateState();
           
            Vector2 pos = game.GetPositionFromPoint(moving.index);
            if (!newIndex.Equals(moving.index))
                pos += new Point(add.x, -add.y).ToVector() * game.nodeSize / 4f;
            moving.MovePositionTo(pos);
        }
    }

    public void MovePiece(NodePiece piece)
    {
        if (moving != null) return;
        moving = piece;
        mouseStart = Input.mousePosition;
    }

    public void DropPiece()
    {
        bool isOpponentMoving = game.board[newIndex.x, newIndex.y].GetPiece().GetUpdateState();
        if (moving == null) return;
        if (!newIndex.Equals(moving.index) && !isOpponentMoving)
            game.FlipPieces(moving.index, newIndex, true);
        else
            game.ResetPiece(moving);
        moving = null;
    }
}