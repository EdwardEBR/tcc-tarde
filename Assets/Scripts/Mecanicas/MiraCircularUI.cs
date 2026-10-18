using UnityEngine;
using UnityEngine.UI;

/// <summary>Desenha uma mira circular como geometria UI, sem depender de textura ou sprite.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MiraCircularUI : MaskableGraphic
{
    [SerializeField, Range(12, 128)] private int segmentos = 64;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        float raio = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
        if (raio <= 0f || segmentos < 3)
            return;

        UIVertex centro = UIVertex.simpleVert;
        centro.color = color;
        centro.position = Vector3.zero;
        vh.AddVert(centro);

        for (int i = 0; i < segmentos; i++)
        {
            float angulo = (Mathf.PI * 2f * i) / segmentos;
            UIVertex vertice = UIVertex.simpleVert;
            vertice.color = color;
            vertice.position = new Vector3(Mathf.Cos(angulo) * raio, Mathf.Sin(angulo) * raio);
            vh.AddVert(vertice);
        }

        for (int i = 0; i < segmentos; i++)
            vh.AddTriangle(0, i + 1, (i + 1) % segmentos + 1);
    }
}
