# 📝 Informe Técnico — MONATAC

**Trabajo Final Integrador · Programación de Videojuegos II · UNaHur**
**Alumno:** Braian Zapater · **Modalidad:** individual · **Repo:** github.com/wackxion/MONATAC

> Acompaña al diagrama UML de red ([`UML-Online.md`](UML-Online.md)).

---

## 1. El juego

**MONATAC** es un juego de **combate por turnos** con dados y cartas para **2 a 4 jugadores**. En cada
turno, el jugador elige una acción —**Atacar, Curarse o Recolectar** (monedas)—, puede usar **cartas** que
modifican la tirada, y **lanza los dados**. Con las monedas se **compran** más cartas. Gana el último en
pie, o —si se juega con **límite de rondas**— el que tenga más HP al terminar.

Hay **dos modos**: *HotSeat* (varios jugadores en una PC, ya entregado y aprobado) y **Online** (en red
local, el foco de esta etapa). Una partida dura ~5-10 min.

---

## 2. Arquitectura general

El proyecto está dividido en **tres capas** con una regla estricta de dependencias:

| Capa | Qué contiene | Depende de |
|---|---|---|
| **`/Data`** | Datos y entidades: `Jugador`, `Carta` (y variantes), `Mazo`, `Dado`, `Reglas`, catálogo de cartas. **C# puro.** | — |
| **`/Rules`** | Reglas del juego: `Partida` (implementa `IPartida`), `Accion`, `GestorCartas`, `FabricaDeCartas`. **C# puro.** | `/Data` |
| **`/Visual`** | Todo lo de Unity: UI, animaciones, y la **capa de red** (`EstadoOnline`, `SesionRed`, presenters). | `/Rules`, `/Data` |

**`/Data` y `/Rules` no conocen Unity ni la red.** Esto es la decisión de diseño más importante del
proyecto, porque habilita el modelo de red: **el servidor corre la misma `Partida` de `/Rules` sin
tocar una línea**, y los clientes nunca recalculan nada. (Ver el diagrama de capas en el UML.)

**Patrones aplicados:** herencia + polimorfismo (`Carta`, `Accion`), *Factory* (`FabricaDeCartas`),
inversión de dependencias / DIP (todo depende de `IPartida`, no de la clase concreta), y *data-driven*
(las 54 cartas son datos en un catálogo/JSON, no código).

---

## 3. Transporte y modelo de red

- **Librería:** *Netcode for GameObjects* (NGO) `2.13.3`, sobre **Unity Transport (UTP)** — protocolo
  **UDP**, puerto por defecto **7777**.
- **Modelo:** **Host** (un jugador *hostea y juega* a la vez) + **autoridad del servidor**. No hay
  servidor dedicado: el host ES el servidor.
- **Conexión:** se establece en el **menú/lobby**, antes de cargar el juego. El host hace `StartHost()`;
  cada cliente `StartClient()` apuntando a la IP del host. El cambio de escena es **sincronizado por NGO**
  (`NetworkManager.SceneManager.LoadScene`), así todos cargan la misma escena.
- **Identidad:** `SesionRed` (singleton persistente, `DontDestroyOnLoad`) arma el **roster**
  `clientId → número de jugador` por orden de conexión (host = Jugador 0). Cada cliente muestra **su ID y
  el código de sala** durante el juego (requisito del TP).

---

## 4. Responsabilidades del servidor (autoritativo)

El servidor es la **única fuente de verdad**:

- **Tiene la `Partida` real** (de `/Rules`); los clientes **no la tienen**.
- **Tira los dados** (`Dado` vive en el server) → nadie puede falsear una tirada.
- **Valida cada acción** antes de ejecutarla: ¿es tu turno?, ¿ya actuaste?, ¿hay ley marcial?, ¿se están
  resolviendo los dados?, ¿te alcanzan las monedas?, etc. Si algo no corresponde, **rechaza** y se lo
  informa solo al que la pidió.
- **Resuelve las reglas** (daño, curación, defensas, cartas, fin de partida) y **espeja** el resultado a
  las variables de red.
