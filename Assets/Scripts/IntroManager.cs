using UnityEngine;
using System.Collections;

public class IntroManager : MonoBehaviour
{
    [Header("Referências")]
    public GameObject camIntroObj;      // Arraste o objeto CamIntro aqui
    public GameObject playerObj;        // Arraste o objeto Player aqui

    void Start()
    {
        // 1. Garante o estado inicial correto ao dar Play
        if (camIntroObj != null) camIntroObj.SetActive(true);
        if (playerObj != null) playerObj.SetActive(false);

        // 2. Inicia a contagem para trocar de câmera
        StartCoroutine(TrocarParaPlayer());
    }

    IEnumerator TrocarParaPlayer()
    {
        // Pega o Animator que está no CamIntro
        Animator anim = camIntroObj.GetComponent<Animator>();
        float tempoDeAnimacao = 3f; // Tempo padrão caso não ache o animator

        if (anim != null)
        {
            // Espera 1 frame para a Unity processar o Animator
            yield return null; 
            // Descobre a duração exata da animação que está a tocar na Intro
            tempoDeAnimacao = anim.GetCurrentAnimatorStateInfo(0).length;
        }

        // Espera o tempo exato da animação da intro terminar
        yield return new WaitForSeconds(tempoDeAnimacao);

        // 3. Ativa o Player e desativa a CamIntro
        if (playerObj != null) playerObj.SetActive(true);
        if (camIntroObj != null) camIntroObj.SetActive(false);
    }
}