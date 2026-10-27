# Primera versión de sabotaje con dos jugadores locales

> Actualización: ahora puedes elegir **Individual con Rival automático** o **Multijugador local**, seleccionar apariencias y consultar el recetario 3D. Consulta `MENU_MODELOS_Y_SKINS.md`. Se aprobaron las 88 comprobaciones de estas funciones y las 100 pruebas anteriores.


Abrir el proyecto actualizado y cargar `Assets/Scenes/KitchenScene.unity`.
La escena de cocina se conserva; los dos jugadores y las vistas se preparan al pulsar Play.
`SampleScene` no contiene la cocina jugable.

## Controles

| Acción | Tomás | Rival |
|---|---|---|
| Moverse | WASD | IJKL |
| Interactuar y usar escaleras | E | O |
| Preparar tacle | — | U |

ENTER comienza la partida después de cerrar la introducción. R reinicia al terminar.
F1, F2 y F3 conservan las fichas, el currículum y la historia. La rueda del ratón cambia el zoom de ambas vistas.
Cada jugador tiene su propia mano, objetivo de interacción y cámara.
El Rival usa el mismo modelo de garzón que Tomás y una pequeña escarapela violeta para distinguirlo.

## Probar el sabotaje

1. Tomás recoge un ingrediente con E. También sirve un plato terminado de la mesa de armado.
2. El Rival se acerca a menos de 3,5 metros, mira hacia Tomás y presiona U.
3. Se muestra una advertencia durante 1,5 segundos. El Rival puede acercarse lentamente durante ese aviso.
4. Después embiste durante 0,25 segundos. Tomás puede alejarse o esquivar.
5. Si existe contacto físico válido, desaparece exactamente el objeto apuntado, Tomás recibe un empujón y queda interrumpido 0,6 segundos.
6. El trabajador queda protegido durante 3 segundos. El Rival debe esperar 25 segundos desde el inicio del intento para volver a atacar.
7. Si Tomás deja, entrega o cambia el objeto, el intento anterior queda invalidado. Volver a recoger el mismo objeto también lo invalida.
8. Una pared impide iniciar el ataque y un obstáculo físico detiene la embestida. El tacle requiere cercanía y no atraviesa muebles.
9. El impacto deja restos de comida durante 30 segundos, con referencia interna al atacante, la víctima y el objeto perdido.
10. Perder comida no descuenta puntos ni registra un pedido perdido. Se puede preparar un reemplazo antes del vencimiento normal.

El Rival humano elige cuándo atacar; no se le obliga a sabotear mediante azar.
Los avisos aleatorios anteriores quedan desactivados. Esta versión no agrega una IA de persecución.
El aviso se comparte en la pantalla; no muestra barras de sospecha ni culpabilidad.

## Valores y archivos

Los valores del ataque se pueden ajustar en el componente `TackleSabotage` del Rival durante Play.
Los valores iniciales están en `Assets/Scripts/Player/TackleSabotage.cs`.
El montaje local está en `Assets/Scripts/Core/LocalMultiplayer.cs`.
La recogida, salida e identificación de objetos está centralizada en `PlayerController`.
Al reiniciar se limpian manos, rastros, aturdimiento, protección y espera del ataque, y ambos jugadores vuelven a sus posiciones iniciales.
Volver de una pausa conserva el tiempo y los puntos.
Se conservaron los tiempos de cocina y los modelos de platos existentes.

## Verificación

La compilación de los scripts se comprobó por separado contra las bibliotecas instaladas de Unity 6000.6.0f1 y los paquetes disponibles.
Tras activar la licencia, se ejecutó una prueba dentro de Unity en una copia temporal de ruta corta: pasaron 100 comprobaciones de multijugador, cocina, sabotaje e investigación. Consulta `PRUEBAS_UNITY.md`. La revisión visual y el balance manual permanecen pendientes.

Hay una comprobación de ejecución preparada en `Assets/Scripts/Editor/LocalSabotageValidation.cs`.
Ejecutarla en un proceso separado de Unity, con el editor principal cerrado y una licencia activa:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'RUTA_ABSOLUTA_DEL_PROYECTO' -executeMethod LocalSabotageValidation.Run -logFile 'RUTA_ABSOLUTA_DEL_LOG'
```

La comprobación abre KitchenScene, entra en Play, comprueba controles simultáneos, inventarios, cámaras, pausa, impactos, obstáculos, cambio de objeto, escape, rastros y reinicio. Termina su propio proceso del editor al finalizar. El resultado se guarda en `Logs/local-sabotage-result.txt`.

## Siguiente fase

La patrulla, las denuncias, las expulsiones y la victoria por tercera expulsión se agregaron en la fase siguiente. Consulta `CAPIBARA_E_INVESTIGACION.md`. Las otras condiciones de victoria asimétricas siguen pendientes de atribución y balance.
Las imágenes de comida y sabotaje aportadas se conservan como referencia; todavía no se convierten en modelos ni en sabotajes de sal, quemado o enfriamiento.
