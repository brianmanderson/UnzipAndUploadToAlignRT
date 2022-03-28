using System;
using System.Threading;
using System.IO;
using System.IO.Compression;
using UnzipForAlignRT.Services;
using System.Collections.Generic;

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
        static List<string> default_file_paths = new List<string> { @"\\ro-ariaimg-v\va_data$\ETHOS\AlignRT" };
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
            foreach (string zip_file in all_files)
            {
                FileInfo zip_file_info = new FileInfo(zip_file);
                bool move_on = false;
                int tries = 0;
                Thread.Sleep(3000);
                while (IsFileLocked(zip_file_info))
                {
                    Console.WriteLine("Waiting for file to be fully transferred...");
                    tries += 1;
                    Thread.Sleep(3000);
                    if (tries > 2)
                    {
                        move_on = true;
                        break;
                    }
                }
                if (move_on)
                {
                    Console.WriteLine("Taking too long, will come back and try again...");
                    continue;
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
        static void UploadDicom(UploadDicomClass dicom_uploader, string dicom_directory)
        {
            string[] all_files = Directory.GetFiles(dicom_directory, "**.dcm", SearchOption.AllDirectories);
            foreach (string file in all_files)
            {
                FileInfo dcm_file_info = new FileInfo(file);
                bool move_on = false;
                int tries = 0;
                Thread.Sleep(3000);
                while (IsFileLocked(dcm_file_info))
                {
                    Console.WriteLine("Waiting for file to be fully transferred...");
                    tries += 1;
                    Thread.Sleep(3000);
                    if (tries > 5)
                    {
                        move_on = true;
                        break;
                    }
                }
                if (move_on)
                {
                    Console.WriteLine("Taking too long, will come back and try again...");
                    continue;
                }
                dicom_uploader.AddDicomFile(file);
                File.Delete(file);
            }
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Running...");
            UploadDicomClass dicom_uploader = new UploadDicomClass();
            while (true)
            {
                List<string> file_paths = new List<string> { };
                foreach (string file_path in default_file_paths)
                {
                    file_paths.Add(file_path);
                }

                string file_paths_file = Path.Join(".", $"FilePaths.txt");
                if (File.Exists(file_paths_file))
                {
                    try
                    {
                        string all_file_paths = File.ReadAllText(file_paths_file);
                        foreach (string file_path in all_file_paths.Split("\r\n"))
                        {
                            if (!file_paths.Contains(file_path))
                            {
                                file_paths.Add(file_path);
                            }
                        }
                    }
                    catch
                    {
                        Console.WriteLine("Couldn't read the FilePaths.txt file...");
                        Thread.Sleep(3000);
                    }
                }
                // First lets unzip the life images
                foreach (string file_path in file_paths)
                {
                    Thread.Sleep(3000);
                    try
                    {
                        if (Directory.Exists(file_path))
                        {
                            UnzipFiles(file_path);
                            UploadDicom(dicom_uploader, Path.Join(file_path, "Finished"));
                        }
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
