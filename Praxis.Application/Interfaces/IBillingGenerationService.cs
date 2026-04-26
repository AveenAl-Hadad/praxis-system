using Praxis.Domain.Entities;

namespace Praxis.Application.Interfaces;

public interface IBillingGenerationService
{
    Task<Invoice> CreateInvoiceFromAppointmentAsync(int appointmentId);
}