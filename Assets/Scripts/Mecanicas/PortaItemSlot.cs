using UnityEngine;

public class PortaItemSlot : MonoBehaviour
{
    [Header("Configuração do Slot")]
    public string nomeDoItemAceito = "Xícara"; // Nome do item que se encaixa aqui
    public MeshRenderer visualQuadradoAzul;    // Arraste o MeshRenderer do quadrado azul aqui
    public GameObject objetoAoOcupar;           // Ex.: CanecaOri, usada como representação no cenário
    
    [Header("Estado Inicial")]
    public bool slotOcupado = false; // Marque apenas se o slot já começar ocupado na cena
    public string nomeDoVisualTransferido = "CafeNaXicara"; // Visual de café que acompanha a xícara entre as duas representações

    private Outline outlineScript;
    private ItemSeguravel itemGuardado;
    public ItemSeguravel ItemGuardado => itemGuardado;

    public bool TemItemVisivelNoSlot
    {
        get
        {
            if (!slotOcupado) return false;

            if (objetoAoOcupar != null && (itemGuardado == null || objetoAoOcupar != itemGuardado.gameObject))
            {
                return objetoAoOcupar.activeInHierarchy && TemRendererVisivelNaRepresentacao();
            }

            return itemGuardado != null && itemGuardado.gameObject.activeInHierarchy;
        }
    }

    public bool PodeRetirarItem
    {
        get
        {
            return TemItemVisivelNoSlot && (itemGuardado == null || itemGuardado.podeSerPego);
        }
    }

    public static PortaItemSlot EncontrarSlotDaRepresentacao(Transform objeto)
    {
        if (objeto == null) return null;

        PortaItemSlot[] slots = FindObjectsByType<PortaItemSlot>(FindObjectsInactive.Include);
        foreach (PortaItemSlot slot in slots)
        {
            if (slot == null || slot.objetoAoOcupar == null) continue;

            Transform representacao = slot.objetoAoOcupar.transform;
            if (objeto == representacao || objeto.IsChildOf(representacao))
            {
                return slot;
            }
        }

        return null;
    }

    void Start()
    {
        outlineScript = GetComponent<Outline>();
        if (outlineScript != null)
        {
            outlineScript.enabled = false;
        }

        // Recupera o item inicial se a cena já começa com o slot ocupado.
        if (slotOcupado && itemGuardado == null && objetoAoOcupar != null)
        {
            // A xícara real pode ser irmã da representação (CanecaTeleporte e CanecaOri
            // aparecem como objetos separados na cena). Use primeiro o vínculo com este slot.
            ItemSeguravel[] itensDaCena = FindObjectsByType<ItemSeguravel>(FindObjectsInactive.Include);
            foreach (ItemSeguravel candidato in itensDaCena)
            {
                if (candidato != null && candidato.gameObject != objetoAoOcupar
                    && candidato.slotDeOrigem == this)
                {
                    itemGuardado = candidato;
                    break;
                }
            }

            // Compatibilidade com cenas nas quais o item real está dentro da representação.
            ItemSeguravel[] itens = objetoAoOcupar.GetComponentsInChildren<ItemSeguravel>(true);
            foreach (ItemSeguravel candidato in itens)
            {
                if (itemGuardado == null && candidato.gameObject != objetoAoOcupar)
                {
                    itemGuardado = candidato;
                    break;
                }
            }

            // Compatibilidade com slots cuja referência aponta diretamente para o item real.
            if (itemGuardado == null)
            {
                itemGuardado = objetoAoOcupar.GetComponent<ItemSeguravel>();
            }
        }

        if (itemGuardado != null) itemGuardado.slotDeOrigem = this;

        if (ObjetoDeExibicaoPodeSerAlternado())
        {
            bool mostrarRepresentacao = slotOcupado;
            if (itemGuardado != null && objetoAoOcupar != itemGuardado.gameObject)
            {
                itemGuardado.gameObject.SetActive(false);
            }

            ConfigurarRepresentacaoInterativa(mostrarRepresentacao);
        }

        // O PortaItem é uma peça fixa da cena: seu renderer permanece visível ocupado ou vazio.
        if (visualQuadradoAzul != null)
            visualQuadradoAzul.enabled = true;
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
        itemGuardado = null;
        if (visualQuadradoAzul != null)
        {
            visualQuadradoAzul.enabled = true;
        }
    }

    // A xícara foi deslocada para outro ponto da cena pela mecânica de olhar.
    public void MarcarItemMovidoParaOutroLocal()
    {
        DesocuparSlot();
    }

    // Chamado quando o jogador clica no quadrado azul para devolver o item
    public bool OcuparSlot(ItemSeguravel item)
    {
        if (item == null)
        {
            return false;
        }

        // A representação pode conter o próprio PortaItem. Nunca a reparentar para dentro do slot.
        if (objetoAoOcupar != null
            && (item.gameObject == objetoAoOcupar || transform.IsChildOf(item.transform)))
        {
            Debug.LogError("Não é possível guardar a representação da xícara no próprio PortaItem. Use o ItemSeguravel da xícara real.", this);
            return false;
        }

        slotOcupado = true;
        itemGuardado = item;
        itemGuardado.slotDeOrigem = this;

        // Se o script de troca já escondeu o local original, restaura o ponto do slot.
        TrocarXicaraAoDesviarOlhar troca = EncontrarTrocaDestaXicara();
        if (troca != null)
        {
            troca.CancelarTrocaERecolocarNoLocalOriginal();
        }

        if (objetoAoOcupar != null)
        {
            objetoAoOcupar.SetActive(true);
        }

        if (itemGuardado != null)
        {
            itemGuardado.SoltarItem(transform);
        }

        if (ObjetoDeExibicaoPodeSerAlternado() && objetoAoOcupar != itemGuardado.gameObject)
        {
            TransferirVisual(itemGuardado.transform, objetoAoOcupar.transform);
            itemGuardado.gameObject.SetActive(false);
            ConfigurarRepresentacaoInterativa(true);
        }

        // O PortaItem e seus renderers ficam visíveis mesmo quando o slot está ocupado.
        if (outlineScript != null)
        {
            outlineScript.enabled = false;
        }

        return true;
    }

