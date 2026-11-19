using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class IntroManager : MonoBehaviour
{
    [Header("Referências")]
    public GameObject camIntroObj;      // Arraste a CamIntro aqui
    public GameObject playerObj;        // Arraste o Player aqui
    public GameObject telaPretaObj;     // Arraste o objeto da tela preta/painel aqui
    public GameObject telaPretaFechandoObj;
    public GameObject telaPretaFecharRapidoObj;

    private Volume volumeVisaoEmbacada;
    private VolumeProfile perfilVisaoEmbacada;
    private DepthOfField profundidadeDeCampo;
    private Vignette vinhetaVisaoEmbacada;

    void Start()
    {
        if (string.Equals(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "Nivel4", System.StringComparison.OrdinalIgnoreCase))
            PrepararVisaoEmbacada();

        // Este efeito pertence somente à conclusão da tarefa de regar a planta.
        if (telaPretaFecharRapidoObj != null)
            telaPretaFecharRapidoObj.SetActive(false);

        if (telaPretaFechandoObj == null)
            telaPretaFechandoObj = GameObject.Find("TelaPretaFechar");
        if (telaPretaFechandoObj != null)
            telaPretaFechandoObj.SetActive(false);

        // 1. Estado inicial: Intro e tela preta ligadas, player desligado
        if (camIntroObj != null) camIntroObj.SetActive(true);
        if (telaPretaObj != null) telaPretaObj.SetActive(true);
        if (playerObj != null) playerObj.SetActive(false);

        StartCoroutine(TocarSomDeAcordar());

        // 2. Inicia a contagem
        StartCoroutine(TrocarParaPlayer());
    }

    private void PrepararVisaoEmbacada()
    {
        GameObject objetoVolume = new GameObject("Volume-VisaoEmbacada-AoAcordar");
        objetoVolume.layer = LayerMask.NameToLayer("Default");
        volumeVisaoEmbacada = objetoVolume.AddComponent<Volume>();
        volumeVisaoEmbacada.isGlobal = true;
        volumeVisaoEmbacada.priority = 100f;
        volumeVisaoEmbacada.weight = 1f;

        perfilVisaoEmbacada = ScriptableObject.CreateInstance<VolumeProfile>();
        perfilVisaoEmbacada.name = "Perfil-VisaoEmbacada-AoAcordar";
        profundidadeDeCampo = perfilVisaoEmbacada.Add<DepthOfField>(true);
        profundidadeDeCampo.active = true;
        profundidadeDeCampo.mode.Override(DepthOfFieldMode.Gaussian);
        profundidadeDeCampo.gaussianStart.Override(0f);
        profundidadeDeCampo.gaussianEnd.Override(0.1f);
        profundidadeDeCampo.gaussianMaxRadius.Override(1.5f);
        profundidadeDeCampo.highQualitySampling.Override(true);

        vinhetaVisaoEmbacada = perfilVisaoEmbacada.Add<Vignette>(true);
        vinhetaVisaoEmbacada.active = true;
        vinhetaVisaoEmbacada.color.Override(Color.black);
        vinhetaVisaoEmbacada.center.Override(new Vector2(0.5f, 0.5f));
        vinhetaVisaoEmbacada.intensity.Override(0.55f);
        vinhetaVisaoEmbacada.smoothness.Override(0.75f);
        vinhetaVisaoEmbacada.rounded.Override(false);
        volumeVisaoEmbacada.sharedProfile = perfilVisaoEmbacada;
    }

    private IEnumerator ClarearVisaoAoAcordar()
    {
        if (volumeVisaoEmbacada == null || profundidadeDeCampo == null || vinhetaVisaoEmbacada == null) yield break;

        const float duracao = 5f;
        const float desfoqueInicial = 0.1f;
        const float desfoqueDuranteOJogo = 5f;
        const float vinhetaInicial = 0.55f;
        const float vinhetaDuranteOJogo = 0.4f;
        float tempo = 0f;
        while (tempo < duracao && volumeVisaoEmbacada != null && profundidadeDeCampo != null && vinhetaVisaoEmbacada != null)
        {
            tempo += Time.unscaledDeltaTime;
            float progresso = Mathf.Clamp01(tempo / duracao);
            float suave = progresso * progresso * (3f - 2f * progresso);
            profundidadeDeCampo.gaussianEnd.value = Mathf.Lerp(desfoqueInicial, desfoqueDuranteOJogo, suave);
            vinhetaVisaoEmbacada.intensity.value = Mathf.Lerp(vinhetaInicial, vinhetaDuranteOJogo, suave);
            yield return null;
        }

        // Mantém os dois efeitos ativos no restante do Nivel4 com intensidade reduzida.
    }

    private IEnumerator TocarSomDeAcordar()
    {
        AudioClip bocejo = SomJogo.Carregar("Snd-Bocejo");
        if (bocejo == null)
        {
            Debug.LogWarning("IntroManager: não foi possível carregar Snd-Bocejo em Resources/Sons/AudioClip.", this);
            yield break;
        }

        bocejo.LoadAudioData();
        while (bocejo.loadState == AudioDataLoadState.Loading)
            yield return null;

        if (bocejo.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogWarning("IntroManager: os dados de Snd-Bocejo não foram carregados.", this);
            yield break;
        }

        AudioSource fonte = GetComponent<AudioSource>();
        if (fonte == null) fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.spatialBlend = 0f;
        fonte.PlayOneShot(bocejo);
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
        if (volumeVisaoEmbacada != null)
            StartCoroutine(ClarearVisaoAoAcordar());
    }
}
