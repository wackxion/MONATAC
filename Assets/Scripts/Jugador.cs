// ============================================================
//  Jugador.cs  —  Capa: DATA (datos)
//  Representa a un jugador y GUARDA su estado: vida y monedas.
//  Los métodos protegen las reglas del estado (ej: el HP nunca
//  queda negativo ni supera el máximo). Eso es ENCAPSULAMIENTO:
//  el estado solo se modifica a través de métodos controlados.
// ============================================================

public class Jugador
{
    // --- Atributos: los datos que define a un jugador ---
    public string nombre;   // nombre para mostrar
    public int hp;          // vida actual
    public int hpMaximo;    // vida máxima (tope al curarse)
    public int monedas;     // monedas acumuladas

    // Constructor: se ejecuta al crear el jugador (new Jugador("Ana", 100)).
    // Recibe el nombre y el HP con el que arranca.
    public Jugador(string nombre, int hpInicial)
    {
        this.nombre = nombre;
        this.hp = hpInicial;       // arranca con la vida llena
        this.hpMaximo = hpInicial; // y ese valor queda como su tope
        this.monedas = 0;          // empieza sin monedas
    }

    // Devuelve true si el jugador todavía tiene vida (hp mayor a 0).
    public bool EstaVivo()
    {
        return hp > 0;
    }

    // Le resta daño a la vida.
    public void RecibirDanio(int cantidad)
    {
        hp -= cantidad;          // resta el daño
        if (hp < 0) hp = 0;      // REGLA: la vida nunca baja de 0
    }

    // Le suma vida al curarse.
    public void Curar(int cantidad)
    {
        hp += cantidad;                    // suma la curación
        if (hp > hpMaximo) hp = hpMaximo;  // REGLA: no supera el máximo
    }

    // Suma monedas a la reserva del jugador.
    public void GanarMonedas(int cantidad)
    {
        monedas += cantidad;
    }

    // Intenta gastar monedas. Devuelve true si le alcanzó y las gastó,
    // o false si no tenía suficientes (y no gasta nada).
    public bool GastarMonedas(int cantidad)
    {
        if (monedas >= cantidad)   // ¿tiene suficiente?
        {
            monedas -= cantidad;   // sí: las descuenta
            return true;
        }
        return false;              // no: no gasta y avisa que no pudo
    }
}
