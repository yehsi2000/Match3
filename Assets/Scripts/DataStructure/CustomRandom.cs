using System;
using System.Diagnostics;
using UnityEngine;

public class CustomRandom : System.Random {
    System.Random r;
    public CustomRandom(int a) {
        r = new System.Random(a);
    }
    public override int Next(int minValue, int maxValue) {
        int val = r.Next(minValue, maxValue);
        //Console.WriteLine("randval = "+val);
        return val;
    }
}