using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class GuidDuplicationTest
    {
        [Test]
        public void AllMetaFilesHaveUniqueGUIDs()
        {
            var guids = new Dictionary<string, string>();
            var metaFiles = Directory.GetFiles(Application.dataPath, "*.meta", SearchOption.AllDirectories);

            foreach (var filePath in metaFiles)
            {
                var lines = File.ReadAllLines(filePath);
                var guidLine = lines.FirstOrDefault(line => line.StartsWith("guid: "));

                if (guidLine != null)
                {
                    var guid = guidLine.Substring("guid: ".Length).Trim();

                    if (guids.ContainsKey(guid))
                    {
                        Assert.Fail($"Duplicate GUID found:\n" +
                                    $"GUID: {guid}\n" +
                                    $"File 1: {guids[guid]}\n" +
                                    $"File 2: {filePath}");
                    }
                    else
                    {
                        guids.Add(guid, filePath);
                    }
                }
            }

            Assert.Pass("All .meta files have unique GUIDs.");
        }
    }
}
