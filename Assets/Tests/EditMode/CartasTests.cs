// ============================================================
//  CartasTests.cs  —  Pruebas unitarias (EditMode / NUnit)
//  Verifica la lógica de las cartas (capa Data): el método
//  AplicaA (para resaltar cartas usables) y la absorción de
//  daño de la CartaReaccion. Lógica pura, sin Unity.
// ============================================================

using NUnit.Framework;

[TestFixture]
public class CartasTests
{
    // ------------------------------------------------------------
    //  REGLA (AplicaA): una carta con acción asociada solo aplica
    //  a esa acción.
    // ------------------------------------------------------------
    [Test]
    public void AplicaA_PasivaSoloSuAccion()
    {
        // ARRANGE: pasiva de Ataque.
        Carta carta = new CartaPasiva("Filo Eterno", TipoAccion.Atacar, 4);

        // ACT + ASSERT
        Assert.IsTrue(carta.AplicaA(TipoAccion.Atacar), "Debe aplicar a su propia acción.");
        Assert.IsFalse(carta.AplicaA(TipoAccion.Curarse), "No debe aplicar a otra acción.");
    }

    // ------------------------------------------------------------
    //  REGLA (AplicaA): el comodín de dado sirve para cualquier acción.
    // ------------------------------------------------------------
    [Test]
    public void AplicaA_ComodinDadoSirveParaCualquierAccion()
    {
        // ARRANGE
        Carta carta = new CartaComodinDado("Comodín Adicional", 1);

        // ACT + ASSERT
        Assert.IsTrue(carta.AplicaA(TipoAccion.Atacar), "El comodín de dado sirve para atacar.");
        Assert.IsTrue(carta.AplicaA(TipoAccion.Recolectar), "…y también para recolectar.");
    }

    // ------------------------------------------------------------
    //  REGLA (AplicaA): las cartas defensivas NO se eligen a mano.
    // ------------------------------------------------------------
    [Test]
    public void AplicaA_DefensivasNuncaAplican()
    {
        // ARRANGE
        Carta escudo = new CartaReaccion("Escudo de Monedas");

        // ACT + ASSERT
        Assert.IsFalse(escudo.AplicaA(TipoAccion.Atacar), "Las defensivas se activan solas, no se eligen.");
        Assert.IsFalse(escudo.AplicaA(TipoAccion.Curarse), "Idem para cualquier acción.");
    }

    // ------------------------------------------------------------
    //  REGLA (CartaReaccion): absorbe 1 de daño por cada 2 monedas,
    //  hasta que se acaba el daño o las monedas.
    // ------------------------------------------------------------
    [Test]
    public void CartaReaccion_AbsorbeSegunLasMonedas()
    {
        // ARRANGE: jugador con 4 monedas (alcanza para absorber 2 de daño).
        Jugador jugador = new Jugador("Test", 40);
        jugador.GanarMonedas(4);
        CartaReaccion escudo = new CartaReaccion("Escudo de Monedas");

        // ACT: recibe 5 de daño; con 4 monedas absorbe 2 (gasta 4 monedas).
        int danioRestante = escudo.Absorber(5, jugador);

        // ASSERT
        Assert.AreEqual(3, danioRestante, "Debe quedar 3 de daño (absorbió 2).");
        Assert.AreEqual(0, jugador.monedas, "Debe haber gastado las 4 monedas.");
    }
}
