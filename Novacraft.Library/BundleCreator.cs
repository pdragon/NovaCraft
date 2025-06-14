using System;
using System.IO;
using System.Linq;

namespace Novacraft.Library
{
    public class BundleCreator
    {
        public void CreateInstaller(string launcherExe, string zipPath, string outputPath)
        {
            byte[] launcherBytes = File.ReadAllBytes(launcherExe);
            byte[] zipBytes = File.ReadAllBytes(zipPath);
            byte[] sizeBytes = BitConverter.GetBytes((long)launcherBytes.Length);

            if (zipBytes.Length < 1024)
            {
                throw new InvalidDataException("Error1: File too small");
            }

            File.WriteAllBytes(outputPath, CombineArrays(
                launcherBytes,
                zipBytes,
                sizeBytes
            ));
        }

        private byte[] CombineArrays(params byte[][] arrays)
        {
            int totalLength = arrays.Sum(a => a.Length);
            byte[] result = new byte[totalLength];
            int offset = 0;

            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, result, offset, array.Length);
                offset += array.Length;
            }

            return result;
        }
    }
}
