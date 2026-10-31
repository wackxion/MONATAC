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
    public TMP_Text textoEstado;     // (opcional) avisos de red: ej "no se pudo conectar"

    // Me suscribo a los avisos de red mientras el panel está abierto.
    void OnEnable()
    {
        if (SesionRed.Instancia != null) SesionRed.Instancia.OnMensajeRed += MostrarMensaje;
    }

    void OnDisable()
    {
        if (SesionRed.Instancia != null) SesionRed.Instancia.OnMensajeRed -= MostrarMensaje;
    }

    private void MostrarMensaje(string msg)
    {
        Debug.Log("[PanelOnline] " + msg);
        if (textoEstado != null) textoEstado.text = msg;
    }

    // Botón "Crear partida": arranco como Host.
    public void OnCrear()
    {
        if (textoEstado != null) textoEstado.text = "";   // limpio avisos previos
        if (SesionRed.Instancia != null) SesionRed.Instancia.Crear();
    }

    // Botón "Unirse a partida": me conecto a la IP escrita.
    public void OnUnirse()
    {
        if (textoEstado != null) textoEstado.text = "Conectando...";
        if (SesionRed.Instancia != null)
            SesionRed.Instancia.Unirse(inputIP != null ? inputIP.text : "");
    }
}
