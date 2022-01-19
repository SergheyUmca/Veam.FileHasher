using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace VeeamFileHasher
{
    public class FileHandler
    {
        // 0 - Init, 1 - Start, 2 Complete, 3 error
        private readonly ConcurrentDictionary<long, int> _bypassDictionary = new ConcurrentDictionary<long, int>(); 
        
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

                var getOptimalThreadsCount = CheckOptimalCountThreads(blockSize, fileSize);
                var d = GC.GetTotalMemory(true);
                var listActiveThreads = new long[getOptimalThreadsCount];
                var listLazyThreads = new List<KeyValuePair<Thread, FileBlockServiceRequest>>();
                long prevBlockPosition = 0;
                for (var i = 0; i < blockCounts; i++)
                {
                    _bypassDictionary.TryAdd(i, 0);
                    var countRead = i < blockCounts - 1 ? blockSize : lastBlockSize;
                    if (i < getOptimalThreadsCount)
                    {
                        var newThread = new Thread( new FileBlockService(_bypassDictionary).FileBlockCalcHash);
                        newThread.Start(new FileBlockServiceRequest
                        {
                            BlockNumber = i,
                            SizeBlock = countRead,
                            StartByte = prevBlockPosition,
                            FilePath = filePath
                        });

                        listActiveThreads[i] = i;
                    }
                    else
                    {
                        listLazyThreads.Add(new KeyValuePair<Thread, FileBlockServiceRequest>(
                            new Thread(new FileBlockService(_bypassDictionary).FileBlockCalcHash), 
                            new FileBlockServiceRequest
                            {
                                BlockNumber = i,
                                SizeBlock = countRead,
                                StartByte = prevBlockPosition,
                                FilePath = filePath
                            } ));
                    }

                    prevBlockPosition += countRead;
                }

                while (true)
                {
                    if(listLazyThreads.Count == 0)
                        break;
                    
                    var i = 0;
                    while (true)
                    {
                        if(i == listActiveThreads.Length || listLazyThreads.Count == 0)
                            break;
                        
                        if (_bypassDictionary[listActiveThreads[i]] != 2)
                        {
                            if (_bypassDictionary[listActiveThreads[i]] == 3)
                            {
                                var countRead = listActiveThreads[i] < blockCounts - 1 ? blockSize : lastBlockSize;
                                listLazyThreads.Add(new KeyValuePair<Thread, FileBlockServiceRequest>(
                                    new Thread(new FileBlockService(_bypassDictionary).FileBlockCalcHash), 
                                    new FileBlockServiceRequest
                                    {
                                        BlockNumber = listActiveThreads[i],
                                        SizeBlock = countRead,
                                        StartByte = prevBlockPosition,
                                        FilePath = filePath
                                    } ));
                            }

                            _bypassDictionary.TryUpdate(listActiveThreads[i], 0, 3);
                            
                            i++;
                            continue;
                        }

                        var (key, value) = listLazyThreads.First();
                        key.Start(value);

                        listActiveThreads[i] = value.BlockNumber;

                        listLazyThreads.RemoveAt(0);
                    }
                }
                
                while (true)
                {
                    if(_bypassDictionary.Values.Any(v => v == 0 || v == 1))
                        continue;
                    
                    break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        public long CheckOptimalCountThreads(long blockSize, long fileLength)
        {
            try
            {
                var blockCounts = fileLength / blockSize;
                var lastBlockSize = fileLength % blockSize;

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (lastBlockSize > 0)
                    blockCounts++;

                var t = GC.GetTotalMemory(false);
                
                var listForLoadMemory = new List<byte[]>();
                for (var i = 0; i < blockCounts; i++)
                {
                    try
                    {
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                        if (lastBlockSize > 0 && i == blockSize - 1)
                            listForLoadMemory.Add( new byte [lastBlockSize + lastBlockSize/2]);
                        else
                            listForLoadMemory.Add( new byte [blockSize + blockSize/2]);
                    }
                    catch (Exception)
                    {
                        listForLoadMemory.Clear();
                        return i / 2;
                    }
                }
                listForLoadMemory.Clear();
                var d = GC.GetTotalMemory(false);
                var sum = d - t;
                
                return blockCounts;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                var d = GC.GetTotalMemory(true);
            }
        }
    }
}