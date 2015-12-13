using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;

namespace FuncWorks.Unity.UTiled {
    public class UTiledMapProcessor : AssetPostprocessor {
        public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath) {
            foreach (var asset in importedAssets) {
                if (Path.GetExtension(asset).ToLower().Equals(".tmx")) {

                    string settingsFileName = string.Format("{0}/{1}.asset", Path.GetDirectoryName(asset), Path.GetFileNameWithoutExtension(asset));
                    UTiledImportSettings settings = (UTiledImportSettings)AssetDatabase.LoadAssetAtPath(settingsFileName, typeof(UTiledImportSettings));
                    if (settings == null) {
                        settings = ScriptableObject.CreateInstance<UTiledImportSettings>();
                        settings.PixelsPerUnit = 100;
                        AssetDatabase.CreateAsset(settings, settingsFileName);
                    }

                    settings.MapFilename = asset;
                    settings.TileLayerSettings = CreateTileLayerSettings(asset);
                    settings.ObjectLayerSettings = CreateObjectLayerSettings(asset);
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        private static UTiledObjectLayerSetting[] CreateObjectLayerSettings(string mapFilename) {
            List<UTiledObjectLayerSetting> results = new List<UTiledObjectLayerSetting>();
            XDocument input = XDocument.Load(mapFilename);

            foreach (var lElem in input.Document.Root.Elements("objectgroup")) {
                UTiledObjectLayerSetting setting = new UTiledObjectLayerSetting();

                setting.LayerName = lElem.Attribute("name") == null ? "Unnamed" : lElem.Attribute("name").Value;
                setting.ImportLayer = true;

                results.Add(setting);
            }

            return results.ToArray();
        }

        private static UTiledTileLayerSetting[] CreateTileLayerSettings(string mapFilename) {
            List<UTiledTileLayerSetting> results = new List<UTiledTileLayerSetting>();
            XDocument input = XDocument.Load(mapFilename);

            foreach (var lElem in input.Document.Root.Elements("layer")) {
                UTiledTileLayerSetting setting = new UTiledTileLayerSetting();

                setting.LayerName = lElem.Attribute("name") == null ? "Unnamed" : lElem.Attribute("name").Value;
                setting.GenerateRenderMesh = lElem.Attribute("visible") == null ? true : lElem.Attribute("visible").Equals("1");
                setting.GenerateCollisionMesh = false;

                results.Add(setting);
            }

            return results.ToArray();
        }
    }
}
