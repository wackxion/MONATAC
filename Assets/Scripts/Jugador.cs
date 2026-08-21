// Jugador.cs — Un jugador de MONATAC

public class Jugador
{
    public string nombre;
    public int hp;
    public int hpMaximo;
    public int monedas;

    // Constructor: se crea con un nombre y su HP inicial
    public Jugador(string nombre, int hpInicial)
    {
        this.nombre = nombre;
        this.hp = hpInicial;
        this.hpMaximo = hpInicial;
        this.monedas = 0;
    }

    // ¿Sigue vivo?
    public bool EstaVivo()
    {
        return hp > 0;
    }

    // Recibe daño (el HP no baja de 0)
    public void RecibirDanio(int cantidad)
    {
        hp -= cantidad;
        if (hp < 0) hp = 0;
    }

    // Se cura (sin pasar el HP máximo)
    public void Curar(int cantidad)
    {
        hp += cantidad;
        if (hp > hpMaximo) hp = hpMaximo;
    }

    // Suma monedas
    public void GanarMonedas(int cantidad)
    {
        monedas += cantidad;
    }

    // Intenta gastar monedas: devuelve true si le alcanzó
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
