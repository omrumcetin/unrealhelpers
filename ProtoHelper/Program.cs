using System;

internal class Program
{
    private static void Main(string[] args)
    {
        string path = AppDomain.CurrentDomain.BaseDirectory;
        var files = Directory.GetFiles(path, "*.cc", SearchOption.AllDirectories);
        char[] Buffer = new char[10000];

        foreach (var file in files)
        {
            File.Copy(file, file + ".orig");
            string renamedFile = file + ".orig";
            using (StreamReader sr = new StreamReader(renamedFile))
            using (StreamWriter sw = new StreamWriter(file, false))
            {
                sw.WriteLine("#include \"EnableGrpcIncludes.h\"");
                int read;
                while ((read = sr.Read(Buffer, 0, Buffer.Length)) > 0) 
                    sw.Write(Buffer, 0, read);

                sw.Write("#include \"DisableGrpcIncludes.h\"");
            }

            File.Delete(renamedFile);
        }
    }
}