using System;
using System.IO;
using EvilDICOM.Core;
using EvilDICOM.Network;
using EvilDICOM.Network.Enums;
using EvilDICOM.Network.SCUOps;

namespace UnzipForAlignRT.Services
{
    class UploadDicomClass
    {
        //Store the details of the daemon (Ae Title, IP, port)
        static Entity daemon = new Entity("AlignRT7", "172.19.121.41", 105);
        //Store the details of the client (Ae Title, port) -> IP address is determined by CreateLocal() method
        static Entity local = Entity.CreateLocal("DCMTK", 50600);
        //Set up a client (DICOM SCU = Service Class User)
        static DICOMSCU client = new DICOMSCU(local);
        static CStorer storer = client.GetCStorer(daemon);
        static ushort msgId = 1;
        public void AddDicom(string storagePath)
        {

            string[] dcmFiles = Directory.GetFiles(storagePath, "*.dcm", SearchOption.AllDirectories);
            foreach (string path in dcmFiles)
            {
                //Reads DICOM object into memory
                var dcm = DICOMObject.Read(path);
                var response = storer.SendCStore(dcm, ref msgId);
                //Write results to console
                Console.WriteLine($"DICOM C-Store from {local.AeTitle} => " +
                        $"{daemon.AeTitle} @{daemon.IpAddress}:{daemon.Port}:" +
                        $"{(Status)response.Status}");
            }
            Console.Read(); //Stop here
        }
        public void AddDicomFile(string path)
        {
            var dcm = DICOMObject.Read(path);
            var response = storer.SendCStore(dcm, ref msgId);
            //Write results to console
            Console.WriteLine($"DICOM C-Store from {local.AeTitle} => " +
                    $"{daemon.AeTitle} @{daemon.IpAddress}:{daemon.Port}:" +
                    $"{(Status)response.Status}");
        }
    }
}
