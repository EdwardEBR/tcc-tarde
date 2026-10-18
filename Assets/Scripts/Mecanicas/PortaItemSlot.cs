using UnityEngine;

public class PortaItemSlot : MonoBehaviour
{
    [Header("Configuração do Slot")]
    public string nomeDoItemAceito = "Xícara"; // Nome do item que se encaixa aqui
    public MeshRenderer visualQuadradoAzul;    // Arraste o MeshRenderer do quadrado azul aqui
    public GameObject objetoAoOcupar;           // Ex.: CanecaOri, usada como representação no cenário
    
    [Header("Estado Inicial")]
    public bool slotOcupado = false; // Marque apenas se o slot já começar ocupado na cena

    private Outline outlineScript;
    private ItemSeguravel itemGuardado;

    void Start()
    {
        outlineScript = GetComponent<Outline>();
        if (outlineScript != null)
        {
            outlineScript.enabled = false;
        }

        // Quando o slot começa vazio, deixa oculta a representação que só aparece ao ocupá-lo.
        if (!slotOcupado && ObjetoDeExibicaoPodeSerAlternado())
        {
            objetoAoOcupar.SetActive(false);
        }
    }

    public void SetHighlight(bool state)
    {
        if (outlineScript != null)
        {
            outlineScript.enabled = state;
        }
    }

    // Chamado quando o jogador pega o item que estava aqui
    public void DesocuparSlot()
    {
        slotOcupado = false;
        if (visualQuadradoAzul != null)
        {
            visualQuadradoAzul.enabled = true; // Mostra o indicador azul
        }
    }

    // Chamado quando o jogador clica no quadrado azul para devolver o item
    public void OcuparSlot(ItemSeguravel item)
    {
        if (item == null)
        {
            return;
        }

        slotOcupado = true;
        itemGuardado = item;

        if (itemGuardado != null)
        {
            itemGuardado.SoltarItem(transform);
        }

        if (ObjetoDeExibicaoPodeSerAlternado() && objetoAoOcupar != itemGuardado.gameObject)
        {
            itemGuardado.gameObject.SetActive(false);
            objetoAoOcupar.SetActive(true);
        }

        // O PortaItem e seus renderers ficam visíveis mesmo quando o slot está ocupado.
        if (outlineScript != null)
        {
            outlineScript.enabled = false;
        }
    }

    // Retira a representação visível e devolve à mão o item real guardado no slot.
    public ItemSeguravel RetirarItem()
    {
        if (!slotOcupado || itemGuardado == null)
        {
            return null;
        }

        ItemSeguravel item = itemGuardado;
        itemGuardado = null;

        if (ObjetoDeExibicaoPodeSerAlternado() && objetoAoOcupar != item.gameObject)
        {
            objetoAoOcupar.SetActive(false);
            item.gameObject.SetActive(true);
        }

        DesocuparSlot();

        return item;
    }

    private bool ObjetoDeExibicaoPodeSerAlternado()
    {
        if (objetoAoOcupar == null)
        {
            return false;
        }

        // Nunca desative o próprio PortaItem nem um objeto pai que o contenha.
        if (objetoAoOcupar.transform == transform || transform.IsChildOf(objetoAoOcupar.transform))
        {
            Debug.LogError("O campo 'Objeto Ao Ocupar' do PortaItemSlot não pode apontar para o próprio slot ou para um objeto pai dele.", this);
            return false;
        }

        return true;
    }
}
