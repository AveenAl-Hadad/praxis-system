using Microsoft.EntityFrameworkCore;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Persistence;
using Praxis.Domain.Constants;

namespace Praxis.Infrastructure.Services
{
    /// <summary>
    /// Service zur Verwaltung von Terminen (Appointments).
    /// Enthält CRUD-Operationen sowie Logik für Validierung und Konfliktprüfung.
    /// </summary>
    public class AppointmentService : IAppointmentService
    {
        private readonly PraxisDbContext _context;

        /// <summary>
        /// Konstruktor mit Dependency Injection für den DbContext.
        /// </summary>
        public AppointmentService(PraxisDbContext context)
        {
            _context = context;
        }
        /// <summary>
        /// Fügt einen neuen Termin hinzu.
        /// </summary>
        public async Task AddAppointmentAsync(Appointment appointment)
        {
            ValidateAppointment(appointment);  // Eingaben prüfen
            await CheckForConflictAsync(appointment); // Zeitkonflikte prüfen
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
        }
        /// <summary>
        /// Gibt alle Termine sortiert nach Startzeit zurück.
        /// </summary>
        public async Task<List<Appointment>> GetAllAppointmentsAsync()
        {
            return await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.AppointmentType)
                .OrderBy(a => a.StartTime)
                .ToListAsync();
        }
        /// <summary>
        /// Gibt alle Termine für einen bestimmten Tag zurück.
        /// </summary>
        public async Task<List<Appointment>> GetAppointmentsByDateAsync(DateTime date)
        {
            var nextDate = date.Date.AddDays(1);

            return await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.AppointmentType)
                .Where(a => a.StartTime >= date.Date && a.StartTime < nextDate)
                .OrderBy(a => a.StartTime)
                .ToListAsync();
        }
        /// <summary>
        /// Gibt alle Termine einer Woche zurück.
        /// </summary>
        public async Task<List<Appointment>> GetAppointmentsByWeekAsync(DateTime startOfWeek)
        {
            var start = startOfWeek.Date;
            var end = start.AddDays(7);

            return await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.AppointmentType)
                .Where(a => a.StartTime >= start && a.StartTime < end)
                .OrderBy(a => a.StartTime)
                .ToListAsync();
        }
        /// <summary>
        /// Gibt Termine einer Woche optional gefiltert nach Patient zurück.
        /// </summary>
        public async Task<List<Appointment>> GetAppointmentsByWeekAndPatientAsync(DateTime startOfWeek, int? patientId)
        {
            var start = startOfWeek.Date;
            var end = start.AddDays(7);

            var query = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.AppointmentType)
                .Where(a => a.StartTime >= start && a.StartTime < end);

            if (patientId.HasValue)
            {
                query = query.Where(a => a.PatientId == patientId.Value);
            }

            return await query
                .OrderBy(a => a.StartTime)
                .ToListAsync();
        }
        /// <summary>
        /// Holt einen Termin anhand seiner ID.
        /// </summary>
        public async Task<Appointment?> GetAppointmentByIdAsync(int id)
        {
            return await _context.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == id);
        }
        /// <summary>
        /// Gibt alle "aktiven" Termine eines Tages zurück (z.B. fürs Wartezimmer).
        /// </summary>
        public async Task<List<Appointment>> GetWaitingRoomAppointmentsAsync(DateTime date)
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);

            return await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.AppointmentType)
                .Where(a => a.StartTime >= startOfDay && a.StartTime < endOfDay)
                .Where(a => a.TreatmentState != AppointmentStates.Abgesagt
                            && a.TreatmentState != AppointmentStates.Abgeschlossen)
                .OrderBy(a => a.CheckedInAt ?? a.StartTime)
                .ToListAsync();
        }
        /// <summary>
        /// Aktualisiert einen bestehenden Termin.
        /// </summary>
        public async Task UpdateAppointmentAsync(Appointment appointment)
        {
            ValidateAppointment(appointment);
            await CheckForConflictAsync(appointment);

            var existing = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == appointment.Id);

            if (existing == null)
                throw new InvalidOperationException("Termin wurde nicht gefunden.");

            //Felder aktualisieren
            existing.PatientId = appointment.PatientId;
            existing.StartTime = appointment.StartTime;
            existing.DurationMinutes = appointment.DurationMinutes;
            existing.Reason = appointment.Reason;
            existing.Status = appointment.Status;
            existing.RoomName = appointment.RoomName;
            existing.QueueNumber = appointment.QueueNumber;
            existing.CheckedInAt = appointment.CheckedInAt;
            existing.InternalNote = appointment.InternalNote;
            existing.TreatmentState = appointment.TreatmentState;

            await _context.SaveChangesAsync();
        }
        /// <summary>
        /// Aktualisiert nur den Status eines Termins.
        /// </summary>
        public async Task UpdateAppointmentStatusAsync(int appointmentId, string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                throw new ArgumentException("Status darf nicht leer sein.");

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
                throw new InvalidOperationException("Termin wurde nicht gefunden.");

            appointment.Status = status.Trim();
            await _context.SaveChangesAsync();
        }
        /// <summary>
        /// Löscht einen Termin.
        /// </summary>
        public async Task DeleteAppointmentAsync(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
                throw new InvalidOperationException("Termin wurde nicht gefunden.");

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
        }
        /// <summary>
        /// Validiert die Eingabedaten eines Termins.
        /// </summary>
        private void ValidateAppointment(Appointment appointment)
        {
            if (appointment.PatientId <= 0)
                throw new ArgumentException("Patient muss ausgewählt werden.");

            if (appointment.StartTime == default)
                throw new ArgumentException("Startzeit ist ungültig.");

            if (appointment.DurationMinutes <= 0)
                throw new ArgumentException("Dauer muss größer als 0 sein.");

            if (string.IsNullOrWhiteSpace(appointment.Reason))
                throw new ArgumentException("Grund darf nicht leer sein.");

            if (string.IsNullOrWhiteSpace(appointment.RoomName))
                throw new ArgumentException("Raum muss ausgewählt werden.");

            appointment.Reason = appointment.Reason.Trim();
            appointment.RoomName = appointment.RoomName.Trim();

            appointment.Status = string.IsNullOrWhiteSpace(appointment.Status)
                                    ? AppointmentStates.Geplant
                                    : appointment.Status.Trim();

            appointment.TreatmentState = string.IsNullOrWhiteSpace(appointment.TreatmentState)
                                        ? AppointmentStates.Geplant
                                        : appointment.TreatmentState.Trim();
        }
        /// <summary>
        /// Prüft, ob ein Termin mit bestehenden Terminen kollidiert.
        /// </summary>
        private async Task CheckForConflictAsync(Appointment appointment)
        {
            var roomName = appointment.RoomName?.Trim();

            var isAvailable = await IsTimeSlotAvailableAsync(
                appointment.StartTime,
                appointment.DurationMinutes,
                roomName,
                appointment.Id == 0 ? null : appointment.Id);

            if (!isAvailable)
            {
                if (string.IsNullOrWhiteSpace(roomName))
                    throw new InvalidOperationException("Es existiert bereits ein Termin in diesem Zeitraum.");

                throw new InvalidOperationException(
                    $"Der Raum '{roomName}' ist in diesem Zeitraum bereits belegt.");
            }
        }
        /// <summary>
        /// Gibt verfügbare Zeitfenster für einen Tag zurück.
        /// </summary>
        public async Task<List<DateTime>> GetAvailableSlotsAsync(DateTime date, int durationMinutes, string? roomName = null)
        {
            var availableSlots = new List<DateTime>();

            if (date.Date < DateTime.Today)
                return availableSlots;

            if (durationMinutes <= 0)
                return availableSlots;

            var normalizedRoomName = roomName?.Trim();
            var workingRanges = GetWorkingTimeRanges(date);
            var now = DateTime.Now;
            var isToday = date.Date == now.Date;

            foreach (var range in workingRanges)
            {
                var firstPossibleSlot = range.Start;

                if (isToday)
                {
                    var nextPossibleTime = RoundUpToNext15Minutes(now);

                    if (nextPossibleTime > firstPossibleSlot)
                    {
                        firstPossibleSlot = nextPossibleTime;
                    }
                }

                for (var slot = firstPossibleSlot; slot.AddMinutes(durationMinutes) <= range.End; slot = slot.AddMinutes(15))
                {
                    var isAvailable = await IsTimeSlotAvailableAsync(
                        slot,
                        durationMinutes,
                        normalizedRoomName);

                    if (isAvailable)
                    {
                        availableSlots.Add(slot);
                    }
                }
            }

            return availableSlots;
        }
        public async Task<List<DateTime>> GetAvailableSlotsForEditAsync(
                                                                    DateTime date,
                                                                    int durationMinutes,
                                                                    string? roomName,
                                                                    int appointmentId)
        {
            var availableSlots = new List<DateTime>();

            if (date.Date < DateTime.Today)
                return availableSlots;

            if (durationMinutes <= 0)
                return availableSlots;

            var normalizedRoomName = roomName?.Trim();
            var workingRanges = GetWorkingTimeRanges(date);
            var now = DateTime.Now;
            var isToday = date.Date == now.Date;

            foreach (var range in workingRanges)
            {
                var firstPossibleSlot = range.Start;

                if (isToday)
                {
                    var nextPossibleTime = RoundUpToNext15Minutes(now);

                    if (nextPossibleTime > firstPossibleSlot)
                    {
                        firstPossibleSlot = nextPossibleTime;
                    }
                }

                for (var slot = firstPossibleSlot; slot.AddMinutes(durationMinutes) <= range.End; slot = slot.AddMinutes(15))
                {
                    var isAvailable = await IsTimeSlotAvailableAsync(
                        slot,
                        durationMinutes,
                        normalizedRoomName,
                        appointmentId);

                    if (isAvailable)
                    {
                        availableSlots.Add(slot);
                    }
                }
            }

            var currentAppointment = await _context.Appointments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (currentAppointment != null &&
                currentAppointment.StartTime.Date == date.Date &&
                string.Equals(
                    currentAppointment.RoomName?.Trim(),
                    normalizedRoomName,
                    StringComparison.OrdinalIgnoreCase))
            {
                if (!availableSlots.Contains(currentAppointment.StartTime))
                {
                    availableSlots.Add(currentAppointment.StartTime);
                }
            }

            return availableSlots
                .OrderBy(x => x)
                .ToList();
        }
        /// <summary>
        /// Gibt die Arbeitszeiten je Wochentag zurück.
        /// </summary>
        private List<(DateTime Start, DateTime End)> GetWorkingTimeRanges(DateTime date)
        {
            var ranges = new List<(DateTime Start, DateTime End)>();

            switch (date.DayOfWeek)
            {
                case DayOfWeek.Monday:
                case DayOfWeek.Tuesday:
                case DayOfWeek.Thursday:
                    ranges.Add((date.Date.AddHours(8), date.Date.AddHours(12)));
                    ranges.Add((date.Date.AddHours(15), date.Date.AddHours(18)));
                    break;

                case DayOfWeek.Wednesday:
                    ranges.Add((date.Date.AddHours(8), date.Date.AddHours(12)));
                    break;

                case DayOfWeek.Friday:
                    ranges.Add((date.Date.AddHours(8), date.Date.AddHours(14)));
                    break;

                case DayOfWeek.Saturday:
                case DayOfWeek.Sunday:
                    break;
            }

            return ranges;
        }
        /// <summary>
        /// Rundet eine Uhrzeit auf das nächste 15-Minuten-Intervall auf.
        /// </summary>
        private DateTime RoundUpToNext15Minutes(DateTime dateTime)
        {
            var trimmed = new DateTime(
                dateTime.Year,
                dateTime.Month,
                dateTime.Day,
                dateTime.Hour,
                dateTime.Minute,
                0);

            var remainder = trimmed.Minute % 15;

            if (remainder == 0)
                return trimmed > dateTime ? trimmed : trimmed;

            return trimmed.AddMinutes(15 - remainder);
        }
        /// <summary>
        /// Prüft, ob ein Zeitfenster frei ist (keine Überschneidung).
        /// </summary>
        public async Task<bool> IsTimeSlotAvailableAsync(
                                                         DateTime startTime,
                                                         int durationMinutes,
                                                         string? roomName = null,
                                                         int? excludeAppointmentId = null)
        {
            if (durationMinutes <= 0)
                return false;

            var endTime = startTime.AddMinutes(durationMinutes);
            var normalizedRoomName = roomName?.Trim();

            var query = _context.Appointments.AsQueryable();

            if (excludeAppointmentId.HasValue)
            {
                query = query.Where(a => a.Id != excludeAppointmentId.Value);
            }

            // Abgesagte Termine sollen keinen Raum blockieren
            query = query.Where(a => a.TreatmentState != "Abgesagt" && a.Status != "Abgesagt");

            // Nur Konflikte im selben Raum prüfen
            if (!string.IsNullOrWhiteSpace(normalizedRoomName))
            {
                query = query.Where(a => a.RoomName == normalizedRoomName);
            }

            var conflict = await query.AnyAsync(a =>
                startTime < a.StartTime.AddMinutes(a.DurationMinutes) &&
                endTime > a.StartTime);

            return !conflict;
        }
        public async Task CheckInAsync(int appointmentId, string? note = null)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
                throw new InvalidOperationException("Termin wurde nicht gefunden.");

            appointment.CheckedInAt = DateTime.Now;
            appointment.CheckInTime = DateTime.Now;
            appointment.TreatmentState = AppointmentStates.Wartet;
            appointment.Status = "Angemeldet";

            if (!string.IsNullOrWhiteSpace(note))
                appointment.InternalNote = note.Trim();

            await _context.SaveChangesAsync();
        }

        public async Task MoveToRoomAsync(int appointmentId, string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName))
                throw new ArgumentException("Raumname darf nicht leer sein.");

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
                throw new InvalidOperationException("Termin wurde nicht gefunden.");

            appointment.RoomName = roomName.Trim();
            appointment.TreatmentState = AppointmentStates.InBehandlung;
            appointment.Status = "In Behandlung";
            await _context.SaveChangesAsync();
        }

        public async Task CompleteAppointmentAsync(int appointmentId)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
                throw new InvalidOperationException("Termin wurde nicht gefunden.");

            appointment.TreatmentState = AppointmentStates.Abgeschlossen;
            appointment.Status = AppointmentStates.Abgeschlossen;
            await _context.SaveChangesAsync();
        }

        public async Task CancelAppointmentAsync(int appointmentId, string? note = null)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
                throw new InvalidOperationException("Termin wurde nicht gefunden.");

            appointment.TreatmentState = AppointmentStates.Abgesagt;
            appointment.Status = AppointmentStates.Abgesagt;

            if (!string.IsNullOrWhiteSpace(note))
                appointment.InternalNote = note.Trim();

            await _context.SaveChangesAsync();
        }

        public async Task<List<DateTime>> GetAvailableOnlineSlotsAsync(DateTime date, int appointmentTypeId, int doctorId)
        {
            var result = new List<DateTime>();

            if (date.Date < DateTime.Today)
                return result;

            var appointmentType = await _context.AppointmentTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == appointmentTypeId);

            if (appointmentType == null)
                return result;

            if (!appointmentType.IsActive || !appointmentType.AllowOnlineBooking)
                return result;

            var doctor = await _context.Doctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == doctorId);

            if (doctor == null)
                return result;

            if (!doctor.IsActive || !doctor.AllowOnlineBooking)
                return result;

            var doctorAllowed = await _context.DoctorAppointmentTypes
                .AnyAsync(x => x.DoctorId == doctorId && x.AppointmentTypeId == appointmentTypeId);

            if (!doctorAllowed)
                return result;

            var now = DateTime.Now;
            var earliestAllowed = now.AddHours(appointmentType.MinLeadHours);
            var latestAllowed = now.Date.AddDays(appointmentType.MaxAdvanceDays + 1).AddTicks(-1);

            if (date.Date < earliestAllowed.Date || date.Date > latestAllowed.Date)
                return result;

            var schedules = (await _context.DoctorSchedules
                .AsNoTracking()
                .Where(s => s.DoctorId == doctorId &&
                            s.IsActive &&
                            s.DayOfWeek == date.DayOfWeek)
                .ToListAsync())
                .OrderBy(s => s.StartTime)
                .ToList();

            if (schedules.Count == 0)
                return result;

            var dayStart = date.Date;
            var dayEnd = date.Date.AddDays(1);

            var blockTimes = await _context.DoctorBlockTimes
                .AsNoTracking()
                .Where(x => x.DoctorId == doctorId && x.IsActive)
                .Where(x => x.StartTime < dayEnd && x.EndTime > dayStart)
                .ToListAsync();

            var appointments = (await _context.Appointments
                  .AsNoTracking()
                  .Where(a => a.DoctorId == doctorId)
                  .Where(a => a.TreatmentState != "Abgesagt")
                  .Where(a => a.StartTime < dayEnd)
                  .ToListAsync())
                  .Where(a => a.StartTime.AddMinutes(a.DurationMinutes) > dayStart)
                  .ToList();

            foreach (var schedule in schedules)
            {
                var rangeStart = date.Date.Add(schedule.StartTime);
                var rangeEnd = date.Date.Add(schedule.EndTime);

                var slot = rangeStart;

                if (date.Date == now.Date)
                {
                    var roundedNow = RoundUpToNext15Minutes(now);
                    if (roundedNow > slot)
                        slot = roundedNow;
                }

                while (slot.AddMinutes(appointmentType.DurationMinutes) <= rangeEnd)
                {
                    var slotEnd = slot.AddMinutes(appointmentType.DurationMinutes);

                    if (slot < earliestAllowed)
                    {
                        slot = slot.AddMinutes(15);
                        continue;
                    }

                    var overlapsBreak =
                        schedule.BreakStart.HasValue &&
                        schedule.BreakEnd.HasValue &&
                        slot < date.Date.Add(schedule.BreakEnd.Value) &&
                        slotEnd > date.Date.Add(schedule.BreakStart.Value);

                    if (overlapsBreak)
                    {
                        slot = slot.AddMinutes(15);
                        continue;
                    }

                    var overlapsBlock = blockTimes.Any(x =>
                        x.StartTime < slotEnd &&
                        x.EndTime > slot);

                    if (overlapsBlock)
                    {
                        slot = slot.AddMinutes(15);
                        continue;
                    }

                    var overlapsAppointment = appointments.Any(a =>
                        a.StartTime < slotEnd &&
                        a.EndTime > slot);

                    if (!overlapsAppointment)
                    {
                        result.Add(slot);
                    }

                    slot = slot.AddMinutes(15);
                }
            }

            return result;
        }
        public async Task AddOnlineAppointmentAsync(int patientId, int appointmentTypeId, int doctorId, DateTime startTime)
        {
            var patientExists = await _context.Patients.AnyAsync(p => p.Id == patientId);
            if (!patientExists)
                throw new InvalidOperationException("Patient wurde nicht gefunden.");

            var appointmentType = await _context.AppointmentTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == appointmentTypeId);

            if (appointmentType == null)
                throw new InvalidOperationException("Terminart wurde nicht gefunden.");

            if (!appointmentType.IsActive || !appointmentType.AllowOnlineBooking)
                throw new InvalidOperationException("Diese Terminart ist nicht online buchbar.");

            var doctor = await _context.Doctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == doctorId);

            if (doctor == null)
                throw new InvalidOperationException("Behandler wurde nicht gefunden.");

            if (!doctor.IsActive || !doctor.AllowOnlineBooking)
                throw new InvalidOperationException("Dieser Behandler ist nicht online buchbar.");

            var doctorAllowed = await _context.DoctorAppointmentTypes
                .AnyAsync(x => x.DoctorId == doctorId && x.AppointmentTypeId == appointmentTypeId);

            if (!doctorAllowed)
                throw new InvalidOperationException("Dieser Behandler darf diese Terminart nicht durchführen.");

            var availableSlots = await GetAvailableOnlineSlotsAsync(startTime.Date, appointmentTypeId, doctorId);
            if (!availableSlots.Contains(startTime))
                throw new InvalidOperationException("Der gewählte Termin ist nicht mehr verfügbar.");

            var appointment = new Appointment
            {
                PatientId = patientId,
                DoctorId = doctorId,
                AppointmentTypeId = appointmentTypeId,
                StartTime = startTime,
                DurationMinutes = appointmentType.DurationMinutes,
                Reason = appointmentType.Name,
                Status = "Bestätigt",
                TreatmentState = "Geplant",
                RoomName = doctor.DefaultRoomName,
                IsOnlineBooking = true
            };

            await AddAppointmentAsync(appointment);
        }

        private async Task<bool> IsDoctorAllowedForAppointmentTypeAsync(int doctorId, int appointmentTypeId)
        {
            return await _context.DoctorAppointmentTypes
                .AnyAsync(x => x.DoctorId == doctorId && x.AppointmentTypeId == appointmentTypeId);
        }

        private async Task<bool> HasDoctorBlockingConflictAsync(int doctorId, DateTime startTime, DateTime endTime)
        {
            return await _context.DoctorBlockTimes
                .Where(x => x.DoctorId == doctorId && x.IsActive)
                .Where(x => x.StartTime < endTime && x.EndTime > startTime)
                .AnyAsync();
        }


        public async Task<List<Appointment>> GetTodayOnlineAppointmentsAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            return await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.AppointmentType)
                .Where(a => a.IsOnlineBooking)
                .Where(a => a.StartTime >= today && a.StartTime < tomorrow)
                .OrderBy(a => a.StartTime)
                .ToListAsync();
        }

        public async Task<int> GetTodayOnlineAppointmentCountAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            return await _context.Appointments
                .Where(a => a.IsOnlineBooking)
                .Where(a => a.StartTime >= today && a.StartTime < tomorrow)
                .CountAsync();
        }

    }

}