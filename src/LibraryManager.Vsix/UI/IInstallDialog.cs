﻿using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Vsix.UI
{
    /// <summary>
    /// Test contract for add client side libraries dialog.
    /// </summary>
    public interface IInstallDialog
    {
        string Library { get; set; }

        Task ClickInstallAsync();

        bool IsAnyFileSelected { get; }
    }
}
