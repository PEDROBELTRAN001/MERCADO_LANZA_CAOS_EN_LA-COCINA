# Cocina de Doña Rosa

Abrir Assets/Scenes/KitchenScene.unity. La estación está guardada en la misma escena, bajo COCINA_DOÑA_ROSA. No necesita ejecutar un generador al iniciar el juego.

## Contenido

### Ampliación visual de la cocina principal

La instancia guardada en `KitchenScene` tiene un suelo de 13,8 m de ancho (antes 12 m), ampliado hacia el oeste conservando la entrada este. El fogón principal es un 15 % más ancho y un 10 % más profundo; las tres hornallas y sus puntos de interacción se escalan junto con el mueble. La olla y su mesa se desplazaron hacia el oeste para separarlas del fogón.

La tabla de picar mide 2,16 × 1,44 m y la mesa de armado 1,44 × 2,07 m, conservando sus alturas. El mesón café del oeste se alineó con el borde ampliado (centro x = −16,2; borde x = −16,8), trasladando juntos lavaplatos, grifo y platos decorativos. La tabla de picar queda junto a ese mesón en x = −14,45 y z = −2,4, fuera del frente de las hornallas. La separación horizontal respecto al borde izquierdo del fogón es de aproximadamente 1,10 m y respecto a la mesa de la olla de 1,36 m. Los rótulos acompañan los desplazamientos.

La zona de cámara de esta cocina usa un ángulo de 48° y zoom ortográfico 5, con un área de activación más ancha: permite ver mejor las superficies de preparación. Los demás puestos y sus cámaras conservan sus ajustes. Se modificó la escena existente; no se regeneraron los prefabs ni el mercado. Ejecutar el generador original puede reemplazar estos ajustes manuales.

Comprobación realizada: referencias de la escena conservadas, acceso este sin desplazamiento y separación horizontal entre las mesas de preparación y los muebles contiguos. Falta revisar el encuadre y recorrer los pasillos en Play Mode.

### Recursos de la cocina

La opción **Mercado > Acomodar cocina y seleccionar cambios** aplica la distribución revisada directamente en la escena abierta, con Undo y selección de objetos. Mueve el mesón inferior izquierdo y sus dos platos al lado derecho; coloca la tabla de picar en el espacio liberado; lleva la barra de pedidos con sus dos platos a la esquina superior derecha; y alinea los once insumos, incluidas las frutas, en z = 4,2. Conserva las alturas de los insumos y mueve sus objetos completos, aunque estén bajo otros padres.

El lavaplatos pasa a medir 1,6 × 2,3 m, con pileta, grifo, agua y platos más visibles. Se desactiva `Counter_W_1`, que era un mesón sin interacción superpuesto a la misma altura que el lavaplatos, y se separa la superficie del agua de la del acero. Esto elimina esas superficies coincidentes; debe comprobarse el resultado visual en Play Mode.

`Counter_N_0`, el mueble café situado delante del estante del fondo, es decorativo: no contiene un componente de interacción. La opción lo conserva. Tras aplicar la distribución, guardar con Ctrl+S; Ctrl+Z permite deshacer el ajuste completo. Volver a ejecutar la opción aplica las mismas coordenadas sin sumar desplazamientos.

- Fogón industrial: tres hornallas con CookingStation, perillas visuales, puntos de colocación y vapor durante la cocción.
- Estante con cajas de papa, choclo y carne.
- Barra con KitchenCounter para dejar y recoger objetos desde Hand.
- Olla hueca, sartén, cajas y saco de papa.
- Modelos decorativos de plato paceño y sándwich de chola. No agregan recetas nuevas al sistema de pedidos.

Los ocho prefabs están en Assets/DonaRosa/Prefabs. Los materiales y mallas low-poly están en las carpetas Materials y Meshes del mismo directorio. Todos pueden editarse en Unity.

## Generación
Mercado > Integrar cocina de Dona Rosa reemplaza solamente la estación generada y los muebles provisionales que ocupa, en la escena activa. Guardar después con Ctrl+S. Al regenerar se pierden los ajustes manuales hechos en esos objetos generados; personalizar una copia si se quieren conservar variantes.
Mercado > Build Complete Scene conserva su función de reconstruir todo el mercado y ahora incluye la estación.

La pared sur de la cocina queda baja para ver el fogón desde la cámara. La mesa de preparación se desplaza para dejar un pasillo frente a las hornallas. El patio de comidas, jugador y estaciones existentes permanecen en el escenario.

## Alcance de las interacciones
Las tres hornallas reutilizan CookingStation y el sistema actual de ingredientes. Las perillas y los platos bolivianos son modelos visuales; no se añadió sabotaje, apagado con humo ni nuevas reglas de recetas. La barra funciona como superficie de intercambio, no como entrega automática de puntos.

Se corrigió KitchenCounter para devolver el objeto a Hand, impedir recoger con la mano ocupada y limpiar su referencia. El generador configura la máscara de interacción del jugador.

## Verificación
DonaRosaBuilder.BuildAndSave abre KitchenScene, integra dos veces para comprobar regeneración, valida las tres hornallas, referencias y materiales, comprueba acceso mediante raycasts y prueba colocar/recoger en la barra. Guarda la escena y genera Documentation/CocinaDonaRosa.png.
El registro de la ejecución queda en Logs/DonaRosaBuild.log. Estas verificaciones de editor no sustituyen una partida completa en Play Mode.

## Ajuste de huecos entre muebles (11 de noviembre de 2026)

Se retira de la vista `Counter_N_0` desactiv?ndolo junto con su colisi?n. La tabla de picar se extiende desde el borde oeste hasta las hornallas (centro x = -14,5375; ancho 4,525 m); el mes?n oeste inferior llega a su borde posterior (profundidad 2,76 m). La mesa y cubierta de la sart?n se extienden desde las hornallas hasta el borde este (centro x = -4,7625; ancho 3,525 m), con la sart?n centrada. Estos ajustes reemplazan las medidas anteriores de esas piezas y est?n guardados en KitchenScene y KitchenLayoutTool. Se comprobaron num?ricamente las cuatro uniones; falta revisi?n visual en Play Mode.

El objeto redondo claro detr?s de los insumos es el saco de papas de `06_Cajas_Sacos`: junto con sus cajas es decoraci?n, independiente de las cajas interactivas de ingredientes.

## Correccion: desplazar hornallas, no ensanchar picado

La tabla conserva 2,16 m de ancho en x=-14,2. Las hornallas se desplazan a x=-10,245, tocando su borde derecho. Olla (x=-6,2775) y sarten (x=-4,0925) quedan a su derecha, todas en z=-5,1; las dos mesas miden 2,185 m de ancho. Este ajuste reemplaza el ensanchamiento anterior. KitchenLayoutTool aplica tambien estas coordenadas. La solicitud puntual Library/KitchenRowLayout.request actualiza y guarda la escena cargada al volver a modo edicion y luego se elimina.
