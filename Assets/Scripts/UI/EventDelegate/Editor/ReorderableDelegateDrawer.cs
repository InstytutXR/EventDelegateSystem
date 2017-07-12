﻿using System;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

using UIEventDelegate;

[CustomPropertyDrawer(typeof(ReorderableEventList), true)]
public class ReorderableDelegateDrawer : UnityEditor.PropertyDrawer
{
	private UnityEditorInternal.ReorderableList list;

    EventDelegate eventDelegate = new EventDelegate();

    /// <summary>
    /// The standard height size for each property line.
    /// </summary>

    const int lineHeight = 16;

    private UnityEditorInternal.ReorderableList getList(SerializedProperty property)
	{
		if (list == null)
		{
			list = new ReorderableList(property.serializedObject, property, true, true, true, true);

            list.drawElementCallback = (UnityEngine.Rect rect, int index, bool isActive, bool isFocused) =>
			{
                rect.width -= 10;
                rect.x += 8;

                SerializedProperty elemtProp = property.GetArrayElementAtIndex(index);

                if (EditorApplication.isCompiling)
                {
                    SerializedProperty updateMethodsProp = elemtProp.FindPropertyRelative("mUpdateEntryList");
                    if(updateMethodsProp != null)
                        updateMethodsProp.boolValue = true;
                }

                EditorGUI.PropertyField(rect, elemtProp, true);
			};

            list.elementHeightCallback = (index) =>
            {
                var element = property.GetArrayElementAtIndex(index);

                float yOffset = 12;

                SerializedProperty showGroup = element.FindPropertyRelative("mShowGroup");
                if (!showGroup.boolValue)
                    return lineHeight + 1;

                float lines = (3 * lineHeight) + yOffset;

                SerializedProperty targetProp = element.FindPropertyRelative("mTarget");
                if (targetProp.objectReferenceValue == null)
                    return lines;

                lines += lineHeight;

                SerializedProperty methodProp = element.FindPropertyRelative("mMethodName");

                if (methodProp.stringValue == "<Choose>" || methodProp.stringValue.StartsWith("<Missing - "))
                    return lines;

				eventDelegate.target = targetProp.objectReferenceValue;
                eventDelegate.methodName = methodProp.stringValue;

                if (eventDelegate.isValid == false)
                    return lines;

                SerializedProperty paramArrayProp = element.FindPropertyRelative("mParameters");
                EventDelegate.Parameter[] ps = eventDelegate.parameters;

                if (ps != null)
                {
					int imax = ps.Length;
                    paramArrayProp.arraySize = imax;
                    for (int i = 0; i < imax; i++)
                    {
                        EventDelegate.Parameter param = ps[i];

                        lines += lineHeight;

                        SerializedProperty paramProp = paramArrayProp.GetArrayElementAtIndex(i);
                        SerializedProperty objProp = paramProp.FindPropertyRelative("obj");

                        bool useManualValue = paramProp.FindPropertyRelative("paramRefType").enumValueIndex == (int)ParameterType.Value;

                        if (useManualValue)
                        {
                            if (param.expectedType == typeof(string) || param.expectedType == typeof(int) ||
                                param.expectedType == typeof(float) || param.expectedType == typeof(double) ||
                                param.expectedType == typeof(bool) || param.expectedType.IsEnum ||
                                param.expectedType == typeof(Color))
                            {
                                continue;
                            }
                            else if (param.expectedType == typeof(Vector2) || param.expectedType == typeof(Vector3) || param.expectedType == typeof(Vector4))
                            {
                                //TODO: use minimalist method
                                lines += 2f;
                                continue;
                            }
                        }

                        UnityEngine.Object obj = objProp.objectReferenceValue;

                        if (obj == null)
                            continue;

                        Type type = obj.GetType();

                        GameObject selGO = null;
                        if (type == typeof(GameObject))
                            selGO = obj as GameObject;
                        else if (type.IsSubclassOf(typeof(Component)))
                            selGO = (obj as Component).gameObject;

                        if (selGO != null)
                            lines += lineHeight;
                    }
                }

                return lines - lineHeight/2;
            };
        }

        return list;
	}

	public override float GetPropertyHeight(SerializedProperty property, UnityEngine.GUIContent label)
	{
        if (list == null)
            list = getList(property.FindPropertyRelative("List"));

        return list.GetHeight();
	}

	public override void OnGUI(UnityEngine.Rect position, SerializedProperty property, UnityEngine.GUIContent label)
	{
        if(list == null)
        {
            var listProperty = property.FindPropertyRelative("List");

            list = getList(listProperty);
        }

        if(list != null)
        {
            list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, property.name);
            };

            list.DoList(position);
        }
        
	}
}