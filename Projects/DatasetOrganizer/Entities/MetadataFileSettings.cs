namespace DatasetOrganizer
{
    public class MetadataFileSettings
    {
        public string LesionIdColumnName { get; private set; }
        public string ImageIdColumnName { get; private set; }
        public string DxColumnName { get; private set; }
        public string DxTypeColumnName { get; private set; }
        public string AgeColumnName { get; private set; }
        public string SexColumnName { get; private set; }
        public string LocalizationColumnName { get; private set; }

        public MetadataFileSettings(string lesionIdColumnName, string imageIdColumnName, string dxColumnName, string dxTypeColumnName, string ageColumnName, string sexColumnName, string localizationColumnName)
        {
            LesionIdColumnName = lesionIdColumnName;
            ImageIdColumnName = imageIdColumnName;
            DxColumnName = dxColumnName;
            DxTypeColumnName = dxTypeColumnName;
            AgeColumnName = ageColumnName;
            SexColumnName = sexColumnName;
            LocalizationColumnName = localizationColumnName;
        }
    }
}
