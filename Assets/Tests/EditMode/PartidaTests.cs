// ============================================================
//  PartidaTests.cs  —  Pruebas unitarias (EditMode / NUnit)
//  Verifica las reglas críticas de la clase Partida (capa Rules):
//  la CONDICIÓN DE VICTORIA y el paso de turno salteando muertos.
//  Es lógica pura: se crea con "new Partida(...)" sin abrir el juego.
// ============================================================

using NUnit.Framework;

[TestFixture]
public class PartidaTests
{
    // ------------------------------------------------------------
    //  REGLA (victoria): si queda UN SOLO jugador vivo, la partida
    //  termina y ese jugador es el ganador.
    // ------------------------------------------------------------
    [Test]
    public void VerificarVictoria_UltimoVivoGana()
    {
        // ARRANGE: partida de 2 jugadores
        Partida partida = new Partida(2, 40);
        // Matamos al Jugador 2 (índice 1)
        partida.Jugadores[1].RecibirDanio(40);

        // ACT
        partida.VerificarVictoria();

        // ASSERT: terminó y ganó el Jugador 1 (índice 0)
        Assert.IsTrue(partida.Terminada, "La partida debería haber terminado.");
        Assert.AreEqual(partida.Jugadores[0], partida.Ganador, "El ganador debería ser el último vivo.");
    }

    // ------------------------------------------------------------
    //  REGLA (no victoria): si hay más de un vivo, la partida sigue.
    // ------------------------------------------------------------
    [Test]
    public void VerificarVictoria_ConVariosVivosNoTermina()
    {
        // ARRANGE: partida de 3 jugadores, todos vivos
        Partida partida = new Partida(3, 40);

        // ACT
        partida.VerificarVictoria();

        // ASSERT
        Assert.IsFalse(partida.Terminada, "Con varios vivos la partida no debe terminar.");
        Assert.IsNull(partida.Ganador, "No debería haber ganador todavía.");
    }

    // ------------------------------------------------------------
    //  REGLA (turno circular): PasarTurno saltea a los jugadores
    //  muertos y le da el turno al siguiente que esté vivo.
    // ------------------------------------------------------------
    [Test]
    public void PasarTurno_SalteaAlJugadorMuerto()
    {
        // ARRANGE: 3 jugadores. Turno arranca en el Jugador 1 (índice 0).
        Partida partida = new Partida(3, 40);
        // Matamos al Jugador 2 (índice 1) para que lo saltee.
        partida.Jugadores[1].RecibirDanio(40);

        // ACT: pasa el turno desde el índice 0
        partida.PasarTurno();

        // ASSERT: debería saltear el índice 1 (muerto) e ir al índice 2
        Assert.AreEqual(2, partida.IndiceActual, "Debe saltear al muerto y darle el turno al siguiente vivo.");
    }
}
