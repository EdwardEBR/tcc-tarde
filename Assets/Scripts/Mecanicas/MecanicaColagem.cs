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

    [Header("Iluminação da árvore")]
    public Light luzLuminaria;

    private bool colagemMontada;
    private Transform quarto;
    private Outline outlineArvore;

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

        if (luzLuminaria != null)
            luzLuminaria.enabled = false;
    }

    private void Update()
    {
        if (luzLuminaria == null || !ArvoreOuFotosEstaoVisiveis()) return;

        // O GameObject da luminária pode ter sido desativado no Inspector.
        // Ligar só o componente Light não funciona enquanto o objeto está inativo.
        if (!luzLuminaria.gameObject.activeSelf)
            luzLuminaria.gameObject.SetActive(true);

        if (!luzLuminaria.enabled)
            luzLuminaria.enabled = true;
    }

    private bool ArvoreOuFotosEstaoVisiveis()
    {
        if (arvoreGenealogica != null && arvoreGenealogica.activeInHierarchy)
            return true;

        if (fotosDaColagem == null) return false;

        foreach (GameObject foto in fotosDaColagem)
        {
            if (foto != null && foto.activeInHierarchy)
                return true;
        }

        return false;
    }

    private void Start()
    {
        // O QuickOutline precisa terminar o Awake antes de ser desligado.
        if (outlineArvore != null) outlineArvore.enabled = false;
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

            // Esconde a representação que ficou no PortaFoto quando a foto foi colocada.
            if (porta.objetoAoOcupar != null)
                porta.objetoAoOcupar.SetActive(false);
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
            item.slotDeOrigem = null;
            item.podeSerPego = true;
            // O raycast do Player usa a layer Interativel. Aplicá-la à hierarquia
            // inteira evita que um collider em um filho fique fora da máscara.
            DefinirCamadaRecursiva(objeto.transform, CamadaInterativa);
            foreach (Collider colisor in objeto.GetComponentsInChildren<Collider>(true))
            {
                colisor.enabled = true;
            }
            objeto.SetActive(false);
        }
    }

    private void PrepararColagem()
    {
        if (arvoreGenealogica != null)
        {
            arvoreGenealogica.layer = CamadaInterativa;
            outlineArvore = arvoreGenealogica.GetComponent<Outline>();
        }

        if (fotosDaColagem == null) return;
        foreach (GameObject foto in fotosDaColagem)
        {
            if (foto != null) foto.SetActive(false);
        }
    }

    public void SetHighlightArvore(bool ativo)
    {
        if (outlineArvore != null)
            outlineArvore.enabled = ativo && TodasAsPortasPreenchidas() && !colagemMontada;
    }

    public bool EhPortaDaColagem(PortaItemSlot porta)
    {
        if (porta == null || portasFotos == null) return false;
        foreach (PortaItemSlot portaFoto in portasFotos)
        {
            if (portaFoto == porta) return true;
        }
        return false;
    }

    public bool EhFotoDaSala(ItemSeguravel item)
    {
        if (item == null || fotosDaSala == null) return false;
        foreach (GameObject foto in fotosDaSala)
        {
            if (foto == item.gameObject) return true;
        }
        return false;
    }

    public bool PodeColocarFoto(ItemSeguravel item, PortaItemSlot porta)
    {
        if (item == null || porta == null || porta.slotOcupado || !EhPortaDaColagem(porta)
            || !EhFotoDaSala(item)) return false;

        string nomeAceito = porta.nomeDoItemAceito;
        return string.Equals(nomeAceito, item.nomeDoItem, System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(nomeAceito, item.gameObject.name, System.StringComparison.OrdinalIgnoreCase);
    }

    public ItemSeguravel EncontrarFotoNaMira(Ray raio, float distanciaMaxima, out float distancia)
    {
        distancia = float.MaxValue;
        ItemSeguravel melhorFoto = null;
        if (fotosDaSala == null) return null;

        foreach (GameObject foto in fotosDaSala)
        {
            if (foto == null || !foto.activeInHierarchy) continue;
            ItemSeguravel item = foto.GetComponent<ItemSeguravel>();
            if (item == null || !item.podeSerPego) continue;

            foreach (Renderer rendererFoto in foto.GetComponentsInChildren<Renderer>())
            {
                if (rendererFoto.bounds.IntersectRay(raio, out float distanciaBounds)
                    && distanciaBounds <= distanciaMaxima && distanciaBounds < distancia)
                {
                    distancia = distanciaBounds;
                    melhorFoto = item;
                }
            }
        }

        return melhorFoto;
    }

    private void PrepararPortasFotos()
    {
        if (portasFotos == null) return;

        foreach (PortaItemSlot porta in portasFotos)
        {
            if (porta == null) continue;
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
