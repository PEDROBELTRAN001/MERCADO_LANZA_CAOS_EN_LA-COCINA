# Integración puntual de menú y modos

Base: rama `presentacion_3`, commit `8efe18d196cf55d9a9bf75a1fa1165dca9c50cf6`.

Esta entrega complementa el proyecto estable con historia, currículum, cinco fichas con foto y descripción, selección de apariencia y elección de modo individual o multijugador local. Se conserva la decoración andina de las ventanas existentes.

## Jugar

Abrir `Assets/Scenes/KitchenScene.unity` y pulsar Play. Avanzar la historia, el currículum y las fichas o saltarlos con Esc. El menú permite volver a consultar esas pantallas y elegir el modo y las apariencias antes del turno.

| Participante | Movimiento | Interacción | Correr |
|---|---|---|---|
| Tomás / jugador 1 | WASD o flechas | E | Shift izquierdo |
| Rival / jugador 2 local | IJKL | O | Shift derecho |

En individual se usa una cámara completa y el Rival tiene desplazamiento automático. Los sabotajes automáticos siguen a cargo del sistema original y conservan su ritmo.

En multijugador local hay dos cámaras y manos independientes. El Rival usa U junto a un fogón o estación para activar el humo/aceite que ya existe en el proyecto. Tiene 20 segundos de espera entre usos; el jugador 1 no recibe ese control. Las estaciones y recetas son compartidas y se conservan.

F1 vuelve a abrir las fichas, F2 el currículum y F3 la historia. Estas pantallas pausan el turno. Al terminar, R vuelve al menú para elegir la siguiente partida.

## Recursos y compatibilidad

Las fotos se guardan en `Assets/Resources/CharacterPortraits`. El catálogo del selector está en `Assets/Resources/MenuIntegration`. Los prefabs originales de `Assets/Characters` se conservan; para la selección se usan copias auxiliares que resuelven referencias de malla/material ausentes sin reconstruir sus formas.

Tomás conserva `TomasAvatar`, su vestimenta y sus animaciones originales. El controlador de la cámara principal sigue siendo el de la rama estable; la segunda cámara tiene un adaptador independiente.

Se conservan escenas, recetas, paquetes, ajustes del proyecto, lógica de estaciones y modelos originales. `SabotageEffects` se mantiene intacto. `SabotageSystem` incorpora una señal del responsable para registrar evidencia y omite nuevos sabotajes mientras el Rival está suspendido; los efectos de humo/aceite y sus tiempos originales se conservan.

## Capibara, evidencias y suspensión

Esta entrega incluye al capibara como policía: usa el modelo aprobado y rutas calculadas desde los colliders existentes. Un sabotaje real genera un rastro atribuido al Rival que lo ejecutó, tanto humano como automático.

Tomás debe acercarse al rastro y examinarlo con E, luego acercarse al capibara y presentar la denuncia con E. El policía comprueba la prueba y busca una ruta hasta el saboteador. Solo al alcanzarlo y tener línea de visión lo suspende durante ocho segundos. Las fichas y la historia pausan esa cuenta; después el Rival reaparece con un tiempo de espera antes de volver a sabotear.

Una denuncia sin evidencia no inicia la persecución. La prueba consumida no puede reutilizarse. Tomás conserva su movilidad y sus ingredientes; su apariencia o proximidad al sabotaje no lo convierten en sospechoso. Si no hay ruta o el Rival escapa, la prueba se conserva para otra denuncia. Reiniciar limpia los incidentes del turno.

En individual, el sistema automático atribuye sus sabotajes al Rival automático y también se detiene durante su suspensión. En multijugador, solo el Rival humano puede activar U. Elegir el aspecto del capibara como jugador no cambia estos roles.
