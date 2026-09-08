# 🎲 MONATAC

**Juego de combate por turnos con dados y cartas — Multijugador local (HotSeat) para 2 a 4 jugadores.**

MONATAC es un juego de mesa digital donde la estrategia, la economía de recursos y la
suerte de los dados determinan quién es el último en pie. Cada turno el jugador elige
una acción (**atacar, curarse, recolectar o descartar**) y potencia sus jugadas con
**cartas** compradas del mazo.

---

## 🎮 Sobre el juego

| Campo | Detalle |
|---|---|
| **Género** | Combate por turnos con gestión de recursos |
| **Jugadores** | 2 a 4 (multijugador local · HotSeat) |
| **Duración** | 15–30 minutos |
| **Condición de victoria** | Ser el último jugador vivo, o el de más HP al agotarse las rondas |

---

## 🧩 MDA del juego (Mecánicas · Dinámicas · Estética)

### ⚙️ Mecánicas (las reglas del sistema)
- **Turnos** por jugador (2–4, HotSeat): la pantalla muestra siempre al jugador activo.
- **4 acciones** por turno (jerarquía polimórfica `Accion`):
  - **Atacar** — 3d4 de daño al rival elegido.
  - **Curarse** — 2d4 de HP recuperado.
  - **Recolectar** — 3d4 monedas y habilita comprar cartas.
  - **Descartar** — tira 1 carta de la mano; consume el turno (mecánica adicional).
- **Dados** de 4 caras (d4) como fuente de azar.
- **Sistema de cartas (54 cartas):** compra (6 monedas), mano de hasta 5, uso de cartas
  (bonus / comodines / vencimiento), **defensivas automáticas** al ser atacado, **grupales**
  al comprarse, descarte y **mazo circular** (se recicla al agotarse).
- **Selección de objetivo** (rota entre rivales vivos).
- **Configuración inicial:** cantidad de jugadores, HP inicial y límite de rondas.
- **Fin de partida:** pantalla con el ganador y botones **Reiniciar / Volver al Menú**.

### 🔄 Dinámicas (lo que emerge al jugar)
- **Gestión de recursos:** ¿gasto monedas en cartas ahora o acumulo para una jugada grande?
- **Riesgo / recompensa:** atacar, curarse o juntar según cómo venís de HP.
- **Focalización de amenazas:** decidir a quién atacar (al más fuerte o al más débil).
- **Tensión temporal:** con límite de rondas, apurar el ataque antes de que "gane el de más HP".
- **Timing defensivo:** guardar escudos/reflejos para el momento justo.

### 🎭 Estética (la experiencia buscada)
- **Desafío** — competencia por ser el último en pie.
- **Azar y sorpresa** — dados y cartas que cambian la partida.
- **Rivalidad social** — todos en la misma pantalla (HotSeat).
- **Descubrimiento** — qué carta te toca y cómo la aprovechás.

---

## 🕹️ Guía rápida de juego (controles e interacciones)

1. **Menú:** elegí **2, 3 o 4 jugadores**. (Opcional: botón **Personalización** para elegir HP y rondas.)
2. **En tu turno:**
   - **Clic** en una acción: **Atacar / Curarse / Recolectar / Descartar**.
   - (Opcional) **Clic en una carta** de tu mano para marcarla `[USAR]`.
   - (Si atacás) **Cambiar Objetivo** para rotar el rival apuntado.
   - **Lanzar Dados** para resolver la jugada.
   - Si Recolectaste, podés **Comprar carta** (6 monedas c/u) o **acumular**.
   - **Pasar Turno**.
   - *(Para descartar: elegí **Descartar** y tocá la carta a tirar — perdés el turno.)*
3. **Fin:** cuando queda un solo jugador vivo (o se acaban las rondas), aparece la pantalla
   de fin con **Reiniciar** (nueva partida) o **Volver al Menú**.

---

## 🏛️ Arquitectura de capas

El código está separado en **tres capas** con responsabilidades distintas. `/Data` y
`/Rules` son **C# puro** (no dependen de Unity), por lo que se pueden probar por separado.

- **`/Data`** — el **estado y las entidades** del juego: `Jugador`, `Carta` (+ subtipos),
  `Mazo`, `PilaDescarte`, `Config`, `Enums`.
- **`/Rules`** — la **lógica y las reglas**: `Partida` (turnos, objetivo, rondas, victoria),
  `GestorCartas` (resolución de cartas y combate defensivo), `Accion` (+ subtipos),
  `FabricaDeCartas`, `Dado`, `ContextoGrupal`, las interfaces **`IPartida` / `IGestorCartas`** (DIP)
  e **`IVistaJuego` + `PresentadorJuego`** (MVP).
- **`/Visual`** — la **presentación e input** (MonoBehaviours): `GameManager` (orquesta),
  **`VistaJuegoUI`** (dibuja la pantalla), `MenuManager`, `PersonalizacionManager`, `BotonValor`,
  `AnimacionDados`, `EfectoHoverCarta`. Solo **muestra** información y **captura** clics; no conoce
  las reglas ni altera los datos directamente.

