namespace BuildTasks;

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class MergeNuspec : Task
{
    [Required]
    public ITaskItem[] Sources { get; set; }
    [Required]
    public ITaskItem Output { get; set; }

    public override bool Execute()
    {
        Log.LogMessage(MessageImportance.High, $"Merging {string.Join(";", (object[])Sources)} => {Output}");
        XDocument output = null;

        foreach (var input in Sources.Select(i => XDocument.Load(i.ItemSpec)))
        {
            if (output == null)
            {
                output = input;
            }
            else
            {
                Merge(output.Root, input.Root, false);
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Output.ItemSpec));
        output.Save(Output.ItemSpec);
        return true;
    }

    private void Merge(XElement target, XElement source, bool append)
    {
        Log.LogMessage(MessageImportance.Low, $"Merging {source} into {target}");
        foreach (var e in source.Elements())
        {
            if (!append && target.Element(e.Name) is { } te)
            {
                Merge(te, e, e.Name.LocalName == "dependencies" || e.Name.LocalName == "files" || e.Name.LocalName == "frameworkAssemblies");
            }
            else if (e.Name.LocalName != "file" || !target.Elements(e.Name).Any(te => te.Attribute("target")?.Value == e.Attribute("target")?.Value))
            {
                target.Add(e);
            }
        }
        Log.LogMessage(MessageImportance.Low, $"Result {target}");
    }
}
