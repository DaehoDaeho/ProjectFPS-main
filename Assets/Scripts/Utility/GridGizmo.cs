using UnityEngine;


public class GridGizmo : MonoBehaviour
{
    public int smallStep = 1; // 1m;
    public int largeStep = 5; // 5m;
    public int halfExtent = 25; // ¹Ý°æ(m);


    private void OnDrawGizmos()
    {
        Camera cam = Camera.main;
        Vector3 center = Vector3.zero;
        if (cam != null)
        {
            center = cam.transform.position;
        }


        int minX = Mathf.FloorToInt(center.x) - halfExtent;
        int maxX = Mathf.CeilToInt(center.x) + halfExtent;
        int minZ = Mathf.FloorToInt(center.z) - halfExtent;
        int maxZ = Mathf.CeilToInt(center.z) + halfExtent;


        Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
        for (int x = minX; x <= maxX; x += smallStep)
        {
            Gizmos.DrawLine(new Vector3(x, 0f, minZ), new Vector3(x, 0f, maxZ));
        }
        for (int z = minZ; z <= maxZ; z += smallStep)
        {
            Gizmos.DrawLine(new Vector3(minX, 0f, z), new Vector3(maxX, 0f, z));
        }


        Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
        for (int x = minX; x <= maxX; x += largeStep)
        {
            Gizmos.DrawLine(new Vector3(x, 0f, minZ), new Vector3(x, 0f, maxZ));
        }
        for (int z = minZ; z <= maxZ; z += largeStep)
        {
            Gizmos.DrawLine(new Vector3(minX, 0f, z), new Vector3(maxX, 0f, z));
        }


        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawLine(new Vector3(-this.halfExtent, 0f, 0f), new Vector3(this.halfExtent, 0f, 0f));
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.8f);
        Gizmos.DrawLine(new Vector3(0f, 0f, 0f), new Vector3(0f, this.halfExtent * 0.2f, 0f));
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.8f);
        Gizmos.DrawLine(new Vector3(0f, 0f, -this.halfExtent), new Vector3(0f, 0f, this.halfExtent));
    }
}