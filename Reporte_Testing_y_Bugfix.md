# 🧪 Reporte de Testing & Bugfix — MONATAC (Hito 4)

Documento del **Hito 4** con (1) las pruebas unitarias implementadas y cómo ejecutarlas, y
(2) las correcciones de errores realizadas.

---

## 1. Pruebas unitarias (Unit Testing)

**Framework:** Unity Test Framework (NUnit) · **Tipo:** EditMode (lógica pura, sin abrir el juego).
**Ubicación:** `Assets/Tests/EditMode/`.
**Resultado:** 18 métodos de test (4 archivos) → **~26 casos**, todos en **verde** ✅.

### Cómo ejecutar los tests
1. En Unity: `Window → General → Test Runner`.
2. Pestaña **EditMode**.
3. Botón **Run All**.
4. Todos los casos deben quedar en verde.

> Se testean en EditMode porque las clases de `/Data` (`Jugador`, `Cartas`, `Mazo`) y `/Rules`
> (`Partida`) son **clases C# puras** (no `MonoBehaviour`): se instancian con `new` y se prueban sin
> ejecutar el juego, en milisegundos.

### Lista de tests

#### `JugadorTests.cs` — capa Data (`Jugador`)
| Test | Caso de prueba que cubre | N° casos |
|---|---|---|
| `RecibirDanio_NuncaBajaDeCero` | Al recibir daño, el HP nunca queda negativo | 3 |
| `Curar_NoSuperaElMaximo` | Al curarse, el HP nunca supera el `hpMaximo` | 3 |
| `GastarMonedas_SoloSiAlcanza` | Solo gasta monedas si le alcanza (si no, devuelve `false`) | 3 |
| `EstaVivo_EsFalsoConCeroHP` | Con 0 HP el jugador está muerto | 1 |
| `EstablecerMonedas_NuncaNegativo` | Las monedas nunca quedan en negativo | 3 |
| `GanarMonedas_SumaLasMonedas` | Ganar monedas las suma al total | 1 |

#### `PartidaTests.cs` — capa Rules (`Partida`)
| Test | Caso de prueba que cubre |
|---|---|
| `VerificarVictoria_UltimoVivoGana` | Si queda un solo jugador vivo → la partida termina y ese es el ganador |
| `VerificarVictoria_ConVariosVivosNoTermina` | Con varios vivos → no termina y no hay ganador |
| `PasarTurno_SalteaAlJugadorMuerto` | El turno saltea a los jugadores muertos |
| `FinPorRondas_GanaElDeMasHP` | Al agotarse las rondas, gana el jugador con más HP |
| `LeyMarcial_SeActivaYSeGasta` | La Ley Marcial se activa y se consume con los turnos |
| `CambiarObjetivo_NuncaApuntaAlActual` | El objetivo nunca es el jugador en turno |

#### `CartasTests.cs` — capa Data (`Cartas`)
| Test | Caso de prueba que cubre |
|---|---|
| `AplicaA_PasivaSoloSuAccion` | Una carta pasiva solo aplica a su propia acción |
| `AplicaA_ComodinDadoSirveParaCualquierAccion` | El comodín de dado aplica a cualquier acción |
| `AplicaA_DefensivasNuncaAplican` | Las cartas defensivas no se eligen a mano |
| `CartaReaccion_AbsorbeSegunLasMonedas` | El escudo absorbe 1 de daño por cada 2 monedas |

#### `MazoTests.cs` — capa Data (`Mazo`)
| Test | Caso de prueba que cubre |
|---|---|
| `Robar_SacaCartaYVaciaElMazo` | Robar saca la carta; de un mazo vacío devuelve `null` |
| `Reciclar_RecuperaLasCartasDelDescarte` | El mazo circular recupera el descarte y lo deja vacío |

### Ejemplo de un test (patrón Arrange–Act–Assert, data-driven)
```csharp
using NUnit.Framework;

[TestFixture]
public class JugadorTests
{
    [TestCase(40, 10, 30, Description = "Daño normal resta bien")]
    [TestCase(40, 40,  0, Description = "Daño exacto deja en 0")]
    [TestCase(40, 50,  0, Description = "Daño mayor NO baja de 0")]
    public void RecibirDanio_NuncaBajaDeCero(int hpInicial, int danio, int hpEsperado)
    {
        // ARRANGE
        Jugador jugador = new Jugador("Test", hpInicial);
        // ACT
        jugador.RecibirDanio(danio);
        // ASSERT
        Assert.AreEqual(hpEsperado, jugador.hp, "El HP no puede ser negativo.");
    }
}
```

> **Regla crítica de dominio validada:** la **condición de victoria** (`Partida.VerificarVictoria`) y la
> integridad del estado del jugador (HP no negativo, HP no supera el máximo, economía de monedas).

---

## 2. Correcciones de errores (Bugfixes)

### Error destacado (setup de las pruebas): dependencia circular entre assemblies
- **Síntoma:** al separar el código en dos assemblies (`MONATAC.Data` y `MONATAC.Rules`) para poder
  testear, `Cartas.cs` daba `error CS0246: type or namespace not found` y el Test Runner no cargaba.
- **Causa:** las **cartas grupales** (en `/Data`) usan `ContextoGrupal` (en `/Rules`), y a la vez
  `/Rules` usa `/Data`. Al depender **mutuamente**, no pueden ser dos assemblies separados (Unity no
  permite dependencias circulares).
- **Solución:** unificar todo el código del juego en **un solo assembly** (`Assets/Scripts/MONATAC.asmdef`),
  y que el assembly de tests (`MONATAC.Tests`) lo referencie.

### Otras correcciones

| # | Error | Causa | Solución | Archivo |
|---|---|---|---|---|
| 1 | El contador de rondas se rompía si moría el Jugador 1 | Se detectaba la ronda nueva con `IndiceActual == 0`, que asume un índice fijo | Detectar la vuelta a la mesa con `IndiceActual < indiceAnterior` | `Partida.cs` |
| 2 | El juego se congelaba al agotarse las rondas | El `GameManager` llamaba a `IniciarTurno()` sin chequear si la partida ya había terminado | En `OnPasarTurno`: `if (juegoTerminado) { MostrarPantallaFin(); return; }` | `GameManager.cs` |
| 3 | El 3er dado mostraba un valor viejo (al usar acciones de 2 dados) | La animación no limpiaba los dados no usados | Limpiar los textos de los dados sobrantes al inicio de la animación | `AnimacionDados.cs` |
| 4 | No se podía asignar el avatar del jugador en el Inspector | El campo era `Image[]` (UI) pero los avatares son `SpriteRenderer` (mundo) | Cambiar el campo a `SpriteRenderer[]` | `VistaJuegoUI.cs` |

---

**Materia:** Programación en Videojuegos II — Hito 4 · Autor: Braian Zapater ·
Repositorio: https://github.com/wackxion/MONATAC
