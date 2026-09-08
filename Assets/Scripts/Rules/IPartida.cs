// ============================================================
//  IPartida.cs  —  Capa: RULES (reglas)
//  CONTRATO de la Partida: lo que el resto del juego necesita
//  pedirle a una partida, SIN atarse a la clase concreta.
//  Base del principio DIP: el GameManager (y el PresentadorJuego)
//  dependen de esta abstracción, no de la clase Partida directamente.
// ============================================================

using System.Collections.Generic;

public interface IPartida
{
    // --- Estado (solo lectura) ---
    List<Jugador> Jugadores { get; }
    int IndiceActual { get; }
    int IndiceObjetivo { get; }
    bool Terminada { get; }
    Jugador Ganador { get; }
    int RondaActual { get; }
    int TotalRondas { get; }

    // --- Consultas ---
    Jugador Actual();
    Jugador Objetivo();
    bool LeyMarcialActiva();

    // --- Acciones sobre la partida ---
    void PasarTurno();
    void CambiarObjetivo();
    void ElegirObjetivoPorDefecto();
    void VerificarVictoria();
    void ActivarLeyMarcial();
}
