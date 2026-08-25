// ============================================================
//  Enums.cs  —  Capa: DATA (datos)
//  Un "enum" (enumeración) es una lista fija de valores con nombre.
//  Sirve para no usar números o textos sueltos ("magic strings"):
//  en vez de escribir "atacar", usamos TipoAccion.Atacar, que el
//  compilador entiende y no se puede escribir mal.
// ============================================================

// Las tres acciones que un jugador puede elegir en su turno.
public enum TipoAccion
{
    Atacar,      // hace daño a un rival
    Curarse,     // recupera HP propio
    Recolectar   // gana monedas
}

// Los seis tipos de carta del juego (según cuándo actúa cada una).
public enum TipoCarta
{
    Pasiva,       // bonus fijo mientras esté activa
    UnUso,        // se usa una vez y se descarta
    Vencimiento,  // dura X turnos pagando mantenimiento
    Reflectante,  // reacciona cuando te atacan
    Reaccion,     // reacciona cuando recibís daño (ej. Escudo de Monedas)
    Grupal        // afecta a todos los jugadores a la vez
}
