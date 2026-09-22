using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class PotteryVase : MonoBehaviour
{
    [Header("Kích thước bình")]
    public float height = 3.0f;
    public float maxRadius = 1.2f;

    [Header("Độ chi tiết")]
    [Range(16, 128)]
    public int radialSegments = 64;

    [Range(8, 64)]
    public int heightSegments = 32;

    [Header("Đáy bình")]
    public float bottomRadius = 0.65f;

    [Header("Miệng bình")]
    public float mouthRadius = 0.48f;

    void Start()
    {
        CreateVase();
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            CreateVase();
        }
    }

    void CreateVase()
    {
        Mesh mesh = new Mesh();

        mesh.name = "CeramicVase";

        Vector3[] vertices =
            new Vector3[(heightSegments + 1) * radialSegments];

        Vector3[] normals =
            new Vector3[vertices.Length];

        Vector2[] uv =
            new Vector2[vertices.Length];

        int[] triangles =
            new int[heightSegments * radialSegments * 6];

        // -----------------------------
        // Tạo các điểm theo chiều cao
        // -----------------------------

        for (int y = 0; y <= heightSegments; y++)
        {
            float t = (float)y / heightSegments;

            float currentHeight = t * height;

            float radius = GetRadius(t);

            for (int x = 0; x < radialSegments; x++)
            {
                float angle =
                    (float)x / radialSegments *
                    Mathf.PI * 2f;

                float px =
                    Mathf.Cos(angle) * radius;

                float pz =
                    Mathf.Sin(angle) * radius;

                int index =
                    y * radialSegments + x;

                vertices[index] =
                    new Vector3(
                        px,
                        currentHeight,
                        pz
                    );

                uv[index] =
                    new Vector2(
                        (float)x / radialSegments,
                        t
                    );
            }
        }

        // -----------------------------
        // Tạo mặt tam giác
        // -----------------------------

        int triangleIndex = 0;

        for (int y = 0; y < heightSegments; y++)
        {
            for (int x = 0; x < radialSegments; x++)
            {
                int current =
                    y * radialSegments + x;

                int next =
                    y * radialSegments +
                    (x + 1) % radialSegments;

                int upper =
                    (y + 1) * radialSegments + x;

                int upperNext =
                    (y + 1) * radialSegments +
                    (x + 1) % radialSegments;

                triangles[triangleIndex++] = current;
                triangles[triangleIndex++] = upper;
                triangles[triangleIndex++] = next;

                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = upper;
                triangles[triangleIndex++] = upperNext;
            }
        }

        // -----------------------------
        // Normals
        // -----------------------------

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;

        // -----------------------------
        // Collider
        // -----------------------------

        MeshCollider collider =
            GetComponent<MeshCollider>();

        if (collider == null)
        {
            collider =
                gameObject.AddComponent<MeshCollider>();
        }

        collider.sharedMesh = null;
        collider.sharedMesh = mesh;
    }

    float GetRadius(float t)
    {
        /*
         * t = 0 → đáy
         * t = 1 → miệng
         */

        // Đáy
        if (t < 0.08f)
        {
            return Mathf.Lerp(
                bottomRadius,
                0.75f,
                t / 0.08f
            );
        }

        // Phần dưới thân
        if (t < 0.25f)
        {
            float p =
                (t - 0.08f) / 0.17f;

            return Mathf.Lerp(
                0.75f,
                maxRadius,
                p
            );
        }

        // Bụng bình
        if (t < 0.55f)
        {
            return maxRadius;
        }

        // Vai bình
        if (t < 0.72f)
        {
            float p =
                (t - 0.55f) / 0.17f;

            return Mathf.Lerp(
                maxRadius,
                0.72f,
                p
            );
        }

        // Cổ bình
        if (t < 0.92f)
        {
            float p =
                (t - 0.72f) / 0.20f;

            return Mathf.Lerp(
                0.72f,
                mouthRadius,
                p
            );
        }

        // Miệng bình
        return mouthRadius;
    }
}