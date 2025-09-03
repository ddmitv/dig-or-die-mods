using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace SaveTool;

static class Extensions {
    public static string ToLowerFirstChar(this string input) {
        return char.ToLower(input[0]) + input.Substring(1);
    }
}

enum Action {
    Compress,
    Decompress,
    Convert,
}
enum SaveVersion {
    V0_13,
    V0_25,
}

class ParsedArgs {
    public Action? action = null;
    public string? path = null;
    public string? output = null;
    public bool force = false;
    public bool openInImHex = false;
    public bool printHelp = false;
    public SaveVersion? saveVersion = null;
}
static class Program {
    static ParsedArgs ParseArgs(string[] args) {
        ParsedArgs result = new();

        for (int i = 0; i < args.Length; ++i) {
            switch (args[i]) {
            case "--compress" or "-c":
                result.action = Action.Compress;
                break;
            case "--decompress" or "-d":
                result.action = Action.Decompress;
                break;
            case "--convert" or "-u":
                result.action = Action.Convert;
                break;
            case "--output" or "-o":
                if (i + 1 >= args.Length) {
                    throw new ArgumentException("Expected argument after '--output' or '-o'");
                }
                result.output = args[i + 1];
                i += 1;
                break;
            case "--force" or "-f":
                result.force = true;
                break;
            case "--imhex":
                result.openInImHex = true;
                break;
            case "-h" or "--help":
                result.printHelp = true;
                break;
            case "-v0.13":
                result.saveVersion = SaveVersion.V0_13;
                break;
            case "-v0.25":
                result.saveVersion = SaveVersion.V0_25;
                break;
            default:
                if (args[i].StartsWith("-")) {
                    throw new ArgumentException($"Invalid option '{args[i]}'");
                }
                if (result.path is not null) {
                    throw new ArgumentException("Expected only one input file");
                }
                result.path = args[i];
                break;
            }
        }
        if (result.printHelp) {
            return result;
        }
        if (result.action is null) {
            throw new ArgumentException($"Expected '--compress'/'-c' or '--decompress'/'-d' or '--convert'/'-u'");
        }
        if (result.path is null) {
            throw new ArgumentException($"Expected path to the compressed/decompressed/convert save file");
        }

        return result;
    }
    static void PrintHelp() {
        Console.WriteLine("""
Usage: save-tool <source> [options]
    <source>           Path to the input file.
    -h --help          Show this message.
    -c --compress      Compress the <source> file with LZF algorithm.
    -d --decompress    Decompress the <source> file with LZF algorithm.
    -u --convert       Convert old save file format to new one.
    -o --output        Specify the location of the output file explicitly.
                       By default, it will use this template:
                       '<path>/<filename>.save' if compressing, '<path>/<filename>.uncompressed-save' if decompressing.
    -f --force         Will not overwrite existing files unless this option is specified.
    --imhex            Open the output file in the ImHex. Will use ImHex at 'C:/ProgramFiles/ImHex/imhex-gui.exe'.
"""
    );
    }

    static string GetOutputPathExtensions(Action action) {
        return action switch {
            Action.Compress or Action.Convert => ".save",
            Action.Decompress => ".uncompressed-save",
            _ => throw new InvalidEnumArgumentException()
        };
    }
    static void OpenInImHex(string path) {
        string programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string imHexPath = Path.Combine(programFilesPath, "ImHex/imhex-gui.exe");
        if (!File.Exists(imHexPath)) {
            throw new FileNotFoundException("Failed to find ImHex", imHexPath);
        }
        Process.Start(imHexPath, $"\"{path}\"");
    }
    static string GetOutputPath(ParsedArgs args) {
        string resultPath = args.output ?? Path.ChangeExtension(args.path, GetOutputPathExtensions(args.action!.Value));
        if (Directory.Exists(resultPath)) {
            string resultPathFileName = Path.GetFileName(args.path);
            resultPath = Path.Combine(resultPath, Path.ChangeExtension(resultPathFileName, GetOutputPathExtensions(args.action!.Value)));
        }
        string backupPath = resultPath + ".backup";
        if (!args.force && File.Exists(resultPath) && !File.Exists(backupPath)) {
            Console.WriteLine($"Created backup for previous version of the file: '{backupPath}'");
            File.Move(resultPath, backupPath);
        }
        return resultPath;
    }

    static int PerformAction(ParsedArgs args, byte[] rawData) {
        switch (args.action) {
        case Action.Decompress: {
            string resultPath = GetOutputPath(args);

            byte[]? decompressedData = Utils.CLZF2.Decompress(rawData);
            if (decompressedData is null) {
                Console.Error.WriteLine("Decompression error: EINVAL");
                return 1;
            }
            try {
                File.WriteAllBytes(resultPath, decompressedData);
            } catch (Exception exception) {
                Console.Error.WriteLine($"File writing error: {exception.Message.ToLowerFirstChar()}");
                return 1;
            }

            Console.WriteLine($"Successfully decompressed at '{Path.GetFullPath(resultPath)}'");
            if (args.openInImHex) {
                OpenInImHex(Path.GetFullPath(resultPath));
            }
            break;
        }
        case Action.Compress: {
            string resultPath = GetOutputPath(args);

            byte[] compressedData = Utils.CLZF2.Compress(rawData);
            if (compressedData is null) {
                Console.Error.WriteLine("Compression error: EINVAL");
                return 1;
            }
            try {
                File.WriteAllBytes(resultPath, compressedData);
            } catch (Exception exception) {
                Console.Error.WriteLine($"File writing error: {exception.Message.ToLowerFirstChar()}");
                return 1;
            }
            Console.WriteLine($"Successfully compressed at '{Path.GetFullPath(resultPath)}'");
            break;
        }
        case Action.Convert: {
            string resultPath = GetOutputPath(args);

            using var saveReader = new BinaryReader(new MemoryStream(rawData));
            Data.GameState? gameState = null;
            if (args.saveVersion == SaveVersion.V0_25) {
                gameState = SaveTool.V0_25_Converter.Deserialize(saveReader);
            } else if (args.saveVersion == SaveVersion.V0_13) {
                gameState = SaveTool.V0_13_Converter.Deserialize(saveReader);
            } else {
                throw new InvalidEnumArgumentException();
            }
            byte[] compressedData = SaveTool.V1_11_Converter.Serialize(gameState);

            try {
                File.WriteAllBytes(resultPath, compressedData);
            } catch (Exception exception) {
                Console.Error.WriteLine($"File writing error: {exception.Message.ToLowerFirstChar()}");
                return 1;
            }
            Console.WriteLine($"Successfully converter save at '{Path.GetFullPath(args.path)}' to '{Path.GetFullPath(resultPath)}'");
            break;
        }
        default:
            throw new InvalidEnumArgumentException();
        }
        return 0;
    }

    static int Main(string[] args) {
        // Display exception messages in English
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

        ParsedArgs parsedArgs;
        if (args.Length == 0) {
            PrintHelp();
            return 0;
        }
        try {
            parsedArgs = ParseArgs(args);
        } catch (Exception exception) {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
        if (parsedArgs.printHelp) {
            PrintHelp();
            return 0;
        }

        byte[] rawData;
        try {
            rawData = File.ReadAllBytes(parsedArgs.path);
        } catch (Exception exception) {
            Console.Error.WriteLine($"File reading error: {exception.Message.ToLowerFirstChar()}");
            return 1;
        }

        return PerformAction(parsedArgs, rawData);
    }
}

