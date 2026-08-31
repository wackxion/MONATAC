// ============================================================
//  Dado.cs  —  Capa: RULES (reglas / lógica)
//  Representa un dado de 4 caras (d4). Es "lógica pura":
//  no sabe nada de la interfaz de Unity, solo genera azar.
//  Al no depender de la UI, se podría probar por separado.
// ============================================================

public class Dado
{
    // Generador de azar de C# puro (System.Random), NO el de Unity.
    // Así esta clase no depende del motor gráfico y podría probarse sola.
    // Es 'static' para compartir un único generador entre todos los dados
    // (evita que dos dados creados en el mismo instante repitan resultados).
    private static readonly System.Random azar = new System.Random();

    // 'private' = solo esta clase puede tocar este dato (encapsulamiento).
    // Guarda cuántas caras tiene el dado.
    private int caras;

    // Constructor: es el "molde" que se ejecuta al crear el dado con "new Dado()".
    // El "= 4" hace que, si no se aclara, tenga 4 caras por defecto.
    public Dado(int caras = 4)
    {
        this.caras = caras;   // 'this.caras' es el atributo; 'caras' es el parámetro
    }

    // Tira el dado y devuelve un número al azar.
    public int Tirar()
    {
        // azar.Next(min, max) incluye el min pero EXCLUYE el max (igual que antes).
        // Por eso usamos (1, caras + 1) para que salga del 1 al 4 (no del 1 al 3).
        return azar.Next(1, caras + 1);
    }
}
