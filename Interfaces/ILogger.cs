namespace Rappen.XTB.Helpers.Interfaces
{
    using System;

    public interface ILogger
    {
        #region Public Methods

        void EndSection();

        void Log(string message);

        void Log(Exception ex);

        void StartSection(string name = null);

        #endregion Public Methods
    }
}