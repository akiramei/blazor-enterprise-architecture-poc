# Integration Tests Configuration Guide

## Overview

This document explains how to configure and run integration tests securely without committing sensitive credentials to source control.

## Test Credentials Configuration

### Configuration Priority

Test credentials are loaded in the following order (highest to lowest priority):

1. **Environment Variables** (highest priority)
2. **appsettings.Test.Local.json** (local development, not committed)
3. **appsettings.Test.json** (default test values, committed)
4. **appsettings.json** (base configuration)

### Required Environment Variables

The following environment variables can be used to override test credentials:

- `TEST_ADMIN_EMAIL` - Administrator user email (default: admin@example.com)
- `TEST_ADMIN_PASSWORD` - Administrator user password (default: Admin@123)
- `TEST_USER_EMAIL` - Regular user email (default: user@example.com)
- `TEST_USER_PASSWORD` - Regular user password (default: User@123)

## Local Development Setup

### Option 1: Using Environment Variables

Set environment variables before running tests:

**Linux/macOS (Bash/Zsh):**
```bash
export TEST_ADMIN_EMAIL="admin@example.com"
export TEST_ADMIN_PASSWORD="Admin@123"
export TEST_USER_EMAIL="user@example.com"
export TEST_USER_PASSWORD="User@123"

# Run tests
dotnet test
```

**Windows (PowerShell):**
```powershell
$env:TEST_ADMIN_EMAIL="admin@example.com"
$env:TEST_ADMIN_PASSWORD="Admin@123"
$env:TEST_USER_EMAIL="user@example.com"
$env:TEST_USER_PASSWORD="User@123"

# Run tests
dotnet test
```

**Windows (Command Prompt):**
```cmd
set TEST_ADMIN_EMAIL=admin@example.com
set TEST_ADMIN_PASSWORD=Admin@123
set TEST_USER_EMAIL=user@example.com
set TEST_USER_PASSWORD=User@123

rem Run tests
dotnet test
```

### Option 2: Using appsettings.Test.Local.json

Create a local configuration file that will NOT be committed to source control:

**tests/ProductCatalog.Web.IntegrationTests/appsettings.Test.Local.json:**
```json
{
  "TestCredentials": {
    "AdminEmail": "admin@example.com",
    "AdminPassword": "Admin@123",
    "UserEmail": "user@example.com",
    "UserPassword": "User@123"
  }
}
```

**tests/PurchaseManagement.Web.IntegrationTests/appsettings.Test.Local.json:**
```json
{
  "TestCredentials": {
    "AdminEmail": "admin@example.com",
    "AdminPassword": "Admin@123",
    "UserEmail": "user@example.com",
    "UserPassword": "User@123"
  }
}
```

⚠️ **IMPORTANT:** `appsettings.Test.Local.json` is listed in `.gitignore` and will NOT be committed to source control.

## CI/CD Pipeline Configuration

### GitHub Actions

Add the test credentials as repository secrets:

1. Go to repository **Settings** → **Secrets and variables** → **Actions**
2. Add the following secrets:
   - `TEST_ADMIN_EMAIL`
   - `TEST_ADMIN_PASSWORD`
   - `TEST_USER_EMAIL`
   - `TEST_USER_PASSWORD`

3. Update your workflow file (`.github/workflows/test.yml`):

```yaml
name: Run Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'

    - name: Run Tests
      env:
        TEST_ADMIN_EMAIL: ${{ secrets.TEST_ADMIN_EMAIL }}
        TEST_ADMIN_PASSWORD: ${{ secrets.TEST_ADMIN_PASSWORD }}
        TEST_USER_EMAIL: ${{ secrets.TEST_USER_EMAIL }}
        TEST_USER_PASSWORD: ${{ secrets.TEST_USER_PASSWORD }}
      run: dotnet test --configuration Release
```

### Azure DevOps

