import '@dotnet/jsinterop/dist/Microsoft.JSInterop';
import '@browserjs/GlobalExports';
import { OutOfProcessRenderBatch } from '@browserjs/Rendering/RenderBatch/OutOfProcessRenderBatch';
import { setEventDispatcher } from '@browserjs/Rendering/RendererEventDispatcher';
import { internalFunctions as navigationManagerFunctions } from '@browserjs/Services/NavigationManager';
import { renderBatch } from '@browserjs/Rendering/Renderer';
import { decode } from 'base64-arraybuffer';
import * as ipc from './IPC';

function boot() {
  setEventDispatcher((eventDescriptor, eventArgs) => DotNet.invokeMethodAsync('Microsoft.AspNetCore.Components.Electron', 'DispatchEvent', eventDescriptor, JSON.stringify(eventArgs)));
  navigationManagerFunctions.listenForNavigationEvents((uri: string, intercepted: boolean) => {
    return DotNet.invokeMethodAsync('Microsoft.AspNetCore.Components.Electron', 'NotifyLocationChanged', uri, intercepted);
  });

  // Configure the mechanism for JS<->NET calls
  DotNet.attachDispatcher({
    beginInvokeDotNetFromJS: (callId: number, assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, argsJson: string) => {
      ipc.send('BeginInvokeDotNetFromJS', [callId ? callId.toString() : null, assemblyName, methodIdentifier, dotNetObjectId || 0, argsJson]);
    },
    endInvokeJSFromDotNet: (callId: number, succeeded: boolean, resultOrError: any) => {
      ipc.send('EndInvokeJSFromDotNet', [callId, succeeded, resultOrError]);
    }
  });

  // Wait until the .NET process says it is ready
  ipc.once('components:init', async () => {
    navigationManagerFunctions.enableNavigationInterception();

    // Confirm that the JS side is ready for the app to start
    ipc.send('components:init', [
      navigationManagerFunctions.getLocationHref().replace(/\/index\.html$/, ''),
      navigationManagerFunctions.getBaseURI()]);
  });

  ipc.on('JS.BeginInvokeJS', (_, asyncHandle, identifier, argsJson) => {
    DotNet.jsCallDispatcher.beginInvokeJSFromDotNet(asyncHandle, identifier, argsJson);
  });

  ipc.on('JS.EndInvokeDotNet', (_, callId, success, resultOrError) => {
    DotNet.jsCallDispatcher.endInvokeDotNetFromJS(callId, success, resultOrError);
  });

  ipc.on('JS.RenderBatch', (_, rendererId, batchBase64) => {
    var batchData = new Uint8Array(decode(batchBase64));
    renderBatch(rendererId, new OutOfProcessRenderBatch(batchData));
  });

  ipc.on('JS.Error', (_, message) => {
    console.error(message);
  });
}

boot();
