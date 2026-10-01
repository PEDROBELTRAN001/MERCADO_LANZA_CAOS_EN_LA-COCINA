# Cocina de Doña Rosa

Abrir Assets/Scenes/KitchenScene.unity. La estación está guardada en la misma escena, bajo COCINA_DOÑA_ROSA. No necesita ejecutar un generador al iniciar el juego.

## Contenido
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
