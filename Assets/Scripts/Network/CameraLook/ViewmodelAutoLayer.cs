using UnityEngine;

/// <summary>
/// ViewmodelRoot 이하 모든 자식의 레이어를 'Viewmodel'로 강제한다.
/// 계층 편집 중 섞이는 문제를 방지.
/// </summary>
public class ViewmodelAutoLayer : MonoBehaviour
{
    public string viewmodelLayerName = "Viewmodel";

    private void Awake()
    {
        int layer = LayerMask.NameToLayer(viewmodelLayerName);
        if (layer < 0)
        {
            return;
        }
        ApplyLayerRecursive(transform, layer);
    }

    private void ApplyLayerRecursive(Transform t, int layer)
    {
        if (t == null)
        {
            return;
        }

        t.gameObject.layer = layer;

        for (int i = 0; i < t.childCount; i = i + 1)
        {
            ApplyLayerRecursive(t.GetChild(i), layer);
        }
    }
}
