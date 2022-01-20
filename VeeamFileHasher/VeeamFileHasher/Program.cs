using System;

namespace VeeamFileHasher
{
    static class Program
    {
        static void Main()
        {
            try
            {
                Console.WriteLine("Please enter filePath");
                var filePath = Console.ReadLine();
                if (string.IsNullOrEmpty(filePath))
                    throw new Exception("FilePath is empty");
            
                Console.WriteLine("Please enter size blocks");
                var sizeBlocksStr = Console.ReadLine();
                if (!long.TryParse(sizeBlocksStr, out var blocksSizeInByte)) 
                    throw new Exception("Cant Parse size block");

                new FileHandler().CalcHashForFileBlocks(filePath, blocksSizeInByte);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}