﻿using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace ASPXUpdater
{
    /// <summary>
    /// Meta utility script to update project ASPX files from our bootstrap exports.<br/>
    /// HTML must be in ./BSD/HTML/.<br/>
    /// ASPX must be in ./AdminSystem/.<br/>
    /// must be run in the same folder as the Skeleton.sln.<br/>
    /// </summary>
    class ASPXUpdater
    {
        private static bool DEBUG => Debugger.IsAttached;
        private static String EXE_RELATIVE => DEBUG ? "./" : "../../../../";
        private static readonly String BOOTSTRAP_LOC        = EXE_RELATIVE + "BSD/";
        private static readonly String ASPX                 = EXE_RELATIVE + "AdminSystem/";
        private static readonly String SLN                  = EXE_RELATIVE + "Skeleton.sln";
        private static readonly String GEN_COMMENT          = "\n\n<!-- " +
                                                              "\nThis ASPX was generated by a purpose built internal Monday 9AM Ensemble development utility." +
                                                              "\nThe HTML content was generated by bootstrap studio." +
                                                              "\n\nIt looks dreadful in the Visual Studio preview, but fine when hosted in a browser." +
                                                              "\nThe bootstrap front end seems perfectly compatable." +
                                                              "\n-->\n\n";

        [Obsolete]
        private static readonly String ASSETS = "assets/";
        private static readonly String ASSETS_INPUT = BOOTSTRAP_LOC + ASSETS;
        private static readonly String ASSETS_OUTPUT = ASPX + ASSETS;
        private static readonly List<String> Logs = new List<string>();
        private static readonly String LOG_LOC = "./ASPX UPDATER LASTEST.log";

        private class ASPXGenException : Exception { public ASPXGenException(String message) : base("Failed to generate ASPX: " + message) { } }

        static void LogAndWrite(String s)
        {
            Logs.Add(s);
            Console.WriteLine(s);
        }

        static void Info(String s) => LogAndWrite("[INFO] " + s);
        static void Error(String s) => LogAndWrite("[ERR] " + s);
        static void Success(String s) => LogAndWrite("[PASS] " + s);
        static void Bar(String s) => LogAndWrite(BR + "==================" + s +"===================" + BR);
        static String BR => "\n\n";

        static String ABS(String s) => Path.GetFullPath(s);
        static void Main(string[] args)
        {
            HTMLToASPConverter.runTest();
            Bar("Startup");
            if (DEBUG) Info("Debugger attached!\n Will work in debug directory!\n Real project will not be modified!");
           
            Info("Will attempt to work at solution " + ABS(SLN));

            Info("Checking is in project..  ");
            AssertInProject();

            Success("Is in project!");
            UpdateAllASPX();

            Success("Finished updating ASPX. Updating " + ABS(ASSETS_OUTPUT));
            UpdateAssets();

            Success("Finished!");
            if (DEBUG)
            {
                Bar("Paused for debug");
                Info("Attached to debugger. Press 'continue' or 'stop' in Visual Studio.");
                Debugger.Break();
                Info("Continue!");
            }
            Bar("Idle. Press any key to Halt.");
            Console.ReadKey();
            WriteLog();
        }

        /// <summary>
        /// Generates a new ASPX file for every HTML found in the design location
        /// </summary>
        private static void UpdateAllASPX()
        {
            Info("Getting all HTML and ASPX files..");
            string[] HTMLFiles = GetAllHTML();
            Success("Loaded " + HTMLFiles.Length + " files from " + ABS(BOOTSTRAP_LOC));

            string[] ASPXFiles = GetAllASPX();
            Success("Loaded " + ASPXFiles.Length + " files from " + ABS(ASPX));

            Bar("Checking HTML");
            Compare(HTMLFiles, ASPXFiles, "HTML", "has no matching ASPX. It will fail, and be ignored.", ASPX, "aspx");

            Bar("Checking ASPX");
            Compare(ASPXFiles, HTMLFiles, "ASPX", "has no matching HTML in the design. It will not be updated.", BOOTSTRAP_LOC, "html");


            Bar("Begin ASPX gen");
            foreach (String HTMLFile in HTMLFiles)
            {
                try
                {
                    ProcessFile(HTMLFile);
                    Success("Updated ASPX for " + HTMLFile);
                }
                catch (Exception e)
                {
                    Error("Failed to process " + HTMLFile + ".");
                    Error(e.Message);
                    Error("STACKTRACE : \n" + e.StackTrace);
                }
            }
            Bar("End ASPX gen");
        }

        private static void ProcessFile(String HTMLFile)
        {
            String ASPXFile = ASPX + Path.GetFileNameWithoutExtension(HTMLFile) + ".aspx";
            String ASPXContent;
            try
            {
                ASPXContent = File.ReadAllText(ASPXFile);
            }
            catch (FileNotFoundException)
            {
                throw new ASPXGenException("'" + HTMLFile + "' had no corresponding ASPX file to update. Generate one in Visual Studio, or check file names and paths.");
            }

            File.WriteAllText(ASPXFile, HTMLToASPConverter.parse(CompileASPX(ReadASPXMeta(ASPXContent), HTMLFile)));
        }

        /// <summary>
        /// Uses the sln file to assert that the pwd is the project root.
        /// </summary>
        private static void AssertInProject()
        {
            if (File.Exists(SLN))
                return;
            else
                throw new ASPXGenException("Program wasn't run in parallel to solution");
        }

        /// <summary>
        /// When parsed the context of an ASPX file, returns the meta found on line 1.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static String ReadASPXMeta(String file)
        {
            String line = file.Split('\n')[0];
            if (Regex.Match(line, "<%@.*%>").Success)
                return line;
            else
                throw new ASPXGenException("[err] ASPX File '" + file + "' did not begin with ASPX decleration");
        }

        /// <summary>
        /// Reads a file, and returns the text it contains.<br/>
        /// Wrapper for File.ReadAllText.
        /// </summary>
        /// <param name="Path"></param>
        /// <returns></returns>
        private static String PathToText(String Path)
        {
            return File.ReadAllText(Path);
        }

        /// <summary>
        /// Compiles a new ASPX file with the provided meta, and html loaded from the file at the provided path.
        /// </summary>
        /// <param name="Meta"></param>
        /// <param name="HTMLPath"></param>
        /// <returns></returns>
        private static String CompileASPX(String Meta, String HTMLPath)
        {
            return (Meta + GEN_COMMENT + '\n' + PathToText(HTMLPath));
        }

        private static void UpdateAssets()
        {
            if (Directory.Exists(ASSETS_OUTPUT))
            {
                Info("Assets folder already exists in target. Deleting it...");
                DeleteDirectory(ASSETS_OUTPUT);
            }
            Info("Copying over new assets folder...");
            DirectoryCopy(ASSETS_INPUT, ASSETS_OUTPUT);
            Success("Done!");
        }

        public static void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, false);
        }

        /// <summary>
        /// 100% stolen from the documentation.
        /// </summary>
        /// <param name="sourceDirName"></param>
        /// <param name="destDirName"></param>
        /// <param name="copySubDirs"></param>
        private static void DirectoryCopy(string sourceDirName, string destDirName)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            foreach (DirectoryInfo subdir in dirs)
            {
                string tempPath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath);
            }
        }

        static String[] GetAllASPX() => GetAllByExtention(ASPX, "aspx");
        static String[] GetAllHTML() => GetAllByExtention(BOOTSTRAP_LOC, "html");
        

        static String[] GetAllByExtention(String path, String Extention)
        {
            String[] paths = Directory.GetFiles(ABS(path));
            ArrayList Paths = new ArrayList();
            foreach (String s in paths)
                if (s.EndsWith('.' + Extention)) Paths.Add(s);

            return (String[]) Paths.ToArray(typeof(String));
        }

        static void Compare(String[] IN, String[] OUT, String preMsg, String postMsg, String outURI, String outExtention)
        {
            foreach (String s in IN)
                if (!Contains(OUT, ABS(outURI + Path.GetFileNameWithoutExtension(s) + '.' +outExtention))) 
                    Info(preMsg + " '" + Path.GetFileNameWithoutExtension(s) + "' " + postMsg);
        }

        static bool Contains(String[] arr, String el)
        {
            foreach (String s in arr)
                if (ABS(s).Equals(el)) return true;

            return false;
        }

        static void WriteLog() => File.WriteAllText(LOG_LOC, ArrayToString((String[])Logs.ToArray()));

        static String ArrayToString(String[] arr)
        {
            String line =  "";
            foreach (String s in arr)
                line += s + '\n';

            return line;
        }
    }

    class HTMLToASPConverter
    {
        public static void runTest()
        {
            String s = "<label class=\"col - form - label\" >[myLabel:This Is A Label]</label>" +
                "<label class=\"col - form - label\" >[myLabel:This Is A Label]</label>" +
                "<label class=\"col - form - label\" >[myLabel1:This Is Not a Label ;)]</label>" +
                "<label class=\"col - form - label\" >[myLabel2:This Is Also A Label]</label>" +
                "<p>[myLabel3:This a paragraph label]</p>";
            String r = parse(s);
        }
        static readonly String ID_TEXT_PATTERN = "[[].*:.*[\\]]";
        static readonly String ID_PATTERN = "__ID__";
        static readonly String TEXT_PATTERN = "__TEXT__";
        static volatile String current_tag_pattern = "";
        static volatile String current_replace_pattern = "";
        static readonly MatchEvaluator TagEvaluator = new MatchEvaluator(ReplaceTag);

        static readonly String[][] TagPatterns = new String[][]
        {
           new String[]{ "<label[^>]*>[^<]*</label>|<label[^/]*/>|<p>[^<]*</p>", "<asp:Label ID=\"__ID__\" runat=\"server\" Text=\"__TEXT__\"></asp:Label>" },
           new String[]{ "<button[^>]*>[^<]*</button>|<button[^/]*/>", "<asp:Button ID=\"__ID__\" runat=\"server\" Text=\"__TEXT__\"></asp:Button>" },
           new String[]{ "<input type=\"text\" value=\"[^>]*\">", "<asp:TextBox runat=\"server\" ID=\"__ID__\">__TEXT__</asp:TextBox>" }
           // <div class="form-check"> <input class="form-check-input" type="checkbox" id="formCheck-1"><label class="form-check-label" for="formCheck-1">Label</label></div>
        };

        public static string parse(String s)
        {
            String content = s;
            foreach (String[] patts in TagPatterns)
            {
                current_tag_pattern = patts[0];
                current_replace_pattern = patts[1];
                content = Regex.Replace(content, current_tag_pattern, TagEvaluator);
            }

            return content;
        }

        public static string ReplaceTag(Match m)
        {
            String line = m.Value;
            String[] ValID = GetTextID(line);
            String current_ID = ValID[0];
            String current_text = ValID[1];

            String newLine = current_replace_pattern;
            newLine = Regex.Replace(newLine, ID_PATTERN, current_ID);
            newLine = Regex.Replace(newLine, TEXT_PATTERN, current_text);

            return newLine;
        }


        private static String[] GetTextID(String s)
        {
            String TextID = Regex.Match(s, ID_TEXT_PATTERN).Value;
            String ID = Regex.Match(TextID, "[[].*:").Value;
            ID = ID.Substring(1, ID.Length - 2);

            String Text = Regex.Match(TextID, ":.*]").Value;
            Text = Text.Substring(1, Text.Length - 2);

            return new String[] { ID, Text };
        }


        static readonly String LabelStyleTag = "<label [^>]*>[^<]*</label>|<label[^/]*/>";
        static readonly String LabelStyleReplace = "<asp:Label ID=\"__ID__\" runat=\"server\" Text=__TEXT__\"\"></asp:Label>";
        private static String[] generateLabelStyleTag(String component)
        {
            String replace = Regex.Replace(LabelStyleReplace, "Label", component);
            return new string[]
            {
                Regex.Replace(LabelStyleTag, "label", component).ToLower()
                ,
                replace.Substring(0,1).ToUpper() + replace.Substring(1).ToLower()
            };

        }


    }
}
