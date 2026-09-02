// ============================================================
//  Accion.cs  —  Capa: RULES (reglas)
//  Representa una ACCIÓN que puede hacer el jugador en su turno.
//  Es una clase ABSTRACTA: define QUÉ tiene toda acción (un nombre,
//  cuántos dados tira y qué efecto aplica), pero NO se puede crear
//  directamente con "new Accion()". Cada acción concreta (Atacar,
//  Curarse, Recolectar) HEREDA de ella y define su comportamiento.
//
//  HERENCIA + POLIMORFISMO: el juego puede guardar una 'Accion' sin
//  saber cuál es; al llamar Aplicar(), cada subclase responde distinto.
// ============================================================
public abstract class Accion
{   
     // El nombre que se muestra en pantalla (ej. "Atacar").
    public abstract string Nombre { get; }
    
    // Cuántos dados de 4 caras (d4) tira esta acción.
    public abstract int CantidadDados { get; }

    // Aplica el efecto de la acción usando el 'total' ya calculado
    // (dados + bonus de cartas). Cada subclase lo hace a su modo.
    public abstract void Aplicar(Partida partida, int total);
}

public class AccionAtacar : Accion
{
    public override string Nombre => "Atacar";
    public override int CantidadDados => 3;

    public override void Aplicar(Partida partida, int total)
    {
        partida.Objetivo().RecibirDanio(total);
    }
}
// --- Acción CURARSE: tira 2 dados y cura al jugador en turno ---
public class AccionCurarse : Accion
{
    public override string Nombre => "Curarse";
    public override int CantidadDados => 2;

    public override void Aplicar(Partida partida, int total)
    {
        // La curación es para uno mismo (el jugador actual).
        partida.Actual().Curar(total);
    }
}

// --- Acción RECOLECTAR: tira 3 dados y suma monedas al jugador en turno ---
public class AccionRecolectar : Accion
{
    public override string Nombre => "Recolectar";
    public override int CantidadDados => 3;

    public override void Aplicar(Partida partida, int total)
    {
        // Las monedas son para uno mismo (el jugador actual).
        partida.Actual().GanarMonedas(total);
    }
}

// --- Acción DESCARTAR: NO tira dados. El jugador tira una carta de su mano
// y pierde el turno. El descarte en sí lo maneja el GameManager, porque
// necesita la mano del jugador y la pila de descarte (que no están en Partida).
public class AccionDescartar : Accion
{
    public override string Nombre => "Descartar";
    public override int CantidadDados => 0;   // no tira dados

    public override void Aplicar(Partida partida, int total)
    {
        // No hace nada acá: el descarte se resuelve en el GameManager
        // (necesita la carta elegida y la PilaDescarte).
    }
}