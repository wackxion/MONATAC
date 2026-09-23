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

### 🎛️ Mapa de controles

El juego es **100% con el mouse** (HotSeat), con un atajo de teclado para la pausa:

| Control | Acción |
|---|---|
| **Clic izquierdo** | Todo: elegir acción, marcar cartas `[USAR]`, botones de menú |
| **Botón Atacar / Curarse / Recolectar / Descartar** | Elegir la acción del turno |
| **Botón Cambiar Objetivo** | Rotar el rival apuntado (solo al atacar) |
| **Botón Lanzar Dados** | Resolver la jugada |
| **Botón Comprar** | Comprar una carta (6 monedas, tras Recolectar) |
| **Botón Pasar Turno** | Terminar el turno |
| **Tecla `Esc`** *(o botón ⏸)* | Pausar / Reanudar (en gameplay) |
| **Botón ❓ Instrucciones** | Abrir el panel de cómo se juega (en el menú) |
| **Botón Salir** | Cerrar el juego (en el menú) |

> Los botones se **iluminan/apagan** según lo que se puede hacer en cada momento del turno, y las
> cartas usables se **resaltan** al elegir una acción.

---

## 🏛️ Arquitectura de capas

El código está separado en **tres capas** con responsabilidades distintas. `/Data` y
`/Rules` son **C# puro** (no dependen de Unity), por lo que se pueden probar por separado.

- **`/Data`** — el **estado y las entidades** del juego: `Jugador`, `Carta` (+ subtipos),
  `Mazo`, `PilaDescarte`, `Config`, `Reglas` (constantes), `Enums`.
- **`/Rules`** — la **lógica y las reglas**: `Partida` (turnos, objetivo, rondas, victoria),
  `GestorCartas` (resolución de cartas y combate defensivo), `Accion` (+ subtipos),
  `FabricaDeCartas`, `Dado`, `ContextoGrupal`, las interfaces **`IPartida` / `IGestorCartas`** (DIP)
  e **`IVistaJuego` + `PresentadorJuego`** (MVP).
- **`/Visual`** — la **presentación e input** (MonoBehaviours), organizada en subcarpetas:
  - **`Managers/`** — `GameManager` (orquesta), `GestorAudio` (sonido), `MenuManager`, `MenuPausa`,
    `Splash`, `PersonalizacionManager`, `SeleccionPersonajesManager`.
  - **`UI/`** — `VistaJuegoUI` (dibuja la pantalla), `PanelSimple`, `BotonValor`, `EfectoHoverCarta`,
    `LoadingScreen`.
  - **`Animacion/`** — `AnimacionDados`, `AnimacionBarraHP`, `AnimacionCartaGrupal`, `NumeroFlotante`,
    `SpawnerNumerosFlotantes`.

  Solo **muestra** información y **captura** clics; no conoce las reglas ni altera los datos directamente.

**Escenas:** `Splash` (video de intro) · `MENU` (elegir jugadores) · `Loading` (pantalla de carga) ·
`SeleccionPersonajes` (personaje y nombre) · `Personalizacion` (HP y rondas) · `juego` (partida).

---

## 📐 Diagrama UML

El diagrama de clases del proyecto (las 3 capas, las clases de cada una y sus relaciones de herencia,
composición e interfaces) está en **[`UML.md`](UML.md)**.

- **`/Data`** — entidades y estado (`Jugador`, `Carta` + subtipos, `Mazo`, `Config`, `Reglas`, enums).
- **`/Rules`** — reglas puras + interfaces (`Partida`, `GestorCartas`, `Accion` + subtipos, `Dado`,
  `FabricaDeCartas`, `IPartida`, `IGestorCartas`, `IVistaJuego`, `PresentadorJuego`).
- **`/Visual`** — MonoBehaviours (Managers / UI / Animación).

Relaciones destacadas: `Carta` y `Accion` **abstractas** con sus subtipos (herencia/polimorfismo);
`Partida`→`IPartida`, `GameManager`→`IVistaJuego`, `PresentadorJuego`→`IPartida`/`IVistaJuego` (DIP/MVP).

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
6. **Feedback de estado** — los botones se **iluminan/apagan** según lo que se puede hacer en el turno,
   las cartas **usables se resaltan** al elegir una acción y las en uso muestran un **marco**.
