#!/usr/bin/env sh
# Runs every test project with coverage and fails the build when the layers that hold the business
# rules (Domain, Application) are below 100% line OR branch coverage.
# Infrastructure/Api (integration tests, real MySQL) are measured and reported but not gated.
# Requires: dotnet SDK 9 and TEST_MYSQL_CONNECTION (see README).
set -eu
cd "$(dirname "$0")/.."

dotnet tool restore >/dev/null
rm -rf coverage

# %2c is the MSBuild escape for a comma inside a property value.
COMMON="-p:CollectCoverage=true -p:CoverletOutputFormat=cobertura -p:ExcludeByFile=**/*.g.cs%2c**/Migrations/*.cs"

gate() { # <test project> <assembly>  -> fails below 100% (line and branch)
  echo "==> $2 (gate: 100% line + branch)"
  dotnet test "tests/DddHexagonal.$1.Tests" $COMMON "-p:Include=[DddHexagonal.$2]*" \
    -p:Threshold=100 "-p:ThresholdType=line%2cbranch" "-p:CoverletOutput=$PWD/coverage/$2/"
}

report() { # <test project> <include filter> <out dir>  -> measured, not gated
  echo "==> $3 (reported only)"
  dotnet test "tests/DddHexagonal.$1.Tests" $COMMON "-p:Include=$2" "-p:CoverletOutput=$PWD/coverage/$3/"
}

gate Domain Domain
gate Application Application
report Infrastructure "[DddHexagonal.Infrastructure]*" Infrastructure
# The end-to-end tests also exercise the DI wiring of Infrastructure; reportgenerator merges both runs.
report Api "[DddHexagonal.Api]*%2c[DddHexagonal.Infrastructure]*" Api

echo "==> Architecture tests"
dotnet test tests/DddHexagonal.Architecture.Tests

dotnet reportgenerator "-reports:coverage/*/coverage.cobertura.xml" "-targetdir:coverage/report" \
  "-reporttypes:Html;TextSummary;MarkdownSummaryGithub;JsonSummary" >/dev/null
cat coverage/report/Summary.txt
