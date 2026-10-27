# Investigación y expulsiones con el Capibara

> Actualización: ahora puedes elegir **Individual con Rival automático** o **Multijugador local**, seleccionar apariencias y consultar el recetario 3D. Consulta `MENU_MODELOS_Y_SKINS.md`. Se aprobaron las 88 comprobaciones de estas funciones y las 100 pruebas anteriores.


Esta fase agrega investigación de restos, denuncia con pruebas, patrulla, persecución, expulsión temporal del Rival y victoria de los trabajadores en la tercera expulsión.
Los controles anteriores se conservan: Tomás WASD y E; Rival IJKL, O y U.

## Recorrido de prueba

1. Abrir `Assets/Scenes/KitchenScene.unity` y entrar en Play. El Capibara aparece cerca del punto inicial después de preparar las rutas del mapa.
2. Comenzar la partida. Tomás recoge comida y el Rival conecta un tacle.
3. Tomás se acerca y mira hacia los restos del suelo. Pulsar E cuando aparezca `Restos de comida · examinar rastro`.
4. Buscar al Capibara, acercarse, mirar hacia él y pulsar E para presentar la denuncia. Es posible hacerlo llevando comida.
5. Con una prueba válida, el Capibara persigue al Rival durante un máximo de 20 segundos. Respeta las rutas navegables y comprueba que no haya una pared antes de detenerlo.
6. Al alcanzarlo, el Rival desaparece del área durante 8 segundos. Su pantalla muestra cuánto falta para regresar; no puede moverse, cocinar, usar escaleras ni atacar durante ese período.
7. Al regresar vuelve al punto inicial y debe esperar 25 segundos antes de otro tacle.
8. Repetir con sabotajes nuevos. La tercera expulsión termina la partida con victoria de los trabajadores.

## Reglas de las pruebas

- Ver el aviso de sabotaje no concede una prueba. Hay que examinar físicamente los restos.
- Los restos permanecen 30 segundos. Una prueba examinada permanece registrada hasta resolverse el caso o reiniciarse el turno.
- Examinar el mismo rastro varias veces no crea pruebas adicionales.
- La prueba queda reservada durante la persecución. Si el Rival escapa, se libera para una denuncia posterior y no se aplica una penalización.
- Si no existe una ruta completa al sospechoso, no se consume la prueba ni se descuenta tiempo.
- Cada captura se registra una sola vez. Una expulsión invalida todos los rastros anteriores de ese Rival; se necesita un nuevo sabotaje para otra expulsión.
- Denunciar sin una prueba examinada descuenta 5 segundos. Hay 10 segundos de espera entre denuncias para evitar penalizaciones repetidas al pulsar E.
- El Rival no puede presentar denuncias.
- No se muestran barras de sospecha ni un contador permanente que revele culpabilidad.

En esta versión de dos participantes, la denuncia señala al otro garzón. La selección de sospechoso para partidas con más trabajadores sigue pendiente.

## Movimiento del policía

`MarketNavigation` genera una superficie de navegación al comenzar, usando los colliders existentes del mercado y excluyendo jugadores y comida transportable. No reconstruye la escena.
Las escaleras que tengan `StairTrigger` se conectan usando sus destinos existentes. El Capibara atraviesa esos enlaces con la misma transición de posición que usa el jugador. Si un nivel no tiene una conexión navegable, no se finge una captura ni se penaliza al denunciante.
La patrulla y el alcance real del mapa requieren revisión en Play. El modelo local procede de `OficialCapibara.prefab`. Sus referencias de malla, materiales, script y controlador no estaban presentes en esta copia del proyecto. Una copia en Resources conserva los nombres, las posiciones, las escalas y la estructura de las piezas; usa cubos internos de Unity y seis materiales locales de URP para reparar las dependencias ausentes. El prefab original se conserva. El movimiento actual no incluye nuevas animaciones de caminar, silbato ni captura.

## Valores iniciales

| Regla | Valor |
|---|---:|
| Fuera del mercado | 8 s |
| Penalización por denuncia sin pruebas | 5 s |
| Espera entre denuncias | 10 s |
| Duración máxima de persecución | 20 s |
| Velocidad de patrulla | 2 m/s |
| Velocidad de persecución | 5,7 m/s |
| Distancia de captura, con línea de visión | 1,15 m |

Los valores están en `InvestigationSystem`, `CapibaraPolice` y `TackleSabotage`.
Reiniciar la partida limpia las pruebas, las expulsiones, la persecución, la detención y los tiempos de espera.
Pausar congela la patrulla, la persecución y el regreso del Rival.

## Validación y límites

Se comprobaron por compilación los scripts del editor y del juego usando las bibliotecas instaladas de Unity 6000.6.0f1.
Las reglas independientes de pruebas y expulsiones pasaron 25 comprobaciones ejecutables que cubren duplicados, acusación incorrecta, reserva, escape, invalidación de pruebas antiguas, tres expulsiones y reinicio. Se pueden repetir desde el menú del editor `Mercado > Comprobar reglas de investigación`. Este menú comprueba las reglas, sin iniciar ni alterar una partida.
La ejecución automática en Unity ya se realizó con una licencia activa: pasaron 100 comprobaciones de multijugador, cocina, sabotaje, navegación y expulsiones. Se usó una copia temporal con una ruta corta. Consulta `PRUEBAS_UNITY.md` para el alcance y las limitaciones; la revisión visual y una partida manual con dos personas siguen pendientes.

La victoria por tercera expulsión está integrada. La victoria por una meta de pedidos y la derrota por pedidos arruinados siguen usando el comportamiento previo del prototipo y necesitan su propia fase de atribución y balance. Tirar un ingrediente por un tacle continúa sin contar automáticamente como pedido arruinado.
