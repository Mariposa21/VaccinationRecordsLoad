using System;
using System.Collections.Generic;

namespace VaccinationRecordsLoad.VaccinationCentralSystemContextDirectory.Models;

public partial class TblDataLoadLog
{
    public long DataLoadId { get; set; }

    public string DataLoadAffiliatedOrganization { get; set; } = null!;

    public string DataLoadFileName { get; set; } = null!;

    public DateTime DataLoadStartDateTime { get; set; }

    public DateTime? DateLoadEndDateTime { get; set; }

    public int? TotalRecordsLoaded { get; set; }

    public string LastStep { get; set; } = null!;

    public bool IsError { get; set; }

    public string? ErrorMessage { get; set; }
}
