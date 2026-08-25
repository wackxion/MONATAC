// ============================================================
//  Cartas.cs  —  Capa: DATA / RULES
//  La jerarquía de cartas de MONATAC. Muestra 3 pilares de la POO:
//    - ABSTRACCIÓN: 'Carta' es un molde que no se crea solo.
//    - HERENCIA: los 6 tipos heredan de 'Carta'.
//    - POLIMORFISMO: todas responden a los mismos métodos,
//      pero cada una a su manera (con 'override').
// ============================================================

using System.Collections.Generic;

// --- Clase MADRE abstracta: lo común a TODAS las cartas ---
public abstract class Carta
{
    public string nombre;    // nombre de la carta
    public TipoCarta tipo;   // a qué tipo pertenece

    // Constructor que usan todas las hijas (con "base(...)").
    public Carta(string nombre, TipoCarta tipo)
    {
        this.nombre = nombre;
        this.tipo = tipo;
    }

    // Bonus fijo que la carta suma a una acción (0 = no aporta nada).
    // 'virtual' = las hijas PUEDEN redefinirlo con 'override'.
    public virtual int BonusPara(TipoAccion accion) { return 0; }

    // Multiplicador sobre los dados (1 = no cambia nada; 2 = x2; etc.).
    public virtual int Multiplicador(TipoAccion accion) { return 1; }

    // Texto para mostrar en pantalla.
    public virtual string Descripcion() { return nombre; }
}

// --- 1) PASIVA: bonus fijo cada vez que elegís su acción ---
public class CartaPasiva : Carta
{
    private TipoAccion accionAsociada;
    private int bonus;

    public CartaPasiva(string nombre, TipoAccion accion, int bonus)
        : base(nombre, TipoCarta.Pasiva)   // 'base' llama al constructor de Carta
    {
        this.accionAsociada = accion;
        this.bonus = bonus;
    }

    // Solo aporta si la acción coincide (ej: Filo Eterno solo al Atacar).
    public override int BonusPara(TipoAccion accion)
    {
        return (accion == accionAsociada) ? bonus : 0;
    }

    public override string Descripcion()
    {
        return nombre + " (+" + bonus + " al " + accionAsociada + ")";
    }
}

// --- 2) UN USO: da su bonus una sola vez y después se descarta ---
public class CartaUnUso : Carta
{
    private TipoAccion accionAsociada;
    private int bonus;
    public bool usada = false;   // cuando es true, el mazo/descarte la saca

    public CartaUnUso(string nombre, TipoAccion accion, int bonus)
        : base(nombre, TipoCarta.UnUso)
    {
        this.accionAsociada = accion;
        this.bonus = bonus;
    }

    public override int BonusPara(TipoAccion accion)
    {
        if (accion == accionAsociada && !usada)
        {
            usada = true;   // se marca como usada
            return bonus;
        }
        return 0;
    }
}

// --- 3) VENCIMIENTO: da bonus durante varios turnos, con mantenimiento ---
public class CartaVencimiento : Carta
{
    private TipoAccion accionAsociada;
    private int bonus;
    public int turnosRestantes;
    public int costoMantenimiento = 2;   // cuesta 2 monedas por turno seguir activa

    public CartaVencimiento(string nombre, TipoAccion accion, int bonus, int turnos)
        : base(nombre, TipoCarta.Vencimiento)
    {
        this.accionAsociada = accion;
        this.bonus = bonus;
        this.turnosRestantes = turnos;
    }

    public override int BonusPara(TipoAccion accion)
    {
        return (accion == accionAsociada && turnosRestantes > 0) ? bonus : 0;
    }

    // Se llama al final de cada turno: baja el contador.
    public void Decrementar() { turnosRestantes--; }

    // ¿Se le acabaron los turnos?
    public bool HaExpirado() { return turnosRestantes <= 0; }
}

// --- 4) REFLECTANTE: reacciona cuando te atacan (fuera de tu turno) ---
public class CartaReflectante : Carta
{
    private int danioReflejado;

    public CartaReflectante(string nombre, int danioReflejado)
        : base(nombre, TipoCarta.Reflectante)
    {
        this.danioReflejado = danioReflejado;
    }

    // Devuelve cuánto daño le rebota al atacante.
    public int Reflejar() { return danioReflejado; }
}

// --- 5) REACCIÓN: absorbe daño gastando monedas (Escudo de Monedas) ---
public class CartaReaccion : Carta
{
    public CartaReaccion(string nombre) : base(nombre, TipoCarta.Reaccion) { }

    // Gasta monedas del jugador para absorber daño (2 monedas = 1 HP).
    // Devuelve el daño que QUEDA después de absorber.
    public int Absorber(int danio, Jugador jugador)
    {
        // Mientras haya daño y le alcancen 2 monedas, absorbe 1 de HP.
        while (danio > 0 && jugador.GastarMonedas(2))
        {
            danio -= 1;
        }
        return danio;
    }
}

// --- 6) GRUPAL: afecta a todos los jugadores al activarse ---
public class CartaGrupal : Carta
{
    public CartaGrupal(string nombre) : base(nombre, TipoCarta.Grupal) { }

    // El efecto concreto depende de cada carta grupal (Colecta, Penitencia...).
    // Por ahora es un molde; el efecto se implementará según la carta.
    public virtual void AplicarATodos(List<Jugador> jugadores)
    {
        // (a implementar por cada carta grupal específica)
    }
}
