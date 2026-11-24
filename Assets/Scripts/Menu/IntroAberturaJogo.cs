using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Abertura cinematográfica em tempo real para Memórias Partidas.
/// O cenário low-poly é montado com formas 3D; nenhuma imagem do projeto é carregada.
/// </summary>
public sealed class IntroAberturaJogo : MonoBehaviour
{
    private const int CamadaDaIntro = 30;
    private const string ChaveVolumeMusica = "MusicVolume";
    private const string ChaveVolumeSfx = "SFXVolume";
    private const float DuracaoDaIntro = 31f;

    private enum Fase
    {
        Despertar,
        Rotina,
        Lapso,
        Reconstrucao,
        Titulo,
        Concluida
    }

    private readonly List<Material> materiaisGerados = new();
    private readonly List<Mesh> malhasGeradas = new();

    private GameObject canvasDaIntro;
    private GameObject raizDaCena;
    private CanvasGroup grupoDaTela;
    private CanvasGroup grupoDoTitulo;
    private RawImage imagemDaCena;
    private RawImage escurecimentoDoTitulo;
    private RawImage sobreposicaoDoLapso;
    private TMP_Text textoTitulo;
    private TMP_Text textoTituloEco;
    private TMP_Text textoIdentidade;
    private TMP_Text textoFones;
    private RectTransform retanguloDoTitulo;
    private Camera cameraDaCena;
    private Camera cameraDoMenu;
    private RenderTexture renderDaCena;
    private Transform cameraTransform;
    private Transform ponteiroDosSegundos;
    private Transform ponteiroDosMinutos;
    private Transform metadeEsquerdaDaFoto;
    private Transform metadeDireitaDaFoto;
    private Light luzPrincipal;
    private Light luzDaJanela;
    private AudioSource musica;
    private AudioSource somDoRelogio;
    private Coroutine fadeDeMusica;
    private Action aoConcluir;
    private Fase fase;
    private float instanteInicial;
    private float progressoDaFase;
    private float anguloDosSegundos;
    private float anguloDosMinutos;
    private float volumeDoRelogio;
    private int mascaraOriginalDaCamera;
    private bool mascaraOriginalSalva;
    private bool concluida;

    public void Iniciar(AudioSource trilha, Action callback)
    {
        if (canvasDaIntro != null) return;

        musica = trilha;
        aoConcluir = callback;
        instanteInicial = Time.unscaledTime;

        CriarCena3D();
        CriarInterface();
        CriarAudioRelogio();

        if (musica != null)
            fadeDeMusica = StartCoroutine(FadeMusica(VolumeMusicaConfigurado(), 4.5f));
    }

    private void Update()
    {
        if (concluida || cameraDaCena == null) return;

        float tempo = Time.unscaledTime - instanteInicial;
        if (tempo >= DuracaoDaIntro)
        {
            ConcluirIntro();
            return;
        }

        AtualizarFase(tempo);
        AtualizarCamera(tempo);
        AtualizarRelogio();
        AtualizarColagem();
        AtualizarLuzes();
        AtualizarInterface(tempo);
        AtualizarAudio(tempo);
    }

    private void CriarCena3D()
    {
        cameraDoMenu = Camera.main;
        if (cameraDoMenu != null)
        {
            mascaraOriginalDaCamera = cameraDoMenu.cullingMask;
            mascaraOriginalSalva = true;
            cameraDoMenu.cullingMask &= ~(1 << CamadaDaIntro);
        }

        raizDaCena = new GameObject("Cenario_Cinematografico_Procedural");
        raizDaCena.layer = CamadaDaIntro;

        CriarParedeEJanela(raizDaCena.transform);
        CriarRelogioDeParede(raizDaCena.transform);
        CriarQuadroDeRotina(raizDaCena.transform);
        CriarMesa(raizDaCena.transform);
        CriarXicara(raizDaCena.transform);
        CriarFotografiaDaFamilia(raizDaCena.transform);
        CriarRemedios(raizDaCena.transform);
        CriarPlanta(raizDaCena.transform);
        CriarIluminacao(raizDaCena.transform);
        CriarCameraCinematografica(raizDaCena.transform);
    }