**Escenas:** `MENU` (elegir jugadores) · `Personalizacion` (HP y rondas) · `juego` (partida).

---

## 🎯 Patrones de diseño (justificación técnica)

### Patrón principal: **MVP (Modelo–Vista–Presentador)** + DIP
- **Clases:** **Vista** = `GameManager` (implementa `IVistaJuego`) · **Modelo** = `Partida` /
  `Jugador` · **Presentador** = `PresentadorJuego`.
- **Problema que resuelve:** desacopla las **reglas** de la **interfaz de Unity**. La Vista y el
  Modelo **nunca se comunican directo**: la Vista dispara un **evento** (`AlPedirCambiarObjetivo`),
  el Presentador lo escucha, actualiza el Modelo (`Partida`) y le pide a la Vista que refresque
  **a través de la interface** `IVistaJuego`. Así el `PresentadorJuego` depende de una
  **abstracción** (`IVistaJuego`), no del `GameManager` concreto → **Inversión de Dependencias (DIP)**.
  Aplicado en la interacción **"Cambiar Objetivo"**.

### Otros patrones aplicados
- **Singleton** → `GameManager` (`static Instance` + `Awake`): garantiza una única instancia
  global accesible desde cualquier script; evita duplicados que pisen el estado.
- **Factory** → `FabricaDeCartas.CrearMazo()`: centraliza la creación de las 54 cartas en un
  solo lugar; cambiar la composición del mazo no toca el resto del código.
- **Herencia + Polimorfismo** → `Carta` (abstracta) y `Accion` (abstracta) con sus subtipos:
  agregar una carta o una acción nueva es crear una clase, sin `if` gigantes ni tocar el flujo.
- **Observer (evento)** → `event System.Action AlPedirCambiarObjetivo`: la Vista notifica que
  pasó algo sin saber quién la escucha.

### SOLID aplicado
- **SRP** — las reglas se extrajeron del `GameManager` a `Partida` y `GestorCartas`; el **dibujado**
  se separó en **`VistaJuegoUI`** (el `GameManager` solo orquesta).
- **DIP** — el `PresentadorJuego` depende de `IVistaJuego`, y el `GameManager` depende de
  **`IPartida` / `IGestorCartas`** (abstracciones), no de las clases concretas.

---

## 📁 Estructura del proyecto

```
Assets/
 ├─ Scenes/        # MENU, Personalizacion, juego
 ├─ Scripts/
 │   ├─ Data/      # Enums, Jugador, Cartas, Mazo, Config
 │   ├─ Rules/     # Partida, GestorCartas, Accion, FabricaDeCartas, Dado,
 │   │             #   ContextoGrupal, IPartida, IGestorCartas,
 │   │             #   IVistaJuego, PresentadorJuego
 │   └─ Visual/    # GameManager, VistaJuegoUI, MenuManager, PersonalizacionManager,
 │                 #   BotonValor, AnimacionDados, EfectoHoverCarta
 ├─ Sprites/       # Arte de las cartas
 └─ Settings/      # Configuración de render (URP)
ProjectSettings/   # Configuración del proyecto Unity
Packages/          # Dependencias
```

---

## 🛠️ Pila tecnológica

| Herramienta | Uso |
|---|---|
| **Unity** (motor) | Desarrollo del videojuego |
| **C#** | Lenguaje de scripting |
| **Git + GitHub** | Control de versiones |
| **Visual Studio / Rider** | Editor de código |

---

## 🚀 Cómo abrir el proyecto

1. Clonar el repositorio:
   ```bash
   git clone https://github.com/wackxion/MONATAC.git
   ```
2. Abrir **Unity Hub** → `Add project from disk` → seleccionar la carpeta clonada.
3. Abrir con la versión de Unity indicada en `ProjectSettings/ProjectVersion.txt`.
4. Abrir la escena **`MENU`** en `Assets/Scenes/` y presionar **Play**.

---

## 📋 Backlog de tareas

Backlog de programación (tareas hechas / en progreso / pendientes):
- **Trello:** https://trello.com/invite/b/6a9ec9aebfc402696fe71d44/ATTIdf3bf5328ba3949bab7a2a2d03c60532751D1977/grupo-b
- **Backlog local:** [`Backlog.md`](Backlog.md), organizado por área e hito.

---

## 👥 Integrantes

- **Braian Zapater**
- **Alvarez Pilar**
- **Julian Gabriel Blanco**

**Materia:** Programación en Videojuegos II — Segundo Cuatrimestre

---

## 🔗 Enlaces

- **Repositorio:** https://github.com/wackxion/MONATAC
- **Carpeta del proyecto (Drive):** https://drive.google.com/drive/folders/137EptYPsfEYhwZU-GM0MS9eKm2sp86rI?usp=sharing

---

## 📌 Estado del proyecto

🎮 **Jugable de principio a fin** — menú, configuración inicial, partida de 2 a 4 jugadores
por turnos, sistema completo de cartas (54), condición de victoria y **pantalla de fin con
reinicio / volver al menú**. Arquitectura por capas con MVP + DIP. Materia Programación en
Videojuegos II.
