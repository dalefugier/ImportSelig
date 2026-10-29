# ImportSelig
ImportSelig is a Rhino file import plug-in that imports Selig-formatted airfoil data.

ImportSelig is available in Rhino via the [PackageManager](https://www.rhino3d.com/features/package-manager/).

For details on Selig airfoil data and [sample data files](https://m-selig.ae.illinois.edu/ads/coord_database.html), visit the [UIUC Airfoil Data Site](https://m-selig.ae.illinois.edu/ads.html).

### How files are imported

Each `.dat` file is imported as a single degree-3 NURBS curve that passes through every point in the file. Where possible, the data is handled the same way [XFOIL](https://web.mit.edu/drela/Public/web/xfoil/) handles it:

- The first line is the airfoil name, which is assigned to the curve. If the first line contains coordinates, the file name is used instead.
- Lines beginning with `#` or `!` are comments, and are ignored.
- The curve is interpolated using chord-length parameterization, which suits the points clustered near the leading and trailing edges.
- Points listed in clockwise order are reversed, so all curves run counterclockwise: from the trailing edge, along the upper surface to the leading edge, and back along the lower surface.
- A doubled point (two identical successive points) marks a sharp corner. The curve is interpolated separately on each side of the corner.
- Airfoils with a sharp trailing edge produce a closed curve. Airfoils with a finite trailing edge thickness produce an open curve. Use the `Line` command to close it, if needed.

Coordinates in `.dat` files are fractions of the chord, so the curve is placed in the world XZ plane with a chord length of 1 in the document's units. Scale it to the chord length you need.

Lednicer-format files and multi-element files are not supported.

### Prerequisites

ImportSelig is a Rhino 9 plug-in that targets .NET 10. The following tools are required to build ImportSelig:

- [Microsoft Visual Studio](https://visualstudio.microsoft.com/) 2026 or later.
- The [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), which is included with Visual Studio 2026.
- [Rhino 9](https://www.rhino3d.com/), to run and debug the plug-in.

Note, the solution uses the [RhinoCommon](https://www.nuget.org/packages/rhinocommon) package available on [NuGet](https://www.nuget.org/).

### Compiling

1. Clone the repository. At a command prompt, enter the following command:

```
git clone https://github.com/dalefugier/ImportSelig
```

2. Open the `ImportSelig.csproj` project file in Visual Studio.
3. Press <kbd>F7</kbd>, or click *Build > Build Solution*  to build the solution.

You can also build from a command prompt with `dotnet build`. The plug-in is written to `bin\<Configuration>\net10.0\ImportSelig.rhp`.

## License

See the file [LICENSE](https://github.com/dalefugier/ImportSelig/blob/main/LICENSE) for details.
