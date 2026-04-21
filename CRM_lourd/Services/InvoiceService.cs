namespace CRM_lourd.Services
{
    public class InvoiceService
    {
        public decimal CalculerMontantTTC(decimal montantHT, double tauxTVA)
        {
            if (montantHT < 0) return 0;
            return montantHT * (decimal)(1 + tauxTVA);
        }

        // Renommé ici pour correspondre au test
        public bool VerifierDisponibilite(int stockActuel, int quantiteDemandee)
        {
            return quantiteDemandee > 0 && stockActuel >= quantiteDemandee;
        }
    }
}