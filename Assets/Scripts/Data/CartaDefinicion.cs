// ============================================================
//  CartaDefinicion.cs  —  Capa: DATA (datos)
//  PARTE 1 de 3 del flujo (ver [[FabricaDeCartas]]):
//    1) ESTE = el molde · 2) CatalogoCartas = la tabla · 3) FabricaDeCartas = construye.
//
//  "Ficha" de datos que describe UNA carta del catálogo:
//  su nombre, cuántas copias van al mazo, qué clase es y sus
//  valores. Es C# PURO (no depende de Unity) → mantiene /Data
//  independiente del motor (no se usa ScriptableObject a propósito).
//
//  La FabricaDeCartas lee estas fichas y construye las Carta reales.
//  Agregar una carta de un tipo existente = agregar UNA ficha al
//  CatalogoCartas (sin tocar la lógica de construcción).
// ============================================================

// Qué clase concreta de carta construir (más fino que TipoCarta,
// porque "Grupal" abarca varias y hay comodines/especiales).
public enum ClaseCarta
{
    Pasiva,
    UnUso,
    Vencimiento,
    Reflectante,
    Reaccion,
    ComodinMultiplicador,
    ComodinDado,
    BolsilloRoto,
    VampirismoDefensivo,
    LeyMarcial,
    Colecta,
    DescarteGrupal,
    Exceso,
    Penitencia
}

public class CartaDefinicion
{
    public string nombre;      // nombre de la carta
    public int copias;         // cuántas van al mazo
    public ClaseCarta clase;   // qué tipo de carta construir
    public TipoAccion accion;  // acción asociada (Pasiva/UnUso/Vencimiento/ComodinMultiplicador)
    public int valor;          // número principal: bonus / daño / monedas / curación / factor / dados extra
    public int usos;           // solo Vencimiento (cuántos turnos dura)
    public bool conAccion;     // ComodinMultiplicador: si usa 'accion' (si no, aplica a cualquier acción)

    // Constructor con valores por defecto: cada carta pone solo lo que necesita.
    public CartaDefinicion(string nombre, int copias, ClaseCarta clase,
                           TipoAccion accion = TipoAccion.Atacar, int valor = 0,
                           int usos = 0, bool conAccion = true)
    {
        this.nombre = nombre;
        this.copias = copias;
        this.clase = clase;
        this.accion = accion;
        this.valor = valor;
        this.usos = usos;
        this.conAccion = conAccion;
    }
}
