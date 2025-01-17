using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameControllerBase : MonoBehaviour {
    public virtual int AutoBlockWeightMultiplier { get; }

    public virtual void SpecialBlockPressed() { }
    public virtual void ProcessMatch(Board board, List<Point> connected) { }

}