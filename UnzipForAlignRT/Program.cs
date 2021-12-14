using System;
using System.Threading;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using FellowOakDicom;

namespace UnzipForAlignRT
{
    public class FileWatcher
    {
        private bool folder_changed;
        public bool Folder_Changed
        {
            get
            {
                return folder_changed;
            }
            set
            {
                folder_changed = value;
            }
        }
        public FileWatcher(string directory)
        {
            FileSystemWatcher file_watcher = new FileSystemWatcher(directory);
            file_watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;
            file_watcher.Changed += OnChanged;
            file_watcher.EnableRaisingEvents = true;
        }
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                folder_changed = false;
                return;
            }
            folder_changed = true;
        }
    }
    class Program
    {
        public bool folder_changed;
        static string[] file_paths = { @"\\ro-ariaimg-v\va_data$\ETHOS\AlignRT" };
        static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;
            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
        static void DeleteUnneededFiles(string dicom_directory)
        {
            string[] all_files = Directory.GetFiles(dicom_directory, "*.dcm");
            foreach (string file in all_files)
            {
                if (Path.GetFileName(file).StartsWith("RP") | (Path.GetFileName(file).StartsWith("RS")))
                {
                    continue;
                }
                else
                {
                    File.Delete(file);
                }
            }
        }
        static void MoveFolder(string moving_directory, string current_folder)
        {
            string folder_name = Path.GetFileName(current_folder);
            if (!Directory.Exists(moving_directory))
            {
                Directory.CreateDirectory(moving_directory);
            }
            Directory.Move(current_folder, Path.Combine(moving_directory, folder_name));
        }
        static void UnzipFiles(string zip_file_directory)
        {
            string[] all_files = Directory.GetFiles(zip_file_directory, "*.zip");
            string overall_status;
            foreach (string zip_file in all_files)
            {
                FileInfo zip_file_info = new FileInfo(zip_file);
                Thread.Sleep(3000);
                while (IsFileLocked(zip_file_info))
                {
                    Console.WriteLine("Waiting for file to be fully transferred...");
                    Thread.Sleep(3000);
                }
                string file_name = Path.GetFileName(zip_file);
                string output_dir = Path.Join(Path.GetDirectoryName(zip_file), file_name.Substring(0, file_name.Length - 4));
                if (!Directory.Exists(output_dir))
                {
                    Directory.CreateDirectory(output_dir);
                    Console.WriteLine("Extracting...");
                    ZipFile.ExtractToDirectory(zip_file, output_dir);
                    Console.WriteLine("Deleting other files...");
                    DeleteUnneededFiles(output_dir);
                    File.Delete(zip_file);
                }
                MoveFolder(moving_directory: Path.Join(zip_file_directory, "Finished"), current_folder: output_dir);
                Console.WriteLine("Running...");
                Thread.Sleep(3000);
            }
        }
        static void CheckDownPath(string file_path)
        {
            if (Directory.Exists(file_path))
            {
                UnzipFiles(file_path);
            }
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Running...");
            while (true)
            {
                // First lets unzip the life images
                foreach (string file_path in file_paths)
                {
                    Thread.Sleep(3000);
                    try
                    {
                        CheckDownPath(file_path);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
        }
    }
}
