# AI Decision Flowchart

このドキュメントは、AIがパターンを選択するための具体的なアルゴリズムを提供します。

---

## 🎯 基本方針

1. **Feature Slice 優先**: 迷ったら Feature Slice を選択
2. **明示的な判断**: ai_decision_matrix と ai_selection_hints を活用
3. **信頼度チェック**: confidence > 0.8 で採用

---

## 📊 ステップ1: ユーザー要求の分類

```python
import re
from enum import Enum
from typing import List, Dict, Optional

class RequestType(Enum):
    NEW_FEATURE = "新機能追加"
    CROSS_CUTTING = "システム全体の機能追加"
    MODIFICATION = "既存ファイルの修正"
    LEARNING = "パターン理解・学習"

class PatternSelector:
    def __init__(self, catalog_index: dict):
        self.catalog = catalog_index
        self.decision_matrix = catalog_index["ai_decision_matrix"]["user_intent_to_pattern"]

    def classify_request(self, user_request: str) -> tuple[RequestType, float]:
        """
        ユーザーの要求を分類し、信頼度を返す

        Returns:
            (RequestType, confidence)
        """
        scores = {}

        # Empty check: if decision_matrix is empty, return a default
        if not self.decision_matrix:
            # Return a default intent with low confidence
            return RequestType.NEW_FEATURE, 0.0

        # 各カテゴリのスコアを計算
        for intent, config in self.decision_matrix.items():
            score = 0.0
            trigger_keywords = config["trigger_keywords"]

            if not trigger_keywords:
                scores[intent] = (0.0, config["confidence"])
                continue

            # 重複を避けるため、マッチしたキーワードを記録
            matched_keywords = set()

            # キーワードマッチング（単語境界を考慮、大文字小文字を区別しない）
            for keyword in trigger_keywords:
                # 単語境界を使った正規表現マッチング
                pattern = r'\b' + re.escape(keyword) + r'\b'
                if re.search(pattern, user_request, re.IGNORECASE) and keyword.lower() not in matched_keywords:
                    matched_keywords.add(keyword.lower())

            # スコアを計算（マッチ数 / 全キーワード数）、1.0を上限とする
            score = min(len(matched_keywords) / len(trigger_keywords), 1.0)
            scores[intent] = (score, config["confidence"])

        # Empty check: if no scores were computed, return a default
        if not scores:
            return RequestType.NEW_FEATURE, 0.0

        # 最もスコアが高いものを選択
        best_intent = max(scores.items(), key=lambda x: x[1][0] * x[1][1])
        intent_name = best_intent[0]
        confidence = best_intent[1][0] * best_intent[1][1]

        # RequestTypeにマッピング (名前または値で検索)
        request_type = self._resolve_request_type(intent_name)

        return request_type, confidence

    def _resolve_request_type(self, intent_name: str) -> RequestType:
        """
        intent_nameから適切なRequestTypeを解決する。

        名前（例: "NEW_FEATURE"）または値（例: "新機能追加"）のどちらでも検索可能。
        一致するものがない場合はデフォルトでNEW_FEATUREを返す。

        Args:
            intent_name: 検索する文字列（enum名またはenum値）

        Returns:
            RequestType: 解決されたRequestType
        """
        # 1. 名前で検索を試行
        try:
            return RequestType[intent_name]
        except KeyError:
            pass

        # 2. 値で検索を試行
        for member in RequestType:
            if member.value == intent_name:
                return member

        # 3. 一致なし: デフォルトとしてNEW_FEATUREを返す
        print(f"Warning: '{intent_name}'に一致するRequestTypeが見つかりません。NEW_FEATUREをデフォルトとして使用します。")
        return RequestType.NEW_FEATURE

# 使用例
user_request = "商品を作成する機能を追加してください"
selector = PatternSelector(catalog_index)
request_type, confidence = selector.classify_request(user_request)
# => (RequestType.NEW_FEATURE, 0.95)
```

---

## 📊 ステップ2: パターンの選択

