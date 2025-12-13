#!/usr/bin/env python3
"""
Catalog Binding Validator

パターン ID の妥当性を検証するスクリプト。
manifests/**/*.yaml や plan.md に記載された pattern_id が
catalog/index.json や catalog/patterns/*.yaml に実在するかをチェックする。

Usage:
    python scripts/validate_catalog_binding.py [--verbose]

Exit codes:
    0: All pattern IDs are valid
    1: Unknown pattern IDs found
"""

import json
import re
import sys
from pathlib import Path


def load_valid_pattern_ids(catalog_dir: Path) -> set[str]:
    """
    catalog/index.json と catalog/patterns/*.yaml から有効なパターン ID を収集。
    """
    ids = set()

    # index.json から ID を収集
    index_path = catalog_dir / "index.json"
    if index_path.exists():
        try:
            data = json.loads(index_path.read_text(encoding="utf-8"))
            # Top-level patterns 配列から id を抽出（正本）
            for p in data.get("patterns", []):
                pid = p.get("id")
                if pid:
                    ids.add(pid)

            # categories 配下にも patterns がある場合は収集（後方互換）
            for category in (data.get("categories") or {}).values():
                for p in category.get("patterns", []) if isinstance(category, dict) else []:
                    pid = p.get("id")
                    if pid:
                        ids.add(pid)
        except (json.JSONDecodeError, KeyError) as e:
            print(f"[WARN] Failed to parse {index_path}: {e}", file=sys.stderr)

    # patterns/**/*.yaml から id を収集（バックアップ）
    patterns_dir = catalog_dir / "patterns"
    if patterns_dir.exists():
        for yaml_file in patterns_dir.rglob("*.yaml"):
            # ファイル名から ID を推測（xxx.yaml -> xxx）
            # 実際の YAML パース無しで簡易的に取得
            stem = yaml_file.stem
            ids.add(stem)

            # YAML 内の id: フィールドも確認
            try:
                content = yaml_file.read_text(encoding="utf-8")
                match = re.search(r'^id:\s*["\']?([a-z0-9-]+)["\']?\s*$', content, re.MULTILINE)
                if match:
                    ids.add(match.group(1))
            except Exception:
                pass

    return ids


def collect_used_pattern_ids_from_manifests(manifests_dir: Path) -> list[tuple[Path, str]]:
    """
    manifests/**/*.yaml から使用されている pattern_id を収集。
    """
    ids = []
    if not manifests_dir.exists():
        return ids

    for manifest in manifests_dir.rglob("*.yaml"):
        try:
            content = manifest.read_text(encoding="utf-8")
            # from_catalog のリスト要素のみを対象（catalog.id 等のメタ情報は除外）
            # 例: - id: feature-create-entity
            for match in re.finditer(r'^\s*-\s*id:\s*["\']?([a-z0-9-]+)["\']?\s*$', content, re.MULTILINE):
                ids.append((manifest, match.group(1)))

            # supplemental_guidance / creative_boundary で使われる pattern_id/provider も収集
            for match in re.finditer(r'^\s*pattern_id:\s*["\']?([a-z0-9-]+)["\']?\s*$', content, re.MULTILINE):
                ids.append((manifest, match.group(1)))
            for match in re.finditer(r'^\s*provider:\s*["\']?([a-z0-9-]+)["\']?\s*$', content, re.MULTILINE):
                ids.append((manifest, match.group(1)))

            # from_catalog セクションも確認
            for match in re.finditer(r'from_catalog:\s*\[([^\]]+)\]', content):
                items = match.group(1).split(',')
                for item in items:
                    item = item.strip().strip('"').strip("'")
                    if item:
                        ids.append((manifest, item))
        except Exception as e:
            print(f"[WARN] Failed to read {manifest}: {e}", file=sys.stderr)

    return ids


def collect_used_pattern_ids_from_plans(specs_dir: Path) -> list[tuple[Path, str]]:
    """
    specs/**/plan.md の Catalog Binding セクションから pattern_id を収集。
    """
    ids = []
    if not specs_dir.exists():
        return ids

    for plan_file in specs_dir.rglob("plan.md"):
        try:
            content = plan_file.read_text(encoding="utf-8")
            # Catalog Binding テーブルから ID を抽出
            # 例: | 機能作成 | feature-create-entity | matched |
            for match in re.finditer(r'\|\s*[^|]+\s*\|\s*([a-z0-9-]+)\s*\|\s*(matched|auto-applied|selected)', content, re.IGNORECASE):
                ids.append((plan_file, match.group(1)))
        except Exception as e:
            print(f"[WARN] Failed to read {plan_file}: {e}", file=sys.stderr)

    return ids


def main():
    verbose = "--verbose" in sys.argv or "-v" in sys.argv

    # プロジェクトルートを特定
    script_dir = Path(__file__).parent
    project_root = script_dir.parent

    catalog_dir = project_root / "catalog"
    manifests_dir = project_root / "manifests"
    specs_dir = project_root / "specs"

    # 有効な pattern ID を収集
    valid_ids = load_valid_pattern_ids(catalog_dir)

    if verbose:
        print(f"[INFO] Found {len(valid_ids)} valid pattern IDs in catalog")
        for pid in sorted(valid_ids):
            print(f"       - {pid}")
        print()

    # 使用されている pattern ID を収集
    used_ids = []
    used_ids.extend(collect_used_pattern_ids_from_manifests(manifests_dir))
    used_ids.extend(collect_used_pattern_ids_from_plans(specs_dir))

    if verbose:
        print(f"[INFO] Found {len(used_ids)} pattern ID references")
        print()

    # 検証
    errors = []
    for source_file, pid in used_ids:
        if pid not in valid_ids:
            errors.append((source_file, pid))

    if errors:
        print("[ERROR] Unknown pattern IDs found:")
        for source_file, pid in errors:
            rel_path = source_file.relative_to(project_root) if source_file.is_relative_to(project_root) else source_file
            print(f"  - '{pid}' in {rel_path}")
        print()
        print("Available pattern IDs:")
        for pid in sorted(valid_ids)[:20]:
            print(f"  - {pid}")
        if len(valid_ids) > 20:
            print(f"  ... and {len(valid_ids) - 20} more")
        sys.exit(1)
    else:
        if used_ids:
            print(f"[OK] All {len(used_ids)} pattern ID references are valid.")
        else:
            print("[OK] No pattern ID references found to validate.")
        sys.exit(0)


if __name__ == "__main__":
    main()
