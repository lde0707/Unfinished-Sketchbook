using System;
using System.IO;
using System.Linq;

namespace Unfin.Editor
{
    public static class PreviewOutput
    {
        // Windows previewers may memory-map the previous PNG. Replacing a completed
        // file avoids truncating that mapped file and exposing a partial new image.
        public static void Write(string path, byte[] bytes)
        {
            if(File.Exists(path)&&File.ReadAllBytes(path).SequenceEqual(bytes))return;
            string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                File.WriteAllBytes(temporary,bytes);
                if(File.Exists(path))File.Replace(temporary,path,null);
                else File.Move(temporary,path);
            }
            finally { if(File.Exists(temporary))File.Delete(temporary); }
        }
    }
}
