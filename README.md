# Baking Pizza

Entorno virtual interactivo en primera persona, parodia de *Breaking Bad*. Jesse fue secuestrado y los secuestradores piden una pizza a cambio de su vida: el jugador tiene que cocinarla en el laboratorio al que se entra por la casa rodante, con la pureza suficiente para rescatarlo.

**UTN FRC · Creatividad y Desarrollo de Entornos Virtuales (CDEV) · Curso 5K4 · Grupo 6**

**Profesores:** Ing. Inés Gimenez, Ing. Franco Mana, Ing. Bruno Aresu Barella

## Equipo

| Área | Integrantes |
|---|---|
| Liderazgo y Dirección | Julieta Guzmán |
| Diseño de Experiencia e Interacción | Lourdes Osella Huber, Arian Ocampo Vysin |
| Programación y Desarrollo Técnico | Juan Cruz Patiño, Matías Palomeque Galindo |
| Arte y Diseño Sonoro | Carolina Jaureguialzo |
| Diseño Narrativo y Guion | Abel Castillo, Bautista Mandrilli |

## Cómo jugar

**Opción 1, sin instalar Unity:** descargar el ejecutable para Windows desde la sección *Releases* del repositorio, descomprimirlo y abrir el `.exe`.

**Opción 2, desde el proyecto:**
1. Instalar Unity Hub y el editor **Unity 6000.6.0f1**.
2. En Unity Hub: *Add project from disk* y elegir la carpeta del repositorio. La primera apertura tarda unos minutos porque Unity regenera la carpeta `Library`.
3. Abrir la escena `Assets/Scenes/MainMenu.unity` y apretar **Play**.

## Recorrido

1. **Menú**: pantalla de título, elección de dificultad (Fácil 40 %, Medio 60 %, Difícil 80 % e Imposible 97 % de pureza mínima; además cambia el ritmo de los minijuegos) y pantalla de controles.
2. **Desierto de Nuevo México**: exploración en primera persona hasta encontrar la casa rodante.
3. **Laboratorio**: el jugador se pone la máscara y Walter White le revela el giro: hay que cocinar una pizza para salvar a Jesse.
4. **Cocina**: cinco minijuegos (amasado, dosificación de salsa, rallado de queso, armado con pepperoni y horneado), con cronómetro y pureza en vivo.
5. **Informe final**: pureza, veredicto del rescate, rango, tiempo y récord por dificultad.
6. **Créditos finales**.

## Controles

| Acción | Teclado y mouse | Joystick |
|---|---|---|
| Caminar / mirar | W A S D / mouse | Stick izquierdo / derecho |
| Interactuar | E o click | X |
| Ponerse la máscara | K | Triángulo |
| Minijuegos | Click / Espacio (A y D para rallar) | X / R2 (L1 y R1 para rallar) |
| Pausa y ayuda | Esc | Options |

## Estructura del código (`Assets/Scripts`)

| Carpeta | Contenido |
|---|---|
| `Core` | Etapas del juego (`GameManager`), dificultad, pureza y cronómetro (`PizzaScore`), récords, cielo del desierto, filtro de color y límites del laboratorio. |
| `Player` | Controlador en primera persona y brazos del jugador. |
| `Interaction` / `Interactables` | Detección de objetos interactuables, puerta de la casa rodante, máscara y easter egg. |
| `Kitchen` / `Minigames` | Mesadas de la cocina y los cinco minijuegos. |
| `UI` | Menú, HUD, giro narrativo, informe final, créditos, pausa y ayuda. La interfaz se genera por código. |
| `Audio` | Música por etapa y efectos de sonido, sintetizados por código. |
| `Controls` | Entrada unificada de teclado, mouse y joystick (Input System). |
| `Editor` | Herramientas de editor (colisiones y limpieza de escena). |

Tecnología: Unity 6 (6000.6.0f1), Universal Render Pipeline, C#, Input System, TextMeshPro y glTFast.

## Créditos de terceros

Modelos 3D de Sketchfab bajo licencia [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/):

- *Breaking Bad 1986 Fleetwood Bounder RV*, de VectorMan
- *Kitchen And Lab by Amogusstrikesback2*, de @sanyabeast
- *Breaking Bad Hazmat Mask Style*, de FAKlR
- *Walter White Lab Suit, Bryan Cranston*, de 00amza
- *LosPollosHermanos bucket*, de Batuhan13

Entorno del desierto: *Low Poly Desert Environment*, de Tiny Teacup Studio.

*Breaking Bad* fue creada por Vince Gilligan (AMC, 2008–2013). Los créditos finales usan escenas de la serie y su tema de cierre (Dave Porter). Este proyecto es una parodia sin fines de lucro, realizada con fines académicos.
