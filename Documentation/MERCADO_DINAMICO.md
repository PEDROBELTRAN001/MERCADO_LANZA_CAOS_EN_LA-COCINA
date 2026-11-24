# Cámara, plano y vida del Mercado Lanza

Abrir `Assets/Scenes/KitchenScene.unity` y pulsar Play. Los nuevos sistemas se conectan automáticamente al HUD existente; no hace falta regenerar la escena ni ejecutar los constructores del menú Mercado.

## Cámara y orientación

- Encuadre inicial de 5,6 unidades ortográficas, frente a las 8 anteriores. La rueda permite ajustar el zoom.
- La cámara gira suavemente hacia la marcha, anticipa un poco el camino y abre ligeramente el encuadre al correr.
- Sigue al personaje por todo el mercado, sin quedar detenida en los antiguos límites de la cocina.
- Mantener las mismas teclas conserva el rumbo mientras gira la cámara. Al cambiar de teclas se toma la orientación actual de la vista.
- Una flecha turquesa sobre el suelo indica el frente de Tomás; otra flecha en el minimapa indica su posición y orientación.
- En multijugador local cada cámara gira según el movimiento de su propio personaje.

## Minimapa

Plano completo ampliado en la zona inferior derecha, con norte fijo y proporciones reales. El panel pasa de 252 × 308 a 380 × 430 unidades de interfaz; el área del plano también aumenta y su textura pasa de 384 a 512 píxeles. Se genera una vez desde las superficies de navegación conectadas al suelo del jugador: los pasillos se ven claros, y los puestos/obstáculos quedan oscuros. No necesita una cámara adicional ni renderizar el escenario cada fotograma.

Muestra a Tomás en turquesa, a los caseros con puntos discretos y a las mesas con pedidos en dorado. Los potenciadores tienen símbolos propios de hoja verde y sachet amarillo/rojo, junto con los segundos restantes. Debajo del plano se muestran las próximas apariciones, el multiplicador de velocidad y las entregas reservadas.

## Caseros

Se añaden 36 personajes, reutilizando los modelos y la paleta existentes, con distintas prendas y alturas. Seis permanecen junto a su puesto; el resto alterna paseos y pausas, con velocidades y recorridos diferentes. Usan el NavMesh existente y evitan otros agentes. Sus colliders de interacción son triggers para dejar pasar al jugador.

Los pregones se alternan en burbujas visibles cerca de Tomás. Acercarse y pulsar E permite conversar: hay saludos, comentarios sobre La Paz, gastronomía y pistas para las misiones y los potenciadores. El diálogo usa las tarjetas informativas existentes, con altura ajustada al texto.

## Potenciadores exclusivos de Tomás

| Objeto | Aparición desde el inicio del turno | Permanencia | Efecto al pasar por encima |
| --- | --- | --- | --- |
| Hoja de coca | Cada 25 segundos | 10 segundos | Multiplica la velocidad actual por 1,15 durante el resto del turno. |
| Cascadita | Cada 40 segundos | 10 segundos | Entrega automáticamente el pedido pendiente con menos tiempo restante. |

Las ubicaciones se sortean sobre pasillos transitables, con separación de los bordes y espacio libre para recogerlos. El Rival, la IA rival y los caseros no pueden consumirlos. Una recogida solo se contabiliza una vez.

La velocidad se acumula: una hoja ×1,15; dos ×1,3225; tres ×1,520875. El multiplicador también se aplica al correr, sin modificar la velocidad base configurada del controlador.

Cascadita utiliza las reglas de entrega existentes: suma los puntos de la receta, la bonificación de tiempo, la reputación y el progreso VIP correspondiente, y libera la mesa. No consume lo que Tomás lleva en la mano. Si no hay pedidos, guarda una entrega para el próximo pedido; varias recogidas pueden reservar varias entregas.

Pausar o abrir una ventana que bloquea el juego congela los intervalos y la permanencia de los objetos. Reiniciar limpia hojas, objetos, entregas reservadas y temporizadores.

## Audio y radio

Los cinco sonidos suministrados se copian a `Assets/Resources/Audio` y se conectan automáticamente:

| Archivo | Uso |
| --- | --- |
| mercado.mp3 | Ambiente en bucle continuo, también en el menú y al terminar la jornada. |
| campana.mp3 | Cada misión/pedido nuevo. |
| jump.mp3 | Cada potenciador recogido correctamente por Tomás. |
| error.mp3 | Derrota; no se reproduce al ganar. |
| radio_transicion.mp3 | Transición completa antes de cada fragmento musical. |

Las canciones se colocan en **`Assets/StreamingAssets/RadioMusic`**. Ya incluye `Milo j - Ama De Mi Sol.mp3`. Se pueden añadir más MP3 antes de pulsar Play o durante una sesión: la radio vuelve a revisar la carpeta en cada ciclo. En una exportación Windows se usa `<juego>_Data/StreamingAssets/RadioMusic`.

