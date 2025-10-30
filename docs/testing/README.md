# ContextSensor Testing Documentation

Welcome to the ContextSensor testing documentation. This directory contains comprehensive guides for testing all aspects of the application.

## 📚 Documentation Index

| Document | Purpose | Audience |
|----------|---------|----------|
| **[GUIDE.md](GUIDE.md)** | Main testing guide covering installer, integration, and manual testing | Everyone |
| **[PERFORMANCE_TESTING.md](PERFORMANCE_TESTING.md)** | Performance benchmarks, resource monitoring, optimization | Developers |
| **[UPDATER_TESTING.md](UPDATER_TESTING.md)** | Comprehensive auto-update testing procedures | Developers/QA |
| **[UPDATER_TESTING_QUICKSTART.md](UPDATER_TESTING_QUICKSTART.md)** | Quick start guide for updater testing | Everyone ⭐ |
| **[TEST_RESULTS.md](TEST_RESULTS.md)** | Latest validation results and metrics | Everyone |

## 🚀 Quick Start

### For Developers: Run Automated Tests

```powershell
# From repository root - run ALL tests
dotnet test -c Release

# Just performance tests
dotnet test test/ContextSensor.PerformanceTests -c Release

# Just updater tests  
dotnet test test/ContextSensor.UpdaterTests -c Release

# Run performance benchmarks
cd test/ContextSensor.PerformanceTests
dotnet run -c Release
```

**Results**: ✅ 55/55 tests passing

### For QA: Test Real Updater Service

```powershell
# Prerequisite: ContextSensor must be installed
cd test

# Run end-to-end updater test
.\Test-UpdaterEndToEnd.ps1
```

See **[UPDATER_TESTING_QUICKSTART.md](UPDATER_TESTING_QUICKSTART.md)** for details.

## 📊 Test Coverage Overview

### Current Test Status

| Test Category | Tests | Status | Documentation |
|---------------|-------|--------|---------------|
| **Updater Unit Tests** | 42 | ✅ All Pass | [UPDATER_TESTING.md](UPDATER_TESTING.md) |
| **Performance Tests** | 13 | ✅ All Pass | [PERFORMANCE_TESTING.md](PERFORMANCE_TESTING.md) |
| **Performance Benchmarks** | 7 | ✅ Working | [PERFORMANCE_TESTING.md](PERFORMANCE_TESTING.md) |
| **Installer Tests** | Manual | 📋 Documented | [GUIDE.md](GUIDE.md) |
| **Total Automated** | **55** | **✅ 100%** | - |

### Test Projects Location

```
test/
├── ContextSensor.PerformanceTests/     # 13 tests + 7 benchmarks
├── ContextSensor.UpdaterTests/         # 42 tests
├── UpdateServer/                        # Test HTTP server
└── Test-UpdaterEndToEnd.ps1            # E2E updater test script
```

## 🎯 Testing Approaches

### Approach 1: Automated Tests (Fast & Continuous)

**Best for**:
- CI/CD pipelines
- Quick validation
- Logic verification
- Regression testing

**How to run**:
```powershell
dotnet test -c Release
```

**Time**: ~40 seconds for all tests

---

### Approach 2: End-to-End Testing (Real-World)

**Best for**:
- Pre-release validation
- Updater service testing
- Full workflow verification
- Integration validation

**How to run**:
See guides:
- [UPDATER_TESTING_QUICKSTART.md](UPDATER_TESTING_QUICKSTART.md) - Updater E2E testing
- [GUIDE.md](GUIDE.md) - Installer testing

**Time**: ~10-30 minutes depending on tests

---

## 📖 Documentation Guide

### Start Here

**New to testing ContextSensor?**  
→ Start with **[GUIDE.md](GUIDE.md)** for overview

**Want to test performance?**  
→ See **[PERFORMANCE_TESTING.md](PERFORMANCE_TESTING.md)**

**Want to test auto-updates?**  
→ Start with **[UPDATER_TESTING_QUICKSTART.md](UPDATER_TESTING_QUICKSTART.md)** ⭐

**Need detailed updater info?**  
→ See **[UPDATER_TESTING.md](UPDATER_TESTING.md)**

**Want to see test results?**  
→ Check **[TEST_RESULTS.md](TEST_RESULTS.md)**

### Document Descriptions

#### GUIDE.md (Main Testing Guide)
- 📋 Installer testing procedures
- 🔧 Manual test scenarios
- ✅ Verification procedures
- 🐛 Common issues and solutions
- 📊 Test reporting templates

**Updated sections**:
- Overview of all test categories
- Quick reference for test projects
- Running all tests instructions
- CI/CD integration examples

