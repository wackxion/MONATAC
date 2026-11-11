# 📐 Diagrama UML (Online / Red) — MONATAC

Diagrama de clases del proyecto, **dónde se usan los RPCs** y **qué datos se sincronizan** (lo que pide la
consigna del TFI). Los diagramas están en **Mermaid**: GitHub los renderiza solo. Complementa al UML de
clases del modo HotSeat ([`UML.md`](UML.md)) y al informe técnico ([`Informe-Tecnico.md`](Informe-Tecnico.md)).

> **Arquitectura en 3 capas** (regla de oro del proyecto): `/Data` y `/Rules` son **C# puro** (no conocen
> Unity ni la red); `/Visual` es todo lo de Unity. El online **reusa** `/Rules` tal cual en el servidor.

---

## 1) Capas y dependencias

```mermaid
flowchart TB
    subgraph Visual["/Visual - Unity + Red (NGO)"]
        EO[EstadoOnline]
        SR[SesionRed]
        VO["VistaOnline / ManoOnline / InfoRed"]
        GM["GameManager (HotSeat)"]
    end
    subgraph Rules["/Rules - C# puro (reglas)"]
        PA["Partida (IPartida)"]
        GC[GestorCartas]
        FC[FabricaDeCartas]
        AC[Accion]
    end
    subgraph Data["/Data - C# puro (datos)"]
        JU[Jugador]
        CA[Carta]
        MZ["Mazo / PilaDescarte"]
        DA[Dado]
        RG["Reglas / Catalogo"]
    end
    Visual --> Rules --> Data
    Visual -. reusa .-> Data
    classDef pure fill:#e8f5e9,stroke:#43a047
    class Rules,Data pure
```

**Por qué importa (defensa):** la lógica no depende del motor ni de la red → el servidor corre `Partida`
sin cambios y los clientes nunca calculan nada (servidor autoritativo).

---

## 2) Clases del dominio (`/Data` + `/Rules`)

```mermaid
classDiagram
    class IPartida {
        <<interface>>
        +List~Jugador~ Jugadores
        +int IndiceActual
        +int IndiceObjetivo
        +bool Terminada
        +Jugador Ganador
        +int RondaActual
        +int TotalRondas
        +PasarTurno()
        +CambiarObjetivo()
        +ElegirObjetivoPorDefecto()
        +VerificarVictoria()
        +LeyMarcialActiva() bool
    }
    class Partida
    IPartida <|.. Partida

    class Jugador {
        +string nombre
        +int hp
        +int hpMaximo
        +int monedas
        +List~Carta~ mano
        +RecibirDanio(int)
        +Curar(int)
        +GastarMonedas(int) bool
        +EstaVivo() bool
    }
    Partida "1" *-- "2..4" Jugador
    Jugador "1" o-- "0..*" Carta : mano

    class Carta {
        <<abstract>>
        +string nombre
        +AplicaA(TipoAccion) bool
        +BonusPara(TipoAccion) int
        +Multiplicador(TipoAccion) int
        +DadosExtra(TipoAccion) int
    }
    class CartaGrupal {
        <<abstract>>
        +AplicarATodos(ContextoGrupal) string
    }
    Carta <|-- CartaPasiva
    Carta <|-- CartaUnUso
    Carta <|-- CartaVencimiento
    Carta <|-- CartaReflectante
    Carta <|-- CartaReaccion
    Carta <|-- CartaComodinMultiplicador
    Carta <|-- CartaComodinDado
    Carta <|-- CartaBolsilloRoto
    Carta <|-- CartaVampirismoDefensivo
    Carta <|-- CartaGrupal
    CartaGrupal <|-- CartaColecta
    CartaGrupal <|-- CartaExceso
    CartaGrupal <|-- CartaDescarteGrupal
    CartaGrupal <|-- CartaPenitencia
    CartaGrupal <|-- CartaLeyMarcial

    class Accion {
        <<abstract>>
        +int CantidadDados
        +Aplicar(IPartida, int)
    }
    Accion <|-- AccionAtacar
    Accion <|-- AccionCurarse
    Accion <|-- AccionRecolectar
    Accion <|-- AccionDescartar

    class Mazo {
        +Robar() Carta
        +Mezclar()
        +Reciclar(PilaDescarte)
    }
    class PilaDescarte
    class Dado {
        +Tirar() int
    }
    class GestorCartas {
        +Bonus(...) int
        +Absorber(...) int
        +Reflejar(...)
    }
    class FabricaDeCartas {
        +CrearMazo() Mazo
    }
    Mazo o-- Carta
    FabricaDeCartas ..> Mazo
    GestorCartas ..> Carta
    Accion ..> IPartida
```

**Patrones visibles:** *herencia + polimorfismo* (Carta y Accion), *Factory* (FabricaDeCartas), *DIP*
(todos dependen de `IPartida`, no de `Partida`).

---

## 3) Capa de red (`/Visual/Online`)

