# 入力要件（Input Requirements）

UI 強化スキルを適用する際に必要な入力ファイルを定義する。

---

## 必須入力

### 1. .razor ファイル

対象となる Razor コンポーネントファイル。

**読み取る情報:**

| 情報 | 目的 |
|------|------|
| フォーム項目 | どの入力フィールドがあるか |
| テーブル構造 | 一覧表示のカラム構成 |
| ボタン配置 | アクションボタンの種類と位置 |
| Command/Query 呼び出し | どの Command/Query を使用しているか |
| Store/Actions 参照 | 状態管理との連携方法 |

**例:**

```razor
@page "/products/create"
@inject CreateProductStore Store
@inject CreateProductActions Actions

<EditForm Model="@model" OnValidSubmit="@HandleSubmit">
    <input type="text" @bind="model.Name" />
    <button type="submit" disabled="@isSubmitting">作成</button>
</EditForm>
```

---

### 2. Command / Query

対象画面で使用される Command または Query。

**読み取る情報:**

| 情報 | 目的 |
|------|------|
| プロパティ | フォーム項目との対応 |
| 戻り値の型 | 成功時の処理方法 |
| バリデーション属性 | クライアント側バリデーションのヒント |

**Command の例:**

```csharp
public sealed record CreateProductCommand(
    string Name,
    string? Description,
    decimal Price,
    int Stock
) : ICommand<Result<Guid>>;
```

**Query の例:**

```csharp
public sealed record GetProductsQuery(
    string? SearchTerm,
    int Page,
    int PageSize
) : IQuery<Result<PagedResult<ProductDto>>>;
```

---

## 条件付き必須

### 3. Domain Entity（CanXxx() がある場合）

操作可否の判定ロジックを持つ Entity。

**必要な条件:**

- UI に操作ボタン（編集、削除、承認など）がある
- 操作可否がビジネスルールで決まる
- ボタンの活性/非活性を動的に制御したい

**読み取る情報:**

| 情報 | 目的 |
|------|------|
| CanXxx() メソッド | 操作可否の判定ロジック |
| 戻り値の型 | BoundaryDecision / bool / Result 等 |
| Reason プロパティ | 不許可理由の取得方法 |

**例:**

```csharp
public class Loan : AggregateRoot<LoanId>
{
    public BoundaryDecision CanExtend(bool hasReservations)
    {
        if (Status != LoanStatus.Active)
            return BoundaryDecision.Deny("貸出中ではありません");

        if (ExtensionCount >= MaxExtensions)
            return BoundaryDecision.Deny("延長回数の上限に達しています");

        if (hasReservations)
            return BoundaryDecision.Deny("予約が入っているため延長できません");

        return BoundaryDecision.Allow();
    }
}
```

---

## 任意入力

### 4. Validator

FluentValidation の Validator クラス。

**必要な条件:**

- クライアント側でバリデーションメッセージを表示したい
- サーバーサイドと同じメッセージを使いたい

**読み取る情報:**

| 情報 | 目的 |
|------|------|
| RuleFor 定義 | バリデーションルール |
| WithMessage | エラーメッセージ |
| 条件付きルール | 動的なバリデーション |

**例:**

```csharp
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("商品名は必須です")
            .MaximumLength(100).WithMessage("商品名は100文字以内で入力してください");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("価格は0より大きい値を入力してください");
    }
}
```

---

## 入力の優先度

| 優先度 | ファイル | 状況 |
|:------:|----------|------|
| 1 | .razor | 常に必須 |
| 2 | Command/Query | 常に必須 |
| 2.5 | UI-IR | 存在する場合（優先参照） |
| 3 | Domain Entity | CanXxx() がある場合 |
| 4 | Validator | バリデーション強化時 |

---

## 優先度 2.5: UI-IR（オプション）

UI-IR が存在する場合、以下を優先参照する。

**ファイルパス**: `specs/{feature}/{slice}.ui-ir.yaml`

### UI-IR から読み取る情報

| UI-IR セクション | 用途 |
|-----------------|------|
| component_mapping | フィールドタイプ → MudBlazor 自動選択 |
| main_actions.disabled_when | CanXxx() 参照パターン |
| main_actions.confirmation_level | ダイアログ要否判定 |
| main_actions.is_destructive | danger_overrides 適用 |
| main_actions.priority | ボタン Variant/Color 決定 |

### UI-IR がある場合の処理順序

1. UI-IR の component_mapping を適用（フィールドタイプ → MudBlazor）
2. main_actions から confirmation_level を取得
3. disabled_when から CanXxx() 連携を生成
4. is_destructive: true のアクションに danger_overrides 適用
5. priority に基づいてボタンスタイルを決定

### UI-IR 参照例

```yaml
# specs/product/create.ui-ir.yaml
main_actions:
  - id: "act-create"
    name: "作成"
    priority: "Primary"
    frequency: "High"
    confirmation_level: "None"
    component_hint: 'MudButton(Variant="Filled", Color="Primary")'

  - id: "act-cancel"
    name: "キャンセル"
    priority: "Secondary"
    is_destructive: false
    confirmation_level: "None"
    component_hint: 'MudButton(Variant="Outlined")'
```

**UI-IR がない場合**: 優先度 3 以降のファイルから情報を取得

---

## ファイル検索パス

入力ファイルは以下のパスから検索する：

```
src/Application/Features/{Feature}/
├── {Feature}.razor           ← 対象 .razor
├── {Feature}Command.cs       ← Command
├── {Feature}Query.cs         ← Query
└── {Feature}CommandValidator.cs  ← Validator

src/Domain/{BoundedContext}/{Aggregate}/
└── {Entity}.cs               ← Domain Entity（CanXxx()）
```

---

## 入力チェックリスト

```
□ .razor ファイルのパスは正しいか？
□ Command/Query は特定できたか？
□ CanXxx() メソッドを持つ Entity はあるか？
□ Validator は存在するか？（任意）
```
