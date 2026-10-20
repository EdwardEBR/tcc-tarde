using UnityEngine;

/// <summary>Controla a coleta das fotos e a montagem da colagem da árvore genealógica.</summary>
public class MecanicaColagem : MonoBehaviour
{
    [Header("Fotos encontradas na sala")]
    public GameObject[] fotosDaSala;

    [Header("Três espaços para guardar as fotos")]
    public PortaItemSlot[] portasFotos;

    [Header("Colagem montada no quarto")]
    public GameObject arvoreGenealogica;
    public GameObject[] fotosDaColagem;

    private bool colagemMontada;
    private Transform quarto;

    public GameObject ColagemVisual => quarto != null ? quarto.gameObject : null;

    private void Awake()
    {
        Transform sala = transform.Find("Sala");
        quarto = transform.Find("Quarto");

        if (fotosDaSala == null || fotosDaSala.Length == 0)
        {
            fotosDaSala = new GameObject[]
            {
                EncontrarFilho(sala, "Criança"),
                EncontrarFilho(sala, "Homem"),
                EncontrarFilho(sala, "Mulher")
            };
        }

        if (arvoreGenealogica == null)
            arvoreGenealogica = EncontrarFilho(quarto, "ArvoreGenealogica");

        if (fotosDaColagem == null || fotosDaColagem.Length == 0)
        {
            fotosDaColagem = new GameObject[]
            {
                EncontrarFilho(quarto, "Foto1"),
                EncontrarFilho(quarto, "Foto2"),
                EncontrarFilho(quarto, "Foto3")
            };
        }

        if (portasFotos == null || portasFotos.Length == 0)
            portasFotos = GetComponentsInChildren<PortaItemSlot>(true);

        PrepararFotosDaSala();
        PrepararColagem();
        PrepararPortasFotos();
    }

    public bool EhAlvoDaArvore(Transform alvo)
    {
        return arvoreGenealogica != null && alvo != null
            && (alvo == arvoreGenealogica.transform || alvo.IsChildOf(arvoreGenealogica.transform));
    }

    public bool TentarMontarColagem()
    {
        if (colagemMontada || !TodasAsPortasPreenchidas()) return false;

        foreach (PortaItemSlot porta in portasFotos)
        {
            ItemSeguravel foto = porta.ItemGuardado;
            if (foto != null)
            {
                foto.podeSerPego = false;
                foto.gameObject.SetActive(false);
            }
        }

        foreach (GameObject foto in fotosDaSala)
        {
            if (foto != null) foto.SetActive(false);
        }

        foreach (GameObject foto in fotosDaColagem)
        {
            if (foto != null) foto.SetActive(true);
        }

        colagemMontada = true;
        return true;
    }

    private bool TodasAsPortasPreenchidas()
    {
        if (portasFotos == null || portasFotos.Length != 3) return false;

        foreach (PortaItemSlot porta in portasFotos)
        {
            if (porta == null || !porta.slotOcupado || porta.ItemGuardado == null)
                return false;
        }

        return true;
    }

    private void PrepararFotosDaSala()
    {
        if (fotosDaSala == null) return;

        foreach (GameObject objeto in fotosDaSala)
        {
            if (objeto == null) continue;

            ItemSeguravel item = objeto.GetComponent<ItemSeguravel>();
            if (item == null) item = objeto.AddComponent<ItemSeguravel>();
            item.nomeDoItem = "FotoColagem";
            item.slotDeOrigem = null;
            item.podeSerPego = true;
            objeto.layer = CamadaInterativa;
            objeto.SetActive(false);
        }
    }

    private void PrepararColagem()
    {
        if (arvoreGenealogica != null)
            arvoreGenealogica.layer = CamadaInterativa;

        if (fotosDaColagem == null) return;
        foreach (GameObject foto in fotosDaColagem)
        {
            if (foto != null) foto.SetActive(false);
        }
    }

    private void PrepararPortasFotos()
    {
        if (portasFotos == null) return;

        foreach (PortaItemSlot porta in portasFotos)
        {
            if (porta == null) continue;
            porta.nomeDoItemAceito = "FotoColagem";
            DefinirCamadaRecursiva(porta.transform, CamadaInterativa);
        }
    }

    private static GameObject EncontrarFilho(Transform pai, string nome)
    {
        if (pai == null) return null;

        foreach (Transform filho in pai)
        {
            if (string.Equals(filho.name, nome, System.StringComparison.OrdinalIgnoreCase))
                return filho.gameObject;
        }

        return null;
    }

    private static int CamadaInterativa
    {
        get
        {
            int camada = LayerMask.NameToLayer("Interativel");
            return camada >= 0 ? camada : 0;
        }
    }

    private static void DefinirCamadaRecursiva(Transform raiz, int camada)
    {
        raiz.gameObject.layer = camada;
        foreach (Transform filho in raiz)
            DefinirCamadaRecursiva(filho, camada);
    }
}
