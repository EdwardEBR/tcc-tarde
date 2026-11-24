using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Leitura comum para gamepads padronizados e controles HID genéricos.</summary>
public static class ControleCompatibilidade
{
    private static float proximoMovimentoUI;
    private const float TamanhoMinimoAlvoTouchDp = 48f;
    private const float DensidadeFallbackDp = 3f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegistrarPreparacaoDaInterface()
    {
        SceneManager.sceneLoaded -= PrepararInterfaceDaCena;
        SceneManager.sceneLoaded += PrepararInterfaceDaCena;
    }

    private static void PrepararInterfaceDaCena(Scene cena, LoadSceneMode modo)
    {
        EventSystem[] sistemas = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
        EventSystem sistemaPrincipal = EventSystem.current;
        if (sistemaPrincipal == null || !sistemaPrincipal.isActiveAndEnabled)
        {
            for (int i = 0; i < sistemas.Length; i++)
            {
                if (sistemas[i] != null && sistemas[i].isActiveAndEnabled)
                {
                    sistemaPrincipal = sistemas[i];
                    break;
                }
            }
        }

        if (sistemaPrincipal == null)
        {
            GameObject objeto = new GameObject("EventSystem (Compatibilidade)", typeof(EventSystem));
            sistemaPrincipal = objeto.GetComponent<EventSystem>();
            System.Array.Resize(ref sistemas, sistemas.Length + 1);
            sistemas[sistemas.Length - 1] = sistemaPrincipal;
        }

        for (int i = 0; i < sistemas.Length; i++)
        {
            EventSystem sistema = sistemas[i];
            if (sistema == null || sistema == sistemaPrincipal) continue;

            StandaloneInputModule legado = sistema.GetComponent<StandaloneInputModule>();
            if (legado != null) legado.enabled = false;
            InputSystemUIInputModule moderno = sistema.GetComponent<InputSystemUIInputModule>();
            if (moderno != null) moderno.enabled = false;
            sistema.enabled = false;
        }

        GarantirModuloUI(sistemaPrincipal);
        if (Application.isMobilePlatform)
            AjustarAlvosTouchEmLayouts();
    }

    private static void AjustarAlvosTouchEmLayouts()
    {
        Selectable[] elementos = Object.FindObjectsByType<Selectable>(FindObjectsInactive.Include);
        for (int i = 0; i < elementos.Length; i++)
        {
            Selectable elemento = elementos[i];
            if (elemento == null) continue;

            VerticalLayoutGroup grupo = elemento.transform.parent != null
                ? elemento.transform.parent.GetComponent<VerticalLayoutGroup>()
                : null;
            RectTransform alvo = elemento.transform as RectTransform;
            if (grupo == null || alvo == null) continue;

            Canvas canvas = elemento.GetComponentInParent<Canvas>();
            float densidadeDp = Screen.dpi > 0f ? Screen.dpi / 160f : DensidadeFallbackDp;
            float escalaCanvas = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
            float tamanhoMinimo = TamanhoMinimoAlvoTouchDp * densidadeDp / escalaCanvas;

            Vector2 tamanho = alvo.sizeDelta;
            bool alterado = false;
            bool larguraInsuficiente = tamanho.x < tamanhoMinimo;
            bool alturaInsuficiente = tamanho.y < tamanhoMinimo;
            if (larguraInsuficiente)
            {
                tamanho.x = tamanhoMinimo;
                alterado = true;
            }
            if (alturaInsuficiente)
            {
                tamanho.y = tamanhoMinimo;
                alterado = true;
            }

            if (grupo.childControlWidth || grupo.childControlHeight)
            {
                LayoutElement metricas = elemento.GetComponent<LayoutElement>();
                if (metricas == null) metricas = elemento.gameObject.AddComponent<LayoutElement>();
                if (grupo.childControlWidth && larguraInsuficiente)
                    metricas.minWidth = Mathf.Max(metricas.minWidth, tamanhoMinimo);
                if (grupo.childControlHeight && alturaInsuficiente)
                {
                    metricas.minHeight = Mathf.Max(metricas.minHeight, tamanhoMinimo);
                    metricas.preferredHeight = Mathf.Max(metricas.preferredHeight, tamanhoMinimo);
                }
            }

            if (alterado)
            {
                alvo.sizeDelta = tamanho;
                LayoutRebuilder.MarkLayoutForRebuild(grupo.GetComponent<RectTransform>());
            }
        }
    }

