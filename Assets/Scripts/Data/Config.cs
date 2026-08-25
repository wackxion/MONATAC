// ============================================================
//  Config.cs  —  Capa: DATA
//  Guarda datos que deben SOBREVIVIR el cambio de escena
//  (del Menú al Juego). Es 'static': existe una sola copia
//  global, sin necesidad de crear un objeto con "new".
// ============================================================

public static class Config
{
    // Cuántos jugadores eligió el menú. Por defecto 4 (si se prueba el juego directo).
    public static int cantidadJugadores = 4;
}
