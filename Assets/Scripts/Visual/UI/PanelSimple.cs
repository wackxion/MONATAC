// ============================================================
//  PanelSimple.cs  —  Capa: VISUAL (Unity)
//  Abre/cierra un panel de UI (instrucciones, créditos, etc.).
//  Es puro mostrar/ocultar: no toca reglas ni estado del juego.
//  Sirve para no pelear con el checkbox de SetActive en el botón:
//  el botón "Abrir" llama Abrir() y el botón "X" llama Cerrar().
// ============================================================

using UnityEngine;

public class PanelSimple : MonoBehaviour
{
    // El panel que se muestra/oculta. Arranca desactivado en la escena.
    public GameObject panel;

    // Botón que abre el panel → conectar a este método.
    public void Abrir()
    {
        if (panel != null) panel.SetActive(true);
    }

    // Botón X que cierra el panel → conectar a este método.
    public void Cerrar()
    {
        if (panel != null) panel.SetActive(false);
    }
}
