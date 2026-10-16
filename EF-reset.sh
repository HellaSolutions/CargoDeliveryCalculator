# 1. Drop the dev database (removes the old schema and its migration history)
dotnet ef database drop --force --project CargoDeliveryCalculator

# 2. Delete the existing migrations, including the model snapshot
Remove-Item -Recurse -Force CargoDeliveryCalculator\Migrations

# 3. Scaffold a new initial migration from the current model
dotnet ef migrations add InitialCreate --project CargoDeliveryCalculator

# 4. Recreate the database (or just `dotnet run`, which migrates automatically in Development)
#dotnet ef database update --project CargoDeliveryCalculator

#To keep your existing migration and add a new one on top for your schema change, run this from the solution root:
#dotnet ef migrations add SchemaChange --project CargoDeliveryCalculator
