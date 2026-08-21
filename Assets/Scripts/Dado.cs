// Dado.cs — Un dado de 4 caras (d4)

public class Dado
{
    private int caras;

    // Constructor: por defecto tiene 4 caras
    public Dado(int caras = 4)
    {
        this.caras = caras;
    }

    // Tira el dado: devuelve un número al azar del 1 al 4
    public int Tirar()
    {
        return UnityEngine.Random.Range(1, caras + 1);
    }
}
