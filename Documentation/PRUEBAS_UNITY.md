# Resultado de las pruebas en Unity

> Actualización: ahora puedes elegir **Individual con Rival automático** o **Multijugador local**, seleccionar apariencias y consultar el recetario 3D. Consulta `MENU_MODELOS_Y_SKINS.md`. Se aprobaron las 88 comprobaciones de estas funciones y las 100 pruebas anteriores.


Fecha: 27 de octubre de 2026.
Editor: Unity 6000.6.0f1.
Resultado: 100 comprobaciones automáticas aprobadas dentro de Unity, sin errores de compilación ni fallos en las comprobaciones del juego.

La prueba se ejecutó en una copia temporal con una ruta corta para no interferir con el editor abierto del usuario ni con los archivos de caché bloqueados del proyecto original. Se usaron los scripts y las escenas del proyecto actualizado. No se guardaron modificaciones de la escena durante las pruebas.

## Qué se comprobó

- Dos personajes, controles WASD e IJKL independientes y dos cámaras.
- Inventarios separados y límite de un objeto transportado.
- Tacle con contacto físico, aviso, obstáculo, protección y espera entre ataques.
- Cambiar, dejar o volver a recoger comida invalida el objetivo del ataque anterior.
- Escapar permite conservar el plato. Perder un ingrediente no cuenta como pedido perdido.
- Cocción y picado con sus temporizadores y recuperación del ingrediente preparado.
- Plato limpio, armado y entrega de Plato Paceño, Sándwich de Chola, Api con Pastel y Ensalada de Frutas, con pedidos completados y puntuación.
- Importación del modelo del garzón y de los cuatro modelos de platos.
- Aparición del Capibara y navegación en el mercado real.
- Denuncia sin pruebas, penalización y protección contra penalizaciones repetidas.
- Persecución física y captura en cada una de las tres expulsiones.
- Restricciones del Rival mientras está detenido, regreso y espera del ataque.
- Pausa, victoria por tercera expulsión y reinicio de la partida.

## Archivos gráficos recuperados

Se encontraron 28 archivos de modelos, animaciones e imágenes que contenían únicamente referencias de Git LFS. Se recuperó el contenido original desde el repositorio y la copia local disponible, y se verificó el tamaño y SHA-256 de cada archivo antes de instalarlo en el proyecto actualizado. Se conservaron sus archivos meta.

## Límites de la prueba

La ejecución fue automática y sin representación gráfica en pantalla. No sustituye la revisión visual de iluminación, materiales, interfaz, encuadre y animaciones, ni una partida manual con dos personas para ajustar la dificultad.
Unity registró una excepción interna de su índice de búsquedas (`UnityEditor.Search.SearchDatabase`). No pertenece a los scripts del juego y no impidió completar las 100 comprobaciones; queda como incidencia del editor.
Los errores de paquetes y caché de la carpeta larga abierta del usuario se observaron por separado; el resultado aprobado corresponde a la copia temporal de ruta corta. La carpeta original recibe los archivos gráficos recuperados y la prueba reproducible, pero su editor puede necesitar terminar la reimportación.
Las condiciones de victoria adicionales por meta de pedidos y por pedidos arruinados siguen pendientes de implementación y balance.

El menú `Mercado > Probar multijugador y sabotaje` permite repetir la prueba desde el editor. Guarda antes los cambios de la escena. La prueba entra en Play y lo detiene al terminar; no cierra el editor interactivo. El resultado se guarda en `Logs/local-sabotage-result.txt`.
