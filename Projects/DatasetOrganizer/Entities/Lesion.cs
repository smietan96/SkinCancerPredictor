using Enums;

namespace DatasetOrganizer
{
    internal class Lesion
    {
        public string IdLesion { get; private set; }
        public string IdImage { get; private set; }
        public DiagnoseCode DiagnoseCode { get; private set; }
        public DiagnoseType DiagnoseType { get; private set; }

        //available for being blank in metadata file
        public int? Age { get; private set; }
        public Sex Sex { get; private set; }
        public LesionLocalization LesionLocalization { get; private set; }
        public string FilePath { get; private set; }

        public Lesion(string idLesion, string idImage, DiagnoseCode diagnoseCode, DiagnoseType diagnoseType, int? age, Sex sex, LesionLocalization lesionLocalization, string filePath)
        {
            IdLesion = idLesion;
            IdImage = idImage;
            DiagnoseCode = diagnoseCode;
            DiagnoseType = diagnoseType;
            Age = age;
            Sex = sex;
            LesionLocalization = lesionLocalization;
            FilePath = filePath;
        }

        public override string ToString()
        {
            return $"IdLesion: {IdLesion}, IdImage: {IdImage}, DiagnoseCode: {DiagnoseCode}, DiagnoseType: {DiagnoseType}, Age: {Age}, Sex: {Sex}, LesionLocalization: {LesionLocalization}";
        }
    }
}
