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

    // Desde cuántas monedas recolectadas se considera una recolección "alta"
    // (Recolectar tira 3 dados: rango 3-18, promedio ~10,5). Se usa para el SFX.
    public const int RecoleccionAlta = 11;

    // --- Dados (balance) ---
    // Caras de cada dado (d4). Cambiar acá afecta el rango de todas las tiradas.
    public const int CarasDado = 4;

    // Cuántos dados tira cada acción. Subir/bajar estos números ajusta el "poder"
    // de cada acción sin tocar la lógica (Accion.cs los lee de acá).
    public const int DadosAtacar     = 3;
    public const int DadosCurarse    = 2;
    public const int DadosRecolectar = 3;
    public const int DadosDescartar  = 0;   // Descartar no tira dados
}
