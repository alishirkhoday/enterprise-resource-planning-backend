namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// How far along a deal is. Each stage carries an agreed probability, which is what makes one
/// salesperson's forecast comparable with another's.
/// </summary>
public enum OpportunityStage
{
    /// <summary>Making first contact.</summary>
    Prospecting = 0,

    /// <summary>Establishing whether there is a real budget and need.</summary>
    Qualification = 1,

    NeedsAnalysis = 2,
    Proposal = 3,
    Negotiation = 4,
    ClosedWon = 5,
    ClosedLost = 6
}
