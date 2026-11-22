using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class IntroManager : MonoBehaviour
{
    private static bool entradaNivel5Preparada;
    private static Vector3 posicaoEntradaNivel5;
    private static bool manterVisaoEmbacadaNoNivel5;
    private static float desfoqueNivel5 = 5f;
    private static float raioDesfoqueNivel5 = 1.5f;
    private static float vinhetaNivel5 = 0.4f;

    private const float DistanciaParaAproximarMedico = 3f;
    private const float TempoMaximoParaVisitarMedicos = 40f;
    private const float AtrasoAntesDaTransicaoNivel5 = 3f;
    private const float DuracaoAteTeleporteNivel4 = 7f;
    private const float DuracaoFechamentoTransicao = TaskManager.DuracaoFechamentoTelaPretaRapida;
    private const float DuracaoEfeitoFinalNivel4 = DuracaoAteTeleporteNivel4 - DuracaoFechamentoTransicao;
    private static readonly Vector3 PosicaoEntradaNivel5Padrao = new Vector3(11.4799995f, 4.67500019f, -31.3330002f);

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
    private Transform[] medicosNivel4;
    private readonly HashSet<int> medicosVisitados = new();
    private bool visaoInicialNivel4Concluida;
    private bool transicaoNivel4Iniciada;

    public static void PrepararEntradaDiretaNivel5(Vector3 posicao)
    {
        posicaoEntradaNivel5 = posicao;
        entradaNivel5Preparada = true;
    }

    void Start()
    {
        string cenaAtual = SceneManager.GetActiveScene().name;
        if (string.Equals(cenaAtual, "Nivel1", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(cenaAtual, "Nivel2", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(cenaAtual, "Nivel3", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(cenaAtual, "Nivel4", System.StringComparison.OrdinalIgnoreCase))
        {
            StartCoroutine(TocarSomDeAcordar());
        }

        if (string.Equals(cenaAtual, "Nivel5", System.StringComparison.OrdinalIgnoreCase))
        {
            IniciarNivel5SemIntro();
            return;
        }

        if (string.Equals(cenaAtual, "Nivel4", System.StringComparison.OrdinalIgnoreCase))
        {
            GarantirColisaoMedicos();
            PrepararVisaoEmbacada();
            medicosNivel4 = new Transform[3];
            string[] nomesMedicos = { "Medico1", "Medico2", "Medico3" };
            for (int i = 0; i < nomesMedicos.Length; i++)
            {
                GameObject medico = GameObject.Find(nomesMedicos[i]);
                if (medico != null) medicosNivel4[i] = medico.transform;
                else Debug.LogWarning($"IntroManager: não encontrou {nomesMedicos[i]} no Nivel4.", this);
            }
            StartCoroutine(AguardarAproximacaoDosMedicos());
        }

        SaveGameDatabase.SaveData save = SaveGameDatabase.Load();
        if (save != null
            && string.Equals(save.sceneName, cenaAtual, System.StringComparison.OrdinalIgnoreCase))
        {
            IniciarDoSaveSemAnimacao();
            return;
        }

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

        // 2. Inicia a contagem
        StartCoroutine(TrocarParaPlayer());
    }

    // Restaura o jogo carregado sem ativar câmera, painel ou áudio da introdução.
    private void IniciarDoSaveSemAnimacao()
    {
        if (camIntroObj == null) camIntroObj = GameObject.Find("CamIntro");
        if (telaPretaObj == null) telaPretaObj = EncontrarObjetoNaCena("TelaPreta");
        if (telaPretaFechandoObj == null) telaPretaFechandoObj = EncontrarObjetoNaCena("TelaPretaFechar");
        if (telaPretaFecharRapidoObj == null) telaPretaFecharRapidoObj = EncontrarObjetoNaCena("TelaPretaFecharRapido");

        if (camIntroObj != null) camIntroObj.SetActive(false);
        if (telaPretaObj != null) telaPretaObj.SetActive(false);
        if (telaPretaFechandoObj != null) telaPretaFechandoObj.SetActive(false);
        if (telaPretaFecharRapidoObj != null) telaPretaFecharRapidoObj.SetActive(false);

        if (playerObj == null) return;
        playerObj.SetActive(true);
        PlayerFPS jogador = playerObj.GetComponent<PlayerFPS>();
        if (jogador == null) return;

        Camera cameraDoJogador = jogador.cameraPlayer != null
            ? jogador.cameraPlayer.GetComponent<Camera>()
            : null;
        if (cameraDoJogador != null)
        {
            foreach (Camera cameraNaCena in FindObjectsByType<Camera>())
                cameraNaCena.enabled = cameraNaCena == cameraDoJogador;
            cameraDoJogador.enabled = true;
        }

        // PauseMenuManager restaura posição e rotação após PlayerFPS.Start
        // capturar a posição local original da câmera.
        Debug.Log("IntroManager: introdução ignorada; posição será restaurada pelo menu de pausa.", this);
    }

    private void IniciarNivel5SemIntro()
    {
        bool veioDoNivel4 = entradaNivel5Preparada;
        if (telaPretaObj == null) telaPretaObj = EncontrarObjetoNaCena("TelaPreta");
        if (telaPretaFechandoObj == null) telaPretaFechandoObj = EncontrarObjetoNaCena("TelaPretaFechar");

        if (camIntroObj != null) camIntroObj.SetActive(false);
        if (telaPretaObj != null) telaPretaObj.SetActive(false);
        if (telaPretaFechandoObj != null) telaPretaFechandoObj.SetActive(false);

        if (playerObj != null)
        {
            playerObj.SetActive(true);
            if (entradaNivel5Preparada)
            {
                CharacterController controlador = playerObj.GetComponent<CharacterController>();
                if (controlador != null) controlador.enabled = false;
                playerObj.transform.position = posicaoEntradaNivel5;
                Physics.SyncTransforms();
                if (controlador != null) controlador.enabled = true;
            }
        }

        MusicaNivel5 musicaNivel5 = GetComponent<MusicaNivel5>();
        if (musicaNivel5 == null) musicaNivel5 = gameObject.AddComponent<MusicaNivel5>();
        musicaNivel5.IniciarAposChegada();

        if (veioDoNivel4 && manterVisaoEmbacadaNoNivel5)
        {
            PrepararVisaoEmbacada();
            profundidadeDeCampo.gaussianEnd.value = desfoqueNivel5;
            profundidadeDeCampo.gaussianMaxRadius.value = raioDesfoqueNivel5;
            vinhetaVisaoEmbacada.intensity.value = vinhetaNivel5;
            manterVisaoEmbacadaNoNivel5 = false;
        }
        else if (!veioDoNivel4)
        {
            // Entradas diretas e carregamentos do Nivel5 devem ter a mesma
            // visão persistente que sobra após a transição vinda do Nivel4.
            PrepararVisaoEmbacada();
            profundidadeDeCampo.gaussianEnd.value = desfoqueNivel5;
            profundidadeDeCampo.gaussianMaxRadius.value = raioDesfoqueNivel5;
            vinhetaVisaoEmbacada.intensity.value = vinhetaNivel5;
        }

        StartCoroutine(SalvarDepoisDeLiberarJogador());

        GameObject telaRapida = telaPretaFecharRapidoObj != null
            ? telaPretaFecharRapidoObj
            : EncontrarObjetoNaCena("TelaPretaFecharRapido");
        if (telaRapida != null)
        {
            if (veioDoNivel4)
            {
                telaRapida.SetActive(true);
                foreach (Animator animator in telaRapida.GetComponentsInChildren<Animator>(true))
                {
                    string estado = animator.name == "Cima" ? "FecharRapidoCima"
                        : animator.name == "Baixo" ? "FecharRapidoBaixo" : null;
                    if (estado == null) continue;

                    animator.enabled = true;
                    animator.Play(estado, 0,
                        TaskManager.DuracaoFechamentoTelaPretaRapida / TaskManager.DuracaoAnimacaoTelaPretaRapida);
                    animator.Update(0f);
                }
                StartCoroutine(OcultarTelaRapidaAposAbertura(telaRapida));
            }
            else
            {
                telaRapida.SetActive(false);
            }
        }

        entradaNivel5Preparada = false;
    }

    private IEnumerator OcultarTelaRapidaAposAbertura(GameObject telaRapida)
    {
        yield return new WaitForSeconds(
            TaskManager.DuracaoAnimacaoTelaPretaRapida - TaskManager.DuracaoFechamentoTelaPretaRapida);
        if (telaRapida != null) telaRapida.SetActive(false);
    }

    private IEnumerator AguardarAproximacaoDosMedicos()
    {
        while (!visaoInicialNivel4Concluida)
            yield return null;

        PlayerFPS jogador = playerObj != null ? playerObj.GetComponent<PlayerFPS>() : null;
        if (jogador == null || medicosNivel4 == null) yield break;

        float inicioBuscaMedicos = Time.time;
        while (!transicaoNivel4Iniciada)
        {
            for (int i = 0; i < medicosNivel4.Length; i++)
            {
                Transform medico = medicosNivel4[i];
                if (medico == null || medicosVisitados.Contains(i)) continue;

                Vector2 jogadorXZ = new Vector2(jogador.transform.position.x, jogador.transform.position.z);
                Vector2 medicoXZ = new Vector2(medico.position.x, medico.position.z);
                if (Vector2.Distance(jogadorXZ, medicoXZ) <= DistanciaParaAproximarMedico)
                    medicosVisitados.Add(i);
            }

            bool tempoEsgotado = Time.time - inicioBuscaMedicos >= TempoMaximoParaVisitarMedicos;
            if (medicosVisitados.Count == medicosNivel4.Length || tempoEsgotado)
            {
                if (tempoEsgotado && medicosVisitados.Count < medicosNivel4.Length)
                {
                    for (int i = 0; i < medicosNivel4.Length; i++)
                        medicosVisitados.Add(i);
                    Debug.Log("IntroManager: limite de 40 segundos atingido; médicos restantes marcados como vistos.", this);
                }
                transicaoNivel4Iniciada = true;
                yield return StartCoroutine(ConcluirNivel4AposVisitarMedicos());
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator ConcluirNivel4AposVisitarMedicos()
    {
        yield return new WaitForSeconds(AtrasoAntesDaTransicaoNivel5);

        float duracaoEfeitoFinal = DuracaoEfeitoFinalNivel4;
        AudioClip zumbido = Resources.Load<AudioClip>("Sons/AudioClip/Snd-ZumbidoTeleporteNivel5");
        if (zumbido != null)
        {
            zumbido.LoadAudioData();
            while (zumbido.loadState == AudioDataLoadState.Loading)
                yield return null;

            if (zumbido.loadState == AudioDataLoadState.Loaded)
            {
                AudioSource fonteZumbido = GetComponent<AudioSource>();
                if (fonteZumbido == null) fonteZumbido = gameObject.AddComponent<AudioSource>();
                fonteZumbido.playOnAwake = false;
                fonteZumbido.loop = false;
                fonteZumbido.spatialBlend = 0f;
                fonteZumbido.volume = 1f;
                fonteZumbido.clip = zumbido;
                fonteZumbido.Play();
                duracaoEfeitoFinal = Mathf.Max(0.1f, zumbido.length - DuracaoFechamentoTransicao);
            }
            else
            {
                Debug.LogWarning("IntroManager: não foi possível carregar o zumbido para a transição ao Nivel5.", this);
            }
        }
        else
        {
            Debug.LogWarning("IntroManager: não encontrou Snd-ZumbidoTeleporteNivel5 em Resources/Sons/AudioClip.", this);
        }

        float desfoqueInicial = profundidadeDeCampo.gaussianEnd.value;
        float vinhetaInicial = vinhetaVisaoEmbacada.intensity.value;
        float suavidadeVinhetaInicial = vinhetaVisaoEmbacada.smoothness.value;
        float raioInicial = profundidadeDeCampo.gaussianMaxRadius.value;
        float tempo = 0f;
        while (tempo < duracaoEfeitoFinal)
        {
            tempo += Time.deltaTime;
            float progresso = Mathf.Clamp01(tempo / duracaoEfeitoFinal);
            float suave = progresso * progresso * (3f - 2f * progresso);
            profundidadeDeCampo.gaussianEnd.value = Mathf.Lerp(desfoqueInicial, 0.1f, suave);
            profundidadeDeCampo.gaussianMaxRadius.value = Mathf.Lerp(raioInicial, 2f, suave);
            vinhetaVisaoEmbacada.intensity.value = Mathf.Lerp(vinhetaInicial, 1f, suave);
            vinhetaVisaoEmbacada.smoothness.value = Mathf.Lerp(suavidadeVinhetaInicial, 1f, suave);
            yield return null;
        }

        profundidadeDeCampo.gaussianEnd.value = 0.1f;
        profundidadeDeCampo.gaussianMaxRadius.value = 2f;
        vinhetaVisaoEmbacada.intensity.value = 1f;
        vinhetaVisaoEmbacada.smoothness.value = 1f;

        // No Nivel5, retoma a visão leve que permanece após a sequência de acordar.
        desfoqueNivel5 = 5f;
        raioDesfoqueNivel5 = 1.5f;
        vinhetaNivel5 = 0.4f;
        manterVisaoEmbacadaNoNivel5 = true;

        TaskManager tarefas = FindAnyObjectByType<TaskManager>();
        if (tarefas != null)
            tarefas.ReproduzirTelaPretaFecharRapido();
        else
            Debug.LogWarning("IntroManager: não encontrou TaskManager para iniciar TelaPretaFecharRapido.", this);

        yield return new WaitForSeconds(DuracaoFechamentoTransicao);
        ManterFechamentoTotalAteTrocarCena();
        PrepararEntradaDiretaNivel5(PosicaoEntradaNivel5Padrao);
        SceneManager.LoadScene("Nivel5");
    }

    private void ManterFechamentoTotalAteTrocarCena()
    {
        GameObject telaRapida = EncontrarObjetoNaCena("TelaPretaFecharRapido");
        if (telaRapida == null) return;

        float pontoFechamento = TaskManager.DuracaoFechamentoTelaPretaRapida
            / TaskManager.DuracaoAnimacaoTelaPretaRapida;
        foreach (Animator animator in telaRapida.GetComponentsInChildren<Animator>(true))
        {
            string estado = animator.name == "Cima" ? "FecharRapidoCima"
                : animator.name == "Baixo" ? "FecharRapidoBaixo" : null;
            if (estado == null) continue;

            animator.Play(estado, 0, pontoFechamento);
            animator.Update(0f);
            animator.speed = 0f;
        }
    }

    private static void GarantirColisaoMedicos()
    {
        string[] nomes = { "Medico1", "Medico2", "Medico3" };
        foreach (string nome in nomes)
        {
            GameObject medico = GameObject.Find(nome);
            if (medico == null || medico.GetComponent<CapsuleCollider>() != null) continue;

            Renderer[] renderers = medico.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) continue;

            Bounds limites = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                limites.Encapsulate(renderers[i].bounds);

            Vector3 escala = medico.transform.lossyScale;
            float escalaY = Mathf.Max(Mathf.Abs(escala.y), 0.001f);
            float escalaHorizontal = Mathf.Max(Mathf.Abs(escala.x), Mathf.Abs(escala.z), 0.001f);
            CapsuleCollider colisor = medico.AddComponent<CapsuleCollider>();
            colisor.center = medico.transform.InverseTransformPoint(limites.center);
            colisor.height = Mathf.Max(limites.size.y / escalaY, 0.5f);
            colisor.radius = Mathf.Max(Mathf.Max(limites.size.x, limites.size.z) * 0.3f / escalaHorizontal, 0.2f);
        }
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
            tempo += Time.deltaTime;
            float progresso = Mathf.Clamp01(tempo / duracao);
            float suave = progresso * progresso * (3f - 2f * progresso);
            profundidadeDeCampo.gaussianEnd.value = Mathf.Lerp(desfoqueInicial, desfoqueDuranteOJogo, suave);
            vinhetaVisaoEmbacada.intensity.value = Mathf.Lerp(vinhetaInicial, vinhetaDuranteOJogo, suave);
            yield return null;
        }

        // Mantém os dois efeitos ativos no restante do Nivel4 com intensidade reduzida.
        visaoInicialNivel4Concluida = true;
    }

    private static GameObject EncontrarObjetoNaCena(string nome)
    {
        foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform encontrado = EncontrarDescendente(raiz.transform, nome);
            if (encontrado != null) return encontrado.gameObject;
        }
        return null;
    }

    private static Transform EncontrarDescendente(Transform raiz, string nome)
    {
        if (raiz == null) return null;
        if (raiz.name == nome) return raiz;
        for (int i = 0; i < raiz.childCount; i++)
        {
            Transform encontrado = EncontrarDescendente(raiz.GetChild(i), nome);
            if (encontrado != null) return encontrado;
        }
        return null;
    }

    private IEnumerator TocarSomDeAcordar()
    {
        yield return null;

        AudioClip bocejo = SomJogo.Carregar("Snd-Bocejo");
        if (bocejo == null)
        {
            Debug.LogWarning("IntroManager: não foi possível carregar Snd-Bocejo em Resources/Sons/AudioClip.", this);
            yield break;
        }

        if (bocejo.loadState != AudioDataLoadState.Loaded)
            bocejo.LoadAudioData();
        float tempoLimiteDeCarregamento = 5f;
        while (bocejo.loadState == AudioDataLoadState.Loading
            || bocejo.loadState == AudioDataLoadState.Unloaded)
        {
            if (tempoLimiteDeCarregamento <= 0f)
                break;
            tempoLimiteDeCarregamento -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (bocejo.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogWarning("IntroManager: os dados de Snd-Bocejo não foram carregados.", this);
            yield break;
        }

        AudioListener ouvinte = Camera.main != null
            ? Camera.main.GetComponent<AudioListener>()
            : FindAnyObjectByType<AudioListener>();
        AudioSource fonte = ouvinte != null
            ? ouvinte.GetComponent<AudioSource>()
            : GetComponent<AudioSource>();
        if (fonte == null)
            fonte = (ouvinte != null ? ouvinte.gameObject : gameObject).AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.loop = false;
        fonte.mute = false;
        fonte.volume = 1f;
        fonte.ignoreListenerPause = true;
        fonte.spatialBlend = 0f;
        fonte.clip = bocejo;
        fonte.Play();
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

        yield return SalvarDepoisDeLiberarJogador();
    }

    private IEnumerator SalvarDepoisDeLiberarJogador()
    {
        if (playerObj == null || playerObj.GetComponent<PlayerFPS>() == null)
            yield break;

        // O PlayerFPS adiciona o PauseMenuManager no Start; aguarde essa inicialização.
        yield return null;
        while (FindAnyObjectByType<PauseMenuManager>() == null)
            yield return null;

        PauseMenuManager.TentarAutosave();
    }
}