    private void CriarInterface()
    {
        canvasDaIntro = new GameObject(
            "Canvas_Intro_Memorias_Partidas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));

        Canvas canvas = canvasDaIntro.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32000;

        CanvasScaler escalador = canvasDaIntro.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        grupoDaTela = canvasDaIntro.GetComponent<CanvasGroup>();
        grupoDaTela.alpha = 1f;
        grupoDaTela.interactable = false;
        grupoDaTela.blocksRaycasts = true;

        imagemDaCena = CriarImagemDeTela(canvasDaIntro.transform, "Imagem_Da_Cena_3D", Texture2D.blackTexture);
        if (renderDaCena != null) imagemDaCena.texture = renderDaCena;
        imagemDaCena.color = Color.white;

        sobreposicaoDoLapso = CriarImagemDeTela(canvasDaIntro.transform, "Lapso_De_Memoria", Texture2D.whiteTexture);
        sobreposicaoDoLapso.color = new Color(0.42f, 0.46f, 0.47f, 0f);

        escurecimentoDoTitulo = CriarImagemDeTela(canvasDaIntro.transform, "Escurecimento_Da_Cartela", Texture2D.whiteTexture);
        escurecimentoDoTitulo.color = new Color(0.045f, 0.038f, 0.034f, 0f);

        CriarCartelaDeTitulo(canvasDaIntro.transform);

        RawImage inicioPreto = CriarImagemDeTela(canvasDaIntro.transform, "Abertura_Em_Preto", Texture2D.whiteTexture);
        inicioPreto.color = Color.black;
        inicioPreto.transform.SetAsLastSibling();
        inicioPreto.name = "Abertura_Em_Preto";
        fadeInicial = inicioPreto;
        fadeInicial.color = Color.black;
    }

    private RawImage fadeInicial;

    private void CriarCartelaDeTitulo(Transform pai)
    {
        GameObject objetoGrupo = new GameObject("Cartela_Do_Titulo", typeof(RectTransform), typeof(CanvasGroup));
        objetoGrupo.transform.SetParent(pai, false);
        EsticarNaTela(objetoGrupo.GetComponent<RectTransform>());
        grupoDoTitulo = objetoGrupo.GetComponent<CanvasGroup>();
        grupoDoTitulo.alpha = 0f;
        grupoDoTitulo.interactable = false;
        grupoDoTitulo.blocksRaycasts = false;

        escurecimentoDoTitulo.transform.SetParent(objetoGrupo.transform, false);
        escurecimentoDoTitulo.transform.SetAsFirstSibling();

        GameObject marca = CriarContainer(objetoGrupo.transform, "Marca_Da_Turma", new Vector2(0f, 225f), new Vector2(1500f, 96f));
        textoIdentidade = CriarTexto(marca.transform, "Credito_3_PJD", "3º PJD - 2026", 28f, Vector2.zero, new Vector2(850f, 56f), FontStyles.Normal, new Color(0.88f, 0.78f, 0.64f, 1f));
        textoIdentidade.characterSpacing = 6f;
        CriarRetangulo(marca.transform, "Filete_Do_Credito", new Vector2(0f, -36f), new Vector2(120f, 1.5f), new Color(0.76f, 0.57f, 0.37f, 0.78f));

        GameObject titulo = CriarContainer(objetoGrupo.transform, "Titulo_Em_Duas_Linhas", Vector2.zero, new Vector2(1600f, 420f));
        textoTituloEco = CriarTexto(titulo.transform, "Eco_Impresso", "MEMÓRIAS\nPARTIDAS", 126f, new Vector2(3f, -7f), new Vector2(1520f, 400f), FontStyles.Bold, new Color(0.69f, 0.44f, 0.29f, 0.22f));
        textoTituloEco.characterSpacing = 9f;
        textoTituloEco.lineSpacing = -18f;
        textoTitulo = CriarTexto(titulo.transform, "Nome_Do_Jogo", "MEMÓRIAS\nPARTIDAS", 126f, Vector2.zero, new Vector2(1520f, 400f), FontStyles.Bold, new Color(0.95f, 0.91f, 0.82f, 1f));
        textoTitulo.characterSpacing = 9f;
        textoTitulo.lineSpacing = -18f;
        retanguloDoTitulo = titulo.GetComponent<RectTransform>();

        textoFones = CriarTexto(objetoGrupo.transform, "Mensagem_Dos_Fones", "Use fones para uma melhor experiência", 26f, new Vector2(0f, -392f), new Vector2(1200f, 56f), FontStyles.Normal, new Color(0.82f, 0.80f, 0.74f, 1f));
        textoFones.characterSpacing = 1.5f;
    }

    private void CriarParedeEJanela(Transform pai)
    {
        CriarCubo(pai, "Parede_Da_Casa", new Vector3(0f, 4.2f, -3.2f), new Vector3(13f, 9f, 0.3f), new Color(0.49f, 0.44f, 0.34f));
        CriarCubo(pai, "Sombra_Do_Canto", new Vector3(-6.35f, 4f, -1.5f), new Vector3(0.25f, 8.8f, 3.2f), new Color(0.38f, 0.34f, 0.27f));

        Vector3 centroJanela = new Vector3(-3.65f, 5.15f, -2.94f);
        CriarCubo(pai, "Moldura_Da_Janela", centroJanela, new Vector3(2.55f, 2.42f, 0.14f), new Color(0.30f, 0.25f, 0.20f));
        CriarCubo(pai, "Vidro_Da_Janela", centroJanela + new Vector3(0f, 0f, 0.09f), new Vector3(2.29f, 2.16f, 0.04f), new Color(0.56f, 0.63f, 0.62f));
        CriarCubo(pai, "Reflexo_Do_Vidro", centroJanela + new Vector3(-0.57f, 0f, 0.12f), new Vector3(1.12f, 2.06f, 0.025f), new Color(0.76f, 0.76f, 0.64f));
        CriarCubo(pai, "Travessa_Da_Janela_Vertical", centroJanela + new Vector3(0f, 0f, 0.16f), new Vector3(0.09f, 2.2f, 0.06f), new Color(0.35f, 0.29f, 0.23f));
        CriarCubo(pai, "Travessa_Da_Janela_Horizontal", centroJanela + new Vector3(0f, 0f, 0.16f), new Vector3(2.38f, 0.09f, 0.06f), new Color(0.35f, 0.29f, 0.23f));
        CriarCubo(pai, "Cortina_Esquerda", centroJanela + new Vector3(-1.18f, -0.02f, 0.25f), new Vector3(0.34f, 2.55f, 0.17f), new Color(0.52f, 0.42f, 0.32f));
        CriarCubo(pai, "Cortina_Direita", centroJanela + new Vector3(1.16f, -0.02f, 0.25f), new Vector3(0.38f, 2.55f, 0.17f), new Color(0.52f, 0.42f, 0.32f));
    }

    private void CriarRelogioDeParede(Transform pai)
    {
        GameObject objeto = new GameObject("Relogio_De_Parede");
        objeto.transform.SetParent(pai, false);
        objeto.transform.localPosition = new Vector3(0.15f, 5.18f, -2.88f);

        CriarCilindro(objeto.transform, "Aro_De_Madeira", Vector3.zero, new Vector3(1.75f, 0.13f, 1.75f), new Vector3(90f, 0f, 0f), new Color(0.29f, 0.20f, 0.14f));
        CriarCilindro(objeto.transform, "Mostrador", new Vector3(0f, 0f, 0.11f), new Vector3(1.55f, 0.07f, 1.55f), new Vector3(90f, 0f, 0f), new Color(0.82f, 0.76f, 0.64f));

        for (int i = 0; i < 12; i++)
        {
            float angulo = i * Mathf.PI / 6f;
            Vector3 posicao = new Vector3(Mathf.Sin(angulo) * 0.59f, Mathf.Cos(angulo) * 0.59f, 0.17f);
            CriarCubo(objeto.transform, "Marca_Do_Relogio", posicao, new Vector3(i % 3 == 0 ? 0.055f : 0.03f, i % 3 == 0 ? 0.14f : 0.085f, 0.035f), new Color(0.38f, 0.30f, 0.23f));
        }

        GameObject eixo = new GameObject("Eixo_Dos_Ponteiros");
        eixo.transform.SetParent(objeto.transform, false);
        eixo.transform.localPosition = new Vector3(0f, 0f, 0.21f);
        ponteiroDosMinutos = CriarCubo(eixo.transform, "Ponteiro_Dos_Minutos", new Vector3(0f, 0.20f, 0f), new Vector3(0.06f, 0.43f, 0.035f), new Color(0.28f, 0.23f, 0.19f)).transform;
        ponteiroDosSegundos = CriarCubo(eixo.transform, "Ponteiro_Dos_Segundos", new Vector3(0f, 0.27f, 0.035f), new Vector3(0.025f, 0.58f, 0.025f), new Color(0.60f, 0.31f, 0.22f)).transform;
        CriarEsfera(objeto.transform, "Centro_Do_Relogio", new Vector3(0f, 0f, 0.25f), new Vector3(0.11f, 0.11f, 0.07f), new Color(0.35f, 0.27f, 0.20f));
    }

    private void CriarQuadroDeRotina(Transform pai)
    {
        Vector3 centro = new Vector3(3.75f, 5.0f, -2.88f);
        CriarCubo(pai, "Moldura_Do_Quadro", centro, new Vector3(1.55f, 1.95f, 0.13f), new Color(0.34f, 0.25f, 0.18f));
        CriarCubo(pai, "Papel_Da_Rotina", centro + new Vector3(0f, 0f, 0.09f), new Vector3(1.34f, 1.74f, 0.035f), new Color(0.83f, 0.76f, 0.63f));
        CriarCubo(pai, "Cabecalho_Da_Rotina", centro + new Vector3(0f, 0.57f, 0.12f), new Vector3(0.82f, 0.045f, 0.025f), new Color(0.46f, 0.37f, 0.28f));

        for (int i = 0; i < 3; i++)
        {
            float y = 0.25f - i * 0.42f;
            CriarCubo(pai, "Caixa_De_Tarefa", centro + new Vector3(-0.43f, y, 0.13f), new Vector3(0.15f, 0.15f, 0.035f), new Color(0.56f, 0.48f, 0.37f));
            CriarCubo(pai, "Papel_Da_Caixa", centro + new Vector3(-0.43f, y, 0.155f), new Vector3(0.095f, 0.095f, 0.025f), new Color(0.82f, 0.76f, 0.63f));
            CriarCubo(pai, "Linha_Da_Tarefa", centro + new Vector3(0.12f, y, 0.13f), new Vector3(0.72f, 0.035f, 0.025f), new Color(0.50f, 0.42f, 0.32f));
        }
    }

    private void CriarMesa(Transform pai)
    {
        CriarCubo(pai, "Tampo_Da_Mesa", new Vector3(0f, 1.63f, 0.15f), new Vector3(10.4f, 0.30f, 2.8f), new Color(0.38f, 0.26f, 0.19f));
        CriarCubo(pai, "Borda_Quente_Da_Mesa", new Vector3(0f, 1.78f, 0.15f), new Vector3(10.4f, 0.045f, 2.8f), new Color(0.54f, 0.38f, 0.26f));
        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
                CriarCubo(pai, "Pe_Da_Mesa", new Vector3(x * 4.65f, 0.77f, 0.15f + z * 1.13f), new Vector3(0.20f, 1.50f, 0.20f), new Color(0.30f, 0.21f, 0.16f));
        }
    }

