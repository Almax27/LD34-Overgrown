using UnityEngine;
using System.Collections.Generic;
using System;


namespace FuncWorks.Unity.UTiled {

    [Serializable]
    public class UTiledImportSettings : ScriptableObject {

        [SerializeField]
        public UTiledTileLayerSetting[] TileLayerSettings;

        [SerializeField]
        public UTiledObjectLayerSetting[] ObjectLayerSettings;

        [SerializeField]
        public string MapFilename;

        [SerializeField]
        public int PixelsPerUnit;

    }

    [Serializable]
    public class UTiledTileLayerSetting {

        [SerializeField]
        public string LayerName;
        
        [SerializeField]
        public bool GenerateRenderMesh;

        [SerializeField]
        public bool GenerateCollisionMesh;

        [SerializeField]
        public bool ImportLayer;
    }

    [Serializable]
    public class UTiledObjectLayerSetting {

        [SerializeField]
        public string LayerName;

        [SerializeField]
        public bool ImportLayer;
    }
}