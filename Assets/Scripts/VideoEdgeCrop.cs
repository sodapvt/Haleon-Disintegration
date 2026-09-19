using UnityEngine;
using UnityEngine.UI;

// Trim the mesh and its UVs together so the remaining picture does not move or stretch.
// This removes decoder padding while retaining alignment with the static slide objects.
[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
public class VideoEdgeCrop : BaseMeshEffect
{
    private Vector2 cropPixels;

    public Vector2 CropPixels
    {
        get { return cropPixels; }
        set
        {
            cropPixels = value;
            if (graphic != null)
            {
                graphic.SetVerticesDirty();
            }
        }
    }

    public override void ModifyMesh(VertexHelper vertices)
    {
        RawImage image = graphic as RawImage;
        if (!IsActive() || image == null || image.texture == null)
        {
            return;
        }

        Rect rect = image.GetPixelAdjustedRect();
        Rect uv = image.uvRect;
        float sourceWidth = image.texture.width * Mathf.Abs(uv.width);
        float sourceHeight = image.texture.height * Mathf.Abs(uv.height);
        if (rect.width <= 0f || rect.height <= 0f || sourceWidth <= 1f || sourceHeight <= 1f)
        {
            return;
        }

        float right = Mathf.Clamp(cropPixels.x, 0f, sourceWidth - 1f) / sourceWidth;
        float bottom = Mathf.Clamp(cropPixels.y, 0f, sourceHeight - 1f) / sourceHeight;
        UIVertex vertex = new UIVertex();
        for (int i = 0; i < vertices.currentVertCount; i++)
        {
            vertices.PopulateUIVertex(ref vertex, i);
            float x = (vertex.position.x - rect.xMin) / rect.width;
            float y = (vertex.position.y - rect.yMin) / rect.height;
            vertex.position.x -= rect.width * right * x;
            vertex.position.y += rect.height * bottom * (1f - y);
            vertex.uv0.x -= uv.width * right * x;
            vertex.uv0.y += uv.height * bottom * (1f - y);
            vertices.SetUIVertex(vertex, i);
        }
    }
}
