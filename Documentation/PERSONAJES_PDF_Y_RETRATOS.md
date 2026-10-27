# Personajes del PDF y fotos de entrada

Referencia: `PRIMER_ENTREGABLE_GRÁFICA .docx.pdf`, fichas de personajes de las páginas 6 a 12. El usuario pidió usar las proporciones de cada referencia, en lugar de igualar todas las siluetas.

| Personaje | Referencia aplicada |
|---|---|
| Tomás | Figura ovalada/chibi, sonrisa, sombrero negro de copa baja con cinta roja y pluma, camisa blanca, chaleco gris y base/botines negros. Bandeja metálica decorativa. |
| Rival | Misma proporción que Tomás, chaleco gris más oscuro, corbata negra y boca recta. Se conservan las manchas del sabotaje y un pequeño distintivo violeta. |
| Doña Rosa | Figura más baja y ancha que el garzón, cofia blanca, barbijo, dos trenzas, mandil blanco sobre ropa gris, aguayo de colores y cucharón metálico. |
| Cholita Paceña | Bombín café, blusa morada, mandil azul con bolsillos, pollera en capas naranja/amarilla/verde, pañuelo/aguayo y canasta con papas y chuño. |
| Capibara policía | Diseño erguido y ovalado ya aprobado: uniforme verde oliva, gorra con banda roja y dorada, cuello blanco, placa, radio, silbato y bandeja de inspección. |

Los modelos se crearon como mallas 3D; no se extrajeron recortes de las ilustraciones para usarlos como personajes. Las proporciones se conservan al ajustarlos al espacio de movimiento del jugador. Brazos y piernas se articulan al caminar y al transportar comida.

## Fotos de cada personaje

Se guardan diez PNG renderizados por Unity en `Assets/Resources/CharacterPortraits`: cinco fotos de cuerpo y cinco de rostro. El currículum y las fichas usan los retratos de rostro; el selector utiliza las fotos de cuerpo. Estas imágenes se cargan como recursos al entrar y funcionan sin crear una cámara de retrato durante la introducción.

La introducción incluye cinco fichas, incluida la Cholita. El currículum tiene un área desplazable y márgenes corregidos para leer la información en ventanas pequeñas.

La opción `Mercado/Generar fotos de los personajes` regenera los retratos después de editar los modelos. `CharacterArtBuilder.BuildDocumentCharacters` reconstruye los modelos y las fotos con el motor gráfico activo.

## Alcance

Se actualizan diseño, proporciones, fotos y presentación. Las fichas del PDF también describen comportamientos de PNJ, sabotajes y recetas que no son instrucciones para implementarlos en esta tarea. No se añadieron nuevas reglas de cocina, FOV, misiones, diálogos o comportamientos de clientes. Los roles, el modo individual, el multijugador local y el tacle existentes se mantienen.

Las capturas reales del currículum, fichas y selector están en `Documentation/Visuals/Entrada_*.png`; la lámina de modelos está en `Visuals/PersonajesPDF.png`.
