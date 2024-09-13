using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NodePiece : MonoBehaviour
{
    public int value;
    public Point index;
    [HideInInspector]
    public Vector2 pos;
    [SerializeField]
    public float moveSpeed = 16f;

    bool updating;
    SpriteRenderer img;
    float nodeSize;

    public void Initialize(int v, Point p, Sprite piece, float size){
        img = GetComponent<SpriteRenderer>();
        nodeSize = size;

        value = v;
        SetIndex(p);
        img.sprite = piece;
    }

    public void SetIndex(Point p){
        index = p;
        ResetPosition();
        UpdateName();
    }

    public void ResetPosition(){
        pos = new Vector2(
            nodeSize/2 + (nodeSize * ( index.x - Match3.getWidth()/2f )), 
            -nodeSize/2 - (nodeSize * ( index.y - Match3.getHeight()/2f ))
            );
    }

    void UpdateName(){
        transform.name = "Node [" + index.x + ", " + index.y + "]";
    }

    public void MovePosition(Vector2 move){
        transform.position += new Vector3(move.x,move.y,0) * Time.deltaTime * moveSpeed;
    }

    public void MovePositionTo(Vector2 move){
        transform.position = Vector2.Lerp(transform.position, move, Time.deltaTime * moveSpeed);
    }

    public bool UpdatePiece(){
        //Debug.LogFormat("dist : {0} obj {1}",Vector3.Distance(rect.anchoredPosition, pos),this.index.x,this.index.y, this.updating);
        if(Vector3.Distance(transform.position, pos) > nodeSize/64f){
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

    void OnMouseDown()
    {
        if (!Match3.isClickable) {
            Debug.Log("Cannot click");
        };
        if (updating || !Match3.isClickable) return;
        MovePieces.instance.MovePiece(this);
    }

    void OnMouseUp()
    {
        Debug.Log("mouse up");
        MovePieces.instance.DropPiece();
    }
}
