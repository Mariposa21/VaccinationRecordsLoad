--Creating Vaccination Central System database 
CREATE DATABASE VaccinationCentralSystem; 

GO

USE [VaccinationCentralSystem]

GO 

--Static tables that are used as references/dictionary tables.
--Comprehensive list of vaccination types that could be loaded into system
CREATE TABLE dbo.stbVaccination (
	VaccinationId int IDENTITY(1,1) NOT NULL PRIMARY KEY, 
	VaccinationName varchar(200) NOT NULL, 
	VaccinationManufacturer varchar(200) NOT NULL
); 

--Comprehensive list of locations where these vaccinations could take place (ex:hospitals, etc.)
CREATE TABLE dbo.stbLocation (
	LocationId smallint IDENTITY(1,1) NOT NULL PRIMARY KEY, 
	LocationName varchar(100), 
	AffiliatedOrganization varchar(100)
); 

--Data load log table
CREATE TABLE dbo.tblDataLoadLog (
	DataLoadId bigint IDENTITY(1,1) NOT NULL PRIMARY KEY, 
	DataLoadAffiliatedOrganization varchar(100) NOT NULL, 
	DataLoadFileName varchar(8000) NOT NULL, 
	DataLoadStartDateTime datetime NOT NULL, 
	DateLoadEndDateTime datetime NULL, 
	TotalRecordsLoaded int NULL, 
	LastStep varchar(100) NOT NULL, 
	IsError bit NOT NULL DEFAULT(0), 
	ErrorMessage varchar(8000) NULL
); 

ALTER TABLE dbo.tblDataLoadLog
	ALTER COLUMN TotalRecordsLoaded int NULL
--Bronze table --> This is the actual loading of data into the system. Cleared out after every successful load
CREATE TABLE dbo.tblVaccinationRecordLoadStg (
	RowNumber bigint NOT NULL IDENTITY(1,1) PRIMARY KEY, 
	DataLoadId bigint NOT NULL, 
	VaccinatedIndividualId varchar(500) NULL, 
	FirstName varchar(500) NULL, 
	MiddleInitial varchar(500) NULL, 
	LastName varchar(500) NULL, 
	DateofBirth varchar(500) NULL, 
	SexAtBirth varchar(500) NULL, 
	AddressLine1 varchar(500) NULL, 
	AddressLine2 varchar(500) NULL, 
	AddressCity varchar(500) NULL, 
	AddressState varchar(500) NULL, 
	AddressZip varchar(500) NULL, 
	VaccinationType varchar(500) NULL, 
	VaccinationDate varchar(500) NULL, 
	VaccinationLocation varchar(500) NULL, 
	DoseNumber varchar(500) NULL
); 

--Gold table --> These are the finalized business record tables that the silver table feeds into 
--Vaccinated individuals table -- ideally, s/b able to associated all vaccines to one individual via fuzzy match 
CREATE TABLE dbo.tblVaccinatedIndividuals (
	VaccinatedIndividualId bigint IDENTITY(1,1) NOT NULL PRIMARY KEY, 
	FirstName varchar(100) NOT NULL, 
	MiddleInitial char(1) NULL, 
	LastName varchar(100) NOT NULL, 
	DateofBirth date NOT NULL, 
	SexAtBirth char(1) NOT NULL, 
	AddressLine1 varchar(100) NULL, 
	AddressLine2 varchar(100) NULL, 
	AddressCity varchar(100) NULL, 
	AddressState char(2) NULL, 
	AddressZip varchar(5) NULL, 
); 

--Vaccinated records table where an individual's vaccinations are held 
CREATE TABLE dbo.tblVaccinationRecords (
	VaccinatedIndividualId bigint NOT NULL, 
	VaccinationId int NOT NULL,
	VaccinationDate date NOT NULL, 
	VaccinationLocation smallint NULL, 
	DoseNumber varchar(100) NULL, 
	PRIMARY KEY (VaccinatedIndividualId, VaccinationId)
); 

--Silver table --> This is the load data table that contains clean, loaded data. 
CREATE TABLE dbo.tblVaccinationRecordLoadData (
	VaccinationRecordLoadDataId bigint IDENTITY(1,1) NOT NULL PRIMARY KEY, 
	VaccinatedIndividualId bigint NULL FOREIGN KEY REFERENCES dbo.tblVaccinatedIndividuals(VaccinatedIndividualId), 
	FirstName varchar(100) NOT NULL, 
	MiddleInitial char(1) NULL, 
	LastName varchar(100) NOT NULL, 
	DateofBirth date NOT NULL, 
	SexAtBirth char(1) NOT NULL, 
	AddressLine1 varchar(100) NULL, 
	AddressLine2 varchar(100) NULL, 
	AddressCity varchar(100) NULL, 
	AddressState char(2) NULL, 
	AddressZip varchar(5) NULL, 
	VaccinationId int NOT NULL FOREIGN KEY REFERENCES dbo.stbVaccination (VaccinationId), 
	VaccinationDate date NOT NULL, 
	VaccinationLocationId smallint NULL FOREIGN KEY REFERENCES dbo.stbLocation (LocationId), 
	VaccinationDoseNumber varchar(100) NULL
); 

--Insert lookup records into dbo.stbLocation and dbo.stbVaccination 
INSERT INTO dbo.stbLocation (LocationName, AffiliatedOrganization) 
VALUES ('Boston MGH', 'MGH')

INSERT INTO dbo.stbVaccination (VaccinationName, VaccinationManufacturer) 
VALUES ('Polio Sanofi', 'Sanofi'), 
('Hep A Sanofi', 'Sanofi')