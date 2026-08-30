// ============================================================
//  Config.cs  —  Capa: DATA
//  Guarda datos que deben SOBREVIVIR el cambio de escena
//  (del Menú al Juego). Es 'static': existe una sola copia
//  global, sin necesidad de crear un objeto con "new".
// ============================================================

//hecho por pilar
public static class Config
{
    // Cuántos jugadores eligió el menú. Por defecto 4 (si se prueba el juego directo).
    public static int cantidadJugadores = 4;
    
    //hecho por pilar
    // HP máximo de los jugadores. Por defecto 40 (para partidas rápidas).
    public static int hpMaximo = 40;
    
    //hecho por pilar
    // Cantidad de rondas de la partida. Por defecto 0 = sin límite.
    public static int cantidadRondas = 0;
}
