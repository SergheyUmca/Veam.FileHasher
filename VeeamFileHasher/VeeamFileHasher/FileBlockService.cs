using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace VeeamFileHasher
{
    public class FileBlockService
    {
        private readonly ConcurrentDictionary<long, ThreadStatus> _bypassDictionary;
        private long _blockNumber;
        
        public FileBlockService(ConcurrentDictionary<long, ThreadStatus> bypassDictionary)
        {
            _bypassDictionary = bypassDictionary;
        }
        
        public async void FileBlockCalcHash(object request)
        {
            try
            {
                var requestUnboxing = (FileBlockServiceRequest)request;
                _blockNumber = requestUnboxing.BlockNumber;
                _bypassDictionary.TryUpdate(requestUnboxing.BlockNumber, ThreadStatus.START, ThreadStatus.INIT);
                
                var blockFileBytes = new List<byte>();
                var sizeBlock = requestUnboxing.SizeBlock;
                
                await using (var fs = new FileStream(requestUnboxing.FilePath, FileMode.Open, FileAccess.Read)) 
                {
                    try
                    {
                        fs.Position = requestUnboxing.StartByte;
                        var actualRead = 0;
                        while (actualRead < sizeBlock)
                        {
                            int countRead;
                            if (sizeBlock - actualRead > int.MaxValue)
                                countRead = int.MaxValue;
                            else
                                countRead = (int)(sizeBlock - actualRead);

                            var bufferForRead = new byte[countRead];
                            var currentRead = fs.Read(bufferForRead, actualRead, countRead);
                            if (currentRead == 0)
                                break;

                            blockFileBytes.AddRange(bufferForRead);

                            actualRead += currentRead;
                        }
                    }
                    finally
                    {
                        fs.Flush();
                        fs.Close();
                    }
                }
                
                var sb = new StringBuilder();
                // ReSharper disable once ConvertToUsingDeclaration
                using (var hash = SHA256.Create())            
                {
                   var result = hash.ComputeHash(blockFileBytes.ToArray());
                    foreach (var b in result)
                        sb.Append(b.ToString("x2"));
                    
                    blockFileBytes.Clear();
                }
                
                Console.WriteLine($"Block № : {requestUnboxing.BlockNumber} , sha256 = {sb}");
                
                _bypassDictionary.TryUpdate(requestUnboxing.BlockNumber, ThreadStatus.COMPLETE, ThreadStatus.START);
            }
            catch (Exception e)
            {
                _bypassDictionary.TryUpdate(_blockNumber, ThreadStatus.ERROR, ThreadStatus.START);
                Console.WriteLine(e);
            }
            finally
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            }
        }
    }
}