namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// What kind of leave - استحقاقی، استعلاجی و غیره. Each kind has its own balance, so this
/// is part of the key an entitlement is looked up by.
/// </summary>
public enum HumanRequestLeaveType
{
    Paid = 0,
    Unpaid = 1,
    Sick = 2,
    Maternity = 3,
    Paternity = 4,
    Marriage = 5,
    Compassionate = 6,
    Other = 7
}
