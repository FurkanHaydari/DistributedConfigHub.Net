using DistributedConfigHub.Application.Interfaces;
using DistributedConfigHub.Domain.Entities;
using DistributedConfigHub.Domain.Enums;
using MediatR;

namespace DistributedConfigHub.Application.Features.Commands;

public record CreateConfigurationCommand(string Name, ConfigurationType Type, string Value, string ApplicationName, string Environment) : IRequest<Guid>;

public class CreateConfigurationCommandHandler(
    IConfigurationRepository repository, 
    IMessagePublisher messagePublisher) 
    : IRequestHandler<CreateConfigurationCommand, Guid>
{
    public async Task<Guid> Handle(CreateConfigurationCommand request, CancellationToken cancellationToken)
    {
        // 1. Veritabanında (Aktif veya Pasif) böyle bir kayıt var mı:
        var existingRecord = await repository.GetByNameAsync(request.Name, request.ApplicationName, request.Environment, cancellationToken);

        if (existingRecord != null)
        {
            if (existingRecord.IsActive)
            {
                // Kayıt var ve zaten aktif. Çakışma var (409 Conflict)
                throw new InvalidOperationException($"Configuration '{request.Name}' already exists.");
            }
            else
            {
                // Kayıt var ama pasif (Soft-deleted)
                // Tipini (Type) değiştirmeye çalışıyorsa izin verilmesin, çünkü eski loglar ve Consumer'lar patlayabilir.
                if (existingRecord.Type != request.Type)
                    throw new InvalidOperationException($"A deleted configuration exists but with type '{existingRecord.Type}'. You cannot change the type of a restored configuration.");

                // Unique Index (DbUpdateException) hatasını önlemek ve Audit Log zincirini koparmamak için
                // yeni bir satır eklemek yerine eski silinmiş kaydı diriltip (Restore) güncelliyoruz.
                existingRecord.Activate("admin");
                existingRecord.UpdateValue(request.Value, "admin");
                
                await repository.UpdateAsync(existingRecord, cancellationToken);
                
                // Event'i fırlat
                await messagePublisher.PublishConfigurationUpdatedEventAsync(existingRecord.ApplicationName, existingRecord.Environment, cancellationToken);
                
                return existingRecord.Id;
            }
        }

        // 2. Kayıt hiç yoksa sıfırdan yarat
        var newRecord = new ConfigurationRecord(
            request.Name, 
            request.Type, 
            request.Value, 
            request.ApplicationName, 
            request.Environment,
            "admin");

        // 3. Kaydet ve haber ver
        await repository.AddAsync(newRecord, cancellationToken);
        await messagePublisher.PublishConfigurationUpdatedEventAsync(newRecord.ApplicationName, newRecord.Environment, cancellationToken);

        return newRecord.Id;
    }
}