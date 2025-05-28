.PHONY: build lint format test report check-format-tool check-report-tool

# Ensure `dotnet-format` is installed.
check-format-tool:
	@dotnet tool list -g | grep dotnet-format || dotnet tool install -g dotnet-format

# Ensure `dotnet-reportgenerator-globaltool` is installed.
check-report-tool:
	@dotnet tool list -g | grep dotnet-reportgenerator-globaltool || dotnet tool install -g dotnet-reportgenerator-globaltool

# Build the project
build:
	dotnet restore src/Bucket.Sdk.sln
	dotnet build src/Bucket.Sdk.sln --configuration Release

# Check formatting and code style
lint: check-format-tool
	dotnet format src/Bucket.Sdk.sln --verify-no-changes --verbosity diagnostic
	dotnet format style --verify-no-changes --verbosity diagnostic src/Bucket.Sdk.sln
	dotnet format analyzers --verify-no-changes --verbosity diagnostic src/Bucket.Sdk.sln

# Apply formatting to the entire project.
format: check-format-tool
	dotnet format src/Bucket.Sdk.sln
	dotnet format style src/Bucket.Sdk.sln
	dotnet format analyzers src/Bucket.Sdk.sln

# Run tests.
test: build check-report-tool
	dotnet test src/Bucket.Sdk.sln --configuration Release --verbosity normal --blame-hang --blame-hang-timeout 30000 --collect:"Code Coverage;Format=cobertura" --logger "trx;LogFileName=test-results.trx"
	reportgenerator -reports:"**/TestResults/**/*.cobertura.xml" -targetdir:"coveragereport" -reporttypes:"HtmlInline;Cobertura;Badges;MarkdownSummary"
	@echo "Test report generated in coveragereport directory"
	@[ -f coveragereport/Summary.md ] && cat coveragereport/Summary.md || echo "Summary not found"

# Generate report from existing test results.
report: check-report-tool
	reportgenerator -reports:"**/TestResults/**/*.cobertura.xml" -targetdir:"coveragereport" -reporttypes:"HtmlInline;Cobertura;Badges;MarkdownSummary"
	@echo "Report generated in coveragereport directory"
	@[ -f coveragereport/Summary.md ] && cat coveragereport/Summary.md || echo "Summary not found"
