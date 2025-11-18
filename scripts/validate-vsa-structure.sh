#!/bin/bash

# VSA Structure Validation Script
# Purpose: Detect Clean Architecture / Layered Architecture patterns
# Expected: Vertical Slice Architecture only

set -e

echo "========================================="
echo "VSA Structure Validation"
echo "========================================="
echo ""

ERRORS=0
WARNINGS=0

# Color codes
RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
NC='\033[0m' # No Color

# Check 1: src/ direct children should NOT have layer names
echo "[Check 1] Checking for layer-based projects in src/..."
LAYER_PROJECTS=$(ls src/ 2>/dev/null | grep -E "\.(Application|Domain|Infrastructure|Web|UI|Core|Api)$" || true)

if [ -n "$LAYER_PROJECTS" ]; then
    echo -e "${RED}❌ FAIL: Layer-based projects found in src/${NC}"
    echo ""
    echo "Found:"
    echo "$LAYER_PROJECTS" | sed 's/^/  - /'
    echo ""
    echo "This is Clean Architecture / Layered Architecture, NOT VSA."
    echo ""
    echo "Expected structure:"
    echo "  src/"
    echo "  └── {BoundedContext}/"
    echo "      └── Features/"
    echo ""
    ERRORS=$((ERRORS + 1))
else
    echo -e "${GREEN}✅ PASS: No layer-based projects in src/${NC}"
fi
echo ""

# Check 2: BC (Bounded Context) folder should exist
echo "[Check 2] Checking for Bounded Context folder..."
BC_FOLDERS=$(find src/ -maxdepth 1 -mindepth 1 -type d 2>/dev/null | wc -l)

if [ "$BC_FOLDERS" -eq 0 ]; then
    echo -e "${RED}❌ FAIL: No Bounded Context folder found in src/${NC}"
    echo ""
    echo "Expected at least one folder like:"
    echo "  src/ProductCatalog/"
    echo "  src/OrderManagement/"
    echo ""
    ERRORS=$((ERRORS + 1))
else
    echo -e "${GREEN}✅ PASS: Bounded Context folder(s) found${NC}"
    find src/ -maxdepth 1 -mindepth 1 -type d | sed 's/^/  - /'
fi
echo ""

# Check 3: Features/ folder should exist under BC
echo "[Check 3] Checking for Features/ folder under BC..."
FEATURES_FOLDERS=$(find src/*/Features -type d 2>/dev/null || true)

if [ -z "$FEATURES_FOLDERS" ]; then
    echo -e "${RED}❌ FAIL: No Features/ folder found under Bounded Context${NC}"
    echo ""
    echo "Expected structure:"
    echo "  src/"
    echo "  └── ProductCatalog/"
    echo "      └── Features/  ← This folder is missing"
    echo ""
    ERRORS=$((ERRORS + 1))
else
    echo -e "${GREEN}✅ PASS: Features/ folder found${NC}"
    echo "$FEATURES_FOLDERS" | sed 's/^/  - /'
fi
echo ""

# Check 4: Feature folders should contain layer subfolders
echo "[Check 4] Checking feature slice structure..."
FEATURE_SLICES=$(find src/*/Features/* -maxdepth 0 -type d 2>/dev/null || true)

if [ -z "$FEATURE_SLICES" ]; then
    echo -e "${YELLOW}⚠️  WARNING: No feature slices found yet${NC}"
    WARNINGS=$((WARNINGS + 1))
else
    echo "Found feature slices:"
    echo "$FEATURE_SLICES" | sed 's/^/  - /'
    echo ""

    # Check each feature slice for layer folders
    for SLICE in $FEATURE_SLICES; do
        SLICE_NAME=$(basename "$SLICE")
        HAS_LAYERS=$(find "$SLICE" -maxdepth 1 -type d \( -name "Application" -o -name "Domain" -o -name "Infrastructure" -o -name "UI" \) 2>/dev/null | wc -l)

        if [ "$HAS_LAYERS" -eq 0 ]; then
            echo -e "${YELLOW}⚠️  WARNING: $SLICE_NAME has no layer folders${NC}"
            echo "    Expected: Application/, Domain/, Infrastructure/, UI/"
            WARNINGS=$((WARNINGS + 1))
        else
            echo -e "${GREEN}✅ $SLICE_NAME has layer folders${NC}"
        fi
    done
fi
echo ""

# Check 5: Verify no cross-feature dependencies (future implementation)
echo "[Check 5] Cross-feature dependency check..."
echo -e "${YELLOW}ℹ️  TODO: Implement cross-feature dependency detection${NC}"
echo ""

# Summary
echo "========================================="
echo "Validation Summary"
echo "========================================="
echo ""

if [ $ERRORS -gt 0 ]; then
    echo -e "${RED}❌ FAILED with $ERRORS error(s)${NC}"
    echo ""
    echo "Action required:"
    echo "1. Review the errors above"
    echo "2. Refer to: docs/architecture/VSA-STRICT-RULES.md"
    echo "3. Restructure the project to VSA"
    echo ""
    exit 1
elif [ $WARNINGS -gt 0 ]; then
    echo -e "${YELLOW}⚠️  PASSED with $WARNINGS warning(s)${NC}"
    echo ""
    echo "Review warnings above and ensure VSA structure is correct."
    echo ""
    exit 0
else
    echo -e "${GREEN}✅ ALL CHECKS PASSED${NC}"
    echo ""
    echo "Project structure conforms to Vertical Slice Architecture."
    echo ""
    exit 0
fi
