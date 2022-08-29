using Enums;
using System.Configuration;
using System.Data;

namespace DatasetOrganizer
{
    internal class Program
    {
        private static int ImagesToTrainCount = 300;
        private static int ImagesToTestCount = 20;
        private static List<DiagnoseCode> DiagnosesToProcess = new List<DiagnoseCode>()
        {
            DiagnoseCode.bcc,
            DiagnoseCode.mel,
            DiagnoseCode.nv,
            DiagnoseCode.vasc,
        };

        private static Configuration Configuration;
        private static MetadataFileSettings Settings;

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

                List<Lesion> lesions = GetLesions(metaDataFilePath);

                foreach (DiagnoseCode diagnose in DiagnosesToProcess)
                {
                    List<Lesion> lesionsToProcess = lesions.Where(x => x.DiagnoseCode == diagnose).ToList();
                    SplitLesionImages(lesionsToProcess);
                    //ExtractTrainTestSets(diagnose, ImagesToTrainCount, ImagesToTestCount);
                    ExtractTrainTestSets(diagnose, 0.1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("DataSetOrganizer finished");
            Console.ReadKey();
        }

        private static void ExtractTrainTestSets(DiagnoseCode code, double testFraction)
        {
            if (testFraction <= 0 || testFraction >= 1)
            {
                throw new Exception($"Invalid {nameof(testFraction)}");
            }

            string diagnoseAllDir = Path.Combine(Configuration.BasePath, Configuration.AllImagesDir, code.ToString());
            string diagnoseTrainDir = Path.Combine(Configuration.BasePath, Configuration.TrainImagesDir, code.ToString());
            string diagnoseTestDir = Path.Combine(Configuration.BasePath, Configuration.TestImagesDir, code.ToString());

            DirectoryInfo directoryInfoAll = new DirectoryInfo(diagnoseAllDir);
            DirectoryInfo directoryInfoTrain = new DirectoryInfo(diagnoseTrainDir);
            DirectoryInfo directoryInfoTest = new DirectoryInfo(diagnoseTestDir);

            try
            {
                FileInfo[] files = directoryInfoAll.GetFiles();

                int testFilesCount = (int)(testFraction * files.Length);
                int trainFilesCount = files.Length - testFilesCount;

                if (trainFilesCount > files.Length)
                {
                    trainFilesCount = files.Length - testFilesCount;
                }

                Random random = new Random();
                List<FileInfo> filesToTrain = new List<FileInfo>();
                List<FileInfo> filesToTest = new List<FileInfo>();

                while (filesToTrain.Count < trainFilesCount)
                {
                    int fileIndex = random.Next(files.Length);
                    FileInfo file = files[fileIndex];

                    if (!filesToTrain.Contains(file))
                    {
                        filesToTrain.Add(file);
                    }
                }

                while (filesToTest.Count < testFilesCount)
                {
                    int fileIndex = random.Next(files.Length);
                    FileInfo file = files[fileIndex];

                    if (!filesToTest.Contains(file) && !filesToTrain.Contains(file))
                    {
                        filesToTest.Add(file);
                    }
                }

                if (!Directory.Exists(diagnoseTrainDir))
                {
                    Directory.CreateDirectory(diagnoseTrainDir);
                }

                if (!Directory.Exists(diagnoseTestDir))
                {
                    Directory.CreateDirectory(diagnoseTestDir);
                }

                DeleteExistingFiles(directoryInfoTrain);
                DeleteExistingFiles(directoryInfoTest);

                CopyFiles(filesToTrain, diagnoseTrainDir);
                CopyFiles(filesToTest, diagnoseTestDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                if (Directory.Exists(diagnoseTrainDir))
                {
                    Directory.Delete(diagnoseTrainDir, true);
                }

                if (Directory.Exists(diagnoseTestDir))
                {
                    Directory.Delete(diagnoseTestDir, true);
                }
            }
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

        private static Configuration InitConfiguration()
        {
            string basePath = ConfigurationManager.AppSettings["basePath"];
            string metadataFilePath = ConfigurationManager.AppSettings["metadataFilePath"];
            string sourceImagesPath1 = ConfigurationManager.AppSettings["sourceImagesPath1"];
            string sourceImagesPath2 = ConfigurationManager.AppSettings["sourceImagesPath2"];
            string allImgDir = ConfigurationManager.AppSettings["allImgDir"];
            string trainImgDir = ConfigurationManager.AppSettings["trainImgDir"];
            string testImgDir = ConfigurationManager.AppSettings["testImgDir"];

            return new Configuration(basePath, metadataFilePath, sourceImagesPath1, sourceImagesPath2,
                allImgDir, trainImgDir, testImgDir);
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

        private static void SplitLesionImages(List<Lesion> lesions)
        {
            if (lesions == null || lesions.Count == 0)
            {
                return;
            }

            int count = 0;
            int max = lesions.Count;
            List<IGrouping<DiagnoseCode, Lesion>> lesionsGrouped = lesions.GroupBy(x => x.DiagnoseCode).ToList();
            string allImagesDir = Path.Combine(Configuration.BasePath, Configuration.AllImagesDir);

            try
            {
                Console.WriteLine($"Performing {nameof(SplitLesionImages)}... ");
                using (ProgressBar progress = new ProgressBar())
                {
                    foreach (IGrouping<DiagnoseCode, Lesion> group in lesionsGrouped)
                    {
                        Console.WriteLine($"Lesion diagnose: {group.Key.ToString()}");
                        string outputDirectory = Path.Combine(allImagesDir, group.Key.ToString());

                        if (!Directory.Exists(outputDirectory))
                        {
                            Directory.CreateDirectory(outputDirectory);
                        }

                        if (Directory.GetFiles(outputDirectory).Any())
                        {
                            Console.WriteLine($"There are already files in {outputDirectory}");
                            continue;
                        }

                        foreach (Lesion lesion in group.ToList())
                        {
                            try
                            {
                                count++;
                                string outputFilePath = Path.Combine(outputDirectory, $"{lesion.IdImage}.jpg");
                                File.Copy(lesion.FilePath, outputFilePath);
                                progress.Report((double)count / max);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error while copying file {lesion.FilePath}. Message: {ex.Message}");
                                continue;
                            }
                        }
                    }
                }
                Console.WriteLine("Done.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                foreach (IGrouping<DiagnoseCode, Lesion> group in lesionsGrouped)
                {
                    string outputDirectory = Path.Combine(allImagesDir, group.Key.ToString());

                    if (Directory.Exists(outputDirectory))
                    {
                        Directory.Delete(outputDirectory, true);
                    }
                }
            }
        }

        private static void ExtractTrainTestSets(DiagnoseCode code, int trainFilesCount, int testFilesCount)
        {
            if (trainFilesCount == 0 || testFilesCount == 0)
            {
                return;
            }

            string diagnoseAllDir = Path.Combine(Configuration.BasePath, Configuration.AllImagesDir, code.ToString());
            string diagnoseTrainDir = Path.Combine(Configuration.BasePath, Configuration.TrainImagesDir, code.ToString());
            string diagnoseTestDir = Path.Combine(Configuration.BasePath, Configuration.TestImagesDir, code.ToString());

            DirectoryInfo directoryInfoAll = new DirectoryInfo(diagnoseAllDir);
            DirectoryInfo directoryInfoTrain = new DirectoryInfo(diagnoseTrainDir);
            DirectoryInfo directoryInfoTest = new DirectoryInfo(diagnoseTestDir);

            try
            {
                FileInfo[] files = directoryInfoAll.GetFiles();

                if (trainFilesCount > files.Length)
                {
                    trainFilesCount = files.Length - testFilesCount;
                }

                Random random = new Random();
                List<FileInfo> filesToTrain = new List<FileInfo>();
                List<FileInfo> filesToTest = new List<FileInfo>();

                while (filesToTrain.Count < trainFilesCount)
                {
                    int fileIndex = random.Next(files.Length);
                    FileInfo file = files[fileIndex];

                    if (!filesToTrain.Contains(file))
                    {
                        filesToTrain.Add(file);
                    }
                }

                while (filesToTest.Count < testFilesCount)
                {
                    int fileIndex = random.Next(files.Length);
                    FileInfo file = files[fileIndex];

                    if (!filesToTest.Contains(file) && !filesToTrain.Contains(file))
                    {
                        filesToTest.Add(file);
                    }
                }

                if (!Directory.Exists(diagnoseTrainDir))
                {
                    Directory.CreateDirectory(diagnoseTrainDir);
                }

                if (!Directory.Exists(diagnoseTestDir))
                {
                    Directory.CreateDirectory(diagnoseTestDir);
                }

                DeleteExistingFiles(directoryInfoTrain);
                DeleteExistingFiles(directoryInfoTest);

                CopyFiles(filesToTrain, diagnoseTrainDir);
                CopyFiles(filesToTest, diagnoseTestDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                if (Directory.Exists(diagnoseTrainDir))
                {
                    Directory.Delete(diagnoseTrainDir, true);
                }

                if (Directory.Exists(diagnoseTestDir))
                {
                    Directory.Delete(diagnoseTestDir, true);
                }
            }
        }

        private static void CopyFiles(List<FileInfo> files, string diagnoseOutputDir)
        {
            string sourceDir = files.First().DirectoryName;
            int count = 0;
            int max = files.Count;

            Console.WriteLine($"Performing {nameof(CopyFiles)} from '{sourceDir}' to '{diagnoseOutputDir}'...");
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
            Console.WriteLine($"Copied {files.Count} files from '{sourceDir}' to '{diagnoseOutputDir}'");
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