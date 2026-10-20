// ============================================================
//  CargadorCartas.cs  —  Capa: VISUAL (lee archivos · usa Unity)
//  Lee las cartas desde un archivo de TEXTO (Resources/cartas.json)
//  y las convierte en la MISMA lista de fichas (CartaDefinicion)
//  que hasta ahora devolvía CatalogoCartas (en código).
//
//  POR QUÉ ACÁ Y NO EN /Data:
//    Leer un archivo necesita Unity (Resources/JsonUtility). Como
//    /Data y /Rules son C# PURO (independientes del motor), la lectura
//    vive en /Visual. Las FICHAS que produce siguen siendo datos puros,
//    y la FabricaDeCartas (en /Rules) no se entera de dónde salieron.
//
//  FLUJO:  cartas.json → (este cargador) → List<CartaDefinicion> → FabricaDeCartas.CrearMazo(fichas)
//
//  Si el archivo no está o es inválido, devuelve null y el que llama
//  (GameManager) usa el catálogo en código como respaldo (nunca rompe).
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public static class CargadorCartas
{
    // --- Espejos serializables del JSON ---
    // JsonUtility arma objetos leyendo estos campos. Los enums van como
    // TEXTO (string), porque JsonUtility no sabe leer un enum por su nombre;
    // después nosotros los traducimos a ClaseCarta / TipoAccion.

    [System.Serializable]
    private class FichaJson
    {
        public string nombre;
        public int copias;
        public string clase;
        // Valores por defecto IGUALES a los de CartaDefinicion: si el JSON
        // no trae el campo, queda este default (JsonUtility respeta el valor inicial).
        public string accion = "Atacar";
        public int valor = 0;
        public int usos = 0;
        public bool conAccion = true;
    }

    // Envoltorio: JsonUtility necesita un objeto raíz (no puede leer una lista suelta).
    [System.Serializable]
    private class ArchivoCartas
    {
        public List<FichaJson> cartas;
    }

    // Carga las fichas desde Resources/cartas.json.
    // Devuelve null si no se puede (el que llama usa el catálogo en código).
    public static List<CartaDefinicion> Cargar()
    {
        // Resources.Load busca por nombre SIN extensión: Assets/Resources/cartas.json
        TextAsset archivo = Resources.Load<TextAsset>("cartas");
        if (archivo == null)
        {
            Debug.LogWarning("[CargadorCartas] No encontré Resources/cartas.json. Uso el catálogo en código.");
            return null;
        }

        // Texto del archivo → objetos (FichaJson) mediante JsonUtility.
        ArchivoCartas datos = JsonUtility.FromJson<ArchivoCartas>(archivo.text);
        if (datos == null || datos.cartas == null || datos.cartas.Count == 0)
        {
            Debug.LogWarning("[CargadorCartas] cartas.json vacío o inválido. Uso el catálogo en código.");
            return null;
        }

        // Cada FichaJson (con strings) → una CartaDefinicion (con enums reales).
        List<CartaDefinicion> fichas = new List<CartaDefinicion>();
        foreach (FichaJson f in datos.cartas)
        {
            ClaseCarta clase = ParsearEnum<ClaseCarta>(f.clase, ClaseCarta.Pasiva);
            TipoAccion accion = ParsearEnum<TipoAccion>(f.accion, TipoAccion.Atacar);
            fichas.Add(new CartaDefinicion(f.nombre, f.copias, clase, accion, f.valor, f.usos, f.conAccion));
        }

        Debug.Log("[CargadorCartas] Cartas cargadas desde JSON: " + fichas.Count + " fichas.");
        return fichas;
    }

    // Traduce un texto a un valor del enum T. Si el texto está mal escrito,
    // avisa y usa un valor por defecto (en vez de romper la partida).
    private static T ParsearEnum<T>(string texto, T porDefecto) where T : struct
    {
        if (System.Enum.TryParse(texto, out T valor)) return valor;
        Debug.LogWarning("[CargadorCartas] Valor desconocido en el JSON: '" + texto + "'. Uso " + porDefecto + ".");
        return porDefecto;
    }
}
