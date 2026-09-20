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

    // ------------------------------------------------------------
    //  REGLA (fin por rondas): al agotarse las rondas, gana el
    //  jugador con más HP.
    // ------------------------------------------------------------
    [Test]
    public void FinPorRondas_GanaElDeMasHP()
    {
        // ARRANGE: 2 jugadores, límite de 1 ronda. Bajamos el HP del Jugador 1.
        Partida partida = new Partida(2, 40, 1);
        partida.Jugadores[0].RecibirDanio(10);   // Jugador 1 queda con 30; Jugador 2 con 40

        // ACT: pasamos turnos hasta cerrar la ronda (da la vuelta a la mesa).
        partida.PasarTurno();   // J1 -> J2 (sigue ronda 1)
        partida.PasarTurno();   // J2 -> J1 (arranca ronda 2 > límite -> termina)

        // ASSERT: terminó y ganó el de más HP (Jugador 2).
        Assert.IsTrue(partida.Terminada, "Al superar el límite de rondas la partida debe terminar.");
        Assert.AreEqual(partida.Jugadores[1], partida.Ganador, "Debe ganar el jugador con más HP.");
    }

    // ------------------------------------------------------------
    //  REGLA (Ley Marcial): se activa y se va gastando con los turnos.
    // ------------------------------------------------------------
    [Test]
    public void LeyMarcial_SeActivaYSeGasta()
    {
        // ARRANGE: 2 jugadores (dura tantos turnos como jugadores).
        Partida partida = new Partida(2, 40);

        // ACT + ASSERT: al activarla está activa...
        partida.ActivarLeyMarcial();
        Assert.IsTrue(partida.LeyMarcialActiva(), "Recién activada, debe estar activa.");

        // ...y después de 2 turnos (2 jugadores) se agota.
        partida.PasarTurno();
        partida.PasarTurno();
        Assert.IsFalse(partida.LeyMarcialActiva(), "Tras gastarse los turnos, ya no debe estar activa.");
    }

    // ------------------------------------------------------------
    //  REGLA (objetivo): CambiarObjetivo nunca apunta al jugador actual.
    // ------------------------------------------------------------
    [Test]
    public void CambiarObjetivo_NuncaApuntaAlActual()
    {
        // ARRANGE: 3 jugadores.
        Partida partida = new Partida(3, 40);

        // ACT: rotamos el objetivo varias veces.
        for (int i = 0; i < 3; i++)
        {
            partida.CambiarObjetivo();
            // ASSERT: el objetivo siempre es un rival vivo distinto del actual.
            Assert.AreNotEqual(partida.IndiceActual, partida.IndiceObjetivo, "El objetivo no puede ser el jugador en turno.");
        }
    }
}
