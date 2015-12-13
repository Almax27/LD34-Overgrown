using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Xml.Linq;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace FuncWorks.Unity.UTiled {

    public class UTiledMapReader {

        private const UInt32 FLIPPED_HORIZONTALLY_FLAG = 0x80000000;
        private const UInt32 FLIPPED_VERTICALLY_FLAG = 0x40000000;
        private const UInt32 FLIPPED_DIAGONALLY_FLAG = 0x20000000;

        private enum MapOrientation {
            Orthogonal,
            Isometric,
            Staggered,
            Unknown
        }

        private class MeshData {
            public String materialName = String.Empty;
            public List<Vector3> verts = new List<Vector3>();
            public List<Vector3> colverts = new List<Vector3>();
            public List<int> tris = new List<int>();
            public List<Vector3> norms = new List<Vector3>();
            public List<Vector2> uvs = new List<Vector2>();
        }

        private class SourceData {
            public Int32 offsetX = 0;
            public Int32 offsetY = 0;
            public Vector2 textureSize = new Vector2();
            public String spriteName;
        }

        public static void Import(UTiledImportSettings settings) {
            String mapFilename = settings.MapFilename;
            XDocument input = XDocument.Load(mapFilename);

            String mapDirectory = Path.GetDirectoryName(mapFilename) + "/" + Path.GetFileNameWithoutExtension(mapFilename);
            String meshDirectory = mapDirectory + "/" + "meshes";
            String matDirectory = mapDirectory + "/" + "materials";

            if (!Directory.Exists(mapDirectory))
                Directory.CreateDirectory(mapDirectory);
            if (!Directory.Exists(meshDirectory))
                Directory.CreateDirectory(meshDirectory);
            if (!Directory.Exists(matDirectory))
                Directory.CreateDirectory(matDirectory);

            MapOrientation mapOrientation = MapOrientation.Unknown;
            switch (input.Document.Root.Attribute("orientation").Value) {
                case "orthogonal":
                    mapOrientation = MapOrientation.Orthogonal;
                    break;

                case "isometric":
                    mapOrientation = MapOrientation.Isometric;
                    break;

                case "staggered":
                    mapOrientation = MapOrientation.Staggered;
                    break;

                default:
                    mapOrientation = MapOrientation.Unknown;
                    break;
            }

            if (mapOrientation != MapOrientation.Orthogonal)
                throw new NotSupportedException("UTiled supports only orthogonal maps");

            String mapName = Path.GetFileNameWithoutExtension(mapFilename);
            Int32 mapWidth = Convert.ToInt32(input.Document.Root.Attribute("width").Value);
            Int32 mapHeight = Convert.ToInt32(input.Document.Root.Attribute("height").Value);
            Int32 mapTileWidth = Convert.ToInt32(input.Document.Root.Attribute("tilewidth").Value);
            Int32 mapTileHeight = Convert.ToInt32(input.Document.Root.Attribute("tileheight").Value);

            Dictionary<UInt32, SourceData> gid2sprite = new Dictionary<UInt32, SourceData>();
            Dictionary<String, List<UTiledProperty>> spriteprops = new Dictionary<string, List<UTiledProperty>>();
            List<String> imageList = new List<string>();

            GameObject mapGameObject = new GameObject();
            mapGameObject.name = mapName;

            if (input.Document.Root.Element("properties") != null) {
                UTiledProperties props = mapGameObject.AddComponent<UTiledProperties>();
                foreach (var pElem in input.Document.Root.Element("properties").Elements("property"))
                    props.Add(pElem.Attribute("name").Value, pElem.Attribute("value").Value);
            }

            Int32 tsNum = 1;
            foreach (var elem in input.Document.Root.Elements("tileset")) {
                UInt32 FirstGID = Convert.ToUInt32(elem.Attribute("firstgid").Value);
                XElement tsElem = elem;

                if (elem.Attribute("source") != null) {
                    XDocument tsx = XDocument.Load(Path.Combine(Path.GetDirectoryName(mapFilename), elem.Attribute("source").Value));
                    tsElem = tsx.Root;
                }

                List<UTiledProperty> tilesetProps = new List<UTiledProperty>();
                if (tsElem.Element("properties") != null)
                    foreach (var pElem in tsElem.Element("properties").Elements("property"))
                        tilesetProps.Add(new UTiledProperty() { Name = pElem.Attribute("name").Value, Value = pElem.Attribute("value").Value });

                Int32 tsTileWidth = tsElem.Attribute("tilewidth") == null ? 0 : Convert.ToInt32(tsElem.Attribute("tilewidth").Value);
                Int32 tsTileHeight = tsElem.Attribute("tileheight") == null ? 0 : Convert.ToInt32(tsElem.Attribute("tileheight").Value);
                Int32 tsSpacing = tsElem.Attribute("spacing") == null ? 0 : Convert.ToInt32(tsElem.Attribute("spacing").Value);
                Int32 tsMargin = tsElem.Attribute("margin") == null ? 0 : Convert.ToInt32(tsElem.Attribute("margin").Value);

                Int32 tsTileOffsetX = 0;
                Int32 tsTileOffsetY = 0;
                if (tsElem.Element("tileoffset") != null) {
                    tsTileOffsetX = Convert.ToInt32(tsElem.Element("tileoffset").Attribute("x").Value);
                    tsTileOffsetY = Convert.ToInt32(tsElem.Element("tileoffset").Attribute("y").Value);
                }

                if (tsElem.Element("image") != null) {
                    XElement imgElem = tsElem.Element("image");
                    String tsImageFileName = Path.Combine(Path.GetDirectoryName(mapFilename), imgElem.Attribute("source").Value);

                    if (!File.Exists(tsImageFileName))
                        throw new Exception("Cannot find referenced tilesheet: " + tsImageFileName);

                    if (!imageList.Contains(tsImageFileName))
                        imageList.Add(tsImageFileName);

                    TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(tsImageFileName);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Multiple;
                    importer.filterMode = FilterMode.Point;
                    importer.spritePixelsToUnits = settings.PixelsPerUnit;

                    // Reflection Hack because this is a private method to get the non scaled size of the texture
                    object[] args = new object[2] { 0, 0 };
                    MethodInfo mi = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
                    mi.Invoke(importer, args);
                    Int32 tsImageWidth = (int)args[0];
                    Int32 tsImageHeight = (int)args[1];
                    // yea, it's lame and should be a public method - vote for it here: 
                    // http://feedback.unity3d.com/suggestions/get-original-width-and-height-of

                    List<SpriteMetaData> spritesmeta = new List<SpriteMetaData>(importer.spritesheet);
                    UInt32 gid = FirstGID;

                    for (int y = tsImageHeight - tsTileHeight - tsMargin; y >= tsMargin; y -= tsTileHeight + tsSpacing) {
                        for (int x = tsMargin; x < tsImageWidth - tsMargin; x += tsTileWidth + tsSpacing) {
                            if (x + tsTileWidth > tsImageWidth - tsMargin)
                                continue;

                            SpriteMetaData s = new SpriteMetaData();
                            s.name = String.Format(String.Format("{0}_{1}_{2}", Path.GetFileNameWithoutExtension(mapFilename), tsNum, gid));
                            s.rect = new Rect(x, y, tsTileWidth, tsTileHeight);
                            s.pivot = new Vector2(tsTileWidth / 2, tsTileHeight / 2);
                            s.alignment = 0;

                            if (tilesetProps.Count > 0) {
                                spriteprops[s.name] = new List<UTiledProperty>();
                                foreach (var item in tilesetProps)
                                    spriteprops[s.name].Add(item);
                            }

                            if (spritesmeta.Any(sx => sx.name.Equals(s.name)))
                                spritesmeta.RemoveAll(sx => sx.name.Equals(s.name));

                            spritesmeta.Add(s);
                            gid2sprite[gid] = new SourceData() {
                                textureSize = new Vector2(tsImageWidth, tsImageHeight),
                                offsetX = tsTileOffsetX,
                                offsetY = tsTileOffsetY,
                                spriteName = s.name,
                            };
                            gid++;
                        }
                    }

                    foreach (var tileElem in tsElem.Elements("tile")) {
                        UInt32 id = Convert.ToUInt32(tileElem.Attribute("id").Value);
                        String spriteName = String.Format(String.Format("{0}_{1}_{2}", Path.GetFileNameWithoutExtension(mapFilename), tsNum, id + FirstGID));
                        if (tileElem.Element("properties") != null) {
                            if (!spriteprops.ContainsKey(spriteName))
                                spriteprops[spriteName] = new List<UTiledProperty>();

                            foreach (var pElem in tileElem.Element("properties").Elements("property"))
                                spriteprops[spriteName].Add(new UTiledProperty() { Name = pElem.Attribute("name").Value, Value = pElem.Attribute("value").Value });
                        }
                    }

                    importer.spritesheet = spritesmeta.ToArray();
                    AssetDatabase.WriteImportSettingsIfDirty(tsImageFileName);
                    AssetDatabase.LoadAssetAtPath(tsImageFileName, typeof(Texture2D));
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                }

                tsNum++;
            }

            List<UnityEngine.Object> sprites = new List<UnityEngine.Object>();
            foreach (var tsImageFileName in imageList)
                sprites.AddRange(AssetDatabase.LoadAllAssetRepresentationsAtPath(tsImageFileName));

            Int32 lCount = 1;
            Int32 olCount = 1;
            Int32 sortOrder = 1;
            float xoff = mapWidth * mapTileWidth - mapWidth * mapTileWidth / 2;
            float yoff = mapHeight * mapTileHeight - mapHeight * mapTileHeight / 2 + mapTileHeight;

            foreach (var lElem in input.Document.Root.Elements()) {
                if (lElem.Name.LocalName.Equals("layer")) {
                    bool importMesh = false;
                    bool importCollision = false;

                    if (settings.TileLayerSettings.Length >= lCount) {
                        importCollision = settings.TileLayerSettings[lCount - 1].GenerateCollisionMesh;
                        importMesh = settings.TileLayerSettings[lCount - 1].GenerateRenderMesh;
                    }

                    if (!importMesh && !importCollision) {
                        lCount++;
                        continue;
                    }

                    GameObject layerGameObject = new GameObject();
                    layerGameObject.transform.parent = mapGameObject.transform;

                    layerGameObject.name = lElem.Attribute("name") == null ? "Unnamed" : lElem.Attribute("name").Value;
                    float layerOpacity = lElem.Attribute("opacity") == null ? 1.0f : Convert.ToSingle(lElem.Attribute("opacity").Value);
                    bool layerVisible = lElem.Attribute("visible") == null ? true : lElem.Attribute("visible").Equals("1");

                    UTiledLayerSettings layerSettings = layerGameObject.AddComponent<UTiledLayerSettings>();
                    layerSettings.sortingOrder = sortOrder;
                    layerSettings.opacity = layerOpacity;

                    EditorUtility.DisplayProgressBar("UTiled", "Generating Prefabs for " + settings.TileLayerSettings[lCount - 1].LayerName, (float)(lCount - 1) / settings.TileLayerSettings.Length);

                    if (lElem.Element("properties") != null) {
                        UTiledProperties props = layerGameObject.AddComponent<UTiledProperties>();
                        foreach (var pElem in lElem.Element("properties").Elements("property"))
                            props.Add(pElem.Attribute("name").Value, pElem.Attribute("value").Value);
                    }

                    if (lElem.Element("data") != null) {
                        List<UInt32> gids = new List<UInt32>();
                        if (lElem.Element("data").Attribute("encoding") != null || lElem.Element("data").Attribute("compression") != null) {

                            // parse csv formatted data
                            if (lElem.Element("data").Attribute("encoding") != null && lElem.Element("data").Attribute("encoding").Value.Equals("csv")) {
                                foreach (var gid in lElem.Element("data").Value.Split(",\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                                    gids.Add(Convert.ToUInt32(gid));
                            }
                            else if (lElem.Element("data").Attribute("encoding") != null && lElem.Element("data").Attribute("encoding").Value.Equals("base64")) {
                                Byte[] data = Convert.FromBase64String(lElem.Element("data").Value);

                                if (lElem.Element("data").Attribute("compression") == null) {
                                    // uncompressed data
                                    for (int i = 0; i < data.Length; i += sizeof(UInt32)) {
                                        gids.Add(BitConverter.ToUInt32(data, i));
                                    }
                                }
                                else {
                                    throw new NotSupportedException(String.Format("Compression '{0}' not supported.", lElem.Element("data").Attribute("compression").Value));
                                }
                            }
                            else {
                                throw new NotSupportedException(String.Format("Encoding '{0}' not supported.  UTiled supports csv or base64", lElem.Element("data").Attribute("encoding").Value));
                            }
                        }
                        else {
                            // parse xml formatted data
                            foreach (var tElem in lElem.Element("data").Elements("tile"))
                                gids.Add(Convert.ToUInt32(tElem.Attribute("gid").Value));
                        }

                        List<MeshData> meshes = new List<MeshData>();
                        for (int i = 0; i < gids.Count; i++) {
                            UInt32 ID = gids[i] & ~(FLIPPED_HORIZONTALLY_FLAG | FLIPPED_VERTICALLY_FLAG | FLIPPED_DIAGONALLY_FLAG);

                            if (ID > 0) {
                                SourceData sd = gid2sprite[ID];
                                Sprite source = (Sprite)sprites.First(s => s.name.Equals(sd.spriteName));
                                String texFile = AssetDatabase.GetAssetPath(source.texture);

                                Boolean tdFlipHorizontally = Convert.ToBoolean(gids[i] & FLIPPED_HORIZONTALLY_FLAG);
                                Boolean tdFlipVertically = Convert.ToBoolean(gids[i] & FLIPPED_VERTICALLY_FLAG);
                                Boolean tdFlipDiagonally = Convert.ToBoolean(gids[i] & FLIPPED_DIAGONALLY_FLAG);

                                Int32 meshId = 0;
                                if (meshes.Any(m => m.materialName.Equals(texFile))) {
                                    meshId = meshes.IndexOf(meshes.First(x => x.materialName.Equals(texFile)));
                                }
                                else {
                                    meshes.Add(new MeshData() { materialName = texFile });
                                    meshId = meshes.Count - 1;
                                }

                                int triStart = meshes[meshId].verts.Count;
                                meshes[meshId].tris.AddRange(new int[] { triStart, triStart + 1, triStart + 2, triStart + 2, triStart + 3, triStart });

                                for (int j = 0; j < 4; j++)
                                    meshes[meshId].norms.Add(new Vector3(0, 0, -1));

                                if (mapOrientation == MapOrientation.Orthogonal) {
                                    float x = i % mapWidth;
                                    float y = mapHeight - Mathf.Floor(i / mapWidth) - 1;

                                    Rect colLoc = new Rect(
                                        (x * mapTileWidth - xoff) / settings.PixelsPerUnit,
                                        (y * mapTileHeight + mapTileHeight - yoff) / settings.PixelsPerUnit,
                                        (float)mapTileWidth / settings.PixelsPerUnit,
                                        (float)mapTileHeight / settings.PixelsPerUnit
                                    );

                                    meshes[meshId].colverts.Add(new Vector3(colLoc.xMin, colLoc.yMax, 0));
                                    meshes[meshId].colverts.Add(new Vector3(colLoc.xMax, colLoc.yMax, 0));
                                    meshes[meshId].colverts.Add(new Vector3(colLoc.xMax, colLoc.yMin, 0));
                                    meshes[meshId].colverts.Add(new Vector3(colLoc.xMin, colLoc.yMin, 0));

                                    Vector3[] loc = new Vector3[] {
                                        new Vector3((x * mapTileWidth - xoff + sd.offsetX) / settings.PixelsPerUnit,  (y * mapTileHeight + mapTileHeight - yoff + sd.offsetY + source.rect.height * sd.textureSize.y / source.texture.height) / settings.PixelsPerUnit),
                                        new Vector3((x * mapTileWidth - xoff + sd.offsetX + source.rect.width * sd.textureSize.x / source.texture.width) / settings.PixelsPerUnit, (y * mapTileHeight + mapTileHeight - yoff + sd.offsetY + source.rect.height * sd.textureSize.y / source.texture.height) / settings.PixelsPerUnit),
                                        new Vector3((x * mapTileWidth - xoff + sd.offsetX + source.rect.width * sd.textureSize.x / source.texture.width) / settings.PixelsPerUnit,  (y * mapTileHeight + mapTileHeight - yoff + sd.offsetY) / settings.PixelsPerUnit),
                                        new Vector3((x * mapTileWidth - xoff + sd.offsetX) / settings.PixelsPerUnit, (y * mapTileHeight + mapTileHeight - yoff + sd.offsetY) / settings.PixelsPerUnit)
                                    };

                                    if (tdFlipDiagonally) {
                                        Vector3 pivot = new Vector3(loc[0].x + (loc[1].x - loc[0].x) / 2, loc[0].y + (loc[3].y - loc[0].y) / 2);
                                        for (int j = 0; j < loc.Length; j++) {
                                            Vector3 direction = pivot - loc[j];
                                            direction = Quaternion.Euler(new Vector3(0, 0, -90)) * direction;
                                            loc[j] = direction + pivot;
                                        }
                                        tdFlipVertically = !tdFlipVertically;
                                    }
                                    if (tdFlipHorizontally) {
                                        Vector3 temp = loc[0];
                                        loc[0] = loc[1];
                                        loc[1] = temp;

                                        temp = loc[3];
                                        loc[3] = loc[2];
                                        loc[2] = temp;
                                    }
                                    if (tdFlipVertically) {
                                        Vector3 temp = loc[0];
                                        loc[0] = loc[3];
                                        loc[3] = temp;

                                        temp = loc[1];
                                        loc[1] = loc[2];
                                        loc[2] = temp;
                                    }

                                    meshes[meshId].verts.AddRange(loc);
                                }

                                Rect uvRect = new Rect(
                                    source.rect.x / source.texture.width,
                                    source.rect.y / source.texture.height,
                                    source.rect.width / source.texture.width,
                                    source.rect.height / source.texture.height
                                );

                                meshes[meshId].uvs.AddRange(new Vector2[] {
                                new Vector2(uvRect.xMin, uvRect.yMax),
                                new Vector2(uvRect.xMax, uvRect.yMax),
                                new Vector2(uvRect.xMax, uvRect.yMin),
                                new Vector2(uvRect.xMin, uvRect.yMin)
                            });


                            }
                        }
                        List<MeshData> collMesh = new List<MeshData>();
                        collMesh.Add(new MeshData());
                        Int32 currentColMesh = 0;

                        for (int i = 0; i < meshes.Count; i++) {
                            String spriteMatFileName = String.Format("{0}/{1}-{2}.mat", matDirectory, layerGameObject.name, Path.GetFileNameWithoutExtension(meshes[i].materialName));
                            Material spriteMat = (Material)AssetDatabase.LoadAssetAtPath(spriteMatFileName, typeof(Material));

                            if (importMesh) {
                                if (spriteMat == null) {
                                    spriteMat = new Material(Shader.Find("Sprites/Default"));
                                    spriteMat.SetFloat("PixelSnap", 1);
                                    spriteMat.EnableKeyword("PIXELSNAP_ON");
                                    spriteMat.color = new Color(1, 1, 1, layerOpacity);
                                    spriteMat.mainTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(meshes[i].materialName, typeof(Texture2D));
                                    AssetDatabase.CreateAsset(spriteMat, spriteMatFileName);
                                }
                                else {
                                    spriteMat.SetFloat("PixelSnap", 1);
                                    spriteMat.EnableKeyword("PIXELSNAP_ON");
                                    spriteMat.color = new Color(1, 1, 1, layerOpacity);
                                    spriteMat.mainTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(meshes[i].materialName, typeof(Texture2D));
                                    EditorUtility.SetDirty(spriteMat);
                                }
                            }

                            if (importCollision) {
                                for (int j = 0; j < meshes[i].colverts.Count; j += 4) {
                                    if (collMesh[currentColMesh].colverts.Count >= 64997) {
                                        collMesh.Add(new MeshData());
                                        currentColMesh++;
                                    }

                                    Int32 v1, v2, v3, v4;
                                    v1 = FindVertIndex(meshes[i].colverts[j], collMesh[currentColMesh].colverts);
                                    if (v1 == -1) {
                                        collMesh[currentColMesh].colverts.Add(meshes[i].colverts[j]);
                                        collMesh[currentColMesh].norms.Add(meshes[i].norms[j]);
                                        collMesh[currentColMesh].uvs.Add(meshes[i].uvs[j]);
                                        v1 = collMesh[currentColMesh].colverts.Count - 1;
                                    }

                                    v2 = FindVertIndex(meshes[i].colverts[j + 1], collMesh[currentColMesh].colverts);
                                    if (v2 == -1) {
                                        collMesh[currentColMesh].colverts.Add(meshes[i].colverts[j + 1]);
                                        collMesh[currentColMesh].norms.Add(meshes[i].norms[j + 1]);
                                        collMesh[currentColMesh].uvs.Add(meshes[i].uvs[j + 1]);
                                        v2 = collMesh[currentColMesh].colverts.Count - 1;
                                    }

                                    v3 = FindVertIndex(meshes[i].colverts[j + 2], collMesh[currentColMesh].colverts);
                                    if (v3 == -1) {
                                        collMesh[currentColMesh].colverts.Add(meshes[i].colverts[j + 2]);
                                        collMesh[currentColMesh].norms.Add(meshes[i].norms[j + 2]);
                                        collMesh[currentColMesh].uvs.Add(meshes[i].uvs[j + 2]);
                                        v3 = collMesh[currentColMesh].colverts.Count - 1;
                                    }

                                    v4 = FindVertIndex(meshes[i].colverts[j + 3], collMesh[currentColMesh].colverts);
                                    if (v4 == -1) {
                                        collMesh[currentColMesh].colverts.Add(meshes[i].colverts[j + 3]);
                                        collMesh[currentColMesh].norms.Add(meshes[i].norms[j + 3]);
                                        collMesh[currentColMesh].uvs.Add(meshes[i].uvs[j + 3]);
                                        v4 = collMesh[currentColMesh].colverts.Count - 1;
                                    }

                                    collMesh[currentColMesh].tris.AddRange(new int[] { v1, v2, v3, v3, v4, v1 });
                                }
                            }

                            if (importMesh) {
                                int totalMeshReq = Convert.ToInt32(Mathf.Ceil((float)meshes[i].verts.Count / 65000f));
                                for (int j = 0; j < totalMeshReq; j++) {

                                    var verts = meshes[i].verts.Skip(j * 65000).Take(65000).ToArray();
                                    var tris = meshes[i].tris.Skip(j * 97500).Take(97500).Select(t => t - j * 65000).ToArray();
                                    var norms = meshes[i].norms.Skip(j * 65000).Take(65000).ToArray();
                                    var uvs = meshes[i].uvs.Skip(j * 65000).Take(65000).ToArray();

                                    GameObject layerMeshObject = new GameObject();
                                    layerMeshObject.transform.parent = layerGameObject.transform;
                                    layerMeshObject.name = String.Format("{0} - {1}", Path.GetFileNameWithoutExtension(meshes[i].materialName), j + 1);

                                    MeshFilter filter = layerMeshObject.AddComponent<MeshFilter>();
                                    MeshRenderer renderer = layerMeshObject.AddComponent<MeshRenderer>();

                                    renderer.sortingOrder = sortOrder;
                                    renderer.sharedMaterial = spriteMat;
                                    renderer.castShadows = false;
                                    renderer.receiveShadows = false;

                                    String meshFileName = String.Format("{0}/{1}-{2}-{3}-{4}-{5}.asset", meshDirectory, layerGameObject.name,
                                                                        Path.GetFileNameWithoutExtension(meshes[i].materialName), lCount, i + 1, j + 1);
                                    Mesh mesh = (Mesh)AssetDatabase.LoadAssetAtPath(meshFileName, typeof(Mesh));
                                    if (mesh == null) {
                                        mesh = new Mesh() {
                                            vertices = verts,
                                            triangles = tris,
                                            normals = norms,
                                            uv = uvs,
                                        };
                                        AssetDatabase.CreateAsset(mesh, meshFileName);
                                    }
                                    else {
                                        mesh.Clear(false);
                                        mesh.vertices = verts;
                                        mesh.triangles = tris;
                                        mesh.normals = norms;
                                        mesh.uv = uvs;
                                        EditorUtility.SetDirty(mesh);
                                    }

                                    filter.mesh = mesh;
                                }
                            }
                        }

                        if (importCollision) {
                            for (int i = 0; i < collMesh.Count; i++) {
                                GameObject layerColMeshObject = new GameObject();
                                layerColMeshObject.transform.parent = layerGameObject.transform;
                                layerColMeshObject.name = String.Format("Collision Mesh - {0}", i + 1);

                                MeshFilter colFilter = layerColMeshObject.AddComponent<MeshFilter>();
                                MeshCollider collider = layerColMeshObject.AddComponent<MeshCollider>();

                                String colMeshFileName = String.Format("{0}/{1}-{2}-{3}-collision.asset", meshDirectory, layerGameObject.name, lCount, i + 1);
                                Mesh colMesh = (Mesh)AssetDatabase.LoadAssetAtPath(colMeshFileName, typeof(Mesh));

                                if (colMesh == null) {
                                    colMesh = new Mesh() {
                                        vertices = collMesh[i].colverts.ToArray(),
                                        triangles = collMesh[i].tris.ToArray(),
                                        normals = collMesh[i].norms.ToArray(),
                                        uv = collMesh[i].uvs.ToArray(),
                                    };
                                    AssetDatabase.CreateAsset(colMesh, colMeshFileName);
                                }
                                else {
                                    colMesh.Clear(false);
                                    colMesh.vertices = collMesh[i].colverts.ToArray();
                                    colMesh.triangles = collMesh[i].tris.ToArray();
                                    colMesh.normals = collMesh[i].norms.ToArray();
                                    colMesh.uv = collMesh[i].uvs.ToArray();
                                    EditorUtility.SetDirty(colMesh);
                                }

                                colFilter.mesh = colMesh;
                                collider.sharedMesh = colMesh;
                            }
                        }
                    }

                    layerGameObject.SetActive(layerVisible);
                    AssetDatabase.SaveAssets();
                    lCount++;
                    sortOrder++;
                }
                else if (lElem.Name.LocalName.Equals("objectgroup")) {

                    bool importlayer = false;

                    if (settings.ObjectLayerSettings.Length >= olCount)
                        importlayer = settings.ObjectLayerSettings[olCount - 1].ImportLayer;

                    if (!importlayer) {
                        olCount++;
                        continue;
                    }

                    GameObject layerGameObject = new GameObject();
                    layerGameObject.transform.parent = mapGameObject.transform;

                    layerGameObject.name = lElem.Attribute("name") == null ? "Unnamed" : lElem.Attribute("name").Value;
                    float layerOpacity = lElem.Attribute("opacity") == null ? 1.0f : Convert.ToSingle(lElem.Attribute("opacity").Value);
                    bool layerVisible = lElem.Attribute("visible") == null ? true : lElem.Attribute("visible").Equals("1");
                    string layerColor = lElem.Attribute("color") == null ? "#a0a0a4" : lElem.Attribute("color").Value; // #a0a0a4 is Tiled's default grey, but won't be in the file unless manually set

                    layerGameObject.SetActive(layerVisible);

                    UTiledLayerSettings layerSettings = layerGameObject.AddComponent<UTiledLayerSettings>();
                    layerSettings.opacity = layerOpacity;
                    layerSettings.sortingOrder = sortOrder;

                    if (lElem.Element("properties") != null) {
                        UTiledProperties props = layerGameObject.AddComponent<UTiledProperties>();
                        foreach (var pElem in lElem.Element("properties").Elements("property"))
                            props.Add(pElem.Attribute("name").Value, pElem.Attribute("value").Value);
                    }

                    foreach (var oElem in lElem.Elements("object")) {
                        string oName = oElem.Attribute("name") == null ? "Unnamed" : oElem.Attribute("name").Value;
                        string oType = oElem.Attribute("type") == null ? null : oElem.Attribute("type").Value;
                        float? oX = oElem.Attribute("x") == null ? null : (float?)Convert.ToSingle(oElem.Attribute("x").Value);
                        float? oY = oElem.Attribute("y") == null ? null : (float?)Convert.ToSingle(oElem.Attribute("y").Value);
                        float? oWidth = oElem.Attribute("width") == null ? null : (float?)Convert.ToSingle(oElem.Attribute("width").Value);
                        float? oHeight = oElem.Attribute("height") == null ? null : (float?)Convert.ToSingle(oElem.Attribute("height").Value);
                        uint? tileGID = oElem.Attribute("gid") == null ? null : (uint?)Convert.ToUInt32(oElem.Attribute("gid").Value);
                        bool oVisible = oElem.Attribute("visible") == null ? true : oElem.Attribute("visible").Equals("1");

                        GameObject obj = new GameObject();
                        obj.transform.parent = layerGameObject.transform;
                        obj.name = oName;
                        obj.SetActive(oVisible);

                        if (oElem.Element("properties") != null || oType != null) {
                            UTiledProperties props = obj.AddComponent<UTiledProperties>();

                            if (oElem.Element("properties") != null)
                                foreach (var pElem in oElem.Element("properties").Elements("property"))
                                    props.Add(pElem.Attribute("name").Value, pElem.Attribute("value").Value);

                            if (oType != null)
                                props.ObjectType = oType;
                        }

                        if (oElem.Element("ellipse") != null && oX.HasValue && oY.HasValue && oWidth.HasValue && oHeight.HasValue) {
                            obj.AddComponent<CircleCollider2D>();
                            obj.transform.localScale = new Vector3(oWidth.Value / settings.PixelsPerUnit, oHeight.Value / settings.PixelsPerUnit, 1);
                            obj.transform.localPosition = new Vector3((oX.Value - .5f * mapWidth * mapTileWidth + .5f * oWidth.Value) / settings.PixelsPerUnit,
                                                                      (-1 * (oY.Value - .5f * mapHeight * mapTileHeight + .5f * oHeight.Value)) / settings.PixelsPerUnit, 0);
                            if (oWidth.Value != oHeight.Value)
                                Debug.LogWarning("Unity does not support Ellispe, importing as Circle: " + obj.name);
                        }
                        else if (oElem.Element("polygon") != null && oX.HasValue && oY.HasValue) {
                            PolygonCollider2D collider = obj.AddComponent<PolygonCollider2D>();
                            obj.transform.localPosition = new Vector3((oX.Value - .5f * mapWidth * mapTileWidth) / settings.PixelsPerUnit,
                                                                      (-1 * (oY.Value - .5f * mapHeight * mapTileHeight)) / settings.PixelsPerUnit, 0);

                            List<Vector2> points = new List<Vector2>();
                            foreach (var point in oElem.Element("polygon").Attribute("points").Value.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)) {
                                String[] coord = point.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                                points.Add(new Vector2(Convert.ToSingle(coord[0]) / settings.PixelsPerUnit, (-1f * Convert.ToSingle(coord[1])) / settings.PixelsPerUnit));
                            }
                            collider.points = points.ToArray();
                        }
                        else if (oElem.Element("polyline") != null && oX.HasValue && oY.HasValue) {
                            EdgeCollider2D collider = obj.AddComponent<EdgeCollider2D>();
                            obj.transform.localPosition = new Vector3((oX.Value - .5f * mapWidth * mapTileWidth) / settings.PixelsPerUnit,
                                                                      (-1 * (oY.Value - .5f * mapHeight * mapTileHeight)) / settings.PixelsPerUnit, 0);

                            List<Vector2> points = new List<Vector2>();
                            foreach (var point in oElem.Element("polyline").Attribute("points").Value.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)) {
                                String[] coord = point.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                                points.Add(new Vector2(Convert.ToSingle(coord[0]) / settings.PixelsPerUnit, (-1 * Convert.ToSingle(coord[1])) / settings.PixelsPerUnit));
                            }
                            collider.points = points.ToArray();
                        }
                        else if (tileGID.HasValue) {
                            SourceData sd = gid2sprite[tileGID.Value];
                            Sprite source = (Sprite)sprites.First(s => s.name.Equals(sd.spriteName));
                            obj.transform.localPosition = new Vector3((oX.Value - .5f * mapWidth * mapTileWidth + .5f * source.rect.width) / settings.PixelsPerUnit,
                                                                      (-1 * (oY.Value - .5f * mapHeight * mapTileHeight - .5f * source.rect.height)) / settings.PixelsPerUnit, 0);

                            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();

                            sr.sprite = source;
                            sr.sortingOrder = sortOrder;
                            sr.color = new Color(1, 1, 1, layerSettings.opacity);

                            BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
                            collider.size = new Vector2(source.rect.width / settings.PixelsPerUnit, source.rect.height / settings.PixelsPerUnit);
                        }
                        else if (!tileGID.HasValue && oX.HasValue && oY.HasValue && oWidth.HasValue && oHeight.HasValue) {
                            obj.AddComponent<BoxCollider2D>();
                            obj.transform.localScale = new Vector3(oWidth.Value / settings.PixelsPerUnit, oHeight.Value / settings.PixelsPerUnit, 1);
                            obj.transform.localPosition = new Vector3((oX.Value - .5f * mapWidth * mapTileWidth + .5f * oWidth.Value) / settings.PixelsPerUnit,
                                                                      (-1 * (oY.Value - .5f * mapHeight * mapTileHeight + .5f * oHeight.Value)) / settings.PixelsPerUnit, 0);
                        }
                    }

                    olCount++;
                    sortOrder++;
                }
            }

            String mapPrefapFile = mapDirectory + "/" + String.Format("{0}.prefab", mapName);
            GameObject mapPrefab = PrefabUtility.CreatePrefab(mapPrefapFile, mapGameObject);
            EditorUtility.SetDirty(mapPrefab);
            GameObject.DestroyImmediate(mapGameObject);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        private static int FindVertIndex(Vector3 v, List<Vector3> list) {
            for (int i = 0; i < list.Count; i++) {
                if (list[i].x == v.x && list[i].y == v.y)
                    return i;
            }
            return -1;
        }
    }
}
