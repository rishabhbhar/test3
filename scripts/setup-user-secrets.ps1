

try {
	Write-Host "Generating secure JWT secret..."
	$bytes = New-Object byte[] 32
	[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
	$secret = [Convert]::ToBase64String($bytes)

	$projects = @(
		"src\AuthService",
		"src\InventoryService",
		"src\OrderService"
	)

	foreach ($proj in $projects) {
		if (-not (Test-Path $proj)) {
			Write-Warning "Project path not found: $proj — skipping"
			continue
		}

		Push-Location $proj
		Write-Host "Configuring user-secrets for $proj..."

		# Initialize user-secrets for the project (idempotent)
		dotnet user-secrets init | Out-Null

		# Set the Jwt values (secret is kept in secrets store)
		dotnet user-secrets set "Jwt:Secret" $secret | Out-Null
		dotnet user-secrets set "Jwt:Issuer" "microservices-auth" | Out-Null
		dotnet user-secrets set "Jwt:Audience" "microservices-clients" | Out-Null
		dotnet user-secrets set "Jwt:ExpiryMinutes" "60" | Out-Null

		Pop-Location
	}

	Write-Host "Done. User-secrets configured for projects:"
	$projects | ForEach-Object { Write-Host " - $_" }
	Write-Host "Note: The Jwt:Secret value was generated and NOT printed to the console for security."
}
catch {
	Write-Error "An error occurred: $_"
	exit 1
}
