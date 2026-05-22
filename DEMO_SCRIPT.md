# RescueFlow - Demo Script

This script walks through demonstrating the 6 core scenarios designed to highlight the Safety-Critical Rescue Case Management and Dispatch Algorithm (SC-RCMDA) features in the user interface.

## Prerequisites

Reset the database and start the application:
```powershell
cd C:\Users\HP\Desktop\Algorithm\RescueFlow\RescueFlow.Web
dotnet ef database drop --force
dotnet ef database update
dotnet run --urls "http://localhost:5000"
```

Open a web browser and navigate to [http://localhost:5000](http://localhost:5000).

---

## Scenario 1: Minor Injury (Normal Flow - No Hospital Required)

Demonstrates a standard case that does not require hospital transport.

1. Click **Report New Case** on the sidebar.
2. Fill out the form:
   - **Reporter Name**: John Smith
   - **Reporter Phone**: 555-0101
   - **Location**: Central Park Zoo Entrance
   - **Coordinates**: Lat `40.7300`, Lng `-74.0010`
   - **Category**: Medical
   - **Primary Injury**: Minor Injury
   - **Patients**: 1
   - **Description**: Scraped knee and slight ankle pain after falling near zoo gate.
3. Click **Submit Case**.
4. **Expected Result**: 
   - Redirects to Case Details.
   - Status badge shows **AutoAssigned**.
   - Severity Score: **1**.
   - The response plan specifies **1 Ambulance** assigned, and **Requires Hospital: False** (no hospital selected).
   - Review the *Audit Log* and *Dispatch Decision Log* at the bottom to see the step-by-step logic.

---

## Scenario 2: Heart Attack (Specialized Hospital Capability Match)

Demonstrates capability-based hospital matching (requires ICU/Cardiac) and proximity selection.

1. Click **Report New Case**.
2. Fill out the form:
   - **Reporter Name**: Alice Cooper
   - **Reporter Phone**: 555-0202
   - **Location**: Corporate Office, 5th Floor
   - **Coordinates**: Lat `40.7350`, Lng `-74.0150`
   - **Category**: Medical
   - **Primary Injury**: Heart Attack
   - **Patients**: 1
   - Check **Breathing Problem** and **Unconscious**.
   - **Description**: Elderly male experiencing chest pain, gasping for breath, unconscious.
3. Click **Submit Case**.
4. **Expected Result**:
   - Status badge shows **AutoAssigned**.
   - Severity Score: **4** (elevated from 3 because of life threats).
   - Resources Assigned: **1 Ambulance** (from nearest station).
   - Hospital Selected: **City General** (or nearest hospital with ICU and Cardiac specialties).
   - Hospital ICU load is incremented by 1.

---

## Scenario 3: Trapped Victim (Multi-Resource Dispatch & Release on Closure)

Demonstrates matching multiple vehicle types (Ambulance + Fire Engine + Heavy Rescue) and releasing resources back to service on case closure.

1. Click **Report New Case**.
2. Fill out the form:
   - **Reporter Name**: Officer Davis
   - **Reporter Phone**: 555-0303
   - **Location**: Interstate 95, Exit 4
   - **Coordinates**: Lat `40.7420`, Lng `-74.0180`
   - **Category**: Traffic Accident
   - **Primary Injury**: Multiple Injuries
   - **Patients**: 2
   - Check **Severe Bleeding** and **Trapped Victim**.
   - **Description**: Sedan crash. Victim trapped with heavy bleeding.
3. Click **Submit Case**.
4. **Expected Result**:
   - Status badge shows **AutoAssigned**.
   - Severity Score: **4**.
   - Resources Assigned: **1 Ambulance** + **1 Fire Engine** + **1 Heavy Rescue Vehicle** (satisfying entrapment & fire safety requirements).
   - Hospital Selected: **Trauma Medical Center** (matching Trauma capabilities).
5. Click **Close Case** on the Details page.
6. **Expected Result**:
   - Status changes to **Closed**.
   - All assigned vehicles return to **Available** status (check *Vehicles* menu).
   - Hospital Trauma Load decrements back to its original state (check *Hospitals* menu).

---

## Scenario 4: Chemical Hazard (HazMat Equipment Requirement)

Demonstrates specialized HazMat vehicle matching with equipment checks (Gas Detector, Chemical Suit).

1. Click **Report New Case**.
2. Fill out the form:
   - **Reporter Name**: Supervisor Jenkins
   - **Reporter Phone**: 555-0404
   - **Location**: Apex Chem Warehouse B
   - **Coordinates**: Lat `40.7550`, Lng `-73.9850`
   - **Category**: Industrial Accident
   - **Primary Injury**: Chemical Exposure
   - **Patients**: 3
   - Check **Chemical Risk** and **Breathing Problem**.
   - **Description**: Ammonia vapor cloud leak from valve.
3. Click **Submit Case**.
4. **Expected Result**:
   - Status badge shows **AutoAssigned**.
   - Severity Score: **4**.
   - Resources Assigned: **1 Ambulance** + **1 HazMat Vehicle** (specifically equipped with Chemical Suits/Gas Detectors).
   - Hospital Selected: **Industrial Toxicology Hospital** (nearest toxicology specialist).

---

## Scenario 5: Missing Resource (Safety Validator Blocking Dispatch)

Demonstrates the Safety Validator blocking dispatch and transitioning the case to **BlockedNoSafePlan** when required resources are unavailable.

1. Navigate to **Vehicles** in the navigation bar.
2. Find the Heavy Rescue Vehicle (**RESCUE-01**) and click **Set to Maintenance** (or use the status update button).
3. Verify its status is now **Maintenance**.
4. Navigate to **Report New Case** and report a trapped victim:
   - **Reporter Name**: Wayne Gretzky
   - **Reporter Phone**: 555-0505
   - **Location**: Industrial Site construction shaft
   - **Coordinates**: Lat `40.7480`, Lng `-74.0120`
   - **Category**: Search and Rescue
   - **Primary Injury**: Fracture
   - **Patients**: 1
   - Check **Trapped Victim**.
   - **Description**: Pinned worker. Heavy Rescue required.
5. Click **Submit Case**.
6. **Expected Result**:
   - The Safety Validator detects that no Available Heavy Rescue vehicle exists with Hydraulic Cutters.
   - Status badge shows **BlockedNoSafePlan**.
   - Review the *Safety Violations* section to see the explicit message: `"Safety Violation: Missing required vehicle count for HeavyRescueVehicle."`

---

## Scenario 6: Invalid Input (Input Validation & Escalation)

Demonstrates boundary input verification which bypasses the algorithm and flags the case as **Escalated** for manual oversight.

1. Click **Report New Case**.
2. Fill out the form:
   - **Reporter Name**: Anxious Caller
   - **Reporter Phone**: 555-9999
   - **Location**: Empty Lot
   - **Coordinates**: Lat `40.7100`, Lng `-74.0200`
   - **Category**: Industrial Accident
   - **Primary Injury**: None
   - **Patients**: 0 (Invalid - must be >= 1)
   - Check **Chemical Risk**.
   - **Description**: (Leave completely blank - violates chemical description rule).
3. Click **Submit Case**.
4. **Expected Result**:
   - Status badge shows **Escalated**.
   - The *Decision Log* lists the validation failures: `"Input validation failed: Invalid patient count (must be >= 1).; Incomplete input: HazMat risk reported but no chemical type or description specified."`