    private void CriarXicara(Transform pai)
    {
        Vector3 posicao = new Vector3(-2.85f, 2.18f, 0.1f);
        CriarCilindro(pai, "Xicara_De_Cafe", posicao, new Vector3(0.82f, 0.48f, 0.82f), Vector3.zero, new Color(0.78f, 0.73f, 0.64f));
        CriarCilindro(pai, "Cafe_Quente", posicao + new Vector3(0f, 0.455f, 0f), new Vector3(0.67f, 0.035f, 0.67f), Vector3.zero, new Color(0.25f, 0.15f, 0.095f));
        CriarMeshArgola(pai, "Alca_Da_Xicara", posicao + new Vector3(0.47f, 0.02f, 0f), 0.31f, 0.065f, new Color(0.78f, 0.73f, 0.64f));
    }

    private void CriarFotografiaDaFamilia(Transform pai)
    {
        GameObject moldura = new GameObject("Fotografia_Da_Familia");
        moldura.transform.SetParent(pai, false);
        moldura.transform.localPosition = new Vector3(-0.25f, 2.48f, 0.58f);

        CriarCubo(moldura.transform, "Moldura_De_Madeira", Vector3.zero, new Vector3(1.76f, 1.28f, 0.16f), new Color(0.31f, 0.22f, 0.16f));
        CriarCubo(moldura.transform, "Borda_Dourada", new Vector3(0f, 0f, 0.095f), new Vector3(1.62f, 1.14f, 0.04f), new Color(0.66f, 0.50f, 0.33f));
        CriarCubo(moldura.transform, "Papel_Da_Fotografia", new Vector3(0f, 0f, 0.125f), new Vector3(1.50f, 1.02f, 0.035f), new Color(0.72f, 0.65f, 0.53f));

        GameObject esquerda = new GameObject("Metade_Esquerda_Da_Foto");
        esquerda.transform.SetParent(moldura.transform, false);
        esquerda.transform.localPosition = new Vector3(-0.37f, 0f, 0.16f);
        CriarCubo(esquerda.transform, "Imagem_Antiga", Vector3.zero, new Vector3(0.73f, 0.98f, 0.025f), new Color(0.71f, 0.64f, 0.52f));
        CriarPessoaNaFoto(esquerda.transform, -0.06f, 0.05f, new Color(0.36f, 0.32f, 0.28f), new Color(0.39f, 0.48f, 0.50f), 0.02f);

        GameObject direita = new GameObject("Metade_Direita_Da_Foto");
        direita.transform.SetParent(moldura.transform, false);
        direita.transform.localPosition = new Vector3(0.37f, 0f, 0.16f);
        CriarCubo(direita.transform, "Imagem_Antiga", Vector3.zero, new Vector3(0.73f, 0.98f, 0.025f), new Color(0.75f, 0.66f, 0.52f));
        CriarPessoaNaFoto(direita.transform, -0.20f, 0.07f, new Color(0.46f, 0.36f, 0.29f), new Color(0.63f, 0.51f, 0.39f), 0.05f);
        CriarPessoaNaFoto(direita.transform, 0.18f, -0.01f, new Color(0.38f, 0.34f, 0.30f), new Color(0.47f, 0.45f, 0.38f), 0.08f);

        CriarCubo(moldura.transform, "Linha_Da_Colagem", new Vector3(0f, 0f, 0.215f), new Vector3(0.025f, 1.04f, 0.018f), new Color(0.38f, 0.29f, 0.21f, 0.75f));
        metadeEsquerdaDaFoto = esquerda.transform;
        metadeDireitaDaFoto = direita.transform;
    }

