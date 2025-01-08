[System.Serializable]
public class Node {
    public INodeType typeVal;
    public Point index;
    NodePiece piece;

    public Node(INodeType t, Point i) {
        index = i;
        typeVal = t;
    }

    public INodeType GetValue() {
        return typeVal;
    }

    public void SetValue(INodeType val) {
        typeVal = val;
    }

    public void SetPiece(NodePiece p) {
        piece = p;
        typeVal = (p == null) ? new BlankType(): p.NodeVal;
        if (piece == null) return;
        piece.SetIndex(index);
    }

    public NodePiece GetPiece() {
        return piece;
    }
}