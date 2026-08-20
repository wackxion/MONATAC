# 🎲 MONATAC

**Juego de combate por turnos con dados y cartas — Multijugador local (HotSeat) para 2 a 4 jugadores.**

MONATAC es un juego de mesa digital donde la estrategia, la economía de recursos y la
suerte de los dados determinan quién es el último en pie. Cada turno el jugador elige
entre **atacar, curarse o recolectar monedas**, y potencia sus acciones con cartas
compradas a ciegas.

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

## 🚀 Cómo abrir el proyecto

1. Clonar el repositorio:
   ```bash
   git clone https://github.com/wackxion/MONATAC.git
   ```
2. Abrir **Unity Hub** → `Add project from disk` → seleccionar la carpeta clonada.
3. Abrir el proyecto con la versión de Unity indicada en `ProjectSettings/ProjectVersion.txt`.
4. Abrir la escena principal en `Assets/Scenes/` y presionar **Play**.

---

## 📁 Estructura del proyecto

```
Assets/
 ├─ Scenes/        # Escenas del juego
 ├─ Scripts/       # Lógica: Juego, Jugador, Carta, Turno, Accion...
 ├─ Prefabs/       # Objetos reutilizables (cartas, dados, UI)
 ├─ Sprites/       # Arte 2D
 └─ UI/            # Interfaz de usuario
ProjectSettings/   # Configuración del proyecto Unity
Packages/          # Dependencias
```

---

## 📄 Documentación

- **Documento de diseño (MDA):** Mecánicas, Dinámicas y Estéticas del juego.
- **Diagrama UML:** arquitectura de clases principales (en proceso).
- **Reglamento oficial:** reglas completas del juego.

---

## 📌 Estado del proyecto

🚧 En desarrollo — Proyecto base para la materia Programación en Videojuegos II.
