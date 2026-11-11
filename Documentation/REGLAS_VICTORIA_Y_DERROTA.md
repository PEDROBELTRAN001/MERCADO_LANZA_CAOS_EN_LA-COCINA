# Reglas de victoria y derrota

Implementación basada en las condiciones del perfil inicial. Abrir `Assets/Scenes/KitchenScene.unity` y pulsar Play. Funciona en individual contra el Rival automático y en multijugador local.

## Resultado del turno

Los garzones ganan al terminar los 180 segundos si cumplen **las tres metas**:

- Reputación de al menos 3,5 sobre 5 estrellas.
- Dos pedidos VIP entregados correctamente.
- Cuota de ventas de 100 puntos. Se reutiliza el puntaje existente, incluido el bono por rapidez y descontadas las penalizaciones; no hay una moneda adicional.

El impostor gana inmediatamente si la reputación llega a cero, tres clientes se van sin atención o se entregan dos platos contaminados a la VIP. También gana si termina el turno y falta alguna meta. Cumplir las metas temprano no termina la partida: deben mantenerse hasta el cierre.

La pantalla final anuncia el ganador y la causa, con reputación, ventas, VIP, entregas y penalizaciones. R vuelve al menú. El resultado deja de cambiar al terminar.

## Reputación y pedidos VIP

- Inicio: 5 estrellas.
- Entrega correcta: +0,25 estrellas, con máximo de 5.
- Pedido vencido: −0,5 estrellas y la penalización existente de 10 puntos; cuenta como cliente perdido.
- Plato salado entregado: −1,5 estrellas y −50 puntos; sin recompensa ni bono de rapidez. Si era VIP, cuenta para la clausura. No cuenta como cliente que abandonó sin atención.

La reputación es un medidor acumulado del servicio, no un promedio temporal. Estos incrementos son decisiones de balance nuevas: el perfil no especificaba su cálculo.

Los pedidos alternan entre VIP y normales desde el primero. Los VIP representan a la Cholita Paceña y piden Plato Paceño o Api con Pastel (`vipEligible` en las recetas). La tarjeta identifica **VIP** y el HUD cuenta sus entregas. Se reutilizan las mesas existentes; esta integración no añade nuevas animaciones de comensales.

## Comida salada y pistas

En local, el Rival se acerca a la mesa de armado y pulsa **U** cuando hay un plato terminado. Sala ese plato y mantiene su espera existente de 20 segundos. Sin plato disponible conserva el sabotaje de aceite de esa estación. El Rival automático prioriza salar una comanda lista cuando llega su siguiente sabotaje.

El plato muestra **¡SAL!** y aparece una advertencia. Tomás puede recogerlo con E y descartarlo en la basura con E. Descartarlo evita la penalización; entregar la receta correcta pero salada hace que el cliente la rechace. El sabotaje emite el mismo evento de evidencia que los demás y permite denunciar al Rival ante el Capibara. No se puede salar dos veces el mismo plato.

Los sabotajes anteriores de humo y aceite se conservan. La denuncia sigue usando E para examinar el rastro y E para presentar la prueba; la suspensión policial sigue siendo de ocho segundos. No se añadió comida descompuesta: por ahora la contaminación jugable es por exceso de sal.

## Reinicio y pausa

Reiniciar limpia reputación, ventas, VIP, clientes perdidos, contaminación y resultado, además de los platos terminados y el contenido de la mesa de armado. Pausar y continuar conserva el progreso y no vuelve a disparar el inicio de ronda.

## Verificación

`Assets/Scripts/Editor/RoundRulesValidation.cs` comprueba las tres metas, los límites de reputación, las derrotas anticipadas, el resultado inmutable, la pausa y el reinicio. Se ejecuta mediante `-batchmode -nographics -executeMethod RoundRulesValidation.Run` en una copia temporal con `GameManager.cs` y el verificador; cierra ese editor al terminar.

Prueba manual del flujo completo en KitchenScene:

1. Empezar una partida y comprobar estrellas, ventas, VIP y tarjetas VIP.
2. Entregar dos pedidos VIP correctos y alcanzar la cuota; comprobar que la victoria espera al cierre.
3. En local, dejar una comanda lista y pulsar U con el Rival cerca de armado. Comprobar la señal SAL, la espera de sabotaje y el rastro para denunciar.
4. Tirar un plato salado y comprobar que no penaliza; después entregar dos platos salados a VIP y comprobar clausura inmediata sin puntos de entrega.
5. Reiniciar y dejar vencer tres pedidos para comprobar la otra derrota inmediata.
6. Pausar y continuar: confirmar que el reloj, los pedidos, el puntaje y los objetivos conservan su progreso.

El perfil DOCX original se conserva como antecedente. Esta guía registra el alcance implementado y las adaptaciones para preparar su próxima versión.