#### PERFORMANCE_TESTING.md
- ⚡ Performance benchmarks and targets
- 💾 Resource usage monitoring
- 📈 Analyzing results
- 🔍 Troubleshooting performance issues
- 💡 Optimization best practices

**Coverage**:
- Event processing throughput
- Memory and CPU monitoring
- Resource leak detection
- Latency measurements

#### UPDATER_TESTING.md
- 🔄 Comprehensive updater testing
- 📦 Manifest testing procedures
- 🧪 Service validation tests
- 🌐 Integration test scenarios
- 🛠️ Manual testing workflows

**Coverage**:
- Manifest parsing
- Version comparison
- Download verification
- Installation workflow

#### UPDATER_TESTING_QUICKSTART.md ⭐
- 🚀 Quick start guide
- 📝 Step-by-step instructions
- 🔧 Manual and automated approaches
- 🧹 Cleanup procedures

**This is the best starting point for updater testing!**

#### TEST_RESULTS.md
- 📊 Latest test run results
- ✅ Pass/fail status
- 📈 Performance metrics
- 💡 Recommendations

---

## 🎓 Common Testing Workflows

### Pre-Commit Testing

```powershell
# Quick validation before commit
dotnet test -c Release
```

**Time**: ~40 seconds  
**Coverage**: All automated tests

### Pre-Release Validation

```powershell
# 1. Run all automated tests
dotnet test -c Release

# 2. Run performance benchmarks
cd test/ContextSensor.PerformanceTests
dotnet run -c Release

# 3. Test updater end-to-end (if installed)
cd ../../test
.\Test-UpdaterEndToEnd.ps1

# 4. Manual installer testing
# See GUIDE.md for procedures
```

**Time**: ~1-2 hours  
**Coverage**: Complete validation

### Continuous Integration

```yaml
# Example CI pipeline
steps:
  - task: DotNetCoreCLI@2
    displayName: 'Run All Tests'
    inputs:
      command: 'test'
      arguments: '-c Release --logger trx'

  - task: PublishTestResults@2
    inputs:
      testResultsFormat: 'VSTest'
      testResultsFiles: '**/*.trx'
```

---

## 📁 Directory Structure

```
docs/testing/
├── README.md                             # This file - Documentation index
├── GUIDE.md                              # Main testing guide (1207 lines)
├── PERFORMANCE_TESTING.md                # Performance testing (544 lines)
├── UPDATER_TESTING.md                    # Updater testing (728 lines)
├── UPDATER_TESTING_QUICKSTART.md         # Quick start guide (308 lines) ⭐
└── TEST_RESULTS.md                       # Validation results (161 lines)

Total: 2,948 lines of testing documentation
```

---

## 🔗 Related Resources

### Test Projects
- [`test/ContextSensor.PerformanceTests/`](../../test/ContextSensor.PerformanceTests/) - Performance tests
- [`test/ContextSensor.UpdaterTests/`](../../test/ContextSensor.UpdaterTests/) - Updater tests
- [`test/`](../../test/) - Test scripts and utilities

### Other Documentation
- [Installer Guide](../installer/GUIDE.md) - Building and deploying installers
- [Update Manifest Reference](../installer/UPDATE_MANIFEST_REFERENCE.md) - Manifest format
- [Configuration Guide](../CONFIGURATION.md) - Application configuration
- [Design Documentation](../DESIGN.md) - Architecture and design

---

## 💡 Tips

1. **Start with automated tests** - They're fast and don't require installation
2. **Use end-to-end tests** before releases - They catch integration issues
3. **Monitor baselines** - Track performance metrics over time
4. **Document failures** - Capture logs and steps to reproduce
5. **Test on clean VMs** - Avoid environment contamination

---

## 🆘 Getting Help

**Tests failing?**
1. Check [GUIDE.md](GUIDE.md) - Common Test Issues section
2. Review logs in `C:\ProgramData\ContextSensor\logs\`
3. Check specific guide for your test category

**Performance issues?**
1. See [PERFORMANCE_TESTING.md](PERFORMANCE_TESTING.md) - Troubleshooting section
2. Run benchmarks to identify bottlenecks
3. Compare against baseline metrics

**Updater issues?**
1. Start with [UPDATER_TESTING_QUICKSTART.md](UPDATER_TESTING_QUICKSTART.md)
2. Check [UPDATER_TESTING.md](UPDATER_TESTING.md) for detailed scenarios
3. Verify test server is accessible

---

**Last Updated**: 2025-11-01  
**Version**: 1.0.0