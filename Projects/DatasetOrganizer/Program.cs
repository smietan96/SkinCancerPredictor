using Enums;
using System.Configuration;
using System.Data;

namespace DatasetOrganizer
{
    internal class Program
    {
        private static Configuration Configuration;
        private static MetadataFileSettings Settings;

        private static double TestFraction = 0.1;
        private static List<DiagnoseCode> DiagnosesToProcess = new List<DiagnoseCode>()
        {
            DiagnoseCode.bcc,
            DiagnoseCode.mel,
            DiagnoseCode.nv,
            DiagnoseCode.vasc,
        };

        static void Main(string[] args)
        {
            Console.WriteLine("DataSetOrganizer started");
            try
            {
                Configuration = InitConfiguration();
                Console.WriteLine("Configuration created successfully");
                Settings = InitMetadataFileSettings();
                Console.WriteLine("Metadata file settings created successfully");

                string metaDataFilePath = Path.Combine(Configuration.BasePath, Configuration.MetadataFilePath);

                if (string.IsNullOrEmpty(metaDataFilePath))
                {
                    throw new Exception("Metadata file path null or empty");
                }

                List<Lesion> allLesions = GetLesions(metaDataFilePath);
                List<Lesion>? lesions = allLesions?.Where(x => DiagnosesToProcess.Contains(x.DiagnoseCode)).ToList();

                if (lesions == null || lesions.Count == 0)
                {
                    throw new Exception($"Null or empty {nameof(lesions)}");
                }

                ExtractTrainTestSets(lesions, TestFraction);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("DataSetOrganizer finished");
            Console.ReadKey();
        }

        private static Configuration InitConfiguration()
        {
            string basePath = ConfigurationManager.AppSettings["basePath"];
            string metadataFilePath = ConfigurationManager.AppSettings["metadataFilePath"];
            string sourceImagesPath1 = ConfigurationManager.AppSettings["sourceImagesPath1"];
            string sourceImagesPath2 = ConfigurationManager.AppSettings["sourceImagesPath2"];
            string trainImgDir = ConfigurationManager.AppSettings["trainImgDir"];
            string testImgDir = ConfigurationManager.AppSettings["testImgDir"];

            return new Configuration(basePath, metadataFilePath, sourceImagesPath1, sourceImagesPath2, trainImgDir, testImgDir);
        }

        private static MetadataFileSettings InitMetadataFileSettings()
        {
            string lesionIdColumnName = ConfigurationManager.AppSettings["lesionIdColumnName"];
            string imageIdColumnName = ConfigurationManager.AppSettings["imageIdColumnName"];
            string dxColumnName = ConfigurationManager.AppSettings["dxColumnName"];
            string dxTypeColumnName = ConfigurationManager.AppSettings["dxTypeColumnName"];
            string ageColumnName = ConfigurationManager.AppSettings["ageColumnName"];
            string sexColumnName = ConfigurationManager.AppSettings["sexColumnName"];
            string localizationColumnName = ConfigurationManager.AppSettings["localizationColumnName"];

            return new MetadataFileSettings(lesionIdColumnName, imageIdColumnName, dxColumnName, dxTypeColumnName, ageColumnName, sexColumnName, localizationColumnName);
        }

        private static List<Lesion> GetLesions(string path)
        {
            List<Lesion> result = new List<Lesion>();

            int allRecords = 0;
            int successRecords = 0;
            int failedRecords = 0;

            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    return result;
                }

                DataTable table = GetDataTable(path);

                if (table == null)
                {
                    return result;
                }

                allRecords = table.Rows.Count;

                foreach (DataRow row in table.Rows)
                {
                    try
                    {
                        string idLesion = row[Settings.LesionIdColumnName].ToString();
                        string idImage = row[Settings.ImageIdColumnName].ToString();
                        DiagnoseCode diagnoseCode = (DiagnoseCode)Enum.Parse(typeof(DiagnoseCode), row[Settings.DxColumnName].ToString());
                        DiagnoseType diagnoseType = (DiagnoseType)Enum.Parse(typeof(DiagnoseType), row[Settings.DxTypeColumnName].ToString());

                        string? ageStr = row[Settings.AgeColumnName]?.ToString();
                        int? age = !string.IsNullOrEmpty(ageStr) ? (int)Convert.ToDouble(ageStr.Replace('.', ',')) : null;

                        Sex sex = (Sex)Enum.Parse(typeof(Sex), row[Settings.SexColumnName].ToString());
                        //replace ' ' to '_' (from CSV to enum)
                        LesionLocalization localization = (LesionLocalization)Enum.Parse(typeof(LesionLocalization), row[Settings.LocalizationColumnName].ToString().Replace(' ', '_'));
                        string fileName = $"{idImage}.jpg";
                        string filePath = Path.Combine(Configuration.BasePath, Configuration.SourceImagesPath1, fileName);

                        if (!File.Exists(filePath))
                        {
                            filePath = Path.Combine(Configuration.BasePath, Configuration.SourceImagesPath2, fileName);

                            if (!File.Exists(filePath))
                            {
                                throw new Exception($"File {fileName} does not exist in neither of data directories");
                            }
                        }

                        result.Add(new Lesion(idLesion, idImage, diagnoseCode, diagnoseType, age, sex, localization, filePath));
                        successRecords++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting lesion data. Row: {table.Rows.IndexOf(row)}, Error message: {ex.Message}");
                        failedRecords++;
                        continue;
                    }
                }

                return result;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Console.WriteLine($"All records count: {allRecords}\nConverted lesion object list count: {successRecords}\nNot converted lesions count: {failedRecords}");
            }
        }

