using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PVideoStreaming
{
    class DownloadingFrom
    {
        public DownloadingFrom()
        {
            FileSection = null;
            PercentageDownloaded = 0;
        }

        public DownloadingFrom(FileSection fileSection, double percentageDownloaded)
        {
            FileSection = fileSection;
            PercentageDownloaded = percentageDownloaded;
        }

        public static implicit operator UploadingTo(DownloadingFrom downloadingFrom)
        {
            if (downloadingFrom == null)
                return null; 

            return new UploadingTo
            {
                FileSection = downloadingFrom.FileSection,
                PercentageUploaded = downloadingFrom.PercentageDownloaded
            };
        }

        public FileSection FileSection { get; set; }
        public double PercentageDownloaded { get; set; }
    }
}
