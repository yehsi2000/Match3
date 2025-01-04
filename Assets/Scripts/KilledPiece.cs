using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.Events;

public class KilledPiece : MonoBehaviour
{
    public bool falling;

    public float speed = 16f;
    public float gravity = 32f;
    static internal UnityEvent<KilledPiece> onKilledPieceRemove = new UnityEvent<KilledPiece>();
    Vector2 moveDir;
    
    SpriteRenderer img;
    
    // Start is called before the first frame update
    public void Initialize(Sprite piece, Vector2 start, float size)
    {
        falling = true;
        moveDir = Vector2.up;
        moveDir.x = Random.Range(-1.0f, 1.0f);
        moveDir *= speed / 2;

        img = GetComponent<SpriteRenderer>();
        
        img.sprite = piece;
        transform.position = start;
        transform.localScale = new Vector3(size / 2.5f, size / 2.5f, 1);
    }

    void Update()
    {
        if (!falling) return;
        moveDir.y -= Time.deltaTime * gravity;
        moveDir.x = Mathf.Lerp(moveDir.x, 0, Time.deltaTime);
        transform.position += new Vector3(moveDir.x * Time.deltaTime * speed,moveDir.y * Time.deltaTime,0);
        //Debug.Log(GetComponent<Renderer>().isVisible);
        //if (rect.position.x < -32f || rect.position.x > Screen.width + 32f || rect.position.y < -32f || rect.position.y > Screen.height + 32f)
        if (!GetComponent<Renderer>().isVisible) {
            //falling = false;
            onKilledPieceRemove.Invoke(this);
            Destroy(this.gameObject);
        }
            
            
    }
}
