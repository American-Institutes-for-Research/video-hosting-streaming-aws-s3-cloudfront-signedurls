# video-hosting-streaming-aws-s3-cloudfront-signedurls
Video Hosting and Streaming w/ AWS S3 and CloudFront

These code snippets are to be used to help with various video streaming scenarios. Each scenario described below is created as a separate function in the Home Controller, for ease of reuse. 

1. Public S3 Bucket (most basic)
- Uses a public AWS S3 bucket to host the video, for users to pull down for use in their websites.
- Update link to point to your bucket and file

2. Private S3 Bucket
- Uses a private AWS S3 bucket to host a video, for only credentialed sites to use, by downloading the file locally.
- Update link to point to your bucket and file
- Update keys, to use your private access keys
- Update folder to store videos

3. Private S3 Bucket with public CloudFront distribution
- Uses a private AWS S3 bucket, to limit access to the video file, but allows public access through CloudFront.
- Update link to point to your CloudFront distribution and file

4. Private S3 Bucket with signed URLs in CloudFront (most restrictive)
- Uses a private AWS S3 Bucket, and requires signed URLs to access the video through CloudFront.
- Update link to point to your CloudFront distribution and file
- Add your .pem and PolicyStatement.json files to the project