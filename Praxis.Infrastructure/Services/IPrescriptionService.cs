using Praxis.Domain.Entities;

namespace Praxis.Infrastructure.Services;

public interface IPrescriptionService
{
    Task<List<Prescription>> GetAllPrescriptionsAsync();
    Task<List<Prescription>> GetPrescriptionsByPatientAsync(int patientId);
    Task<Prescription?> GetPrescriptionByIdAsync(int id);
    Task AddPrescriptionAsync(Prescription prescription, string userName);
    Task DeletePrescriptionAsync(int id, string usreName);
}