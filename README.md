# o2jam_converter

Just a couple of utilities designed to make working with o2jam files easier. Currently an implementation for dumping ojm files (as per the ojm documentation on the open2jam blogspot) and also some work on ojn headers. 

This currently has working osz conversion for OGG only o2jam files (which is most of the files anyways). There is currently a CLI interface as shown below, but possibly plans for a GUI if needed. 

This tool is more geared towards non-keysounded ojm files, and doesn't work as well for keysounded files (at least your mileage may vary).

## Usage

Currently the program has a CLI interface. You can use it by the following syntax. It will try to autodetect keysounded files, and use virtual mode on osu! when necessary. 

If you have ffmpeg and want to reduce the size of fmod dumped wav files, you can use the -f flag as follows.

Batch processing can be done with the powershell script in the o2jam_cli project

```
O2JamCLI.exe --help
  -i, --input=VALUE          the input directory
  -o, --output=VALUE         output beatmaps folder
  -f, --useffmpeg            use ffmpeg to encode mp3
  -z, --ziposz.              zip the contents at the end
  -h, --help                 show this message and exit
```

Example usage:

```
.\O2JamCLI.exe -i D:\Games\o2servers\new\o2ma2438.ojn -o D:\temp\output\ -z
```

## Dependencies

- [fmod (fmod.dll & fmodstudio.dll)](https://www.fmod.com/)

- [SupersonciSound (Included in c# project)](https://github.com/martindevans/SupersonicSound)

- [ndesk (Included in c# project)](https://www.nuget.org/packages/NDesk.Options/)

## Known Issues

- Wav files not rendering correctly
