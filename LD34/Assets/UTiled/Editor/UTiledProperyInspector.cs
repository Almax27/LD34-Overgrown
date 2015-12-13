using UnityEngine;
using UnityEditor;
using System.Collections;

namespace FuncWorks.Unity.UTiled {
    [CustomEditor(typeof(UTiledProperties))]
    public class UTiledProperyInspector : Editor {
        UTiledProperties _props;

        void OnEnable() {
            _props = (UTiledProperties)target;
        }

        public override void OnInspectorGUI() {
            float width = Screen.width - 26;

            EditorGUILayout.BeginVertical();
            GUILayout.Label("UTiled Properties", EditorStyles.boldLabel);

            for (int i = 0; i < _props.Properties.Length; i++) {
                UTiledProperty prop = _props.Properties[i];
                EditorGUILayout.BeginHorizontal();
                string newName = EditorGUILayout.TextArea(prop.Name, GUILayout.Width(width * .4f));
                string newValue = EditorGUILayout.TextArea(prop.Value, GUILayout.Width(width * .4f));
                bool delete = GUILayout.Button("delete", GUILayout.Width(width * .2f));

                if (delete)
                    _props.DeleteProperty(i);
                else if (!newName.Equals(prop.Name))
                    _props.RenameProperty(i, newName);
                else if (!newValue.Equals(prop.Value))
                    _props.SetValue(i, newValue);

                EditorGUILayout.EndHorizontal();
            }

            if(GUILayout.Button("add property", GUILayout.Width(width * .4f)))
                _props.Add(string.Empty, string.Empty);


            GUILayout.Label("UTiled Object Type", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Object Type", GUILayout.Width(width * .4f));
            _props.ObjectType = EditorGUILayout.TextArea(_props.ObjectType, GUILayout.Width(width * .6f));
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            if (GUI.changed) {
                EditorUtility.SetDirty(_props);
            }
        }
    }

}