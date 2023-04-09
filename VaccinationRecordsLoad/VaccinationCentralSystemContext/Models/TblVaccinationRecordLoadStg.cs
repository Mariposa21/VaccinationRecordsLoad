using System;
using System.Collections.Generic;

namespace VaccinationRecordsLoad.VaccinationCentralSystemContextDirectory.Models;

public partial class TblVaccinationRecordLoadStg
{
    public long RowNumber { get; set; }

    public long DataLoadId { get; set; }

    public string? VaccinatedIndividualId { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleInitial { get; set; }

    public string? LastName { get; set; }

    public string? DateofBirth { get; set; }

    public string? SexAtBirth { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? AddressCity { get; set; }

    public string? AddressState { get; set; }

    public string? AddressZip { get; set; }

    public string? VaccinationType { get; set; }

    public string? VaccinationDate { get; set; }

    public string? VaccinationLocation { get; set; }

    public string? DoseNumber { get; set; }
}
