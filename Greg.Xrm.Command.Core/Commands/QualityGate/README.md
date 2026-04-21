# Quality Gate Command

Parses pac solution check results and fails CI builds on high severity issues.

## Commands

- `quality gate` — Parse solution check results, fail on High severity

## Usage

```bash
# Parse solution check results from a ZIP file
pacx quality gate --input SolutionCheckerResults.zip --fail-on High

# Parse from a directory
pacx quality gate --input ./SolutionCheckerResults --fail-on Error

# Output as JSON for CI/CD integration
pacx quality gate --input ./results --format json
```

## Options

| Option | Alias | Description | Default |
|--------|-------|-------------|--------|
| `--input` | `-i` | Path to solution check result ZIP or directory | Auto-detect |
| `--fail-on` | | Minimum severity to fail: Error, High, Medium, Low | High |
| `--format` | `-f` | Output format: table, json | table |
| `--solution` | `-s` | Run solution check on this solution first | |

## Examples

### Basic Usage

```bash
# Fail build on Error or High severity issues
pacx quality gate -i solution-check.zip --fail-on High

# Strict mode - fail on any issue
pacx quality gate -i results/ --fail-on Medium
```

### CI/CD Integration

```yaml
# Azure Pipelines example
- task: Bash@3
  displayName: 'Run Quality Gate'
  inputs:
    targetType: inline
    script: |
      pacx quality gate --input $(Build.ArtifactStagingDirectory)/solution-check.zip --fail-on High
```

## Exit Codes

- `0` — Quality gate passed
- `1` — Quality gate failed (issues found at or above threshold)