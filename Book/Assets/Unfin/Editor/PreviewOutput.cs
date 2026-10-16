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
            string previous=temporary+".previous.tmp";
            try
            {
                File.WriteAllBytes(temporary,bytes);
                // Retain the old file during the swap: Windows image previewers may
                // still have it memory-mapped and reject replacing it with no backup.
                if(File.Exists(path))File.Replace(temporary,path,previous);
                else File.Move(temporary,path);
            }
            finally
            {
                if(File.Exists(temporary))File.Delete(temporary);
                try { if(File.Exists(previous))File.Delete(previous); }
                catch(IOException) { /* A previewer can release this ignored backup later. */ }
            }
        }
    }
}