```python
class Pattern:
    def __init__(self, pattern_data: dict):
        self.id = pattern_data["id"]
        self.name = pattern_data["name"]
        self.category = pattern_data["category"]
        self.intent = pattern_data["intent"]
        self.file = pattern_data["file"]

def select_pattern(
    request_type: RequestType,
    user_request: str,
    catalog: dict
) -> Optional[Pattern]:
    """
    RequestTypeとユーザー要求から適切なパターンを選択
    """

    # カテゴリを決定
    category_map = {
        RequestType.NEW_FEATURE: "feature-slice",
        RequestType.CROSS_CUTTING: "pipeline-behavior",
        RequestType.MODIFICATION: "layer-element",
        RequestType.LEARNING: "layer-element"
    }

    target_category = category_map[request_type]

    # 該当カテゴリのパターンを取得
    candidates = [
        Pattern(p) for p in catalog["patterns"]
        if p["category"] == target_category
    ]

    # Feature Sliceの場合、CRUD操作を判定
    if request_type == RequestType.NEW_FEATURE:
        operation = detect_operation(user_request)
        candidates = [
            p for p in candidates
            if operation in p.id or operation in str(p.intent)
        ]

    # 最初の候補を返す（通常は1つに絞られる）
    return candidates[0] if candidates else None

def detect_operation(user_request: str) -> str:
    """
    CRUD操作を検出
    """
    operations = {
        "create": ["作成", "登録", "追加", "新規"],
        "search": ["検索", "一覧", "表示", "フィルタ"],
        "update": ["更新", "編集", "変更"],
        "delete": ["削除", "消去"]
    }

    for op, keywords in operations.items():
        if any(kw in user_request for kw in keywords):
            return op

    return "create"  # デフォルト

# 使用例
pattern = select_pattern(RequestType.NEW_FEATURE, user_request, catalog)
# => Pattern(id="feature-create-entity", ...)
```

---

## 📊 ステップ3: パターンYAMLの読み込みと検証

```python
import yaml

def load_and_validate_pattern(
    pattern: Pattern,
    user_request: str,
    catalog_base_path: str
) -> tuple[dict, float]:
    """
    パターンのYAMLファイルを読み込み、ai_selection_hintsで検証

    Returns:
        (pattern_yaml, confidence)
    """

    # YAMLファイルを読み込む
    yaml_path = f"{catalog_base_path}/{pattern.file}"
    with open(yaml_path, 'r', encoding='utf-8') as f:
        pattern_yaml = yaml.safe_load(f)

    # ai_selection_hintsが存在するか確認
    if "ai_selection_hints" not in pattern_yaml:
        return pattern_yaml, 0.7  # デフォルト信頼度

    hints = pattern_yaml["ai_selection_hints"]
    confidence = calculate_confidence(user_request, hints)

    return pattern_yaml, confidence

def calculate_confidence(user_request: str, hints: dict) -> float:
    """
    ai_selection_hintsを使って信頼度を計算
    """
    confidence = 0.5  # ベース信頼度

    # trigger_phrasesのチェック
    if "trigger_phrases" in hints:
        for phrase in hints["trigger_phrases"]:
            if phrase in user_request:
                confidence += 0.2
                break

    # confidence_keywordsのチェック
    if "confidence_keywords" in hints:
        for level, keywords in hints["confidence_keywords"].items():
            if any(kw in user_request for kw in keywords):
                if level == "high":
                    confidence += 0.3
                elif level == "medium":
                    confidence += 0.2
                elif level == "low":
                    confidence += 0.1
                break

    # anti_patternsのチェック
    if "anti_patterns" in hints:
        for anti in hints["anti_patterns"]:
            if anti in user_request:
                confidence -= 0.3
                break

    return min(max(confidence, 0.0), 1.0)  # 0.0～1.0に正規化

# 使用例
pattern_yaml, confidence = load_and_validate_pattern(
    pattern,
    user_request,
    "catalog"
)
# => (pattern_yaml, 0.95)
```

---

## 📊 ステップ4: 最終決定

```python
def make_final_decision(
    pattern: Pattern,
    pattern_yaml: dict,
    confidence: float,
    threshold: float = 0.8
) -> dict:
    """
    最終的なパターン選択の決定を行う

    Returns:
        {
            "pattern": Pattern,
            "pattern_yaml": dict,
            "confidence": float,
            "should_confirm": bool,
            "reason": str
        }
    """

    should_confirm = confidence < threshold

    if should_confirm:
        reason = f"信頼度が閾値（{threshold}）を下回っています。ユーザーに確認してください。"
    else:
        reason = f"高い信頼度（{confidence:.2f}）でパターンを選択しました。"

    return {
        "pattern": pattern,
        "pattern_yaml": pattern_yaml,
        "confidence": confidence,
        "should_confirm": should_confirm,
        "reason": reason
    }

# 使用例
decision = make_final_decision(pattern, pattern_yaml, confidence)

if decision["should_confirm"]:
    print(f"確認が必要: {decision['reason']}")
    # ユーザーに確認ダイアログを表示
else:
    print(f"パターン決定: {decision['pattern'].name}")
    # コード生成に進む
```

