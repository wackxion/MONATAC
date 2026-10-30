// ============================================================
//  LobbyUI.cs  —  Capa: VISUAL (UI de la sala de espera)
//  Pegamento de la escena Lobby con la SesionRed persistente.
//  El botón "Iniciar" SOLO lo ve/usa el host.
// ============================================================

using Unity.Netcode;
using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    public GameObject botonIniciar;   // el botón "Iniciar" (solo visible para el host)
    public SalaEspera salaEspera;     // (opcional) para iniciar CON validación de la config (B3)

    void Start()
    {
        // "Iniciar" solo aparece para el host (el que creó la sala).
        bool esHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        if (botonIniciar != null) botonIniciar.SetActive(esHost);
    }

    // Botón "Iniciar": el host lleva a TODOS a la escena del juego.
    // Pasa por SalaEspera.Iniciar() para que SIEMPRE valide la config y copie nombres/personajes (B3).
    public void OnIniciar()
    {
        SalaEspera sala = (salaEspera != null) ? salaEspera : FindObjectOfType<SalaEspera>();
        if (sala != null) { sala.Iniciar(); return; }
        // Respaldo (no debería pasar): sin SalaEspera, arranca directo.
        if (SesionRed.Instancia != null) SesionRed.Instancia.Iniciar();
    }
}
