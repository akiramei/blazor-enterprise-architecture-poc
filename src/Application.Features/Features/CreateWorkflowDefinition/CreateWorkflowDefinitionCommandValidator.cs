using FluentValidation;

namespace Application.Features.CreateWorkflowDefinition;

/// <summary>
/// ワークフロー定義作成コマンドバリデーター
/// </summary>
public sealed class CreateWorkflowDefinitionCommandValidator
    : AbstractValidator<CreateWorkflowDefinitionCommand>
{
    public CreateWorkflowDefinitionCommandValidator()
    {
        RuleFor(x => x.ApplicationType)
            .IsInEnum()
            .WithMessage("有効な申請タイプを指定してください");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("ワークフロー名は必須です")
            .MaximumLength(200)
            .WithMessage("ワークフロー名は200文字以内で入力してください");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("説明は2000文字以内で入力してください");

        RuleFor(x => x.Steps)
            .NotEmpty()
            .WithMessage("最低1つの承認ステップが必要です");

        RuleForEach(x => x.Steps).ChildRules(step =>
        {
            step.RuleFor(s => s.Role)
                .NotEmpty()
                .WithMessage("ロールは必須です")
                .MaximumLength(100)
                .WithMessage("ロールは100文字以内で入力してください");

            step.RuleFor(s => s.Name)
                .MaximumLength(200)
                .WithMessage("ステップ名は200文字以内で入力してください");
        });
    }
}
