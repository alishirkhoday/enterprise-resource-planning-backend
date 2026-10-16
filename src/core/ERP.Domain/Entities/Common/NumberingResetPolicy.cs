namespace ERP.Domain.Entities.Common;

/// <summary>
/// When a numbering series starts counting again. Most Iranian documents restart each
/// financial year, which is why the series knows about years at all.
/// </summary>
public enum NumberingResetPolicy
{
    Never = 0,
    Daily = 1,
    Monthly = 2,
    Yearly = 3
}