    public static Vector2 LerMovimento()
    {
        Vector2 movimento = Gamepad.current != null
            ? Gamepad.current.leftStick.ReadValue()
            : Joystick.current != null ? Joystick.current.stick.ReadValue() : Vector2.zero;

        // Alguns drivers expõem o controle no sistema novo, mas não mapeiam os eixos.
        // Nesse caso, aproveita o eixo legado em vez de aceitar um vetor sempre zerado.
        movimento = ComFallbackLegado(movimento, "ControllerNavigateX", "ControllerNavigateY");
        return ComFallbackLegado(movimento, "Horizontal", "Vertical");
    }

    public static Vector2 LerOlharGenerico()
    {
        Vector2 olhar = Gamepad.current != null ? Gamepad.current.rightStick.ReadValue() : Vector2.zero;

        // No layout legado do Xbox 360, os eixos 3 e 4 correspondem ao analógico direito.
        return ComFallbackLegado(olhar, "ControllerLookX", "ControllerLookY");
    }

    public static bool BotaoXboxAPressionado(Gamepad gamepad)
    {
        return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
    }

    public static bool BotaoXboxBPressionado(Gamepad gamepad)
    {
        return gamepad != null && gamepad.buttonEast.wasPressedThisFrame;
    }

    public static bool BotaoXboxXPressionado(Gamepad gamepad)
    {
        return gamepad != null && gamepad.buttonWest.wasPressedThisFrame;
    }

    public static bool BotaoXboxYPressionado(Gamepad gamepad)
    {
        return gamepad != null && gamepad.buttonNorth.wasPressedThisFrame;
    }

    public static bool AlgumBotaoXboxAPressionado()
    {
        for (int i = 0; i < Gamepad.all.Count; i++)
            if (BotaoXboxAPressionado(Gamepad.all[i])) return true;
        return false;
    }

    public static bool AlgumBotaoXboxBPressionado()
    {
        for (int i = 0; i < Gamepad.all.Count; i++)
            if (BotaoXboxBPressionado(Gamepad.all[i])) return true;
        return false;
    }

    private static Vector2 ComFallbackLegado(Vector2 valor, string eixoX, string eixoY)
    {
        if (valor.sqrMagnitude > 0.0001f) return valor;

        Vector2 legado = new Vector2(Input.GetAxisRaw(eixoX), Input.GetAxisRaw(eixoY));
        return legado.sqrMagnitude > 0.0001f ? legado : valor;
    }

    public static void LerAnalogicos(out Vector2 esquerdo, out Vector2 direito)
    {
        esquerdo = LerMovimento();
        direito = LerOlharGenerico();
    }

    public static bool BotaoJoystickPressionado(int indice)
    {
        Joystick joystick = Joystick.current;
        ButtonControl botao = joystick != null
            ? joystick.TryGetChildControl<ButtonControl>("button" + indice)
            : null;
        if (botao != null && botao.wasPressedThisFrame) return true;

        return Input.GetKeyDown(CodigoTeclaJoystick(indice));
    }

    public static bool BotaoJoystickSegurado(int indice)
    {
        Joystick joystick = Joystick.current;
        ButtonControl botao = joystick != null
            ? joystick.TryGetChildControl<ButtonControl>("button" + indice)
            : null;
        if (botao != null && botao.isPressed) return true;

        return Input.GetKey(CodigoTeclaJoystick(indice));
    }

