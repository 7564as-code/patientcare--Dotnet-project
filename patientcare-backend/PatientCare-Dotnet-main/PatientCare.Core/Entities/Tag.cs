namespace PatientCare.Core.Entities;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<CareShift> CareShifts { get; set; } = new List<CareShift>();
}
