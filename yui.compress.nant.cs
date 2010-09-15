using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using System.Xml;
using System.Globalization;
using NAnt.Core.Tasks;
using Yahoo.Yui.Compressor;

/*
Author:
  Aleem Bawany (2009)

Description:
  NAnt task for YUI Compressor

Usage:
  <yuicompressor todir="${build.dir}">
    <fileset basedir="javascripts">
       <include name="jquery.js" />
       <include name="base.js" />
    </fileset>
  </yuicompressor>

License:
  New BSD License
  http://www.opensource.org/licenses/bsd-license.php

Based on:
  http://code.google.com/p/devfuelnet/
  http://yuicompressor.codeplex.com/

*/

namespace AleemB.YuiCompress
{
    [TaskName("yuicompressor")]
    public class YuiCompressorTask : Task
    {
        #region Private Fields

        private DirectoryInfo m_ToDir;
        private bool m_Flatten;
        private FileSet m_InputFileSet = new FileSet();

        #endregion

        #region Public Properties

        [TaskAttribute("todir", Required = true)]
        public virtual DirectoryInfo ToDirectory
        {
            get { return m_ToDir; }
            set { m_ToDir = value; }
        }

        [TaskAttribute("flatten")]
        [BooleanValidator()]
        public virtual bool Flatten
        {
            get { return m_Flatten; }
            set { m_Flatten = value; }
        }

        [BuildElement("fileset", Required = true)]
        public virtual FileSet InputFileSet
        {
            get { return m_InputFileSet; }
            set { m_InputFileSet = value; }
        }

        #endregion

        [Obsolete]
        protected override void InitializeTask(XmlNode taskNode)
        {
            if (m_ToDir == null)
                throw new BuildException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The 'todir' attribute must be set to specify the output directory of the minified JS/CSS files."),
                    this.Location);

            if (!m_ToDir.Exists)
                m_ToDir.Create();

            if (m_InputFileSet == null)
                throw new BuildException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The <fileset> element must be used to specify the JS/CSS files to Compress."),
                    Location);
        }

        protected override void ExecuteTask()
        {
            if (m_InputFileSet.BaseDirectory == null)
                m_InputFileSet.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);

            Log(Level.Info, "Compressing {0} JavaScript/CSS file(s) to '{1}'.", m_InputFileSet.FileNames.Count, m_ToDir.FullName);          

            foreach (string srcPath in m_InputFileSet.FileNames)
            {
                FileInfo srcFile = new FileInfo(srcPath);

                if (srcFile.Exists)
                {
                    string destPath = GetDestPath(m_InputFileSet.BaseDirectory, srcFile);

                    DirectoryInfo destDir = new DirectoryInfo(Path.GetDirectoryName(destPath));

                    if (!destDir.Exists)
                        destDir.Create();

                    Log(Level.Verbose, "Compressing '{0}' to '{1}'.", srcPath, destPath);

                    string fileText = File.ReadAllText(srcPath);
                    if (Path.GetExtension(srcPath).ToLower() == ".css")
                    {
                        File.WriteAllText(destPath, CssCompressor.Compress(fileText));
                    }
                    else if (Path.GetExtension(srcPath).ToLower() == ".js")
                    {
                        File.WriteAllText(destPath, JavaScriptCompressor.Compress(fileText, Verbose));
                    }
                    else
                    {
                        throw new BuildException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Expected a .css or .js extension. Got \"{0}\"",
                            Path.GetFileName(srcPath)),
                        Location);
                    }
                }
                else
                {
                    throw new BuildException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Could not find file '{0}' to Compress.",
                            srcFile.FullName),
                        Location);
                }
            }
        }

        private string GetDestPath(DirectoryInfo srcBase, FileInfo srcFile)
        {
            string destPath = string.Empty;

            if (m_Flatten)
            {
                destPath = Path.Combine(m_ToDir.FullName, srcFile.Name);
            }
            else
            {
                if (srcFile.FullName.IndexOf(srcBase.FullName, 0) != -1)
                    destPath = srcFile.FullName.Substring(srcBase.FullName.Length);
                else
                    destPath = srcFile.Name;

                if (destPath[0] == Path.DirectorySeparatorChar)
                    destPath = destPath.Substring(1);

                destPath = Path.Combine(m_ToDir.FullName, destPath);
            }

            return destPath;
        }
    }
}
