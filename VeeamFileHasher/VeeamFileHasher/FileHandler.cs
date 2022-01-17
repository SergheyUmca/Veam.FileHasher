using System;
using System.Threading;

namespace VeeamFileHasher
{
    public class FileHandler
    {
        public void CalcHashForFileBlocks(string filePath, long blockSize)
        {
            try
            {
                var fileSize = new System.IO.FileInfo(filePath).Length;
                
                var blockCounts = fileSize / blockSize;
                var lastBlockSize = fileSize % blockSize;
                
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (lastBlockSize > 0)
                    blockCounts ++;
                
                long lastBlockByte = 0;
                for (var i = 0; i < blockCounts; i++)
                {
                    var endBlockByte = i < blockCounts -1 ? lastBlockByte + blockSize : lastBlockByte + lastBlockSize;

                    new Thread(new FileBlockService().FileBlockCalcHash)
                        .Start(new FileBlockServiceRequest
                        {
                            BlockNumber = i,
                            EndByte = endBlockByte,
                            StartByte = lastBlockByte + 1,
                            FilePath = filePath
                        });
                    
                    lastBlockByte = endBlockByte + 1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}