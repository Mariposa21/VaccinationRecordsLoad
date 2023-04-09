USE [VaccinationCentralSystem] 

GO 

CREATE PROCEDURE dbo.uspVaxRecordsLoadStagingData 
AS 
BEGIN 
--Loop limit for processing
DECLARE @iLoopAmnt int = 50000; 

--Temp tables for various inserts and loop processing
CREATE TABLE #NewVaccinationLoadRecordIds (
	VaccinationRecordLoadDataId bigint NOT NULL PRIMARY KEY, 
	VaccinationRecordIndividualId bigint NULL
); 

CREATE TABLE #NewVaccinationIndividualIdRecords (
	VaccinationRecordIndividualId bigint NOT NULL 
); 

CREATE TABLE #StagingRowsToEvaluate (
	StagingRowNumber bigint NOT NULL PRIMARY KEY
); 

--Loops will occur and until there is no longer any data in staging table for loading 
WHILE EXISTS (SELECT 1 
			  FROM dbo.tblVaccinationRecordLoadStg) 
BEGIN 
BEGIN TRY
	TRUNCATE TABLE #StagingRowsToEvaluate
	TRUNCATE TABLE #NewVaccinationLoadRecordIds
	TRUNCATE TABLE #NewVaccinationIndividualIdRecords

	INSERT INTO #StagingRowsToEvaluate (StagingRowNumber) 
	SELECT TOP (@iLoopAmnt) RowNumber
	FROM dbo.tblVaccinationRecordLoadStg
	ORDER BY RowNumber ASC

	--Insert into silver table valid data records
	INSERT INTO dbo.tblVaccinationRecordLoadData (VaccinatedIndividualId, FirstName, MiddleInitial, LastName, DateofBirth, SexAtBirth, 
		AddressLine1, AddressLine2, AddressCity, AddressState, AddressZip, VaccinationId, VaccinationDate, VaccinationLocationId, VaccinationDoseNumber)
	OUTPUT Inserted.VaccinationRecordLoadDataId, Inserted.VaccinatedIndividualId INTO #NewVaccinationLoadRecordIds (VaccinationRecordLoadDataId, VaccinationRecordIndividualId)
	SELECT SQ.VaccinatedIndividualId, SQ.FirstName, SQ.MiddleInitial, SQ.LastName, SQ.DateofBirth, SQ.SexAtBirth, 
		SQ.AddressLine1, SQ.AddressLine2, SQ.AddressCity, SQ.AddressState, SQ.AddressZip, V.VaccinationId, SQ.VaccinationDate, L.LocationId, SQ.DoseNumber
	FROM (
	SELECT CASE WHEN ISNUMERIC(VaccinatedIndividualId) = 1 AND VaccinatedIndividualId <> '0' THEN VaccinatedIndividualId ELSE NULL END AS VaccinatedIndividualId, 
		CASE WHEN TRIM(FirstName) <> '' THEN LEFT(TRIM(FirstName), 100) ELSE NULL END AS FirstName, 
		CASE WHEN TRIM(MiddleInitial) <> '' THEN LEFT(TRIM(MiddleInitial), 1) ELSE NULL END AS MiddleInitial, 
		CASE WHEN TRIM(LastName) <> '' THEN LEFT(TRIM(LastName), 100) ELSE NULL END AS LastName, 
		TRY_PARSE(DateofBirth AS datetime) AS DateOfBirth, CASE WHEN TRIM(SexAtBirth) IN ('F', 'M') THEN TRIM(SexAtBirth) ELSE NULL END AS SexAtBirth,
		CASE WHEN TRIM(AddressLine1) <> '' THEN LEFT(TRIM(AddressLine1), 100) ELSE NULL END AS AddressLine1, 
		CASE WHEN TRIM(AddressLine2) <> '' THEN LEFT(TRIM(AddressLine2), 100) ELSE NULL END AS AddressLine2, 
		CASE WHEN TRIM(AddressCity) <> '' THEN LEFT(TRIM(AddressCity), 100) ELSE NULL END AS AddressCity, 
		CASE WHEN TRIM(AddressState) <> '' THEN LEFT(TRIM(AddressState), 2) ELSE NULL END AS AddressState, 
		CASE WHEN TRIM(AddressZip) <> '' THEN LEFT(TRIM(AddressZip), 5) ELSE NULL END AS AddressZip, 
		CASE WHEN TRIM(VaccinationType) <> '' THEN LEFT(TRIM(VaccinationType), 200) ELSE NULL END AS VaccinationType, 
		TRY_PARSE(VaccinationDate AS date) AS VaccinationDate,
		CASE WHEN TRIM(VaccinationLocation) <> '' THEN LEFT(TRIM(VaccinationLocation), 100) ELSE NULL END AS VaccinationLocation, 
		CASE WHEN TRIM(DoseNumber) <> '' THEN LEFT(TRIM(DoseNumber), 100) ELSE NULL END AS DoseNumber 
	FROM dbo.tblVaccinationRecordLoadStg Stg
		INNER JOIN #StagingRowsToEvaluate Stg2
			ON Stg.RowNumber = Stg2.StagingRowNumber) SQ
		INNER JOIN dbo.stbVaccination V 
			ON SQ.VaccinationType = V.VaccinationName
		INNER JOIN dbo.stbLocation L 
			ON SQ.VaccinationLocation = L.LocationName
	WHERE SQ.FirstName IS NOT NULL 
	AND SQ.LastName IS NOT NULL 
	AND SQ.VaccinationDate IS NOT NULL

	--Insert any vaccinated individual records that do not already exist (based on having valid vaccinatedindividualid) into table
	--In future, this portion could also include updates of addresses for existing vaccinatedindividualids
	INSERT INTO dbo.tblVaccinatedIndividuals (FirstName, MiddleInitial, LastName, DateofBirth, SexAtBirth, 
			AddressLine1, AddressLine2, AddressCity, AddressState, AddressZip)
	OUTPUT Inserted.VaccinatedIndividualId INTO #NewVaccinationIndividualIdRecords (VaccinationRecordIndividualId)
	SELECT LD.FirstName, LD.MiddleInitial, LD.LastName, LD.DateofBirth, LD.SexAtBirth, 
			LD.AddressLine1, LD.AddressLine2, LD.AddressCity, LD.AddressState, LD.AddressZip
	FROM dbo.tblVaccinationRecordLoadData LD 
		INNER JOIN #NewVaccinationLoadRecordIds VR
			ON VR.VaccinationRecordLoadDataId = LD.VaccinationRecordLoadDataId
	WHERE VR.VaccinationRecordIndividualId IS NULL
	ORDER BY VR.VaccinationRecordLoadDataId ASC

	--Update individual ids
	--This associates newly inserted vaccinated individual ids w/ the newly inserted data load records by lining up the insert order of the two inserts. 
	UPDATE V
		SET V.VaccinationRecordIndividualId = S2.VaccinationRecordIndividualId
	FROM #NewVaccinationLoadRecordIds V
		INNER JOIN (SELECT R.VaccinationRecordLoadDataId, ROW_NUMBER() OVER(ORDER BY R.VaccinationRecordLoadDataId ASC) AS RowNum
					FROM #NewVaccinationLoadRecordIds R
					WHERE R.VaccinationRecordIndividualId IS NULL) S1 
			ON V.VaccinationRecordLoadDataId = S1.VaccinationRecordLoadDataId
		INNER JOIN (SELECT R.VaccinationRecordIndividualId, ROW_NUMBER() OVER(ORDER BY R.VaccinationRecordIndividualId ASC) AS RowNum
					FROM #NewVaccinationIndividualIdRecords R) S2
			ON S1.RowNum = S2.RowNum

	INSERT INTO dbo.tblVaccinationRecords (VaccinatedIndividualId, VaccinationId, VaccinationLocation, VaccinationDate, DoseNumber) 
	SELECT VR.VaccinationRecordIndividualId, LD.VaccinationId, LD.VaccinationLocationId, LD.VaccinationDate, LD.VaccinationDoseNumber
	FROM dbo.tblVaccinationRecordLoadData LD 
		INNER JOIN #NewVaccinationLoadRecordIds VR
			ON VR.VaccinationRecordLoadDataId = LD.VaccinationRecordLoadDataId
	WHERE vr.VaccinationRecordIndividualId IS NOT NULL

	--Delete out all loaded records 
	DELETE Stg
	FROM dbo.tblVaccinationRecordLoadStg Stg
		INNER JOIN #StagingRowsToEvaluate R 
			ON Stg.RowNumber = R.StagingRowNumber

END TRY 
BEGIN CATCH 
	;THROW
END CATCH 
END

--Truncate staging table out to reseed row number column after loading is complete 
TRUNCATE TABLE dbo.tblVaccinationRecordLoadStg

DROP TABLE #NewVaccinationLoadRecordIds
DROP TABLE #StagingRowsToEvaluate
DROP TABLE #NewVaccinationIndividualIdRecords

END

