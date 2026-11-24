using UnityEngine;

public class TrocarXicaraAoDesviarOlhar : MonoBehaviour
{
    [Header("Configuração das Xícaras")]
    public GameObject xicaraOriginal;     
    public GameObject xicaraNovoLocal;    

    [Header("Estado da Tarefa")]
    public bool tarefaCafeConcluida = false;

    [Header("Câmera do jogador")]
    public Camera camPrincipal;

    [Header("Tempo antes de vigiar")]
    [Min(0f)] public float atrasoAntesDeVigiar = 1f;

    [Header("Confirmação de saída da tela")]
    [Min(0f)] public float tempoForaDaTelaParaTrocar = 0.3f;

    private bool jaTrocou = false;
    private bool xicaraJaFoiVista;
    private bool vigilanciaIniciada;
    private float iniciarVigilanciaEm;
    private float tempoForaDaTela;
    private Renderer[] renderizadoresDaXicara;
    private bool liberadaParaTrocar;
    private bool podePegarNoSlot = true;

    void Start()
    {
        EncontrarCameraDoJogador();

        if (xicaraNovoLocal != null)
            xicaraNovoLocal.SetActive(false);
            
        if (xicaraOriginal == null)
            xicaraOriginal = this.gameObject;

        renderizadoresDaXicara = xicaraOriginal.GetComponentsInChildren<Renderer>(true);
    }

    void Update()
    {
        if (!liberadaParaTrocar || jaTrocou || !tarefaCafeConcluida) return;

        if (camPrincipal == null)
            EncontrarCameraDoJogador();

        if (camPrincipal == null) return;

        if (!vigilanciaIniciada)
        {
            if (Time.time < iniciarVigilanciaEm) return;

            // Começa a acompanhar a partir do estado atual, depois da animação estabilizar.
            xicaraJaFoiVista = EstaNaVisaoDaCamera();
            vigilanciaIniciada = true;
            return;
        }

        bool xicaraEstaNaVisao = EstaNaVisaoDaCamera();
        if (xicaraEstaNaVisao)
        {
            xicaraJaFoiVista = true;
            tempoForaDaTela = 0f;
        }
        else if (xicaraJaFoiVista)
        {
            tempoForaDaTela += Time.deltaTime;
            if (tempoForaDaTela >= tempoForaDaTelaParaTrocar)
                FazerTroca();
        }
    }

    bool EstaNaVisaoDaCamera()
    {
        if (renderizadoresDaXicara == null || renderizadoresDaXicara.Length == 0)
        {
            Vector3 ponto = camPrincipal.WorldToViewportPoint(xicaraOriginal.transform.position);
            return ponto.z > 0f && ponto.x >= 0f && ponto.x <= 1f
                && ponto.y >= 0f && ponto.y <= 1f;
        }

        foreach (Renderer renderizador in renderizadoresDaXicara)
        {
            if (renderizador != null && renderizador.enabled
                && renderizador.gameObject.activeInHierarchy
                && LimitesEstaoNaTela(renderizador.bounds))
            {
                return true;
            }
        }

        return false;
    }

    bool LimitesEstaoNaTela(Bounds limites)
    {
        Vector3 centro = limites.center;
        Vector3 extents = limites.extents;
        float minimoX = float.PositiveInfinity;
        float minimoY = float.PositiveInfinity;
        float maximoX = float.NegativeInfinity;
        float maximoY = float.NegativeInfinity;
        bool temPontoNaFrente = false;

        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            Vector3 canto = centro + Vector3.Scale(extents, new Vector3(x, y, z));
            Vector3 ponto = camPrincipal.WorldToViewportPoint(canto);
            if (ponto.z <= camPrincipal.nearClipPlane) continue;

            temPontoNaFrente = true;
            minimoX = Mathf.Min(minimoX, ponto.x);
            maximoX = Mathf.Max(maximoX, ponto.x);
            minimoY = Mathf.Min(minimoY, ponto.y);
            maximoY = Mathf.Max(maximoY, ponto.y);
        }

