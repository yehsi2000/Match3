using System.IO;
using UnityEngine;

public interface INodeType{
    bool isEqual(INodeType node);
}

public class BlockedNodeType : INodeType {

    public BlockedNodeType() {}

    public bool isEqual(INodeType node) {
        if (node is BlockedNodeType) {
            return true;
        } else {
            return false;
        }
    }
    override public string ToString() {
        return "BlockedType";
    }
}

public class BlankType : INodeType {

    public BlankType() { }

    public bool isEqual(INodeType node) {
        if (node is BlankType) {
            return true;
        } else {
            return false;
        }
    }
    override public string ToString() {
        return "BlankType";
    }
}

public class NormalType : INodeType {

    public NormalType(ENormalType type) {
        if ((int)type >= 5) 
            throw new InvalidDataException();
        typeval = type;
    }

    public enum ENormalType {
        //HOLE = -1,
        //BLANK = 0,
        DAVI = 0,
        LIZA = 1,
        MONA = 2,
        MITRA = 3,
        SANGA = 4
    }

    private ENormalType typeval;

    public ENormalType TypeVal {
        get { return typeval; }
    }

    override public string ToString() {
        return TypeVal.ToString();
    }

    public bool isEqual(INodeType node) {
        if (node is NormalType) {
            NormalType normal = node as NormalType;
            return normal.TypeVal == this.typeval;
        } else {
            return false;
        }
    }
}

public class SpecialType : INodeType {

    public SpecialType(ESpecialType type) {
        typeval = type;
    }

    public enum ESpecialType {
        DAVISP=0,
        LIZASP=1,
        MONASP=2,
        MITRASP=3,
        SANGASP=4,
        SITRISP = 100,
    }

    private ESpecialType typeval;

    public ESpecialType TypeVal {
        get { return typeval; }
    }

    public bool isEqual(INodeType node) {
        if (node is SpecialType) {
            SpecialType special = node as SpecialType;
            return special.TypeVal == this.typeval;
        } else {
            return false;
        }
    }
    override public string ToString() {
        return TypeVal.ToString();
    }
}