using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomPropertyDrawer(typeof(ArrayLayout))]
public class CustPropertyDrawer : PropertyDrawer {

	public override void OnGUI(Rect position,SerializedProperty property,GUIContent label){
		EditorGUI.PrefixLabel(position,label);
		Rect newposition = position;
		newposition.y += SingleGameController.getWidth()*2f;
		SerializedProperty data = property.FindPropertyRelative("rows");
        if (data.arraySize != SingleGameController.getHeight())
            data.arraySize = SingleGameController.getHeight();
		//data.rows[0][]
		for(int j=0;j<SingleGameController.getHeight();j++){
			SerializedProperty row = data.GetArrayElementAtIndex(j).FindPropertyRelative("row");
			newposition.height = SingleGameController.getHeight()*2f;
			if(row.arraySize != SingleGameController.getWidth())
				row.arraySize = SingleGameController.getWidth();
			newposition.width = position.width/SingleGameController.getWidth();
			for(int i=0;i<SingleGameController.getWidth();i++){
				EditorGUI.PropertyField(newposition,row.GetArrayElementAtIndex(i),GUIContent.none);
				newposition.x += newposition.width;
			}

			newposition.x = position.x;
			newposition.y += 18f;
		}
	}

	public override float GetPropertyHeight(SerializedProperty property,GUIContent label){
		return SingleGameController.getHeight()*2f * 10;
	}
}
