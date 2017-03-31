using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibraryInstaller.Contracts;
using LibraryInstaller.Mocks;
using LibraryInstaller.Providers.Cdnjs;
using LibraryInstaller.Providers.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibraryInstaller.Test
{
    [TestClass]
    public class ManifestTest
    {
        private string _filePath;
        private string _cacheFolder;
        private string _projectFolder;
        private IDependencies _dependencies;

        [TestInitialize]
        public void Setup()
        {
            _cacheFolder = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\");
            _projectFolder = Path.Combine(Path.GetTempPath(), "LibraryInstaller");
            _filePath = Path.Combine(_projectFolder, "library.json");

            var hostInteraction = new HostInteraction(_projectFolder, _cacheFolder);
            _dependencies = new Dependencies(hostInteraction, new CdnjsProvider(), new FileSystemProvider());

            Directory.CreateDirectory(_projectFolder);
            File.WriteAllText(_filePath, _doc);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(_projectFolder, true);
        }

        [TestMethod]
        public async Task InstallLibraryAsync()
        {
            var manifest = new Manifest(_dependencies);

            IProvider provider = _dependencies.GetProvider("cdnjs");
            var desiredState = new LibraryInstallationState
            {
                LibraryId = "jquery@3.1.1",
                ProviderId = "cdnjs",
                Path = "lib",
                Files = new[] { "jquery.min.js" }
            };

            ILibraryInstallationResult result = await provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(result.Success);

            manifest.AddLibrary(desiredState);
            await manifest.SaveAsync(_filePath, CancellationToken.None).ConfigureAwait(false);

            Manifest newManifest = await Manifest.FromFileAsync(_filePath, _dependencies, CancellationToken.None).ConfigureAwait(false);

            Assert.IsTrue(File.Exists(_filePath));
            Assert.AreEqual(manifest.Libraries.Count, newManifest.Libraries.Count);
            Assert.AreEqual(manifest.Version, newManifest.Version);
        }

        [TestMethod]
        public async Task RestoreLibrariesAsync()
        {
            var manifest = Manifest.FromJson(_doc, _dependencies);
            IEnumerable<ILibraryInstallationResult> result = await manifest.RestoreAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(1, result.Count(v => v.Success));
        }

        [TestMethod]
        public async Task RestoreUnknownProviderAsync()
        {
            var dependencies = new Dependencies(_dependencies.GetHostInteractions());
            var manifest = Manifest.FromJson(_doc, dependencies);
            IEnumerable<ILibraryInstallationResult> result = await manifest.RestoreAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(2, result.Count(v => !v.Success));
        }

        [TestMethod]
        public async Task RestoreCancelledAsync()
        {
            var manifest = Manifest.FromJson(_doc, _dependencies);
            var source = new CancellationTokenSource();
            source.Cancel();
            IEnumerable<ILibraryInstallationResult> result = await manifest.RestoreAsync(source.Token);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.ElementAt(0).Cancelled);
        }

        [TestMethod]
        public void FromMalformedJson()
        {
            var manifest = Manifest.FromJson("{", _dependencies);
            Assert.IsNull(manifest);
        }

        [TestMethod]
        public async Task FromFileNotFoundAsync()
        {
            Manifest manifest = await Manifest.FromFileAsync(@"c:\file\not\found.json", _dependencies, CancellationToken.None);
            Assert.IsNotNull(manifest);
            Assert.AreEqual(0, manifest.Libraries.Count);
            Assert.AreEqual("1.0", manifest.Version);
        }

        private const string _doc = @"{
  ""version"": ""1.0"",
  ""packages"": [
    {
      ""id"": ""jquery@3.1.1"",
      ""provider"": ""cdnjs"",
      ""path"": ""lib"",
      ""files"": [ ""jquery.js"", ""jquery.min.js"" ]
    },
    {
      ""id"": ""../path/to/file.txt"",
      ""provider"": ""filesystem"",
      ""path"": ""lib"",
      ""files"": [ ""file.txt"" ]
    }
  ]
}
";
    }
}
