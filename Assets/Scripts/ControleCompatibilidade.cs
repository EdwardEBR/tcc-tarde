using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Leitura comum para gamepads padronizados e controles HID genéricos.</summary>
public static class ControleCompatibilidade
{
    private static float proximoMovimentoUI;

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

        bool confirmou = false;
        for (int i = 0; i < Gamepad.all.Count && !confirmou; i++)
            confirmou = Gamepad.all[i].buttonSouth.wasPressedThisFrame;
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

        // O EventSystem serializado nas cenas aponta para um asset removido.
        // Usa o módulo legado, já mapeado para Horizontal/Vertical/Submit no Input Manager.
        InputSystemUIInputModule moduloInputSystem = sistema.GetComponent<InputSystemUIInputModule>();
        if (moduloInputSystem != null)
            moduloInputSystem.enabled = false;

        StandaloneInputModule moduloLegado = sistema.GetComponent<StandaloneInputModule>();
        if (moduloLegado == null)
            moduloLegado = sistema.gameObject.AddComponent<StandaloneInputModule>();

        moduloLegado.horizontalAxis = "Horizontal";
        moduloLegado.verticalAxis = "Vertical";
        moduloLegado.submitButton = "Submit";
        moduloLegado.cancelButton = "Cancel";
        moduloLegado.enabled = true;
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
