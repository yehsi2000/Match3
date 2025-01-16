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

[System.Serializable]
public class FlippedPieces {
    public NodePiece one;
    public NodePiece two;

    public FlippedPieces(NodePiece o, NodePiece t) {
        one = o;
        two = t;
    }

    public NodePiece GetOtherPiece(NodePiece p) {
        if (p == one) return two;
        else if (p == two) return one;
        else return null;
    }
}