7. **Selección de personaje** — cada avatar usa el personaje elegido en la pantalla de selección.

### 🔊 Audio (`GestorAudio`, patrón Singleton) — 3+ fuentes
1. **Música de fondo (BGM)** — en loop, continua entre escenas (`DontDestroyOnLoad`).
2. **SFX de UI** — clic en todos los botones (se enganchan automáticamente al cargar cada escena).
3. **SFX de gameplay** — **daño** (atacar), **curación** (curarse), **dados** (tirar), **comprar** carta,
   **cambiar objetivo**, **recolectar** (dos sonidos según muchas/pocas monedas) y **victoria** en el fin.

### 🎬 Animaciones / VFX — feedback visual
1. **Dados girando** (`AnimacionDados`).
2. **Barra de HP suave** (`AnimacionBarraHP`, con `Mathf.Lerp`).
3. **Números flotantes** de daño/cura (`NumeroFlotante` + `SpawnerNumerosFlotantes`).
4. **Carta grupal** con fade al comprarse (`AnimacionCartaGrupal`).
5. **Pantalla de carga** con barra de progreso (`LoadingScreen`).

### 🧭 Flujo y menús (Hito 5)
1. **Splash** — video de intro en loop; al terminar la 1ª vuelta aparece **Jugar** (`Splash`).
2. **Menú de pausa** — panel con Reanudar / Volver al Menú, tecla `Esc`, congela con `Time.timeScale`
   (`MenuPausa`).
3. **Instrucciones** — panel con las reglas por fases del turno, desde el menú (`PanelSimple`).
4. **Pantalla de fin con estadísticas** — el ganador + HP, monedas y cartas de todos los jugadores.
5. **Botón Salir** — cierra el juego (`Application.Quit`).

> Todos los sistemas **superan el mínimo de 3** que pide el hito.
>
> **Detalle técnico completo** (componentes, código y eventos de cada sistema):
> **[`Reporte_Sistemas_UI_Audio_VFX.md`](Reporte_Sistemas_UI_Audio_VFX.md)**.

---

## 🧪 Testing & 🐛 Bugfix

- **Pruebas unitarias:** 18 tests **EditMode / NUnit** (26 casos, varios *data-driven* con `[TestCase]`)
  sobre la lógica de dominio (`Jugador`, `Partida`, `Cartas`, `Mazo`), todos en verde. Se corren con
  `Window → General → Test Runner → EditMode → Run All`.
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
 ├─ Scenes/        # Splash, MENU, Loading, SeleccionPersonajes, Personalizacion, juego
 ├─ Scripts/       # MONATAC.asmdef (un solo assembly con todo el código del juego)
 │   ├─ Data/      # Enums, Jugador, Cartas, Mazo, Config, Reglas
 │   ├─ Rules/     # Partida, GestorCartas, Accion, FabricaDeCartas, Dado,
 │   │             #   ContextoGrupal, IPartida, IGestorCartas,
 │   │             #   IVistaJuego, PresentadorJuego
 │   └─ Visual/    # presentación e input, organizado en subcarpetas:
 │       ├─ Animacion/  # AnimacionDados, AnimacionBarraHP, AnimacionCartaGrupal,
 │       │              #   NumeroFlotante, SpawnerNumerosFlotantes
 │       ├─ Managers/   # GameManager, GestorAudio, MenuManager, MenuPausa, Splash,
 │       │              #   PersonalizacionManager, SeleccionPersonajesManager
 │       └─ UI/         # VistaJuegoUI, PanelSimple, BotonValor, EfectoHoverCarta, LoadingScreen
 ├─ Tests/         # EditMode: MONATAC.Tests.asmdef, JugadorTests, PartidaTests, CartasTests, MazoTests
 ├─ Audio/         # Música y efectos de sonido
 ├─ Video/         # Video del splash
 ├─ Sprites/       # Arte de las cartas
 └─ Settings/      # Configuración de render (URP)
