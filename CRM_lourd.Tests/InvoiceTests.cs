using Microsoft.VisualStudio.TestTools.UnitTesting;
using CRM_lourd.Services;

namespace CRM_lourd.Tests
{
    [TestClass]
    public class InvoiceTests
    {
        // ── CalculerMontantTTC ──────────────────────────────

        [TestMethod]
        public void TestCalculTTC_TVA20_RetourneMontantCorrect()
        {
            var service = new InvoiceService();
            var resultat = service.CalculerMontantTTC(100, 0.20);
            Assert.AreEqual(120m, resultat);
        }

        [TestMethod]
        public void TestCalculTTC_TVA10_RetourneMontantCorrect()
        {
            var service = new InvoiceService();
            var resultat = service.CalculerMontantTTC(200, 0.10);
            Assert.AreEqual(220m, resultat);
        }

        [TestMethod]
        public void TestCalculTTC_MontantNegatif_RetourneZero()
        {
            var service = new InvoiceService();
            var resultat = service.CalculerMontantTTC(-50, 0.20);
            Assert.AreEqual(0m, resultat);
        }

        [TestMethod]
        public void TestCalculTTC_MontantZero_RetourneZero()
        {
            var service = new InvoiceService();
            var resultat = service.CalculerMontantTTC(0, 0.20);
            Assert.AreEqual(0m, resultat);
        }

        // ── VerifierDisponibilite ───────────────────────────

        [TestMethod]
        public void TestStock_Disponible_RetourneTrue()
        {
            var service = new InvoiceService();
            bool resultat = service.VerifierDisponibilite(10, 5);
            Assert.IsTrue(resultat);
        }

        [TestMethod]
        public void TestStock_ExactementEgal_RetourneTrue()
        {
            var service = new InvoiceService();
            bool resultat = service.VerifierDisponibilite(5, 5);
            Assert.IsTrue(resultat);
        }

        [TestMethod]
        public void TestStock_Insuffisant_RetourneFalse()
        {
            var service = new InvoiceService();
            bool resultat = service.VerifierDisponibilite(5, 10);
            Assert.IsFalse(resultat);
        }

        [TestMethod]
        public void TestStock_QuantiteZero_RetourneFalse()
        {
            var service = new InvoiceService();
            bool resultat = service.VerifierDisponibilite(10, 0);
            Assert.IsFalse(resultat);
        }

        [TestMethod]
        public void TestStock_QuantiteNegative_RetourneFalse()
        {
            var service = new InvoiceService();
            bool resultat = service.VerifierDisponibilite(10, -1);
            Assert.IsFalse(resultat);
        }
    }
}