using Microsoft.VisualStudio.TestTools.UnitTesting;
using CRM_lourd.Services; // Accès à vos services

namespace CRM_lourd.Tests
{
    [TestClass]
    public class InvoiceTests
    {
        [TestMethod]
        public void TestCalculTVA_20Pourcent()
        {
            // Arrange
            var service = new InvoiceService();
            decimal montantHT = 100;
            double tva = 0.20;

            // Act
            var resultat = service.CalculerMontantTTC(montantHT, tva);

            // Assert
            Assert.AreEqual(120, resultat);
        }

        [TestMethod]
        public void TestStock_Indisponible()
        {
            var service = new InvoiceService();
            int stock = 5;
            int demande = 10;

            // Le nom doit être identique à celui défini dans le service
            bool estPossible = service.VerifierDisponibilite(stock, demande);

            Assert.IsFalse(estPossible);
        }
    }
}