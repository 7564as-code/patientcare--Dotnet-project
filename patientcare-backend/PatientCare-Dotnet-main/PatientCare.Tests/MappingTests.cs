using AutoMapper;
using PatientCare.Core.DTOs;
using PatientCare.Core.Entities;
using PatientCare.Core.Enums;
using PatientCare.Service.Mapping;
using PatientCare.Tests.TestSupport;
using Xunit;

namespace PatientCare.Tests;

public class MappingTests
{
    [Fact]
    public void MappingConfiguration_IsValid()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void Patient_IsManager_DependsOnViewer()
    {
        var mapper = ServiceFactory.CreateMapper();
        var patient = new Patient
        {
            Id = 1,
            MedicalCode = "אמא",
            PatientGender = Gender.Female,
            DateOfBirth = new DateTime(1950, 1, 1),
            Department = "בית",
            ManagerCaregiverId = 7
        };

        var asManager = mapper.Map<PatientResponse>(patient).ForViewer(7);
        var asOther = mapper.Map<PatientResponse>(patient).ForViewer(8);

        Assert.True(asManager.IsManager);
        Assert.False(asOther.IsManager);
        Assert.Equal(AgeCategory.Senior, asManager.AgeCategory);
    }

    [Fact]
    public void CareShift_MapsTagsCaregiversAndIsFull()
    {
        var mapper = ServiceFactory.CreateMapper();
        var shift = new CareShift
        {
            Id = 3,
            PatientId = 1,
            Patient = new Patient { MedicalCode = "אמא" },
            Capacity = 1,
            CurrentCount = 1,
            Tags = { new Tag { Name = "לילה" } },
            Assignments = { new ShiftAssignment { CaregiverId = 5, Caregiver = new Caregiver { FullName = "רחל" } } }
        };

        var dto = mapper.Map<CareShiftResponse>(shift);

        Assert.True(dto.IsFull);
        Assert.Equal("אמא", dto.PatientMedicalCode);
        Assert.Equal(new[] { "לילה" }, dto.Tags);
        Assert.Equal("רחל", Assert.Single(dto.Caregivers).FullName);
    }
}