ProjectSettings/   # Configuración del proyecto Unity
Packages/          # Dependencias
```

---

## 🛠️ Pila tecnológica

| Herramienta | Versión / Uso |
|---|---|
| **Unity** (motor) | **6000.3.8f1** (Unity 6) |
| **C#** | Lenguaje de scripting |
| **Git + GitHub** | Control de versiones |
| **Visual Studio / Rider** | Editor de código |

### 📚 Librerías / paquetes de Unity usados
| Paquete | Versión | Para qué |
|---|---|---|
| **Input System** (`com.unity.inputsystem`) | 1.18.0 | Entrada (tecla Esc de pausa, saltar splash) |
| **Universal Render Pipeline (URP)** (`com.unity.render-pipelines.universal`) | 17.3.0 | Render 2D |
| **uGUI + TextMeshPro** (`com.unity.ugui`) | 2.0.0 | Interfaz y textos |
| **Test Framework** (`com.unity.test-framework`) | 1.6.0 | Pruebas unitarias (NUnit, EditMode) |
| **Video** (`com.unity.modules.video`) | 1.0.0 | Reproducción del splash |
| **Particle System** (`com.unity.modules.particlesystem`) | 1.0.0 | VFX |

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

## 📋 Gestión de proyecto

### Quién hizo qué

| Área | Tareas principales | Responsable(s) |
|---|---|---|
| **Arquitectura y dominio** | Capas Data/Rules/Visual, `Partida`, `GestorCartas`, `Accion`, `Cartas`, `FabricaDeCartas` | Braian *(Pilar en parte de `Partida`)* |
| **Patrones (MVP/DIP)** | `IPartida`, `IGestorCartas`, `IVistaJuego`, `PresentadorJuego` | Braian |
| **UI de juego** | `VistaJuegoUI`, feedback de botones/cartas, estadísticas de fin | Braian |
| **Animaciones / VFX** | `AnimacionDados`, `EfectoHoverCarta`, integración en `GameManager` | Julian |
| **Personalización** | `PersonalizacionManager`, `BotonValor`, `Config` | Julian · Pilar |
| **Menú de inicio** | `MenuManager`, navegación de escenas | Pilar |
| **Audio** | `GestorAudio` (música + SFX) | Braian |
| **Flujo Hito 5** | Splash, pausa, instrucciones, salir, estadísticas de fin | Braian |
| **Testing** | 18 tests EditMode (Jugador, Partida, Cartas, Mazo) | Braian |
| **Documentación** | README, reportes, vault de documentación | Braian |

### Backlog

Backlog de programación (tareas hechas / en progreso / pendientes):
- **Backlog local:** [`Backlog.md`](Backlog.md), organizado por área e hito.
- **Trello:** https://trello.com/invite/b/6a9ec9aebfc402696fe71d44/ATTIdf3bf5328ba3949bab7a2a2d03c60532751D1977/grupo-b

---

## 👥 Integrantes y roles

| Integrante | Rol |
|---|---|
| **Braian Zapater** | Programador principal · Arquitectura (3 capas, MVP, DIP, patrones), lógica de dominio, audio, testing y flujo del Hito 5 |
| **Julian Gabriel Blanco** | Programación de animaciones/VFX (dados, hover de cartas) y pantalla de personalización |
| **Alvarez Pilar** | Configuración de partida, menú de inicio y parte de la lógica de `Partida` |

> Para el **Parcial 1 (Hito 5)** el proyecto lo continúa **Braian Zapater en solitario**. Los roles de
> arriba reflejan el trabajo realizado por el equipo en las etapas previas (Hitos anteriores).

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

**Hito 5 (Parcial 1):** flujo de juego completo — **splash** con video, **menú de pausa**,
**instrucciones**, **botón salir** y **pantalla de fin con estadísticas** — más pulido de código
(constantes en `Reglas`, desuscripción de eventos, OCP explícito) y ampliación de tests (18 tests).
Materia Programación en Videojuegos II.
