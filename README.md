# MercadoGame - Unity 6.6

Juego estilo Overcooked ambientado en un mercado con cocina (planta baja) y patio de comidas (planta alta).

## Requisitos

- Unity 6.6 o superior
- Input System Package (ya incluido en manifest.json)

## Configuracion Inicial en Unity

### Paso 1: Abrir el Proyecto

1. Abrir Unity Hub
2. Click en "Open" > Seleccionar la carpeta del proyecto
3. Esperar a que Unity compile los scripts

### Paso 2: Configurar Input System

1. Edit > Project Settings > Player
2. En "Active Input Handling" seleccionar "Input System Package (New)"
3. Reiniciar Unity cuando pida

### Paso 3: Configurar Calidad (para AMD Radeon 610M)

1. Edit > Project Settings > Quality
2. Crear nivel "Baja" con:
   - Anti-aliasing: Don't Use
   - Shadows: Don't Use
   - V Sync Count: Every V Blank
3. Seleccionar este nivel como default

### Paso 4: Crear la Escena

1. File > New Scene
2. Guardar como "KitchenScene" en Assets/Scenes/

### Paso 5: Crear Materiales

En la carpeta Assets/Materials, crear estos materiales (Right Click > Create > Material):

| Nombre | Color RGB | Para que |
|--------|-----------|----------|
| WallMaterial | (70, 130, 180) | Paredes azul/teal |
| FloorMaterial | (240, 240, 240) | Piso blanco |
| CounterMaterial | (160, 100, 50) | Mesadas marron |
| StoveMaterial | (80, 80, 80) | Estufa/horno |
| PlateMaterial | (250, 250, 250) | Platos |
| StairMaterial | (140, 120, 100) | Escalera |
| FloorUpperMaterial | (120, 100, 80) | Piso planta alta |

### Paso 6: Construir Planta Baja (Cocina)

Crear la estructura usando cubes y scale:

#### Paredes (Scale: 12x3x0.5)
```
- Pared Norte: Position (0, 1.5, 6)
- Pared Sur: Position (0, 1.5, -6)
- Pared Este: Position (6, 1.5, 0)
- Pared Oeste: Position (-6, 1.5, 0)
```

#### Piso (Scale: 12x0.1x12)
```
- Piso Cocina: Position (0, 0, 0)
```

#### Mesadas/Cajones (Scale: 2x1x0.8)
```
- Cajon Norte 1: Position (-4, 0.5, 5.2)
- Cajon Norte 2: Position (-2, 0.5, 5.2)
- Cajon Norte 3: Position (0, 0.5, 5.2)
- Cajon Norte 4: Position (2, 0.5, 5.2)
- Cajon Norte 5: Position (4, 0.5, 5.2)

- Cajon Oeste 1: Position (-5.2, 0.5, 3)
- Cajon Oeste 2: Position (-5.2, 0.5, 1)
- Cajon Oeste 3: Position (-5.2, 0.5, -1)
- Cajon Oeste 4: Position (-5.2, 0.5, -3)

- Cajon Este 1: Position (5.2, 0.5, 3)
- Cajon Este 2: Position (5.2, 0.5, 1)
- Cajon Este 3: Position (5.2, 0.5, -1)
- Cajon Este 4: Position (5.2, 0.5, -3)
```

#### Estaciones de Cocina
```
- Estufa 1: Position (-3, 0.75, -4.5) [Tag: CookingStation]
- Estufa 2: Position (-1, 0.75, -4.5) [Tag: CookingStation]
- Horno: Position (1, 0.75, -4.5) [Tag: CookingStation]

- Mesa Prep 1: Position (3, 0.5, -4.5) [Tag: ChoppingStation]
- Mesa Prep 2: Position (5, 0.5, -4.5) [Tag: ChoppingStation]

- Lavabo: Position (-4, 0.5, 0) [Tag: WashingStation]
- Dispensador Platos: Position (4, 0.5, 0) [Tag: PlateStation]
```

#### Escalera (Scale: 2x0.5x3)
```
- Escalera: Position (0, 0.25, -2)
- Agregar escalones como cubes pequenos
```

### Paso 7: Construir Planta Alta (Patio de Comidas)

#### Piso Superior (Scale: 14x0.1x12)
```
- Piso Patio: Position (0, 3, 0) [y=3 para elevar]
```

#### Paredes Superiores
```
- Mismo esquema que planta baja pero en y=3
```

#### Mesas (Scale: 1.5x0.8x1.5)
```
- Mesa 1: Position (-3, 3.4, 2)
- Mesa 2: Position (0, 3.4, 2)
- Mesa 3: Position (3, 3.4, 2)
- Mesa 4: Position (-3, 3.4, -2)
- Mesa 5: Position (0, 3.4, -2)
- Mesa 6: Position (3, 3.4, -2)
```

