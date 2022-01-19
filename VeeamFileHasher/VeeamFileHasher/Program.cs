using System;

namespace VeeamFileHasher
{
    class Program
    {
        static void Main()
        {
            try
            {
                
// #if DEBUG
//                   new FileHandler().CalcHashForFileBlocks(@"C:\Users\User\Downloads\Сквозь горизонт.1997.Blu-Ray.Remux.1080p.mkv", 524288000);           
// #endif
//#if !DEBUG
                Console.WriteLine("Please enter filePath");
                var filePath = Console.ReadLine();
                if (string.IsNullOrEmpty(filePath))
                    throw new Exception("FilePath is empty");
            
                Console.WriteLine("Please enter size blocks");
                var sizeBlocksStr = Console.ReadLine();
                if (!long.TryParse(sizeBlocksStr, out var blocksSizeInByte)) 
                    throw new Exception("Cant Parse size block");

                new FileHandler().CalcHashForFileBlocks(filePath, blocksSizeInByte);
//#endif
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}