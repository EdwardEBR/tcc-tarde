using UnityEngine;
using System.Collections;

public class IntroManager : MonoBehaviour
{
    [Header("Referências")]
    public GameObject camIntroObj;      // Arraste a CamIntro aqui
    public GameObject playerObj;        // Arraste o Player aqui
    public GameObject telaPretaObj;     // Arraste o objeto da tela preta/painel aqui
    public GameObject telaPretaFechandoObj;

    void Start()
    {
        if (telaPretaFechandoObj == null)
            telaPretaFechandoObj = GameObject.Find("TelaPretaFechar");
        if (telaPretaFechandoObj != null)
            telaPretaFechandoObj.SetActive(false);

        // 1. Estado inicial: Intro e tela preta ligadas, player desligado
        if (camIntroObj != null) camIntroObj.SetActive(true);
        if (telaPretaObj != null) telaPretaObj.SetActive(true);
        if (playerObj != null) playerObj.SetActive(false);

        // 2. Inicia a contagem
        StartCoroutine(TrocarParaPlayer());
    }

    IEnumerator TrocarParaPlayer()
    {
        Animator anim = camIntroObj != null ? camIntroObj.GetComponent<Animator>() : null;
        if (anim != null)
        {
            // A transição só deve acontecer quando a animação tiver realmente
            // chegado ao fim; esperar apenas o seu comprimento pode perder o
            // frame final e revelar o primeiro frame caso o clipe esteja em loop.
            while (anim != null && anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
                yield return null;
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        // Troca as câmeras na mesma atualização, sem renderizar as duas juntas.
        if (camIntroObj != null) camIntroObj.SetActive(false);
        if (playerObj != null) playerObj.SetActive(true);
        if (telaPretaObj != null) telaPretaObj.SetActive(false);
    }
}
