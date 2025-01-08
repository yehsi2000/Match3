using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomPropertyDrawer(typeof(ArrayLayout))]
public class CustPropertyDrawer : PropertyDrawer {

	public override void OnGUI(Rect position,SerializedProperty property,GUIContent label){
		EditorGUI.PrefixLabel(position,label);
		Rect newposition = position;
		if(GameManager.Instance == null)
            return;
        if (GameManager.Instance.boardManager == null)
            return;
        newposition.y += GameManager.Instance.boardManager.Width*2f;
		SerializedProperty data = property.FindPropertyRelative("rows");
		if (data == null)
			return;
        if (data.arraySize != GameManager.Instance.boardManager.Height)
            data.arraySize = GameManager.Instance.boardManager.Height;
		//data.rows[0][]
		for(int j=0;j<GameManager.Instance.boardManager.Height;j++){
			SerializedProperty row = data.GetArrayElementAtIndex(j).FindPropertyRelative("row");
			newposition.height = GameManager.Instance.boardManager.Height*2f;
			if(row.arraySize != GameManager.Instance.boardManager.Width)
				row.arraySize = GameManager.Instance.boardManager.Width;
			newposition.width = position.width/GameManager.Instance.boardManager.Width;
			for(int i=0;i<GameManager.Instance.boardManager.Width;i++){
				EditorGUI.PropertyField(newposition,row.GetArrayElementAtIndex(i),GUIContent.none);
				newposition.x += newposition.width;
			}

			newposition.x = position.x;
			newposition.y += 18f;
		}
	}

	public override float GetPropertyHeight(SerializedProperty property,GUIContent label){
		return GameManager.Instance.boardManager.Height*2f * 10;
	}
}
