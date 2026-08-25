// ============================================================
//  FabricaDeCartas.cs  —  Capa: RULES  —  patrón FACTORY
//  Una "fábrica" que construye el mazo a partir de la tabla de
//  cartas del reglamento. Concentrar la creación en un solo lugar
//  es la idea del patrón Factory: si cambia la composición del
//  mazo, se toca ACÁ y en ningún otro lado.
// ============================================================

using System.Collections.Generic;

public static class FabricaDeCartas
{
    // Crea y devuelve el mazo completo, ya listo para usar.
    public static Mazo CrearMazo()
    {
        List<Carta> cartas = new List<Carta>();

        // Helper local: agrega 'copias' cartas creadas por la función 'crear'.
        // Usamos una función para que cada copia sea un objeto NUEVO
        // (importante en las de un solo uso, que tienen su propio estado).
        void Agregar(int copias, System.Func<Carta> crear)
        {
            for (int i = 0; i < copias; i++) cartas.Add(crear());
        }

        // --- Ataque ---
        Agregar(2, () => new CartaPasiva("Filo Eterno", TipoAccion.Atacar, 4));
        Agregar(4, () => new CartaUnUso("Golpe Rapido", TipoAccion.Atacar, 4));
        Agregar(2, () => new CartaUnUso("Golpe Directo", TipoAccion.Atacar, 5));
        Agregar(2, () => new CartaVencimiento("Impulso", TipoAccion.Atacar, 5, 2));
        Agregar(2, () => new CartaVencimiento("Frenesi", TipoAccion.Atacar, 6, 3));
        Agregar(2, () => new CartaReflectante("Espejo de Sangre", 6));

        // --- Monedas ---
        Agregar(2, () => new CartaPasiva("Vena de Oro", TipoAccion.Recolectar, 2));
        Agregar(4, () => new CartaUnUso("Rebusque", TipoAccion.Recolectar, 4));
        Agregar(2, () => new CartaUnUso("Bolsa de Monedas", TipoAccion.Recolectar, 5));
        Agregar(2, () => new CartaVencimiento("Comerciante", TipoAccion.Recolectar, 5, 2));
        Agregar(3, () => new CartaVencimiento("Racha", TipoAccion.Recolectar, 6, 3));

        // --- Curacion ---
        Agregar(1, () => new CartaPasiva("Savia Vital", TipoAccion.Curarse, 2));
        Agregar(3, () => new CartaUnUso("Alivio", TipoAccion.Curarse, 4));
        Agregar(2, () => new CartaVencimiento("Recuperacion", TipoAccion.Curarse, 5, 2));
        Agregar(2, () => new CartaVencimiento("Vitalidad Sostenida", TipoAccion.Curarse, 6, 3));

        // --- Proteccion ---
        Agregar(4, () => new CartaReaccion("Escudo de Monedas"));

        // NOTA: las cartas grupales y los comodines se sumarán más adelante.

        return new Mazo(cartas);
    }
}
