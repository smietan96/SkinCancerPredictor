using Enums;
using System.Collections.Generic;

namespace SkinCancerPredictor
{
    internal static class Constants
    {
        internal static readonly Dictionary<DiagnoseCode, string> DiagnoseDescriptionDictionary = new Dictionary<DiagnoseCode, string>
        {
            { DiagnoseCode.akiec, "Bowen's disease" },
            { DiagnoseCode.bcc, "Basal cell carcinoma" },
            { DiagnoseCode.bkl, "Benign keratosis-like lesions" },
            { DiagnoseCode.df, "Dermatofibroma" },
            { DiagnoseCode.mel, "Melanoma" },
            { DiagnoseCode.nv, "Melanocytic nevi" },
            { DiagnoseCode.vasc, "Vascular lesions" },
        };

        internal static readonly Dictionary<DiagnoseType, string> DiagnoseTypeDescriptionDictionary = new Dictionary<DiagnoseType, string>
        {
            { DiagnoseType.histo, "Histopathology" },
            { DiagnoseType.follow_up, "Follow-up examination" },
            { DiagnoseType.consensus, "Expert consensus" },
            { DiagnoseType.confocal, "In-vivo confocal microscopy" },
        };
    }
}