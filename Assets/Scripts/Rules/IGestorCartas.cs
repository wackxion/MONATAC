// ============================================================
//  IGestorCartas.cs  —  Capa: RULES (reglas)
//  CONTRATO del gestor de cartas: resolver el efecto de las
//  cartas elegidas y el combate defensivo. El GameManager
//  depende de esta abstracción (DIP), no de la clase concreta.
// ============================================================

using System.Collections.Generic;

public interface IGestorCartas
{
    // Avisos generados en la última resolución (pagos, descartes, combate).
    List<string> Mensajes { get; }

    // Efecto de las cartas elegidas sobre una acción.
    int Bonus(List<Carta> seleccionadas, Jugador j, TipoAccion accion);
    int DadosExtra(List<Carta> seleccionadas, TipoAccion accion);
    int Multiplicador(List<Carta> seleccionadas, TipoAccion accion);

    // Combate defensivo del defensor.
    int Absorber(Jugador defensor, int dano);
    void Reflejar(Jugador defensor, Jugador atacante);

    // Manda al descarte las cartas de un solo uso ya gastadas.
    void DescartarUsadas(Jugador j);
}
