using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace VeeamFileHasher
{
    public class FileBlockService
    {
        private readonly Dictionary<long, int> _bypassDictionary;
        private long _blockNumber;
        public FileBlockService(Dictionary<long, int> bypassDictionary)
        {
            _bypassDictionary = bypassDictionary;
        }
        
        public async void FileBlockCalcHash(object request)
        {
            try
            {
                var requestUnboxing = (FileBlockServiceRequest)request;
                _blockNumber = requestUnboxing.BlockNumber;
                _bypassDictionary[requestUnboxing.BlockNumber] = 1;
                
                var d = GC.GetTotalMemory(true);
                var blockFileBytes = new List<byte>();
                var sizeBlock = requestUnboxing.SizeBlock;
                
                var d2 = GC.GetTotalMemory(true);
                await using (var fs = new FileStream(requestUnboxing.FilePath, FileMode.Open, FileAccess.Read)) 
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
                        if(currentRead == 0)
                            break;

                        blockFileBytes.AddRange(bufferForRead);

                        actualRead += currentRead;
                    }
                    
                    fs.Flush();
                    fs.Close();
                }
                var d4 = GC.GetTotalMemory(true);
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
                
                _bypassDictionary[requestUnboxing.BlockNumber] = 2;
            }
            catch (Exception e)
            {
                _bypassDictionary[_blockNumber] = 3;
                Console.WriteLine(e);
            }
            finally
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                var d3 = GC.GetTotalMemory(true);
            }
        }
    }
}