- Los clientes solo **piden** (por RPC) y **leen** el espejo para dibujar. Esto cumple el criterio del TP
  de que *los clientes no calculan resultados*.

---

## 5. RPCs y eventos

Toda la comunicación del juego vive en **`EstadoOnline`** (un `NetworkBehaviour`). Se usan **tres patrones
de RPC** según a quién va dirigido:

| Patrón (`SendTo`) | Dirección | Ejemplos | Uso |
|---|---|---|---|
| **`Server`** | cliente → servidor | `AccionRpc`, `SiguienteTurnoRpc`, `CambiarObjetivoRpc`, `DescartarRpc`, `ComprarRpc`, `PedirMiManoRpc`, `ReiniciarRpc`, `ClienteListoRpc` | el cliente **pide** algo; el server valida y ejecuta |
| **`Everyone`** | servidor → todos | `MostrarTiradaRpc`, `FinRpc`, `OcultarFinRpc`, `SonidoRpc`, `MostrarGrupalRpc`, `TodosListosRpc` | el server **difunde** un evento (animar dados, fin, sonido, grupal, pantalla de carga) |
| **`SpecifiedInParams`** + `RpcTarget.Single` | servidor → **uno** | `ResultadoRpc` (al emisor), `ManoPrivadaRpc` (al dueño) | información **privada/dirigida** |

El emisor de un RPC se identifica con `rpcParams.Receive.SenderClientId`.

**Eventos de C# (dentro del cliente):** además de los RPCs, hay eventos locales que desacoplan el estado
de la UI: `MiManoCambio` (redibuja la mano), `UICambio` (refresca botones/cartas al elegir acción) y
`GrupalJugada` (dispara la animación de la carta grupal). Y las `NetworkVariable`/`NetworkList` tienen sus
propios `OnValueChanged`/`OnListChanged` a los que la UI se suscribe.

### Flujo de una acción (Atacar) — efecto *después* de la animación

1. El cliente elige la acción (local) y, al **Lanzar dados**, manda `AccionRpc(Atacar, cartas)`.
2. El server **valida**, tira los dados y difunde `MostrarTiradaRpc` → **todos** animan los dados.
3. El server **espera** (corrutina) lo que dura la animación y **recién ahí aplica** el daño, descuenta
   HP y espeja el estado → en todas las pantallas la barra baja cuando los dados paran.
4. Avisa el resultado al emisor (`ResultadoRpc`) y reparte las manos privadas (`ManoPrivadaRpc`).

(El diagrama de secuencia está en [`UML-Online.md`](UML-Online.md).)

---

## 6. Datos sincronizados

**Público — `NetworkVariable` (server escribe, todos leen):** `turnoActual`, `objetivoActual`,
`codigoSala`, `hpMaximoSala`, `generacionPartida` (para la revancha), `rondaActual`/`rondasTotales`
(contador de rondas), las banderas de **fase del turno** `turnoYaActuo`, `turnoResolviendo`,
`leyMarcialRed`, `partidaTerminada` (para que cada cliente sepa qué botones habilitar), y `turnoTerminaEn`
(instante de fin del turno, para el **tiempo límite de turno**).

**Público — `NetworkList<EstadoJugador>`:** una entrada por jugador con `hp`, `monedas`, `cartas`
(cantidad), `personaje`, `clientId` y `nombre`. `EstadoJugador` implementa `INetworkSerializable`
(define cómo viaja) e `IEquatable` (para sincronizar solo lo que cambió).

**Privado — por RPC dirigido:** el **contenido de la mano** de cada jugador viaja solo a su dueño
(`ManoPrivadaRpc`, nombres de cartas separados por `|`) y el **resultado** de cada acción solo al emisor
(`ResultadoRpc`). Así los rivales no ven tus cartas.

**Sincronización por eventos:** nunca se manda "todo el estado" cada frame. El server escribe una
variable/lista **cuando cambia** y NGO propaga el delta; la UI reacciona por `OnValueChanged`/
`OnListChanged`. Los sucesos puntuales (tirada, fin, sonido) van por RPC de broadcast. El **tiempo de
turno** usa este mismo principio: el server fija un **instante de fin** (`turnoTerminaEn`) una vez por
turno y cada cliente calcula los segundos restantes localmente con `ServerTime` (no se spamea la red).