La radio comienza con la partida y se detiene cuando esta termina. Su ciclo es: transición completa → canción aleatoria desde un punto aleatorio durante 20 segundos → transición completa. Con varias canciones evita repetir inmediatamente la anterior. Si un archivo dura menos de 20 segundos, repite ese archivo hasta completar el fragmento. Solo mantiene cargada una canción y la libera al cambiarla.

Radio, fondo y efectos tienen ganancias independientes. El ambiente y los efectos son 2D para que se oigan igual en ambas cámaras del multijugador local. Se mantiene una sola escucha de audio.

## Menú de pausa

**ESC**, **P** o el botón superior derecho abren la pausa. Los botones son **Reanudar**, **Volver a jugar** y **Volúmenes**. El reinicio conserva el modo y las preferencias de sonido, y restablece reloj, pedidos y bonificaciones.

Volúmenes ofrece un interruptor de activación y un deslizador de 0 a 100 % para **Música radio**, **Fondo** y **Efectos**. Silenciar conserva el volumen elegido. Los ajustes se guardan entre sesiones. ESC vuelve desde volúmenes al menú de pausa; ESC/P en el menú principal reanudan la partida.

La pausa congela las fuentes de audio, el punto de reproducción, el fragmento de radio, el reloj y los potenciadores. Abrir historia/fichas también pausa el audio. Las ventanas de fichas no se abren por detrás del menú de pausa.

## Ajustes y verificación

- `CameraFollow`: `framingScale`, `turnSmoothTime`, `maxTurnSpeed`, `lookAhead` y `followWholeMarket`.
- `MarketLife`: `pedestrianCount`; constantes `CocaInterval`, `CascaditaInterval` y `PickupLifetime`.
- Pruebas de integración: ejecutar Unity con `-batchmode -projectPath "<ruta del proyecto>" -executeMethod MarketLifeValidation.Run -logFile Logs/market-life-validation.log`, sin `-quit`. El comprobador cierra su propia sesión al terminar y guarda el resultado en `Logs/market-life-result.txt`.
- Las capturas de revisión se guardan en `Logs/market-life-single.png`, `Logs/market-life-reset.png` y `Logs/market-life-powerups.png`.
- Audio/pausa: ejecutar `MarketAudioValidation.Run` del mismo modo, sin `-quit`; resultado en `Logs/market-audio-result.txt`. Capturas en `Logs/market-pause.png`, `Logs/market-volumes.png` y `Logs/market-larger-minimap.png`.

## Verificación realizada

Audio, pausa y minimapa ampliado: **53 comprobaciones superadas** en Unity 6000.6.0f1. Incluyen la carga del MP3 local y su punto de inicio aleatorio, los fragmentos de veinte segundos, el retorno a la transición, la liberación de cada canción, la campana por misión, el sonido de recogida, la derrota sin falso aviso al ganar, los tres controles de volumen, la persistencia de ajustes, ESC/P, reinicio y la permanencia de diez segundos. No se registraron errores de ejecución durante estas pruebas.

Se revisaron el menú de pausa, los volúmenes y el minimapa en 1280 × 720. En el segundo pase se corrigieron el título recortado y los adornos que desbordaban el panel, y se distinguieron los deslizadores con el color dorado del HUD. La leyenda norte y el pie del plano también conservan espacio para la tipografía ampliada.

Compilación en Unity 6000.6.0f1 e integración en la escena original: **67 comprobaciones superadas**. Incluyen las 36 posiciones de peatones, marcha y puestos quietos, conversación, exclusión del Rival, recogida única y fuera de alcance, velocidad acumulada, apariciones, caducidad, pausa, puntuación/VIP, entregas reservadas, reinicio y giro de ambas cámaras. La comprobación del teclado confirma que mantener una tecla no provoca giros circulares.

Se revisaron las tres capturas a 1280 × 720. Se ajustaron el límite mínimo de zoom y la posición de la flecha para que el encuadre cercano y el rumbo se vean durante el juego. La captura de potenciadores muestra la hoja, ambos símbolos con cuenta atrás, un pedido y la burbuja de conversación.

Revisión visual limitada a los elementos modificados:

- UI intentionality: 3/5 — plano legible, proporciones conservadas, leyenda y franja andina, sin tapar la interacción inferior.
- Feedback visibility: 3/5 — rumbo visible en el mundo y en el mapa, hoja reconocible, cuentas atrás y conversación visibles.

Se conservaron la cámara propia, IMGUI y la navegación del proyecto, sin añadir paquetes. Referencias consultadas: guías Unity `camera-rigs`, `create-ui`/`imgui-skill`, `visual-scorecard` y `unity-cli`/`build-run-test`; documentación de AI Navigation 2.0 (`AboutAgents`, `MixingComponents`, `NavAgentPatrol`), y API de Unity 6000.6 [CalculateTriangulation](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/AI.NavMesh.CalculateTriangulation.html), [SmoothDampAngle](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Mathf.SmoothDampAngle.html) y [personalización de IMGUI](https://docs.unity3d.com/6000.6/Documentation/Manual/gui-Customization.html).
