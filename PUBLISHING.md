# DbReactor NuGet Publishing Guide

This guide explains how to publish DbReactor packages to NuGet using GitHub Actions.

## Prerequisites

### 1. NuGet API Key
1. Go to [nuget.org](https://www.nuget.org) and sign in
2. Go to Account Settings → API Keys
3. Create a new API Key with:
   - **Key Name**: `DbReactor GitHub Actions`
   - **Select Scopes**: `Push new packages and package versions`
   - **Select Packages**: `*` (or select specific packages once they exist)
   - **Glob Pattern**: `DbReactor.*`

### 2. GitHub Repository Secrets
Add the following secret to your GitHub repository:
1. Go to `https://github.com/rmconvery/DbReactor/settings/secrets/actions`
2. Click "New repository secret"
3. Name: `NUGET_API_KEY`
4. Value: Your NuGet API key from step 1

## Publishing Methods

### Method 1: Manual Workflow Dispatch
This is the recommended method for controlled releases:

1. Go to `https://github.com/rmconvery/DbReactor/actions`
2. Select "Publish to NuGet" workflow
3. Click "Run workflow"
4. Fill in:
   - **Version**: e.g., `1.0.0`, `1.1.0`, `2.0.0-beta1`
   - **Is prerelease**: Check if this is a beta/alpha/rc version
5. Click "Run workflow"

The workflow will:
- Run all tests
- Build both packages
- Update version numbers
- Publish to NuGet
- Create a GitHub release
- Upload package files as release assets

### Method 2: GitHub Release (Automatic)
When you create a GitHub release, packages are automatically published:

1. Go to `https://github.com/rmconvery/DbReactor/releases`
2. Click "Create a new release"
3. Create a tag: `v1.0.0` (must start with `v`)
4. Fill in release title and description
5. Mark as prerelease if needed
6. Publish release

The workflow automatically triggers and publishes packages.

## Version Management

### Version Format
- **Stable releases**: `1.0.0`, `1.1.0`, `2.0.0`
- **Prereleases**: `1.0.0-beta1`, `1.0.0-rc1`, `2.0.0-alpha1`

### Semantic Versioning Guidelines
- **Major version** (1.x.x → 2.x.x): Breaking changes to public API
- **Minor version** (1.0.x → 1.1.x): New features, backward compatible
- **Patch version** (1.0.0 → 1.0.1): Bug fixes, backward compatible

### Updating Base Version
To change the base version in project files:

1. Edit `DbReactor.Core/DbReactor.Core.csproj`:
   ```xml
   <Version>1.1.0</Version>
   <AssemblyVersion>1.1.0.0</AssemblyVersion>
   <FileVersion>1.1.0.0</FileVersion>
   ```

2. Edit `DbReactor.MSSqlServer/DbReactor.MSSqlServer.csproj`:
   ```xml
   <Version>1.1.0</Version>
   <AssemblyVersion>1.1.0.0</AssemblyVersion>
   <FileVersion>1.1.0.0</FileVersion>
   ```

Note: The GitHub Actions workflow will override these versions during publishing.

## Package Dependencies

The workflows automatically handle dependencies:
- `DbReactor.MSSqlServer` depends on `DbReactor.Core`
- Both packages are published with the same version number
- NuGet dependency resolution ensures compatibility

## Continuous Integration

The CI workflow (`ci.yml`) runs on every push and PR:
- Builds all projects
- Runs all tests
- Creates CI packages with version `1.0.0-ci-{build-number}`
- Validates packages using `dotnet-validate`

## Troubleshooting

### Common Issues

**1. "Package already exists" error**
- Use `--skip-duplicate` flag (already included in workflow)
- Or increment the version number

**2. "Invalid API key" error**
- Check that `NUGET_API_KEY` secret is correctly set
- Verify API key has proper permissions
- Ensure API key hasn't expired

**3. Tests failing**
- Check the CI workflow first
- Fix failing tests before publishing
- Both packages must pass all tests to publish

**4. Missing files in package**
- Check that README.md and LICENSE files exist in repo root
- Verify embedded resources are properly marked in .csproj files

### Viewing Published Packages
- DbReactor.Core: `https://www.nuget.org/packages/DbReactor.Core/`
- DbReactor.MSSqlServer: `https://www.nuget.org/packages/DbReactor.MSSqlServer/`

### Manual Local Testing
Before publishing, you can test package creation locally:

```bash
# Build and pack both projects
dotnet build --configuration Release
dotnet pack DbReactor.Core/DbReactor.Core.csproj --configuration Release --output ./nupkg
dotnet pack DbReactor.MSSqlServer/DbReactor.MSSqlServer.csproj --configuration Release --output ./nupkg

# Test packages locally
dotnet add package DbReactor.Core --source ./nupkg --version 1.0.0
```

## Release Checklist

Before publishing a new version:

- [ ] All tests pass locally and in CI
- [ ] Documentation is updated
- [ ] CHANGELOG.md is updated (if you have one)
- [ ] Version numbers are appropriate for the changes
- [ ] Dependencies are correct and compatible
- [ ] README examples work with the new version
- [ ] Breaking changes are documented

## Publishing History

Track your published versions here:

| Version | Date | Type | Notes |
|---------|------|------|-------|
| 1.0.0   | TBD  | Release | Initial release |

---

For more information about NuGet packaging, see the [official NuGet documentation](https://docs.microsoft.com/en-us/nuget/).