# Mapa de flujo del jugador · Cocina de Doña Rosa

Documentación del nivel jugable de **Mercado Lanza: Caos en la Cocina**: cómo se mueve el jugador (Tomás, el garzón) por la cocina y el comedor para cumplir un pedido, y cómo están balanceados la dificultad, el ritmo de los pedidos y los sabotajes.

![Mapa de flujo del jugador](MapaFlujoJugador.png)

## Zonas del nivel

| Zona | Dónde está | Para qué sirve |
|---|---|---|
| Estante y cajas de insumos | Norte de la cocina | Cada caja da su insumo sin límite (papa, choclo, haba, queso, pan, cerdo, maíz morado, masa, fruta) |
| Tabla de picar | Suroeste | Pica los insumos que lo necesitan |
| Hornallas ×3 y sartén | Sur | Cocinan los insumos que lo necesitan |
| Platos y lavaplatos | Oeste | Dan un plato limpio para armar el pedido |
| Mesa de armado | Noreste, junto al panel PEDIDOS | Plato + ingredientes → plato terminado |
| Basura | Al lado de la mesa de armado | Para tirar lo que salió mal |
| Comedor (6 mesas) | Al este, saliendo de la cocina | Las mesas hacen los pedidos y ahí se entregan |

## Flujo de un pedido

1. **Mirar el pedido**: en la tarjeta del HUD (mesa, foto del plato, barra de tiempo y cada ingrediente con su ícono de preparación: cuchillo = picar, olla = cocinar). Cada caja de insumos tiene encima el mismo ícono.
2. **Tomar el insumo** de su caja.
3. **Preparar**: a) picar en la tabla o b) cocinar en una hornalla, según la receta.
4. **Sacar un plato limpio** del lavaplatos.
5. **Armar** en la mesa de armado: plato + ingredientes. En la tarjeta, cada ingrediente que ya está en el plato se pone verde. Cuando están todos, el plato se arma solo.
6. **Entregar** en la mesa que lo pidió y volver a la cocina.

| Receta | Pasos | Tiempo | Puntos base |
|---|---|---|---|
| Sándwich de Chola (pan + cerdo cocido) | 2 → 3b → 4 → 5 → 6 | 55 s | 30 |
| Ensalada de Frutas (plátano, papaya y frutilla picados) | 2 → 3a ×3 → 4 → 5 → 6 | 60 s | 35 |
| Api con Pastel (maíz morado y masa cocidos) | 2 → 3b → 4 → 5 → 6 | 60 s | 30 |
| Plato Paceño (papa, choclo, haba y queso cocidos) | 2 → 3b → 4 → 5 → 6 | 75 s | 40 |

Al entregar se gana un bono de hasta **+25 puntos** según el tiempo que sobró. Si el pedido vence, el cliente se va sin comer: **−10 puntos**.

## Ritmo de la partida (balance final)

La partida dura **3 minutos** y la meta es **100 puntos**. La dificultad sube de forma gradual:

| Tiempo jugado | Qué pasa |
|---|---|
| 0:00 | Aviso flotante **"Pasea para ubicarte del mapa"**: lista de las 5 zonas, que se marcan al visitarlas |
| 0:05 | Llega el primer pedido |
| 0:00 → 3:00 | Los pedidos pasan de **uno cada 20 s a uno cada 12 s**, con hasta **20 % menos de tiempo** al final (máximo 5 a la vez) |
| 0:45 | Primer **sabotaje** del impostor; los siguientes llegan cada **55 s → 30 s** (±20 % al azar) |
| 2:00 | Aviso **"¡HORA PUNTA!"** (2/3 de la partida) |
| Pedido con ≤ 10 s | Aviso **"¡Apresúrate!"** con la mesa que se impacienta |
| 2:30 | Aviso **"¡Apresúrate, se te acaba el tiempo!"** y borde rojo en la pantalla |
| Últimos 15 s | Ya no hay sabotajes, para que el cierre dependa del jugador |

Todos estos valores se pueden ajustar en el Inspector: `OrderSystem` (dificultad progresiva), `SabotageSystem` (ritmo de sabotajes) y `FloatingWindows` (avisos).

## Sabotajes

| Sabotaje | Dónde | Efecto visual |
|---|---|---|
| Fuego al máximo | Hornallas | Humo gris que sube, con un resplandor naranja que parpadea (12 s) |
| Aceite derramado | Al lado de la tabla de picar o de la mesa de armado, del lado del pasillo | Mancha de aceite brillante en el piso que se desvanece (25 s) |

Cada sabotaje muestra la ventana **"¡Advertencia! Intento de sabotaje"** con la pista para el Capibara-policía. La pantalla final muestra cuántos sabotajes hubo en la partida.

## Controles

| Tecla | Acción |
|---|---|
| WASD / flechas | Moverse |
| Shift | Correr |
| E | Interactuar (tomar, dejar, picar, cocinar, armar, entregar) |
| Rueda del mouse | Zoom |
| ENTER | Avanzar en la historia y empezar la partida |
| F1 / F2 / F3 | Fichas de personaje / currículum de Tomás / historia |
| R | Jugar otra vez al terminar |
