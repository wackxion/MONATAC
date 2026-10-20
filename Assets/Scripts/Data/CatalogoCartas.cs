// ============================================================
//  CatalogoCartas.cs  —  Capa: DATA (datos)
//  PARTE 2 de 3 del flujo (ver [[FabricaDeCartas]]):
//    1) CartaDefinicion = el molde · 2) ESTE = la tabla · 3) FabricaDeCartas = construye.
//
//  La TABLA de cartas del juego (las 54), como DATOS puros.
//  Antes esta info estaba hardcodeada en FabricaDeCartas con
//  "() => new CartaX(...)"; ahora es una lista declarativa.
//
//  AGREGAR UNA CARTA = agregar UNA línea acá (nombre, cantidad y
//  efecto). No se toca la fábrica ni se escribe "new CartaX(...)".
//
//  CÓMO LEER UNA FILA:
//    new CartaDefinicion( nombre , copias , clase , acción , valor , usos )
//    - nombre : cómo se llama (y con qué busca su sprite la Vista)
//    - copias : cuántas van al mazo
//    - clase  : qué tipo de carta construir (ClaseCarta)
//    - acción : a qué acción aplica (Atacar/Curarse/Recolectar) — las que la usan
//    - valor  : el número principal (bonus / daño / monedas / curación / factor / dados)
//    - usos   : solo Vencimiento (cuántos turnos dura)
// ============================================================

using System.Collections.Generic;

public static class CatalogoCartas
{
    public static List<CartaDefinicion> Todas()
    {
        return new List<CartaDefinicion>
        {
            // --- Ataque ---
            new CartaDefinicion("Filo Eterno",    2, ClaseCarta.Pasiva,               TipoAccion.Atacar, 4),
            new CartaDefinicion("Golpe Rapido",   4, ClaseCarta.UnUso,                TipoAccion.Atacar, 4),
            new CartaDefinicion("Golpe Directo",  2, ClaseCarta.UnUso,                TipoAccion.Atacar, 5),
            new CartaDefinicion("Impulso",        2, ClaseCarta.Vencimiento,          TipoAccion.Atacar, 5, usos: 2),
            new CartaDefinicion("Frenesi",        2, ClaseCarta.Vencimiento,          TipoAccion.Atacar, 6, usos: 3),
            new CartaDefinicion("Espejo de Sangre", 2, ClaseCarta.Reflectante,        valor: 6),
            new CartaDefinicion("Golpe Critico",  1, ClaseCarta.ComodinMultiplicador, TipoAccion.Atacar, 3),

            // --- Monedas ---
            new CartaDefinicion("Vena de Oro",      2, ClaseCarta.Pasiva,               TipoAccion.Recolectar, 2),
            new CartaDefinicion("Rebusque",         4, ClaseCarta.UnUso,                TipoAccion.Recolectar, 4),
            new CartaDefinicion("Bolsa de Monedas", 2, ClaseCarta.UnUso,                TipoAccion.Recolectar, 5),
            new CartaDefinicion("Comerciante",      2, ClaseCarta.Vencimiento,          TipoAccion.Recolectar, 5, usos: 2),
            new CartaDefinicion("Racha",            3, ClaseCarta.Vencimiento,          TipoAccion.Recolectar, 6, usos: 3),
            new CartaDefinicion("Golpe de Suerte",  2, ClaseCarta.ComodinMultiplicador, TipoAccion.Recolectar, 2),
            new CartaDefinicion("Bolsillo Roto",    2, ClaseCarta.BolsilloRoto,         valor: 10),

            // --- Curacion ---
            new CartaDefinicion("Savia Vital",         1, ClaseCarta.Pasiva,              TipoAccion.Curarse, 2),
            new CartaDefinicion("Alivio",              3, ClaseCarta.UnUso,               TipoAccion.Curarse, 4),
            new CartaDefinicion("Regeneracion Total",  1, ClaseCarta.UnUso,               TipoAccion.Curarse, 8),
            new CartaDefinicion("Vampirismo Defensivo",2, ClaseCarta.VampirismoDefensivo, valor: 5),
            new CartaDefinicion("Recuperacion",        2, ClaseCarta.Vencimiento,         TipoAccion.Curarse, 5, usos: 2),
            new CartaDefinicion("Vitalidad Sostenida", 2, ClaseCarta.Vencimiento,         TipoAccion.Curarse, 6, usos: 3),

            // --- Proteccion ---
            new CartaDefinicion("Escudo de Monedas", 4, ClaseCarta.Reaccion),

            // --- Comodines ---
            new CartaDefinicion("Comodin Multiplicador", 1, ClaseCarta.ComodinMultiplicador, valor: 2, conAccion: false),
            new CartaDefinicion("Comodin Adicional",     1, ClaseCarta.ComodinDado,           valor: 1),

            // --- Grupales (se activan al comprarse) ---
            new CartaDefinicion("Ley Marcial",     1, ClaseCarta.LeyMarcial),
            new CartaDefinicion("Colecta",         1, ClaseCarta.Colecta),
            new CartaDefinicion("Descarte Grupal", 1, ClaseCarta.DescarteGrupal),
            new CartaDefinicion("Exceso",          1, ClaseCarta.Exceso),
            new CartaDefinicion("Penitencia",      1, ClaseCarta.Penitencia),
        };
    }
}