---

## 7. Flujo completo de una partida

**Menú → Online:** Crear (host) o Unirse (IP). **Lobby:** cada uno elige nombre y personaje; el **host**
configura **HP inicial**, **cantidad de rondas** y **tiempo de turno** (con validación: `int.TryParse`,
rangos, mensajes de error, sin excepciones sin capturar). **Inicio:** el host lleva a todos a la escena del
juego (carga en red) y una **pantalla de carga** espera a que todos entren para revelar la partida a la vez.
**Juego:** turnos en 2 pasos (elegir acción → cambiar objetivo / elegir cartas → lanzar dados), con un
**tiempo límite por turno** (si se vence, el server pasa el turno solo). **Fin:** `FinRpc` muestra el
ganador y las estadísticas; **Revancha** (`ReiniciarRpc`) recrea la partida sin desconectar a nadie.
**Desconexión:** si se cae el host, los clientes vuelven al menú; si se cae un **cliente**, queda
**ausente** (conserva su estado) y puede **reconectar** a su mismo lugar; si no vuelve a tiempo, se elimina
y la partida sigue.

---

## 8. Sistema de cartas (profundidad y extensión)

Las cartas son el sistema de **extensión** con profundidad: hay **pasivas, de un uso, de vencimiento,
comodines (dado/multiplicador), reflectantes/defensivas y grupales**. Cada tipo es una subclase de `Carta`
que decide su efecto por **polimorfismo** (`AplicaA`, `BonusPara`, `Multiplicador`, `DadosExtra`). Las
**grupales** afectan a todos (Ley Marcial, Colecta, etc.) y se aplican con `AplicarATodos(ContextoGrupal)`.
Agregar una carta de un tipo existente es **solo un dato** en el catálogo; el mismo sistema corre igual en
HotSeat y en Online porque es `/Rules` puro.

---

## 9. Decisiones de diseño y problemas resueltos

- **Cliente/servidor sin duplicar lógica:** en vez de copiar la `Partida` al cliente, se la dejó **solo en
  el server** y se "espeja" a variables de red. El cliente es un *presenter* que lee el espejo.
- **Efecto después de la animación:** el server resuelve el efecto al terminar la animación de dados
  (corrutina + bandera `resolviendo`), para que el feedback (HP, flotantes, sonido) salga sincronizado en
  todas las pantallas.
- **Pantalla de carga sincronizada:** *handshake* `ClienteListoRpc`/`TodosListosRpc` con un tiempo mínimo,
  para que todos vean la partida recién cuando están todos dentro.
- **Tiempo límite de turno:** el server fija el instante de fin con `ServerTime` (sincronizado por NGO) y,
  en su `Update`, pasa el turno solo cuando se vence; los clientes muestran el contador calculándolo en
  local, sin tráfico extra.
- **Reconexión de clientes:** al caerse, el jugador no se elimina sino que queda **ausente**; con
  *Connection Approval* y un `playerId` estable, el server lo reconoce al volver y lo devuelve a su lugar
  (el estado público llega por *late-join* de las `NetworkVariable`; la mano se reenvía por RPC dirigido).
- **Robustez de conexión:** al unirse sin sala, se **falla rápido** (timeout corto) y se avisa; antes de
  crear/unirse se **cierra** cualquier conexión colgada, para no quedar trabado.
- **Nombre del cliente:** se sincroniza en cada tecla (`onValueChanged`) porque el input del cliente no
  pierde el foco cuando el host inicia.

---

## 10. Conclusión

MONATAC Online cumple el MVP pedido: **servidor + ≥2 clientes en red local, servidor autoritativo,
partida completa con fin claro, configuración validada al inicio, interfaz informativa (turno, estado,
acciones, resultado, pausa y reinicio) y pulido** (animaciones, sonido, feedback). La fortaleza central es
la **arquitectura en capas**: separar reglas puras de la presentación hizo que llevar el juego a la red
fuera *agregar una capa*, no reescribirlo.
