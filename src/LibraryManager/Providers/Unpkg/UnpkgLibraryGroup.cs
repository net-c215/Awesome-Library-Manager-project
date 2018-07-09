﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    internal class UnpkgLibraryGroup : ILibraryGroup
    {
        public UnpkgLibraryGroup(string displayName, string description = null)
        {
            DisplayName = displayName;
            Description = description;
        }
        public string DisplayName { get; }

        public string Description { get; }

        public async Task<IEnumerable<string>> GetLibraryIdsAsync(CancellationToken cancellationToken)
        {
            NpmPackageInfo npmPackageInfo = await NpmPackageInfoCache.GetPackageInfoAsync(DisplayName, CancellationToken.None);

            if (npmPackageInfo != null)
            {
                return npmPackageInfo.Versions
                    .OrderByDescending(v => v)
                    .Select(semanticVersion => LibraryIdToNameAndVersionConverter.Instance.GetLibraryId(DisplayName, semanticVersion.ToString(), UnpkgProvider.IdText))
                    .ToList();
            }

            return Enumerable.Empty<string>();
        }
    }
}
