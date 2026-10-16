namespace ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

/// <summary>
/// Whether the customer is a person or an organisation. Decides which name is used on
/// documents and which tax identifiers are required.
/// </summary>
public enum CustomerType
{
    Individual = 0,
    Organization = 1
}
