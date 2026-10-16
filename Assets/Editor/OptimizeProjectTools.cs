#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class OptimizeProjectTools : EditorWindow
{
    [MenuItem("Tools/Optimize Project (Museum VR)")]
    public static void ShowWindow()
    {
        GetWindow<OptimizeProjectTools>("Museum VR Optimizer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Museum VR - Bộ Công cụ Tối ưu & Tương tác Hiện vật", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "PHẦN A: TỐI ƯU HÓA ĐỒ HỌA & KIẾN TRÚC TĨNH\n" +
            "1. Tối ưu nén tất cả file FBX (Mesh Compression High, tắt Read/Write).\n" +
            "2. Tối ưu tất cả Texture (Bật Streaming Mipmaps, chuẩn hóa PBR).\n" +
            "3. Tối ưu hóa kiến trúc tĩnh (Bật Static Batching & Occlusion Culling cho toàn bộ tường, sàn, trần, tủ kính).\n\n" +
            "PHẦN B: HỆ THỐNG HIỆN VẬT & TƯƠNG TÁC (TỦ 1 - 4)\n" +
            "4. Thiết lập vùng đệm 20cm chống đi xuyên cho các tủ kính.\n" +
            "5. Tự động ánh xạ và gán PotteryItem + Collider cho toàn bộ 59 bình gốm theo Tủ & Số.\n" +
            "6. Đồng bộ lại thông tin hiện vật từ file Resources/PotteryData.json vào Scene.\n" +
            "7. Xuất thông tin hiện vật từ Scene ra file JSON.",
            MessageType.Info
        );

        EditorGUILayout.Space();

        if (GUILayout.Button("1. Reimport & Nén toàn bộ FBX", GUILayout.Height(30)))
        {
            OptimizeAllFBX();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("2. Reimport & Chuẩn hóa toàn bộ Textures", GUILayout.Height(30)))
        {
            OptimizeAllTextures();
        }

        EditorGUILayout.Space();

        GUI.backgroundColor = new Color(0.35f, 0.75f, 1f, 1f);
        if (GUILayout.Button("3. Tối ưu hóa Kiến trúc tĩnh (Static Batching & Occlusion)", GUILayout.Height(32)))
        {
            OptimizeStaticArchitecture();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();

        if (GUILayout.Button("4. Tạo BoxCollider chống đi xuyên cho tất cả Tủ Kính", GUILayout.Height(30)))
        {
            CreateCabinetCollidersInEditor();
        }

        EditorGUILayout.Space(10);
        GUILayout.Label("QUẢN LÝ 59 HIỆN VẬT GỐM SỨ (TỦ 1 -> 4)", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.5f, 1f);
        if (GUILayout.Button("5. Tự động ánh xạ & Gán PotteryItem + Collider cho 59 hiện vật", GUILayout.Height(35)))
        {
            MapAndSetupAllPottery();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(5);

        if (GUILayout.Button("6. Đồng bộ lại dữ liệu từ PotteryData.json vào Scene", GUILayout.Height(30)))
        {
            SyncAllPotteryFromDatabase();
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("7. Xuất dữ liệu hiện vật trong Scene ra PotteryData.json", GUILayout.Height(30)))
        {
            ExportAllPotteryToDatabase();
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
                CabinetBarrier barrier = go.GetComponent<CabinetBarrier>();
                if (barrier == null)
                {
                    barrier = go.AddComponent<CabinetBarrier>();
                }
                barrier.bufferDistance = 0.2f;
                barrier.SetupCollider();
                EditorUtility.SetDirty(go);
                added++;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        EditorUtility.DisplayDialog("Thành công", $"Đã thiết lập vùng đệm 20cm cho {added} tủ kính trưng bày!", "OK");
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

    [MenuItem("Tools/Museum VR/Gán PotteryItem & Collider cho 59 hiện vật (Tủ 1 - 4)")]
    public static void MapAndSetupAllPottery()
    {
        int layerIndex = LayerMask.NameToLayer("Interactable");
        if (layerIndex == -1)
        {
            EditorUtility.DisplayDialog("Lỗi", "Chưa có Layer 'Interactable' trong TagManager! Hãy tạo Layer 6 tên 'Interactable'.", "OK");
            return;
        }

        ArtifactDatabase db = PotteryItem.GetDatabase();
        if (db == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy file Assets/Resources/PotteryData.json!", "OK");
            return;
        }

        // Tạo bảng tra cứu linh hoạt
        Dictionary<string, ArtifactData> dictById = new Dictionary<string, ArtifactData>(System.StringComparer.OrdinalIgnoreCase);
        Dictionary<string, ArtifactData> dictByModelKey = new Dictionary<string, ArtifactData>(System.StringComparer.OrdinalIgnoreCase);

        foreach (var art in db.artifacts)
        {
            dictById[art.id] = art;
            string key = $"{art.cabinetKey}_{art.number}";
            dictByModelKey[key] = art;
        }

        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int setupCount = 0;

        foreach (GameObject go in allObjects)
        {
            string objName = go.name.ToLower();

            // Bỏ qua các đối tượng môi trường, phòng, đèn, player, barrier
            if (objName.Contains("camera") || objName.Contains("light") || objName.Contains("spot") ||
                objName.Contains("led") || objName.Contains("canvas") || objName.Contains("event") ||
                objName.Contains("player") || objName.Contains("glass") || objName.Contains("frame") ||
                objName.Contains("wall") || objName.Contains("san") || objName.Contains("tru") ||
                objName.Contains("tran") || objName.Contains("cot") || objName.Contains("hanhlang") ||
                objName.Contains("dennho") || objName.Contains("bedon") || objName.Contains("barrier"))
            {
                continue;
            }

            if (objName.Contains("pedestal") || objName.Contains("ceramic_front") || 
                objName.Contains("ceramic_center") || objName.Contains("ceramic_back") || 
                objName.Contains("ceramic_left") || objName.Contains("display_ceramic"))
            {
                continue;
            }

            string cabinetKey = InteractionSystem.DetectCabinetKey(go.transform);
            if (string.IsNullOrEmpty(cabinetKey)) continue;

            Renderer[] rends = go.GetComponentsInChildren<Renderer>();
            if (rends == null || rends.Length == 0) continue;

            char firstChar = go.name.Length > 0 ? go.name[0] : ' ';
            if (!char.IsDigit(firstChar) && !go.name.StartsWith("Tu", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // 1. Gán Layer Interactable cho toàn bộ các phần tử con
            SetLayerRecursively(go, layerIndex);

            // 2. Gán BoxCollider với kích thước tính chuẩn theo World Bounds
            Bounds worldBounds = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
            {
                if (rends[i] is ParticleSystemRenderer) continue;
                worldBounds.Encapsulate(rends[i].bounds);
            }

            BoxCollider box = go.GetComponent<BoxCollider>();
            if (box == null)
            {
                box = go.AddComponent<BoxCollider>();
            }

            Vector3 localCenter = go.transform.InverseTransformPoint(worldBounds.center);
            Vector3 lossy = go.transform.lossyScale;
            float lx = lossy.x != 0 ? Mathf.Abs(lossy.x) : 1f;
            float ly = lossy.y != 0 ? Mathf.Abs(lossy.y) : 1f;
            float lz = lossy.z != 0 ? Mathf.Abs(lossy.z) : 1f;

            Vector3 localSize = new Vector3(
                worldBounds.size.x / lx,
                worldBounds.size.y / ly,
                worldBounds.size.z / lz
            );

            box.center = localCenter;
            box.size = localSize * 1.2f;
            box.isTrigger = false;

            // Đảm bảo MeshCollider ở mesh con
            foreach (Renderer rend in rends)
            {
                if (rend is ParticleSystemRenderer) continue;
                MeshFilter mf = rend.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    MeshCollider mc = rend.GetComponent<MeshCollider>();
                    if (mc == null)
                    {
                        mc = rend.gameObject.AddComponent<MeshCollider>();
                        mc.convex = false;
                    }
                }
            }

            // 3. Gán PotteryItem và nạp dữ liệu từ Database
            PotteryItem item = go.GetComponent<PotteryItem>();
            if (item == null)
            {
                item = go.AddComponent<PotteryItem>();
            }

            item.cabinetName = cabinetKey;
            item.itemNumber = go.name;
            item.LoadFromDatabase(cabinetKey, go.name);

            EditorUtility.SetDirty(go);
            setupCount++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        EditorUtility.DisplayDialog("Thành công", $"Đã hoàn tất ánh xạ và gán PotteryItem + Collider cho {setupCount} hiện vật gốm sứ!", "OK");
    }

    [MenuItem("Tools/Museum VR/Đồng bộ lại dữ liệu từ PotteryData.json vào Scene")]
    public static void SyncAllPotteryFromDatabase()
    {
        ArtifactDatabase db = PotteryItem.GetDatabase();
        if (db == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy file Assets/Resources/PotteryData.json!", "OK");
            return;
        }

        PotteryItem[] items = Object.FindObjectsByType<PotteryItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int synced = 0;

        foreach (var item in items)
        {
            if (item.LoadFromDatabase(item.artifactId))
            {
                EditorUtility.SetDirty(item.gameObject);
                synced++;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        EditorUtility.DisplayDialog("Thành công", $"Đã cập nhật lại thông tin cho {synced} hiện vật từ file JSON!", "OK");
    }

    [MenuItem("Tools/Museum VR/Xuất dữ liệu hiện vật từ Scene ra PotteryData.json")]
    public static void ExportAllPotteryToDatabase()
    {
        PotteryItem[] items = Object.FindObjectsByType<PotteryItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (items.Length == 0)
        {
            EditorUtility.DisplayDialog("Thông báo", "Chưa có PotteryItem nào trong Scene để xuất!", "OK");
            return;
        }

        ArtifactDatabase db = new ArtifactDatabase();
        db.version = "1.0";
        db.description = "Dữ liệu hiện vật gốm sứ xuất trực tiếp từ Scene.";
        db.total_artifacts = items.Length;
        db.artifacts = new ArtifactData[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            PotteryItem item = items[i];
            db.artifacts[i] = new ArtifactData
            {
                id = item.artifactId,
                cabinet = item.cabinetName,
                number = item.itemNumber,
                artifactName = item.artifactName,
                material = item.material,
                period = item.period,
                origin = item.origin,
                notableFeatures = item.notableFeatures,
                description = item.description
            };
        }

        string json = JsonUtility.ToJson(db, true);
        string path = Path.Combine(Application.dataPath, "Resources/PotteryData.json");
        File.WriteAllText(path, json, System.Text.Encoding.UTF8);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Thành công", $"Đã xuất {items.Length} hiện vật ra file Assets/Resources/PotteryData.json!", "OK");
    }

    [MenuItem("Tools/Museum VR/Tối ưu hóa kiến trúc tĩnh (Static Batching & Occlusion)")]
    public static void OptimizeStaticArchitecture()
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int staticCount = 0;
        int dynamicCount = 0;

        StaticEditorFlags targetFlags = StaticEditorFlags.ContributeGI | 
                                        StaticEditorFlags.OccluderStatic | 
                                        StaticEditorFlags.BatchingStatic | 
                                        StaticEditorFlags.NavigationStatic | 
                                        StaticEditorFlags.OccludeeStatic | 
                                        StaticEditorFlags.OffMeshLinkGeneration | 
                                        StaticEditorFlags.ReflectionProbeStatic;

        foreach (GameObject go in allObjects)
        {
            string lowerName = go.name.ToLower();

            // 1. Kiểm tra đối tượng động bắt buộc: Player, Camera, Lights động, Volume, EventSystem, Canvas
            if (lowerName.Contains("player") || 
                go.GetComponent<CharacterController>() != null ||
                go.GetComponentInParent<CharacterController>() != null ||
                go.GetComponent<Camera>() != null ||
                go.GetComponentInParent<Camera>() != null ||
                go.GetComponent<Canvas>() != null ||
                go.GetComponentInParent<Canvas>() != null ||
                go.GetComponent<UnityEngine.Rendering.Volume>() != null ||
                go.GetComponent<UnityEngine.EventSystems.EventSystem>() != null ||
                lowerName.Contains("eventsystem") ||
                lowerName.Contains("directional light"))
            {
                GameObjectUtility.SetStaticEditorFlags(go, 0);
                EditorUtility.SetDirty(go);
                dynamicCount++;
                continue;
            }

            // 2. Kiểm tra hiện vật gốm sứ (PotteryItem): BẮT BUỘC ĐỂ DYNAMIC ĐỂ TƯƠNG TÁC 3D VÀ KHÔNG BỊ COMBINE MESH
            if (go.GetComponent<PotteryItem>() != null || 
                go.GetComponentInParent<PotteryItem>() != null)
            {
                GameObjectUtility.SetStaticEditorFlags(go, 0);
                EditorUtility.SetDirty(go);
                dynamicCount++;
                continue;
            }

            // Kiểm tra tên hiện vật (bắt đầu bằng số hoặc "Tu") nếu chưa gắn PotteryItem
            char firstChar = go.name.Length > 0 ? go.name[0] : ' ';
            if ((char.IsDigit(firstChar) || go.name.StartsWith("Tu", System.StringComparison.OrdinalIgnoreCase)) &&
                !lowerName.Contains("tukinh") && !lowerName.Contains("tuong"))
            {
                GameObjectUtility.SetStaticEditorFlags(go, 0);
                EditorUtility.SetDirty(go);
                dynamicCount++;
                continue;
            }

            // 3. Toàn bộ kiến trúc tĩnh: Tường, Sàn, Trần, Cột, Bệ đôn, Khung kính, Đèn cố định, v.v.
            GameObjectUtility.SetStaticEditorFlags(go, targetFlags);
            EditorUtility.SetDirty(go);
            staticCount++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        Debug.Log($"[OptimizeStaticArchitecture] Đã tối ưu hóa kiến trúc tĩnh: {staticCount} đối tượng được gán Static (Batching & Occlusion), {dynamicCount} đối tượng giữ Dynamic (Player, Camera, 59 hiện vật gốm sứ).");

        EditorUtility.DisplayDialog(
            "Tối ưu hóa kiến trúc tĩnh hoàn tất",
            $"Đã gán Static (Static Batching + Occlusion Culling + GI) cho {staticCount} thành phần kiến trúc tĩnh trong bảo tàng!\n\n" +
            $"Đã bảo lưu {dynamicCount} đối tượng động (Nhân vật, Camera, Toàn bộ hiện vật gốm sứ 3D để tương tác mượt mà).",
            "OK"
        );
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
