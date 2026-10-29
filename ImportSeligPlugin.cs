using Rhino;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.PlugIns;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace ImportSelig
{
  public class ImportSeligPlugin : FileImportPlugIn
  {
    /// <summary>
    /// Constructor
    /// </summary>
    public ImportSeligPlugin()
    {
      Instance = this;
    }

    /// <summary>
    /// Gets the only instance of this plug-in.
    /// </summary>
    public static ImportSeligPlugin Instance { get; private set; }

    /// <summary>
    /// Defines file extensions that this import plug-in is designed to read.
    /// </summary>
    /// <param name="options">Options that specify how to read files.</param>
    /// <returns>A list of file types that can be imported.</returns>
    protected override FileTypeList AddFileTypes(FileReadOptions options)
    {
      var fileTypeList = new Rhino.PlugIns.FileTypeList();
      fileTypeList.AddFileType("Selig Airfoil Data (*.dat)", "dat");
      return fileTypeList;
    }

    /// <summary>
    /// Called when a user requests to import a .dat file.
    /// </summary>
    /// <param name="filename">The complete path to the new file.</param>
    /// <param name="index">The index of the file type as it had been specified by the AddFileTypes method.</param>
    /// <param name="doc">The document to be written.</param>
    /// <param name="options">Options that specify how to write file.</param>
    /// <returns>A value that defines success or a specific failure.</returns>
    protected override bool ReadFile(string filename, int index, RhinoDoc doc, FileReadOptions options)
    {
      bool rc = false;
      try
      {
        // Selig Airfoil data files are just text files.
        // The first line in the file should contain the name of the airfoil.
        // If it does not, the file name is used instead.
        // The rest of the lines should contain two space-delimited coordinates.
        // We treat these coordinates as x, z.
        // Points run from the trailing edge, along the upper surface to the
        // leading edge, and back along the lower surface to the trailing edge.
        // Lines beginning with '#' or '!' are comments.
        // Where possible, we handle the data the same way XFOIL does.

        var lines = File.ReadAllLines(filename);
        if (null != lines && lines.Length > 4)
        {
          var name = string.Empty;
          var points = new List<Point3d>();
          var delimiters = new char[] { ' ', '\t' };

          // Data files always use '.' as the decimal separator.
          var style = NumberStyles.Float;
          var culture = CultureInfo.InvariantCulture;

          for (var i = 0; i < lines.Length; i++)
          {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line) || "#!".IndexOf(line[0]) >= 0)
              continue;

            var elements = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            if (2 == elements.Length && double.TryParse(elements[0], style, culture, out var x) && double.TryParse(elements[1], style, culture, out var z))
            {
              if (string.IsNullOrEmpty(name))
                name = Path.GetFileNameWithoutExtension(filename);

              points.Add(new Point3d(x, 0.0, z));
            }
            else if (string.IsNullOrEmpty(name))
            {
              name = line;
            }
          }

          if (points.Count > 3)
          {
            // Reverse points listed in clockwise order.
            var area = 0.0;
            for (var i = 0; i < points.Count; i++)
            {
              var p0 = points[i];
              var p1 = points[(i + 1) % points.Count];
              area += 0.5 * (p0.Z + p1.Z) * (p0.X - p1.X);
            }
            if (area < 0.0)
              points.Reverse();

            // A doubled point marks a sharp corner, so interpolate the points
            // between doubled points separately.
            // Use non-periodic curves so the trailing edge stays sharp.
            // Chord length knots suit the points clustered near the edges.
            // Airfoils with a finite trailing edge thickness are left open.
            var segments = new List<Curve>();
            var start = 0;
            for (var i = 1; i <= points.Count; i++)
            {
              if (i == points.Count || points[i] == points[i - 1])
              {
                var count = i - start;
                if (count > 1)
                {
                  var segment = Curve.CreateInterpolatedCurve(points.GetRange(start, count), 3, CurveKnotStyle.Chord);
                  if (null != segment)
                    segments.Add(segment);
                }
                start = i;
              }
            }

            Curve curve = null;
            if (1 == segments.Count)
            {
              curve = segments[0];
            }
            else if (segments.Count > 1)
            {
              var polycurve = new PolyCurve();
              foreach (var segment in segments)
                polycurve.Append(segment);
              curve = polycurve;
            }

            if (null != curve)
            {
              var attributes = doc.CreateDefaultAttributes();
              attributes.Name = name;
              var id = doc.Objects.AddCurve(curve, attributes);
              if (id != Guid.Empty)
                rc = true;
            }
          }
        }
      }
      catch (Exception ex)
      {
        RhinoApp.WriteLine(ex.Message);
      }

      if (!rc)
        RhinoApp.WriteLine("Error reading Selig Airfoil Data.");

      doc.Views.Redraw();

      return rc;
    }
  }
}