    public static void AtualizarNavegacaoUI()
    {
        EventSystem sistema = EventSystem.current;
        if (sistema == null) return;

        GarantirSelecaoUI(sistema);

        Vector2 movimento = Vector2.zero;
        for (int i = 0; i < Gamepad.all.Count && movimento.sqrMagnitude < 0.0001f; i++)
        {
            Gamepad gamepad = Gamepad.all[i];
            movimento = gamepad.dpad.ReadValue();
            if (movimento.sqrMagnitude < 0.0001f)
                movimento = gamepad.leftStick.ReadValue();
        }

        for (int i = 0; i < Joystick.all.Count && movimento.sqrMagnitude < 0.0001f; i++)
        {
            Joystick joystick = Joystick.all[i];
            movimento = joystick.stick.ReadValue();
            if (joystick.hatswitch != null)
                movimento += joystick.hatswitch.ReadValue();
        }

        if (movimento.sqrMagnitude < 0.0001f)
            movimento = new Vector2(Input.GetAxisRaw("ControllerNavigateX"), Input.GetAxisRaw("ControllerNavigateY"));
        if (movimento.sqrMagnitude < 0.0001f)
            movimento = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (movimento.sqrMagnitude > 1f) movimento.Normalize();

        StandaloneInputModule moduloLegado = sistema.GetComponent<StandaloneInputModule>();
        bool moduloLegadoAtivo = moduloLegado != null && moduloLegado.isActiveAndEnabled;
        bool moduloLegadoRecebeuMovimento = moduloLegadoAtivo
            && (Mathf.Abs(Input.GetAxisRaw(moduloLegado.horizontalAxis)) > 0.0001f
                || Mathf.Abs(Input.GetAxisRaw(moduloLegado.verticalAxis)) > 0.0001f);

        InputSystemUIInputModule moduloInputSystem = sistema.GetComponent<InputSystemUIInputModule>();
        bool moduloInputSystemAtivo = moduloInputSystem != null && moduloInputSystem.isActiveAndEnabled
            && sistema.currentInputModule == moduloInputSystem;
        InputAction acaoNativaMover = moduloInputSystem != null && moduloInputSystem.move != null
            ? moduloInputSystem.move.action
            : null;
        bool moduloInputSystemRecebeuMovimento = moduloInputSystemAtivo
            && acaoNativaMover != null && acaoNativaMover.enabled
            && acaoNativaMover.expectedControlType == "Vector2"
            && acaoNativaMover.ReadValue<Vector2>().sqrMagnitude > 0.0001f;
        bool moduloRecebeuMovimento = moduloLegadoRecebeuMovimento || moduloInputSystemRecebeuMovimento;

        if (movimento.sqrMagnitude > 0.36f && !moduloRecebeuMovimento
            && Time.unscaledTime >= proximoMovimentoUI)
        {
            Vector2 direcao = Mathf.Abs(movimento.x) > Mathf.Abs(movimento.y)
                ? new Vector2(Mathf.Sign(movimento.x), 0f)
                : new Vector2(0f, Mathf.Sign(movimento.y));
            MoverSelecaoUI(sistema, direcao);
            proximoMovimentoUI = Time.unscaledTime + 0.22f;
        }

        bool confirmou = AlgumBotaoXboxAPressionado();
        if (!confirmou)
            confirmou = BotaoJoystickPressionado(0);
        bool moduloLegadoRecebeuConfirmacao = moduloLegadoAtivo
            && Input.GetButtonDown(moduloLegado.submitButton);
        InputAction acaoNativaConfirmar = moduloInputSystem != null && moduloInputSystem.submit != null
            ? moduloInputSystem.submit.action
            : null;
        bool moduloInputSystemRecebeuConfirmacao = moduloInputSystemAtivo
            && acaoNativaConfirmar != null && acaoNativaConfirmar.enabled
            && acaoNativaConfirmar.WasPressedThisFrame();
        bool moduloRecebeuConfirmacao = moduloLegadoRecebeuConfirmacao || moduloInputSystemRecebeuConfirmacao;
        if (!confirmou || moduloRecebeuConfirmacao) return;

        GameObject alvo = sistema.currentSelectedGameObject;
        if (alvo == null) return;

        Button botao = alvo.GetComponent<Button>();
        if (botao != null && botao.isActiveAndEnabled && botao.interactable)
            botao.onClick.Invoke();
        else
            ExecuteEvents.Execute(alvo, new BaseEventData(sistema), ExecuteEvents.submitHandler);
    }

