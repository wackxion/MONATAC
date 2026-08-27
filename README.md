# 🎲 MONATAC

**Juego de combate por turnos con dados y cartas — Multijugador local (HotSeat) para 2 a 4 jugadores.**

MONATAC es un juego de mesa digital donde la estrategia, la economía de recursos y la
suerte de los dados determinan quién es el último en pie. Cada turno el jugador elige
entre **atacar, curarse o recolectar monedas**, y potencia sus acciones con **cartas**
compradas del mazo.

---

## 🎮 Sobre el juego

| Campo | Detalle |
|---|---|
| **Género** | Combate por turnos con gestión de recursos |
| **Jugadores** | 2 a 4 (multijugador local · HotSeat) |
| **Duración** | 15–30 minutos |
| **Edad** | 12+ |
| **Condición de victoria** | Ser el último jugador con HP mayor a 0 |

**Las tres acciones por turno:**
- ⚔️ **Atacar** — 3d4 de daño a un rival elegido.
- ❤️ **Curarse** — 2d4 de HP recuperado.
- 💰 **Recolectar** — 3d4 monedas y habilita comprar cartas.

**Sistema de cartas (54 cartas):** pasivas, de un uso, de vencimiento, reflectantes,
de reacción, comodines y grupales. Las ofensivas se **eligen** de la mano; las defensivas
se **activan solas** al ser atacado; las grupales **al comprarse**.

---

## 🕹️ Cómo se juega

1. En el **menú**, elegís **2, 3 o 4 jugadores**.
2. En tu turno: **elegís una acción** → (opcional) **elegís cartas** de tu mano y/o **cambiás el objetivo** → **Lanzás los dados** → **Pasás el turno**.
3. Si Recolectaste, podés **comprar cartas** (6 monedas c/u) o **acumular** monedas.
4. Gana el **último jugador con vida**.

---

## 🛠️ Pila tecnológica

| Herramienta | Uso |
|---|---|
| **Unity** (motor) | Desarrollo del videojuego |
| **C#** | Lenguaje de scripting |
| **Git + GitHub** | Control de versiones |
| **Visual Studio / Rider** | Editor de código |

> **Por qué Unity:** motor estándar de la industria, gran comunidad y documentación,
> C# como lenguaje (alineado con la formación previa), y herramientas de UI ideales para
> un juego por turnos basado en cartas y menús.

---

## 🏛️ Arquitectura

El código está separado en **tres capas** con responsabilidades distintas:

- **`/Data`** — qué **es** el juego (estado y entidades): `Jugador`, `Carta` (+ subtipos), `Mazo`, `PilaDescarte`, `Config`, `Enums`.
- **`/Rules`** — qué **puede pasar** (lógica sin Unity): `Dado`, `FabricaDeCartas`.
- **`/Visual`** — cómo se **ve y se controla** (MonoBehaviours): `GameManager`, `MenuManager`.

**Patrones de diseño aplicados:**
- **Singleton** → `GameManager` (una sola instancia global).
- **Factory** → `FabricaDeCartas` (arma las 54 cartas en un solo lugar).
- **Herencia + Polimorfismo** → jerarquía de `Carta` (extensible con nuevos tipos).
- **Encapsulamiento** → `Jugador` protege su estado con propiedades de solo lectura.

---

## 📁 Estructura del proyecto

```
Assets/
 ├─ Scenes/        # MENU y juego
 ├─ Scripts/
 │   ├─ Data/      # Enums, Jugador, Cartas, Mazo, Config
 │   ├─ Rules/     # Dado, FabricaDeCartas
 │   └─ Visual/    # GameManager, MenuManager, EfectoHoverCarta
 ├─ Sprites/       # Arte de las cartas
 └─ Settings/      # Configuración de render (URP)
ProjectSettings/   # Configuración del proyecto Unity
Packages/          # Dependencias
```

---

## 🚀 Cómo abrir el proyecto

1. Clonar el repositorio:
   ```bash
   git clone https://github.com/wackxion/MONATAC.git
   ```
2. Abrir **Unity Hub** → `Add project from disk` → seleccionar la carpeta clonada.
3. Abrir el proyecto con la versión de Unity indicada en `ProjectSettings/ProjectVersion.txt`.
4. Abrir la escena **`MENU`** en `Assets/Scenes/` y presionar **Play**.

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

## 📄 Documentación

La documentación del proyecto está disponible en el **repositorio** y en la **carpeta de Google Drive**:
- **UML** — diagramas de clases, estados y secuencia.
- **Backlog de programación** — tareas del proyecto y su estado.
- **Documento MDA** y **reglamento oficial** — diseño y reglas del juego.

🔗 **[Carpeta del proyecto en Drive](https://drive.google.com/drive/folders/137EptYPsfEYhwZU-GM0MS9eKm2sp86rI?usp=sharing)**

---

## 📌 Estado del proyecto

🎮 **Jugable** — menú, 2 a 4 jugadores por turnos, sistema completo de cartas (54) y
condición de victoria funcionando. En desarrollo continuo (pulido visual y mejoras de
arquitectura). Materia Programación en Videojuegos II.
