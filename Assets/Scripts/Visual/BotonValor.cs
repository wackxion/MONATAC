// ============================================================
//  BotonValor.cs  —  Capa: VISUAL (Unity)
//  Botón que representa un valor específico (HP o rondas).
//  Al hacer clic, establece ese valor en PersonalizacionManager.
// ============================================================

using UnityEngine;
using UnityEngine.UI;

public class BotonValor : MonoBehaviour
{
    //hecho/modificado por Julian
    // Valor que representa este botón
    public int valor;
    
    //hecho/modificado por Julian
    // Tipo de valor (HP o Rondas)
    public TipoValor tipo;
    
    //hecho/modificado por Julian
    // Colores para el estado seleccionado/no seleccionado
    public Color colorNormal = Color.white;
    public Color colorSeleccionado = new Color(0.4f, 0.8f, 0.4f);
    
    //hecho/modificado por Julian
    // Referencia al PersonalizacionManager
    private PersonalizacionManager manager;

    public enum TipoValor
    {
        HP,
        Rondas
    }

    void Start()
    {
        //hecho/modificado por Julian
        // Buscar el PersonalizacionManager en la escena
        manager = FindObjectOfType<PersonalizacionManager>();
        
        //hecho/modificado por Julian
        // Configurar el botón
        Button boton = GetComponent<Button>();
        if (boton != null)
        {
            boton.onClick.AddListener(OnClick);
        }
    }

    //hecho/modificado por Julian
    // Acción al hacer clic
    private void OnClick()
    {
        if (manager == null) return;
        
        if (tipo == TipoValor.HP)
        {
            manager.SetHP(valor);
        }
        else if (tipo == TipoValor.Rondas)
        {
            manager.SetRondas(valor);
        }
    }

    //hecho/modificado por Julian
    // Marcar este botón como seleccionado o no
    public void SetSeleccionado(bool seleccionado)
    {
        Image img = GetComponent<Image>();
        if (img != null)
            img.color = seleccionado ? colorSeleccionado : colorNormal;
    }
}