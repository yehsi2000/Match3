using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameControllerBase : MonoBehaviour {
    public virtual int AutoBlockWeightMultiplier { get; }

    public virtual void SpecialBlockPressed(Board board) { }
    public virtual LinkedList<ValueTuple<SpecialType, int>> ProcessMatch(Board board, List<Point> connected) { return null; }
    public virtual void GameOver() { }
}