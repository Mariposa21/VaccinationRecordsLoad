using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VaccinationRecordsLoad.VaccinationCentralSystemContextDirectory.Models;

namespace VaccinationRecordsLoad.VaccinationCentralSystemContextDirectory;

public partial class VaccinationCentralSystemContext : DbContext
{
    public VaccinationCentralSystemContext()
    {
    }

    public VaccinationCentralSystemContext(DbContextOptions<VaccinationCentralSystemContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TblDataLoadLog> TblDataLoadLogs { get; set; }

    public virtual DbSet<TblVaccinationRecordLoadStg> TblVaccinationRecordLoadStgs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var builder = new ConfigurationBuilder()
                      .SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("appsettings.json", optional: false);

        IConfiguration config = builder.Build();

        optionsBuilder.UseSqlServer(config["dbConnectionString"].ToString());
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblDataLoadLog>(entity =>
        {
            entity.HasKey(e => e.DataLoadId).HasName("PK__tblDataL__C2CB2553BABB2376");

            entity.ToTable("tblDataLoadLog");

            entity.Property(e => e.DataLoadAffiliatedOrganization)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.DataLoadFileName)
                .HasMaxLength(8000)
                .IsUnicode(false);
            entity.Property(e => e.DataLoadStartDateTime).HasColumnType("datetime");
            entity.Property(e => e.DateLoadEndDateTime).HasColumnType("datetime");
            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(8000)
                .IsUnicode(false);
            entity.Property(e => e.LastStep)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblVaccinationRecordLoadStg>(entity =>
        {
            entity.HasKey(e => e.RowNumber).HasName("PK__tblVacci__AAAC09D89CFABE88");

            entity.ToTable("tblVaccinationRecordLoadStg");

            entity.Property(e => e.AddressCity)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.AddressLine1)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.AddressLine2)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.AddressState)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.AddressZip)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.DateofBirth)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.DoseNumber)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.MiddleInitial)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.SexAtBirth)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.VaccinatedIndividualId)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.VaccinationDate)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.VaccinationLocation)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.VaccinationType)
                .HasMaxLength(500)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
