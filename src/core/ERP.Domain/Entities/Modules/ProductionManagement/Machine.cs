namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// A piece of equipment on the shop floor. Its running hours and status are what drive
/// preventive maintenance and availability reporting.
/// </summary>
public sealed class Machine : BaseEntity, IBranchScoped
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Model { get; private set; } = default!;
    public string SerialNumber { get; private set; } = default!;
    public string Location { get; private set; } = default!;

    public MachineStatus Status { get; private set; }
    public string? Manufacturer { get; private set; }
    public DateOnly? PurchaseDate { get; private set; }
    public DateOnly? WarrantyExpiresOn { get; private set; }

    public Guid? WorkCenterId { get; private set; }
    public Guid? FixedAssetId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public Guid? BranchId { get; private set; }

    /// <summary>Nominal output per hour, used to plan capacity.</summary>
    public decimal? NominalCapacityPerHour { get; private set; }

    /// <summary>Hours the machine has run, which preventive schedules count against.</summary>
    public decimal RunningHours { get; private set; }

    /// <summary>Hours between preventive services, when maintenance is driven by usage.</summary>
    public decimal? MaintenanceIntervalHours { get; private set; }
    public decimal RunningHoursAtLastService { get; private set; }

    public string? Description { get; private set; }

    private Machine() { }

    public Machine(string code, string name, string model, string serialNumber, string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(model, nameof(model));
        ArgumentException.ThrowIfNullOrWhiteSpace(serialNumber, nameof(serialNumber));
        ArgumentException.ThrowIfNullOrWhiteSpace(location, nameof(location));

        Code = code.Trim();
        Name = name.Trim();
        Model = model.Trim();
        SerialNumber = serialNumber.Trim();
        Location = location.Trim();
        Status = MachineStatus.Operational;
    }

    public void Update(string name, string model, string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(model, nameof(model));
        ArgumentException.ThrowIfNullOrWhiteSpace(location, nameof(location));
        Name = name.Trim();
        Model = model.Trim();
        Location = location.Trim();
    }

    public void SetAcquisition(string? manufacturer, DateOnly? purchaseDate, DateOnly? warrantyExpiresOn)
    {
        Manufacturer = string.IsNullOrWhiteSpace(manufacturer) ? null : manufacturer.Trim();
        PurchaseDate = purchaseDate;
        WarrantyExpiresOn = warrantyExpiresOn;
    }

    public void AssignToWorkCenter(Guid workCenterId) => WorkCenterId = workCenterId == Guid.Empty ? null : workCenterId;

    public void LinkToFixedAsset(Guid fixedAssetId) => FixedAssetId = fixedAssetId == Guid.Empty ? null : fixedAssetId;

    public void AssignCostCenter(Guid costCenterId) => CostCenterId = costCenterId == Guid.Empty ? null : costCenterId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetCapacity(decimal nominalCapacityPerHour)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(nominalCapacityPerHour, nameof(nominalCapacityPerHour));
        NominalCapacityPerHour = nominalCapacityPerHour;
    }

    public void SetMaintenanceInterval(decimal intervalHours)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(intervalHours, nameof(intervalHours));
        MaintenanceIntervalHours = intervalHours;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void RecordRunningHours(decimal hours)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hours, nameof(hours));
        RunningHours += hours;
    }

    public void RecordService() => RunningHoursAtLastService = RunningHours;

    public void ReportBreakdown(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        Status = MachineStatus.BrokenDown;
        Description = reason.Trim();
        Raise(new MachineBrokeDown(Id, Code, Description));
    }

    public void StartMaintenance()
    {
        if (Status == MachineStatus.Retired)
            throw new InvalidOperationException("A retired machine cannot go into maintenance.");
        Status = MachineStatus.UnderMaintenance;
    }

    public void ReturnToService()
    {
        if (Status == MachineStatus.Retired)
            throw new InvalidOperationException("A retired machine cannot return to service.");
        Status = MachineStatus.Operational;
    }

    public void Retire(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        Status = MachineStatus.Retired;
        Description = reason.Trim();
    }

    /// <summary>Whether the machine has run far enough to be due its next service.</summary>
    public bool IsDueForMaintenance =>
        MaintenanceIntervalHours.HasValue
        && RunningHours - RunningHoursAtLastService >= MaintenanceIntervalHours.Value;

    public bool IsAvailable => Status == MachineStatus.Operational && !IsDeleted;

    public bool IsUnderWarrantyOn(DateOnly date) => WarrantyExpiresOn.HasValue && WarrantyExpiresOn.Value >= date;
}
