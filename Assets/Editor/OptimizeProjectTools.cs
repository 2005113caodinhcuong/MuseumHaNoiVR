#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class OptimizeProjectTools : EditorWindow
{
    [MenuItem("Tools/Optimize Project (Museum VR)")]
    public static void ShowWindow()
    {
        GetWindow<OptimizeProjectTools>("Museum VR Optimizer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Museum VR - Công cụ Tối ưu hóa Nhanh", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "1. Tối ưu nén tất cả file FBX (Mesh Compression High, tắt Read/Write).\n" +
            "2. Tối ưu tất cả Texture (Bật Streaming Mipmaps, chuẩn hóa Normal/PBR, nén Max Size).\n" +
            "3. Tự động gán Layer 'Interactable' cho các hiện vật gốm trong Scene.",
            MessageType.Info
        );

        EditorGUILayout.Space();

        if (GUILayout.Button("1. Reimport & Nén toàn bộ FBX", GUILayout.Height(35)))
        {
            OptimizeAllFBX();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("2. Reimport & Chuẩn hóa toàn bộ Textures", GUILayout.Height(35)))
        {
            OptimizeAllTextures();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("3. Tự động gán Layer 'Interactable' cho đồ gốm trong Scene", GUILayout.Height(35)))
        {
            AssignInteractableLayers();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("4. Tạo BoxCollider chống đi xuyên cho tất cả Tủ Kính", GUILayout.Height(35)))
        {
            CreateCabinetCollidersInEditor();
        }
    }

    public static void CreateCabinetCollidersInEditor()
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int added = 0;

        foreach (GameObject go in allObjects)
        {
            string lowerName = go.name.ToLower();
            if (lowerName.StartsWith("display_ceramic") || lowerName.Contains("tukinh"))
            {
                Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;

                Bounds worldBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    if (renderers[i] is ParticleSystemRenderer) continue;
                    worldBounds.Encapsulate(renderers[i].bounds);
                }

                BoxCollider boxCol = go.GetComponent<BoxCollider>();
                if (boxCol == null)
                {
                    boxCol = go.AddComponent<BoxCollider>();
                }

                Vector3 localCenter = go.transform.InverseTransformPoint(worldBounds.center);
                Vector3 lossyScale = go.transform.lossyScale;

                Vector3 localSize = new Vector3(
                    lossyScale.x != 0 ? Mathf.Abs(worldBounds.size.x / lossyScale.x) : worldBounds.size.x,
                    lossyScale.y != 0 ? Mathf.Abs(worldBounds.size.y / lossyScale.y) : worldBounds.size.y,
                    lossyScale.z != 0 ? Mathf.Abs(worldBounds.size.z / lossyScale.z) : worldBounds.size.z
                );

                boxCol.center = localCenter;
                boxCol.size = localSize;
                boxCol.isTrigger = false;
                EditorUtility.SetDirty(go);
                added++;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        EditorUtility.DisplayDialog("Thành công", $"Đã gắn BoxCollider vững chắc cho {added} tủ kính trưng bày!", "OK");
    }

    public static void OptimizeAllFBX()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets" });
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
            {
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer != null)
                {
                    bool modified = false;

                    if (importer.meshCompression != ModelImporterMeshCompression.High)
                    {
                        importer.meshCompression = ModelImporterMeshCompression.High;
                        modified = true;
                    }

                    if (importer.isReadable)
                    {
                        importer.isReadable = false;
                        modified = true;
                    }

                    if (!importer.weldVertices)
                    {
                        importer.weldVertices = true;
                        modified = true;
                    }

                    if (modified)
                    {
                        importer.SaveAndReimport();
                        count++;
                    }
                }
            }
        }

        EditorUtility.DisplayDialog("Thành công", $"Đã nén tối ưu {count} file FBX!", "OK");
    }

    public static void OptimizeAllTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" });
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool modified = false;
                string lower = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

                if (!importer.streamingMipmaps)
                {
                    importer.streamingMipmaps = true;
                    modified = true;
                }

                if (lower.Contains("normal"))
                {
                    if (importer.textureType != TextureImporterType.NormalMap)
                    {
                        importer.textureType = TextureImporterType.NormalMap;
                        importer.sRGBTexture = false;
                        modified = true;
                    }
                }
                else if (lower.Contains("roughness") || lower.Contains("metallic") || lower.Contains("specular"))
                {
                    if (importer.sRGBTexture)
                    {
                        importer.sRGBTexture = false;
                        modified = true;
                    }
                    if (importer.maxTextureSize > 1024)
                    {
                        importer.maxTextureSize = 1024;
                        modified = true;
                    }
                }
                else
                {
                    if (importer.maxTextureSize > 2048)
                    {
                        importer.maxTextureSize = 2048;
                        modified = true;
                    }
                }

                if (modified)
                {
                    importer.SaveAndReimport();
                    count++;
                }
            }
        }

        EditorUtility.DisplayDialog("Thành công", $"Đã tối ưu hóa {count} file Texture!", "OK");
    }

    public static void AssignInteractableLayers()
    {
        int layerIndex = LayerMask.NameToLayer("Interactable");
        if (layerIndex == -1)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Layer 'Interactable' trong TagManager!", "OK");
            return;
        }

        PotteryItem[] items = Object.FindObjectsByType<PotteryItem>(FindObjectsSortMode.None);
        int changed = 0;

        foreach (var item in items)
        {
            SetLayerRecursively(item.gameObject, layerIndex);
            EditorUtility.SetDirty(item.gameObject);
            changed++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        EditorUtility.DisplayDialog("Thành công", $"Đã gán Layer 'Interactable' cho {changed} hiện vật gốm sứ!", "OK");
    }

    private static void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}
#endif
