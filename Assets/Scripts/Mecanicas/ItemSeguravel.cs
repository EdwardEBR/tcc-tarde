using UnityEngine;

public class ItemSeguravel : MonoBehaviour
{
    [Header("Configurações do Item")]
    public string nomeDoItem = "Objeto";
    public PortaItemSlot slotDeOrigem; // Arraste o PortaItemSlot correspondente aqui no Inspector
    
    private Rigidbody rb;
    private Collider[] colliders;
    private bool[] collidersEstavamAtivos;
    private Outline outlineScript; // Referência para o Outline do objeto
    [HideInInspector] public bool estaSendoSegurado = false;
    [HideInInspector] public bool temCafeNaXicara = false;
    [HideInInspector] public bool podeSerPego = true;
    [HideInInspector] public bool cafeConsumido = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>(true);
        
        // Procura o componente Outline no próprio objeto
        outlineScript = GetComponent<Outline>();
        if (outlineScript != null)
        {
            outlineScript.enabled = false; // Começa sem brilho
        }
    }

    // Método para ligar/desligar o brilho quando o jogador olha para o item
    public void SetHighlight(bool state)
    {
        if (estaSendoSegurado) return; // Se já estiver na mão, não mostra o outline
        if (outlineScript != null)
        {
            outlineScript.enabled = state;
        }
    }

    // Chamado quando o jogador apanha o objeto
    public void PegarItem(Transform pontoDaMao)
    {
        // Guarda a escala visual atual antes de tirar o item da hierarquia original.
        Vector3 escalaNoMundo = transform.lossyScale;
        estaSendoSegurado = true;

        // Desliga o outline ao apanhar
        if (outlineScript != null)
        {
            outlineScript.enabled = false;
        }

        // Desliga a física para o objeto não cair ou empurrar o jogador
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        // Desativa todos os colliders do item enquanto ele está na mão. Colliders
        // em filhos (por exemplo, no líquido da xícara) também poderiam empurrar o jogador.
        if (colliders == null || colliders.Length == 0)
        {
            colliders = GetComponentsInChildren<Collider>(true);
        }
        collidersEstavamAtivos = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            collidersEstavamAtivos[i] = colliders[i] != null && colliders[i].enabled;
            if (colliders[i] != null) colliders[i].enabled = false;
        }

        // Avisa o slot de origem que o item foi retirado (ativa o indicador do quadrado azul)
        if (slotDeOrigem != null)
        {
            slotDeOrigem.DesocuparSlot();
        }

        // Usa o suporte da câmera sem herdar escalas da hierarquia original do item.
        transform.SetParent(pontoDaMao, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = EscalaLocalParaManterEscalaMundial(escalaNoMundo, pontoDaMao);

        Debug.Log("Item apanhado: " + nomeDoItem);
    }

    // Chamado quando o jogador coloca o objeto num slot exato
    public void SoltarItem(Transform novoPonto = null)
    {
        estaSendoSegurado = false;

        if (novoPonto != null)
        {
            Vector3 escalaNoMundo = transform.lossyScale;
            transform.SetParent(novoPonto, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = EscalaLocalParaManterEscalaMundial(escalaNoMundo, novoPonto);
        }
        else
        {
            transform.SetParent(null);
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
        }

        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = collidersEstavamAtivos != null
                        && i < collidersEstavamAtivos.Length
                        && collidersEstavamAtivos[i];
                }
            }
        }

        Debug.Log("Item posicionado: " + nomeDoItem);
    }

    public bool BeberCafe()
    {
        if (!temCafeNaXicara) return false;

        DesativarVisuaisDeCafe(transform);
        temCafeNaXicara = false;
        cafeConsumido = true;
        podeSerPego = false;
        nomeDoItem = slotDeOrigem != null ? slotDeOrigem.nomeDoItemAceito : gameObject.name;

        // A representação pode manter um visual separado enquanto a xícara está no slot.
        if (slotDeOrigem != null && slotDeOrigem.objetoAoOcupar != null)
        {
            Transform representacao = slotDeOrigem.objetoAoOcupar.transform;
            DesativarVisuaisDeCafe(representacao);

            ItemSeguravel itemRepresentado = representacao.GetComponent<ItemSeguravel>();
            if (itemRepresentado != null && itemRepresentado != this)
            {
                itemRepresentado.temCafeNaXicara = false;
                itemRepresentado.cafeConsumido = true;
                itemRepresentado.podeSerPego = false;
                itemRepresentado.nomeDoItem = slotDeOrigem.nomeDoItemAceito;
            }
        }

        Debug.Log("O café foi bebido; a xícara está vazia.");
        return true;
    }

    private static void DesativarVisuaisDeCafe(Transform raiz)
    {
        if (raiz == null) return;

        foreach (Transform filho in raiz.GetComponentsInChildren<Transform>(true))
        {
            if (filho == raiz) continue;

            if (filho.name.Equals("CafeNaXicara", System.StringComparison.OrdinalIgnoreCase)
                || filho.name.Equals("CafenaXicara", System.StringComparison.OrdinalIgnoreCase)
                || filho.name.Equals("LiquidoCafe", System.StringComparison.OrdinalIgnoreCase))
            {
                filho.gameObject.SetActive(false);
            }
        }
    }

    private static Vector3 EscalaLocalParaManterEscalaMundial(Vector3 escalaMundial, Transform pai)
    {
        Vector3 escalaPai = pai != null ? pai.lossyScale : Vector3.one;
        return new Vector3(
            DividirEscala(escalaMundial.x, escalaPai.x),
            DividirEscala(escalaMundial.y, escalaPai.y),
            DividirEscala(escalaMundial.z, escalaPai.z));
    }

    private static float DividirEscala(float escala, float escalaPai)
    {
        return Mathf.Abs(escalaPai) > 0.0001f ? escala / escalaPai : escala;
    }
}
