﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Microsoft.Web.LibraryManager.Mocks;
using Microsoft.Web.LibraryManager.Mocks.CacheServices;
using Microsoft.Web.LibraryManager.Providers.Cdnjs;

namespace Microsoft.Web.LibraryManager.Test.Providers.Cdnjs
{
    [TestClass]
    public class CdnjsCatalogTest
    {
        private CdnjsCatalog Initialize()
        {
            string projectFolder = Path.Combine(Path.GetTempPath(), "LibraryManager");
            string cacheFolder = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\");
            var hostInteraction = new HostInteraction(projectFolder, cacheFolder);
            var cacheService = new FakeCdnjsCacheService();

            var provider = new CdnjsProvider(hostInteraction, cacheService:null);
            return new CdnjsCatalog(provider, cacheService, new VersionedLibraryNamingScheme());
        }

        [DataTestMethod]
        [DataRow("sample", "sampleLibrary")]
        [DataRow("test", "test-library")]
        [DataRow("test-library2", "test-library2")]
        public async Task SearchAsync_Success(string searchTerm, string expectedId)
        {
            CdnjsCatalog sut = Initialize();

            IReadOnlyList<ILibraryGroup> absolute = await sut.SearchAsync(searchTerm, 1, CancellationToken.None);
            Assert.AreEqual(1, absolute.Count);

            IEnumerable<string> versions = await absolute[0].GetLibraryVersions(CancellationToken.None);
            Assert.IsTrue(versions.Any());

            ILibrary library = await sut.GetLibraryAsync(absolute[0].DisplayName, versions.First(), CancellationToken.None);
            Assert.IsTrue(library.Files.Count > 0);
            Assert.AreEqual(expectedId, library.Name);
            Assert.AreEqual(1, library.Files.Count(f => f.Value));
            Assert.IsNotNull(library.Name);
            Assert.IsNotNull(library.Version);
            Assert.AreEqual(CdnjsProvider.IdText, library.ProviderId);
        }

        [TestMethod]
        public async Task SearchAsync_MultipleMatches()
        {
            CdnjsCatalog sut = Initialize();

            IReadOnlyList<ILibraryGroup> result = await sut.SearchAsync(term:"test", 5, CancellationToken.None);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("test-library", result.First().DisplayName);
            Assert.AreEqual("test-library2", result.Last().DisplayName);
        }

        [TestMethod]
        public async Task SearchAsync_NoHits()
        {
            CdnjsCatalog sut = Initialize();

            IReadOnlyList<ILibraryGroup> absolute = await sut.SearchAsync(@"*9)_-", 1, CancellationToken.None);

            Assert.AreEqual(0, absolute.Count);
        }

        [TestMethod]
        public async Task SearchAsync_EmptyString()
        {
            CdnjsCatalog sut = Initialize();

            IReadOnlyList<ILibraryGroup> absolute = await sut.SearchAsync("", 1, CancellationToken.None);

            Assert.AreEqual(1, absolute.Count);
        }

        [TestMethod]
        public async Task SearchAsync_NullString()
        {
            CdnjsCatalog sut = Initialize();

            IReadOnlyList<ILibraryGroup> absolute = await sut.SearchAsync(null, 1, CancellationToken.None);

            Assert.AreEqual(1, absolute.Count);
        }

        [TestMethod]
        public async Task GetLibraryAsync_Success()
        {
            CdnjsCatalog sut = Initialize();

            ILibrary library = await sut.GetLibraryAsync("sampleLibrary", "3.1.4", CancellationToken.None);

            Assert.IsNotNull(library);
            Assert.AreEqual("sampleLibrary", library.Name);
            Assert.AreEqual("3.1.4", library.Version);
            Assert.IsNotNull(library.Files);
        }

