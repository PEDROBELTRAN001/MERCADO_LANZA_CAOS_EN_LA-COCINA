# Pruebas de la integración sobre presentacion_3

Unity 6000.6.0f1. Base: `8efe18d196cf55d9a9bf75a1fa1165dca9c50cf6`.

- 386 comprobaciones del flujo de historia/currículum/fichas, fotos, apariencias, controles simultáneos, cámaras, manos, estaciones compartidas, cocción y sabotajes de humo/aceite.
- 9 capturas verificadas de las pantallas de entrada, el menú y una partida local. Se revisaron visualmente las imágenes.
- 47 comprobaciones de evidencias y policía: denuncia sin prueba, evidencia atribuida al Rival humano o automático, persecución física, suspensión, protección de Tomás, pausa, regreso y rechazo de pruebas consumidas.
- Compilación de scripts de juego y editor comprobada antes de publicar.

Las pruebas se ejecutaron en una copia independiente. Una comparación por hash confirmó que las escenas, los modelos originales, las recetas, las estaciones, los efectos de sabotaje y la configuración del proyecto conservan su contenido de la rama estable.

Archivos existentes adaptados: `PlayerController.cs`, `FloatingWindows.cs`, `GameHUD.cs` y `SabotageSystem.cs`. El resto de la entrega son componentes y recursos complementarios. El único modelo nuevo es el capibara aprobado y sus dependencias.

El primer intento de la prueba de teclado utilizó una llamada interna con una firma no pública y produjo un aviso de compilación. La llamada se corrigió en el archivo de pruebas y la versión publicada recompila y supera las verificaciones. La simulación de teclas usa temporalmente la configuración de entrada del editor y la restaura al terminar.