1. Go to **Pipelines** → **Library** → **Variable groups**
2. Create a variable group named `Test-Credentials`
3. Add the following variables (mark as secret):
   - `TEST_ADMIN_EMAIL`
   - `TEST_ADMIN_PASSWORD`
   - `TEST_USER_EMAIL`
   - `TEST_USER_PASSWORD`

4. Update your `azure-pipelines.yml`:

```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

variables:
  - group: Test-Credentials

steps:
- task: DotNetCoreCLI@2
  displayName: 'Run Tests'
  inputs:
    command: 'test'
    projects: '**/*Tests.csproj'
  env:
    TEST_ADMIN_EMAIL: $(TEST_ADMIN_EMAIL)
    TEST_ADMIN_PASSWORD: $(TEST_ADMIN_PASSWORD)
    TEST_USER_EMAIL: $(TEST_USER_EMAIL)
    TEST_USER_PASSWORD: $(TEST_USER_PASSWORD)
```

### Jenkins

1. Install the **Credentials Binding Plugin**
2. Add credentials as **Secret text** type:
   - `test-admin-email`
   - `test-admin-password`
   - `test-user-email`
   - `test-user-password`

3. Configure your Jenkinsfile:

```groovy
pipeline {
    agent any

    stages {
        stage('Test') {
            environment {
                TEST_ADMIN_EMAIL = credentials('test-admin-email')
                TEST_ADMIN_PASSWORD = credentials('test-admin-password')
                TEST_USER_EMAIL = credentials('test-user-email')
                TEST_USER_PASSWORD = credentials('test-user-password')
            }
            steps {
                sh 'dotnet test --configuration Release'
            }
        }
    }
}
```

## Default Test Users

The integration tests expect the following test users to exist (created by `IdentityDataSeeder`):

| Email | Password | Role | Tenant |
|-------|----------|------|--------|
| admin@example.com | Admin@123 | Admin | Tenant A |
| user@example.com | User@123 | User | Tenant A |
| userb@example.com | UserB@123 | User | Tenant B |

## Security Best Practices

### ✅ DO

- Use environment variables for CI/CD pipelines
- Use `appsettings.Test.Local.json` for local development (not committed)
- Rotate test credentials regularly
- Use different credentials for different environments
- Validate that credentials are loaded before running tests

### ❌ DON'T

- Commit real passwords to source control
- Use production credentials in tests
- Share credentials in plain text (Slack, email, etc.)
- Hardcode credentials directly in test files

## Troubleshooting

### Error: "Test credentials are not configured properly"

This error occurs when required credentials are missing. Verify that:

1. Environment variables are set correctly
2. Or `appsettings.Test.Local.json` exists with valid credentials
3. Or `appsettings.Test.json` contains default values

### Tests fail with "Unauthorized" error

Verify that:

1. The credentials match the users created by `IdentityDataSeeder`
2. The `IdentityDataSeeder` is being called during test initialization
3. The database is properly initialized

## Test Project Structure

```
tests/
├── ProductCatalog.Web.IntegrationTests/
│   ├── appsettings.json                   # Base configuration (optional)
│   ├── appsettings.Test.json              # Default test values (committed)
│   ├── appsettings.Test.Local.json        # Local overrides (NOT committed)
│   ├── TestConfiguration.cs               # Configuration helper
│   └── *.cs                               # Test files
│
├── PurchaseManagement.Web.IntegrationTests/
│   ├── appsettings.json                   # Base configuration (optional)
│   ├── appsettings.Test.json              # Default test values (committed)
│   ├── appsettings.Test.Local.json        # Local overrides (NOT committed)
│   ├── TestConfiguration.cs               # Configuration helper
│   └── *.cs                               # Test files
│
└── README.md                              # This file
```

## Additional Resources

- [ASP.NET Core Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Managing Secrets in Development](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [GitHub Actions Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [Azure DevOps Variable Groups](https://docs.microsoft.com/en-us/azure/devops/pipelines/library/variable-groups)