    private void CriarPessoaNaFoto(Transform pai, float x, float y, Color corDoRosto, Color corDaRoupa, float z)
    {
        CriarEsfera(pai, "Rosto_Familiar", new Vector3(x, y + 0.23f, z + 0.06f), new Vector3(0.20f, 0.22f, 0.13f), corDoRosto);
        CriarCapsula(pai, "Ombros_Familiares", new Vector3(x, y - 0.19f, z + 0.04f), new Vector3(0.34f, 0.31f, 0.13f), Vector3.zero, corDaRoupa);
    }

    private void CriarRemedios(Transform pai)
    {
        Vector3 posicao = new Vector3(1.88f, 1.96f, 0.34f);
        CriarCubo(pai, "Caixa_De_Remédios", posicao, new Vector3(0.79f, 0.40f, 0.68f), new Color(0.72f, 0.71f, 0.62f));
        CriarCubo(pai, "Tampa_Da_Caixa", posicao + new Vector3(0f, 0.22f, 0f), new Vector3(0.80f, 0.07f, 0.69f), new Color(0.84f, 0.81f, 0.69f));
        CriarCapsula(pai, "Comprimido_Claro", new Vector3(2.58f, 1.90f, 0.50f), new Vector3(0.25f, 0.12f, 0.12f), new Vector3(0f, 0f, 90f), new Color(0.86f, 0.80f, 0.67f));
        CriarCapsula(pai, "Comprimido_Terracota", new Vector3(2.84f, 1.90f, 0.50f), new Vector3(0.23f, 0.12f, 0.12f), new Vector3(0f, 0f, 90f), new Color(0.65f, 0.38f, 0.28f));
    }

    private void CriarPlanta(Transform pai)
    {
        Vector3 basePlanta = new Vector3(3.77f, 2.08f, 0.35f);
        CriarCilindro(pai, "Vaso_Da_Planta", basePlanta, new Vector3(0.74f, 0.58f, 0.74f), Vector3.zero, new Color(0.55f, 0.34f, 0.25f));
        CriarCilindro(pai, "Borda_Do_Vaso", basePlanta + new Vector3(0f, 0.29f, 0f), new Vector3(0.84f, 0.09f, 0.84f), Vector3.zero, new Color(0.68f, 0.45f, 0.32f));
        CriarCilindro(pai, "Terra", basePlanta + new Vector3(0f, 0.34f, 0f), new Vector3(0.68f, 0.035f, 0.68f), Vector3.zero, new Color(0.27f, 0.19f, 0.13f));
        CriarCilindro(pai, "Caule", basePlanta + new Vector3(0f, 0.83f, 0f), new Vector3(0.055f, 0.55f, 0.055f), Vector3.zero, new Color(0.34f, 0.48f, 0.32f));

        CriarFolha3D(pai, basePlanta + new Vector3(-0.30f, 0.76f, 0.03f), new Vector3(0.62f, 0.20f, 0.20f), new Vector3(0f, 0f, -28f), new Color(0.43f, 0.57f, 0.37f));
        CriarFolha3D(pai, basePlanta + new Vector3(0.29f, 0.89f, 0.02f), new Vector3(0.58f, 0.21f, 0.20f), new Vector3(0f, 0f, 28f), new Color(0.48f, 0.60f, 0.39f));
        CriarFolha3D(pai, basePlanta + new Vector3(-0.23f, 1.16f, 0f), new Vector3(0.54f, 0.19f, 0.18f), new Vector3(0f, 0f, 25f), new Color(0.37f, 0.52f, 0.35f));
        CriarFolha3D(pai, basePlanta + new Vector3(0.25f, 1.27f, 0.01f), new Vector3(0.58f, 0.19f, 0.18f), new Vector3(0f, 0f, -27f), new Color(0.42f, 0.55f, 0.36f));
        CriarFolha3D(pai, basePlanta + new Vector3(0f, 1.48f, 0f), new Vector3(0.25f, 0.55f, 0.20f), new Vector3(0f, 0f, 0f), new Color(0.50f, 0.61f, 0.40f));
    }

