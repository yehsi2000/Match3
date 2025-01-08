using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{

    [SerializeField]
    private int width = 9;

    [SerializeField]
    private int height = 14;

    [SerializeField]
    private readonly float nodeSize = 2f;

    public float NodeSize {
        get { return nodeSize; }
    }

    [HideInInspector]
    public Node[,] board;

    public Node[,] Board {
        get { return board; }
        set { board = value; }
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
}
