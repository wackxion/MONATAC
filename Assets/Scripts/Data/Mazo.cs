// ============================================================
//  Mazo.cs  —  Capa: DATA / RULES
//  El mazo de cartas y la pila de descarte.
//  El mazo se puede robar, mezclar y reciclar (mazo circular).
// ============================================================

using System.Collections.Generic;

public class Mazo
{
    // Generador de azar de C# puro (System.Random), NO el de Unity.
    // Mantiene al Mazo independiente del motor gráfico.
    private static readonly System.Random azar = new System.Random();

    private List<Carta> cartas;

    // Recibe la lista de cartas ya armada (por la Fábrica).
    public Mazo(List<Carta> cartas)
    {
        this.cartas = cartas;
    }

    public bool EstaVacio() { return cartas.Count == 0; }
    public int Cantidad()   { return cartas.Count; }

    // Roba la carta de arriba: la saca del mazo y la devuelve.
    public Carta Robar()
    {
        if (EstaVacio()) return null;
        Carta c = cartas[0];
        cartas.RemoveAt(0);
        return c;
    }

    // Mezcla el mazo al azar (algoritmo de Fisher-Yates).
    public void Mezclar()
    {
        for (int i = cartas.Count - 1; i > 0; i--)
        {
            int j = azar.Next(0, i + 1);
            Carta tmp = cartas[i];
            cartas[i] = cartas[j];
            cartas[j] = tmp;
        }
    }

    // Mazo circular: cuando se agota, toma el descarte, lo mezcla y lo vuelve mazo.
    public void Reciclar(PilaDescarte descarte)
    {
        cartas.AddRange(descarte.TomarTodas());
        Mezclar();
    }
}

// La pila de descarte: donde van a parar las cartas usadas.
public class PilaDescarte
{
    private List<Carta> cartas = new List<Carta>();

    public void Agregar(Carta c) { cartas.Add(c); }

    // Devuelve todas las cartas y deja la pila vacía.
    public List<Carta> TomarTodas()
    {
        List<Carta> copia = new List<Carta>(cartas);
        cartas.Clear();
        return copia;
    }
}
