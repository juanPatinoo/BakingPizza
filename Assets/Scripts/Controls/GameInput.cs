using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace BreakingPizza.Controls
{
    // Entrada unificada: teclado, mouse y joystick.
    [DefaultExecutionOrder(-1000)]
    public class GameInput : MonoBehaviour
    {
        private const float StickDeadzone = 0.18f;
        private const float PointerSpeed = 1250f; // píxeles/seg a 1080p

        public static bool UsingGamepad { get; private set; }
        public static event System.Action<bool> OnDeviceChanged;

        private static GameInput instance;
        private static Vector2 pointer;
        private static int navX, navY;
        private static Vector2 prevNavStick;

        public static Gamepad Pad => Gamepad.current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            UsingGamepad = false;
            OnDeviceChanged = null;
            instance = null;
            navX = navY = 0;
            prevNavStick = Vector2.zero;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            if (instance != null) return;
            InputSystem.settings.maxEventBytesPerUpdate = 0;

            var go = new GameObject("[GameInput]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GameInput>();
        }

        private void Update()
        {
            DetectDevice();
            UpdatePointer();
            UpdateNavigation();
        }

        private void LateUpdate()
        {
            if (UsingGamepad && Cursor.visible) Cursor.visible = false;
        }

        // ── Detección de dispositivo ──

        private static void DetectDevice()
        {
            var pad = Pad;
            bool padUsed = pad != null &&
                           (pad.leftStick.ReadValue().magnitude > 0.35f ||
                            pad.rightStick.ReadValue().magnitude > 0.35f ||
                            AnyPadButtonPressed(pad));

            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool kbmUsed = (kb != null && kb.anyKey.wasPressedThisFrame) ||
                           (mouse != null && (mouse.leftButton.wasPressedThisFrame ||
                                              mouse.rightButton.wasPressedThisFrame ||
                                              MouseMovedByUser(mouse)));

            if (padUsed && !UsingGamepad) SetGamepad(true);
            else if (kbmUsed && !padUsed && UsingGamepad) SetGamepad(false);
        }

        private static bool MouseMovedByUser(Mouse mouse)
        {
            return mouse.delta.ReadValue().magnitude > 3f;
        }

        private static void SetGamepad(bool gamepad)
        {
            UsingGamepad = gamepad;
            if (gamepad)
            {
                pointer = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width / 2f, Screen.height / 2f);
            }
            else if (Cursor.lockState != CursorLockMode.Locked)
            {
                // De vuelta al mouse en un menú/minijuego: se vuelve a ver.
                Cursor.visible = true;
            }
            OnDeviceChanged?.Invoke(gamepad);
        }

        private static bool AnyPadButtonPressed(Gamepad pad)
        {
            ButtonControl[] buttons =
            {
                pad.buttonSouth, pad.buttonEast, pad.buttonWest, pad.buttonNorth,
                pad.leftShoulder, pad.rightShoulder, pad.leftTrigger, pad.rightTrigger,
                pad.dpad.up, pad.dpad.down, pad.dpad.left, pad.dpad.right,
                pad.startButton, pad.selectButton, pad.leftStickButton, pad.rightStickButton
            };
            foreach (var b in buttons)
            {
                if (b != null && b.wasPressedThisFrame) return true;
            }
            return false;
        }

        // ── Puntero interno (solo para apuntar en el pepperoni) ──

        // No mueve ni muestra el mouse: es una posición en pantalla que
        // los sticks desplazan. Quien la usa la dibuja a su manera.
        private static void UpdatePointer()
        {
            var pad = Pad;
            if (!UsingGamepad || pad == null || Cursor.lockState == CursorLockMode.Locked) return;

            Vector2 stick = Vector2.ClampMagnitude(pad.rightStick.ReadValue() + pad.leftStick.ReadValue(), 1f);
            float mag = stick.magnitude;
            if (mag < StickDeadzone) return;

            Vector2 velocity = stick.normalized * (mag * mag);
            pointer += velocity * PointerSpeed * (Screen.height / 1080f) * Time.unscaledDeltaTime;
            pointer.x = Mathf.Clamp(pointer.x, 2f, Screen.width - 2f);
            pointer.y = Mathf.Clamp(pointer.y, 2f, Screen.height - 2f);
        }

        // Para centrar el puntero interno donde convenga (ej. en la pizza).
        public static void SetPointer(Vector2 screenPosition)
        {
            pointer = screenPosition;
        }

        // Posición del puntero en pantalla: la del mouse, o la del puntero
        // interno si se está jugando con el control.
        public static Vector2 Pointer
        {
            get
            {
                if (UsingGamepad && Cursor.lockState != CursorLockMode.Locked) return pointer;
                return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            }
        }

        // ── Navegación de menús (flechas / WASD / cruceta / stick) ──

        private static void UpdateNavigation()
        {
            navX = 0;
            navY = 0;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) navX = -1;
                if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) navX = 1;
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) navY = 1;
                if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) navY = -1;
            }

            var pad = Pad;
            if (pad != null)
            {
                if (pad.dpad.left.wasPressedThisFrame) navX = -1;
                if (pad.dpad.right.wasPressedThisFrame) navX = 1;
                if (pad.dpad.up.wasPressedThisFrame) navY = 1;
                if (pad.dpad.down.wasPressedThisFrame) navY = -1;

                // Stick: cuenta como "un toque" cuando cruza el umbral.
                Vector2 s = pad.leftStick.ReadValue();
                if (s.x > 0.6f && prevNavStick.x <= 0.6f) navX = 1;
                if (s.x < -0.6f && prevNavStick.x >= -0.6f) navX = -1;
                if (s.y > 0.6f && prevNavStick.y <= 0.6f) navY = 1;
                if (s.y < -0.6f && prevNavStick.y >= -0.6f) navY = -1;
                prevNavStick = s;
            }
        }

        public static int NavX => navX;
        public static int NavY => navY;

        // ── Acciones ──

        private static bool PadPressed(System.Func<Gamepad, ButtonControl> pick)
        {
            var pad = Pad;
            return pad != null && pick(pad).wasPressedThisFrame;
        }

        private static bool PadHeld(System.Func<Gamepad, ButtonControl> pick)
        {
            var pad = Pad;
            return pad != null && pick(pad).isPressed;
        }

        private static bool MouseLeftPressed => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        private static bool MouseLeftHeld => Mouse.current != null && Mouse.current.leftButton.isPressed;

        private static bool KeyPressed(System.Func<Keyboard, KeyControl> pick)
        {
            var kb = Keyboard.current;
            return kb != null && pick(kb).wasPressedThisFrame;
        }

        private static bool KeyHeld(System.Func<Keyboard, KeyControl> pick)
        {
            var kb = Keyboard.current;
            return kb != null && pick(kb).isPressed;
        }

        // Interactuar con lo que mirás: click, E o X.
        public static bool InteractPressed =>
            MouseLeftPressed || KeyPressed(k => k.eKey) || PadPressed(p => p.buttonSouth);

        // Ponerse la máscara: K o Triángulo.
        public static bool MaskPressed =>
            KeyPressed(k => k.kKey) || PadPressed(p => p.buttonNorth);

        // Golpe de minijuego: click, Espacio, X o R2.
        public static bool ActionPressed =>
            MouseLeftPressed || KeyPressed(k => k.spaceKey) || PadPressed(p => p.buttonSouth) || PadPressed(p => p.rightTrigger);

        // Mantener en minijuego: click, Espacio, X o R2.
        public static bool ActionHeld =>
            MouseLeftHeld || KeyHeld(k => k.spaceKey) || PadHeld(p => p.buttonSouth) || PadHeld(p => p.rightTrigger);

        // "Click" sobre el puntero (botones, pepperoni): click o X.
        public static bool PointerClick => MouseLeftPressed || PadPressed(p => p.buttonSouth);

        // Aceptar en menús: Enter, Espacio, X u Options.
        public static bool ConfirmPressed =>
            KeyPressed(k => k.enterKey) || KeyPressed(k => k.numpadEnterKey) || KeyPressed(k => k.spaceKey) ||
            PadPressed(p => p.buttonSouth) || PadPressed(p => p.startButton);

        // Pausa: Esc, u Options en el control.
        public static bool PausePressed => KeyPressed(k => k.escapeKey) || PadPressed(p => p.startButton);

        // Volver: Círculo.
        public static bool BackPressed => PadPressed(p => p.buttonEast);

        // Ver los créditos: C o Triángulo.
        public static bool CreditsPressed => KeyPressed(k => k.cKey) || PadPressed(p => p.buttonNorth);

        // Saltar escenas (intro, giro): click, Enter, Espacio, X, Círculo u Options.
        public static bool SkipPressed =>
            MouseLeftPressed || KeyPressed(k => k.enterKey) || KeyPressed(k => k.spaceKey) ||
            PadPressed(p => p.buttonSouth) || PadPressed(p => p.buttonEast) || PadPressed(p => p.startButton);

        // Cualquier tecla / botón (para saltar el video de intro).
        public static bool AnyPressed =>
            (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
            MouseLeftPressed || (Pad != null && AnyPadButtonPressed(Pad));

        // Rallar queso: -1 = A / flecha izq / L1, +1 = D / flecha der / R1.
        public static int GrateDirection
        {
            get
            {
                if (KeyPressed(k => k.aKey) || KeyPressed(k => k.leftArrowKey) || PadPressed(p => p.leftShoulder)) return -1;
                if (KeyPressed(k => k.dKey) || KeyPressed(k => k.rightArrowKey) || PadPressed(p => p.rightShoulder)) return 1;
                return 0;
            }
        }

        // Movimiento (WASD + stick izquierdo), magnitud máxima 1.
        public static Vector2 Move
        {
            get
            {
                Vector2 v = Vector2.zero;
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.dKey.isPressed) v.x += 1f;
                    if (kb.aKey.isPressed) v.x -= 1f;
                    if (kb.wKey.isPressed) v.y += 1f;
                    if (kb.sKey.isPressed) v.y -= 1f;
                }
                var pad = Pad;
                if (pad != null)
                {
                    Vector2 s = pad.leftStick.ReadValue();
                    if (s.magnitude > StickDeadzone) v += s;
                }
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        // Stick derecho para mirar (sin zona muerta: devuelve 0 si está quieto).
        public static Vector2 LookStick
        {
            get
            {
                var pad = Pad;
                if (pad == null) return Vector2.zero;
                Vector2 s = pad.rightStick.ReadValue();
                float mag = s.magnitude;
                return mag < StickDeadzone ? Vector2.zero : s.normalized * (mag * mag);
            }
        }

        // ── Textos ──

        // Devuelve el texto que corresponde al dispositivo en uso.
        public static string Label(string keyboardMouse, string gamepad) => UsingGamepad ? gamepad : keyboardMouse;

        // Adapta textos de reglas/instrucciones escritos para teclado y
        // mouse al control ("Click o Espacio" -> "X", "A y D" -> "L1 y R1"...).
        public static string Adapt(string text)
        {
            if (!UsingGamepad || string.IsNullOrEmpty(text)) return text;

            string[,] map =
            {
                { "<b>Click</b> o <b>Espacio</b>", "<b>X</b> o <b>R2</b>" },
                { "click o Espacio", "X o R2" },
                { "Click o Espacio", "X o R2" },
                { "las teclas <b>A</b> y <b>D</b> (o las flechas)", "<b>L1</b> y <b>R1</b>" },
                { "<b>A</b> y <b>D</b> (o las flechas)", "<b>L1</b> y <b>R1</b>" },
                { "dos veces la misma tecla", "dos veces el mismo botón" },
                { "la misma tecla", "el mismo botón" },
                { "Hacé click", "Apretá X" },
                { "<b>Click</b>", "<b>X</b>" },
                { "donde está el mouse", "donde está la rodaja guía (movela con el stick)" },
                { "o presioná ENTER", "o apretá X" },
                { "CLICK para saltar", "X para saltar" },
            };

            for (int i = 0; i < map.GetLength(0); i++)
            {
                text = text.Replace(map[i, 0], map[i, 1]);
            }
            return text;
        }
    }
}
