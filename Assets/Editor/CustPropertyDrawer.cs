using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomPropertyDrawer(typeof(ArrayLayout))]
public class CustPropertyDrawer : PropertyDrawer {

	public override void OnGUI(Rect position,SerializedProperty property,GUIContent label){
		EditorGUI.PrefixLabel(position,label);
		Rect newposition = position;
		newposition.y += Match3.getWidth()*2f;
		SerializedProperty data = property.FindPropertyRelative("rows");
        if (data.arraySize != Match3.getHeight())
            data.arraySize = Match3.getHeight();
		//data.rows[0][]
		for(int j=0;j<Match3.getHeight();j++){
			SerializedProperty row = data.GetArrayElementAtIndex(j).FindPropertyRelative("row");
			newposition.height = Match3.getHeight()*2f;
			if(row.arraySize != Match3.getWidth())
				row.arraySize = Match3.getWidth();
			newposition.width = position.width/Match3.getWidth();
			for(int i=0;i<Match3.getWidth();i++){
				EditorGUI.PropertyField(newposition,row.GetArrayElementAtIndex(i),GUIContent.none);
				newposition.x += newposition.width;
			}

			newposition.x = position.x;
			newposition.y += 18f;
		}
	}

	public override float GetPropertyHeight(SerializedProperty property,GUIContent label){
		return Match3.getHeight()*2f * 10;
	}
}
