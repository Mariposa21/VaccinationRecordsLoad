using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using System;
using System.ComponentModel;
using System.Data;
using System.Linq.Expressions;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using VaccinationRecordsLoad.VaccinationCentralSystemContextDirectory;
using VaccinationRecordsLoad.VaccinationCentralSystemContextDirectory.Models;

namespace VaccinationRecordsLoad 
{
    public class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                      .SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("appsettings.json", optional: false);

            IConfiguration config = builder.Build();

            string loadingDirectory = config["loadingDirectory"].ToString();
            string loadingDirectoryArchive = config["loadingDirectoryArchive"].ToString();
            string logDirectory = config["C:\\LoadingFiles\\LoadingLog"].ToString(); 
            string dbConnectionString = config["dbConnectionString"].ToString();

            //Create log file 
            string logFileName = Path.Combine(logDirectory, "_VaccinationRecordLoadLog_" + DateTime.UtcNow.ToString() + ".txt"); 
            FileStream filestream = new FileStream("out.txt", FileMode.Create);
            var streamwriter = new StreamWriter(filestream);
            streamwriter.AutoFlush = true;
            Console.SetOut(streamwriter);
            Console.SetError(streamwriter);


            //Identify files for loading
            createLogEntry("Starting VaccinationRecordsLoad app. Checking to see if there files to process.");
            string[] filesForLoadingList = Directory.GetFiles(loadingDirectory, "*.csv"); 

