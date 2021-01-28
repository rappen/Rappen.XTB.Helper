using Rappen.XTB.Helpers.Controls;

namespace Rappen.XTB.Helpers.Interfaces
{
    public interface IXRMRecordControl
    {
        XRMRecordHost RecordHost { get; set; }
        string Column { get; set; }
        void PopulateFromRecord();
    }
}
