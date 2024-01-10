﻿using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using SyncToAsync.Shared;
using StreamJsonRpc;

namespace SyncToAsync.CodeLens
{
    /// <summary>
    /// Taken from  https://github.com/bert2/microscope completely.
    /// Take a look to that repo, it's amazing!
    /// </summary>
    public class RemoteCodeLensConnectionHandler : IRemoteCodeLens, IDisposable
    {
        private readonly CodeLensDataPoint _owner;
        private readonly NamedPipeClientStream _stream;
        private JsonRpc? _rpc;

        public async static Task<RemoteCodeLensConnectionHandler> CreateAsync(CodeLensDataPoint owner, int vspid)
        {
            var handler = new RemoteCodeLensConnectionHandler(owner, vspid);
            await handler.ConnectAsync().ConfigureAwait(false);
            return handler;
        }

        public RemoteCodeLensConnectionHandler(CodeLensDataPoint owner, int vspid)
        {
            _owner = owner;
            _stream = new NamedPipeClientStream(
                serverName: ".",
                CodeLensPipeName.Get(vspid),
                PipeDirection.InOut,
                PipeOptions.Asynchronous
                );
        }

        public void Dispose() => _stream.Dispose();

        public void Refresh() => _owner.Refresh();

        private async Task ConnectAsync()
        {
            await _stream.ConnectAsync().ConfigureAwait(false);
            _rpc = JsonRpc.Attach(_stream, this);
            await _rpc.InvokeAsync(nameof(IRemoteVisualStudioCodeLens.RegisterCodeLensDataPoint), _owner.UniqueIdentifier).ConfigureAwait(false);
        }

    }
}
