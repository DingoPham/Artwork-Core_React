using Amazon.S3;
using Amazon.S3.Model;

namespace Artwork_Core.Services
{
    public class R2Service
    {
        private readonly IAmazonS3 _s3;
        private readonly string _bucket;
        private readonly string _publicUrl;

        public R2Service(IConfiguration config)
        {
            var accountId = config["R2:AccountId"];

            var s3Config = new AmazonS3Config
            {
                ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true,
                AuthenticationRegion = "auto"
            };

            _s3 = new AmazonS3Client(
                config["R2:AccessKey"],
                config["R2:SecretKey"],
                s3Config
            );

            _bucket = config["R2:BucketName"];
            _publicUrl = config["R2:PublicUrl"];
        }

        public async Task<string> Upload(IFormFile file)
        {
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var request = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = fileName,
                InputStream = memoryStream,
                ContentType = file.ContentType,
                DisablePayloadSigning = true,
                UseChunkEncoding = false
            };

            await _s3.PutObjectAsync(request);

            return $"{_publicUrl}/{fileName}";
        }
    }
}