        return temPontoNaFrente
            && maximoX >= 0f && minimoX <= 1f
            && maximoY >= 0f && minimoY <= 1f;
    }

    void EncontrarCameraDoJogador()
    {
        // Preserve a câmera arrastada no Inspector; procure automaticamente só se estiver vazia.
        if (camPrincipal != null) return;

        // A cena tem mais de uma câmera com a tag MainCamera; use a que controla o jogador.
        PlayerFPS jogador = Object.FindAnyObjectByType<PlayerFPS>();
        if (jogador != null && jogador.cameraPlayer != null)
            camPrincipal = jogador.cameraPlayer.GetComponent<Camera>();

        if (camPrincipal == null)
            camPrincipal = Camera.main;
    }

    void FazerTroca()
    {
        jaTrocou = true;

        // Depois da troca, o PortaItem fica vazio; a xícara deve ser retirada do novo local.
        if (xicaraOriginal != null)
        {
            PortaItemSlot[] slots = FindObjectsByType<PortaItemSlot>(FindObjectsInactive.Include);
            foreach (PortaItemSlot slot in slots)
            {
                if (slot != null && slot.objetoAoOcupar == xicaraOriginal)
                {
                    slot.MarcarItemMovidoParaOutroLocal();
                }
            }
        }

        OcultarOriginalSemEsconderSlot();
        if (xicaraNovoLocal != null) xicaraNovoLocal.SetActive(true);

        ItemSeguravel itemNoNovoLocal = xicaraNovoLocal != null
            ? xicaraNovoLocal.GetComponent<ItemSeguravel>()
            : null;
        if (itemNoNovoLocal != null)
            itemNoNovoLocal.podeSerPego = true;

        Debug.Log("SUCESSO ABSOLUTO: A xícara trocou de lugar ao desviar o olhar!");
    }

    public void ConcluirTarefaCafe()
    {
        if (!liberadaParaTrocar || jaTrocou || tarefaCafeConcluida) return;

        if (camPrincipal == null)
            EncontrarCameraDoJogador();

        if (camPrincipal == null)
        {
            Debug.LogWarning("Não foi possível iniciar a troca da xícara: nenhuma câmera com a tag MainCamera foi encontrada.");
            return;
        }

        tarefaCafeConcluida = true;
        // A xícara deve poder sumir ainda durante a animação da cafeteira.
        iniciarVigilanciaEm = Time.time + (liberadaParaTrocar ? 0f : atrasoAntesDeVigiar);
        vigilanciaIniciada = false;
        xicaraJaFoiVista = false;

        Debug.Log("Preparo do café iniciado. A xícara será vigiada após " + atrasoAntesDeVigiar + " segundos.");
    }

    // Chamado quando a cafeteira começa ou quando a xícara volta ao slot.
    // Impede que a troca por perda de visão esconda a xícara/representação do slot.
    public void CancelarTrocaERecolocarNoLocalOriginal()
    {
        jaTrocou = true;
        tarefaCafeConcluida = false;
        vigilanciaIniciada = false;
        tempoForaDaTela = 0f;

        if (xicaraOriginal == null) xicaraOriginal = gameObject;
        if (xicaraNovoLocal != null && xicaraNovoLocal != xicaraOriginal)
        {
            ItemSeguravel itemNoNovoLocal = xicaraNovoLocal.GetComponent<ItemSeguravel>();
            bool estaNaMao = itemNoNovoLocal != null && itemNoNovoLocal.estaSendoSegurado;
            if (!estaNaMao)
                xicaraNovoLocal.SetActive(false);
            if (itemNoNovoLocal != null)
                itemNoNovoLocal.podeSerPego = podePegarNoSlot && !itemNoNovoLocal.cafeConsumido;
        }
        if (xicaraOriginal != null)
        {
            xicaraOriginal.SetActive(true);
            MostrarOriginalSemAlterarSlot();
        }

        enabled = false;
    }

    // Libera a vigilância ao fim da animação, mantendo o resultado que já ocorreu.
    public void EncerrarVigilanciaAposAnimacao()
    {
        liberadaParaTrocar = false;
        podePegarNoSlot = true;
        jaTrocou = true;
        tarefaCafeConcluida = false;
        vigilanciaIniciada = false;
        ItemSeguravel item = xicaraNovoLocal != null
            ? xicaraNovoLocal.GetComponent<ItemSeguravel>()
            : null;
        if (item != null) item.podeSerPego = true;
        enabled = false;
    }

    public void PrepararTrocaDuranteAnimacao()
    {
        // A troca por desvio do olhar acontece durante o preparo. Na origem, porém,
        // a xícara fica bloqueada até terminar a animação.
        liberadaParaTrocar = true;
        podePegarNoSlot = false;
        jaTrocou = false;
        tarefaCafeConcluida = false;
        vigilanciaIniciada = false;
        xicaraJaFoiVista = false;
        tempoForaDaTela = 0f;
        ItemSeguravel itemOriginal = xicaraOriginal != null
            ? xicaraOriginal.GetComponent<ItemSeguravel>()
            : null;
        if (itemOriginal != null) itemOriginal.podeSerPego = false;
        ItemSeguravel itemAlternativo = xicaraNovoLocal != null
            ? xicaraNovoLocal.GetComponent<ItemSeguravel>()
            : null;
        if (itemAlternativo != null) itemAlternativo.podeSerPego = false;
        enabled = true;
    }

    public void LiberarTrocaQuandoCafePronto()
    {
        EncerrarVigilanciaAposAnimacao();
    }

    private void OcultarOriginalSemEsconderSlot()
    {
        if (xicaraOriginal == null) return;

        PortaItemSlot[] slots = xicaraOriginal.GetComponentsInChildren<PortaItemSlot>(true);
        if (slots.Length == 0)
        {
            xicaraOriginal.SetActive(false);
            return;
        }

        foreach (Renderer renderer in xicaraOriginal.GetComponentsInChildren<Renderer>(true))
        {
            if (!EstaDentroDeSlot(renderer.transform, slots)) renderer.enabled = false;
        }

        foreach (Collider collider in xicaraOriginal.GetComponentsInChildren<Collider>(true))
        {
            if (!EstaDentroDeSlot(collider.transform, slots)) collider.enabled = false;
        }
    }

    private void MostrarOriginalSemAlterarSlot()
    {
        PortaItemSlot[] slots = xicaraOriginal.GetComponentsInChildren<PortaItemSlot>(true);
        foreach (Renderer renderer in xicaraOriginal.GetComponentsInChildren<Renderer>(true))
        {
            if (!EstaDentroDeSlot(renderer.transform, slots)) renderer.enabled = true;
        }
    }

    private static bool EstaDentroDeSlot(Transform objeto, PortaItemSlot[] slots)
    {
        foreach (PortaItemSlot slot in slots)
        {
            if (slot != null && (objeto == slot.transform || objeto.IsChildOf(slot.transform)))
                return true;
        }

        return false;
    }
}