    private void CriarFolha3D(Transform pai, Vector3 posicao, Vector3 tamanho, Vector3 rotacao, Color cor)
    {
        CriarEsfera(pai, "Folha_Da_Planta", posicao, tamanho, cor).transform.localRotation = Quaternion.Euler(rotacao);
    }

    private void CriarIluminacao(Transform pai)
    {
        GameObject sol = new GameObject("Luz_Da_Manha");
        sol.transform.SetParent(pai, false);
        sol.layer = CamadaDaIntro;
        sol.transform.rotation = Quaternion.Euler(42f, -34f, 0f);
        luzPrincipal = sol.AddComponent<Light>();
        luzPrincipal.type = LightType.Directional;
        luzPrincipal.color = new Color(1f, 0.83f, 0.65f);
        luzPrincipal.intensity = 1.05f;
        luzPrincipal.shadows = LightShadows.Soft;
        luzPrincipal.cullingMask = 1 << CamadaDaIntro;

        GameObject janela = new GameObject("Claridade_Da_Janela");
        janela.transform.SetParent(pai, false);
        janela.layer = CamadaDaIntro;
        janela.transform.position = new Vector3(-4.1f, 6.3f, 0.4f);
        luzDaJanela = janela.AddComponent<Light>();
        luzDaJanela.type = LightType.Point;
        luzDaJanela.color = new Color(1f, 0.79f, 0.55f);
        luzDaJanela.range = 9f;
        luzDaJanela.intensity = 1.8f;
        luzDaJanela.shadows = LightShadows.None;
        luzDaJanela.cullingMask = 1 << CamadaDaIntro;
    }

