# CBrease Blazor WebAssembly Deployment Checklist

## 1. Pre-Deployment

- Confirm working tree status.
- Commit and push intended changes.
- Run from the app/core workspace as appropriate:

```powershell
dotnet test
dotnet build
```

- Verify bridge geometry regression tests pass.
- Confirm there are no unexpected build errors or new warnings.

## 2. Clean Publish

Run from `CBreaseWebApp1`:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish-webapp.ps1
```

The script:

- deletes old `publish-output`,
- republishes `CBreaseWebApp1` in Release mode,
- regenerates hashed Blazor framework assets,
- regenerates `service-worker-assets.js`,
- reports the generated `Brease.Core.*.wasm` file and compact app version string.

Confirm the script reports:

- publish folder path,
- generated `Brease.Core.<hash>.wasm`,
- generated `service-worker-assets.js`,
- version/build display such as `v1.0.0 • May 12 8:01 AM`.

## 3. Deployment Upload

- Upload the contents of `publish-output/wwwroot` to the deployment target.
- Replace existing deployed files completely.
- Confirm `index.html` was updated.
- Confirm `_framework` files were updated.
- Confirm `service-worker-assets.js` was updated.

## 4. Live Verification

- Open the deployed app.
- Confirm the footer/nav version display updated, for example:

```text
v1.0.0 • May 12 8:01 AM
```

- Open browser DevTools -> Network.
- Reload the page.
- Verify the live app loads the new `Brease.Core.<hash>.wasm`.
- Verify old stale `Brease.Core.*.wasm` hashes are no longer loaded.

## 5. Service Worker / Update Verification

- Confirm the deployed `service-worker-assets.js` has the new manifest version.
- For already-open stale clients, confirm the update banner appears when the browser detects the new service worker.
- Click reload in the update banner.
- Confirm the app reloads and shows the new footer/nav version.

## 6. Troubleshooting

- Stale browser cache: hard refresh or clear site data, then reload.
- Stale service worker: unregister the service worker in DevTools -> Application -> Service Workers, then reload.
- Old `publish-output` reuse: rerun `.\scripts\publish-webapp.ps1`; do not deploy from an old folder.
- Incorrect `BaseHref`: confirm deployed `index.html` uses `/cbreasefield/` for the current target.
- Partial upload: replace the full contents of `publish-output/wwwroot`, including `_framework`, `service-worker.js`, and `service-worker-assets.js`.
- Browser still loads older `Brease.Core.*.wasm`: verify the deployed `index.html`, `_framework` folder, and `service-worker-assets.js` all came from the same clean publish output.