    private static void MoverSelecaoUI(EventSystem sistema, Vector2 direcao)
    {
        GameObject selecionado = sistema.currentSelectedGameObject;
        if (selecionado == null) return;

        Selectable atual = selecionado.GetComponent<Selectable>();
        if (atual == null) return;

        Selectable proximo = atual.FindSelectable(direcao);
        if (proximo != null && proximo.isActiveAndEnabled && proximo.interactable)
            sistema.SetSelectedGameObject(proximo.gameObject);
    }

    private static void GarantirSelecaoUI(EventSystem sistema)
    {
        GameObject selecionado = sistema.currentSelectedGameObject;
        Selectable elementoSelecionado = selecionado != null
            ? selecionado.GetComponent<Selectable>()
            : null;
        if (elementoSelecionado != null && elementoSelecionado.isActiveAndEnabled
            && elementoSelecionado.interactable)
            return;

        foreach (Selectable item in Object.FindObjectsByType<Selectable>())
        {
            if (item == null || !item.isActiveAndEnabled || !item.interactable) continue;
            Navigation navegacao = item.navigation;
            if (navegacao.mode == Navigation.Mode.None)
            {
                navegacao.mode = Navigation.Mode.Automatic;
                item.navigation = navegacao;
            }
            sistema.SetSelectedGameObject(item.gameObject);
            return;
        }
    }

    public static void GarantirModuloUI(EventSystem sistema)
    {
        if (sistema == null) return;

        // As cenas têm referências antigas para um asset de ações que já não existe.
        // Recupera as ações padrão do pacote atual para manter mouse, teclado, toque e multitouch.
        StandaloneInputModule moduloLegado = sistema.GetComponent<StandaloneInputModule>();
        if (moduloLegado != null)
            moduloLegado.enabled = false;

        InputSystemUIInputModule moduloInputSystem = sistema.GetComponent<InputSystemUIInputModule>();
        if (moduloInputSystem == null)
            moduloInputSystem = sistema.gameObject.AddComponent<InputSystemUIInputModule>();

        if (moduloInputSystem.actionsAsset == null
            || moduloInputSystem.point == null || moduloInputSystem.point.action == null
            || moduloInputSystem.leftClick == null || moduloInputSystem.leftClick.action == null
            || moduloInputSystem.move == null || moduloInputSystem.move.action == null
            || moduloInputSystem.submit == null || moduloInputSystem.submit.action == null)
        {
            moduloInputSystem.AssignDefaultActions();
        }

        moduloInputSystem.pointerBehavior = UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack;
        moduloInputSystem.enabled = true;
        sistema.sendNavigationEvents = true;
    }

    private static KeyCode CodigoTeclaJoystick(int indice)
    {
        switch (indice)
        {
            case 0: return KeyCode.JoystickButton0;
            case 1: return KeyCode.JoystickButton1;
            case 2: return KeyCode.JoystickButton2;
            case 3: return KeyCode.JoystickButton3;
            case 4: return KeyCode.JoystickButton4;
            case 5: return KeyCode.JoystickButton5;
            case 6: return KeyCode.JoystickButton6;
            case 7: return KeyCode.JoystickButton7;
            case 8: return KeyCode.JoystickButton8;
            case 9: return KeyCode.JoystickButton9;
            default: return KeyCode.None;
        }
    }
}
