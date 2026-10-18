using UnityEngine;

public class ItemSeguravel : MonoBehaviour
{
    [Header("Configurações do Item")]
    public string nomeDoItem = "Objeto";
    
    private Rigidbody rb;
    private Collider col;
    private Outline outlineScript; // Referência para o Outline do objeto
    [HideInInspector] public bool estaSendoSegurado = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        
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

        // Desativa o collider temporariamente
        if (col != null)
        {
            col.enabled = false;
        }

        // Usa o suporte da câmera sem herdar escalas da hierarquia original do item.
        transform.SetParent(pontoDaMao, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = escalaNoMundo;

        Debug.Log("Item apanhado: " + nomeDoItem);
    }

    // Chamado quando o jogador coloca o objeto num slot exato
    public void SoltarItem(Transform novoPonto = null)
    {
        estaSendoSegurado = false;

        if (novoPonto != null)
        {
            transform.SetParent(novoPonto);
            transform.localPosition = Vector3.zero;
            transform.localRotation = novoPonto.rotation;
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

        if (col != null)
        {
            col.enabled = true;
        }

        Debug.Log("Item posicionado: " + nomeDoItem);
    }
}
