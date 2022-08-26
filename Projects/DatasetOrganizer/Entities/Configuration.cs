namespace DatasetOrganizer
{
    public class Configuration
    {
        public string BasePath { get; private set; }
        public string MetadataFilePath { get; private set; }
        public string SourceImagesPath1 { get; private set; }
        public string SourceImagesPath2 { get; private set; }
        public string AllImagesDir { get; private set; }
        public string TrainImagesDir { get; private set; }
        public string TestImagesDir { get; private set; }

        public Configuration(string basePath, string metadataFilePath, string sourceImagesPath1, string sourceImagesPath2,
            string allImagesDir, string trainImagesDir, string testImagesDir)
        {
            BasePath = basePath;
            MetadataFilePath = metadataFilePath;
            SourceImagesPath1 = sourceImagesPath1;
            SourceImagesPath2 = sourceImagesPath2;
            AllImagesDir = allImagesDir;
            TrainImagesDir = trainImagesDir;
            TestImagesDir = testImagesDir;
        }
    }
}
