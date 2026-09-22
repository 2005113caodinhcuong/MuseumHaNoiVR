using UnityEngine;

public class AlignBottomFrame : MonoBehaviour
{
    public GameObject glassCase;
    public GameObject bottomFrame;

    [ContextMenu("CAN KHUNG DUOI")]
    public void AlignFrame()
    {
        if (glassCase == null || bottomFrame == null)
        {
            Debug.LogError("Chua chon GlassCase va Khung duoi!");
            return;
        }

        Bounds caseBounds = GetBounds(glassCase);
        Bounds frameBounds = GetBounds(bottomFrame);

        Vector3 p = bottomFrame.transform.position;

        // Căn chính giữa theo X
        p.x += caseBounds.center.x - frameBounds.center.x;

        // Căn chính giữa theo Z
        p.z += caseBounds.center.z - frameBounds.center.z;

        // Đưa đáy khung trùng với đáy tủ
        p.y += caseBounds.min.y - frameBounds.min.y;

        bottomFrame.transform.position = p;

        Debug.Log("DA CAN KHUNG DUOI CHINH XAC!");
    }

    private Bounds GetBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            return new Bounds(obj.transform.position, Vector3.zero);
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }
}