// ============================================================
//  Reglas.cs  —  Capa: DATA
//  Constantes de las REGLAS del juego: los "números" que antes
//  estaban sueltos en la lógica ahora viven acá, con nombre y en
//  UN SOLO lugar. Si cambia una regla, se toca solo este archivo.
//  Es lógica pura: no depende de Unity.
// ============================================================

public static class Reglas
{
    // Cuánto cuesta comprar una carta del mazo (en monedas).
    public const int CostoCarta = 6;

    // Máximo de cartas que puede tener un jugador en la mano.
    public const int ManoMaxima = 5;

    // Monedas que se gastan para absorber 1 punto de daño (carta Escudo de Monedas).
    public const int MonedasPorAbsorcion = 2;
}
