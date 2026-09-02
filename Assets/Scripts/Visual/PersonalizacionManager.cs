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
    //hecho/modificado por Julian
    // Textos que muestran los valores seleccionados
    public TextMeshProUGUI textoHP;
    public TextMeshProUGUI textoRondas;
    
    //hecho/modificado por Julian
    // Botones de la interfaz (se buscan automáticamente)
    private BotonValor[] botonesHP;
    private BotonValor[] botonesRondas;
    
    //hecho/modificado por Julian
    // Valores actuales seleccionados
    private int hpSeleccionado = 40;
    private int rondasSeleccionadas = 0;
    
    //hecho/modificado por Julian
    // Límites para las opciones
    private int hpMinimo = 20;
    private int hpMaximo = 200;
    private int rondasMinimas = 0;
    private int rondasMaximas = 30;

    void Start()
    {
        //hecho/modificado por Julian
        // Buscar todos los botones de valor en la escena
        BotonValor[] todos = FindObjectsOfType<BotonValor>();
        System.Collections.Generic.List<BotonValor> listaHP = new System.Collections.Generic.List<BotonValor>();
        System.Collections.Generic.List<BotonValor> listaRondas = new System.Collections.Generic.List<BotonValor>();
        foreach (BotonValor b in todos)
        {
            if (b.tipo == BotonValor.TipoValor.HP) listaHP.Add(b);
            else listaRondas.Add(b);
        }
        botonesHP = listaHP.ToArray();
        botonesRondas = listaRondas.ToArray();
        
        //hecho/modificado por Julian
        // Cargar valores actuales de Config
        hpSeleccionado = Config.hpMaximo;
        rondasSeleccionadas = Config.cantidadRondas;
        
        ActualizarUI();
    }

    //hecho/modificado por Julian
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

    //hecho/modificado por Julian
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

    //hecho/modificado por Julian
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

    //hecho/modificado por Julian
    // Actualizar textos de la interfaz y botones
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

        //hecho/modificado por Julian
        // Actualizar color de los botones según selección
        foreach (BotonValor b in botonesHP)
            b.SetSeleccionado(b.valor == hpSeleccionado);
        foreach (BotonValor b in botonesRondas)
            b.SetSeleccionado(b.valor == rondasSeleccionadas);
    }

    //hecho/modificado por Julian
    // Guardar configuración y volver al menú
    public void GuardarYVolver()
    {
        Config.hpMaximo = hpSeleccionado;
        Config.cantidadRondas = rondasSeleccionadas;
        SceneManager.LoadScene("MENU");
    }

    //hecho/modificado por Julian
    // Volver al menú sin guardar
    public void VolverAlMenu()
    {
        SceneManager.LoadScene("MENU");
    }
}