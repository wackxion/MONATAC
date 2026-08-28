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

    // Dados extra que agrega la carta a la tirada (0 = ninguno).
    public virtual int DadosExtra(TipoAccion accion) { return 0; }

    // ¿La carta se va al descarte después de usarse? Por defecto no.
    public virtual bool DebeDescartarse() { return false; }

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

    public override bool DebeDescartarse() { return usada; }
}

// --- 3) VENCIMIENTO: da bonus durante varios turnos, con mantenimiento ---
public class CartaVencimiento : Carta
{
    private TipoAccion accionAsociada;
    private int bonus;
    public int usosRestantes;
    public int costoPorUso = 2;      // del 2do uso en adelante cuesta 2 monedas
    private bool yaSeUso = false;    // el PRIMER uso es gratis

    public CartaVencimiento(string nombre, TipoAccion accion, int bonus, int usos)
        : base(nombre, TipoCarta.Vencimiento)
    {
        this.accionAsociada = accion;
        this.bonus = bonus;
        this.usosRestantes = usos;
    }

    // ¿Sirve para esta acción y todavía le quedan usos?
    public bool SirvePara(TipoAccion accion)
    {
        return accion == accionAsociada && usosRestantes > 0;
    }

    // Costo del PRÓXIMO uso: el primero es gratis (0); los siguientes cuestan 2.
    public int CostoDelProximoUso()
    {
        return yaSeUso ? costoPorUso : 0;
    }

    // Usa la carta: aplica el bonus, marca que ya se usó y gasta un uso.
    public int Usar()
    {
        yaSeUso = true;
        usosRestantes--;
        return bonus;
    }

    // ¿Se quedó sin usos?
    public bool SinUsos() { return usosRestantes <= 0; }

    public override string Descripcion()
    {
        return nombre + " (+" + bonus + " al " + accionAsociada + ", " + usosRestantes + " usos)";
    }
}

// --- 4) REFLECTANTE: reacciona cuando te atacan (fuera de tu turno) ---
public class CartaReflectante : Carta
{
    private int danioReflejado;
    public bool usada = false;   // se descarta después de reflejar una vez

    public CartaReflectante(string nombre, int danioReflejado)
        : base(nombre, TipoCarta.Reflectante)
    {
        this.danioReflejado = danioReflejado;
    }

    // Devuelve cuánto daño le rebota al atacante y se marca como usada.
    public int Reflejar()
    {
        usada = true;
        return danioReflejado;
    }

    public override bool DebeDescartarse() { return usada; }
}

// --- 5) REACCIÓN: absorbe daño gastando monedas (Escudo de Monedas) ---
public class CartaReaccion : Carta
{
    public bool usada = false;   // se descarta después de absorber una vez

    public CartaReaccion(string nombre) : base(nombre, TipoCarta.Reaccion) { }

    // Gasta monedas del jugador para absorber daño (2 monedas = 1 HP).
    // Devuelve el daño que QUEDA después de absorber.
    public int Absorber(int danio, Jugador jugador)
    {
        int inicial = danio;
        // Mientras haya daño y le alcancen 2 monedas, absorbe 1 de HP.
        while (danio > 0 && jugador.GastarMonedas(2))
        {
            danio -= 1;
        }
        if (danio < inicial) usada = true;   // si absorbió algo, queda usada
        return danio;
    }

    public override bool DebeDescartarse() { return usada; }
}

// --- 6) GRUPAL: afecta a todos los jugadores al activarse ---
public abstract class CartaGrupal : Carta
{
    public CartaGrupal(string nombre) : base(nombre, TipoCarta.Grupal) { }

    // Cada carta grupal APLICA su propio efecto a todos y devuelve un mensaje
    // para mostrar. Es el patrón polimórfico: mismo método, distinta respuesta.
    public abstract string AplicarATodos(ContextoGrupal ctx);
}

// --- COLECTA: junta todas las monedas y las reparte parejo ---
public class CartaColecta : CartaGrupal
{
    public CartaColecta() : base("Colecta") { }

    public override string AplicarATodos(ContextoGrupal ctx)
    {
        int suma = 0;
        foreach (Jugador j in ctx.jugadores) suma += j.monedas;
        int cada = suma / ctx.jugadores.Count;
        int resto = suma % ctx.jugadores.Count;
        foreach (Jugador j in ctx.jugadores) j.EstablecerMonedas(cada);
        ctx.comprador.GanarMonedas(resto);
        return "Colecta: se repartieron " + suma + " monedas (" + cada + " a cada uno, resto " + resto + " para " + ctx.comprador.nombre + ").";
    }
}

// --- EXCESO: cada jugador recibe 1 carta gratis ---
public class CartaExceso : CartaGrupal
{
    public CartaExceso() : base("Exceso") { }

