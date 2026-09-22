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
- **`/Visual`** — la **presentación e input** (MonoBehaviours), organizada en subcarpetas:
  - **`Managers/`** — `GameManager` (orquesta), `GestorAudio` (sonido), `MenuManager`,
    `PersonalizacionManager`, `SeleccionPersonajesManager`.
  - **`UI/`** — `VistaJuegoUI` (dibuja la pantalla), `BotonValor`, `EfectoHoverCarta`, `LoadingScreen`.
  - **`Animacion/`** — `AnimacionDados`, `AnimacionBarraHP`, `AnimacionCartaGrupal`, `NumeroFlotante`,
    `SpawnerNumerosFlotantes`.

  Solo **muestra** información y **captura** clics; no conoce las reglas ni altera los datos directamente.

**Escenas:** `MENU` (elegir jugadores) · `Loading` (pantalla de carga) · `SeleccionPersonajes`
(personaje y nombre) · `Personalizacion` (HP y rondas) · `juego` (partida).

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
- **SRP** (Responsabilidad Única) — las reglas se extrajeron del `GameManager` a `Partida` y
  `GestorCartas`; el **dibujado** se separó en **`VistaJuegoUI`** (el `GameManager` solo orquesta).
- **OCP** (Abierto/Cerrado) — las jerarquías `Carta` y `Accion` están **abiertas a extensión** pero
  **cerradas a modificación**: para agregar una carta o acción nueva se **crea una clase** (que hereda
  y hace `override`), **sin tocar** `GestorCartas` ni `GameManager`, que las tratan por polimorfismo.
- **DIP** (Inversión de Dependencias) — el `PresentadorJuego` depende de `IVistaJuego`, y el
  `GameManager` depende de **`IPartida` / `IGestorCartas`** (abstracciones), no de las clases concretas.

---

## 🎛️ Sistemas de juego — Game Feel (UI · Audio · VFX)

### 🖥️ Interfaz de Usuario (UI) — estado en tiempo real
1. **Barras de HP** — la vida de cada jugador (`Image.fillAmount`), coloreadas según turno/objetivo.
2. **Indicador de turno** — "Turno de Jugador X" + marcas (turno)/(objetivo)/(eliminado).
3. **Contador de ronda** — "Ronda 2/10".
4. **Contador de monedas** — recursos del jugador en turno.
5. **Mano de cartas** — muestra las cartas con su dibujo y la marca `[USAR]`.

### 🔊 Audio (`GestorAudio`, patrón Singleton) — 3+ fuentes
1. **Música de fondo (BGM)** — en loop, continua entre escenas (`DontDestroyOnLoad`).
2. **SFX de UI** — clic en todos los botones (se enganchan automáticamente al cargar cada escena).
3. **SFX de gameplay** — **daño** al atacar, **curación** al curarse y **dados** al tirar.

### 🎬 Animaciones / VFX — feedback visual
1. **Dados girando** (`AnimacionDados`).
2. **Barra de HP suave** (`AnimacionBarraHP`, con `Mathf.Lerp`).
3. **Números flotantes** de daño/cura (`NumeroFlotante` + `SpawnerNumerosFlotantes`).
4. **Carta grupal** con fade al comprarse (`AnimacionCartaGrupal`).
5. **Pantalla de carga** con barra de progreso (`LoadingScreen`).

> Todos los sistemas **superan el mínimo de 3** que pide el hito.
>
> **Detalle técnico completo** (componentes, código y eventos de cada sistema):
> **[`Reporte_Sistemas_UI_Audio_VFX.md`](Reporte_Sistemas_UI_Audio_VFX.md)**.

---

## 🧪 Testing & 🐛 Bugfix

- **Pruebas unitarias:** 7 tests **EditMode / NUnit** (13 casos) sobre la lógica de dominio
  (`Jugador` y `Partida`), todos en verde. Se corren con `Window → General → Test Runner → EditMode
  → Run All`.