            //If there are files to load, load them into database, clean up them up, and then archive the loading files. 
            if (filesForLoadingList.Count() > 0)
            {
                createLogEntry(filesForLoadingList.Count().ToString() + " found. Beginning processing.");

                foreach (string file in filesForLoadingList)
                {
                    //Load file into database and clean up via stored procedure call 
                    loadFileToDatabase(file, dbConnectionString);
                    //Archive loading files to loading directory 
                    archiveLoadingFile(file, loadingDirectoryArchive);
                }
            }
            else
            {
                //No files to process, exits out of console app successfully w/o error. 
                createLogEntry("No record to process. Exiting console application.");
                Environment.Exit(0); 
            }
        }

        private static void loadFileToDatabase(string fileName, string dbConnectionString)
        {
            long newLogId = 0;
            try
            {
                string[] fileNameBreakdown = fileName.Split('_');
                string orgName = fileNameBreakdown[1];

                if (!String.IsNullOrWhiteSpace(orgName))
                {
                    TblDataLoadLog newLoadRecord = new TblDataLoadLog()
                    {
                        DataLoadAffiliatedOrganization = orgName,
                        DataLoadFileName = fileName,
                        DataLoadStartDateTime = DateTime.UtcNow,
                        LastStep = "Begin-CreateLogRecord"
                    };

                    using (VaccinationCentralSystemContext vaxContext = new VaccinationCentralSystemContext())
                    {
                        vaxContext.Add(newLoadRecord);
                        vaxContext.SaveChanges();
                        newLogId = newLoadRecord.DataLoadId;
                    }


                    List<TblVaccinationRecordLoadStg> vaxStageRecords = new List<TblVaccinationRecordLoadStg>();

                    using (StreamReader file = new StreamReader(fileName))
                    {
                        string line;
                        int counter = 1;
                        while ((line = file.ReadLine()) != null)
                        {
                            try {
                                //First line is header, so disregard it for load. 
                                if (counter == 1)
                                {
                                    counter++;
                                    continue;
                                }

                                char[] delimiters = new char[] { '|' };
                                string[] lineFields = line.Split(delimiters);

                                TblVaccinationRecordLoadStg newVaxRecordLoadStg = new TblVaccinationRecordLoadStg();
                                newVaxRecordLoadStg.DataLoadId = newLogId;
                                newVaxRecordLoadStg.VaccinatedIndividualId = lineFields[0];
                                newVaxRecordLoadStg.FirstName = lineFields[1];
                                newVaxRecordLoadStg.MiddleInitial = lineFields[2];
                                newVaxRecordLoadStg.LastName = lineFields[3];
                                newVaxRecordLoadStg.DateofBirth = lineFields[4];
                                newVaxRecordLoadStg.SexAtBirth = lineFields[5];
                                newVaxRecordLoadStg.AddressLine1 = lineFields[6];
                                newVaxRecordLoadStg.AddressLine2 = lineFields[7];
                                newVaxRecordLoadStg.AddressCity = lineFields[8];
                                newVaxRecordLoadStg.AddressState = lineFields[9];
                                newVaxRecordLoadStg.AddressZip = lineFields[10];
                                newVaxRecordLoadStg.VaccinationType = lineFields[11];
                                newVaxRecordLoadStg.VaccinationDate = lineFields[12];
                                newVaxRecordLoadStg.VaccinationLocation = lineFields[13];
                                newVaxRecordLoadStg.DoseNumber = lineFields[14];
                                vaxStageRecords.Add(newVaxRecordLoadStg);

                                counter++;

                            }
                            catch (Exception ex)
                            {
                                if (ex.Message != null)
                                {
                                    createLogEntry("Skipping over line " + counter.ToString() + " in " + fileName + "due to error processing line. Error: " + ex.Message.ToString());
                                }
                                else
                                {
                                    createLogEntry("Skipping over line " + counter.ToString() + " in " + fileName + "due to error processing line.");
                                }
                                continue; 
                            }

                        }

                        file.Close();
                        createLogEntry("Closing file. Reading of file completed."); 
                    }

                    DataTable newDataToLoad = ConvertToDataTable(vaxStageRecords);
                    int totalRecords = vaxStageRecords.Count();
                    createLogEntry("Starting SQL bulk copy process of data for " + totalRecords.ToString() + " records.");  

                    using (SqlConnection con = new SqlConnection(dbConnectionString))
                    {
                        using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(dbConnectionString))
                        {
                            sqlBulkCopy.DestinationTableName = "tblVaccinationRecordLoadStg";
                            sqlBulkCopy.BatchSize = 5000; 
                            con.Open();
                            sqlBulkCopy.WriteToServer(newDataToLoad);
                            con.Close();
                        }
                    }

                    createLogEntry("SQL bulk processing complete. Beginning database processing."); 

                    if (newLogId != 0)
                    {
                        using (VaccinationCentralSystemContext vaxContext = new VaccinationCentralSystemContext())
                        {
                            newLoadRecord = vaxContext.TblDataLoadLogs.Where(x => x.DataLoadId == newLogId).FirstOrDefault();
                            newLoadRecord.LastStep = "DirectCSVFileLineLoad";
                            newLoadRecord.TotalRecordsLoaded = totalRecords; 
                            vaxContext.SaveChanges();
                        }
                    }

                    //Stored procedure call does the cleaning up of data and loading into silver/gold tables
                    using (SqlConnection sqlConn = new SqlConnection(dbConnectionString))
                    {
                        using (SqlCommand cmd = new SqlCommand())
                        {
                            cmd.CommandText = "dbo.uspVaxRecordsLoadStagingData";
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Connection = sqlConn;
                            cmd.CommandTimeout = 0;

                            sqlConn.Open();

                            cmd.ExecuteNonQuery();

                        }
                    }

                    if (newLogId != 0)
                    {
                        using (VaccinationCentralSystemContext vaxContext = new VaccinationCentralSystemContext())
                        {
                            newLoadRecord = vaxContext.TblDataLoadLogs.Where(x => x.DataLoadId == newLogId).FirstOrDefault();
                            newLoadRecord.LastStep = "dbo.uspVaxRecordsLoadStagingData";
                            vaxContext.SaveChanges();
                        }
                    }

                    if (newLogId != 0)
                    {
                        using (VaccinationCentralSystemContext vaxContext = new VaccinationCentralSystemContext())
                        {
                            newLoadRecord = vaxContext.TblDataLoadLogs.Where(x => x.DataLoadId == newLogId).FirstOrDefault();
                            newLoadRecord.DateLoadEndDateTime = DateTime.UtcNow;
                            newLoadRecord.LastStep = "DataLoadComplete";
                            vaxContext.SaveChanges();
                        }
                    }

                    createLogEntry("Database processing for data load complete."); 
                }
            }
            catch (Exception ex)
            {
                if (ex.Message != null)
                {
                    createLogEntry("File processing error for " + fileName + ": " + ex.Message.ToString()); 
                }
                else
                {
                    createLogEntry("File processing error for " + fileName); 
                }

                //If an error occurs after a new log record has been created, log the respective log record's error in the database.  
                if (newLogId != 0)
                {
                    using (VaccinationCentralSystemContext vaxContext = new VaccinationCentralSystemContext())
                    {
                        TblDataLoadLog newLoadRecord = vaxContext.TblDataLoadLogs.Where(x => x.DataLoadId == newLogId).FirstOrDefault();
                        newLoadRecord.DateLoadEndDateTime = DateTime.UtcNow;
                        newLoadRecord.IsError = true;
                        newLoadRecord.ErrorMessage = ex.Message.ToString();
                        vaxContext.SaveChanges();
                    }
                }
                else
                {
                    throw;
                }
            }

        }

        //Archive loaded files by moving over from loading drive to archive drive. 
        public static void archiveLoadingFile(string fileName, string loadingDirectoryArchive)
        {
            if (File.Exists(fileName))
            {
                string archiveFileName = Path.Combine(loadingDirectoryArchive, Path.GetFileName(fileName));
                File.Copy(fileName, archiveFileName);
                if (File.Exists(archiveFileName))
                {
                    createLogEntry(fileName + " copied to archive drive as " + archiveFileName); 
                    File.Delete(fileName);
                    createLogEntry(fileName + " deleted following copying to archive drive.");
                }
            }
        }

        public static void createLogEntry(string logStatus)
        {
            Console.WriteLine("___________________________________________________________________");
            Console.WriteLine("Current UTC Time: " + DateTime.UtcNow.ToString());
            Console.WriteLine(logStatus);
            Console.WriteLine("___________________________________________________________________");

        }

        //Generic data table conversion method identified on stack overflow (https://stackoverflow.com/questions/564366/convert-generic-list-enumerable-to-datatable)
        public static DataTable ConvertToDataTable<T>(IList<T> list)
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();
            for (int i = 0; i < props.Count; i++)
            {
                PropertyDescriptor prop = props[i];
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }
            object[] values = new object[props.Count];
            foreach (T item in list)
            {
                for (int i = 0; i < values.Length; i++)
                    values[i] = props[i].GetValue(item) ?? DBNull.Value;
                table.Rows.Add(values);
            }
            return table;
        }
    }


}