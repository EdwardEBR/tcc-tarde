using UnityEngine;

public class TrocarXicaraAoDesviarOlhar : MonoBehaviour
{
    [Header("Configuração das Xícaras")]
    public GameObject xicaraOriginal;     
    public GameObject xicaraNovoLocal;    

    [Header("Estado da Tarefa")]
    public bool tarefaCafeConcluida = false; 

    private Camera camPrincipal;
    private bool jaTrocou = false;
    private bool estavaNaTelaNoFrameAnterior = true;
    private float tempoDeEsperaAteVigiar = 0f;

    void Start()
    {
        camPrincipal = Camera.main;

        if (xicaraNovoLocal != null)
            xicaraNovoLocal.SetActive(false);
            
        if (xicaraOriginal == null)
            xicaraOriginal = this.gameObject;
    }

    void Update()
    {
        // Atalho de teste opcional com a tecla 'C'
        if (Input.GetKeyDown(KeyCode.C))
        {
            ConcluirTarefaCafe();
        }

        if (jaTrocou || camPrincipal == null || !tarefaCafeConcluida) return;

        // Pequeno atraso em segundos para evitar que suma no mesmo frame do clique
        if (tempoDeEsperaAteVigiar > 0)
        {
            tempoDeEsperaAteVigiar -= Time.deltaTime;
            return; // Enquanto o tempo não passar, não faz contas de visão
        }

        // Calcula o ângulo em relação à câmara
        Vector3 direcaoParaXicara = transform.position - camPrincipal.transform.position;
        float angulo = Vector3.Angle(camPrincipal.transform.forward, direcaoParaXicara);
        
        // Se o ângulo for menor que 85°, a xícara está dentro do campo de visão
        bool estaNaTela = angulo < 85f; 

        // Se estava visível no frame anterior e agora deixou de estar (desviou o olhar)
        if (estavaNaTelaNoFrameAnterior && !estaNaTela)
        {
            FazerTroca();
        }

        estavaNaTelaNoFrameAnterior = estaNaTela;
    }

    void FazerTroca()
    {
        jaTrocou = true;

        if (xicaraOriginal != null) xicaraOriginal.SetActive(false);
        if (xicaraNovoLocal != null) xicaraNovoLocal.SetActive(true);

        Debug.Log("SUCESSO ABSOLUTO: A xícara trocou de lugar ao desviar o olhar!");
    }

    public void ConcluirTarefaCafe()
    {
        tarefaCafeConcluida = true;
        tempoDeEsperaAteVigiar = 0.5f; // Dá 0.5 segundos de margem antes de começar a vigiar
        estavaNaTelaNoFrameAnterior = true; 
        
        Debug.Log("O café está pronto! A xícara vai começar a vigiar o olhar em instantes...");
    }
}