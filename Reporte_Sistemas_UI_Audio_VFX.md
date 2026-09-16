# 🎛️ Reporte de Integración de Sistemas — UI · Audio · VFX (Hito 4)

Detalle técnico de los sistemas de **Interfaz**, **Audio** y **Animación/VFX** que forman el
**Game Feel** de MONATAC. Cada sistema supera el mínimo de 3 elementos que pide el hito.

---

## 🖥️ 1. Sistema de UI (Interfaz de Usuario)

Toda la UI vive dentro de un `Canvas` y se dibuja desde la clase **`VistaJuegoUI`** (la Vista del
patrón MVP), que **solo muestra** el estado — no toca las reglas. Elementos en **tiempo real**:

| # | Elemento | Implementación técnica |
|---|---|---|
| 1 | **Barras de HP** | `Image` en modo *Filled* con `fillAmount = hp / hpMaximo` (0 a 1). Se colorean según turno/objetivo/rival. |
| 2 | **Indicador de turno** | `TextMeshProUGUI` con "Turno de Jugador X" + marcas (turno)/(objetivo)/(eliminado). |
| 3 | **Contador de ronda** | `TextMeshProUGUI` "Ronda N/Total" (o solo "Ronda N" si no hay límite). |
| 4 | **Contador de monedas** | `TextMeshProUGUI` con las monedas del jugador en turno. |
| 5 | **Mano de cartas** | Slots con `Image` (sprite de cada carta) + marca `[USAR]`. |

**Conexión UI → lógica:** los `Button` disparan por su `onClick` los métodos públicos del
`GameManager` (ej. `OnAtacar`, `OnLanzarDados`). El `EventSystem` detecta los clics.
**Archivos:** `VistaJuegoUI.cs`, `GameManager.cs`.

---

## 🔊 2. Sistema de Audio

Centralizado en **`GestorAudio`** (patrón **Singleton** + `DontDestroyOnLoad`), con 2 `AudioSource`
(uno para música, otro para efectos). **3 fuentes diferenciadas:**

| # | Fuente | Implementación técnica |
|---|---|---|
| 1 | **Música de fondo (BGM)** | `AudioSource` con `loop = true` + `Play()`. `DontDestroyOnLoad` mantiene la música **sin cortes** entre escenas. |
| 2 | **SFX de UI (clic)** | `PlayOneShot(clic)`. Se engancha a **todos los botones** automáticamente en cada carga de escena (`SceneManager.sceneLoaded` + `FindObjectsByType<Button>` + `onClick.AddListener`). |
| 3 | **SFX de gameplay** | `PlayOneShot(...)` para **daño** (al atacar), **curación** (al curarse) y **dados** (al tirar), llamados desde el `GameManager`. |

**Diferencia técnica clave:** la música usa `Play()` (un clip continuo); los efectos usan
`PlayOneShot()` (varios que pueden solaparse sin cortarse).
**Archivos:** `GestorAudio.cs`, `GameManager.cs`.

---

## 🎬 3. Sistema de Animación / VFX

Animaciones **por código** (corrutinas `IEnumerator` + interpolación con `Mathf.Lerp`), que dan el
"Juice". **Efectos implementados:**

| # | Efecto | Evento que lo dispara | Implementación técnica |
|---|---|---|---|
| 1 | **Dados girando** | Al lanzar los dados | Corrutina que muestra valores random y frena uno por uno (`AnimacionDados`). Usa **callback** al terminar. |
| 2 | **Barra de HP suave** | Al cambiar el HP | Corrutina con `Mathf.Lerp` + `AnimationCurve` (`AnimacionBarraHP`), en vez de saltar de golpe. |
| 3 | **Números flotantes** | Al recibir daño / curarse | `Instantiate` de un prefab que sube y se desvanece bajando el alfa (`NumeroFlotante` + `SpawnerNumerosFlotantes`). |
| 4 | **Carta grupal** | Al comprar una carta grupal | Fade in/out del reverso interpolando `Color.a` (`AnimacionCartaGrupal`). |
| 5 | **Pantalla de carga** | Entre menú y juego | Barra de progreso con carga asíncrona (`LoadingScreen`). |

**Técnica común:** una **corrutina** avanza un valor `t` de 0 a 1 y se cambia una propiedad
(`fillAmount`, posición, `Color.a`) con ese valor → transición suave.
**Archivos:** `AnimacionDados.cs`, `AnimacionBarraHP.cs`, `NumeroFlotante.cs`,
`SpawnerNumerosFlotantes.cs`, `AnimacionCartaGrupal.cs`, `LoadingScreen.cs`.

---

**Materia:** Programación en Videojuegos II — Hito 4 · Autor: Braian Zapater ·
Repositorio: https://github.com/wackxion/MONATAC
