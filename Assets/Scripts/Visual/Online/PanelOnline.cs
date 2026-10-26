// ============================================================
//  PanelOnline.cs  —  Capa: VISUAL (UI del menú)
//  Pegamento entre los botones del panel "Online" del menú y la
//  SesionRed (la lógica de red persistente). Solo lee el input de IP
//  y dispara Crear / Unirse.
// ============================================================

using UnityEngine;
using TMPro;

public class PanelOnline : MonoBehaviour
{
    public TMP_InputField inputIP;   // IP del host para "Unirse" (vacío = 127.0.0.1)

    // Botón "Crear partida": arranco como Host.
    public void OnCrear()
    {
        if (SesionRed.Instancia != null) SesionRed.Instancia.Crear();
    }

    // Botón "Unirse a partida": me conecto a la IP escrita.
    public void OnUnirse()
    {
        if (SesionRed.Instancia != null)
            SesionRed.Instancia.Unirse(inputIP != null ? inputIP.text : "");
    }
}
