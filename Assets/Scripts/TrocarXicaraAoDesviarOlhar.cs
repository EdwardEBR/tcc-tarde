using UnityEngine;

public class TrocarXicaraAoDesviarOlhar : MonoBehaviour
{
    [Header("Configuração das Xícaras")]
    public GameObject xicaraOriginal;     // Arraste a própria xícara da mesa aqui
    public GameObject xicaraNovoLocal;    // Arraste a xícara que está no outro canto (começa desativada)

    [Header("Estado da Tarefa")]
    public bool tarefaCafeConcluida = false; // Fica a true quando o TaskManager avisa

    private Camera camPrincipal;
    private bool jaTrocou = false;
    private bool estavaNaTelaNoFrameAnterior = false;

    void Start()
    {
        camPrincipal = Camera.main;

        // Garante que a xícara do novo local começa desativada no início do jogo
        if (xicaraNovoLocal != null)
            xicaraNovoLocal.SetActive(false);
    }

    void Update()
    {
        if (jaTrocou || camPrincipal == null || !tarefaCafeConcluida) return;

        // Verifica se a xícara original está visível na tela da câmara
        Vector3 viewportPos = camPrincipal.WorldToViewportPoint(transform.position);

        bool estaNaTela = viewportPos.z > 0 && 
                          viewportPos.x >= 0f && viewportPos.x <= 1f && 
                          viewportPos.y >= 0f && viewportPos.y <= 1f;

        // Estava a ser vista e AGORA deixou de ser (o jogador desviou o olhar)
        if (estavaNaTelaNoFrameAnterior && !estaNaTela)
        {
            FazerTroca();
        }

        estavaNaTelaNoFrameAnterior = estaNaTela;
    }

    void FazerTroca()
    {
        jaTrocou = true;

        // Desativa a xícara da mesa e ativa a do novo local
        if (xicaraOriginal != null) xicaraOriginal.SetActive(false);
        if (xicaraNovoLocal != null) xicaraNovoLocal.SetActive(true);

        Debug.Log("Truque de mágica: a xícara trocou de lugar!");
    }

    // Chamado pelo TaskManager quando a TarefaCafe for concluída
    public void ConcluirTarefaCafe()
    {
        tarefaCafeConcluida = true;
    }
}