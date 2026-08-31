// ============================================================
//  PresentadorJuego.cs  —  Capa: RULES (reglas)
//  El PRESENTER del patrón MVP: coordina entre el Modelo (Partida)
//  y la Vista (lo que muestra la pantalla).
//
//  CLAVE (DIP): NO conoce al GameManager. Solo conoce la abstracción
//  'IVistaJuego'. Así las reglas no dependen de Unity.
// ============================================================

public class PresentadorJuego
{
    private readonly IVistaJuego vista;   // la Vista, vista como ABSTRACCIÓN (DIP)
    private readonly Partida partida;     // el Modelo (datos + reglas)

    // Recibe la vista y la partida "desde afuera" (inyección de dependencias).
    public PresentadorJuego(IVistaJuego vista, Partida partida)
    {
        this.vista = vista;
        this.partida = partida;

        // Se SUSCRIBE al evento de la vista (a través de la interface).
        // Cuando la vista dispare ese evento, se ejecutará CambiarObjetivo().
        vista.AlPedirCambiarObjetivo += CambiarObjetivo;
    }

    // El Presentador COORDINA: le pide al Modelo y le avisa a la Vista.
    private void CambiarObjetivo()
    {
        if (partida.Terminada) return;
        partida.CambiarObjetivo();                                      // 1) Modelo hace el trabajo
        vista.RefrescarPantalla();                                       // 2) Vista se actualiza
        vista.MostrarMensaje("Objetivo: " + partida.Objetivo().nombre);  // 3) Vista muestra aviso
    }
}