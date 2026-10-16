namespace ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

/// <summary>How an accepted claim is being honoured, which decides what work follows it.</summary>
public enum WarrantyClaimResolution
{
    /// <summary>Not decided yet.</summary>
    Pending = 0,

    /// <summary>Fix it, which becomes a service order.</summary>
    Repair = 1,

    /// <summary>Send another one, which becomes a replacement order.</summary>
    Replace = 2,

    /// <summary>Give the money back, which becomes a credit note.</summary>
    Refund = 3,

    /// <summary>Reduce what is owed without taking the goods back.</summary>
    PartialCredit = 4,

    /// <summary>Not covered.</summary>
    Declined = 5
}
