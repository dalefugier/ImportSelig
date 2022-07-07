using Rhino;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.PlugIns;
using System;
using System.Collections.Generic;
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
        var lines = File.ReadAllLines(filename);
        if (null != lines && lines.Length > 2)
        {
          var name = lines[0].Trim();
          var points = new List<Point3d>();
          var delimiters = new char[] { ' ', '\t' };

          for (var i = 1; i <lines.Length; i++)
          {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
              continue;

            var elements = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            if (null == elements || 2 != elements.Length)
              continue;

            if (double.TryParse(elements[0], out var x) && double.TryParse(elements[1], out var z))
            {
              var point = new Point3d(x, 0.0, z);
              points.Add(point);
            }
          }

          if (points.Count > 2)
          {
            var curve = Curve.CreateInterpolatedCurve(points, 3);
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
      catch (Exception)
      {
        // TODO...
      }

      if (!rc)
        RhinoApp.WriteLine("Error reading Selig Airfoil Data.");

      doc.Views.Redraw();

      return rc;
    }
  }
}