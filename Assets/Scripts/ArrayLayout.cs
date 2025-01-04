using UnityEngine;
using System.Collections;

[System.Serializable]
public class ArrayLayout  {

	[System.Serializable]
	public struct rowData{
		public bool[] row;
	}

    public Grid grid;
    public rowData[] rows = new rowData[Match3.getHeight()];
}
