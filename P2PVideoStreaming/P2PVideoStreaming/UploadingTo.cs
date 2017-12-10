using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PVideoStreaming
{
    class UploadingTo
    {
        public UploadingTo()
        {
            FileSection = null;
            PercentageUploaded = 0;
        }

        public UploadingTo(FileSection fileSection, double percentageUploaded)
        {
            FileSection = fileSection;
            PercentageUploaded = percentageUploaded;
        }

        public static implicit operator DownloadingFrom(UploadingTo uploadingTo)
        {
            if (uploadingTo == null)
                return null; 

            return new DownloadingFrom
            {
                FileSection = uploadingTo.FileSection,
                PercentageDownloaded = uploadingTo.PercentageUploaded
            };
        }

        public FileSection FileSection { get; set; }
        public double PercentageUploaded { get; set; }
    }
}