```mermaid
classDiagram
    class EstadoOnline {
        <<NetworkBehaviour>>
        +NetworkVariable~int~ turnoActual
        +NetworkVariable~int~ objetivoActual
        +NetworkVariable~int~ codigoSala
        +NetworkVariable~int~ hpMaximoSala
        +NetworkVariable~int~ generacionPartida
        +NetworkVariable~int~ rondaActual
        +NetworkVariable~int~ rondasTotales
        +NetworkVariable~bool~ turnoYaActuo
        +NetworkVariable~bool~ turnoResolviendo
        +NetworkVariable~bool~ leyMarcialRed
        +NetworkVariable~bool~ partidaTerminada
        +NetworkVariable~double~ turnoTerminaEn
        +NetworkList~EstadoJugador~ jugadores
        -IPartida partida
        -Dado dado
        -Mazo mazo
    }
    note for EstadoOnline "partida/dado/mazo existen SOLO en el servidor"
    class EstadoJugador {
        <<struct>>
        +int hp
        +int monedas
        +int cartas
        +int personaje
        +ulong clientId
        +FixedString nombre
    }
    class SesionRed {
        <<DontDestroyOnLoad>>
        +int CodigoSala
        +int HpInicialConfig
        +int RondasConfig
        +int TiempoTurnoConfig
        +bool PartidaIniciada
        +NumeroDe(ulong) int
        +ClientIdDe(int) ulong
    }
    class VistaOnline {
        +ActualizarBotones()
    }
    class ManoOnline {
        +ClickCarta(int)
    }
    EstadoOnline "1" o-- "2..4" EstadoJugador : NetworkList
    EstadoOnline ..> IPartida : crea/usa «server»
    EstadoOnline ..> SesionRed : roster + config
    VistaOnline ..> EstadoOnline : lee estado
    ManoOnline ..> EstadoOnline : lee mano / envía click
```

**Clave:** `EstadoOnline` tiene la `Partida` **solo en el servidor**; a los clientes les llega todo como
NetworkVariables + NetworkList (el "espejo"). Los *presenters* (`VistaOnline`, `ManoOnline`, …) solo
**leen** ese espejo y dibujan. `SesionRed` mantiene el roster `clientId → número` y, para la **reconexión**,
reconoce a un jugador que vuelve por un `playerId` estable (Connection Approval).

---

## 4) RPCs — dónde y para qué

Todos viven en **`EstadoOnline`**. Tres patrones según `SendTo`:

| RPC | SendTo | Dirección | Para qué |
|---|---|---|---|
| `SiguienteTurnoRpc` | Server | cliente → server | pasar turno |
| `AccionRpc` | Server | cliente → server | atacar / curarse / recolectar (con cartas) |
| `CambiarObjetivoRpc` | Server | cliente → server | rotar a quién apunta |
| `DescartarRpc` | Server | cliente → server | descartar una carta |
| `ComprarRpc` | Server | cliente → server | comprar una carta |
| `PedirMiManoRpc` | Server | cliente → server | pedir mi mano privada (también al reconectar) |
| `ReiniciarRpc` | Server | cliente → server | revancha |
| `ClienteListoRpc` | Server | cliente → server | avisar que cargó (pantalla de carga) |
| `MostrarTiradaRpc` | Everyone | server → todos | difundir la tirada + animar dados |
| `FinRpc` / `OcultarFinRpc` | Everyone | server → todos | mostrar / ocultar pantalla de fin |
| `SonidoRpc` | Everyone | server → todos | reproducir un SFX |
| `MostrarGrupalRpc` | Everyone | server → todos | animar la carta grupal |
| `TodosListosRpc` | Everyone | server → todos | ocultar la pantalla de carga |
| `ResultadoRpc` | SpecifiedInParams | server → **emisor** | resultado de la acción (OK / rechazado) |
| `ManoPrivadaRpc` | SpecifiedInParams | server → **dueño** | la mano privada de cada jugador |

### Secuencia de una acción (ejemplo: Atacar)

```mermaid
sequenceDiagram
    participant C as Cliente (en turno)
    participant S as Servidor (Partida)
    participant T as Todos los clientes
    C->>S: AccionRpc(Atacar, cartas)
    S->>S: valida (turno, ya actuó, ley marcial...)
    S->>T: MostrarTiradaRpc(valores)
    Note over T: animan los dados
    Note over S: espera a que paren los dados (corrutina)
    S->>S: aplica efecto en Partida (daño + defensas)
    S->>T: SonidoRpc(Danio)
    S-->>T: NetworkList jugadores (HP/monedas) actualizada
    S->>C: ResultadoRpc("OK: Atacar")
    S->>T: ManoPrivadaRpc(mano) a cada dueño
```

---

## 5) Datos sincronizados

**NetworkVariables** (las escribe **solo el server**, las leen todos):

| Variable | Tipo | Qué representa |
|---|---|---|
| `turnoActual` | int | de quién es el turno |
| `objetivoActual` | int | a quién apunta el ataque |
| `codigoSala` | int | código de la sala (ID visible) |
| `hpMaximoSala` | int | HP máximo (para llenar las barras) |
| `generacionPartida` | int | nº de partida (resetea la UI en la revancha) |
| `rondaActual` / `rondasTotales` | int | contador de rondas |
| `turnoYaActuo` | bool | el jugador en turno ya tiró/descartó |
| `turnoResolviendo` | bool | los dados están girando |
| `leyMarcialRed` | bool | ley marcial activa |
| `partidaTerminada` | bool | ya hay ganador |
| `turnoTerminaEn` | double | instante (ServerTime) en que vence el turno (tiempo límite) |

**NetworkList** `jugadores : NetworkList<EstadoJugador>` — por jugador: `hp, monedas, cartas (cantidad),
personaje, clientId, nombre`. (El **contenido** de la mano NO va acá: es privado.)

**Privado (ClientRpc dirigido):** la **mano** de cada jugador viaja solo a su dueño por `ManoPrivadaRpc`
(nombres de cartas), y el **resultado** de cada acción solo al emisor por `ResultadoRpc`.

**Por qué así (defensa):** lo público (HP, turno, objetivo…) va por NetworkVariables/NetworkList para que
todos vean lo mismo; lo privado (tu mano) va por RPC dirigido para no filtrarlo a los rivales. El contador
de turno sincroniza **un instante de fin** (no los segundos frame a frame): cada cliente calcula lo que
resta localmente con `ServerTime`.
