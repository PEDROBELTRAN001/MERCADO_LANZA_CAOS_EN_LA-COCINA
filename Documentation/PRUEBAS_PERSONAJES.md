# Pruebas de modelos del PDF y retratos

Fecha: 27 de octubre de 2026. Unity 6000.6.0f1.

| Prueba | Resultado |
|---|---|
| DocumentInterfaceValidation.Run, con motor gráfico y vista de juego | PASS: 25 comprobaciones. Diez fotos persistentes, carga desde el selector, proporciones de Tomás/Rival y Doña Rosa, cinco fichas y capturas reales de las pantallas. |
| CharacterDesignValidation.Run, en modo Play | PASS: 255 comprobaciones. Rasgos de los modelos del PDF, mallas y referencias, selección, ajuste de tamaño, manos, caminata, carga y manchas. |
| MenuFeatureValidation.Run, en modo Play | PASS: 88 comprobaciones. Menú, recetario, modos individual/local, selección e IA del Rival. |
| LocalSabotageValidation.Run, en modo Play | PASS: 104 comprobaciones. Cocina, ingredientes, platos, controles independientes, tacle, evidencias, denuncia, persecución y expulsiones. |

Se compilaron los scripts de juego y editor. Se revisaron visualmente los modelos, los cinco retratos en sus fichas y la foto de Tomás en el currículum, usando capturas reales de Unity. La prueba de pantallas se ejecutó con el editor normal, porque las capturas de la interfaz requieren una vista de juego que se renderice.

Los recursos se verificaron por GUID y los archivos instalados se compararon con la versión validada. Las pruebas usan la copia temporal de ruta corta; no guardan cambios en la escena original.

La instalación del editor conserva una excepción previa de UnityEditor.Search.SearchDatabase al iniciar el índice. No impidió estas pruebas. El balance y la comodidad del juego se siguen evaluando con partidas manuales.
