# UnzipForAlignRT

A small .NET console utility that bridges the Varian Ethos treatment system to the
AlignRT surface-guidance system. Ethos drops zipped DICOM exports onto a network
share; this program watches that share, unzips each archive, keeps only the relevant
files, and forwards them to AlignRT over DICOM.

## How it works

The program runs as a continuous loop:

1. Watches one or more directories (default `\\ro-ariaimg-v\va_data$\ETHOS\AlignRT`;
   additional paths can be listed one-per-line in a `FilePaths.txt` file beside the
   executable).
2. Extracts each `*.zip` it finds, waiting for the file to finish transferring
   (checks for file locks) before unzipping.
3. Deletes everything except RT Plan (`RP*`) and RT Structure Set (`RS*`) DICOM
   files, then moves the extracted folder into a `Finished` subfolder.
4. Sends each remaining `.dcm` file to the AlignRT DICOM node via a C-STORE and
   deletes it once transferred.

The DICOM destination (AE title, IP, port) is currently hard-coded in
`Services/UploadDicomClass.cs` (AE title `AlignRT7`), with the local SCU configured
as AE title `DCMTK`. Adjust these constants for a different site or node.

## Requirements

- .NET 5.0
- [EvilDICOM](https://www.nuget.org/packages/EvilDICOM) 2.0.6.5 (DICOM C-STORE SCU),
  restored automatically via NuGet

## Usage

Build and run the console app (`UnzipForAlignRT.sln`). It runs indefinitely, polling
the configured folders. Stop it by closing the console.

Site-specific, not generalized: network paths and DICOM node details are hard-coded
and would need editing for another installation.
