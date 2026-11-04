# Local client-side assets

The MVC application references browser assets from `wwwroot`; pages do not load CSS or JavaScript from a CDN at runtime.

Already included in the repository:
- Bootstrap 5.3.3 LTR and RTL CSS: `wwwroot/lib/bootstrap/dist/css/`
- Bootstrap bundle: `wwwroot/lib/bootstrap/dist/js/bootstrap.bundle.min.js`
- jQuery: `wwwroot/lib/jquery/dist/jquery.min.js`
- Application CSS and JavaScript: `wwwroot/css/` and `wwwroot/js/`

## Restore the local SignalR browser client once

The SignalR browser client is pinned in `libman.json`. From the MVC project directory, run:

```powershell
libman restore
```

Or in Visual Studio, right-click `libman.json` and choose **Restore Client-Side Libraries**.

This places the pinned SignalR 10.0.0 browser file under `wwwroot/lib/signalr/dist/browser/`. The chat page references that local file. The restore requires internet access once; after the file has been restored, the application does not request SignalR, Bootstrap, jQuery, CSS, or application JavaScript from external hosts while running.

If the `libman` command is unavailable, install the Library Manager CLI once:

```powershell
dotnet tool install -g Microsoft.Web.LibraryManager.Cli
```
