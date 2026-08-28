// ============================================================
//  GestorCartas.cs  —  Capa: RULES
//  Resuelve el efecto de las cartas ELEGIDAS sobre una acción:
//  bonus (con pago por uso de las de vencimiento), dados extra,
//  multiplicador y el descarte de las cartas gastadas.
//  No conoce Unity ni la UI: junta los avisos en 'Mensajes' para
//  que el GameManager (Visual) los muestre.
// ============================================================

using System.Collections.Generic;

public class GestorCartas
{
    private PilaDescarte descarte;
    public List<string> Mensajes { get; private set; } = new List<string>();

    public GestorCartas(PilaDescarte descarte)
    {
        this.descarte = descarte;
    }

    // Bonus total de las cartas elegidas para la acción.
    // Las de vencimiento pagan por uso (1er uso gratis, luego 2 monedas);
    // si no puede pagar o se quedan sin usos, se descartan.
    public int Bonus(List<Carta> seleccionadas, Jugador j, TipoAccion accion)
    {
        Mensajes.Clear();
        int total = 0;
        List<Carta> copia = new List<Carta>(seleccionadas);   // copia: podemos descartar

        foreach (Carta c in copia)
        {
            CartaVencimiento v = c as CartaVencimiento;
            if (v != null) 
            {
                if (!v.SirvePara(accion)) continue;

                int costo = v.CostoDelProximoUso();
                if (costo > 0 && !j.GastarMonedas(costo))
                {
                    Mensajes.Add(j.nombre + ": no pudo pagar " + v.nombre + ", se descarta.");
                    Descartar(seleccionadas, j, v);
                    continue;
                }
                if (costo > 0) Mensajes.Add(j.nombre + " paga " + costo + " por usar " + v.nombre + ".");

                total += v.Usar();
                if (v.SinUsos())
                {
                    Mensajes.Add(v.nombre + " se quedó sin usos y se descarta.");
                    Descartar(seleccionadas, j, v);
                }
            }
            else
            {
                total += c.BonusPara(accion);   // pasiva / un uso
            }
        }
        return total;
    }

    // Dados extra que suman las cartas elegidas (comodín +1d4).
    public int DadosExtra(List<Carta> seleccionadas, TipoAccion accion)
    {
        int extra = 0;
        foreach (Carta c in seleccionadas) extra += c.DadosExtra(accion);
        return extra;
    }

    // Multiplicador de las cartas elegidas (comodín x2). Empieza en 1.
    public int Multiplicador(List<Carta> seleccionadas, TipoAccion accion)
    {
        int mult = 1;
        foreach (Carta c in seleccionadas) mult *= c.Multiplicador(accion);
        return mult;
    }

    // COMBATE — REACCIÓN: el defensor absorbe daño con escudos (gasta monedas).
    // Devuelve el daño restante. Los avisos quedan en 'Mensajes'.
    public int Absorber(Jugador defensor, int dano)
    {
        foreach (Carta c in defensor.mano)
        {
            CartaReaccion escudo = c as CartaReaccion;
            if (escudo != null)
            {
                int antes = dano;
                dano = escudo.Absorber(dano, defensor);
                if (dano < antes) Mensajes.Add(defensor.nombre + " absorbe " + (antes - dano) + " con " + escudo.nombre + ".");
            }
        }
        return dano;
    }

    // COMBATE — REFLECTANTES: el defensor devuelve daño / roba monedas / se cura.
    public void Reflejar(Jugador defensor, Jugador atacante)
    {
        foreach (Carta c in defensor.mano)
        {
            // Espejo de Sangre: devuelve daño fijo.
            CartaReflectante espejo = c as CartaReflectante;
            if (espejo != null)
            {
                int reflejo = espejo.Reflejar();
                atacante.RecibirDanio(reflejo);
                Mensajes.Add(defensor.nombre + " refleja " + reflejo + " a " + atacante.nombre + " con " + espejo.nombre + ".");
                continue;
            }

            // Bolsillo Roto: roba monedas al atacante; el faltante entra como daño.
            CartaBolsilloRoto bolsillo = c as CartaBolsilloRoto;
            if (bolsillo != null)
            {
                int robado = System.Math.Min(bolsillo.monedasARobar, atacante.monedas);
                atacante.GastarMonedas(robado);
                defensor.GanarMonedas(robado);
                int faltante = bolsillo.monedasARobar - robado;
                if (faltante > 0) atacante.RecibirDanio(faltante);
                bolsillo.usada = true;
                Mensajes.Add(defensor.nombre + " le roba " + robado + " monedas a " + atacante.nombre +
                    (faltante > 0 ? " (+" + faltante + " de daño)" : "") + " con " + bolsillo.nombre + ".");
                continue;
            }

            // Vampirismo Defensivo: el defensor se cura al ser atacado.
            CartaVampirismoDefensivo vamp = c as CartaVampirismoDefensivo;
            if (vamp != null)
            {
                defensor.Curar(vamp.curacion);
                vamp.usada = true;
                Mensajes.Add(defensor.nombre + " se cura " + vamp.curacion + " con " + vamp.nombre + ".");
                continue;
            }
        }
    }

    // Manda al descarte las cartas de un solo uso ya gastadas (un uso / comodines).
    public void DescartarUsadas(Jugador j)
    {
        for (int i = j.mano.Count - 1; i >= 0; i--)
            if (j.mano[i].DebeDescartarse())
            {
                descarte.Agregar(j.mano[i]);
                j.mano.RemoveAt(i);
            }
    }

    // Saca una carta de la mano (y de la selección) y la manda al descarte.
    private void Descartar(List<Carta> seleccionadas, Jugador j, Carta c)
    {
        seleccionadas.Remove(c);
        j.mano.Remove(c);
        descarte.Agregar(c);
    }
}
