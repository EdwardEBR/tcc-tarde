using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace SlimUI.ModernMenu
{
	public class UISettingsManager : MonoBehaviour
	{
		public enum Platform { Desktop, Mobile }
		public Platform platform;

		[Header("MOBILE SETTINGS")]
		public GameObject mobileSFXtext;
		public GameObject mobileMusictext;
		public GameObject mobileShadowofftextLINE;
		public GameObject mobileShadowlowtextLINE;
		public GameObject mobileShadowhightextLINE;

		[Header("VIDEO SETTINGS")]
		public GameObject fullscreentext;
		public GameObject ambientocclusiontext;
		public GameObject shadowofftextLINE;
		public GameObject shadowlowtextLINE;
		public GameObject shadowhightextLINE;
		public GameObject aaofftextLINE;
		public GameObject aa2xtextLINE;
		public GameObject aa4xtextLINE;
		public GameObject aa8xtextLINE;
		public GameObject vsynctext;
		public GameObject motionblurtext;
		public GameObject texturelowtextLINE;
		public GameObject texturemedtextLINE;
		public GameObject texturehightextLINE;
		public GameObject cameraeffectstext;

		[Header("GAME SETTINGS")]
		public GameObject showhudtext;
		public GameObject tooltipstext;
		public GameObject difficultynormaltext;
		public GameObject difficultynormaltextLINE;
		public GameObject difficultyhardcoretext;
		public GameObject difficultyhardcoretextLINE;

		[Header("CONTROLS SETTINGS")]
		public GameObject invertmousetext;

		public GameObject musicSlider;
		public GameObject sensitivityXSlider;
		public GameObject sensitivityYSlider;
		public GameObject mouseSmoothSlider;

		private const string ChaveVolumeMusica = "MusicVolume";
		private const string ChaveVolumeGeral = "MasterVolume";
		private const string ChaveSensibilidadeMouse = "XSensitivity";
		private const string ChaveSensibilidadeControle = "ControllerSensitivity";
		private const string ChaveSuavidadeOlhar = "MouseSmoothing";
		private const string ChaveCampoVisao = "FieldOfView";
		private Slider sliderVolumeGeral;
		private Slider sliderCampoVisao;
		private PlayerFPS jogador;

		private void Start()
		{
			jogador = FindAnyObjectByType<PlayerFPS>();
			AplicarPerfilDeQualidadeSalvo();
			ConfigurarControlesDeAudio();
			ConfigurarControlesDeOlhar();
			if (PlayerPrefs.HasKey("FullScreen"))
				Screen.fullScreen = PlayerPrefs.GetInt("FullScreen") == 1;
			AtualizarEstadoTelaCheia();
			if (PlayerPrefs.HasKey("VSync"))
				QualitySettings.vSyncCount = Mathf.Clamp(PlayerPrefs.GetInt("VSync"), 0, 2);
			string chaveSombras = platform == Platform.Mobile ? "MobileShadows" : "Shadows";
			ConfigurarSombras(PlayerPrefs.GetInt(chaveSombras, 1));
			ConfigurarTexturas(PlayerPrefs.GetInt("Textures", 2));
			AtualizarEstadoVSync();
			AtualizarEstadoInversao();
			ConfigurarLayoutDaTelaExistente();
		}

		private void ConfigurarControlesDeAudio()
		{
			Slider sliderMusica = ObterSlider(musicSlider);
			if (sliderMusica != null)
			{
				ConfigurarSlider(sliderMusica, 0f, 1f, PlayerPrefs.GetFloat(ChaveVolumeMusica, 1f));
				DefinirPosicaoY(sliderMusica.gameObject, 184f);
			}

			sliderVolumeGeral = CriarSliderDeVolumeGeral();
			if (sliderVolumeGeral != null)
			{
				ConfigurarSlider(sliderVolumeGeral, 0f, 1f, PlayerPrefs.GetFloat(ChaveVolumeGeral, AudioListener.volume));
				DefinirPosicaoY(sliderVolumeGeral.gameObject, 117f);
				sliderVolumeGeral.onValueChanged.AddListener(AtualizarVolumeGeral);
				AudioListener.volume = sliderVolumeGeral.value;
			}
		}

		private Slider CriarSliderDeVolumeGeral()
		{
			if (musicSlider == null) return null;

			Transform painel = musicSlider.transform.parent;
			if (painel == null) return null;

			Transform existente = painel.Find("MasterVolumeSlider");
			GameObject objetoSlider = existente != null
				? existente.gameObject
				: Instantiate(musicSlider, painel, false);
			objetoSlider.name = "MasterVolumeSlider";

			EventTrigger gatilho = objetoSlider.GetComponent<EventTrigger>();
			if (gatilho != null) Destroy(gatilho);

			Slider slider = objetoSlider.GetComponent<Slider>();
			if (slider == null) return null;
			DesativarCallbacksPersistentes(slider);
			return slider;
		}

		private void ConfigurarControlesDeOlhar()
		{
			float sensibilidadeMouse = PlayerPrefs.GetFloat(
				ChaveSensibilidadeMouse,
				jogador != null ? jogador.sensibilidade : 0.15f);
			if (!ValorFinito(sensibilidadeMouse) || sensibilidadeMouse <= 0.01f) sensibilidadeMouse = 0.15f;

			float sensibilidadeControle = PlayerPrefs.GetFloat(
				ChaveSensibilidadeControle,
				jogador != null ? jogador.sensibilidadeControle : 140f);
			float suavidade = PlayerPrefs.GetFloat(
				ChaveSuavidadeOlhar,
				jogador != null ? jogador.suavidadeOlhar : 0.08f);

			ConfigurarSlider(ObterSlider(sensitivityXSlider), 0.05f, 1f, sensibilidadeMouse);
			ConfigurarSlider(ObterSlider(sensitivityYSlider), 40f, 320f, sensibilidadeControle);
			ConfigurarSlider(ObterSlider(mouseSmoothSlider), 0f, 0.3f, suavidade);
		}

		private static void ConfigurarSlider(Slider slider, float minimo, float maximo, float valor)
		{
			if (slider == null) return;

			UnityEventCallState[] estados = new UnityEventCallState[slider.onValueChanged.GetPersistentEventCount()];
			for (int i = 0; i < estados.Length; i++)
			{
				estados[i] = slider.onValueChanged.GetPersistentListenerState(i);
				slider.onValueChanged.SetPersistentListenerState(i, UnityEventCallState.Off);
			}

			slider.minValue = minimo;
			slider.maxValue = maximo;
			slider.SetValueWithoutNotify(Mathf.Clamp(valor, minimo, maximo));

			for (int i = 0; i < estados.Length; i++)
				slider.onValueChanged.SetPersistentListenerState(i, estados[i]);
		}

		private static void DesativarCallbacksPersistentes(Slider slider)
		{
			for (int i = 0; i < slider.onValueChanged.GetPersistentEventCount(); i++)
				slider.onValueChanged.SetPersistentListenerState(i, UnityEventCallState.Off);
		}

		private static Slider ObterSlider(GameObject objeto)
		{
			return objeto != null ? objeto.GetComponent<Slider>() : null;
		}

		private void ConfigurarLayoutDaTelaExistente()
		{
			// Aproveita a linha de HUD que não existe no jogo para o novo controle de volume geral.
			OcultarObjeto("ShowHud_Btn");
			OcultarObjeto("ToolTip_Btn");
			OcultarObjeto("NormDif_Btn");
			OcultarObjeto("HardDif_Btn");
			OcultarTexto("tooltips");
			OcultarTexto("game difficulty");
			OcultarTexto("normal");
			OcultarTexto("hardcore");

			DefinirPosicaoY("MasterVolumeSlider", 117f);
			DefinirPosicaoYDoTexto("music volume", -164.5f);
			DefinirPosicaoYDoTexto("show hud", -231.5f);

			// Só ficam os recursos gráficos que o projeto realmente usa.
			OcultarObjeto("AAOff_Btn");
			OcultarObjeto("AA2X_Btn");
			OcultarObjeto("AA4X_Btn");
			OcultarObjeto("AA8X_Btn");
			OcultarObjeto("MotionBlur_Btn");
			OcultarObjeto("CameraEffects_Btn");
			OcultarObjeto("Dropdown");
			OcultarObjeto("KeyBindings_Btn");
			OcultarObjeto("LineKeyBindings");
			OcultarObjeto("PanelKeyBindings");
			OcultarObjeto("aaofftextLINE");
			OcultarObjeto("aa2xtextLINE");
			OcultarObjeto("aa4xtextLINE");
			OcultarObjeto("aa8xtextLINE");
			OcultarTexto("a-sync");
			SubstituirTexto("motion blur", "Campo de visão");
			OcultarTexto("camera effects");
			OcultarTexto("control layout");

			// Reorganiza as opções visíveis em linhas regulares no mesmo painel.
			DefinirPosicaoY("VSync_Btn", -17f);
			DefinirPosicaoY("TextureLow_Btn", -84f);
			DefinirPosicaoY("TextureMed_Btn", -84f);
			DefinirPosicaoY("TextureHigh_Btn", -84f);
			CriarSliderCampoVisao();
			DefinirPosicaoYDoTexto("v-sync", -365.5f);
			DefinirPosicaoYDoTexto("texture quality", -432.5f);
			DefinirPosicaoYDoTexto("Campo de visão", -499.49f);

			TraduzirTextos();
		}

		private void CriarSliderCampoVisao()
		{
			if (sensitivityXSlider == null) return;

			Transform painelVideo = EncontrarTransform("PanelVideo");
			if (painelVideo == null) return;

			Transform existente = painelVideo.Find("FieldOfViewSlider");
			GameObject objetoSlider = existente != null
				? existente.gameObject
				: Instantiate(sensitivityXSlider, painelVideo, false);
			objetoSlider.name = "FieldOfViewSlider";

			EventTrigger gatilho = objetoSlider.GetComponent<EventTrigger>();
			if (gatilho != null) Destroy(gatilho);

			sliderCampoVisao = objetoSlider.GetComponent<Slider>();
			if (sliderCampoVisao == null) return;

			DesativarCallbacksPersistentes(sliderCampoVisao);
			float campoVisaoPadrao = jogador != null && jogador.cameraPlayer != null
				&& jogador.cameraPlayer.GetComponent<Camera>() != null
				? jogador.cameraPlayer.GetComponent<Camera>().fieldOfView
				: Camera.main != null ? Camera.main.fieldOfView : 60f;
			float campoVisao = PlayerPrefs.GetFloat(ChaveCampoVisao, campoVisaoPadrao);
			ConfigurarSlider(sliderCampoVisao, 60f, 100f, campoVisao);
			DefinirPosicaoY(objetoSlider, -151f);
			sliderCampoVisao.onValueChanged.AddListener(AtualizarCampoVisao);
		}

		private void AtualizarCampoVisao(float valor)
		{
			valor = Mathf.Clamp(valor, 60f, 100f);
			PlayerPrefs.SetFloat(ChaveCampoVisao, valor);
			if (jogador != null) jogador.DefinirCampoVisao(valor);
			PlayerPrefs.Save();
		}

		private void TraduzirTextos()
		{
			Dictionary<string, string> traducao = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				{ "SETTINGS", "CONFIGURAÇÕES" },
				{ "GAME", "JOGO" },
				{ "VIDEO", "VÍDEO" },
				{ "CONTROLS", "CONTROLES" },
				{ "'Controls", "CONTROLES" },
				{ "music volume", "Volume da música" },
				{ "show hud", "Volume geral" },
				{ "full screen", "Tela cheia" },
				{ "ambient occlusion", "Perfil gráfico" },
				{ "shadow quality", "Qualidade das sombras" },
				{ "'v-sync", "Sincronização vertical" },
				{ "v-sync", "Sincronização vertical" },
				{ "texture quality", "Qualidade das texturas" },
				{ "mouse horizontal look", "Sensibilidade do mouse" },
				{ "mouse vertical look", "Sensibilidade do controle" },
				{ "mouse smoothing", "Suavização da câmera" },
				{ "invert mouse", "Inverter eixo vertical" },
				{ "Return", "Voltar" },
				{ "Cancel", "Cancelar" },
				{ "Are you sure?", "Tem certeza?" },
				{ "off", "Desligado" },
				{ "on", "Ligado" },
				{ "low", "Baixa" },
				{ "med", "Média" },
				{ "high", "Alta" },
				{ "normal", "Normal" },
				{ "hardcore", "Difícil" }
			};

			foreach (TMP_Text texto in GetComponentsInChildren<TMP_Text>(true))
			{
				string chave = texto.text.Trim();
				if (!traducao.TryGetValue(chave, out string valor)) continue;

				texto.text = valor;
				AjustarTextoSemQuebra(texto);
			}
		}

		private void OcultarObjeto(string nome)
		{
			Transform encontrado = EncontrarTransform(nome);
			if (encontrado != null) encontrado.gameObject.SetActive(false);
		}

		private void OcultarTexto(string textoAlvo)
		{
			foreach (TMP_Text texto in GetComponentsInChildren<TMP_Text>(true))
			{
				if (string.Equals(texto.text.Trim().Trim('\''), textoAlvo, StringComparison.OrdinalIgnoreCase))
					texto.gameObject.SetActive(false);
			}
		}

		private void SubstituirTexto(string textoAlvo, string novoTexto)
		{
			foreach (TMP_Text texto in GetComponentsInChildren<TMP_Text>(true))
			{
				if (!string.Equals(texto.text.Trim().Trim('\''), textoAlvo, StringComparison.OrdinalIgnoreCase)) continue;

				texto.text = novoTexto;
				AjustarTextoSemQuebra(texto);
			}
		}

		private Transform EncontrarTransform(string nome)
		{
			foreach (Transform item in GetComponentsInChildren<Transform>(true))
				if (item.name == nome) return item;
			return null;
		}

		private void DefinirPosicaoY(string nome, float y)
		{
			Transform item = EncontrarTransform(nome);
			if (item != null) DefinirPosicaoY(item.gameObject, y);
		}

		private static void DefinirPosicaoY(GameObject objeto, float y)
		{
			if (objeto == null) return;
			RectTransform rect = objeto.GetComponent<RectTransform>();
			if (rect == null) return;
			Vector2 posicao = rect.anchoredPosition;
			posicao.y = y;
			rect.anchoredPosition = posicao;
		}

		private void DefinirPosicaoYDoTexto(string textoAlvo, float y)
		{
			foreach (TMP_Text texto in GetComponentsInChildren<TMP_Text>(true))
			{
				if (string.Equals(texto.text.Trim().Trim('\''), textoAlvo.Trim().Trim('\''), StringComparison.OrdinalIgnoreCase))
				{
					RectTransform rect = texto.GetComponent<RectTransform>();
					if (rect == null) continue;
					Vector2 posicao = rect.anchoredPosition;
					posicao.y = y;
					rect.anchoredPosition = posicao;
				}
			}
		}

		private void AplicarPerfilDeQualidadeSalvo()
		{
			if (PlayerPrefs.HasKey("GraphicsQuality"))
			{
				int perfilSalvo = Mathf.Clamp(PlayerPrefs.GetInt("GraphicsQuality"), 0, QualitySettings.names.Length - 1);
				QualitySettings.SetQualityLevel(perfilSalvo, true);
			}
			AtualizarTextoDoPerfilGrafico();
		}

		private static int EncontrarIndiceDeQualidade(string nome)
		{
			for (int i = 0; i < QualitySettings.names.Length; i++)
				if (QualitySettings.names[i].IndexOf(nome, StringComparison.OrdinalIgnoreCase) >= 0) return i;
			return -1;
		}

		private void AtualizarTextoDoPerfilGrafico()
		{
			if (ambientocclusiontext == null) return;
			TMP_Text texto = ambientocclusiontext.GetComponent<TMP_Text>();
			if (texto == null) return;

			int indice = QualitySettings.GetQualityLevel();
			string nome = indice >= 0 && indice < QualitySettings.names.Length
				? QualitySettings.names[indice]
				: string.Empty;
			texto.text = nome.IndexOf("Mobile", StringComparison.OrdinalIgnoreCase) >= 0
				? "Desempenho"
				: nome.IndexOf("PC", StringComparison.OrdinalIgnoreCase) >= 0
					? "Qualidade"
					: "Personalizado";
			AjustarTextoSemQuebra(texto);
		}

		private void ConfigurarSombras(int nivel)
		{
			nivel = Mathf.Clamp(nivel, 0, 2);
			PlayerPrefs.SetInt(platform == Platform.Mobile ? "MobileShadows" : "Shadows", nivel);
			QualitySettings.shadowCascades = nivel == 0 ? 0 : nivel == 1 ? 2 : 4;
			QualitySettings.shadowDistance = nivel == 0 ? 0f : nivel == 1 ? 75f : (platform == Platform.Mobile ? 100f : 500f);
			bool desktop = platform == Platform.Desktop;
			DefinirAtivo(shadowofftextLINE, desktop && nivel == 0);
			DefinirAtivo(shadowlowtextLINE, desktop && nivel == 1);
			DefinirAtivo(shadowhightextLINE, desktop && nivel == 2);
			DefinirAtivo(mobileShadowofftextLINE, !desktop && nivel == 0);
			DefinirAtivo(mobileShadowlowtextLINE, !desktop && nivel == 1);
			DefinirAtivo(mobileShadowhightextLINE, !desktop && nivel == 2);
		}

		private void ConfigurarTexturas(int nivel)
		{
			nivel = Mathf.Clamp(nivel, 0, 2);
			PlayerPrefs.SetInt("Textures", nivel);
			QualitySettings.globalTextureMipmapLimit = 2 - nivel;
			DefinirAtivo(texturelowtextLINE, nivel == 0);
			DefinirAtivo(texturemedtextLINE, nivel == 1);
			DefinirAtivo(texturehightextLINE, nivel == 2);
		}

		private static void DefinirAtivo(GameObject objeto, bool ativo)
		{
			if (objeto != null) objeto.SetActive(ativo);
		}

		private void AtualizarEstadoTelaCheia()
		{
			DefinirEstadoTexto(fullscreentext, Screen.fullScreen);
		}

		private void AtualizarEstadoVSync()
		{
			DefinirEstadoTexto(vsynctext, QualitySettings.vSyncCount != 0);
		}

		private void AtualizarEstadoInversao()
		{
			DefinirEstadoTexto(invertmousetext, PlayerPrefs.GetInt("Inverted", 0) == 1);
		}

		private static void DefinirEstadoTexto(GameObject alvo, bool ligado)
		{
			if (alvo == null) return;
			TMP_Text texto = alvo.GetComponent<TMP_Text>();
			if (texto == null) return;
			texto.text = ligado ? "Ligado" : "Desligado";
			AjustarTextoSemQuebra(texto);
		}

		private static void AjustarTextoSemQuebra(TMP_Text texto)
		{
			if (texto == null) return;
			texto.enableWordWrapping = false;
			texto.enableAutoSizing = true;
			texto.fontSizeMax = texto.fontSize;
			texto.fontSizeMin = texto.fontSize * 0.55f;
		}

		private static bool ValorFinito(float valor)
		{
			return !float.IsNaN(valor) && !float.IsInfinity(valor);
		}

		public void FullScreen()
		{
			Screen.fullScreen = !Screen.fullScreen;
			PlayerPrefs.SetInt("FullScreen", Screen.fullScreen ? 1 : 0);
			PlayerPrefs.Save();
			AtualizarEstadoTelaCheia();
		}

		public void MusicSlider()
		{
			Slider slider = ObterSlider(musicSlider);
			if (slider == null) return;
			PlayerPrefs.SetFloat(ChaveVolumeMusica, slider.value);
			PlayerPrefs.Save();
		}

		private void AtualizarVolumeGeral(float valor)
		{
			AudioListener.volume = valor;
			PlayerPrefs.SetFloat(ChaveVolumeGeral, valor);
			PlayerPrefs.Save();
		}

		public void SensitivityXSlider()
		{
			Slider slider = ObterSlider(sensitivityXSlider);
			if (slider == null) return;
			float valor = Mathf.Clamp(slider.value, 0.05f, 1f);
			PlayerPrefs.SetFloat(ChaveSensibilidadeMouse, valor);
			if (jogador != null) jogador.sensibilidade = valor;
			PlayerPrefs.Save();
		}

		public void SensitivityYSlider()
		{
			Slider slider = ObterSlider(sensitivityYSlider);
			if (slider == null) return;
			float valor = Mathf.Clamp(slider.value, 40f, 320f);
			PlayerPrefs.SetFloat(ChaveSensibilidadeControle, valor);
			if (jogador != null) jogador.sensibilidadeControle = valor;
			PlayerPrefs.Save();
		}

		public void SensitivitySmoothing()
		{
			Slider slider = ObterSlider(mouseSmoothSlider);
			if (slider == null) return;
			float valor = Mathf.Clamp(slider.value, 0f, 0.3f);
			PlayerPrefs.SetFloat(ChaveSuavidadeOlhar, valor);
			if (jogador != null) jogador.suavidadeOlhar = valor;
			PlayerPrefs.Save();
		}

		public void ShowHUD()
		{
			AlternarPreferencia("ShowHUD", showhudtext);
		}

		public void ToolTips()
		{
			AlternarPreferencia("ToolTips", tooltipstext);
		}

		public void NormalDifficulty()
		{
			DefinirAtivo(difficultynormaltextLINE, true);
			DefinirAtivo(difficultyhardcoretextLINE, false);
			PlayerPrefs.SetInt("NormalDifficulty", 1);
			PlayerPrefs.SetInt("HardCoreDifficulty", 0);
			PlayerPrefs.Save();
		}

		public void HardcoreDifficulty()
		{
			DefinirAtivo(difficultynormaltextLINE, false);
			DefinirAtivo(difficultyhardcoretextLINE, true);
			PlayerPrefs.SetInt("NormalDifficulty", 0);
			PlayerPrefs.SetInt("HardCoreDifficulty", 1);
			PlayerPrefs.Save();
		}

		public void ShadowsOff() { ConfigurarSombras(0); PlayerPrefs.Save(); }
		public void ShadowsLow() { ConfigurarSombras(1); PlayerPrefs.Save(); }
		public void ShadowsHigh() { ConfigurarSombras(2); PlayerPrefs.Save(); }
		public void MobileShadowsOff() { ConfigurarSombras(0); PlayerPrefs.Save(); }
		public void MobileShadowsLow() { ConfigurarSombras(1); PlayerPrefs.Save(); }
		public void MobileShadowsHigh() { ConfigurarSombras(2); PlayerPrefs.Save(); }

		public void vsync()
		{
			QualitySettings.vSyncCount = QualitySettings.vSyncCount == 0 ? 1 : 0;
			PlayerPrefs.SetInt("VSync", QualitySettings.vSyncCount);
			AtualizarEstadoVSync();
			PlayerPrefs.Save();
		}

		public void InvertMouse()
		{
			int invertido = PlayerPrefs.GetInt("Inverted", 0) == 0 ? 1 : 0;
			PlayerPrefs.SetInt("Inverted", invertido);
			if (jogador != null) jogador.DefinirInversaoVertical(invertido == 1);
			AtualizarEstadoInversao();
			PlayerPrefs.Save();
		}

		public void AmbientOcclusion()
		{
			int indiceMobile = EncontrarIndiceDeQualidade("Mobile");
			int indicePC = EncontrarIndiceDeQualidade("PC");
			if (indiceMobile < 0) indiceMobile = 0;
			if (indicePC < 0) indicePC = Mathf.Max(0, QualitySettings.names.Length - 1);

			int novoIndice = QualitySettings.GetQualityLevel() == indiceMobile ? indicePC : indiceMobile;
			QualitySettings.SetQualityLevel(novoIndice, true);
			PlayerPrefs.SetInt("GraphicsQuality", novoIndice);
			if (PlayerPrefs.HasKey("VSync"))
				QualitySettings.vSyncCount = Mathf.Clamp(PlayerPrefs.GetInt("VSync"), 0, 2);
			ConfigurarSombras(PlayerPrefs.GetInt(platform == Platform.Mobile ? "MobileShadows" : "Shadows", 1));
			ConfigurarTexturas(PlayerPrefs.GetInt("Textures", 2));
			AtualizarTextoDoPerfilGrafico();
			PlayerPrefs.Save();
		}

		public void TexturesLow() { ConfigurarTexturas(0); PlayerPrefs.Save(); }
		public void TexturesMed() { ConfigurarTexturas(1); PlayerPrefs.Save(); }
		public void TexturesHigh() { ConfigurarTexturas(2); PlayerPrefs.Save(); }

		public void MotionBlur() { AlternarPreferencia("MotionBlur", motionblurtext); }
		public void CameraEffects() { AlternarPreferencia("CameraEffects", cameraeffectstext); }

		public void MobileSFXMute()
		{
			AlternarPreferencia("Mobile_MuteSfx", mobileSFXtext);
		}

		public void MobileMusicMute()
		{
			AlternarPreferencia("Mobile_MuteMusic", mobileMusictext);
		}

		private static void AlternarPreferencia(string chave, GameObject textoAlvo)
		{
			int novoValor = PlayerPrefs.GetInt(chave, 0) == 0 ? 1 : 0;
			PlayerPrefs.SetInt(chave, novoValor);
			DefinirEstadoTexto(textoAlvo, novoValor == 1);
			PlayerPrefs.Save();
		}
	}
}