---

## 🎯 完全なフローの例

```python
def select_pattern_complete(
    user_request: str,
    catalog_index: dict,
    catalog_base_path: str
) -> dict:
    """
    完全なパターン選択フロー
    """

    # ステップ1: 要求を分類
    selector = PatternSelector(catalog_index)
    request_type, initial_confidence = selector.classify_request(user_request)

    print(f"Step 1: 要求タイプ = {request_type.value}, 信頼度 = {initial_confidence:.2f}")

    # ステップ2: パターンを選択
    pattern = select_pattern(request_type, user_request, catalog_index)

    if pattern is None:
        return {
            "error": "適切なパターンが見つかりませんでした",
            "fallback": "feature-slice"
        }

    print(f"Step 2: パターン = {pattern.name}")

    # ステップ3: YAMLを読み込み、検証
    pattern_yaml, yaml_confidence = load_and_validate_pattern(
        pattern,
        user_request,
        catalog_base_path
    )

    print(f"Step 3: YAML信頼度 = {yaml_confidence:.2f}")

    # 総合信頼度を計算
    total_confidence = (initial_confidence + yaml_confidence) / 2

    # ステップ4: 最終決定
    decision = make_final_decision(pattern, pattern_yaml, total_confidence)

    print(f"Step 4: 最終信頼度 = {total_confidence:.2f}")
    print(f"Step 4: {decision['reason']}")

    return decision

# 実行例
user_request = "商品を作成する機能を追加してください"
decision = select_pattern_complete(
    user_request,
    catalog_index,
    "catalog"
)

# 出力:
# Step 1: 要求タイプ = 新機能追加, 信頼度 = 0.95
# Step 2: パターン = Create Entity Feature Slice
# Step 3: YAML信頼度 = 0.95
# Step 4: 最終信頼度 = 0.95
# Step 4: 高い信頼度（0.95）でパターンを選択しました。
```

---

## 📊 エッジケースの処理

### ケース1: 信頼度が低い場合

```python
user_request = "何か作りたい"
decision = select_pattern_complete(user_request, catalog_index, "catalog")

# 信頼度が低いため、ユーザーに確認
if decision["should_confirm"]:
    print("質問: 具体的にどのような機能を作成したいですか？")
    print("1. 商品の作成")
    print("2. 検索機能")
    print("3. その他")
```

### ケース2: 複数の候補がある場合

```python
user_request = "商品を管理する機能を追加してください"

# "管理" は曖昧なので、複数の候補を提示
candidates = [
    "feature-create-entity (作成)",
    "feature-search-entity (検索)",
    "feature-update-entity (更新)",
    "feature-delete-entity (削除)"
]

print("以下のいずれかを選択してください:")
for i, c in enumerate(candidates, 1):
    print(f"{i}. {c}")
```

### ケース3: フォールバック戦略

```python
if decision.get("error"):
    print(f"エラー: {decision['error']}")
    print(f"フォールバック: {decision['fallback']} を使用します")

    # feature-sliceをデフォルトで使用
    pattern = find_pattern_by_id(catalog_index, "feature-create-entity")
```

---

## 🎯 実装チェックリスト

AIがパターン選択を実装する際のチェックリスト:

- [ ] catalog/index.json を読み込む
- [ ] ai_decision_matrix を参照する
- [ ] ユーザー要求を分類する (RequestType)
- [ ] 該当カテゴリのパターンを検索する
- [ ] パターンのYAMLファイルを読み込む
- [ ] ai_selection_hints で信頼度を計算する
- [ ] confidence > 0.8 なら採用
- [ ] confidence < 0.8 ならユーザーに確認
- [ ] エビデンスのファイルパスを提示

---

**最終更新: 2025-11-19**
**カタログバージョン: v2025.11.19**