    private void CriarCameraCinematografica(Transform pai)
    {
        GameObject objetoCamera = new GameObject("Camera_Intro_RenderTexture", typeof(Camera));
        objetoCamera.transform.SetParent(pai, false);
        objetoCamera.layer = CamadaDaIntro;
        cameraDaCena = objetoCamera.GetComponent<Camera>();
        cameraTransform = objetoCamera.transform;
        cameraDaCena.clearFlags = CameraClearFlags.SolidColor;
        cameraDaCena.backgroundColor = new Color(0.13f, 0.115f, 0.095f, 1f);
        cameraDaCena.cullingMask = 1 << CamadaDaIntro;
        cameraDaCena.orthographic = true;
        cameraDaCena.orthographicSize = 5.2f;
        cameraDaCena.nearClipPlane = 0.1f;
        cameraDaCena.farClipPlane = 60f;
        cameraDaCena.allowHDR = false;
        cameraDaCena.allowMSAA = true;
        cameraDaCena.useOcclusionCulling = false;

        int altura = 900;
        float proporcao = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
        proporcao = Mathf.Clamp(proporcao, 1.25f, 2.4f);
        int largura = Mathf.RoundToInt(altura * proporcao);
        renderDaCena = new RenderTexture(largura, altura, 24, RenderTextureFormat.ARGB32)
        {
            name = "Render_Intro_Memorias_Partidas",
            antiAliasing = 2,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        renderDaCena.Create();
        cameraDaCena.targetTexture = renderDaCena;
        cameraDaCena.aspect = proporcao;
    }

    private void CriarAudioRelogio()
    {
        somDoRelogio = canvasDaIntro.AddComponent<AudioSource>();
        somDoRelogio.clip = Resources.Load<AudioClip>("Sons/AudioClip/Snd-Relogio");
        somDoRelogio.loop = true;
        somDoRelogio.playOnAwake = false;
        somDoRelogio.spatialBlend = 0f;
        somDoRelogio.dopplerLevel = 0f;
        somDoRelogio.pitch = 1f;
        volumeDoRelogio = Mathf.Clamp01(PlayerPrefs.GetFloat(ChaveVolumeSfx, 0.65f)) * 0.10f;
        somDoRelogio.volume = 0f;
    }

    private GameObject CriarCubo(Transform pai, string nome, Vector3 posicao, Vector3 escala, Color cor)
    {
        return CriarPrimitivo(pai, nome, PrimitiveType.Cube, posicao, escala, Vector3.zero, cor);
    }

    private GameObject CriarCilindro(Transform pai, string nome, Vector3 posicao, Vector3 escala, Vector3 rotacao, Color cor)
    {
        return CriarPrimitivo(pai, nome, PrimitiveType.Cylinder, posicao, escala, rotacao, cor);
    }

    private GameObject CriarEsfera(Transform pai, string nome, Vector3 posicao, Vector3 escala, Color cor)
    {
        return CriarPrimitivo(pai, nome, PrimitiveType.Sphere, posicao, escala, Vector3.zero, cor);
    }

    private GameObject CriarCapsula(Transform pai, string nome, Vector3 posicao, Vector3 escala, Vector3 rotacao, Color cor)
    {
        return CriarPrimitivo(pai, nome, PrimitiveType.Capsule, posicao, escala, rotacao, cor);
    }

    private GameObject CriarPrimitivo(Transform pai, string nome, PrimitiveType tipo, Vector3 posicao, Vector3 escala, Vector3 rotacao, Color cor)
    {
        GameObject objeto = GameObject.CreatePrimitive(tipo);
        objeto.name = nome;
        objeto.transform.SetParent(pai, false);
        objeto.transform.localPosition = posicao;
        objeto.transform.localRotation = Quaternion.Euler(rotacao);
        objeto.transform.localScale = escala;
        DefinirCamadaRecursiva(objeto.transform, CamadaDaIntro);

        Collider colisor = objeto.GetComponent<Collider>();
        if (colisor != null) Destroy(colisor);

        Renderer renderer = objeto.GetComponent<Renderer>();
        renderer.sharedMaterial = CriarMaterial(cor);
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        return objeto;
    }

    private void CriarMeshArgola(Transform pai, string nome, Vector3 posicao, float raio, float espessura, Color cor)
    {
        const int segmentos = 18;
        const int lados = 6;
        Vector3[] vertices = new Vector3[segmentos * lados];
        int[] triangulos = new int[segmentos * lados * 6];

        for (int i = 0; i < segmentos; i++)
        {
            float anguloPrincipal = i * Mathf.PI * 2f / segmentos;
            for (int j = 0; j < lados; j++)
            {
                float anguloSecundario = j * Mathf.PI * 2f / lados;
                float raioAtual = raio + Mathf.Cos(anguloSecundario) * espessura;
                vertices[i * lados + j] = new Vector3(
                    Mathf.Cos(anguloPrincipal) * raioAtual,
                    Mathf.Sin(anguloPrincipal) * raioAtual,
                    Mathf.Sin(anguloSecundario) * espessura);
            }
        }

        int indice = 0;
        for (int i = 0; i < segmentos; i++)
        {
            int proximoSegmento = (i + 1) % segmentos;
            for (int j = 0; j < lados; j++)
            {
                int proximoLado = (j + 1) % lados;
                int a = i * lados + j;
                int b = proximoSegmento * lados + j;
                int c = proximoSegmento * lados + proximoLado;
                int d = i * lados + proximoLado;
                triangulos[indice++] = a;
                triangulos[indice++] = b;
                triangulos[indice++] = c;
                triangulos[indice++] = a;
                triangulos[indice++] = c;
                triangulos[indice++] = d;
            }
        }

        Mesh malha = new Mesh { name = "Alca_LowPoly_Procedural" };
        malha.vertices = vertices;
        malha.triangles = triangulos;
        malha.RecalculateNormals();
        malhasGeradas.Add(malha);

        GameObject argola = new GameObject(nome, typeof(MeshFilter), typeof(MeshRenderer));
        argola.transform.SetParent(pai, false);
        argola.transform.localPosition = posicao;
        argola.transform.localRotation = Quaternion.identity;
        argola.transform.localScale = Vector3.one;
        argola.layer = CamadaDaIntro;
        argola.GetComponent<MeshFilter>().sharedMesh = malha;
        Renderer renderer = argola.GetComponent<Renderer>();
        renderer.sharedMaterial = CriarMaterial(cor);
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
    }

    private Material CriarMaterial(Color cor)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader)
        {
            name = "Material_Intro_Procedural",
            color = cor,
            hideFlags = HideFlags.HideAndDontSave
        };
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.08f);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.08f);
        materiaisGerados.Add(material);
        return material;
    }

    private void AtualizarFase(float tempo)
    {
        if (tempo < 5f)
        {
            fase = Fase.Despertar;
            progressoDaFase = Mathf.Clamp01(tempo / 5f);
        }
        else if (tempo < 13f)
        {
            fase = Fase.Rotina;
            progressoDaFase = Mathf.Clamp01((tempo - 5f) / 8f);
        }
        else if (tempo < 18.5f)
        {
            fase = Fase.Lapso;
            progressoDaFase = Mathf.Clamp01((tempo - 13f) / 5.5f);
        }
        else if (tempo < 22.5f)
        {
            fase = Fase.Reconstrucao;
            progressoDaFase = Mathf.Clamp01((tempo - 18.5f) / 4f);
        }
        else
        {
            fase = Fase.Titulo;
            progressoDaFase = Mathf.Clamp01((tempo - 22.5f) / 2.5f);
        }
    }

    private void AtualizarCamera(float tempo)
    {
        Vector3 focoRelogio = new Vector3(0.1f, 5.25f, -0.2f);
        Vector3 focoCasa = new Vector3(0f, 3.9f, 0f);
        Vector3 focoFoto = new Vector3(-0.15f, 2.72f, 0.35f);
        Vector3 focoFinal = new Vector3(0f, 3.55f, 0f);
        float tamanho;

        if (tempo < 5f)
        {
            float t = Suavizar(tempo / 5f);
            cameraTransform.position = Vector3.Lerp(focoRelogio, focoCasa, t) + new Vector3(0f, 2.4f, 14f);
            tamanho = Mathf.Lerp(3.45f, 5.2f, t);
        }
        else if (tempo < 13f)
        {
            float deriva = Mathf.Sin(tempo * 0.15f) * 0.10f;
            cameraTransform.position = (focoCasa + new Vector3(deriva, 0f, 0f)) + new Vector3(0f, 2.4f, 14f);
            tamanho = 5.2f;
        }
        else if (tempo < 18.5f)
        {
            float t = Suavizar((tempo - 13f) / 5.5f);
            Vector3 focoLapso = Vector3.Lerp(focoCasa, focoFoto + new Vector3(0.15f, 0.1f, 0f), t);
            focoLapso.x += Mathf.Sin(tempo * 0.62f) * 0.07f;
            cameraTransform.position = focoLapso + new Vector3(0f, 2.4f, 14f);
            tamanho = Mathf.Lerp(5.2f, 4.35f, t);
        }
        else if (tempo < 22.5f)
        {
            float t = Suavizar((tempo - 18.5f) / 4f);
            cameraTransform.position = Vector3.Lerp(focoFoto, focoFinal, t) + new Vector3(0f, 2.4f, 14f);
            tamanho = Mathf.Lerp(4.35f, 5.05f, t);
        }
        else
        {
            cameraTransform.position = focoFinal + new Vector3(0f, 2.4f, 14f);
            tamanho = 5.05f;
        }

        cameraTransform.LookAt(cameraTransform.position - new Vector3(0f, 2.4f, 14f));
        cameraDaCena.orthographicSize = tamanho;
    }

    private void AtualizarRelogio()
    {
        float velocidade = 1f;
        if (fase == Fase.Lapso)
            velocidade = Mathf.Lerp(1f, -0.12f, Suavizar(progressoDaFase));
        else if (fase == Fase.Reconstrucao)
            velocidade = Mathf.Lerp(-0.12f, 1f, Suavizar(progressoDaFase));
        else if (fase == Fase.Titulo)
            velocidade = 0.30f;

        anguloDosSegundos -= Time.unscaledDeltaTime * 6f * velocidade;
        anguloDosMinutos -= Time.unscaledDeltaTime * 0.10f * velocidade;
        ponteiroDosSegundos.localRotation = Quaternion.Euler(0f, 0f, anguloDosSegundos);
        ponteiroDosMinutos.localRotation = Quaternion.Euler(0f, 0f, anguloDosMinutos);
    }

    private void AtualizarColagem()
    {
        float separacao = 0f;
        if (fase == Fase.Lapso)
            separacao = Mathf.Lerp(0f, 0.22f, Suavizar(progressoDaFase));
        else if (fase == Fase.Reconstrucao)
            separacao = Mathf.Lerp(0.22f, 0.025f, Suavizar(progressoDaFase));
        else if (fase == Fase.Titulo)
            separacao = 0.025f;

        if (metadeEsquerdaDaFoto != null)
            metadeEsquerdaDaFoto.localPosition = new Vector3(-0.37f - separacao * 0.5f, 0f, 0.16f);
        if (metadeDireitaDaFoto != null)
            metadeDireitaDaFoto.localPosition = new Vector3(0.37f + separacao * 0.5f, 0f, 0.16f);
    }

    private void AtualizarLuzes()
    {
        float intensidade = 1f;
        float corFria = 0f;
        if (fase == Fase.Lapso)
        {
            float t = Suavizar(progressoDaFase);
            intensidade = Mathf.Lerp(1f, 0.68f, t);
            corFria = Mathf.Lerp(0f, 0.26f, t);
        }
        else if (fase == Fase.Reconstrucao)
        {
            float t = Suavizar(progressoDaFase);
            intensidade = Mathf.Lerp(0.68f, 1.17f, t);
            corFria = Mathf.Lerp(0.26f, 0f, t);
        }
        else if (fase == Fase.Titulo)
        {
            intensidade = 1.17f;
        }

        luzPrincipal.intensity = 1.05f * intensidade;
        luzDaJanela.intensity = 1.8f * intensidade;
        sobreposicaoDoLapso.color = new Color(0.40f, 0.46f, 0.48f, corFria);
    }

    private void AtualizarInterface(float tempo)
    {
        float fadeInicialAlpha = 1f - Suavizar(tempo / 5f);
        Color preto = fadeInicial.color;
        preto.a = fadeInicialAlpha;
        fadeInicial.color = preto;

        float entradaTitulo = Suavizar((tempo - 22.5f) / 2.5f);
        grupoDoTitulo.alpha = entradaTitulo;
        Color escuro = escurecimentoDoTitulo.color;
        escuro.a = 0.56f * entradaTitulo;
        escurecimentoDoTitulo.color = escuro;

        float escala = Mathf.Lerp(1.035f, 1f, entradaTitulo);
        retanguloDoTitulo.localScale = Vector3.one * escala;
        retanguloDoTitulo.anchoredPosition = new Vector2(0f, Mathf.Lerp(-24f, 0f, entradaTitulo));
        Color eco = textoTituloEco.color;
        eco.a = 0.22f * (1f - entradaTitulo) * entradaTitulo * 4f;
        textoTituloEco.color = eco;

        float fadeFinal = 1f - Suavizar((tempo - 29.4f) / 1.6f);
        grupoDaTela.alpha = fadeFinal;
    }

    private void AtualizarAudio(float tempo)
    {
        if (somDoRelogio == null || somDoRelogio.clip == null) return;

        if (tempo >= 1.4f && !somDoRelogio.isPlaying)
            somDoRelogio.Play();

        float envelope = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((tempo - 1.4f) / 1.6f));
        if (fase == Fase.Lapso)
            envelope *= 1f - Suavizar(progressoDaFase);
        else if (fase == Fase.Reconstrucao)
            envelope *= Mathf.Lerp(0f, 0.55f, Suavizar(progressoDaFase));
        else if (fase == Fase.Titulo)
            envelope *= 0.38f;

        somDoRelogio.volume = volumeDoRelogio * envelope;
        somDoRelogio.pitch = fase == Fase.Lapso
            ? Mathf.Lerp(1f, 0.76f, progressoDaFase)
            : fase == Fase.Reconstrucao
                ? Mathf.Lerp(0.76f, 0.96f, progressoDaFase)
                : 1f;
    }

    private void CriarRetangulo(Transform pai, string nome, Vector2 posicao, Vector2 tamanho, Color cor)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        objeto.transform.SetParent(pai, false);
        RectTransform retangulo = objeto.GetComponent<RectTransform>();
        retangulo.anchorMin = retangulo.anchorMax = new Vector2(0.5f, 0.5f);
        retangulo.pivot = new Vector2(0.5f, 0.5f);
        retangulo.sizeDelta = tamanho;
        retangulo.anchoredPosition = posicao;
        RawImage imagem = objeto.GetComponent<RawImage>();
        imagem.texture = Texture2D.whiteTexture;
        imagem.color = cor;
        imagem.raycastTarget = false;
    }

    private RawImage CriarImagemDeTela(Transform pai, string nome, Texture textura)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        objeto.transform.SetParent(pai, false);
        RectTransform retangulo = objeto.GetComponent<RectTransform>();
        EsticarNaTela(retangulo);
        RawImage imagem = objeto.GetComponent<RawImage>();
        imagem.texture = textura;
        imagem.color = Color.white;
        imagem.raycastTarget = false;
        return imagem;
    }

    private GameObject CriarContainer(Transform pai, string nome, Vector2 posicao, Vector2 tamanho)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform));
        objeto.transform.SetParent(pai, false);
        RectTransform retangulo = objeto.GetComponent<RectTransform>();
        retangulo.anchorMin = retangulo.anchorMax = new Vector2(0.5f, 0.5f);
        retangulo.pivot = new Vector2(0.5f, 0.5f);
        retangulo.sizeDelta = tamanho;
        retangulo.anchoredPosition = posicao;
        return objeto;
    }

    private TMP_Text CriarTexto(
        Transform pai,
        string nome,
        string conteudo,
        float tamanhoFonte,
        Vector2 posicao,
        Vector2 tamanho,
        FontStyles estilo,
        Color cor)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        objeto.transform.SetParent(pai, false);
        RectTransform retangulo = objeto.GetComponent<RectTransform>();
        retangulo.anchorMin = retangulo.anchorMax = new Vector2(0.5f, 0.5f);
        retangulo.pivot = new Vector2(0.5f, 0.5f);
        retangulo.sizeDelta = tamanho;
        retangulo.anchoredPosition = posicao;
        TMP_Text texto = objeto.GetComponent<TextMeshProUGUI>();
        texto.font = Resources.Load<TMP_FontAsset>("SlimUI/RUBIK-LIGHT-SDF");
        if (texto.font == null) texto.font = TMP_Settings.defaultFontAsset;
        texto.text = conteudo;
        texto.fontSize = tamanhoFonte;
        texto.fontStyle = estilo;
        texto.color = cor;
        texto.alignment = TextAlignmentOptions.Center;
        texto.textWrappingMode = TextWrappingModes.NoWrap;
        texto.raycastTarget = false;
        return texto;
    }

    private static void DefinirCamadaRecursiva(Transform raiz, int camada)
    {
        raiz.gameObject.layer = camada;
        for (int i = 0; i < raiz.childCount; i++)
            DefinirCamadaRecursiva(raiz.GetChild(i), camada);
    }

    private static void EsticarNaTela(RectTransform retangulo)
    {
        retangulo.anchorMin = Vector2.zero;
        retangulo.anchorMax = Vector2.one;
        retangulo.offsetMin = Vector2.zero;
        retangulo.offsetMax = Vector2.zero;
    }

    private static float Suavizar(float valor)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(valor));
    }

    private static float VolumeMusicaConfigurado()
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(ChaveVolumeMusica, 1f));
    }

    private IEnumerator FadeMusica(float volumeFinal, float duracao)
    {
        if (musica == null) yield break;

        float volumeInicial = musica.volume;
        float tempo = 0f;
        while (tempo < duracao && musica != null)
        {
            tempo += Time.unscaledDeltaTime;
            float progresso = Mathf.Clamp01(tempo / duracao);
            musica.volume = Mathf.Lerp(volumeInicial, volumeFinal, Mathf.SmoothStep(0f, 1f, progresso));
            yield return null;
        }
        if (musica != null) musica.volume = volumeFinal;
        fadeDeMusica = null;
    }

    private void ConcluirIntro()
    {
        if (concluida) return;
        concluida = true;
        fase = Fase.Concluida;

        if (fadeDeMusica != null)
        {
            StopCoroutine(fadeDeMusica);
            fadeDeMusica = null;
        }
        if (musica != null) musica.volume = VolumeMusicaConfigurado();
        if (somDoRelogio != null)
        {
            somDoRelogio.volume = 0f;
            somDoRelogio.Stop();
        }
        RestaurarMascaraDaCamera();

        Action callback = aoConcluir;
        aoConcluir = null;
        callback?.Invoke();

        if (canvasDaIntro != null)
        {
            Destroy(canvasDaIntro);
            canvasDaIntro = null;
        }
        if (raizDaCena != null)
        {
            Destroy(raizDaCena);
            raizDaCena = null;
        }
        Destroy(this);
    }

    private void OnDestroy()
    {
        RestaurarMascaraDaCamera();
        if (canvasDaIntro != null) Destroy(canvasDaIntro);
        if (raizDaCena != null) Destroy(raizDaCena);

        if (renderDaCena != null)
        {
            renderDaCena.Release();
            Destroy(renderDaCena);
        }
        for (int i = 0; i < materiaisGerados.Count; i++)
            if (materiaisGerados[i] != null) Destroy(materiaisGerados[i]);
        for (int i = 0; i < malhasGeradas.Count; i++)
            if (malhasGeradas[i] != null) Destroy(malhasGeradas[i]);
    }

    private void RestaurarMascaraDaCamera()
    {
        if (!mascaraOriginalSalva) return;
        if (cameraDoMenu != null) cameraDoMenu.cullingMask = mascaraOriginalDaCamera;
        mascaraOriginalSalva = false;
    }
}
