﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace JsonConverter
{
    partial class Program
    {
        private void ConvertResource(string file)
        {
            var (outputActivitiesLGFile, outputTextsLGFile) = GetOutputLGFile(file);
            var sbActivities = new StringBuilder();
            var sbTexts = new StringBuilder();
            sbTexts.AppendLine($"[import] ({Path.GetFileName(outputActivitiesLGFile)})").AppendLine();
            var doc = XDocument.Load(file);
            var nodes = doc.Root.Elements("data");
            foreach (var node in nodes)
            {
                var templateName = node.Attribute("name").Value;
                var value = node.Element("value").Value;
                var activity = new Activity
                {
                    Replies = new List<Reply>() { new Reply { Text = value, Speak = value } },
                };
                activity.Correct();
                AddActivity(sbActivities, templateName, activity);
                AddTexts(sbTexts, templateName, activity, false);
            }

            var locale = GetLocale(file);
            Convert(locale, outputActivitiesLGFile, sbActivities, outputTextsLGFile, sbTexts);

            if (!options.KeepOld && locale == options.DefaultLocale)
            {
                var name = GetDialogName(file);
                name = Path.Join(Path.GetDirectoryName(file), name + ".Designer.cs");
                DeleteFile(name);
            }
        }

        public void ConvertResourceFilesToLG(params string[] folders)
        {
            var responseFolder = GetFullPath(folders);
            var jsonFiles = Directory.GetFiles(responseFolder, "*.resx", SearchOption.AllDirectories);
            foreach (var file in jsonFiles)
            {
                ConvertResource(file);
                if (!options.KeepOld)
                {
                    DeleteFile(file);
                }
            }

            if (jsonFiles.Length > 0)
            {
                haveDone.AppendLine("* Create lg files from resource");
                help.AppendLine("* Use LocaleTemplateEngineManager.GetString for all resx strings");

                if (!options.UpdateProject)
                {
                    help.AppendLine("* Change 'Copy to Output Directory' to 'Copy if newer' for lg files from resource");
                    if (!options.KeepOld)
                    {
                        help.AppendLine("* Delete resx files from project manually");
                    }
                }
                else
                {
                    haveDone.AppendLine("* Change 'Copy to Output Directory' to 'Copy if newer' for lg files from resource");
                }

                if (!options.KeepOld)
                {
                    haveDone.AppendLine("* Delete resx files");
                }
            }
        }
    }
}