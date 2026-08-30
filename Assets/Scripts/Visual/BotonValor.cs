// ============================================================
//  BotonValor.cs  —  Capa: VISUAL (Unity)
//  Botón que representa un valor específico (HP o rondas).
//  Al hacer clic, establece ese valor en PersonalizacionManager.
// ============================================================

using UnityEngine;
using UnityEngine.UI;

public class BotonValor : MonoBehaviour
{
    //hecho por pilar
    // Valor que representa este botón
    public int valor;
    
    //hecho por pilar
    // Tipo de valor (HP o Rondas)
    public TipoValor tipo;
    
    //hecho por pilar
    // Referencia al PersonalizacionManager
    private PersonalizacionManager manager;

    public enum TipoValor
    {
        HP,
        Rondas
    }

    void Start()
    {
        //hecho por pilar
        // Buscar el PersonalizacionManager en la escena
        manager = FindObjectOfType<PersonalizacionManager>();
        
        //hecho por pilar
        // Configurar el botón
        Button boton = GetComponent<Button>();
        if (boton != null)
        {
            boton.onClick.AddListener(OnClick);
        }
    }

    //hecho por pilar
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
}