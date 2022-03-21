using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ARVRLab.VPSService
{
    [CustomEditor(typeof(FakeCamera))]
    [CanEditMultipleObjects]
    public class FakeCameraEditor : Editor
    {
        SerializedProperty loadingType;
        SerializedProperty fakeTexture;
        SerializedProperty imagePath;

        void OnEnable()
        {
            loadingType = serializedObject.FindProperty("LoadingType");
            fakeTexture = serializedObject.FindProperty("FakeTexture");
            imagePath = serializedObject.FindProperty("ImageLocalPath");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(loadingType);
            if (serializedObject.FindProperty("LoadingType").enumValueIndex == 0)
            {
                EditorGUILayout.PropertyField(fakeTexture);
            }
            else
            {
                EditorGUILayout.PropertyField(imagePath);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}

