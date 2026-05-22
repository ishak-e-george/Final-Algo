# RescueFlow - Safety-Critical Dispatch and Case Management

RescueFlow is an automated web-based rescue case management and response planning system running the **SC-RCMDA (Safety-Critical Rescue Case Management and Dispatch Algorithm)**. The system dynamically classifies reported cases, computes a multi-factor severity rating, estimates ETAs using geographic calculations, matches the most eligible response vehicles and specialized hospitals, runs verification rules via a hard-constraint Safety Validator, and executes transactional resource allocations.

## Architecture

- **Backend**: ASP.NET Core MVC (C#, .NET 8)
- **Database**: SQL Server / LocalDB (via Entity Framework Core)
- **Frontend**: Custom premium glassmorphic Razor views + Bootstrap
- **Testing**: xUnit

## System Setup and Execution

### Prerequisites

- .NET 8.0 SDK
- LocalDB (`(localdb)\MSSQLLocalDB`)
- Entity Framework Core Tools (`dotnet ef`)

### Running the System

1. **Clean and Reset the Database:**
   ```powershell
   cd C:\Users\HP\Desktop\Algorithm\RescueFlow\RescueFlow.Web
   dotnet ef database drop --force
   dotnet ef database update
   ```

2. **Run the Application:**
   ```powershell
   dotnet run --urls "http://localhost:5000"
   ```

3. **Verify the Dashboard:**
   Open [http://localhost:5000](http://localhost:5000) in your web browser.

### Running Tests

#### Automated xUnit Tests:
```powershell
cd C:\Users\HP\Desktop\Algorithm\RescueFlow
dotnet test RescueFlow.Tests\RescueFlow.Tests.csproj
```

#### Manual Smoke Test Suite:
To execute the multi-case scenario integration tests:
```powershell
powershell -ExecutionPolicy Bypass -File C:\Users\HP\Desktop\Algorithm\RescueFlow\manual-smoke-test.ps1
```
The smoke test script exits with code `0` if all test cases meet their expected validation status, or `1` if any expectation fails.
