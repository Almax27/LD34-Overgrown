using UnityEngine;
using UnityEditor;
using System.Collections;

namespace FuncWorks.Unity.UTiled {

    [CustomEditor(typeof(UTiledImportSettings))]
    public class UTiledImportSettingsInspector : Editor {
        UTiledImportSettings _settings;

        void OnEnable() {
            _settings = (UTiledImportSettings)target;
        }

        public override void OnInspectorGUI() {
            float width = Screen.width - 26;
            float w1 = width * .50f;
            float w2 = width * .25f;
            float w3 = width * .25f;

            EditorGUILayout.BeginVertical();
            GUILayout.Label("UTiled Import Settings", EditorStyles.boldLabel);

            if (_settings.TileLayerSettings.Length > 0) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Tile Layers", EditorStyles.boldLabel, GUILayout.Width(w1));
                EditorGUILayout.LabelField("Render", EditorStyles.boldLabel, GUILayout.Width(w2));
                EditorGUILayout.LabelField("Collision", EditorStyles.boldLabel, GUILayout.Width(w3));
                EditorGUILayout.EndHorizontal();

                GUI.changed = false;
                EditorGUI.indentLevel = 1;
                for (int i = 0; i < _settings.TileLayerSettings.Length; i++) {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(_settings.TileLayerSettings[i].LayerName, GUILayout.Width(w1));
                    _settings.TileLayerSettings[i].GenerateRenderMesh = EditorGUILayout.Toggle(_settings.TileLayerSettings[i].GenerateRenderMesh, GUILayout.Width(w2));
                    _settings.TileLayerSettings[i].GenerateCollisionMesh = EditorGUILayout.Toggle(_settings.TileLayerSettings[i].GenerateCollisionMesh, GUILayout.Width(w2));
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (_settings.ObjectLayerSettings.Length > 0) {
                EditorGUI.indentLevel = 0;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Object Layers", EditorStyles.boldLabel, GUILayout.Width(w1));
                EditorGUILayout.LabelField("Import", EditorStyles.boldLabel, GUILayout.Width(w2));
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel = 1;
                for (int i = 0; i < _settings.ObjectLayerSettings.Length; i++) {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(_settings.ObjectLayerSettings[i].LayerName, GUILayout.Width(w1));
                    _settings.ObjectLayerSettings[i].ImportLayer = EditorGUILayout.Toggle(_settings.ObjectLayerSettings[i].ImportLayer, GUILayout.Width(w2));
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUI.indentLevel = 0;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pixels Per Unit", EditorStyles.boldLabel, GUILayout.Width(w1));
            _settings.PixelsPerUnit = EditorGUILayout.IntField(_settings.PixelsPerUnit, GUILayout.Width(w2));
            EditorGUILayout.EndHorizontal();

            if (_settings.PixelsPerUnit <= 0) {
                _settings.PixelsPerUnit = 100;
                GUI.changed = true;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Generate Prefabs", GUILayout.ExpandWidth(false))) {
                EditorUtility.DisplayProgressBar("UTiled", "Reading Map Data (keep calm, carry on)", 0);
                UTiledMapReader.Import(_settings);
                EditorUtility.ClearProgressBar();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            if (GUI.changed)
                EditorUtility.SetDirty(_settings);
        }
    }
}
