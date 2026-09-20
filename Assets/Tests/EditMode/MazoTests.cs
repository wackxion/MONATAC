// ============================================================
//  MazoTests.cs  —  Pruebas unitarias (EditMode / NUnit)
//  Verifica el Mazo y la PilaDescarte (capa Data): robar/vaciar
//  y el "mazo circular" (reciclar el descarte). Lógica pura.
// ============================================================

using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class MazoTests
{
    // Helper: crea una carta cualquiera para llenar el mazo.
    private Carta CartaCualquiera(string nombre)
    {
        return new CartaPasiva(nombre, TipoAccion.Atacar, 1);
    }

    // ------------------------------------------------------------
    //  REGLA: al robar la última carta, el mazo queda vacío y
    //  robar de un mazo vacío devuelve null.
    // ------------------------------------------------------------
    [Test]
    public void Robar_SacaCartaYVaciaElMazo()
    {
        // ARRANGE: mazo con una sola carta.
        Mazo mazo = new Mazo(new List<Carta> { CartaCualquiera("Filo Eterno") });

        // ACT + ASSERT
        Assert.IsNotNull(mazo.Robar(), "Debe devolver la carta que hay.");
        Assert.IsTrue(mazo.EstaVacio(), "Tras robar la única carta, queda vacío.");
        Assert.IsNull(mazo.Robar(), "Robar de un mazo vacío devuelve null.");
    }

    // ------------------------------------------------------------
    //  REGLA (mazo circular): al reciclar, el mazo recupera las
    //  cartas del descarte y el descarte queda vacío.
    // ------------------------------------------------------------
    [Test]
    public void Reciclar_RecuperaLasCartasDelDescarte()
    {
        // ARRANGE: mazo vacío y descarte con 2 cartas.
        Mazo mazo = new Mazo(new List<Carta>());
        PilaDescarte descarte = new PilaDescarte();
        descarte.Agregar(CartaCualquiera("Golpe Rápido"));
        descarte.Agregar(CartaCualquiera("Alivio"));

        // ACT
        mazo.Reciclar(descarte);

        // ASSERT
        Assert.IsFalse(mazo.EstaVacio(), "Después de reciclar, el mazo tiene cartas.");
        Assert.AreEqual(2, mazo.Cantidad(), "Debe tener las 2 cartas que estaban en el descarte.");
        Assert.AreEqual(0, descarte.TomarTodas().Count, "El descarte debe quedar vacío tras reciclar.");
    }
}
