// ============================================================
//  PersonalizacionManager.cs  —  Capa: VISUAL (Unity)
//  Controla la escena de Personalización: permite al jugador
//  personalizar HP máximo y cantidad de rondas antes de comenzar.
// ============================================================

using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PersonalizacionManager : MonoBehaviour
{
    //hecho por pilar
    // Textos que muestran los valores seleccionados
    public TextMeshProUGUI textoHP;
    public TextMeshProUGUI textoRondas;
    
    //hecho por pilar
    // Valores actuales seleccionados
    private int hpSeleccionado = 40;
    private int rondasSeleccionadas = 0;
    
    //hecho por pilar
    // Límites para las opciones
    private int hpMinimo = 20;
    private int hpMaximo = 100;
    private int rondasMinimas = 0;
    private int rondasMaximas = 20;

    void Start()
    {
        //hecho por pilar
        // Cargar valores actuales de Config
        hpSeleccionado = Config.hpMaximo;
        rondasSeleccionadas = Config.cantidadRondas;
        
        ActualizarUI();
    }

    //hecho por pilar
    // Métodos para HP máximo
    public void AumentarHP()
    {
        hpSeleccionado += 10;
        if (hpSeleccionado > hpMaximo) hpSeleccionado = hpMaximo;
        ActualizarUI();
    }

    public void DisminuirHP()
    {
        hpSeleccionado -= 10;
        if (hpSeleccionado < hpMinimo) hpSeleccionado = hpMinimo;
        ActualizarUI();
    }

    //hecho por pilar
    // Métodos para cantidad de rondas
    public void AumentarRondas()
    {
        rondasSeleccionadas += 1;
        if (rondasSeleccionadas > rondasMaximas) rondasSeleccionadas = rondasMaximas;
        ActualizarUI();
    }

    public void DisminuirRondas()
    {
        rondasSeleccionadas -= 1;
        if (rondasSeleccionadas < rondasMinimas) rondasSeleccionadas = rondasMinimas;
        ActualizarUI();
    }

    //hecho por pilar
    // Métodos para establecer valores directamente (para cuadraditos clickeables)
    public void SetHP(int valor)
    {
        hpSeleccionado = Mathf.Clamp(valor, hpMinimo, hpMaximo);
        ActualizarUI();
    }

    public void SetRondas(int valor)
    {
        rondasSeleccionadas = Mathf.Clamp(valor, rondasMinimas, rondasMaximas);
        ActualizarUI();
    }

    //hecho por pilar
    // Actualizar textos de la interfaz
    private void ActualizarUI()
    {
        if (textoHP != null)
            textoHP.text = hpSeleccionado.ToString();
        
        if (textoRondas != null)
        {
            if (rondasSeleccionadas == 0)
                textoRondas.text = "Sin límite";
            else
                textoRondas.text = rondasSeleccionadas.ToString();
        }
    }

    //hecho por pilar
    // Guardar configuración y volver al menú
    public void GuardarYVolver()
    {
        Config.hpMaximo = hpSeleccionado;
        Config.cantidadRondas = rondasSeleccionadas;
        SceneManager.LoadScene("MENU");
    }

    //hecho por pilar
    // Volver al menú sin guardar
    public void VolverAlMenu()
    {
        SceneManager.LoadScene("MENU");
    }
}