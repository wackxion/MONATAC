// ============================================================
//  FabricaDeCartas.cs  —  Capa: RULES  —  patrón FACTORY
//  Arma el mazo a partir de la TABLA de datos (CatalogoCartas).
//  Ya NO hardcodea las cartas: recorre las fichas (CartaDefinicion)
//  y construye la Carta concreta de cada una.
//
//  EL FLUJO EN 3 PARTES (cómo se conectan los archivos):
//    1) CartaDefinicion.cs  → el MOLDE de una ficha (qué datos describe una carta).
//    2) CatalogoCartas.cs   → la TABLA: la lista de fichas (las 54 cartas como datos).
//    3) FabricaDeCartas.cs  → ESTE: LEE la tabla y CONSTRUYE las Carta reales.
//
//  Para AGREGAR una carta de un tipo existente: se agrega una ficha
//  en CatalogoCartas (o una línea en cartas.json). Solo se toca este
//  archivo si se inventa un TIPO nuevo (una clase Carta nueva + un case).
//
//  DE DÓNDE SALEN LAS FICHAS: a la fábrica le da igual. Recibe una lista
//  de fichas y construye. Hoy pueden venir del catálogo en código
//  (CatalogoCartas) o de un archivo de texto (cartas.json, vía CargadorCartas).
//  Esta clase sigue siendo C# PURO: no sabe de archivos ni de Unity.
// ============================================================

using System.Collections.Generic;

public static class FabricaDeCartas
{
    // Versión sin parámetros: usa el catálogo en código (respaldo y tests).
    // Mantiene la firma de siempre para no tocar a quien ya la llamaba.
    public static Mazo CrearMazo()
    {
        return CrearMazo(CatalogoCartas.Todas());
    }

    // Versión data-driven: arma el mazo a partir de las fichas que le pasen
    // (del catálogo o del JSON). La lógica de construcción es la misma.
    public static Mazo CrearMazo(List<CartaDefinicion> fichas)
    {
        // Acá se van juntando todas las cartas ya construidas.
        List<Carta> cartas = new List<Carta>();

        // PASO 1: recorrer la TABLA de datos (cada 'def' es una ficha: nombre, copias, clase, valores).
        foreach (CartaDefinicion def in fichas)
        {
            // PASO 2: repetir según 'copias' (si la ficha dice 4, van 4 cartas al mazo).
            for (int i = 0; i < def.copias; i++)
            {
                // PASO 3: traducir la ficha (dato) en una Carta real y sumarla.
                //         Es un objeto NUEVO por cada copia (importante para las de un solo uso).
                cartas.Add(Construir(def));
            }
        }

        // PASO 4: devolver el mazo ya armado.
        return new Mazo(cartas);
    }

    // PASO 3 en detalle: traduce UNA ficha de datos a la Carta concreta.
    // Mira la 'clase' de la ficha y elige qué constructor llamar (con sus valores).
    private static Carta Construir(CartaDefinicion d)
    {
        switch (d.clase)
        {
            // Usan: acción + valor (bonus). Vencimiento además usa 'usos'.
            case ClaseCarta.Pasiva:              return new CartaPasiva(d.nombre, d.accion, d.valor);
            case ClaseCarta.UnUso:               return new CartaUnUso(d.nombre, d.accion, d.valor);
            case ClaseCarta.Vencimiento:         return new CartaVencimiento(d.nombre, d.accion, d.valor, d.usos);

            // Usan solo 'valor' (el número principal): daño / monedas / curación / dados extra.
            case ClaseCarta.Reflectante:         return new CartaReflectante(d.nombre, d.valor);
            case ClaseCarta.ComodinDado:         return new CartaComodinDado(d.nombre, d.valor);
            case ClaseCarta.BolsilloRoto:        return new CartaBolsilloRoto(d.nombre, d.valor);
            case ClaseCarta.VampirismoDefensivo: return new CartaVampirismoDefensivo(d.nombre, d.valor);

            // Usa solo el nombre (no lleva valores).
            case ClaseCarta.Reaccion:            return new CartaReaccion(d.nombre);

            // Caso especial: el comodín multiplicador puede ir con acción fija o para cualquiera (null).
            case ClaseCarta.ComodinMultiplicador:
                return d.conAccion
                    ? new CartaComodinMultiplicador(d.nombre, d.valor, d.accion)   // 'valor' = factor
                    : new CartaComodinMultiplicador(d.nombre, d.valor);

            // Grupales: no llevan datos (cada una trae su propio nombre y efecto fijo).
            case ClaseCarta.LeyMarcial:          return new CartaLeyMarcial();
            case ClaseCarta.Colecta:             return new CartaColecta();
            case ClaseCarta.DescarteGrupal:      return new CartaDescarteGrupal();
            case ClaseCarta.Exceso:              return new CartaExceso();
            case ClaseCarta.Penitencia:          return new CartaPenitencia();

            default:                             return null;   // no debería pasar
        }
    }
}
