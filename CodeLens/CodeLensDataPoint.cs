using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens.Remoting;
using Microsoft.VisualStudio.Threading;
using SyncToAsync.Shared;
using SyncToAsync.Shared.Dto;
using static SyncToAsync.Shared.Logging;

namespace SyncToAsync.CodeLens
{
    public class CodeLensDataPoint : IAsyncCodeLensDataPoint, IDisposable
    {
        private readonly ICodeLensCallbackService _callbackService;
        private readonly CodeLensDescriptor _descriptor;
        
        private RemoteCodeLensConnectionHandler? _visualStudioConnection;
        private readonly ManualResetEventSlim _dataHasLoaded = new ManualResetEventSlim(initialState: false);
        
        private SiblingInformationContainer? _siblingInfo;

        public CodeLensDataPoint(
            ICodeLensCallbackService callbackService,
            CodeLensDescriptor descriptor
            )
        {
            if (callbackService is null)
            {
                throw new ArgumentNullException(nameof(callbackService));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            _callbackService = callbackService;
            _descriptor = descriptor;
        }

        public event AsyncEventHandler? InvalidatedAsync;

        public CodeLensDescriptor Descriptor => this._descriptor;

        public Guid UniqueIdentifier
        {
            get;
        } = Guid.NewGuid();

        #region network related code

        internal async Task ConnectToVisualStudioAsync(
            int vspid
            )
        {
            _visualStudioConnection = await RemoteCodeLensConnectionHandler
                .CreateAsync(owner: this, vspid)
                .ConfigureAwait(false)
                ;
        }

        // Called from VS via JSON RPC.
        public void Refresh()
        {
            Invalidate();
        }

        public void Dispose()
        {
            _visualStudioConnection?.Dispose();
            _dataHasLoaded.Dispose();
        }

        #endregion

        public async Task<CodeLensDataPointDescriptor> GetDataAsync(CodeLensDescriptorContext context, CancellationToken token)
        {
            try
            {
                _siblingInfo = await GetSiblingInformation(context, token);
                _dataHasLoaded.Set();

                if (_siblingInfo == null)
                {
                    var response = new CodeLensDataPointDescriptor()
                    {
                        Description = $"No sibling found.",
                        TooltipText = $"Sync-Async actual status",
                        IntValue = null, // no int value
                        //ImageId = GetTypeIcon(),
                    };

                    return response;
                }

                if (_siblingInfo.Disabled)
                {
                    var response = new CodeLensDataPointDescriptor()
                    {
                        Description = $"Sync<->Async: DISABLED",
                        TooltipText = $"Sync-Async actual status",
                        IntValue = null, // no int value
                        //ImageId = GetTypeIcon(),
                    };

                    return response;
                }

                if (_siblingInfo.Idle)
                {
                    var response = new CodeLensDataPointDescriptor()
                    {
                        Description = $"Sync<->Async: Idle",
                        TooltipText = $"Sync-Async actual status",
                        IntValue = null, // no int value
                        //ImageId = GetTypeIcon(),
                    };

                    return response;
                }

                if (_siblingInfo.Sibling is null)
                {
                    var response = new CodeLensDataPointDescriptor()
                    {
                        Description = $"No sibling found",
                        TooltipText = $"Sync-Async actual status",
                        IntValue = null, // no int value
                        //ImageId = GetTypeIcon(),
                    };

                    return response;
                }

                {
                    var response = new CodeLensDataPointDescriptor()
                    {
                        Description = $"Go to " + _siblingInfo.Title,
                        TooltipText = $"Sync-Async actual status",
                        IntValue = null, // no int value
                        //ImageId = GetTypeIcon(),
                    };

                    return response;
                }
            }
            catch (Exception ex)
            {
                LogCL(ex);
                throw;
            }
        }


        public async Task<CodeLensDetailsDescriptor> GetDetailsAsync(CodeLensDescriptorContext context, CancellationToken token)
        {
            try
            {
                // When opening the details pane, the data point is re-created leaving `data` uninitialized. VS will
                // then call `GetDataAsync()` and `GetDetailsAsync()` concurrently.
                if (!_dataHasLoaded.Wait(timeout: TimeSpan.FromSeconds(.5), token))
                {
                    _siblingInfo = await GetSiblingInformation(context, token);
                }

                var result = new CodeLensDetailsDescriptor()
                {
                    Headers = CreateHeaders(),
                    Entries = CreateEntries(),
                    CustomData =
                        _siblingInfo != null && !_siblingInfo.Disabled && _siblingInfo.Sibling != null
                            ? new List<object>() { _siblingInfo }
                            : new List<object>(),
                    PaneNavigationCommands = new List<CodeLensDetailPaneCommand>()
                    {
                    },
                };

                return result;
            }
            catch (Exception ex)
            {
                LogCL(ex);
                throw;
            }
        }

        /// <summary>
        /// Raises <see cref="IAsyncCodeLensDataPoint.Invalidated"/> event.
        /// </summary>
        /// <remarks>
        ///  This is not part of the IAsyncCodeLensDataPoint interface.
        ///  The data point source can call this method to notify the client proxy that data for this data point has changed.
        /// </remarks>
        public void Invalidate()
        {
            _dataHasLoaded.Reset();
            this.InvalidatedAsync?.Invoke(this, EventArgs.Empty).ConfigureAwait(false);
        }


        private async Task<SiblingInformationContainer?> GetSiblingInformation(
            CodeLensDescriptorContext context,
            CancellationToken token
            )
        {
            SiblingInformationContainer? result = null;

            try
            {
                var d = new Dictionary<string, string>();
                foreach(var pair in context.Properties)
                {
                    d[pair.Key.ToString()] = pair.Value.ToString();
                }

                var methodName = _descriptor.ElementDescription;
                var liofd = methodName.LastIndexOf(".");
                if(liofd > 0 && liofd < (methodName.Length - 1))
                {
                    methodName = methodName.Substring(liofd + 1);
                }
                d["MethodName"] = methodName;

                result = await _callbackService
                    .InvokeAsync<SiblingInformationContainer>(
                        this,
                        nameof(ICodeLensListener.GetSiblingInformation),
                        new object[]
                        {
                            new CodeLensTarget(
                                _descriptor.ProjectGuid,
                                _descriptor.FilePath,
                                d,
                                context.ApplicableSpan.HasValue ? context.ApplicableSpan.Value.Start : (int?)null,
                                context.ApplicableSpan.HasValue ? context.ApplicableSpan.Value.Length : (int?)null
                                )
                        },
                        token
                        )
                    .ConfigureAwait(false)
                    ;
            }
            catch (Exception ex)
            {
                LogCL(ex);
            }

            return result;
        }

        private static IEnumerable<CodeLensDetailEntryDescriptor> CreateEntries()
        {
            yield break;
        }

        private static List<CodeLensDetailHeaderDescriptor> CreateHeaders()
        {
            return new List<CodeLensDetailHeaderDescriptor>()
            {
            };
        }

    }
}
