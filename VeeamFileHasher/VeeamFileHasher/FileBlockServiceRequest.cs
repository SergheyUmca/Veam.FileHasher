namespace VeeamFileHasher
{
    public class FileBlockServiceRequest
    {
        public string FilePath { get; set; }
        
        public long StartByte { get; set; } 
        
        public long SizeBlock { get; set; } 
        
        public long BlockNumber { get; set; }
    }
}