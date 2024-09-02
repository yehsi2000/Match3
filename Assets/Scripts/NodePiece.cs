using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class NodePiece : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
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
    Image img;
    int nodeSize;

    public void Initialize(int v, Point p, Sprite piece, int size){
        img = GetComponent<Image>();
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

    public void OnPointerDown(PointerEventData eventData)
    {
        if(updating) return;
        MovePieces.instance.MovePiece(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        MovePieces.instance.DropPiece();
    }
}
