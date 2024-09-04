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
    [HideInInspector]
    public RectTransform rect;
    [SerializeField]
    public float moveSpeed = 16f;

    bool updating;
    SpriteRenderer img;
    int nodeSize;

    public void Initialize(int v, Point p, Sprite piece, int size){
        img = GetComponent<SpriteRenderer>();
        rect = GetComponent<RectTransform>();
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
        pos = new Vector2(nodeSize/2 + (nodeSize * index.x), -nodeSize/2 - (nodeSize * index.y));
    }

    void UpdateName(){
        transform.name = "Node [" + index.x + ", " + index.y + "]";
    }

    public void MovePosition(Vector2 move){
        rect.anchoredPosition += move * Time.deltaTime * moveSpeed;
    }

    public void MovePositionTo(Vector2 move){
        rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, move, Time.deltaTime * moveSpeed);
    }

    public bool UpdatePiece(){
        if (rect == null) return false;

        if(Vector3.Distance(rect.anchoredPosition, pos) > 1){
            MovePositionTo(pos);
            updating = true;
            return true;
        }
        else {
            rect.anchoredPosition = pos;
            updating = false;
            return false;
        }
    }

    void OnMouseDown()
    {
        Debug.Log("mouse down");
        if(updating) return;
        MovePieces.instance.MovePiece(this);
    }

    void OnMouseUp()
    {
        Debug.Log("mouse up");
        MovePieces.instance.DropPiece();
    }
}