#### Mostrador de Servicio (Scale: 4x1x1)
```
- Mostrador: Position (0, 3.5, 5) [Tag: ServingWindow]
```

### Paso 8: Crear el Jugador

1. Create > 3D Object > Capsule
2. Renombrar a "Player"
3. Color: Naranja (255, 140, 0)
4. Position: (0, 1, 0)
5. Agregar componentes:
   - PlayerController.cs
   - Rigidbody (Freeze Rotation Y)
   - Capsule Collider
6. Crear hijo "Hand" vacio en (0, 1.2, 0.5)

### Paso 9: Configurar Camara

1. Seleccionar Main Camera
2. Agregar CameraFollow.cs
3. Arrastrar Player al campo "Target"
4. Configurar:
   - Offset: (0, 10, -8)
   - Smooth Speed: 5
   - Use Bounds: True
   - Min XZ: (-10, -10)
   - Max XZ: (10, 10)

### Paso 10: Configurar GameManager

1. Create > Empty > Renombrar "GameManager"
2. Agregar GameManager.cs
3. Agregar OrderSystem.cs
4. Configurar:
   - Game Duration: 180
   - Target Score: 100
   - Order Interval: 10
   - Order Time Limit: 30

### Paso 11: Crear Recetas

En Assets/Resources/Recipes/, crear ScriptableObjects:

1. Right Click > Create > Kitchen > Recipe Data
2. Configurar "Hamburguesa":
   - Recipe Name: Hamburguesa
   - Required Ingredients: Bread, Meat, Cheese
   - Requires Chopping: false
   - Requires Cooking: true
   - Base Points: 25

3. Configurar "Ensalada":
   - Recipe Name: Ensalada
   - Required Ingredients: Lettuce, Tomato
   - Requires Chopping: true
   - Requires Cooking: false
   - Base Points: 20

### Paso 12: Agregar Ingredientes

Crear cubes pequenos como ingredientes:
- Tomato: Color rojo (255, 0, 0)
- Lettuce: Color verde (0, 200, 0)
- Bread: Color amarillo (255, 220, 0)
- Meat: Color marron (139, 69, 19)
- Cheese: Color amarillo claro (255, 255, 150)

Agregarles: IngredientPickup.cs y Ingredient.cs

## Controles

| Tecla | Accion |
|-------|--------|
| WASD / Flechas | Mover jugador |
| E | Interactuar |
| Espacio | Acelerar (futuro) |
| P | Pausa |

## Estructura del Proyecto

```
Assets/
├── Scripts/
│   ├── Core/           # GameManager.cs
│   ├── Player/         # PlayerController.cs, CameraFollow.cs
│   ├── Kitchen/        # Interactable.cs, KitchenCounter.cs, etc.
│   ├── FoodCourt/      # FoodCourtTable.cs, ServingWindow.cs
│   └── Orders/         # OrderSystem.cs, RecipeData.cs
├── Scenes/
│   └── KitchenScene.unity
├── Materials/
│   └── (materiales de colores)
├── Prefabs/
│   ├── Characters/
│   ├── Kitchen/
│   └── FoodCourt/
└── Resources/
    └── Recipes/
        └── (ScriptableObjects)
```

## Siguientes Pasos

1. Agregar UI para pedidos y temporizador
2. Animaciones de personaje
3. Mas recetas e ingredientes
4. Sonidos
5. Multiple niveles
6. Sistema de puntuacion estrellas

## Solucion de Problemas

- **No detecta input:** Verificar Input System en Project Settings
- **Bajo FPS:** Reducir calidad en Quality Settings
- **Scripts no compilan:** Verificar que todos los archivos estan en carpetas correctas


## Menú, modos y capibara (Douglas)

Integración preparada sobre `presentacion_3`, conservando el mapa, los diseños originales, las recetas y las estaciones. Incluye historia, currículum y cinco fichas con foto, selección de apariencia, modo individual y multijugador local.

- Tomás: WASD/flechas + E; Rival local: IJKL + O y U para el humo/aceite existente.
- En individual, el Rival usa el sabotaje automático. En local lo controla el segundo jugador.
- Examinar el rastro con E y presentar la prueba al capibara con E. El policía persigue al Rival identificado y lo suspende durante ocho segundos cuando lo alcanza, tanto humano como automático. Tomás conserva sus controles y sus ingredientes.
- F1: fichas; F2: currículum; F3: historia. Estas ventanas pausan el turno.

Abrir `Assets/Scenes/KitchenScene.unity` y pulsar Play. Descargar los recursos con Git LFS (`git lfs pull`).

[Guía de uso y alcance](Documentation/MENU_Y_MODOS_PRESENTACION_3.md) · [Pruebas](Documentation/PRUEBAS_MENU_Y_CAPIBARA.md)