        [TestMethod]
        public async Task GetLibraryAsync_InvalidLibraryId()
        {
            CdnjsCatalog sut = Initialize();

            await Assert.ThrowsExceptionAsync<InvalidLibraryException>(async () =>
                await sut.GetLibraryAsync("invalid_id", "invalid_version", CancellationToken.None));
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_Names()
        {
            CdnjsCatalog sut = Initialize();

            CompletionSet result = await sut.GetLibraryCompletionSetAsync("test", 0);

            Assert.AreEqual(0, result.Start);
            Assert.AreEqual(4, result.Length);
            Assert.AreEqual(2, result.Completions.Count());
            Assert.AreEqual("test-library", result.Completions.First().DisplayText);
            Assert.AreEqual("test-library@1.0.0", result.Completions.First().InsertionText);
        }

        [TestMethod]
        public async Task GetLibraryCompletionSetAsync_Versions()
        {
            CdnjsCatalog sut = Initialize();

            CompletionSet result = await sut.GetLibraryCompletionSetAsync("sampleLibrary@", 14);

            Assert.AreEqual("sampleLibrary@".Length, result.Start);
            Assert.AreEqual(0, result.Length);
            Assert.AreEqual(5, result.Completions.Count());
            Assert.AreEqual("2.0.0", result.Completions.Last().DisplayText);
            Assert.AreEqual("sampleLibrary@2.0.0", result.Completions.Last().InsertionText);
        }

        [TestMethod]
        public async Task GetLatestVersion_LatestExist()
        {
            CdnjsCatalog sut = Initialize();

            const string libraryName = "sampleLibrary";
            const string expectedVersion = "3.1.4";
            string result = await sut.GetLatestVersion(libraryName, false, CancellationToken.None);

            Assert.AreEqual(expectedVersion, result);
        }

        [TestMethod]
        public async Task GetLatestVersion_PreRelease()
        {
            CdnjsCatalog sut = Initialize();

            // "twitter-bootstrap@3.3.0"
            const string libraryName = "sampleLibrary";
            const string oldVersion = "4.0.0-beta.1";
            string result = await sut.GetLatestVersion(libraryName, true, CancellationToken.None);

            Assert.AreEqual(oldVersion, result);
        }

        [TestMethod]
        public void ConvertToLibraryGroup_ValidJsonCatalog()
        {
            CdnjsCatalog sut = Initialize();
            string json = @"{""results"":[{""name"":""1140"",""latest"":""https://cdnjs.cloudflare.com/ajax/libs/1140/2.0/1140.min.css"",
""description"":""The 1140 grid fits perfectly into a 1280 monitor. On smaller monitors it becomes fluid and adapts to the width of the browser.""
,""version"":""2.0""}],""total"":1}";

            IEnumerable<CdnjsLibraryGroup> libraryGroup = sut.ConvertToLibraryGroups(json);

            Assert.AreEqual(1, libraryGroup.Count());
            CdnjsLibraryGroup library = libraryGroup.First();
            Assert.AreEqual("1140", library.DisplayName);
            Assert.AreEqual("The 1140 grid fits perfectly into a 1280 monitor. On smaller monitors it becomes fluid and adapts to the width of the browser.", library.Description);
            Assert.AreEqual("2.0", library.Version);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(@"{""results"":[12}")]
        public void ConvertToLibraryGroup_InvalidJsonCatalog(string json)
        {
            CdnjsCatalog sut = Initialize();

            IEnumerable<CdnjsLibraryGroup> libraryGroup = sut.ConvertToLibraryGroups(json);

            Assert.IsNull(libraryGroup);
        }

        [TestMethod]
        public void ConvertToAssets_ValidAsset()
        {
            CdnjsCatalog sut = Initialize();
            string json = @"{""name"":""jquery"",""filename"":""jquery.min.js"",""version"":""3.3.1"",""description"":""JavaScript library for DOM operations"",
""homepage"":""http://jquery.com/"",""keywords"":[""jquery"",""library"",""ajax"",""framework"",""toolkit"",""popular""],""namespace"":""jQuery"",
""repository"":{""type"":""git"",""url"":""https://github.com/jquery/jquery.git""},""license"":""MIT"",
""author"":{""name"":""jQuery Foundation and other contributors"",""url"":""https://github.com/jquery/jquery/blob/master/AUTHORS.txt""},
""autoupdate"":{""type"":""npm"",""target"":""jquery""},
""assets"":[{""version"":""3.3.1"",""files"":[""core.js"",""jquery.js"",""jquery.min.js"",""jquery.min.map"",""jquery.slim.js"",""jquery.slim.min.js"",""jquery.slim.min.map""]}]}";

            List<Asset> list = sut.ConvertToAssets(json);

            Assert.AreEqual(1, list.Count());
            Asset asset = list[0];
            Assert.AreEqual("3.3.1", asset.Version);

            string[] expectedFiles = new string[] { "core.js", "jquery.js", "jquery.min.js", "jquery.min.map", "jquery.slim.js", "jquery.slim.min.js", "jquery.slim.min.map" };
            Assert.AreEqual(7, asset.Files.Count());
            foreach(string file in expectedFiles)
            {
                Assert.IsTrue(asset.Files.Contains(file));
            }

            Assert.AreEqual("jquery.min.js", asset.DefaultFile);
        }

        [TestMethod]
        public void ConvertToAssets_InvalidAsset()
        {
            string json = "abcd";
            CdnjsCatalog sut = Initialize();

            List<Asset> list = sut.ConvertToAssets(json);

            Assert.IsNull(list);
        }
    }
}
