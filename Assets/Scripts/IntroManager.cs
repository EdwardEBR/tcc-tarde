using UnityEngine;
using System.Collections;

public class IntroManager : MonoBehaviour
{
    [Header("Referências")]
    public GameObject camIntroObj;      // Arraste a CamIntro aqui
    public GameObject playerObj;        // Arraste o Player aqui
    public GameObject telaPretaObj;     // Arraste o objeto da tela preta/painel aqui

    void Start()
    {
        // 1. Estado inicial: Intro e tela preta ligadas, player desligado
        if (camIntroObj != null) camIntroObj.SetActive(true);
        if (telaPretaObj != null) telaPretaObj.SetActive(true);
        if (playerObj != null) playerObj.SetActive(false);

        // 2. Inicia a contagem
        StartCoroutine(TrocarParaPlayer());
    }

    IEnumerator TrocarParaPlayer()
    {
        Animator anim = camIntroObj.GetComponent<Animator>();
        float tempoDeAnimacao = 2f; // Tempo de segurança caso não ache o animator

        if (anim != null)
        {
            yield return null; // Espera 1 frame para o Animator iniciar
            tempoDeAnimacao = anim.GetCurrentAnimatorStateInfo(0).length;
        }

        // Espera a animação da intro (e dos olhos) terminar por completo
        yield return new WaitForSeconds(tempoDeAnimacao);

        // 3. Ativa o Player e desativa a Intro e a Tela Preta de uma vez
        if (playerObj != null) playerObj.SetActive(true);
        if (camIntroObj != null) camIntroObj.SetActive(false);
        if (telaPretaObj != null) telaPretaObj.SetActive(false);
    }
}