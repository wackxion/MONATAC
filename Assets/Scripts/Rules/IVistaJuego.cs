// ============================================================
//  IVistaJuego.cs  —  Capa: RULES (reglas)
//  INTERFACE (contrato) que describe lo que la capa de reglas
//  necesita de una "vista", SIN saber que la vista es Unity.
//
//  Esto es la base del principio DIP: el Presentador va a depender
//  de esta abstracción, NO del GameManager concreto. Así las reglas
//  no quedan atadas al motor gráfico.
// ============================================================

public interface IVistaJuego
{
    // EVENTO: la vista AVISA que el usuario pidió cambiar de objetivo.
    // El Presentador se suscribe acá; la vista no sabe quién la escucha.
    event System.Action AlPedirCambiarObjetivo;

    // La vista se compromete a saber hacer estas 2 cosas cuando el
    // Presentador se lo pida:
    void RefrescarPantalla();          // volver a dibujar barras, textos, etc.
    void MostrarMensaje(string texto); // mostrar un aviso en pantalla
}