# Modos de juego y modelos 3D

## Antes de jugar

Abrir `Assets/Scenes/KitchenScene.unity` y pulsar Play. Se conserva la introducción con la historia, el currículum de Tomás y las fichas de personajes. Al terminarla o saltarla con Esc aparece el menú previo.

- **Jugar individual**: Tomás se controla con WASD y E. El Rival se mueve y decide sus intentos de sabotaje automáticamente. Se usa una vista de pantalla completa.
- **Multijugador local**: Tomás usa WASD y E; el Rival usa IJKL y O, y U para iniciar el tacle. Se usan dos vistas de cámara.
- **Apariencias**: las flechas de cada personaje recorren Tomás, Rival, Doña Rosa, Cholita Paceña y Capibara. La apariencia es cosmética; el rol y las habilidades del participante se mantienen.
- **Ver platos e ingredientes**: abre el recetario antes de comenzar. Cada plato muestra un modelo 3D, los ingredientes que debes seleccionar y sus pasos de preparación.
- **Comenzar partida** o Enter inicia el modo seleccionado. Modo y apariencia se eligen antes del turno.

Durante la partida, F4 o el botón Recetario permite consultar los modelos y pausa el reloj, los personajes y la IA. Esc o F4 cierra el recetario y reanuda el turno. F1, F2 y F3 conservan el acceso a personajes, currículum e historia. Al terminar una partida se puede volver al menú para elegir otro modo.

## Modelos hechos para el juego

Las imágenes facilitadas se usaron como referencia de formas, colores y presentación. No se recortaron fotografías ni se pegaron imágenes en los alimentos. Los modelos se construyeron con mallas 3D y materiales de color, y sus piezas se combinaron para reducir el número de renderizados.

Se crearon modelos de tomate, lechuga, pan de chola, cerdo con cuerito, queso, papa, choclo, haba, maíz morado, masa y fruta. Cada ingrediente tiene cuatro versiones de estado: original, picado, cocinado y picado/cocinado. Son 44 prefabs de ingredientes y estados.

Los modelos aparecen como muestras sobre las cajas, al transportar ingredientes, al cocinarlos o picarlos y en las vistas del recetario. El api cocinado utiliza una taza de bebida morada; la masa cocinada se presenta como pastel dorado con azúcar; la fruta picada cambia a una porción preparada.

También se crearon platos 3D de Sándwich de Chola, Api con Pastel, Plato Paceño y Ensalada de Frutas para reemplazar su presentación al terminar el armado. La salteña se incluye como modelo adicional basado en la referencia; todavía no forma parte de las recetas ni de los pedidos jugables.

Los prefabs están en `Assets/Resources/FoodModels/Ingredients` y `Assets/Resources/FoodModels/Dishes`. Sus mallas están en Geometry y sus materiales en Materials. Se mantienen los modelos anteriores del proyecto como archivos originales.

## Apariencias

Los cinco modelos siguen las fichas visuales del primer entregable (páginas 6 a 12): Tomás con sombrero negro, cinta roja y pluma, camisa blanca y chaleco gris; Rival de la misma proporción con corbata negra, chaleco más oscuro y gesto serio; Doña Rosa más baja y ancha, con cofia, barbijo, mandil y aguayo; Cholita con bombín, mandil azul, pollera naranja/amarilla/verde y canasta; y capibara policía redondo con uniforme oliva y equipo de inspección.

Las fotos de rostro y cuerpo se guardan en `Assets/Resources/CharacterPortraits`. El currículum, las cinco fichas de introducción y el selector cargan estas imágenes directamente. El currículum permite desplazarse para leer toda la información en ventanas pequeñas.

Los personajes conservan su rol y sus colisiones. Los objetos decorativos de las manos dejan espacio al ingrediente transportado. El impostor conserva las manchas al cambiar de apariencia y se limpia al reiniciar o regresar de una expulsión.

Ver `PERSONAJES_PDF_Y_RETRATOS.md` y `Visuals/PersonajesPDF.png`.

## Rival automático

La IA evalúa la posibilidad de sabotaje una vez al recoger un ingrediente o plato terminado. Su probabilidad inicial es del 40 %, con cinco segundos iniciales de gracia y entre 22 y 32 segundos de descanso entre evaluaciones. Usa rutas por el mercado y el mismo tacle que el Rival humano.

Si el trabajador deja, cambia o entrega el objeto, se cancela la persecución correspondiente. El Rival puede ser denunciado y expulsado también en modo individual. No se otorga acceso a sus controles IJKL/O/U durante una partida individual.

## Validación

Se ejecutaron en Unity 6000.6.0f1:

- 88 comprobaciones del menú, los modos, el recetario, las cinco apariencias, los modelos y el sabotaje automático.
- 255 comprobaciones de personajes finales, mallas persistentes, manos, animación y estados visuales.
- 104 comprobaciones de regresión (las 100 previas más cuatro sobre las manchas) de cocina, multijugador, tacle, investigación, persecuciones y expulsiones.
- Renderizado y revisión visual de los modelos de ingredientes, platos y skins.

Las pruebas se hicieron en la copia temporal de ruta corta para no interferir con el editor abierto del proyecto original. La carpeta actualizada recibe los scripts, prefabs, mallas, materiales y catálogo listos para importar. Las guías y las vistas de los modelos están en Documentation.

Continúan pendientes el balance con partidas manuales y las condiciones de victoria adicionales por pedidos correctos o arruinados. Los requisitos y tiempos de las cuatro recetas jugables se conservaron.