        private static DataTable GetDataTable(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            DataTable dt = new DataTable();

            using (StreamReader sr = new StreamReader(path))
            {
                string[] headers = sr.ReadLine().Split(',');

                foreach (string header in headers)
                {
                    dt.Columns.Add(header);
                }

                while (!sr.EndOfStream)
                {
                    string[] rows = sr.ReadLine().Split(',');
                    DataRow dr = dt.NewRow();

                    for (int i = 0; i < headers.Length; i++)
                    {
                        dr[i] = rows[i];
                    }

                    dt.Rows.Add(dr);
                }
            }

            return dt;
        }

        private static void ExtractTrainTestSets(List<Lesion> lesions, double testFraction)
        {
            if (lesions == null || lesions.Count == 0)
            {
                return;
            }

            if (testFraction <= 0 || testFraction >= 1)
            {
                throw new Exception($"Invalid {nameof(testFraction)}");
            }

            string trainDirPath = Path.Combine(Configuration.BasePath, Configuration.TrainImagesDir);
            string testDirPath = Path.Combine(Configuration.BasePath, Configuration.TestImagesDir);

            List<IGrouping<DiagnoseCode, Lesion>> lesionsGroupedByDiagnoseCode = lesions.GroupBy(x => x.DiagnoseCode).ToList();

            try
            {
                foreach (IGrouping<DiagnoseCode, Lesion> diagnoseCodeGroup in lesionsGroupedByDiagnoseCode)
                {
                    Console.WriteLine($"Performing {nameof(ExtractTrainTestSets)} for category: {diagnoseCodeGroup.Key}");
                    string subDirName = diagnoseCodeGroup.Key.ToString();
                    string subTrainDirPath = Path.Combine(trainDirPath, subDirName);
                    string subTestDirPath = Path.Combine(testDirPath, subDirName);
                    List<Lesion> diagnoseCodeLesions = diagnoseCodeGroup.ToList();

                    List<FileInfo> files = new List<FileInfo>();
                    diagnoseCodeLesions.ForEach(x => files.Add(new FileInfo(x.FilePath)));

                    int testFilesCount = (int)(testFraction * diagnoseCodeLesions.Count);
                    int trainFilesCount = diagnoseCodeLesions.Count - testFilesCount;

                    Random random = new Random();
                    List<FileInfo> filesToTrain = new List<FileInfo>();
                    List<FileInfo> filesToTest = new List<FileInfo>();

                    while (filesToTrain.Count < trainFilesCount)
                    {
                        int fileIndex = random.Next(files.Count);
                        FileInfo file = files[fileIndex];

                        if (!filesToTrain.Contains(file))
                        {
                            filesToTrain.Add(file);
                        }
                    }

                    while (filesToTest.Count < testFilesCount)
                    {
                        int fileIndex = random.Next(files.Count);
                        FileInfo file = files[fileIndex];

                        if (!filesToTest.Contains(file) && !filesToTrain.Contains(file))
                        {
                            filesToTest.Add(file);
                        }
                    }

                    if (!Directory.Exists(subTrainDirPath))
                    {
                        Directory.CreateDirectory(subTrainDirPath);
                    }

                    if (!Directory.Exists(subTestDirPath))
                    {
                        Directory.CreateDirectory(subTestDirPath);
                    }

                    //breakpoint on any to check for duplicates
                    bool testDuplicates = filesToTest.Count != filesToTest.DistinctBy(x => x.Name).ToList().Count;
                    bool trainDuplicates = filesToTrain.Count != filesToTrain.DistinctBy(x => x.Name).ToList().Count;
                    bool testTrainDuplicates = filesToTrain.Any(x => filesToTest.Select(y => y.Name).ToArray().Contains(x.Name));

                    DeleteExistingFiles(new DirectoryInfo(subTrainDirPath));
                    DeleteExistingFiles(new DirectoryInfo(subTestDirPath));

                    CopyFiles(filesToTrain, subTrainDirPath);
                    CopyFiles(filesToTest, subTestDirPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                if (Directory.Exists(trainDirPath))
                {
                    Directory.Delete(trainDirPath, true);
                }

                if (Directory.Exists(testDirPath))
                {
                    Directory.Delete(testDirPath, true);
                }
            }
        }

        private static void CopyFiles(List<FileInfo> files, string diagnoseOutputDir)
        {
            int count = 0;
            int max = files.Count;

            Console.WriteLine($"Performing {nameof(CopyFiles)} to '{diagnoseOutputDir}'...");
            using (ProgressBar progress = new ProgressBar())
            {
                foreach (FileInfo file in files)
                {
                    count++;
                    string outputPath = Path.Combine(diagnoseOutputDir, file.Name);
                    File.Copy(file.FullName, outputPath);
                    progress.Report((double)count / max);
                }
            }
            Console.WriteLine($"Copied {files.Count} files to '{diagnoseOutputDir}'");
        }

        private static void DeleteExistingFiles(DirectoryInfo di)
        {
            FileInfo[] fileInfo = di.GetFiles();

            if (fileInfo.Any())
            {
                foreach (FileInfo file in fileInfo)
                {
                    file.Delete();
                }

                Console.WriteLine($"Deleted {fileInfo.Length} files in '{di.FullName}'");
            }
            else
            {
                Console.WriteLine($"No files to delete in '{di.FullName}'");
            }
        }
    }
}