    // Retira a representação visível e devolve à mão o item real guardado no slot.
    public ItemSeguravel RetirarItem()
    {
        if (!slotOcupado || itemGuardado == null || !itemGuardado.podeSerPego)
        {
            return null;
        }

        ItemSeguravel item = itemGuardado;

        if (ObjetoDeExibicaoPodeSerAlternado() && objetoAoOcupar != item.gameObject)
        {
            TransferirVisual(objetoAoOcupar.transform, item.transform);
            ConfigurarRepresentacaoInterativa(false);
            item.gameObject.SetActive(true);
        }

        itemGuardado = null;
        DesocuparSlot();

        return item;
    }

    private TrocarXicaraAoDesviarOlhar EncontrarTrocaDestaXicara()
    {
        TrocarXicaraAoDesviarOlhar[] trocas = FindObjectsByType<TrocarXicaraAoDesviarOlhar>(FindObjectsInactive.Include);
        foreach (TrocarXicaraAoDesviarOlhar troca in trocas)
        {
            if (troca != null && objetoAoOcupar != null
                && (troca.xicaraOriginal == objetoAoOcupar || troca.gameObject == objetoAoOcupar))
            {
                return troca;
            }
        }

        return trocas.Length == 1 ? trocas[0] : null;
    }

    private void TransferirVisual(Transform origem, Transform destino)
    {
        if (string.IsNullOrWhiteSpace(nomeDoVisualTransferido)) return;

        Transform visual = origem.Find(nomeDoVisualTransferido);
        // Aceita o nome antigo e diferenças de maiúsculas/minúsculas em cenas já configuradas.
        if (visual == null)
        {
            foreach (Transform filho in origem)
            {
                if (string.Equals(filho.name, nomeDoVisualTransferido, System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(filho.name, "CafeNaXicara", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(filho.name, "CafenaXicara", System.StringComparison.OrdinalIgnoreCase))
                {
                    visual = filho;
                    break;
                }
            }
        }
        if (visual == null) return;

        Vector3 posicaoLocal = visual.localPosition;
        Quaternion rotacaoLocal = visual.localRotation;
        Vector3 escalaLocal = visual.localScale;

        visual.SetParent(destino, false);
        visual.localPosition = posicaoLocal;
        visual.localRotation = rotacaoLocal;
        visual.localScale = escalaLocal;

        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }
    }

    private void ConfigurarRepresentacaoInterativa(bool ativa)
    {
        if (objetoAoOcupar == null) return;

        bool contemSlot = transform.IsChildOf(objetoAoOcupar.transform);
        Transform itemReal = itemGuardado != null ? itemGuardado.transform : null;

        foreach (Transform objeto in objetoAoOcupar.GetComponentsInChildren<Transform>(true))
        {
            if (contemSlot && (objeto == transform || objeto.IsChildOf(transform))) continue;
            if (itemReal != null && (objeto == itemReal || objeto.IsChildOf(itemReal))) continue;
            objeto.gameObject.layer = gameObject.layer;
        }

        foreach (Renderer renderer in objetoAoOcupar.GetComponentsInChildren<Renderer>(true))
        {
            if (contemSlot && (renderer.transform == transform || renderer.transform.IsChildOf(transform))) continue;
            if (itemReal != null && (renderer.transform == itemReal || renderer.transform.IsChildOf(itemReal))) continue;
            renderer.enabled = ativa;
        }

        foreach (Collider collider in objetoAoOcupar.GetComponentsInChildren<Collider>(true))
        {
            if (contemSlot && (collider.transform == transform || collider.transform.IsChildOf(transform))) continue;
            if (itemReal != null && (collider.transform == itemReal || collider.transform.IsChildOf(itemReal))) continue;
            collider.isTrigger = true;
            collider.enabled = ativa;
        }
    }

    private bool TemRendererVisivelNaRepresentacao()
    {
        foreach (Renderer renderer in objetoAoOcupar.GetComponentsInChildren<Renderer>(true))
        {
            if ((!transform.IsChildOf(objetoAoOcupar.transform)
                    || (renderer.transform != transform && !renderer.transform.IsChildOf(transform)))
                && renderer.enabled && renderer.gameObject.activeInHierarchy)
            {
                return true;
            }
        }

        return false;
    }

    private bool ObjetoDeExibicaoPodeSerAlternado()
    {
        if (objetoAoOcupar == null)
        {
            return false;
        }

        // A referência não pode ser o próprio slot; um pai do slot é permitido e seus renderers são alternados separadamente.
        if (objetoAoOcupar.transform == transform)
        {
            Debug.LogError("O campo 'Objeto Ao Ocupar' do PortaItemSlot não pode apontar para o próprio slot.", this);
            return false;
        }

        return true;
    }
}
