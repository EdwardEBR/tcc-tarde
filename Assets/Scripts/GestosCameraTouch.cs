using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Encaminha o arrasto de um único dedo para o olhar do PlayerFPS.</summary>
public sealed class GestosCameraTouch : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private PlayerFPS jogador;
    private int ponteiroAtivo = int.MinValue;

    public void Configurar(PlayerFPS alvo)
    {
        jogador = alvo;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (jogador == null || ponteiroAtivo != int.MinValue || eventData.button != PointerEventData.InputButton.Left)
            return;

        ponteiroAtivo = eventData.pointerId;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (jogador == null || eventData.pointerId != ponteiroAtivo)
            return;

        jogador.AdicionarDeltaOlharToque(eventData.delta);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != ponteiroAtivo)
            return;

        ponteiroAtivo = int.MinValue;
    }

    private void OnDisable()
    {
        ponteiroAtivo = int.MinValue;
        if (jogador != null)
            jogador.CancelarOlharToque();
    }
}
