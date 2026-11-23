using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace SlimUI.ModernMenu{
	public class UIMenuManager : MonoBehaviour {
		private const float DuracaoMinimaCarregamento = 5f;
		private const float DuracaoMinimaNovoJogo = 10f;
		private const float ProgressoAposTempoDeEspera = 0.8f;
		private Animator CameraObject;
	private MainMenuPhotoSlideshow photoSlideshow;
		private bool carregamentoEmAndamento;
		private AudioSource musicaDoMenu;
		private bool diminuindoMusica;
		private const float DuracaoFadeMusica = 5f;
		private const string ChaveVolumeMusica = "MusicVolume";

		// campaign button sub menu
        [Header("MENUS")]
        [Tooltip("The Menu for when the MAIN menu buttons")]
        public GameObject mainMenu;
        [Tooltip("THe first list of buttons")]
        public GameObject firstMenu;
        [Tooltip("The Menu for when the PLAY button is clicked")]
        public GameObject playMenu;
        [Tooltip("The Menu for when the EXIT button is clicked")]
        public GameObject exitMenu;
        [Tooltip("Optional 4th Menu")]
        public GameObject extrasMenu;

        public enum Theme {custom1, custom2, custom3};
        [Header("THEME SETTINGS")]
        public Theme theme;
        public ThemedUIData themeController;

        [Header("PANELS")]
        [Tooltip("The UI Panel parenting all sub menus")]
        public GameObject mainCanvas;
        [Tooltip("The UI Panel that holds the CONTROLS window tab")]
        public GameObject PanelControls;
        [Tooltip("The UI Panel that holds the VIDEO window tab")]
        public GameObject PanelVideo;
        [Tooltip("The UI Panel that holds the GAME window tab")]
        public GameObject PanelGame;
        [Tooltip("The UI Panel that holds the KEY BINDINGS window tab")]
        public GameObject PanelKeyBindings;
        [Tooltip("The UI Sub-Panel under KEY BINDINGS for MOVEMENT")]
        public GameObject PanelMovement;
        [Tooltip("The UI Sub-Panel under KEY BINDINGS for COMBAT")]
        public GameObject PanelCombat;
        [Tooltip("The UI Sub-Panel under KEY BINDINGS for GENERAL")]
        public GameObject PanelGeneral;
        

        // highlights in settings screen
        [Header("SETTINGS SCREEN")]
        [Tooltip("Highlight Image for when GAME Tab is selected in Settings")]
        public GameObject lineGame;
        [Tooltip("Highlight Image for when VIDEO Tab is selected in Settings")]
        public GameObject lineVideo;
        [Tooltip("Highlight Image for when CONTROLS Tab is selected in Settings")]
        public GameObject lineControls;
        [Tooltip("Highlight Image for when KEY BINDINGS Tab is selected in Settings")]
        public GameObject lineKeyBindings;
        [Tooltip("Highlight Image for when MOVEMENT Sub-Tab is selected in KEY BINDINGS")]
        public GameObject lineMovement;
        [Tooltip("Highlight Image for when COMBAT Sub-Tab is selected in KEY BINDINGS")]
        public GameObject lineCombat;
        [Tooltip("Highlight Image for when GENERAL Sub-Tab is selected in KEY BINDINGS")]
        public GameObject lineGeneral;

        [Header("LOADING SCREEN")]
		[Tooltip("If this is true, the loaded scene won't load until receiving user input")]
		public bool waitForInput = true;
        public GameObject loadingMenu;
		[Tooltip("The loading bar Slider UI element in the Loading Screen")]
        public Slider loadingBar;
        public TMP_Text loadPromptText;
		public KeyCode userPromptKey;

		[Header("SFX")]
        [Tooltip("The GameObject holding the Audio Source component for the HOVER SOUND")]
        public AudioSource hoverSound;
        [Tooltip("The GameObject holding the Audio Source component for the AUDIO SLIDER")]
        public AudioSource sliderSound;
        [Tooltip("The GameObject holding the Audio Source component for the SWOOSH SOUND when switching to the Settings Screen")]
        public AudioSource swooshSound;

		void Start(){
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
			IniciarMusicaDoMenu();

			CameraObject = transform.GetComponent<Animator>();

			playMenu.SetActive(false);
			exitMenu.SetActive(false);
			if(extrasMenu) extrasMenu.SetActive(false);
			firstMenu.SetActive(true);
			mainMenu.SetActive(true);
			ConfigurarNavegacaoDoMenu();

			ConfigurarBotoesDeSave();
			SetThemeColors();
			// O objeto referenciado por mainMenu pode ser a raiz Canvas_DefaultTemplate1
			// no Demo1. Não amarre a criação do fundo a um nome específico de GameObject.
			if(mainMenu != null && Camera.main != null){
				GameObject slideshow = new GameObject("Main_Menu_PhotoSlideshow");
				photoSlideshow = slideshow.AddComponent<MainMenuPhotoSlideshow>();
				Canvas canvasMenu = mainMenu.GetComponentInParent<Canvas>();
				if(canvasMenu == null)
					canvasMenu = mainMenu.GetComponentInChildren<Canvas>(true);
				photoSlideshow.Inicializar(Camera.main, canvasMenu);
				photoSlideshow.DefinirContrasteAtivo(true);
				AplicarLegibilidadeMenuPrincipal();
			}
		}

		private void IniciarMusicaDoMenu(){
			AudioClip faixa = Resources.Load<AudioClip>("Music/the_mountain-sad-512361");
			if(faixa == null){
				Debug.LogWarning("Não foi possível carregar a música do menu em Resources/Music.", this);
				return;
			}

			musicaDoMenu = gameObject.AddComponent<AudioSource>();
			musicaDoMenu.clip = faixa;
			musicaDoMenu.loop = true;
			musicaDoMenu.playOnAwake = false;
			musicaDoMenu.volume = PlayerPrefs.GetFloat(ChaveVolumeMusica, 1f);
			musicaDoMenu.Play();
		}

		private void Update(){
			ControleCompatibilidade.AtualizarNavegacaoUI();

			bool voltar = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
			voltar |= ControleCompatibilidade.AlgumBotaoXboxBPressionado();
			voltar |= ControleCompatibilidade.BotaoJoystickPressionado(1);
			if(voltar && (playMenu.activeSelf || exitMenu.activeSelf || (extrasMenu && extrasMenu.activeSelf)))
				ReturnMenu();

			if(musicaDoMenu != null && !diminuindoMusica)
				musicaDoMenu.volume = PlayerPrefs.GetFloat(ChaveVolumeMusica, 1f);
		}

		private void ConfigurarNavegacaoDoMenu(){
			EventSystem[] eventSystems = FindObjectsByType<EventSystem>();
			foreach(EventSystem sistema in eventSystems){
				sistema.sendNavigationEvents = true;
				ControleCompatibilidade.GarantirModuloUI(sistema);
			}

			ConfigurarNavegacao(mainCanvas);
			ConfigurarNavegacao(mainMenu);
			ConfigurarNavegacao(firstMenu);
			ConfigurarNavegacao(playMenu);
			ConfigurarNavegacao(exitMenu);
			ConfigurarNavegacao(extrasMenu);
			SelecionarPrimeiroBotao(firstMenu != null ? firstMenu : mainMenu);
		}

		private static void ConfigurarNavegacao(GameObject raiz){
			if(raiz == null) return;
			foreach(Button botao in raiz.GetComponentsInChildren<Button>(true)){
				Navigation navegacao = botao.navigation;
				if(navegacao.mode == Navigation.Mode.None){
					navegacao.mode = Navigation.Mode.Automatic;
					botao.navigation = navegacao;
				}
			}
		}

		private static void SelecionarPrimeiroBotao(GameObject raiz){
			EventSystem sistema = EventSystem.current;
			if(sistema == null || raiz == null) return;

			foreach(Button botao in raiz.GetComponentsInChildren<Button>(true)){
				if(botao == null || !botao.isActiveAndEnabled || !botao.interactable) continue;
				Navigation navegacao = botao.navigation;
				if(navegacao.mode == Navigation.Mode.None){
					navegacao.mode = Navigation.Mode.Automatic;
					botao.navigation = navegacao;
				}
				sistema.firstSelectedGameObject = botao.gameObject;
				sistema.SetSelectedGameObject(botao.gameObject);
				return;
			}
		}

		private IEnumerator DiminuirMusicaAteSilencio(){
			diminuindoMusica = true;
			float volumeInicial = musicaDoMenu != null ? musicaDoMenu.volume : 0f;
			float tempo = 0f;
			while(tempo < DuracaoFadeMusica && musicaDoMenu != null){
				tempo += Time.unscaledDeltaTime;
				musicaDoMenu.volume = Mathf.Lerp(volumeInicial, 0f, Mathf.Clamp01(tempo / DuracaoFadeMusica));
				yield return null;
			}
			if(musicaDoMenu != null){
				musicaDoMenu.volume = 0f;
				musicaDoMenu.Stop();
			}
		}

		private void IniciarFadeMusica(){
			if(musicaDoMenu != null && !diminuindoMusica)
				StartCoroutine(DiminuirMusicaAteSilencio());
		}

		private void AplicarLegibilidadeMenuPrincipal(){
			if(mainMenu == null) return;

			Button[] botoes = mainMenu.GetComponentsInChildren<Button>(true);
			foreach(Button botao in botoes){
				TMP_Text rotulo = botao.GetComponentInChildren<TMP_Text>(true);
				if(rotulo != null){
					rotulo.color = Color.white;
					rotulo.fontSize = Mathf.Max(rotulo.fontSize, 30f);
					rotulo.fontStyle |= FontStyles.Bold;
					rotulo.enableAutoSizing = false;
				}

				ColorBlock cores = botao.colors;
				cores.highlightedColor = new Color(1f, 0.84f, 0.54f, 1f);
				cores.selectedColor = cores.highlightedColor;
				cores.pressedColor = new Color(1f, 0.65f, 0.24f, 1f);
				cores.fadeDuration = 0.12f;
				botao.colors = cores;
			}
		}

		private void DefinirContrasteDoFundo(bool ativo){
			if(photoSlideshow != null) photoSlideshow.DefinirContrasteAtivo(ativo);
		}

		private void ConfigurarBotoesDeSave(){
			if(playMenu == null) return;
			Button[] botoes = playMenu.GetComponentsInChildren<Button>(true);
			foreach(Button botao in botoes){
				TMP_Text rotulo = botao.GetComponentInChildren<TMP_Text>(true);
				if(rotulo == null) continue;
				string texto = rotulo.text.Trim().ToLowerInvariant();
				if(texto == "continuar" || texto == "carregar jogo"){
					botao.onClick.RemoveAllListeners();
					botao.onClick.AddListener(() => LoadScene("Nivel1"));
				}else if(texto == "novo jogo"){
					botao.onClick.RemoveAllListeners();
					botao.onClick.AddListener(() => NewGame("Nivel1"));
				}
			}
		}

		void SetThemeColors()
		{
			switch (theme)
			{
				case Theme.custom1:
					themeController.currentColor = themeController.custom1.graphic1;
					themeController.textColor = themeController.custom1.text1;
					break;
				case Theme.custom2:
					themeController.currentColor = themeController.custom2.graphic2;
					themeController.textColor = themeController.custom2.text2;
					break;
				case Theme.custom3:
					themeController.currentColor = themeController.custom3.graphic3;
					themeController.textColor = themeController.custom3.text3;
					break;
				default:
					Debug.Log("Invalid theme selected.");
					break;
			}
		}

		public void PlayCampaign(){
			DefinirContrasteDoFundo(false);
			exitMenu.SetActive(false);
			if(extrasMenu) extrasMenu.SetActive(false);
			playMenu.SetActive(true);
			SelecionarPrimeiroBotao(playMenu);
		}
		
		public void PlayCampaignMobile(){
			DefinirContrasteDoFundo(false);
			exitMenu.SetActive(false);
			if(extrasMenu) extrasMenu.SetActive(false);
			playMenu.SetActive(true);
			mainMenu.SetActive(false);
			SelecionarPrimeiroBotao(playMenu);
		}

		public void ReturnMenu(){
			DefinirContrasteDoFundo(true);
			playMenu.SetActive(false);
			if(extrasMenu) extrasMenu.SetActive(false);
			exitMenu.SetActive(false);
			mainMenu.SetActive(true);
			if(firstMenu != null) firstMenu.SetActive(true);
			SelecionarPrimeiroBotao(firstMenu != null ? firstMenu : mainMenu);
		}

		public void LoadScene(string scene){
			SaveGameDatabase.SaveData save = SaveGameDatabase.Load();
			if (save != null && !string.IsNullOrWhiteSpace(save.sceneName)
				&& Application.CanStreamedLevelBeLoaded(save.sceneName))
			{
				IniciarFadeMusica();
				IniciarCarregamentoAssincrono(save.sceneName);
				return;
			}

			if (string.IsNullOrWhiteSpace(scene) || !Application.CanStreamedLevelBeLoaded(scene))
			{
				Debug.LogError("Não foi possível carregar o jogo: não há um save válido e a cena inicial não está nas Build Settings.");
				return;
			}
			IniciarFadeMusica();
			IniciarCarregamentoAssincrono(scene);
		}

		public void NewGame(string scene){
			SaveGameDatabase.Delete();
			if (string.IsNullOrWhiteSpace(scene) || !Application.CanStreamedLevelBeLoaded(scene))
			{
				Debug.LogError("Não foi possível iniciar um novo jogo: a cena inicial não está nas Build Settings.");
				return;
			}
			IniciarFadeMusica();
			IniciarCarregamentoAssincrono(scene, DuracaoMinimaNovoJogo);
		}

		private void IniciarCarregamentoAssincrono(string sceneName,
			float duracaoMinima = DuracaoMinimaCarregamento){
			if(carregamentoEmAndamento) return;
			carregamentoEmAndamento = true;
			StartCoroutine(LoadAsynchronously(sceneName, duracaoMinima));
		}

		public void  DisablePlayCampaign(){
			playMenu.SetActive(false);
		}

		public void Position2(){
			DefinirContrasteDoFundo(false);
			DisablePlayCampaign();
			CameraObject.SetFloat("Animate",1);
		}

		public void Position1(){
			DefinirContrasteDoFundo(true);
			CameraObject.SetFloat("Animate",0);
		}

		void DisablePanels(){
			DefinirContrasteDoFundo(false);
			PanelControls.SetActive(false);
			PanelVideo.SetActive(false);
			PanelGame.SetActive(false);
			PanelKeyBindings.SetActive(false);

			lineGame.SetActive(false);
			lineControls.SetActive(false);
			lineVideo.SetActive(false);
			lineKeyBindings.SetActive(false);

			PanelMovement.SetActive(false);
			lineMovement.SetActive(false);
			PanelCombat.SetActive(false);
			lineCombat.SetActive(false);
			PanelGeneral.SetActive(false);
			lineGeneral.SetActive(false);
		}

		public void GamePanel(){
			DisablePanels();
			PanelGame.SetActive(true);
			lineGame.SetActive(true);
		}

		public void VideoPanel(){
			DisablePanels();
			PanelVideo.SetActive(true);
			lineVideo.SetActive(true);
		}

		public void ControlsPanel(){
			DisablePanels();
			PanelControls.SetActive(true);
			lineControls.SetActive(true);
		}

		public void KeyBindingsPanel(){
			DisablePanels();
			MovementPanel();
			PanelKeyBindings.SetActive(true);
			lineKeyBindings.SetActive(true);
		}

		public void MovementPanel(){
			DisablePanels();
			PanelKeyBindings.SetActive(true);
			PanelMovement.SetActive(true);
			lineMovement.SetActive(true);
		}

		public void CombatPanel(){
			DisablePanels();
			PanelKeyBindings.SetActive(true);
			PanelCombat.SetActive(true);
			lineCombat.SetActive(true);
		}

		public void GeneralPanel(){
			DisablePanels();
			PanelKeyBindings.SetActive(true);
			PanelGeneral.SetActive(true);
			lineGeneral.SetActive(true);
		}

		public void PlayHover(){
			hoverSound.Play();
		}

		public void PlaySFXHover(){
			sliderSound.Play();
		}

		public void PlaySwoosh(){
			swooshSound.Play();
		}

		// Are You Sure - Quit Panel Pop Up
		public void AreYouSure(){
			DefinirContrasteDoFundo(false);
			exitMenu.SetActive(true);
			if(extrasMenu) extrasMenu.SetActive(false);
			DisablePlayCampaign();
			SelecionarPrimeiroBotao(exitMenu);
		}

		public void AreYouSureMobile(){
			DefinirContrasteDoFundo(false);
			exitMenu.SetActive(true);
			if(extrasMenu) extrasMenu.SetActive(false);
			mainMenu.SetActive(false);
			DisablePlayCampaign();
			SelecionarPrimeiroBotao(exitMenu);
		}

		public void ExtrasMenu(){
			DefinirContrasteDoFundo(false);
			playMenu.SetActive(false);
			if(extrasMenu) extrasMenu.SetActive(true);
			exitMenu.SetActive(false);
			if(extrasMenu) SelecionarPrimeiroBotao(extrasMenu);
		}

		public void QuitGame(){
			#if UNITY_EDITOR
				UnityEditor.EditorApplication.isPlaying = false;
			#else
				Application.Quit();
			#endif
		}

		// Load Bar synching animation
		IEnumerator LoadAsynchronously(string sceneName, float duracaoMinima){
			float escalaTempoAnterior = Time.timeScale;
			Time.timeScale = 0f;

			if(mainCanvas != null) mainCanvas.SetActive(false);
			if(loadingMenu != null){
				loadingMenu.SetActive(true);
				CriarPainelControlesCarregamento();
				Canvas loadingCanvas = loadingMenu.GetComponent<Canvas>();
				if(loadingCanvas != null){
					loadingCanvas.overrideSorting = true;
					loadingCanvas.sortingOrder = 100;
				}
			}
			// TextPrompt fica à esquerda. O título CARREGANDO à direita é o único
			// texto de status exibido durante o carregamento.
			if(loadPromptText != null) loadPromptText.gameObject.SetActive(false);
			TMP_Text textoCarregando = loadingMenu != null
				? loadingMenu.transform.Find("LoadingText")?.GetComponent<TMP_Text>()
				: null;
			if(textoCarregando != null){
				textoCarregando.gameObject.SetActive(true);
				textoCarregando.text = "CARREGANDO";
			}
			if(loadingBar != null) loadingBar.value = 0f;
			AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
			if(operation == null){
				Debug.LogError("Não foi possível iniciar o carregamento da cena: " + sceneName);
				Time.timeScale = escalaTempoAnterior;
				if(loadingMenu != null) loadingMenu.SetActive(false);
				if(mainCanvas != null) mainCanvas.SetActive(true);
				carregamentoEmAndamento = false;
				yield break;
			}

			operation.allowSceneActivation = false;
			float inicioCarregamento = Time.unscaledTime;
			while(Time.unscaledTime - inicioCarregamento < duracaoMinima
				|| operation.progress < 0.9f){
				float tempoDecorrido = Time.unscaledTime - inicioCarregamento;
				if(loadingBar != null){
					if(tempoDecorrido < duracaoMinima){
						float progressoDoTempo = Mathf.Clamp01(
							tempoDecorrido / duracaoMinima);
						loadingBar.value = progressoDoTempo * ProgressoAposTempoDeEspera;
					}else{
						float progressoDaCena = Mathf.Clamp01(operation.progress / 0.9f);
						loadingBar.value = Mathf.Lerp(
							ProgressoAposTempoDeEspera, 1f, progressoDaCena);
					}
				}
				if(textoCarregando != null){
					int quantidadePontos = (int)(Time.unscaledTime * 2f) % 4;
					textoCarregando.text = "CARREGANDO" + new string('.', quantidadePontos);
				}
				yield return null;
			}

			if(loadingBar != null) loadingBar.value = 1f;
			// O avanço é automático; não exige Enter ou outra tecla.
			yield return null;

			// Retoma o tempo somente no fim da espera, antes de ativar a próxima cena.
			Time.timeScale = escalaTempoAnterior;
			operation.allowSceneActivation = true;
			while(!operation.isDone) yield return null;
		}

		private void CriarPainelControlesCarregamento(){
			if(loadingMenu == null || loadingMenu.transform.Find("Cartao_Controles") != null) return;

			TMP_FontAsset fonte = loadPromptText != null
				? loadPromptText.font
				: TMP_Settings.defaultFontAsset;

			RawImage cartao = CriarImagemUI(
				loadingMenu.transform,
				"Cartao_Controles",
				new Color(0.025f, 0.038f, 0.060f, 0.94f));
			RectTransform retanguloCartao = cartao.rectTransform;
			retanguloCartao.anchorMin = new Vector2(0.045f, 0.26f);
			retanguloCartao.anchorMax = new Vector2(0.525f, 0.84f);
			retanguloCartao.offsetMin = Vector2.zero;
			retanguloCartao.offsetMax = Vector2.zero;
			UnityEngine.UI.Outline contornoCartao =
				cartao.gameObject.AddComponent<UnityEngine.UI.Outline>();
			contornoCartao.effectColor = new Color(1f, 0.65f, 0.16f, 0.82f);
			contornoCartao.effectDistance = new Vector2(2f, 2f);

			RawImage faixaDestaque = CriarImagemUI(
				cartao.transform,
				"Faixa_Destaque",
				new Color(1f, 0.65f, 0.16f, 1f));
			DefinirRetangulo(faixaDestaque.rectTransform,
				new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
				Vector2.zero, new Vector2(0f, 5f));

			CriarTextoUI(cartao.transform, "Titulo_Controles", "CONTROLES DO JOGO",
				fonte, 32f, FontStyles.Bold, new Color(1f, 0.72f, 0.30f, 1f),
				TextAlignmentOptions.Center,
				new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
				new Vector2(0f, -24f), new Vector2(-48f, 44f));
			CriarTextoUI(cartao.transform, "Subtitulo_Controles", "TECLADO E MOUSE",
				fonte, 17f, FontStyles.Bold, new Color(0.70f, 0.76f, 0.84f, 1f),
				TextAlignmentOptions.Center,
				new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
				new Vector2(0f, -70f), new Vector2(-48f, 28f));

			CriarLinhaDeControle(cartao.transform, fonte, 0,
				new[] { "W", "A", "S", "D" }, "Mover o personagem");
			CriarLinhaDeControle(cartao.transform, fonte, 1,
				new[] { "MOUSE" }, "Olhar ao redor");
			CriarLinhaDeControle(cartao.transform, fonte, 2,
				new[] { "CLIQUE ESQ." }, "Interagir, pegar e devolver itens");
			CriarLinhaDeControle(cartao.transform, fonte, 3,
				new[] { "ESC" }, "Abrir ou fechar o menu de pausa");
			CriarLinhaDeControle(cartao.transform, fonte, 4,
				new[] { "1", "2" }, "Escolher uma resposta na conversa");
		}

		private void CriarLinhaDeControle(Transform pai, TMP_FontAsset fonte,
			int indice, string[] teclas, string descricao){
			RawImage linha = CriarImagemUI(pai, "Linha_Controle_" + indice,
				new Color(1f, 1f, 1f, 0.045f));
			DefinirRetangulo(linha.rectTransform,
				new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
				new Vector2(0f, -122f - indice * 80f), new Vector2(-44f, 62f));

			float deslocamentoTecla = 18f;
			foreach(string tecla in teclas){
				float largura = tecla.Length >= 9 ? 172f : tecla.Length >= 5 ? 126f : 46f;
				RawImage teclaImagem = CriarImagemUI(linha.transform,
					"Tecla_" + tecla.Replace(" ", "_"), new Color(0.11f, 0.15f, 0.21f, 1f));
				DefinirRetangulo(teclaImagem.rectTransform,
					new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
					new Vector2(deslocamentoTecla, 0f), new Vector2(largura, 44f));
				UnityEngine.UI.Outline contornoTecla =
					teclaImagem.gameObject.AddComponent<UnityEngine.UI.Outline>();
				contornoTecla.effectColor = new Color(1f, 0.65f, 0.16f, 0.9f);
				contornoTecla.effectDistance = new Vector2(1f, 1f);

				CriarTextoUI(teclaImagem.transform, "Rotulo", tecla, fonte,
					tecla.Length >= 9 ? 15f : 18f, FontStyles.Bold, Color.white,
					TextAlignmentOptions.Center,
					Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
					Vector2.zero, Vector2.zero);
				deslocamentoTecla += largura + 8f;
			}

			CriarTextoUI(linha.transform, "Descricao", descricao, fonte,
				19f, FontStyles.Normal, new Color(0.94f, 0.95f, 0.98f, 1f),
				TextAlignmentOptions.MidlineLeft,
				new Vector2(0.34f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f),
				new Vector2(6f, 0f), new Vector2(-12f, 0f));
		}

		private static RawImage CriarImagemUI(Transform pai, string nome, Color cor){
			GameObject objeto = new GameObject(nome,
				typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
			objeto.transform.SetParent(pai, false);
			RawImage imagem = objeto.GetComponent<RawImage>();
			imagem.texture = Texture2D.whiteTexture;
			imagem.color = cor;
			imagem.raycastTarget = false;
			return imagem;
		}

		private static TMP_Text CriarTextoUI(Transform pai, string nome, string conteudo,
			TMP_FontAsset fonte, float tamanho, FontStyles estilo, Color cor,
			TextAlignmentOptions alinhamento, Vector2 ancoraMin, Vector2 ancoraMax,
			Vector2 pivo, Vector2 posicao, Vector2 tamanhoRetangulo){
			GameObject objeto = new GameObject(nome,
				typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
			objeto.transform.SetParent(pai, false);
			TMP_Text texto = objeto.GetComponent<TextMeshProUGUI>();
			texto.text = conteudo;
			texto.font = fonte;
			texto.fontSize = tamanho;
			texto.fontStyle = estilo;
			texto.color = cor;
			texto.alignment = alinhamento;
			texto.enableAutoSizing = false;
			texto.raycastTarget = false;
			DefinirRetangulo(texto.rectTransform, ancoraMin, ancoraMax, pivo, posicao, tamanhoRetangulo);
			return texto;
		}

		private static void DefinirRetangulo(RectTransform retangulo, Vector2 ancoraMin,
			Vector2 ancoraMax, Vector2 pivo, Vector2 posicao, Vector2 tamanho){
			retangulo.anchorMin = ancoraMin;
			retangulo.anchorMax = ancoraMax;
			retangulo.pivot = pivo;
			retangulo.anchoredPosition = posicao;
			retangulo.sizeDelta = tamanho;
		}
	}
}
