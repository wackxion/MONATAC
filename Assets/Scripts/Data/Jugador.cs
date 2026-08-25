// ============================================================
//  Jugador.cs  —  Capa: DATA (datos)
//  Representa a un jugador y protege su estado.
//
//  ENCAPSULAMIENTO: 'hp', 'hpMaximo' y 'monedas' son PROPIEDADES
//  con "get público / set private": desde otras clases se pueden
//  LEER pero NO modificar directamente (nadie puede hacer
//  "jugador.hp = -50"). El estado solo cambia a través de los
//  métodos controlados de abajo, que respetan las reglas.
// ============================================================

using System.Collections.Generic;   // para usar List<> (la mano de cartas)

public class Jugador
{
    // --- Estado protegido (solo lectura desde afuera) ---
    public string nombre { get; private set; }
    public int hp { get; private set; }
    public int hpMaximo { get; private set; }
    public int monedas { get; private set; }

    // La mano de cartas del jugador (máximo 5 sin activar).
    public List<Carta> mano = new List<Carta>();

    public Jugador(string nombre, int hpInicial)
    {
        this.nombre = nombre;
        this.hp = hpInicial;
        this.hpMaximo = hpInicial;
        this.monedas = 0;
    }

    public bool EstaVivo() { return hp > 0; }

    // Recibe daño (el HP nunca baja de 0).
    public void RecibirDanio(int cantidad)
    {
        hp -= cantidad;
        if (hp < 0) hp = 0;
    }

    // Se cura (sin superar el máximo).
    public void Curar(int cantidad)
    {
        hp += cantidad;
        if (hp > hpMaximo) hp = hpMaximo;
    }

    // Suma monedas.
    public void GanarMonedas(int cantidad)
    {
        monedas += cantidad;
    }

    // Intenta gastar monedas: devuelve true si le alcanzó.
    public bool GastarMonedas(int cantidad)
    {
        if (monedas >= cantidad)
        {
            monedas -= cantidad;
            return true;
        }
        return false;
    }
}
