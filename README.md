# o2jam_converter

Just a couple of utilities designed to make working with o2jam files easier. Currently an implementation for dumping ojm files (as per the ojm documentation on the open2jam blogspot) and also some work on ojn headers. 

This currently has working osz conversion for OGG only o2jam files (which is most of the files anyways). There currently isn't an interface for this, so you'll have to put your conversion directory manually in the debug file.

## Usage

Currently the program has a CLI interface. You can use it by the following syntax. It will try to autodetect keysounded files, and use virtual mode on osu! when necessary. 

```
O2JamDebug.exe --help
  -i, --input=VALUE          the input directory
  -o, --output=VALUE         output beatmaps folder
  -r, --renderpath=VALUE     path for external audio renderer
  -z, --ziposz.              zip the contents at the end
  -h, --help                 show this message and exit
```

Example usage:

```
.\O2JamCLI.exe -i D:\Games\o2servers\new\o2ma2438.ojn -o D:\temp\output\ -z
```

