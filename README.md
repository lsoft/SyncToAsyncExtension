# SyncToAsyncExtension

A Visual Studio extension (VSIX) which creates codelenses allowed to goto to sync sibling method for async methods and vice-versa even if sibling method is in different file or code generated (for example, via [source generator](https://github.com/zompinc/sync-method-generator)).

![Image 1](example0.png)

When you types the codelenses are in watinig state. After few second after you stopped typing, the codelenses are recalculated. This timeout can be found in `Sync <-> Async` options tab in VS options window.

You can download this extension from [VS marketplace](https://marketplace.visualstudio.com/items?itemName=lsoft.SyncToAsyncExtension).
