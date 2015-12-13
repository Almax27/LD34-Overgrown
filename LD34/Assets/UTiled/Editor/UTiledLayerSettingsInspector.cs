using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using UnityEditorInternal;
using System.Reflection;
using System.Collections.Generic;

namespace FuncWorks.Unity.UTiled {

    [CustomEditor(typeof(UTiledLayerSettings))]
    public class UTiledLayerSettingsInspector : Editor {
        UTiledLayerSettings _settings;
        Dictionary<int, string> _sortlayers;
        List<string> _sortingLayerNames = new List<string>();

        void OnEnable() {
            _settings = (UTiledLayerSettings)target;

            //storinglayer names are currently not exposed - hopefully this changes soon
            _sortingLayerNames.Clear();
            _sortingLayerNames.AddRange((string[])typeof(InternalEditorUtility).GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null, new object[0]));
        }

        public override void OnInspectorGUI() {
            GUI.changed = false;
            int selectedLayer = _sortingLayerNames.IndexOf(_settings.sortingLayerName);
            
            EditorGUILayout.BeginVertical();
            GUILayout.Label("UTiled Layer Settings", EditorStyles.boldLabel);
            selectedLayer = EditorGUILayout.Popup("Sorting Layer", selectedLayer, _sortingLayerNames.ToArray());
            _settings.sortingOrder = EditorGUILayout.IntField("Order in Layer", _settings.sortingOrder);
            _settings.opacity = EditorGUILayout.Slider("Opacity", _settings.opacity, 0f, 1f);
            EditorGUILayout.EndVertical();

            if (GUI.changed) {
                _settings.sortingLayerName = _sortingLayerNames[selectedLayer];
                
                foreach (var renderer in _settings.transform.GetComponentsInChildren<MeshRenderer>()) {
                    renderer.sortingLayerName = _settings.sortingLayerName;
                    renderer.sortingOrder = _settings.sortingOrder;
                    renderer.sharedMaterial.color = new Color(1, 1, 1, _settings.opacity);
                    EditorUtility.SetDirty(renderer);
                }

                foreach (var renderer in _settings.transform.GetComponentsInChildren<SpriteRenderer>()) {
                    renderer.sortingLayerName = _settings.sortingLayerName;
                    renderer.sortingOrder = _settings.sortingOrder;
                    renderer.color = new Color(1, 1, 1, _settings.opacity);
                    EditorUtility.SetDirty(renderer);
                }
                
                EditorUtility.SetDirty(_settings);
            }
        }
    }
}
