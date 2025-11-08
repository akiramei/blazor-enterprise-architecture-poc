using Shared.Kernel;

namespace PurchaseManagement.Shared.Domain.PurchaseRequests;

/// <summary>
/// 承認フロー定義（ValueObject）
/// </summary>
public class ApprovalFlow : ValueObject
{
    public IReadOnlyList<ApprovalFlowStep> Steps { get; }

    public ApprovalFlow(IEnumerable<ApprovalFlowStep> steps)
    {
        var stepList = steps.ToList();

        if (stepList.Count == 0)
            throw new DomainException("承認フローには最低1つのステップが必要です");

        if (stepList.Count > 5)
            throw new DomainException("承認フローは最大5段階までです");

        // ステップ番号の連続性チェック
        for (int i = 0; i < stepList.Count; i++)
        {
            if (stepList[i].StepNumber != i + 1)
                throw new DomainException("承認ステップ番号が連続していません");
        }

        Steps = stepList.AsReadOnly();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        foreach (var step in Steps)
            yield return step;
    }

    /// <summary>
    /// 1段階承認フロー（例: 10万円未満）
    /// </summary>
    public static ApprovalFlow SingleStep(Guid approverId, string approverName, string approverRole = "Manager")
    {
        return new ApprovalFlow(new[]
        {
            new ApprovalFlowStep(1, approverId, approverName, approverRole)
        });
    }

    /// <summary>
    /// 2段階承認フロー（例: 10万円以上50万円未満）
    /// </summary>
    public static ApprovalFlow TwoStep(
        Guid firstApproverId, string firstApproverName,
        Guid secondApproverId, string secondApproverName)
    {
        return new ApprovalFlow(new[]
        {
            new ApprovalFlowStep(1, firstApproverId, firstApproverName, "Manager"),
            new ApprovalFlowStep(2, secondApproverId, secondApproverName, "Director")
        });
    }

    /// <summary>
    /// 3段階承認フロー（例: 50万円以上）
    /// </summary>
    public static ApprovalFlow ThreeStep(
        Guid firstApproverId, string firstApproverName,
        Guid secondApproverId, string secondApproverName,
        Guid thirdApproverId, string thirdApproverName)
    {
        return new ApprovalFlow(new[]
        {
            new ApprovalFlowStep(1, firstApproverId, firstApproverName, "Manager"),
            new ApprovalFlowStep(2, secondApproverId, secondApproverName, "Director"),
            new ApprovalFlowStep(3, thirdApproverId, thirdApproverName, "Executive")
        });
    }
}

/// <summary>
/// 承認フローのステップ定義
/// </summary>
public record ApprovalFlowStep(
    int StepNumber,
    Guid ApproverId,
    string ApproverName,
    string ApproverRole
);