    public override string AplicarATodos(ContextoGrupal ctx)
    {
        foreach (Jugador j in ctx.jugadores)
        {
            if (j.mano.Count >= 5) continue;
            if (ctx.mazo.EstaVacio()) ctx.mazo.Reciclar(ctx.descarte);
            Carta nueva = ctx.mazo.Robar();
            if (nueva == null) continue;
            if (nueva is CartaGrupal) ctx.descarte.Agregar(nueva);   // no encadenar grupales
            else j.mano.Add(nueva);
        }
        return "Exceso: cada jugador recibió una carta gratis.";
    }
}

// --- DESCARTE GRUPAL: cada jugador descarta 1 carta ---
public class CartaDescarteGrupal : CartaGrupal
{
    public CartaDescarteGrupal() : base("Descarte Grupal") { }

    public override string AplicarATodos(ContextoGrupal ctx)
    {
        foreach (Jugador j in ctx.jugadores)
            if (j.mano.Count > 0) { ctx.descarte.Agregar(j.mano[0]); j.mano.RemoveAt(0); }
        return "Descarte Grupal: cada jugador descartó una carta.";
    }
}

// --- PENITENCIA: destruye todas las cartas de vencimiento ---
public class CartaPenitencia : CartaGrupal
{
    public CartaPenitencia() : base("Penitencia") { }

    public override string AplicarATodos(ContextoGrupal ctx)
    {
        foreach (Jugador j in ctx.jugadores)
            for (int i = j.mano.Count - 1; i >= 0; i--)
                if (j.mano[i] is CartaVencimiento) { ctx.descarte.Agregar(j.mano[i]); j.mano.RemoveAt(i); }
        return "Penitencia: se destruyeron todas las cartas de vencimiento.";
    }
}

// --- LEY MARCIAL: el próximo round todos deben Atacar ---
public class CartaLeyMarcial : CartaGrupal
{
    public CartaLeyMarcial() : base("Ley Marcial") { }

    public override string AplicarATodos(ContextoGrupal ctx)
    {
        ctx.partida.ActivarLeyMarcial();
        return "Ley Marcial: este round todos están obligados a Atacar.";
    }
}

// --- COMODÍN ×2: multiplica el resultado de los dados. Un solo uso. ---
public class CartaComodinMultiplicador : Carta
{
    private int factor;
    private TipoAccion? accionAsociada;   // null = sirve para cualquier acción
    public bool usada = false;

    public CartaComodinMultiplicador(string nombre, int factor, TipoAccion? accion = null)
        : base(nombre, TipoCarta.UnUso)   // se comporta como una carta de un solo uso
    {
        this.factor = factor;
        this.accionAsociada = accion;
    }

    // Multiplica una vez. Si tiene acción asociada, solo multiplica esa acción.
    public override int Multiplicador(TipoAccion accion)
    {
        if (usada) return 1;
        if (accionAsociada != null && accion != accionAsociada) return 1;
        usada = true;
        return factor;
    }

    public override bool DebeDescartarse() { return usada; }
    public override string Descripcion() { return nombre + " (x" + factor + ")"; }
}

// --- COMODÍN +1d4: agrega dados extra a la tirada. Un solo uso. ---
public class CartaComodinDado : Carta
{
    private int dadosExtra;
    public bool usada = false;

    public CartaComodinDado(string nombre, int dadosExtra)
        : base(nombre, TipoCarta.UnUso)
    {
        this.dadosExtra = dadosExtra;
    }

    // Al usarse agrega los dados extra una vez.
    public override int DadosExtra(TipoAccion accion)
    {
        if (!usada) { usada = true; return dadosExtra; }
        return 0;
    }

    public override bool DebeDescartarse() { return usada; }
    public override string Descripcion() { return nombre + " (+" + dadosExtra + "d4)"; }
}

// --- BOLSILLO ROTO: reflectante que le roba monedas al atacante (un solo uso) ---
public class CartaBolsilloRoto : Carta
{
    public int monedasARobar;
    public bool usada = false;

    public CartaBolsilloRoto(string nombre, int monedas)
        : base(nombre, TipoCarta.Reflectante)
    {
        this.monedasARobar = monedas;
    }

    public override bool DebeDescartarse() { return usada; }
}

// --- VAMPIRISMO DEFENSIVO: reflectante que cura al defensor al ser atacado (un solo uso) ---
public class CartaVampirismoDefensivo : Carta
{
    public int curacion;
    public bool usada = false;

    public CartaVampirismoDefensivo(string nombre, int curacion)
        : base(nombre, TipoCarta.Reflectante)
    {
        this.curacion = curacion;
    }

    public override bool DebeDescartarse() { return usada; }
}
