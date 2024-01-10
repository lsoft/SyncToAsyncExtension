global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using SyncToAsync.Extension.CodeLens;
using SyncToAsync.Shared;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using static SyncToAsync.Shared.Logging;
using Thread = System.Threading.Thread;

namespace SyncToAsync.Extension
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.ExtensionString)]
    public sealed class ExtensionPackage : ToolkitPackage
    {
        private DelayedCodeLensRefresher _refresher;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _refresher = new DelayedCodeLensRefresher();

            await SubscribeToOpenDocumentChangedAsync();

            VS.Events.DocumentEvents.BeforeDocumentWindowShow += DocumentEvents_BeforeDocumentWindowShow;

            //refresh codelenses
            //we do not wait it for its completion.
            CodeLensConnectionHandler.AcceptCodeLensConnectionsAsync()
                .FileAndForget(nameof(CodeLensConnectionHandler.AcceptCodeLensConnectionsAsync))
                ;

            _refresher.AsyncStart();
        }

        private async void DocumentEvents_BeforeDocumentWindowShow(DocumentView obj)
        {
            try
            {
                await SubscribeToOpenDocumentChangedAsync();
            }
            catch (Exception ex)
            {
                //for additional safety suppress here everything
                LogVS(ex);
            }
        }

        private async Task SubscribeToOpenDocumentChangedAsync()
        {
            var adv = await VS.Documents.GetActiveDocumentViewAsync();
            if (adv != null)
            {
                //trivial heuristic: subscribe only for C# files
                if (!adv.FilePath.EndsWith(".cs"))
                {
                    return;
                }

                adv.TextView.Closed += TextView_Closed;
                adv.TextBuffer.ChangedLowPriority += TextBufferChanged;
            }
        }

        private void TextView_Closed(object sender, EventArgs e)
        {
            var wpftv = sender as Microsoft.VisualStudio.Text.Editor.IWpfTextView;
            if(wpftv == null)
            {
                return;
            }

            var adv = wpftv.ToDocumentView();
            if (adv == null)
            {
                return;
            }

            //trivial heuristic: subscribe only for C# files
            if (!adv.FilePath.EndsWith(".cs"))
            {
                return;
            }

            try
            {
                adv.TextView.Closed -= TextView_Closed;
                adv.TextBuffer.ChangedLowPriority -= TextBufferChanged;
            }
            catch(Exception ex) 
            {
                //for additional safety suppress here everything
                LogVS(ex);
            }
        }

        public void TextBufferChanged(object sender, TextContentChangedEventArgs a)
        {
            _refresher.Delay();
        }


        private class DelayedCodeLensRefresher
        {
            private readonly AutoResetEvent _restartSignal = new AutoResetEvent(false);

            public void AsyncStart()
            {
                var t = new Thread(Work);
                t.IsBackground = true;
                t.Start();
            }

            public void Delay()
            {
                _restartSignal.Set();
            }

            private void Work()
            {
                do
                {
                    try
                    {
                        //wait for next key press, no need to infinitely refresh codelenses without text changes
                        _restartSignal.WaitOne();

                        var restart = false;
                        do
                        {
                            restart = _restartSignal.WaitOne(TimeSpan.FromSeconds(5));
                        }
                        while (restart);

                        //refresh codelenses
                        CodeLensConnectionHandler.RefreshAllCodeLensDataPointsAsync()
                            .FileAndForget(nameof(CodeLensConnectionHandler.RefreshAllCodeLensDataPointsAsync))
                            ;

                        _restartSignal.Reset();
                    }
                    catch
                    {
                        //in case of exception there will be log flood, so suppress here everything
                        //just wait a sec
                        Thread.Sleep(1000);
                    }
                }
                while (true);
            }
        }
    }
}