- **Reporte completo** (lista de tests + detalle de correcciones): **[`Reporte_Testing_y_Bugfix.md`](Reporte_Testing_y_Bugfix.md)**.

### Correcciones realizadas (Bugfixes)
1. **Contador de rondas roto si moría el Jugador 1** → se detecta la ronda nueva con
   `IndiceActual < indiceAnterior` (no depende de un índice fijo). *(Partida)*
2. **El juego se congelaba al agotarse las rondas** → se chequea el fin en `OnPasarTurno`
   (`if (juegoTerminado) { MostrarPantallaFin(); return; }`). *(GameManager)*
3. **El 3er dado quedaba con un valor viejo** al usar acciones de 2 dados → se limpian los dados no
   usados. *(AnimacionDados)*
4. **No se podía asignar el avatar del jugador** → el campo era `Image[]` y los avatares son
   `SpriteRenderer`; se cambió a `SpriteRenderer[]`. *(VistaJuegoUI)*
5. **Errores de compilación al armar los tests** (dependencia circular Data↔Rules) → se unificó el
   código en un solo assembly `MONATAC.asmdef`. *(setup de tests)*

---

## 📁 Estructura del proyecto

```
Assets/
 ├─ Scenes/        # MENU, Loading, SeleccionPersonajes, Personalizacion, juego
 ├─ Scripts/       # MONATAC.asmdef (un solo assembly con todo el código del juego)
 │   ├─ Data/      # Enums, Jugador, Cartas, Mazo, Config
 │   ├─ Rules/     # Partida, GestorCartas, Accion, FabricaDeCartas, Dado,
 │   │             #   ContextoGrupal, IPartida, IGestorCartas,
 │   │             #   IVistaJuego, PresentadorJuego
 │   └─ Visual/    # presentación e input, organizado en subcarpetas:
 │       ├─ Animacion/  # AnimacionDados, AnimacionBarraHP, AnimacionCartaGrupal,
 │       │              #   NumeroFlotante, SpawnerNumerosFlotantes
 │       ├─ Managers/   # GameManager, GestorAudio, MenuManager,
 │       │              #   PersonalizacionManager, SeleccionPersonajesManager
 │       └─ UI/         # VistaJuegoUI, BotonValor, EfectoHoverCarta, LoadingScreen
 ├─ Tests/         # EditMode: MONATAC.Tests.asmdef, JugadorTests, PartidaTests
 ├─ Audio/         # Música y efectos de sonido
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

## 🤖 Uso de Inteligencia Artificial

En este proyecto usé herramientas de IA como **apoyo**, no como reemplazo del trabajo propio:
- **Diseño (ChatGPT):** generación de ideas, conceptos y arte.
- **Asesoría técnica y código (Claude Code, OpenCode):** consultas sobre arquitectura (capas, SOLID,
  patrones), asistencia para escribir y refactorizar código, resolución de errores y documentación.

Todas las decisiones de diseño, la arquitectura y la comprensión del código son propias: revisé,
entendí y validé cada cambio. La IA funcionó como tutor y asistente para acelerar el desarrollo y
reforzar el aprendizaje de conceptos (POO, MVP, DIP, testing).

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
- **Diagrama de flujo del código (FigJam):** https://www.figma.com/board/d5MkaaMNGyEBPYqk24bqyR/MONATAC-flujo

---

## 📌 Estado del proyecto

🎮 **Jugable de principio a fin** — menú, configuración inicial, selección de personaje, partida de
2 a 4 jugadores por turnos, sistema completo de cartas (54), condición de victoria y **pantalla de fin
con reinicio / volver al menú**. Arquitectura por capas con MVP + DIP.

**Hito 4 (Juice & Polish):** sistemas de **UI**, **audio** (`GestorAudio`) y **animaciones/VFX**
integrados (Game Feel), flujo completo de usuario y **pruebas unitarias** (EditMode/NUnit). Ver el
apartado *Testing & Bugfix* y **[`Reporte_Testing_y_Bugfix.md`](Reporte_Testing_y_Bugfix.md)**.
Materia Programación en Videojuegos II.
