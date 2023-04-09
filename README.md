# Vaccination Records Load Console Application :stethoscope: 	

##### *I created this project to show how a data load can be easily achieved in C# and T-SQL via a console application that can either become a Windows Service or be scheduled in the Windows Task Scheduler. This solution can run entirely on cloud infrastructure while still minimizing processing cloud costs. Current version has been tested locally with success.*

## Project Summary :scroll:

#### In Massachusetts, there is a public health database system that stores vaccination data of Massachusetts residents with their permission. The system was useful to me a few years ago when I scheduled a trip to travel abroad in an area requiring a vaccination that I was not sure I had. The system relies heavily on loading in data from providers throughout Massachusetts who process vaccinations, and relaying its comprehensive data back to providers as requested so that vaccinated individuals have access to all of their vaccination data, reglardless of whether all the individuals' vaccines were given in the same place. 

#### I'm in no way affiliated with this system, but I like thinking about how something would work architecturally from a processing perspective. This project shows how I imagine an ELT process could work to load in new data from vaccination sites. Since this is not a critical, real-time use case, I imagine this process would be run daily. Files could be dropped off by various vaccination locations on an SFTP server, and those files are then loaded into the system by an automated process. 

## Stack and Overall Process :desktop_computer:

#### Processing is completed in C# (.NET 6) and T-SQL. There is an appsettings.json file for drive configurations and the db connection string. 

## Overall project pseudocode :books:

##### 1. Get all files on loading drive via C# program. 
##### 2. Load file lines into C# program for respective files, and bulk copy the lines into the database. 
##### 3. Call T-SQL stored procedure via C# program to clean up copied data and load into finalized tables (silver and gold). 
##### 4. Archive files into archiving directory after completion. 

##### *Entire process includes loading both to log file (.txt file) and sql database table. This allows for the capturing of errors at multiple levels. 

## Potential Opportuntities for Enhancement :mage_woman:

##### 1. Add additional detection for erroneous file lines (Lines where only delimiters are present in lines, incorrect expected delimiter count in given line). 
##### 2. Add additional step to generate email when loading process is complete. 
##### 3. Convert to Worker Service (Windows Service capabilities) by moving processing logic to hosted service .cs file and standardize program.cs file further to add db context initially. In a business/professional environment, this would also be an opportunity to move db connection string to Azure Key Vault. 
##### 4. Create a fuzzy logic matching process for vaccinated individuals so that if an id number of the vaccinated individual is not provided, PII of individual is checked against existing vaccinated individual records so that a duplicate vaccinated individual is not created and the system is capable of consolidating an individual's vaccine history across multiple providers/locations. 
##### 5. In actual business environment, file encryption, data masking of rows in database for users based on role, and column-level encryption could be implemented depending on HIPAA and other regulatory requirements. 
