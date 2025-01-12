﻿using Microsoft.Xrm.Sdk;

namespace Rappen.XRM.Helpers.Interfaces
{
    /// <summary>
    /// Core object that helps to keep all objects and methods needed for CRM development in
    /// package easy to access and operate
    /// </summary>
    public interface IBag
    {
        /// <summary>
        /// Get instance of the <see cref="ILogger"/> assosiated with current container
        /// </summary>
        ILogger Logger
        {
            get;
        }

        /// <summary>
        /// Gets instance of <see cref="IOrganizationService"/> assosiated with current container
        /// </summary>
        IOrganizationService Service
        {
            get;
        }

        void Cmd(string args, string folder = null);

        void Trace(string format, params object[] args);
    }
}