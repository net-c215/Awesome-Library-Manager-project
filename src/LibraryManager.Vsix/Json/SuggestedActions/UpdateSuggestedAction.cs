﻿using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Text;
using Microsoft.Web.Editor.SuggestedActions;
using Microsoft.Web.LibraryManager.Contracts;
using System;
using System.Linq;
using System.Threading;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal class UpdateSuggestedAction : SuggestedActionBase
    {
        private static readonly Guid _guid = new Guid("b3b43e69-7d0a-4acf-99ea-015526f76d84");
        private readonly SuggestedActionProvider _provider;
        private readonly string _updatedLibraryId;
        private readonly bool _disabled;

        public UpdateSuggestedAction(SuggestedActionProvider provider, string libraryId, string displayText, bool disabled = false)
            : base(provider.TextBuffer, provider.TextView, displayText, _guid)
        {
            _provider = provider;
            _updatedLibraryId = libraryId;
            _disabled = disabled;

            if (!disabled)
            {
                IconMoniker = KnownMonikers.StatusReady;
            }
        }

        public override void Invoke(CancellationToken cancellationToken)
        {
            Telemetry.TrackUserTask("Invoke-UpdateSuggestedAction");

            if (_disabled)
            {
                return;
            }

            try
            {
                var dependencies = Dependencies.FromConfigFile(_provider.ConfigFilePath);
                IProvider provider = dependencies.GetProvider(_provider.InstallationState.ProviderId);
                ILibraryCatalog catalog = provider?.GetCatalog();

                if (catalog == null)
                {
                    return;
                }

                JSONMember member = _provider.LibraryObject.Children.OfType<JSONMember>().FirstOrDefault(m => m.UnquotedNameText == ManifestConstants.Library);

                if (member != null)
                {
                    using (ITextEdit edit = TextBuffer.CreateEdit())
                    {
                        edit.Replace(new Span(member.Value.Start, member.Value.Length), "\"" + _updatedLibraryId + "\"");
                        edit.Apply();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogEvent(ex.ToString(), LogLevel.Error);
                Telemetry.TrackException("UpdateSuggestedActionFailed", ex);
            }
        }
    }
}
