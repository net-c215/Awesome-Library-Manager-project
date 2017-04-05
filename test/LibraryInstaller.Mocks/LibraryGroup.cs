﻿using LibraryInstaller.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryInstaller.Mocks
{
    /// <summary>
    /// A mock of the <see cref="ILibraryGroup"/> interface.
    /// </summary>
    /// <seealso cref="LibraryInstaller.Contracts.ILibraryGroup" />
    public class LibraryGroup : ILibraryGroup
    {
        /// <summary>
        /// The user facing display name of the library.
        /// </summary>
        public virtual string DisplayName { get; set; }

        /// <summary>
        /// The description of the library.
        /// </summary>
        public virtual string Description { get; set; }

        /// <summary>
        /// Gets a list of IDs for the different versions of the library.
        /// </summary>
        /// <param name="cancellationToken">A token that allows cancellation of the operation.</param>
        /// <returns>
        /// A list of library IDs used to display library information to the user.
        /// </returns>
        public virtual Task<IEnumerable<string>> GetLibraryIdsAsync(CancellationToken cancellationToken)
        {
            string[] ids = { "test" };
            return Task.FromResult<IEnumerable<string>>(ids);
        }
    }
}
