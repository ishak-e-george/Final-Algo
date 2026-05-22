using System;
using System.Collections.Generic;
using System.Linq;
using RescueFlow.Web.Data;
using RescueFlow.Web.Models.Entities;
using RescueFlow.Web.Models.Enums;

namespace RescueFlow.Web.Data;

public static class SeedData
{
    public static void Initialize(RescueFlowDbContext context)
    {
        // Check if DB is already seeded
        if (context.Stations.Any() || context.Hospitals.Any() || context.Equipment.Any())
        {
            return; // DB already seeded
        }

        // 1. Add Equipment
        var equipment = new Dictionary<string, Equipment>
        {
            { "Trauma Bag", new Equipment { Name = "Trauma Bag" } },
            { "AED/Monitor", new Equipment { Name = "AED/Monitor" } },
            { "Hydraulic Cutter", new Equipment { Name = "Hydraulic Cutter" } },
            { "Fire Hose", new Equipment { Name = "Fire Hose" } },
            { "Gas Detector", new Equipment { Name = "Gas Detector" } },
            { "Chemical Suit", new Equipment { Name = "Chemical Suit" } },
            { "HazMat Kit", new Equipment { Name = "HazMat Kit" } }
        };

        foreach (var eq in equipment.Values)
        {
            context.Equipment.Add(eq);
        }
        context.SaveChanges();

        // 2. Add Stations
        var centralEms = new Station
        {
            Name = "Central EMS Station",
            Latitude = 40.7128,
            Longitude = -74.0060,
            IsActive = true
        };

        var centralFire = new Station
        {
            Name = "Central Fire Station",
            Latitude = 40.7250,
            Longitude = -74.0100,
            IsActive = true
        };

        var industrialRescue = new Station
        {
            Name = "Industrial Rescue Station",
            Latitude = 40.7500,
            Longitude = -73.9900,
            IsActive = true
        };

        context.Stations.AddRange(centralEms, centralFire, industrialRescue);
        context.SaveChanges();

        // 3. Add Vehicles
        var vehicles = new List<RescueVehicle>
        {
            // Central EMS Vehicles
            new RescueVehicle
            {
                Code = "EMS-01",
                StationId = centralEms.Id,
                VehicleType = VehicleType.Ambulance,
                Status = VehicleStatus.Available,
                MaxCrewCapacity = 4,
                CurrentCrewCount = 2,
                CurrentLatitude = 40.7128,
                CurrentLongitude = -74.0060
            },
            new RescueVehicle
            {
                Code = "EMS-02",
                StationId = centralEms.Id,
                VehicleType = VehicleType.Ambulance,
                Status = VehicleStatus.Available,
                MaxCrewCapacity = 4,
                CurrentCrewCount = 2,
                CurrentLatitude = 40.7128,
                CurrentLongitude = -74.0060
            },

            // Central Fire Vehicles
            new RescueVehicle
            {
                Code = "FIRE-01",
                StationId = centralFire.Id,
                VehicleType = VehicleType.FireEngine,
                Status = VehicleStatus.Available,
                MaxCrewCapacity = 6,
                CurrentCrewCount = 4,
                CurrentLatitude = 40.7250,
                CurrentLongitude = -74.0100
            },
            new RescueVehicle
            {
                Code = "RESCUE-01",
                StationId = centralFire.Id,
                VehicleType = VehicleType.HeavyRescueVehicle,
                Status = VehicleStatus.Available,
                MaxCrewCapacity = 5,
                CurrentCrewCount = 3,
                CurrentLatitude = 40.7250,
                CurrentLongitude = -74.0100
            },

            // Industrial Rescue Vehicles
            new RescueVehicle
            {
                Code = "HAZMAT-01",
                StationId = industrialRescue.Id,
                VehicleType = VehicleType.HazMatVehicle,
                Status = VehicleStatus.Available,
                MaxCrewCapacity = 5,
                CurrentCrewCount = 3,
                CurrentLatitude = 40.7500,
                CurrentLongitude = -73.9900
            }
        };

        foreach (var v in vehicles)
        {
            context.RescueVehicles.Add(v);
        }
        context.SaveChanges();

        // 4. Link Vehicles and Equipment
        var ems01 = vehicles.First(v => v.Code == "EMS-01");
        var ems02 = vehicles.First(v => v.Code == "EMS-02");
        var fire01 = vehicles.First(v => v.Code == "FIRE-01");
        var rescue01 = vehicles.First(v => v.Code == "RESCUE-01");
        var hazmat01 = vehicles.First(v => v.Code == "HAZMAT-01");

        context.VehicleEquipment.AddRange(
            new VehicleEquipment { RescueVehicleId = ems01.Id, EquipmentId = equipment["Trauma Bag"].Id },
            new VehicleEquipment { RescueVehicleId = ems01.Id, EquipmentId = equipment["AED/Monitor"].Id },

            new VehicleEquipment { RescueVehicleId = ems02.Id, EquipmentId = equipment["Trauma Bag"].Id },
            new VehicleEquipment { RescueVehicleId = ems02.Id, EquipmentId = equipment["AED/Monitor"].Id },

            new VehicleEquipment { RescueVehicleId = fire01.Id, EquipmentId = equipment["Trauma Bag"].Id },
            new VehicleEquipment { RescueVehicleId = fire01.Id, EquipmentId = equipment["Fire Hose"].Id },

            new VehicleEquipment { RescueVehicleId = rescue01.Id, EquipmentId = equipment["Trauma Bag"].Id },
            new VehicleEquipment { RescueVehicleId = rescue01.Id, EquipmentId = equipment["Hydraulic Cutter"].Id },

            new VehicleEquipment { RescueVehicleId = hazmat01.Id, EquipmentId = equipment["Trauma Bag"].Id },
            new VehicleEquipment { RescueVehicleId = hazmat01.Id, EquipmentId = equipment["Gas Detector"].Id },
            new VehicleEquipment { RescueVehicleId = hazmat01.Id, EquipmentId = equipment["Chemical Suit"].Id },
            new VehicleEquipment { RescueVehicleId = hazmat01.Id, EquipmentId = equipment["HazMat Kit"].Id }
        );
        context.SaveChanges();

        // 5. Add Hospitals & Capacities
        var cityGeneral = new Hospital
        {
            Name = "City General Hospital",
            Latitude = 40.7300,
            Longitude = -74.0000,
            IsActive = true
        };

        var traumaMedical = new Hospital
        {
            Name = "Trauma Medical Center",
            Latitude = 40.7400,
            Longitude = -74.0200,
            IsActive = true
        };

        var indToxicology = new Hospital
        {
            Name = "Industrial Toxicology Hospital",
            Latitude = 40.7600,
            Longitude = -73.9800,
            IsActive = true
        };

        context.Hospitals.AddRange(cityGeneral, traumaMedical, indToxicology);
        context.SaveChanges();

        // Add Capabilities
        context.HospitalCapabilities.AddRange(
            new HospitalCapabilityEntity { HospitalId = cityGeneral.Id, Capability = HospitalCapability.GeneralEmergency },
            new HospitalCapabilityEntity { HospitalId = cityGeneral.Id, Capability = HospitalCapability.ICU },

            new HospitalCapabilityEntity { HospitalId = traumaMedical.Id, Capability = HospitalCapability.GeneralEmergency },
            new HospitalCapabilityEntity { HospitalId = traumaMedical.Id, Capability = HospitalCapability.Trauma },
            new HospitalCapabilityEntity { HospitalId = traumaMedical.Id, Capability = HospitalCapability.ICU },

            new HospitalCapabilityEntity { HospitalId = indToxicology.Id, Capability = HospitalCapability.GeneralEmergency },
            new HospitalCapabilityEntity { HospitalId = indToxicology.Id, Capability = HospitalCapability.Toxicology },
            new HospitalCapabilityEntity { HospitalId = indToxicology.Id, Capability = HospitalCapability.ICU },
            new HospitalCapabilityEntity { HospitalId = indToxicology.Id, Capability = HospitalCapability.BurnUnit }
        );

        // Add Capacities
        context.HospitalCapacities.AddRange(
            new HospitalCapacity
            {
                HospitalId = cityGeneral.Id,
                EmergencyCapacity = 10,
                CurrentEmergencyLoad = 0,
                IcuCapacity = 5,
                CurrentIcuLoad = 0,
                TraumaCapacity = 0,
                CurrentTraumaLoad = 0,
                BurnCapacity = 0,
                CurrentBurnLoad = 0,
                ToxicologyCapacity = 0,
                CurrentToxicologyLoad = 0
            },
            new HospitalCapacity
            {
                HospitalId = traumaMedical.Id,
                EmergencyCapacity = 15,
                CurrentEmergencyLoad = 0,
                IcuCapacity = 6,
                CurrentIcuLoad = 0,
                TraumaCapacity = 8,
                CurrentTraumaLoad = 0,
                BurnCapacity = 0,
                CurrentBurnLoad = 0,
                ToxicologyCapacity = 0,
                CurrentToxicologyLoad = 0
            },
            new HospitalCapacity
            {
                HospitalId = indToxicology.Id,
                EmergencyCapacity = 8,
                CurrentEmergencyLoad = 0,
                IcuCapacity = 3,
                CurrentIcuLoad = 0,
                TraumaCapacity = 0,
                CurrentTraumaLoad = 0,
                BurnCapacity = 3,
                CurrentBurnLoad = 0,
                ToxicologyCapacity = 4,
                CurrentToxicologyLoad = 0
            }
        );

        context.SaveChanges();
    }
}
