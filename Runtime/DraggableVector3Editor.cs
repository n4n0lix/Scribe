using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Draggable : PropertyAttribute { }


#if UNITY_EDITOR
[CustomEditor(typeof(MonoBehaviour), true)]
public class DraggableVector3Editor : Editor
{

    readonly GUIStyle style = new GUIStyle();

    void OnEnable()
    {
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;
    }

    public void OnSceneGUI()
    {
        var property = serializedObject.GetIterator();
        while (property.Next(true))
        {
            if (property.propertyType == SerializedPropertyType.Generic && property.isArray)
            {
                if (property.arrayElementType == "Vector3")
                {
                    var field = serializedObject.targetObject.GetType().GetField(property.name);
                    if (field == null)
                        continue;

                    var draggablePoints = field.GetCustomAttributes(typeof(Draggable), false);
                    if (draggablePoints.Length <= 0)
                        continue;

                    for(int i = 0; i < property.arraySize; i++)
                    {
                        var subProp = property.GetArrayElementAtIndex(i);
                        Handles.Label(subProp.vector3Value, property.displayName);
                        subProp.vector3Value = Handles.PositionHandle(subProp.vector3Value, Quaternion.identity);
                    }

                    serializedObject.ApplyModifiedProperties();
                }
            }

            if (property.propertyType == SerializedPropertyType.Vector3)
            {
                var field = serializedObject.targetObject.GetType().GetField(property.name);
                if (field == null)
                {
                    continue;
                }
                var draggablePoints = field.GetCustomAttributes(typeof(Draggable), false);
                if (draggablePoints.Length > 0)
                {
                    Handles.Label(property.vector3Value, property.name);
                    property.vector3Value = Handles.PositionHandle(property.vector3Value, Quaternion.identity);
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}
#endif