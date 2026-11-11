# 🤖 Declaración de uso de IA generativa — MONATAC

**Trabajo Final Integrador · Programación de Videojuegos II · UNaHur**
**Alumno:** Braian Zapater · **Modalidad:** individual

---

## Herramienta utilizada
Para parte del desarrollo usé **IA generativa** (Claude / Claude Code, de Anthropic) como **asistente de
programación y documentación**, trabajando de manera guiada: yo marcaba qué quería, la IA proponía código
o texto, y yo lo **revisaba, integraba, probaba y corregía** antes de darlo por válido.

## En qué partes se usó
- **Capa de red (online):** adaptación de la lógica del juego al modelo cliente/servidor con Netcode for
  GameObjects — scripts como `EstadoOnline`, `VistaOnline`, `ManoOnline`, `SesionRed` (RPCs, sincronización
  por `NetworkVariable`/`NetworkList`, flujo de turno, pantalla de carga, manejo de conexión).
- **Pulido del online:** conectar sonidos, estados de botones, contador de rondas, animación de la carta
  grupal, reusando componentes que ya existían en el modo HotSeat.
- **Features de red avanzadas:** **tiempo límite de turno** (con `ServerTime`) y **reconexión de clientes**
  (Connection Approval + `playerId` estable para volver al mismo lugar).
- **Documentación:** redacción y organización de notas de estudio/defensa, el **diagrama UML** y el
  **informe técnico**, a partir del código real del proyecto.
- **Depuración:** ayuda para encontrar y explicar la causa de algunos bugs (ej. sincronización de nombres,
  estado de red que quedaba colgado al reconectar).

## En qué NO se usó / qué hice yo
- El **diseño del juego** y las **decisiones** (reglas, balance, qué construir y en qué orden) fueron mías.
- Todo el **trabajo en el editor de Unity** (armar escenas, conectar componentes en el Inspector, prefabs,
  UI, importar assets) lo hice yo.
- Las **pruebas** de cada funcionalidad (abrir dos instancias, jugar, verificar) las hice yo; la IA no
  ejecutó el juego.
- La **base del proyecto** (modo HotSeat, arquitectura en capas `/Data` `/Rules` `/Visual`, sistema de
  cartas) es trabajo previo propio sobre el que se construyó el online.

## Comprensión del código
Entiendo el código entregado y puedo **explicarlo y defenderlo**: por qué el servidor es autoritativo, qué
hace cada tipo de RPC, qué datos se sincronizan y por qué, y cómo la separación en capas permitió llevar el
juego a la red. Usé la IA como apoyo para avanzar más rápido, no como reemplazo de entender lo que hago.
