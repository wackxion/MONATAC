// ============================================================
//  ContextoGrupal.cs  —  Capa: RULES
//  "Caja de herramientas" que una carta grupal recibe para poder
//  aplicar su efecto a todos: la lista de jugadores, el mazo, el
//  descarte, quién la compró y la partida (para Ley Marcial).
//  Así cada carta resuelve su efecto sola (polimorfismo), sin que
//  el GameManager tenga un "if" por cada tipo de grupal.
// ============================================================

using System.Collections.Generic;

public class ContextoGrupal
{
    public List<Jugador> jugadores;
    public Mazo mazo;
    public PilaDescarte descarte;
    public Jugador comprador;   // el jugador que compró la carta grupal
    public Partida partida;     // para efectos que tocan las reglas (Ley Marcial)
}

